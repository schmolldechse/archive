# Themengebiet 2: Regeln und öffentliche Verträge

Stand: 16. Oktober 2026. Geprüfter Ausgangspunkt: `87692cc`.

Status: Lösungsvorschlag zur Freigabe. Dieses Dokument enthält die geplanten Änderungen; Anwendungscode wurde für dieses Themengebiet noch nicht geändert.

## 1. Ziel und festgelegte Grenzen

Fachliche Regeln sollen eine gemeinsame Implementierung haben. Öffentliche Verträge sollen konsistent benannt, vollständig beschrieben und mit dem tatsächlichen Verhalten vereinbar sein. Die vorhandenen ASP.NET-Core-Funktionen, DataAnnotations, `IValidatableObject` und Mapperly bleiben die Grundlage.

Die vom Benutzer festgelegten Snapshotgrenzen gelten weiter: Titel 512, Beschreibung 2.048, Quell-URL 4.096 und einzelner Tag 128 Zeichen. Die vorhandenen Unterschiede zwischen Erstellen und Suchen bleiben erhalten, beispielsweise das Verwerfen leerer Such-Tags gegenüber der Ablehnung leerer Tags beim Erstellen.

Für weitere Feldobergrenzen gelten nach der Rückfrage die Beschränkungen der Entitäten. Damit gelten für Anbieterkennung und Anbieteranzeigename jeweils 64 Zeichen. Die vorhandenen Profilregeln bleiben fachlich bestehen: Handle 4–32, Anzeigename 6–64 Zeichen, nach der vorgesehenen Normalisierung. Entitätsobergrenzen bestimmen die Maxima; zusätzliche Mindestlängen und Formatregeln für vollständig registrierte Profile bleiben erforderlich.

Die Längenangabe für vom Backend erzeugte Ressourcen-URLs ist noch Gegenstand der Rückfrage in Abschnitt 8. Sie wird vor der Umsetzung ausdrücklich entschieden.

Nicht Bestandteil dieser Umsetzung sind Projektentfernung, Verarbeitungsparallelität, Zusammenlegung der Speicheradapter, Änderungen der Aufnahme- und Importverfahren oder ein allgemeiner Umbau der Repositories. Kommentare werden in den hier bearbeiteten Regeln, Verträgen und Mappings überarbeitet.

## 2. Befunde

### 2.1 Profilregeln prüfen verschiedene Werte

`Archive.Api/Contracts/Authentication/UpdateUserProfile.cs` prüft mit `[MinLength]` und `[MaxLength]` die unveränderte Eingabe. Seine `Validate`-Methode konstruiert anschließend `UserProfileUpdate`, ruft darauf jedoch nur `Validate` auf. Dieser direkte Methodenaufruf führt die DataAnnotations des Core-Modells nicht aus.

`Archive.Core/Models/Users/UserProfileUpdate.cs` normalisiert bereits im Konstruktor: äußere Leerzeichen entfernen, Anzeigename nach Unicode NFC normalisieren und Handle invariant kleinschreiben. Das Repository führt auf diesem Modell nochmals die vollständige DataAnnotations-Validierung aus. API und Repository bewerten dadurch nicht dieselben Werte.

Am aktuellen Build mit `Validator.TryValidateObject(..., validateAllProperties: true)` nachvollzogen:

| Eingabe für `displayName`, jeweils mit gültigem Handle | API-Vertragsmodell | Core-Modell |
| --- | --- | --- |
| 64 Buchstaben, davor und danach ein Leerzeichen | ungültig: mehr als 64 Zeichen | gültig: nach Normalisierung 64 Zeichen |
| `Alice ` | gültig: Rohwert hat sechs Zeichen | ungültig: nach Normalisierung fünf Zeichen |

Der zweite Fall wird heute erst im Repository abgewiesen. Der erste widerspricht der eigenen API-Beschreibung, die die Länge nach der Bereinigung verspricht.

Zusätzlich steht die Handlelänge in der Regex nochmals als `{2,30}`. Damit existiert neben den Attributen eine weitere Längendefinition.

### 2.2 Allgemeine Validierung ist an Snapshots gebunden und nochmals implementiert

`SnapshotValidation.GetErrors` führt DataAnnotations aus, sammelt Feldfehler, erhält Fehler ohne Feldzuordnung und schreibt Eigenschaftsnamen in camelCase um. Diese Methode ist nicht snapshotspezifisch.

`UserRepository.UpdateProfileAsync` implementiert denselben Grundablauf separat. Dort werden Eigenschaftsnamen nicht umgeschrieben; außerdem würden Ergebnisse ohne `MemberNames` verloren gehen. Der technische Ablauf und die Ausgabe unterscheiden sich somit ohne fachlichen Grund.

### 2.3 Validierungsfehler haben verschiedene Feldnamen

Profilfehler können `DisplayName` und `Handle` oder `displayName` und `handle` enthalten. Die Frontend-Action in `routes/(site)/account/complete/+page.server.ts` muss deshalb beide Varianten auswerten.

Für Snapshoterstellung wird `originUrl` an zwei Stellen in `sourceUrl` übersetzt: in `SnapshotRequestValidation` und in `SnapshotsController.Processing`. Ursache ist der unterschiedliche Name im internen Eingabemodell und im HTTP-Vertrag.

Bei der Suche wird `Order` mit `nameof(request.Order)` als Fehlerfeld verwendet, während andere Fehler `order` nennen. Die Regel für gültige Sortierwerte steht in Vertrag, Controller und Core-Modell.

### 2.4 Gleiche Grenzen werden als unabhängige Zahlen wiederholt

Die Snapshotgrenzen stehen inzwischen in `SnapshotValidation`. Die aktiven Entitäten `Snapshot` und `Tag` verwenden dennoch eigene Zahlen in ihren Attributen.

Für Profile stehen 32 und 64 unter anderem in `ArchiveUser`, `UserProfileUpdate`, `UpdateUserProfile`, `AuthenticatedArchiveUser` und `ArchiveCreatorResponse`. Die Mindestlängen 4 und 6 werden ebenfalls wiederholt. Das führt beim nächsten Ändern einer Regel wieder zu denselben Abweichungsmöglichkeiten.

Für Anbieterkennung und Anbieteranzeigename dokumentiert `AuthenticationProvider` jeweils 32 Zeichen, während `AuthProvider` jeweils 64 zulässt. Die Entitätsgrenzen von 64 sind nach Rückfrage maßgeblich.

Historische Migrationen enthalten ebenfalls Zahlen. Sie sind die Beschreibung früherer Datenbankstände und werden nicht nachträglich auf veränderliche Konstanten umgestellt.

### 2.5 Doppelte Sortier-Enums und abweichendes Query-Binding

`Archive.Api.Contracts.SnapshotOrder` und `Archive.Core.Models.Snapshots.SnapshotSortOrder` enthalten beide nur `Desc` und `Asc`. Mapperly übersetzt zwischen ihnen. Eine fachliche Trennung ist hier nicht erkennbar.

Bei `SourceType` dokumentieren die JSON-Attribute und generierten TypeScript-Typen `URL`, `HTML`, `MHTML` und `WEBARCHIVE`. Das MVC-Query-Binding verwendet jedoch den .NET-Typkonverter; JSON-Attribute ändern dessen Verhalten nicht. Mit den tatsächlichen Enumtypen nachvollzogen:

| Query-Wert | Konvertierung im gegenwärtigen Binding |
| --- | --- |
| `URL` | `SourceType.Url` |
| `HTML`, `MHTML`, `WEBARCHIVE` | abgewiesen |
| `HtmlFile`, `WebArchiveFile` | akzeptiert |
| `1` | `SourceType.HtmlFile` |

Das ist insbesondere für das Frontend relevant: Es sendet die dokumentierten Werte aus den generierten Enums. Für `quality` funktionieren die aktuellen Namen durch die Groß-/Kleinschreibung zufällig zusammen; bei den Dateiarten unterscheiden sich auch die Wörter.

### 2.6 Öffentliche Metadaten und Typgestaltung sind unvollständig

Unter `Archive.Api/Contracts` liegen 14 öffentliche Objektverträge und ein Enum. Hinzu kommen die dort verwendeten vier öffentlichen Core-Enums, die Framework-Fehlerantworten und die Ereignisdaten des SSE-Endpunkts.

Konkrete Lücken:

- `SnapshotReceipt`: weder explizite JSON-Feldnamen noch Typ- oder Feldbeschreibungen.
- `SnapshotProcessingResponse`: weder explizite JSON-Feldnamen noch Beschreibungen; `From` enthält Mappingcode im Vertrag.
- `SnapshotResponse`: keine Typbeschreibung; keine Feldbeschreibung für `sourceType`, `captureStartedAt` und `captureCompletedAt`.
- `SnapshotSearchRequest`, `SnapshotListResponse`, `CaptureDistributionResponse` und `CaptureYearResponse`: keine Typbeschreibung.
- Die beiden Erstellungsverträge haben XML-Zusammenfassungen, aber keine `[Description]` am Typ. XML-Beispiele sind zusätzlich zum expliziten OpenAPI-Beispiel hinterlegt, ohne aktivierte XML-Dokumentationsgenerierung im API-Projekt.
- Authentifizierungsantworten sind veränderliche Klassen mit `set`; Snapshotantworten sind positionale Records mit teilweise sehr langen Attributzeilen.
- `AuthenticationSession.User` ist im generierten Vertrag optional, wird zur Laufzeit aber auch für anonyme Sitzungen als `user: null` ausgegeben. Vorhandensein und Nullbarkeit werden nicht einheitlich beschrieben.
- `ArchiveCreator` deklariert die öffentliche Record-Eigenschaft `profileImage` abweichend von der sonstigen C#-Schreibweise.
- Anbieter heißen je nach Stelle `AuthProviderDescriptor`, `AuthenticationProvider` und `IAuthenticationProvider`; ihre Kennung heißt `Id`, `Key` oder `ProviderKey`.
- Das benannte SSE-Fehlerereignis wird mit einem anonymen `{ status, title }`-Objekt geschrieben. Auch `/healthz` liefert ein anonymes JSON-Objekt.

Das Fehlen von `[JsonPropertyName]` erzeugt heute nicht automatisch falsches JSON: Die Web-Standardeinstellung ist camelCase. Es fehlt aber die im übrigen Vertragsbestand verwendete ausdrückliche Festlegung.

### 2.7 Mapping und JSON-Einstellungen werden unterschiedlich organisiert

Profilbild-URLs entstehen in `ArchiveIdentityClaims.GetProfileImageUrl` und in `ArchiveCreatorMapper.GetProfileImageUrl`. Beide berechnen denselben SHA-256-Versionsparameter und bauen den Link auf. Die unterschiedlichen Zielpfade sind beabsichtigt: aktueller Benutzer versus öffentliches Profil nach Handle. Die URL-Erzeugung gehört allerdings nicht zur Auswertung von Identity-Claims.

JSON-Enumkonvertierung wird in `Program.cs` für MVC und HTTP-JSON eingerichtet und im SSE-Controller nochmals mit eigenen `JsonSerializerOptions` definiert. Eine spätere Änderung kann gewöhnliche JSON-Antworten und Ereignisdaten auseinanderlaufen lassen.

### 2.8 Mehrere Schutzebenen sind keine überflüssigen Duplikate

Diese Strukturen haben unterschiedliche Aufgaben und bleiben grundsätzlich erhalten:

- API-Verträge beschreiben JSON, Query oder Multipart. Core-Modelle enthalten nur die für den Anwendungsfall benötigten Daten. Beispielsweise gehört `IFormFile` nicht ins Core-Modell, und `CreatedByUserId` darf nicht frei aus dem öffentlichen Erstellungsrequest übernommen werden.
- JSON- und Multipart-Erstellung haben bewusst verschiedene Transportverträge, obwohl vier Metadatenfelder gleich aussehen.
- Die Einmaligkeit eines Handles benötigt eine Prüfung gegenüber anderen Benutzern und einen eindeutigen Datenbankindex. Eine reine Textvalidierung kann das nicht ersetzen.
- `[Index]` in `ArchiveUser` und die Fluent-Konfiguration beschreiben denselben Index. Das bedeutet nicht, dass zwei physische Indizes existieren. Die Konfiguration ist dennoch redundant; die gefilterte Fluent-Konfiguration ist die maßgebliche Definition.
- Erzeugte URL- und Importmanifeste haben heute Version 1 beziehungsweise 2 und verschiedene Inhalte. Sie werden in diesem Thema nicht zu einem neuen gemeinsamen Speicherformat umgebaut.
- Ein öffentlicher Benutzerhinweis und das Profil des angemeldeten Benutzers unterscheiden sich insbesondere bei Nullbarkeit und Registrierungsstatus. Sie werden nicht in einen gemeinsamen Antwort-Basistyp gezwungen.

## 3. Einordnung nach gängiger Praxis

Die folgenden Empfehlungen sind eine auf dieses Repository zugeschnittene Ableitung aus der .NET-Dokumentation; sie sind keine allgemeine Vorgabe, dass jede API genau diese Typnamen oder Ordner verwenden muss.

| Frage | Dokumentierte Grundlage | Vorschlag für dieses Repository |
| --- | --- | --- |
| Attribute und zusätzliche Regeln | DataAnnotations und `IValidatableObject` ergänzen sich. Ein direkter Aufruf von `Validate` ersetzt die Auswertung der Attribute nicht. | Beides behalten; gemeinsame vollständige Validierung statt unterschiedlicher Aufrufpfade. |
| API und Core getrennt? | DTOs begrenzen die öffentliche Datenform und verhindern die ungeprüfte Freigabe interner Felder. | Transportmodelle behalten, Regeln gemeinsam nutzen. |
| Records oder Klassen? | Records bieten Wertgleichheit und sind für Datenmodelle geeignet. Klassen mit `init` können ebenfalls unveränderlich verwendet werden. EF-Entitäten benötigen Referenzidentität. | Bewusste Konvention nach Rolle; keine pauschale Konvertierung aller Typen. |
| Record-Attribute | MVC-Binding liest bei positionalen Request-Records Validierungsmetadaten an Konstruktorparametern; OpenAPI verwendet Eigenschaftsmetadaten. | Request-Klassen mit expliziten Properties vermeiden diese Zweideutigkeit. Antwort-Records bekommen ebenfalls explizite Properties. |
| JSON-Feldnamen | camelCase ist Webstandard; `[JsonPropertyName]` überschreibt die NamingPolicy. | JSON-Verträge legen jeden Namen explizit fest. Query-/Formnamen werden mit den passenden Bindingattributen festgelegt. |
| Fehlerschlüssel | ASP.NET Core bietet `SystemTextJsonValidationMetadataProvider` für JSON-Namen in Validierungsfehlern. | Frameworkprovider verwenden und manuell hinzugefügte Core-Fehler nach derselben Namenskonvention übertragen. |
| Öffentliche Beschreibungen | OpenAPI berücksichtigt `[Description]`, Längen-, Bereichs- und Pflichtmetadaten. `required` bedeutet nicht automatisch nicht-null. | Beschreibungen und tatsächlich garantierte Constraints ergänzen; Nullbarkeit und Vorhandensein getrennt behandeln. |

Quellen:

- [Microsoft: Model validation, einschließlich IValidatableObject und JSON-Fehlerschlüsseln](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0).
- [Microsoft: automatische HTTP-400-Antworten und ValidationProblemDetails](https://learn.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-10.0#automatic-http-400-responses).
- [Microsoft: DTOs und die Begrenzung öffentlicher Datenformen](https://learn.microsoft.com/en-us/aspnet/web-api/overview/data/using-web-api-with-entity-framework/part-5/). Die Beispiele stammen aus älterem Web API; verwendet wird hier nur die weiterhin anwendbare Begründung für DTOs.
- [Microsoft: C# record types](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/records).
- [Microsoft: System.Text.Json und unveränderliche Typen](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/immutability).
- [Microsoft: Model binding, insbesondere Konstruktorbindung und Record-Metadaten](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-9.0#constructor-binding-and-record-types).
- [Microsoft: JSON-Eigenschaftsnamen und Enum-Namen](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/customize-properties).
- [Microsoft: OpenAPI-Metadaten und Required-/Nullable-Verhalten](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/include-metadata?view=aspnetcore-10.0).
- [.NET-10-Quellcode: vollständige DataAnnotations-Validierung](https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.ComponentModel.Annotations/src/System/ComponentModel/DataAnnotations/Validator.cs).
- [ASP.NET-Core-10-Quellcode: EnumTypeModelBinder](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Mvc/Mvc.Core/src/ModelBinding/Binders/EnumTypeModelBinder.cs) und [SimpleTypeModelBinder](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Mvc/Mvc.Core/src/ModelBinding/Binders/SimpleTypeModelBinder.cs).

## 4. Geplante Zusammenführung der Regeln

### 4.1 Fachliche Werte einmal definieren

1. `SnapshotValidation` bleibt die vorhandene Stelle für Snapshotgrenzen, Tagregeln und Quell-URL-Prüfung. Die aktiven Entitätsattribute referenzieren dieselben Konstanten.
2. Eine kleine `UserProfileValidation` in `Core/Models/Users` enthält die Profilgrenzen, die bestehenden Normalisierungsfunktionen und die Formatregeln. `ArchiveUser`, Profilmodelle und Profilverträge verwenden dieselben Werte.
3. Die Handle-Regex prüft nur noch erlaubte Zeichen und Anfang/Ende. Die Grenzen 4 und 32 stehen ausschließlich in den gemeinsamen Längenkonstanten und daraus gespeisten Attributen.
4. Für Anbieterkennung und Anbieteranzeigename werden die bestehenden 64-Zeichen-Grenzen gemeinsam benannt und in Entität und Antwortvertrag verwendet. Anbieterwerte werden nicht mit Benutzerprofilnamen gleichgesetzt.
5. Paginationwerte werden zwischen Suchrequest, Core-Suche und Listenantwort gemeinsam benannt: Seite 1–1.000.000, Seitengröße 1–100, Standardseitengröße 24. Dabei erhält die Listenantwort dieselbe maximale Seitennummer wie die Suche.

Historische Migrationen, andere fachlich verschiedene URL-Arten und reine Storagepfadlängen werden nicht unter eine globale String-Konstante gestellt.

### 4.2 Profile vor der Längenprüfung normalisieren

Der Profilrequest bleibt eine eigene Klasse. Seine `init`-Zuweisungen verwenden dieselben Normalisierungsfunktionen wie der Core-Konstruktor. Die Attribute sehen damit bereits die normalisierten Werte. `Validate` selbst verändert keine Properties.

Das Core-Modell normalisiert weiterhin auch bei direkter Verwendung ohne HTTP. Die Funktionen sind wiederverwendbar und idempotent; es gibt keine zweite Implementierung derselben Bereinigung. Eine doppelte Ausführung derselben kleinen Funktion an zwei Eingangsgrenzen ist zulässig, wenn beide Grenzen eigenständig funktionieren müssen.

`IValidatableObject` bleibt erhalten. Die API-Delegation führt die vollständige Core-Validierung einschließlich Attributen aus, statt lediglich dessen `Validate`-Methode aufzurufen. Handleverfügbarkeit wird weiterhin asynchron im Repository geprüft, nicht im synchronen Validator.

Die Profilgrenzen beziehen sich weiter auf .NET-Stringlängen, also UTF-16-Codeeinheiten. Eine Umstellung auf sichtbare Grapheme wäre eine eigene fachliche Änderung und ist hier nicht vorgesehen.

### 4.3 Einen technischen Ablauf für Validierungsfehler verwenden

Die allgemeine Auswertung wird aus `SnapshotValidation.GetErrors` in einen kleinen gemeinsamen Core-Helfer `ModelValidation.GetErrors` verschoben. Snapshot- und Profilrepositories verwenden ihn.

Er wertet DataAnnotations mit `validateAllProperties: true` und damit auch `IValidatableObject` aus, erhält Meldungen ohne Feldzuordnung und liefert die vorhandenen `FieldValidationError`-Werte. Er verwendet keine HTTP- oder JSON-Typen. Die bekannte Reihenfolge von DataAnnotations bleibt bestehen: Objektvalidierung wird nicht zusätzlich erzwungen, wenn vorgeschaltete Eigenschaftsprüfungen bereits fehlschlagen.

In Core erzeugte Eigenschaftsverweise verwenden soweit möglich `nameof`. Die Umsetzung der Namen in öffentliche camelCase-Schlüssel liegt in der API. Dafür genügen der Framework-Metadatenprovider und ein kleiner gemeinsamer Helfer für das Hinzufügen der Repositoryfehler zu `ModelState`; ein eigenes Reflexions- oder Validierungsframework wird nicht eingeführt.

Das Core-Erstellungsmodell und das Core-Lesemodell verwenden `SourceUrl` statt `OriginUrl`. Mapperly bildet die persistierte Eigenschaft `Snapshot.OriginUrl` ausdrücklich darauf ab. Damit entfallen beide wiederholten `originUrl`-zu-`sourceUrl`-Sonderfälle. Die Datenbankspalte `origin_url` und das bestehende Speicherschema werden dabei nicht umbenannt.

### 4.4 API-Verhalten über die vorhandenen Frameworkfunktionen vereinheitlichen

Die globale Unterdrückung des automatischen ModelState-Filters wird nach Absicherung der JSON-, Query- und Multipartfehler entfernt. ASP.NET Core übernimmt wieder die identischen HTTP-400-Prüfungen am Beginn der Actions. Fachliche Fehler aus Repository-Ergebnissen werden weiterhin ausdrücklich als `ValidationProblemDetails` zurückgegeben.

Es bleiben überall flache Fehlerfelder wie `displayName`, `handle`, `sourceUrl` und `order`. Der Frontend-Fallback auf PascalCase wird anschließend entfernt. Überflüssige leere Fehlerlisten oder wiederholte identische Meldungen werden nicht ausgegeben.

Die eindeutige Handlevergabe bleibt durch den vorhandenen Datenbankindex abgesichert. Soweit eine konkurrierende Speicherung dessen konkreten Unique-Constraint verletzt, wird sie derselben bereits dokumentierten HTTP-409-Antwort für `handle` zugeordnet. Andere Datenbankfehler werden nicht als Handlekonflikt maskiert. Die doppelte Indexdeklaration in `ArchiveUser` entfällt zugunsten der bestehenden Fluent-Konfiguration; die physische Indexdefinition bleibt bestehen.

### 4.5 Sortierung und Query-Enums vereinheitlichen

Es bleibt ein gemeinsames `SnapshotSortOrder` mit den unveränderten öffentlichen Werten `desc` und `asc`. Das bisherige API-Enum und das reine Enum-Mapping entfallen. Die bereits vorhandene Verwendung gemeinsamer Core-Enums wird damit fortgeführt.

Ein auf die Such-Enums begrenzter Query-Binder berücksichtigt die vorhandenen `JsonStringEnumMemberName`-Namen. Er enthält keine zweite Tabelle für die Werte von `SourceType`, `SnapshotQuality` oder Sortierung. `HTML`, `MHTML` und `WEBARCHIVE` werden dadurch entsprechend OpenAPI akzeptiert.

Für die Sortierung bleibt die bestehende Regel erhalten: genau ein Parameter mit `asc` oder `desc`. Die HTTP-spezifische Prüfung mehrfach übergebener Werte wird beim Binding behandelt. Sie wird nicht als fachliche Core-Regel nachgebaut.

Für `sourceType` und `quality` bleiben bisher akzeptierte, definierte C#-Namen und numerische Query-Werte aus Kompatibilitätsgründen lesbar; ausgegeben und dokumentiert werden die kanonischen Zeichenketten. Ungültige beziehungsweise nicht definierte Werte bleiben Fehler. Eine Verschärfung auf ausschließlich kanonische Tokens würde bestehende Aufrufer betreffen und ist für das Beheben des dokumentierten Bindings nicht erforderlich.

Core-Suchmodelle prüfen auch bei direkter Verwendung die Gültigkeit ihrer Enumwerte. Sie verlassen sich nicht darauf, dass eine API zuvor gebunden hat.

## 5. Einheitliche Namen und Typen

### 5.1 Benennung

| Bisher | Geplant | Begründung |
| --- | --- | --- |
| `UpdateUserProfile` | `UpdateUserProfileRequest` | Eingabevertrag eindeutig erkennbar. |
| `AuthenticatedArchiveUser` | `UserProfileResponse` | Beschreibt die tatsächlichen Profildaten und passt zu den übrigen Antworten. |
| `AuthenticationSession` | `AuthenticationSessionResponse` | Einheitlicher Antwortsuffix. |
| `AuthenticationProvider` | `AuthenticationProviderResponse` | Verwechslung mit internem Anbieter vermeiden. |
| `SnapshotReceipt` | `CreateSnapshotResponse` | Antwort der bereits so benannten Erstellungsoperation. |
| `SnapshotOrder` plus `SnapshotSortOrder` | ein `SnapshotSortOrder` | Doppeltes Enum und Mapping entfernen. |
| `AuthProviderDescriptor` | `AuthenticationProviderDescriptor` | Abkürzung nur an einzelnen Stellen vermeiden. |
| `IAuthenticationProvider.Id` | `IAuthenticationProvider.Key` | Es handelt sich um den Anbieter-Schlüssel, nicht um eine Entitäts-ID. |
| `ArchiveCreator.profileImage` | `ArchiveCreator.ProfileImage` | Korrekte C#-Eigenschaftsschreibweise. |
| `ExternalIdentityDescriptor.ProfilePicture` | `ExternalIdentityDescriptor.ProfileImage` | Gleiche Bezeichnung für dieselben Bilddaten. |
| `SnapshotCreationRequest.OriginUrl`, `SnapshotDetails.OriginUrl` | jeweils `SourceUrl` | Gleiche Bedeutung wie im öffentlichen Vertrag; weniger Sondermapping. |

Diese Umbenennungen ändern OpenAPI-Komponentennamen und die generierten TypeScript-Typnamen. Alle betroffenen Importe im Frontend werden gemeinsam aktualisiert. HTTP-Routen, Operation-IDs, JSON-Feldnamen und Enum-Werte bleiben bestehen, sofern in diesem Plan keine konkrete Verhaltenskorrektur beschrieben ist.

### 5.2 Typgestaltung

- **API-Requests:** `sealed class` mit expliziten Properties, `init` und `required`, wo das Feld tatsächlich geliefert werden muss. Für Querywerte mit Defaults wird kein künstliches `required` eingeführt.
- **API-Antworten:** `sealed record` mit expliziten Properties und `init`. Immer ausgegebene Felder erhalten `required`, auch wenn ihr Wert null sein darf. Dadurch werden die vorhandenen langen positionalen Attributzeilen aufgelöst.
- **Core-Eingaben, Lesemodelle und Ergebnistypen:** vorhandene Records bleiben Records. Kurze interne Records dürfen positional bleiben; sie benötigen keine JSON- oder OpenAPI-Attribute, solange sie nicht selbst als öffentlicher Vertrag dienen.
- **EF-Entitäten, Optionen, Controller und Dienste:** bleiben Klassen entsprechend ihrer Aufgabe.

Die API-Verträge enthalten keine `From`-Factories oder andere Mappinglogik. Das Mapping von `SnapshotProcessingState` wird in die vorhandene Mapperly-Struktur aufgenommen und für Status, Abbruchantwort und SSE gemeinsam verwendet.

Die beiden flachen Erstellungsrequests behalten ihre eigenen Properties und Transportattribute. Eine neue Vererbungshierarchie nur zum Einsparen ihrer vier Felddeklarationen würde JSON- und Multipart-Binding unnötig koppeln. Die gemeinsamen Regeln und Zahlen sind trotzdem zentral definiert.

## 6. Vollständige öffentliche Metadaten

### 6.1 Feste Konvention

Für jeden selbst definierten JSON-Vertrag gelten:

1. Eine kurze `[Description]` am Typ.
2. Ein explizites `[JsonPropertyName]` und eine sachliche `[Description]` an jedem ausgegebenen oder eingelesenen JSON-Feld.
3. Bei Query-/Formfeldern außerdem beziehungsweise vorrangig `[FromQuery(Name = ...)]` oder `[FromForm(Name = ...)]`. Ein JSON-Attribut ersetzt diese Bindingdefinition nicht. Auf reine Querymodelle werden keine wirkungslosen JSON-Attribute nur für die Optik gesetzt.
4. Garantierte Längen und Zahlenbereiche aus gemeinsamen Konstanten. Kein Attribut wird als Dekoration ergänzt, wenn das System die beschriebene Einschränkung nicht garantiert.
5. Nullbarkeit, Vorhandensein, Defaults und Zeitbezug werden entsprechend der tatsächlichen Ausgabe beschrieben.

`required string?` bei einer Antwort bedeutet beispielsweise, dass der Schlüssel vorhanden ist und sein Wert null sein kann. `[Required]` wird nicht zusätzlich auf nullable Antwortfelder gesetzt, da das eine andere Aussage über gültige Werte wäre. Bei Eingaben bleibt `[Required]` für die inhaltliche Pflichtprüfung sinnvoll.

Die bestehenden Frameworktypen `ProblemDetails` und `ValidationProblemDetails` werden nicht als eigene Projektkopien nachgebaut.

### 6.2 Konkrete Ergänzungen

- Die in Abschnitt 2.6 genannten Lücken werden vollständig geschlossen.
- `AuthenticationSessionResponse.User` wird als stets vorhandenes nullable Antwortfeld beschrieben, entsprechend der heutigen Serialisierung.
- Der aktuelle Benutzer und der öffentliche Ersteller erhalten dieselben Profilmaxima, aber ihre fachlich unterschiedliche Nullbarkeit bleibt erhalten.
- Statusfelder werden konkret beschrieben: `id` ist die Snapshot-ID, `queuePosition` eine einsbasierte Position nur bei wartenden Aufträgen, `canCancel` die aktuell angebotene Abbruchmöglichkeit, `cancelRequested` eine bereits angeforderte Stornierung und `startedAt` der nullable Verarbeitungsbeginn.
- Die Zeitfelder eines Snapshots beschreiben den Beginn und das Ende der Verarbeitung, einschließlich möglicher Nullwerte. Es wird kein Zeitpunkt als tatsächlicher Aufnahmezeitpunkt ausgegeben, den der Code nicht zusichert.
- Ein kleiner benannter Vertrag für das SSE-Fehlerereignis legt `status` und `title` fest. Operationbeschreibung und Beispiele dokumentieren den Unterschied zu normalen Statusereignissen. Die gemeinsame Statusantwort wird weiter sowohl per HTTP als auch über SSE verwendet.
- `/healthz` erhält einen kleinen benannten Antwortvertrag für das bestehende `{ "status": "ok" }`. Inhalt und Bedeutung des Endpunkts ändern sich nicht.
- Die Array-Elementgrenze für Tags bleibt im vorhandenen Schema-Transformer, wird aber ausdrücklich auf die betreffenden Snapshotverträge beziehungsweise Suchparameter begrenzt. Ein beliebiges zukünftiges Feld namens `tags` erhält nicht automatisch eine Snapshotregel.
- Beispiele werden an den tatsächlich verwendeten OpenAPI-Stellen gepflegt. Die nicht verwendeten doppelten XML-Beispiele entfallen.

### 6.3 Sachliche Sprache

Die Beschreibungen sagen, was ein Feld enthält, wann es null ist und welche Einschränkungen gelten. Sie enthalten keine Werbung, keine Entstehungsgeschichte und keine Aufforderungen an spätere Entwickler.

Beispiele für die vorgesehene Form:

| Feld | Vorgesehene englische Beschreibung |
| --- | --- |
| `sourceUrl` | `Original HTTP or HTTPS URL of the archived page.` |
| `screenshotUrl` | `Screenshot URL. Null until the snapshot is published.` |
| `contentUrl` | `Archived start page URL. Null until the snapshot is published.` |
| `queuePosition` | `One-based queue position. Null when the snapshot is not queued.` |
| `captureStartedAt` | `Processing start time, including the UTC offset. Null before processing starts.` |

Längen stehen primär als maschinenlesbare Metadaten im Schema. Ergänzender Text ist dort sinnvoll, wo er beispielsweise die Normalisierung oder eine Unterscheidung zwischen Rohwert und gespeichertem Wert erklärt.

Kommentare, die nur den unmittelbar sichtbaren Code wiederholen oder sich gegen eine frühere Implementierung abgrenzen, entfallen. Hinweise auf nicht offensichtliche technische Anforderungen bleiben in neutraler Form erhalten, etwa zur EF-Projektion oder zum Besitz eines Streams.

## 7. Mapping und Serialisierung

Die Profilbild-Linkbildung wird in einem kleinen API-Helfer zusammengeführt. Er verwendet den bestehenden Bildhash und unterstützt die beiden vorhandenen Routen für das eigene und das öffentliche Profil. Die beiden Zugriffswege bleiben erhalten. `ArchiveIdentityClaims` ist anschließend wieder auf Claims und Benutzer-ID beschränkt. Das Format der Bild-ETags und der gespeicherten Bilder wird hier nicht umgestellt.

Die Enum-JSON-Einstellungen erhalten eine gemeinsame Konfigurationsfunktion für die vorhandenen MVC- und HTTP-JSON-Optionen. Der SSE-Controller verwendet die konfigurierten MVC-Serializeroptionen, statt ein drittes eigenes Optionsobjekt zu pflegen. Alle Pfade behalten String-Enums ohne numerische JSON-Werte bei.

Die vorhandenen Snapshot-, Authentifizierungs- und Ersteller-Mapper bleiben die Stellen für API-Mapping. Ein allgemeines Mappingframework, neue Dienste für einzelne Propertyzuweisungen oder Basisklassen für sämtliche Antworten werden nicht ergänzt.

## 8. Offene Entscheidung: erzeugte Ressourcen-URLs

Die Felder erfüllen unterschiedliche Aufgaben:

| Feld | Herkunft | Verwendung |
| --- | --- | --- |
| `sourceUrl` | Nutzereingabe; gespeichert in `Snapshot.OriginUrl` | Ursprüngliche Website, URL-Aufnahme und Suche. Beim Dateiimport die angegebene Originaladresse. |
| `screenshotUrl` | Öffentliche Backendadresse plus `/api/snapshots/{id}/screenshot` | Vorschaubild des Snapshots. |
| `contentUrl` | Öffentliche Backendadresse plus `/api/snapshots/{id}/content/index.html` | Direkter Zugriff auf die archivierte Startseite. Das aktuelle Frontend baut seinen lokalen Link anhand der Snapshot-ID selbst. |
| `profileImageUrl` | Öffentliche API-Adresse plus Profilbildroute und Hashparameter | Gespeichertes Anbieterprofilbild als Avatar im Konto beziehungsweise bei der Erstellerangabe. |

`profileImageUrl` hat heute in zwei Verträgen eine 256-Zeichen-Angabe. Dafür existieren weder eine gespeicherte URL-Spalte noch eine passende Prüfung. `screenshotUrl` und `contentUrl` haben bisher keine solche Längenangabe.

Empfehlung zur Entscheidung: Die nicht garantierte 256-Zeichen-Angabe entfernen und für erzeugte Links keine feste Maximallänge zusichern. Die Quell-URL bleibt auf 4.096 begrenzt. Die Felder werden dadurch nicht entfernt und ihre Aufgaben ändern sich nicht.

Alternative, falls fachlich gewünscht: Für alle erzeugten Links 4.096 Zeichen garantieren. Dann müssen die konfigurierten öffentlichen Adressen zusammen mit den längsten angehängten Routen und Parametern vor der Ausgabe geprüft werden. Ein Abschneiden ist nicht zulässig, da es die Links beschädigen würde. Auch die aus dem Request abgeleitete öffentliche Basisadresse müsste in diese Zusicherung einbezogen oder durch eine verpflichtende Konfiguration ersetzt werden.

Diese Alternative erweitert die Anforderungen an die öffentliche Adresskonfiguration. Sie wird nicht ohne Entscheidung des Benutzers umgesetzt.

## 9. Umsetzungsschritte nach Freigabe

1. Die Entscheidung aus Abschnitt 8 eintragen. Den erwarteten OpenAPI- und JSON-Bestand als Vergleich für die gezielten Vertragskorrekturen erfassen.
2. Gemeinsame Profil-, Anbieter- und Paginationwerte einführen; aktive Entitäten und Modelle darauf beziehen. Profilnormalisierung und vollständige Validierung vereinheitlichen.
3. Den technischen Validierungshelfer gemeinsam nutzen. Fehlerschlüssel vereinheitlichen, `SourceUrl` in Eingabe-/Lesemodellen angleichen und die automatischen ModelState-Antworten wieder aktivieren.
4. Das doppelte Sortier-Enum entfernen und das Query-Binding der dokumentierten Enumwerte korrigieren. Vorhandene fachliche Such- und Tagsemantik beibehalten.
5. Die aufgeführten Typnamen und die gemeinsame Gestaltungsregel umsetzen; API-Mapping aus den Verträgen in Mapperly verschieben.
6. Öffentliche Metadaten vervollständigen, Texte kürzen, profilbezogene URL-Erzeugung und JSON-Optionen vereinheitlichen.
7. Die API lokal mit dem geänderten Vertrag bereitstellen und im Frontend exakt `bun --bun run generate:api` ausführen. `frontend/openapi-ts.config.ts` und die Art des Spezifikationszugriffs bleiben unverändert. Betroffene Frontendimporte und Fehlerzuordnungen anpassen.
8. Die Abnahmeprüfungen durchführen, ausschließlich die Änderungen dieses Themengebiets zum Index hinzufügen und committen.

Ein neuer Datenbankzustand ist für die vorgeschlagenen Vereinheitlichungen nicht vorgesehen. Vor dem Abschluss wird geprüft, dass die EF-Metadaten weiterhin dieselben Spalten, Typen, Längen und Indizes ergeben. Falls sich wider Erwarten eine echte Schemaänderung als erforderlich erweist, wird sie zunächst konkret erläutert.

Vorgesehene Implementierungsnachricht: `refactor!: unify validation and API contracts`. Das Ausrufezeichen kennzeichnet die Änderungen öffentlicher Schema-/Typnamen und die Vereinheitlichung der Fehlerfelder.

## 10. Abnahme und Prüfungen

### Fachliche Regeln

- Profile direkt über Core und über HTTP prüfen: Mindest-/Maximallänge, jeweils direkt darunter/darüber, äußere Leerzeichen, NFC-Zusammensetzung, ungültige Unicodefolgen, interne Kontrollzeichen und gültige/ungültige Handlezeichen.
- Die beiden in Abschnitt 2.1 reproduzierten Fälle erhalten nach der Korrektur an beiden Grenzen dasselbe Ergebnis.
- Handleverfügbarkeit bleibt ein Feldfehler mit HTTP 409; ein nachgewiesener konkurrierender Verstoß gegen den Handle-Unique-Constraint wird demselben Ergebnis zugeordnet.
- Snapshotgrenzen 512/2.048/4.096/128 werden sowohl direkt im Core als auch über JSON und Multipart eingehalten. Die bestehenden Regeln für Rohwerte, normalisierte URLs, leere Tags und Taganzahl werden nicht stillschweigend geändert.
- Fehler ohne Feldzuordnung bleiben erhalten. Fehlende Pflichtfelder, ungültiges JSON und Multipart-Binding erreichen keinen schreibenden Repositoryaufruf.

### Öffentliche Verträge

- Für die selbst definierten öffentlichen Objektverträge sind Typ- und Feldbeschreibungen sowie die vorgesehenen Namen vollständig. Reine Querymodelle verwenden Bindingnamen statt wirkungsloser JSON-Attribute.
- JSON-Namen, HTTP-Routen, Operation-IDs und kanonische Enumwerte werden mit dem Ausgangsbestand verglichen. Erwartete Änderungen an Komponentennamen, Metadaten und Pflichtfeldern werden ausdrücklich aufgelistet.
- `sourceType=URL|HTML|MHTML|WEBARCHIVE`, beide Qualitätswerte und `order=asc|desc` binden entsprechend dem Schema. Mehrfacher `order`, nicht definierte Werte und die erhaltenen Legacy-Querywerte werden gezielt geprüft.
- Alle bisherigen erfolgreichen Antwortfelder bleiben vorhanden; nullable Felder können weiterhin null sein. `user` ist auch bei anonymer Sitzung vorhanden und null.
- Normale Statusantworten und SSE-Daten verwenden dasselbe Mapping und dieselben JSON-Namen/Enumwerte. Das benannte Fehlerereignis behält `status` und `title`.
- Die Anbietergrenzen lauten im Schema und in der Entitätskonfiguration jeweils 64. Ressourcen-URL-Metadaten entsprechen der noch zu treffenden Entscheidung.
- Die API-Generierung läuft über den unveränderten konfigurierten OpenAPI-Endpunkt. Generierte Dateien werden nicht manuell korrigiert.

Für diese Verhaltensänderungen sind gezielte automatisierte Modell-, HTTP-/Binding- und Schemaprüfungen sinnvoll. Eine kleine Teststruktur kann dafür eingeführt werden; rein mechanische Umbenennungen oder Attributzeilen werden nicht jeweils mit eigenen Tests nachgebildet. Ein Datenbanktest wird nur für den konkret behandelten konkurrierenden Handlekonflikt benötigt und verwendet eine isolierte Testdatenbank.

Abschließende Befehle: Backend-Build und die zugehörigen Tests, `bun --bun run generate:api`, `bun --bun run check`, `bun --bun run build` und Git-Diff-Prüfung. Visuelle Frontendänderungen sind nicht geplant. Sollten sich solche Änderungen als erforderlich erweisen, gelten der benannte Designskill und die Browserprüfung für Desktop und Mobile.

### Bereits für diesen Vorschlag durchgeführt

- Regeln, Verträge, aktive Entitätsattribute, Controller, Mapper, OpenAPI-Transformer und die unmittelbar betroffenen Frontendaufrufer gelesen.
- Profilabweichungen und die Enumkonvertierung mit den gebauten tatsächlichen Typen nachvollzogen; dafür keine Datenbank verwendet oder geändert.
- `dotnet build backend/Archive.slnx --no-restore` mit den lokalen MSBuild-Parametern erfolgreich: keine Fehler. Die zwei bestehenden Worker-Paketwarnungen zu `System.Security.Cryptography.Pkcs` bleiben außerhalb dieses Themengebiets.
- Für diesen Vorschlag wurde ausschließlich dieses Dokument erstellt. Es gab noch keine Implementierung, API-Neugenerierung oder Änderung des Anwendungsschemas.
