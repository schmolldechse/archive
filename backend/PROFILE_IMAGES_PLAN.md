# Themengebiet 3: Profilbilder

Stand: 16. Oktober 2026. Lösungsvorschlag; die Implementierung ist noch nicht freigegeben.

## 1. Festgelegte Anforderungen

- `authentication.users.profile_image` bleibt eine nullable `bytea`-Spalte.
- Künftig enthält die Spalte das fertig erzeugte WebP. Der Nutzer hat diese Speicherung bestätigt.
- Vorhandene Bilder bleiben erhalten und werden einmalig umgewandelt. Der Nutzer hat die Datenübernahme bestätigt.
- Die Routen `/api/users/profile-image` und `/api/users/{handle}/profile-image` bleiben bestehen. Der Nutzer hat bestätigt, dass die URL keine `.webp`-Endung benötigt.
- Die HTTP-Antwort enthält tatsächliche WebP-Daten und `Content-Type: image/webp`.
- Die Umsetzung beginnt erst nach Freigabe dieses Plans. Dieser Stand enthält ausschließlich die Untersuchung und den Vorschlag.

## 2. Aktueller Ablauf und Bewertung

1. [GoogleProvider.cs](C:/Users/christian/Documents/github/archive.org/backend/Archive.Api/Authentication/Provider/GoogleProvider.cs) lädt das Google-Profilbild herunter. Es gelten drei Sekunden Zeitlimit und maximal 2 MiB. Weiterleitungen sind deaktiviert. Die Dateikennung wird auf JPEG, PNG oder WebP geprüft.
2. [AuthenticationRepository.cs](C:/Users/christian/Documents/github/archive.org/backend/Archive.Core/Repositories/AuthenticationRepository/AuthenticationRepository.cs) speichert die heruntergeladenen Originalbytes bei der ersten Kontoanlage. Bei späteren Anmeldungen wird derzeit nur `LastLoginAt` geändert. Diese Übernahme bei der Kontoanlage bleibt der fachliche Ausgangspunkt dieses Plans.
3. [ProfileImageRepository.cs](C:/Users/christian/Documents/github/archive.org/backend/Archive.Core/Repositories/ProfileImageRepository/ProfileImageRepository.cs) lädt für jeden Bildabruf einen vollständigen Benutzer, dekodiert das Bild mit SkiaSharp, kodiert es verlustfrei als WebP und berechnet SHA-256 über das Ergebnis.
4. [ProfileImageController.cs](C:/Users/christian/Documents/github/archive.org/backend/Archive.Api/Controllers/User/ProfileImageController.cs) übersetzt das Ergebnis in HTTP: Bild, 401 oder 404, Cache-Control, `nosniff` und ETag.
5. [ProfileImageUrls.cs](C:/Users/christian/Documents/github/archive.org/backend/Archive.Api/Authentication/ProfileImageUrls.cs) berechnet zusätzlich einen Hash über die gespeicherte Originaldatei für `?v=...`. Dazu enthält auch `ArchiveCreator` die Bildbytes; die Snapshot-Projektion transportiert sie bis zur API-Abbildung.

**Bewertung des Controllers:** Seine Zuständigkeit und Größe sind angemessen. Zwei Actions für unterschiedliche Zugriffsregeln und eine gemeinsame Methode für die Bildantwort sind verständlich. Die OpenAPI-Attribute beschreiben öffentliche Antworten und sind sinnvoll. Ein weiterer Controller, ein eigener HTTP-Cache-Dienst oder zusätzliche Vermittlungsschichten würden den Ablauf hier nicht verbessern. Microsoft empfiehlt, Controller auf HTTP-Aufgaben zu konzentrieren und umfangreiche Verarbeitung auszulagern: [ASP.NET Core MVC](https://learn.microsoft.com/en-us/aspnet/core/mvc/overview?view=aspnetcore-10.0).

Der Controller liefert schon heute echte WebP-Bilder aus. Das wesentliche Problem liegt in der wiederholten Verarbeitung im Repository und in einer falschen Zugriffsregel.

### Konkrete Befunde

| Befund | Auswirkung | Vorgeschlagene Änderung |
| --- | --- | --- |
| Dekodierung und WebP-Kodierung bei jedem Abruf | Wiederholte CPU-Arbeit, Speicherallokationen und Abhängigkeit vom Decoder im Leseweg | Einmal beim Speichern normalisieren; gespeicherte Bytes ausliefern |
| Auch bei passendem `If-None-Match` wird zunächst kodiert | 304 spart Übertragung, bisher aber nicht die Bildverarbeitung | Gespeichertes WebP lesen; ASP.NET wertet den ETag aus |
| Beide Repository-Methoden verlangen abgeschlossene Registrierung | Ein angemeldeter Nutzer erhält vor Registrierungsabschluss 401 für sein eigenes Bild, obwohl die Registrierungsseite es anzeigt | Eigenes Bild anhand gültiger Sitzung und vorhandenem Konto freigeben; öffentliche Freigabe weiter an abgeschlossene Registrierung binden |
| Nur Dateigröße und Dateikennung werden beim Import geprüft | Kleine komprimierte Dateien können große Pixelpuffer erzeugen; eine passende Kennung beweist keine vollständige Dekodierbarkeit | Format und Dimensionen vor Pixelallokation prüfen; vollständige Dekodierung verlangen |
| `Lossless, 100` bei jedem Abruf | Maximaler Kompressionsaufwand auf dem häufigen Leseweg | Einmalige verlustfreie Kodierung mit Aufwand 75 als vorgeschlagenem Ausgangswert |
| Hash in Bild-URL und ETag werden getrennt berechnet | Zusätzliche Arbeit und zwei verschiedene Versionsbegriffe; Snapshot-Metadaten benötigen dadurch Bildbytes | ETag über ausgelieferte Bytes behalten; URL-Versionsparameter entfernen |
| Repository delegiert Bildabfragen an das allgemeine Benutzerrepository | Vollständige Benutzer werden für eine reine Bildantwort geladen | Im Bildrepository direkt die benötigten Spalten mit EF projizieren |

ASP.NET Core behandelt bedingte Dateiantworten bereits im `FileResultHelper`, einschließlich 304 und 412. Eigene Vergleiche von `If-None-Match` sind dafür nicht erforderlich: [ASP.NET-Core-10-Quellcode](https://github.com/dotnet/aspnetcore/blob/v10.0.0/src/Shared/ResultsHelpers/FileResultHelper.cs).

Der Wert 100 steuert im verlustfreien WebP-Modus den Kompressionsaufwand und nicht eine höhere Bildqualität. Ein niedrigerer Wert kodiert schneller und kann größere Dateien ergeben: [Skia-WebP-Encoder](https://github.com/google/skia/blob/main/include/encode/SkWebpEncoder.h).

Die derzeit verwendete SkiaSharp-Methode `SKBitmap.Decode` akzeptiert auch `IncompleteInput`. Deshalb genügt für den neuen Schreibweg die Prüfung auf eine nicht-null Bitmap nicht: [SkiaSharp 4.153.1](https://github.com/mono/SkiaSharp/blob/v4.153.1/binding/SkiaSharp/SKBitmap.cs).

## 3. Zielablauf

```text
Google-Bild -> begrenzter Download -> einmalige Prüfung und WebP-Kodierung -> bytea
                                                                            |
Bild-GET -> Zugriffsprüfung -> gezielte Datenbankabfrage -> FileContentResult <-+
                                                           ETag / 200 / 304
```

Es gibt eine gemeinsame Implementierung für die Bildnormalisierung. Neue Bilder und die einmalige Datenkonvertierung verwenden dieselbe Funktion. Der reguläre Bildabruf führt ausschließlich Datenbankzugriff, Hashberechnung und HTTP-Ausgabe aus.

## 4. Einmalige Bildnormalisierung beim Schreiben

Eine kleine konkrete Klasse `ProfileImageEncoding` im Core bündelt die Regeln und die SkiaSharp-Verarbeitung. Sie benötigt kein eigenes Interface und keine auswählbaren Encoderstrategien. Das `AuthenticationRepository` verwendet sie unmittelbar vor dem erstmaligen Speichern eines Profilbildes.

Vorgeschlagene technische Werte, die mit der Planfreigabe bestätigt werden:

| Eigenschaft | Vorschlag | Begründung |
| --- | --- | --- |
| Eingangsformate | JPEG, PNG, WebP | Entspricht dem vorhandenen Import |
| Maximale Downloadgröße | 2 MiB | Bestehende Grenze beibehalten und zentral referenzieren |
| Maximale Quelldimensionen | Je 4.096 Pixel Breite und Höhe | Begrenzt die zu dekodierenden Bildflächen; RGBA benötigt hierfür bereits bis zu 64 MiB allein für Pixel |
| Maximale Ausgabedimensionen | 512 Pixel an der längsten Seite | Die vorhandene Avatar-Komponente zeigt regulär 32, 40 oder 64 CSS-Pixel; 512 bietet Reserve für hohe Pixeldichte |
| Skalierung | Seitenverhältnis erhalten, kleine Bilder unvergrößert | Kein zusätzlicher Zuschnitt; die Darstellung erfolgt weiterhin durch die vorhandene Avatar-Komponente |
| Kompression | WebP lossless, Aufwand 75 | Keine zusätzliche verlustbehaftete Bildkompression; eine Größenanpassung kann trotzdem Bildinformationen reduzieren |
| Maximale Ausgabegröße | 2 MiB | Auch das tatsächlich gespeicherte Ergebnis bleibt begrenzt |
| Animation | Erstes Bild als statisches Profilbild | Entspricht der bisherigen einzelnen Bitmap-Ausgabe |

Die Umsetzung prüft die erkannte Kodierung mit `SKCodec`, liest die Dimensionen vor der Bitmap-Allokation und akzeptiert nur eine erfolgreiche vollständige Dekodierung des verwendeten Bildes. EXIF-Ausrichtung wird in die Pixel übernommen; Transparenz bleibt erhalten. Beim Erzeugen des neuen Bildes werden entbehrliche Quellmetadaten nicht übernommen. Die Implementierung und die Prüfungen berücksichtigen die Farbdarstellung von Bildern mit Farbprofil.

Für Inhaltsprüfung, begrenzte Größen und das erneute Erzeugen von Bilddaten gibt es etablierte Empfehlungen. Die konkreten Grenzen oben sind Projektentscheidungen, keine universellen Standardwerte: [OWASP File Upload Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html), [SKCodec](https://github.com/mono/SkiaSharp/blob/v4.153.1/binding/SkiaSharp/SKCodec.cs).

Der Google-Downloader bleibt für Transport, Zeitlimit und die Begrenzung während des Lesens zuständig. Die bisherige separate Dateikennungsprüfung `ProfileImageContent` entfällt zugunsten der tatsächlichen Prüfung im Encoder. Eine ungültige, zu große oder nicht vollständig dekodierbare Bilddatei verhindert die Kontoanlage nicht; das neue Konto erhält dann kein Profilbild. Technische Fehler wie eine fehlende native SkiaSharp-Bibliothek werden weiterhin als technische Fehler behandelt.

## 5. Bestehende Bilder erhalten und einmalig konvertieren

Der Spaltentyp und das EF-Schema ändern sich nicht. Benötigt wird eine Datenkonvertierung mit dem Anwendungsencoder; PostgreSQL kann die Bildkodierung nicht durch ein gewöhnliches `ALTER TABLE` erledigen.

Vorgeschlagen wird ein gezielt aufrufbarer Wartungsbefehl in der API-Anwendung:

- `--convert-profile-images`: standardmäßig nur prüfen und die Anzahl konvertierbarer, unveränderter und problematischer Bilder ausgeben.
- `--convert-profile-images --apply`: dieselbe Verarbeitung anwenden und ausschließlich die Bildspalte der betroffenen Nutzer aktualisieren.
- Der Befehl verwendet die bestehende Konfigurationsquelle für die Datenbank. Er startet keinen HTTP-Server und keine regulären Hintergrunddienste.
- Kleine Batches, stabile Reihenfolge nach Nutzer-ID und Freigabe nativer Bildressourcen nach jedem Bild halten den Speicherbedarf begrenzt.
- Ein Wiederholungslauf überspringt byteidentische Ergebnisse. Die Idempotenz des Encoders wird geprüft.
- Fehlerhafte oder außerhalb der Grenzen liegende Bestandsbilder werden mit Nutzer-ID und Fehlergrund ausgewiesen. Sie werden weder gelöscht noch auf null gesetzt. Ein fehlerhafter Prüflauf sperrt die Anwendung des vollständigen Bestands, bis die betroffenen Bilder geklärt sind.
- Ein Fehler bei der Anwendung liefert einen Fehlerstatus und einen nachvollziehbaren Zwischenstand; erfolgreich konvertierte Bilder bleiben bei einer Wiederholung gültig. Updates überschreiben keine zwischenzeitlich geänderten Quellbytes.

Für die Umstellung: Bestand sichern, Kontoanlagen während des Konvertierungsfensters anhalten, Prüflauf durchführen, Konvertierung anwenden, Bestand abschließend prüfen und anschließend die neue API mit reinem WebP-Leseweg starten. So werden keine GET-Anfragen zu versteckten Datenbank-Schreibzugriffen und es bleibt kein dauerhafter Decoder für Altformate im regulären Abruf.

Die Freigabe dieses Plans erlaubt die Implementierung und Prüfung mit isolierten Daten. Die Anwendung auf eine konkrete produktive Datenbank wird als gesonderter Ausführungsschritt mit dem konkreten Prüfergebnis vorbereitet.

## 6. Repository und Ergebnisstruktur vereinfachen

`IProfileImageRepository` behält seine beiden fachlich klaren Abfragen:

- `GetByUserIdAsync`: vorhandenes eigenes Konto genügt, auch während der Registrierung.
- `GetByHandleAsync`: nur öffentlich freigegebene, vollständig registrierte Nutzer.

Das Repository fragt direkt über `DataContext` ab und projiziert nur die für die jeweilige Entscheidung nötigen Werte. Die Leseabfragen bleiben ohne Tracking. Die vorhandenen drei Ergebnisse bleiben sinnvoll: Nutzer nicht gefunden, kein Bild vorhanden, Bild gefunden. Dadurch kann der private Endpunkt ein fehlendes Konto weiterhin von einem fehlenden Bild unterscheiden.

`Found` enthält unmittelbar die WebP-Bytes. Das zusätzliche Modell `ProfileImage(Content, ContentHash)` entfällt. Die ETag-Berechnung gehört zur HTTP-Antwort und findet in der gemeinsamen Controller-Methode statt. Konvertierung und kryptografische HTTP-Metadaten entfallen dadurch aus dem Repository.

## 7. Controller und HTTP-Vertrag

Die bestehende Controller-Struktur bleibt erhalten. Die private Action verwendet eine ausdrückliche Fallunterscheidung anstelle des abschließenden erzwungenen Casts. Eine gemeinsame Methode erzeugt weiterhin die erfolgreiche Bildantwort.

| Anfrage | Vorgesehenes Verhalten |
| --- | --- |
| Eigenes Bild ohne gültige Sitzung | 401 |
| Eigenes Bild mit gültiger Sitzung, aber ohne vorhandenes Konto | 401 |
| Eigenes Bild während noch laufender Registrierung | 200, falls Bild vorhanden; sonst 404 |
| Öffentliches Bild eines nicht registrierten oder unbekannten Handles | 404 |
| Vorhandenes Bild | 200, WebP-Bytes und `image/webp` |
| Fehlendes Bild | 404 als ProblemDetails |
| Passender ETag in `If-None-Match` | 304 ohne Antwortkörper |
| Nicht passende explizite `If-Match`-Vorbedingung | 412 ohne Antwortkörper durch die vorhandene FileResult-Verarbeitung |

ETags werden als SHA-256 über genau die ausgelieferten gespeicherten WebP-Bytes gebildet. `File(byte[], "image/webp", ..., entityTag)` übernimmt die HTTP-Vorbedingungen. `X-Content-Type-Options: nosniff` bleibt gesetzt. Für diese eingebetteten Bilder wird kein erzwungener Dateidownload eingeführt.

Für erfolgreiche Antworten bleiben `private, no-cache` beim eigenen Bild und `public, no-cache` beim öffentlichen Bild bestehen. `no-cache` erlaubt die Speicherung, verlangt aber die erneute Validierung vor Wiederverwendung. Das ist hier passend: Der private Pfad hängt von der Sitzung ab, der öffentliche Pfad vom aktuellen Handle. Die vom Controller ausgegebenen Fehler erhalten `no-store`, damit ein fehlendes Bild nicht als veralteter Fehler zwischengespeichert wird. Grundlage: [RFC 9111, Cache-Control](https://www.rfc-editor.org/rfc/rfc9111.html#section-5.2.2).

Die englischen Endpunktbeschreibungen werden auf das tatsächlich zugesicherte Verhalten gekürzt. Bei normalisierten Bestandsdaten ist die Zusage gespeicherter WebP-Bilder ausreichend; eine vollständige erneute Bildprüfung bei jedem Abruf wird nicht versprochen. Die bereits vorhandene 412-Reaktion wird in der API-Dokumentation ergänzt.

## 8. Profilbild-URLs und Snapshot-Metadaten

Die beiden bestehenden URL-Pfade bleiben unverändert. Der Parameter `?v=<Hash der Originaldatei>` entfällt in neu erzeugten URLs. Bereits ausgegebene URLs mit diesem Parameter bleiben abrufbar; die Actions verwenden ihn schon heute nicht zur Auswahl einer gespeicherten Bildversion. Bei `no-cache` mit ETag ist für die vorgesehenen Abrufe kein zusätzlicher URL-Hash erforderlich.

`ArchiveCreator` enthält künftig `HasProfileImage` statt `byte[]? ProfileImage`. Die EF-Projektion ermittelt diesen booleschen Wert in der Datenbank aus der Bildspalte. So transportieren Snapshot-Listen und Snapshot-Details die Bilddatei nicht mehr zur Berechnung einer URL. `ArchiveCreatorMapper` und `ProfileImageUrls` verwenden den Wert für Bild-URL oder null. Es ist keine zusätzliche Hashspalte erforderlich. Npgsql dokumentiert die Übersetzung von Bytearray-Operationen: [Npgsql-Übersetzungen](https://www.npgsql.org/efcore/mapping/translations.html).

Die Profileigenschaften `profileImageUrl` bleiben nullable Strings. Benutzerbezogene Antworten bilden die vorhandene Entity weiterhin ab; die speziell für Bild-GETs und Snapshot-Metadaten verwendeten Datenzugriffe werden gezielt verschmälert.

## 9. Betroffene Dateien und Abnahme

Die Implementierung betrifft die Bildnormalisierung im Core, `AuthenticationRepository`, den Google-Bilddownload, das Bildrepository mit Ergebnis, den Profilbildcontroller, die gemeinsame URL-Erzeugung und die unmittelbar zugehörige Erstellerprojektion. Hinzu kommen der einmalig aufzurufende Wartungsbefehl und die notwendigen API-Dokumentations- beziehungsweise generierten Typänderungen.

Im aktuellen Checkout existiert kein Backend-Testprojekt. Die folgenden Prüfungen werden gezielt für diese Änderung durchgeführt und die Ergebnisse dokumentiert:

1. Kleine JPEG-, PNG- und WebP-Beispiele einschließlich Transparenz, EXIF-Ausrichtung und Farbprofil normalisieren und unabhängig wieder dekodieren. Format, Seitenverhältnis, Pixelgrenzen und sichtbares Ergebnis prüfen.
2. Dateien mit falscher Kennung, passender Kennung aber defekten Daten, unvollständiger Dekodierung sowie Werte an und über den Byte- und Dimensionsgrenzen prüfen.
3. Statisches Ergebnis bei animierten Eingaben sowie unvergrößerte kleine Bilder prüfen. Wiederholte Normalisierung muss byteidentische Ergebnisse liefern oder vor einem zusätzlichen Schreiben als unverändert erkannt werden.
4. Neue Kontoanlage mit gültigem Bild speichert WebP; eine ungültige Bilddatei ergibt ein Konto ohne Bild. Bestehende Anmeldungen behalten den bisherigen Übernahmezeitpunkt bei.
5. Die Statusfälle aus Abschnitt 7, private und öffentliche Cache-Header, starke und schwache `If-None-Match`-Werte, `If-Match`, leere 304-/412-Antworten und tatsächliche WebP-Daten über HTTP prüfen.
6. Den Wartungsbefehl auf einer isolierten Datenbank mit JPEG, PNG, WebP und null ausführen: Prüflauf schreibt nicht, Anwendung erhält Konten und sonstige Spalten, Wiederholung ändert keine bereits normalisierten Bilder. Defekte Daten führen zu einem ausgewiesenen Fehler und bleiben erhalten.
7. Das von EF erzeugte SQL der Snapshot-Erstellerprojektion prüfen: zurückgegeben wird der Bildverfügbarkeitswert, nicht die Bilddatei. Bild-GETs führen keinen Decoder oder Encoder mehr aus.
8. Backend bauen. Wegen der geänderten öffentlichen Antwortdokumentation und der URL-Ausgabe die Frontendtypen mit exakt `bun --bun run generate:api` regenerieren. `frontend/openapi-ts.config.ts` und die Art des OpenAPI-Zugriffs bleiben erhalten. Bei geänderten generierten Typen die betroffenen Frontend-Verwendungen prüfen.

Die Untersuchung dieses Planstands war statisch und durch Primärquellen ergänzt. Es wurden keine Datenbankinhalte gelesen oder geändert, keine Bestandsbilder konvertiert und keine Anwendungstests ausgeführt.

Nach Freigabe werden die zugehörigen Änderungen geprüft, gezielt gestaged und mit einer kurzen englischen Commit-Nachricht eingecheckt, vorgesehen: `refactor: normalize stored profile images`.
