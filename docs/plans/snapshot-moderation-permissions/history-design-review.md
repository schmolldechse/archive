# Fachliche Prüfung des JSONB-Historienmodells

Stand: 2026-10-17. Planungsentscheidung, keine Implementierung.

## Ergebnis

Die bestätigte Aufteilung wird beibehalten: **eine konkrete Historienentität für gemeinsame Metadaten und ein abstrakter, typisierter Dateninhalt in einer JSONB-Spalte**. Das ist für diesen begrenzten, nur ergänzten Ereigniskatalog eine passende Anwendung dokumentierter JSON- und EF-Mapping-Verfahren. Es ist keine Aussage, dass ein eigener Konverter für jede JSON-Spalte die bevorzugte Lösung wäre.

Das Projekt verwendet laut [Archive.Core.csproj](../../../backend/Archive.Core/Archive.Core.csproj) .NET 10, EF Core 10.0.10 und Npgsql EF Core 10.0.3. Die Entscheidung setzt kein Update dieser Bibliotheken voraus.

## Abgleich der Mapping-Varianten

| Variante                                           | Bewertung für diese Historie                                                                                                                                                                    |
| -------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| EF-Complex-Type mit `ToJson()`                     | Allgemein für fest modellierte JSON-Strukturen empfohlen. Die benötigte Vererbung der abstrakten Payload ist dabei nicht einfach durch JSON-Serializerattribute ergänzt.                        |
| Expliziter ValueConverter mit JSONB-String-Mapping | Gewählter Weg: lokale Serialisierung der kleinen abstrakten Payload-Hierarchie; eine Spalte und keine zusätzliche EF-Entitätenhierarchie.                                                       |
| JSON-DOM, etwa JsonElement                         | Dokumentierte Alternative für variable Dokumente; der Fachcode müsste die typisierten Eintragsdaten zusätzlich daraus abbilden. Für die gewünschte abstrakte Property nicht der einfachste Weg. |
| Altes automatisches POCO-Mapping                   | Für neue Planung nicht gewählt; Npgsql bezeichnet diesen älteren Weg als überholt.                                                                                                              |

Npgsql empfiehlt für modellierte JSON-Daten grundsätzlich `ToJson()`/Complex Types und dokumentiert daneben String- und DOM-Mapping. Diese allgemeine Empfehlung wird berücksichtigt, aber nicht ungeprüft auf eine polymorphe Hierarchie übertragen. [Npgsql: JSON Mapping](https://www.npgsql.org/efcore/mapping/json.html)

EFs Dokumentation nennt die fehlende Vererbungsunterstützung für Complex Types. Deshalb wird hier nicht angenommen, dass `ComplexProperty(...).ToJson()` die abstrakte Basisklasse und alle konkreten Eintragsarten automatisch materialisiert. [Microsoft: Complex Types](https://learn.microsoft.com/en-us/ef/core/modeling/complex-types)

## Konkret gewähltes Verfahren

1. `SnapshotHistoryEntry` enthält die gemeinsamen Spalten und `Data`.
2. `SnapshotHistoryData` wird als abstrakter, unveränderlicher Record modelliert; konkrete Records enthalten nur die Daten ihrer Eintragsart.
3. Genau ein lokaler EF-ValueConverter serialisiert durch den **deklarierten Basistyp** zu JSON und zurück. Enumwerte werden als die dokumentierten Zeichenketten gespeichert. Die Datenbankspalte wird ausdrücklich als `jsonb` konfiguriert. Kein globaler Dynamic-JSON-Schalter und kein allgemeines Konverterframework.
4. `JsonPolymorphic` legt den stabilen Discriminator `type` fest; `JsonDerivedType` registriert jede zulässige konkrete Variante explizit.
5. Kennungen wie `submitted`, `rejected` und `processing_finished` sind fachliche Werte. Keine CLR-/Assembly-Namen im gespeicherten Format; Umbenennen einer C#-Klasse ändert nicht automatisch das Datenformat.
6. Akteur, Snapshot-Bezug und Zeit stehen außerhalb des JSON-Dokuments. Spezifische Angaben wie Ablehnungsgrund oder Publikationsmodus stehen ausschließlich darin.

Das Serialisieren zusammengesetzter Werte über ValueConverter ist ein dokumentiertes EF-Verfahren. Für Typen, deren Daten durch einen solchen Konverter gespeichert werden, können keine beliebigen LINQ-Abfragen auf innere Properties vorausgesetzt werden. [Microsoft: Value Conversions](https://learn.microsoft.com/en-us/ef/core/modeling/value-conversions#composite-value-objects)

EF empfiehlt unveränderliche Typen für ValueConverter. Die geplanten Records enthalten nur unveränderliche Werte wie Strings und Enums und werden nach Erstellung nicht verändert. Deshalb ist hier kein eigener tiefer Change-Tracking-Vergleich für veränderliche Sammlungen erforderlich. Dies gilt für die konkret geplanten Payloads; spätere mutable Daten wären erneut zu bewerten. [Microsoft: Value Comparers](https://learn.microsoft.com/en-us/ef/core/modeling/value-comparers)

## JSON-Serialisierung und Abfragen

Die polymorphe Serialisierung und Deserialisierung mit expliziten Varianten und Typkennungen wird von System.Text.Json unterstützt. Unbekannte Kennungen werden kontrolliert abgewiesen; API-Clients erhalten keinen freien Schreibzugriff auf Historienpayloads. [Microsoft: JSON polymorphism](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/polymorphism)

JSONB erhält die Reihenfolge von Objektschlüsseln nicht. Der lokale Serializer verwendet deshalb `AllowOutOfOrderMetadataProperties`, damit ein umsortierter Discriminator trotzdem gelesen wird. Das ist für die verwendete .NET-Version ohne Paketwechsel möglich. [Npgsql: Polymorphic JSON mapping](https://www.npgsql.org/doc/types/json.html#polymorphic-json-mapping)

Die Konsequenz des Konverters ist ausdrücklich Teil der Planung: Selektion nach JSON-Typkennung oder terminalem Status erfolgt bei Bedarf mit einer kleinen parametrisierten SQL-Projektion im konkreten Repository. Nicht jede Abfrage wird in eine generische JSON-Abfrageschicht verlagert. Historienseiten verwenden SnapshotId und Primärschlüssel; Listen lesen die benötigten letzten Einträge datenbankseitig. Nicht sämtliche Historien erst im Anwendungsspeicher auswerten.

Das erlaubt kleine, unveränderliche Einträge ohne Tabellenvererbung und ohne neue Snapshot-Spalten. Weitere JSON-Änderungen müssen vorhandene gespeicherte Eintragsarten weiterhin lesbar lassen; neue optionale Datenfelder erfordern keinen Änderungszähler.

## Nachweis bei späterer Umsetzung

Die Recherche belegt die gewählten Bausteine und ihre Grenzen. Sie ersetzt keinen Integrationstest des konkreten Konverters. Zur fachlichen Abnahme der späteren Implementierung sind auf PostgreSQL zu prüfen:

- Roundtrip jeder konkreten Eintragsart, inklusive nullablem Ablehnungsgrund.
- Zurücklesen nach tatsächlicher JSONB-Schlüsselumsortierung.
- Kontrolliertes Verhalten bei unbekannter Typkennung und ungültigen Daten.
- Korrekte SQL-Projektion für letzten Einreichungszeitpunkt und terminalen Verarbeitungsausgang, auch nach Processing-Cleanup.
- Reihenfolge und atomare Speicherung unter den bereits geplanten Snapshot-Sperren.
- Keine zusätzlichen Snapshot-Spalten, Versionsspalten oder globalen Provider-Umbauten als Nebenwirkung.

Bis zur ausdrücklichen Implementierungsfreigabe bleibt dies ein dokumentiertes Modell mit dazugehörigen Prüfkriterien.
