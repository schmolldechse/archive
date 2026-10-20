# Lösungsvorschlag: Ressourcen ausstehender Snapshots privat ausliefern

Stand: 2026-10-20

Geprüfter Repository-Stand: `fd63cc4` (`master` und `origin/master`)

Status: Lösungsvorschlag; es wurden keine Produktcodeänderungen vorgenommen.

## 1. Ziel

Eigentümer und berechtigte Moderatoren sollen einen vollständig verarbeiteten, noch nicht genehmigten Snapshot in der privaten Detailansicht korrekt ansehen können. Dazu gehören das HTML-Dokument, Stylesheets und darin referenzierte Bilder, Schriftarten sowie weitere gespeicherte Ressourcen.

Die öffentliche Sichtbarkeit bleibt unverändert: Ein ausstehender Snapshot und seine Ressourcen bleiben über die öffentlichen API-Routen nicht erreichbar. Die privaten Lesezugriffe bleiben an Eigentümerschaft beziehungsweise Moderationsberechtigungen gebunden.

## 2. Bestätigter Fehler und technische Ursache

Der Fehler wurde mit dem angemeldeten Eigentümer und Snapshot `84af9bac-5b5b-4413-aa27-b5f2de367dc5` reproduziert. Die Frontend-Seite unter `/snapshots/{id}` lädt das private HTML-Dokument erfolgreich über `/api/users/snapshots/{id}/content/index.html`. Der Text ist vorhanden, die Darstellung ist jedoch ungestylt; mindestens ein Bild wird ebenfalls nicht geladen.

Die Seite versucht nach einem öffentlichen `404` zunächst den privaten Admin-Endpunkt und danach den privaten Eigentümer-Endpunkt. Die private API-Antwort verweist für das Einstiegsdokument auf die Route der jeweiligen Berechtigung.

Beim Speichern der URL-Aufnahme erzeugt `BrowserCaptureEngine` allerdings Verweise wie `/api/snapshots/{id}/content/assets/{hash}` und setzt dieselbe öffentliche Route als `<base href>`. `OfflinePageNormalizer` verwendet für importierte HTML-, MHTML- und WebArchive-Dateien ebenfalls `/api/snapshots/{id}/content/`. Die öffentliche Snapshot-Abfrage filtert Datensätze auf den Status `APPROVED`. Deshalb kann der private Einstieg geladen werden, während seine Stylesheets und übrigen Ressourcen öffentlich `404` liefern.

Die Moderationsroute verwendet denselben privaten Antwortpfad. Der Adminfall hat somit dieselbe Ursache. Ein eigener Browserdurchlauf mit einer Admin-Sitzung wurde nicht vorgenommen, weil für diese Prüfung nur der normale Benutzer angemeldet war.

Betroffene Bereiche:

- `frontend/src/routes/snapshots/[snapshotId]/+page.server.ts` – Fallback auf private Detailrouten
- `backend/Archive.Api/Controllers/SnapshotSubmissionControllerBase.cs` und `SnapshotSubmissionMapper.cs` – private Einstieg-URL
- `backend/Archive.Api/Controllers/User/SnapshotsController.cs` und `Admin/SnapshotsController.cs` – private Inhaltsrouten
- `backend/Archive.Api/Controllers/SnapshotsController.cs` und `backend/Archive.Core/Repositories/SnapshotRepository/SnapshotRepository.cs` – öffentliche Beschränkung auf genehmigte Snapshots
- `backend/Archive.Worker/Capture/Browser/BrowserCaptureEngine.cs` – Ressourcenpfade bei URL-Aufnahmen
- `backend/Archive.Worker/Import/OfflinePageNormalizer.cs` – Ressourcenpfade bei Dateiimporten

## 3. Verifikationsstand

| Fall | Ergebnis |
| --- | --- |
| Ausstehender URL-Snapshot in der Eigentümeransicht | HTML lädt, Styles fehlen, Bild ist defekt. Fehler visuell bestätigt. |
| Öffentliche Ressourcenroute für ausstehenden Inhalt | Bleibt durch den Moderationsfilter verborgen und antwortet mit `404`. |
| MHTML-Import mit extern referenziertem Stylesheet | Lokale API-Annahme `202`; Verarbeitung `COMPLETED`; Moderation `PENDING`. |
| WebArchive-Import mit extern referenziertem Stylesheet | Gültige lokale Probe wurde angenommen und `COMPLETED`; Moderation `PENDING`. |
| Admin-Detailansicht | Implementierungspfad geprüft; kein eigenständiger Browserdurchlauf mit Admin-Berechtigung. |

Für MHTML und WebArchive gab es keine passenden Projektfixtures. Dafür wurden minimale lokale Proben verwendet und die Fixture-Dateien nach dem Test entfernt. Der erste WebArchive-Probeversuch enthielt eine DTD und wurde vom Parser abgelehnt; die korrigierte Property-List-Datei ohne DTD wurde erfolgreich verarbeitet. Die beiden erfolgreichen Importläufe bestätigen die Verarbeitung, nicht die private Browserdarstellung dieser anonym eingereichten Datensätze. Die gemeinsame Normalisierung beider Importformate bestätigt jedoch, dass sie dieselben öffentlichen Ressourcenpfade erhalten.

## 4. Empfohlene Behebung

### Private Textressourcen beim Ausliefern auf die aktuelle Route umschreiben

Die kleinste Behebung, die auch bereits gespeicherte ausstehende Snapshots erfasst, ist eine gezielte Umschreibung im gemeinsamen privaten Inhalts-Handler `SnapshotSubmissionControllerBase.Content`:

1. Der Aufrufer übergibt den privaten Routenstamm, den er bereits für die Detailantwort verwendet: `api/users/snapshots` oder `api/admin/snapshots`.
2. Der Handler ermittelt aus der Snapshot-ID die bekannten alten öffentlichen Präfixe:
   - `/api/snapshots/{id:N}/content/` für URL-Aufnahmen
   - `/api/snapshots/{id:D}/content/` für Dateiimporte
3. Nur bei privaten Antworten vom Inhaltstyp `text/html` oder `text/css` werden diese Präfixe exakt durch `/{routeBase}/{id:D}/content/` ersetzt. Das deckt den `<base>`-Eintrag und die Verweise in HTML sowie `url(...)`- und `@import`-Verweise im CSS ab.
4. Binäre Ressourcen und Originaldateien werden unverändert ausgeliefert. Die öffentliche Inhaltsroute verwendet weiterhin den gespeicherten Inhalt ohne private Umschreibung.

Die Umschreibung muss auf Präfixe der aktuellen Snapshot-ID und auf die bekannten internen Routen begrenzt sein. Sie darf keine fremden absoluten URLs oder beliebigen Text im Archivdokument umschreiben. So werden Eigentümer- und Adminzugriff über ihre jeweils autorisierte Route geführt, während öffentliche Zugriffsbeschränkungen bestehen bleiben.

`SnapshotSubmissionControllerBase` liefert heute Streams mit aktivierter Range-Verarbeitung aus. Für veränderte HTML- und CSS-Antworten müssen daher Antwortlänge und Range-Verhalten konsistent behandelt werden. Die Implementierung sollte nur Textressourcen puffern beziehungsweise umschreiben und Binärressourcen weiter streamen. Für umgeschriebene Textantworten ist Range-Verarbeitung zu deaktivieren, sofern keine korrekte Range-Berechnung über die transformierten Bytes implementiert wird. Content-Type, CSP, `X-Content-Type-Options` und `Referrer-Policy` sind beizubehalten.

### Warum diese Variante empfohlen wird

- Sie behebt bereits gespeicherte ausstehende Snapshots, ohne deren Capture erneut auszuführen oder gespeicherte Objekte migrieren zu müssen.
- Sie behandelt URL-Aufnahmen und alle Dateiimporte über denselben Auslieferungspfad.
- Sie ändert das Verhalten öffentlicher Routen nicht und erweitert keine Berechtigungen.
- Die Transformation ist auf private HTML- und CSS-Antworten begrenzt; Binärdaten bleiben unverändert.

## 5. Alternative für eine spätere Entkopplung

Langfristig können Capture und Import route-neutrale relative Ressourcenverweise speichern. HTML am Pfad `content/index.html` könnte Ressourcen relativ zur Content-Wurzel referenzieren; CSS unter `content/assets/{hash}` müsste Ressourcen relativ zum CSS-Verzeichnis referenzieren. Für gerahmte Importdokumente wären die relativen Pfade entsprechend ihrer Verzeichnistiefe zu berechnen.

Diese Variante entkoppelt gespeicherte Dokumente von API-Routen, behebt bestehende Datensätze aber nicht allein. Sie benötigt entweder eine kompatible Umschreibung alter Inhalte oder eine erneute Verarbeitung vorhandener Snapshots. Deshalb ist sie nicht die erste Korrektur für den bestätigten Fehler.

Ein Fallback der öffentlichen Route auf private Zugriffsrechte wird nicht empfohlen: Er koppelt eine anonyme Route an Cookies und Berechtigungsprüfungen und erschwert die klare Grenze zwischen öffentlichem und privatem Zugriff.

## 6. Umsetzungsschritte nach Freigabe

1. Einen kleinen, gezielt testbaren Umschreiber für private HTML- und CSS-Ressourcenreferenzen ergänzen.
2. Den privaten Routenstamm aus den Eigentümer- und Admin-Controllern an den gemeinsamen Inhalts-Handler übergeben.
3. Umschreibung auf `text/html` und `text/css` beschränken; Binärantworten, Originalquellen und öffentliche Antworten unverändert lassen.
4. Antwortheader und Range-Verhalten für transformierte und unveränderte Inhalte explizit erhalten beziehungsweise festlegen.
5. Bestehende Testkonventionen prüfen. In der geprüften Arbeitskopie waren keine separaten Backend-Testprojekte oder Dateiimport-Fixtures vorhanden; bei Bedarf einen passenden fokussierten Testsatz ergänzen.
6. Die lokale UI mit dem vorhandenen Eigentümer-Snapshot erneut prüfen und danach die Adminansicht mit einem Konto testen, das `admin.access` und `admin.snapshots.access` besitzt.

## 7. Test- und Abnahmematrix

| Szenario | Erwartung |
| --- | --- |
| Ausstehender URL-Snapshot, Eigentümer | Private HTML-, CSS-, Bild- und Schriftart-Anfragen nutzen `/api/users/snapshots/{id}/content/...` und laden erfolgreich. |
| Ausstehender URL-Snapshot, berechtigter Moderator | Dieselben Inhalte laden über `/api/admin/snapshots/{id}/content/...`. |
| Ausstehender Snapshot, fremder Benutzer oder anonymer Zugriff | Private Route bleibt unzugänglich; öffentliche Metadaten- und Inhaltsrouten liefern weiterhin `404`. |
| Genehmigter Snapshot, öffentliche Ansicht | Öffentliche Ressourcen bleiben erreichbar; die private Umschreibung wird nicht angewendet. |
| MHTML und WebArchive | Eingebettete Stylesheets, CSS-`url(...)`- und `@import`-Ressourcen werden über die passende private Route aufgelöst. |
| GUID-Format | Alte Referenzen mit und ohne Bindestriche werden auf die private Route normalisiert. |
| Ressourcentyp | HTML/CSS werden umgeschrieben; Bilder, Fonts, Medien und Original-Uploads bleiben bytegleich. |
| Sicherheitsheader und Antwortverhalten | CSP und Schutzheader bleiben erhalten; Range-Verarbeitung für unveränderte Binärressourcen bleibt funktionsfähig. |

Abnahmebedingung ist eine gestylte private Vorschau für Eigentümer und Moderatoren bei unverändertem `404`-Verhalten für ausstehende Snapshots auf öffentlichen Routen.

## 8. Nicht Bestandteil dieses Vorschlags

Dieser Vorschlag implementiert die Behebung nicht, ändert keine Datenbankmigration und entscheidet keine Moderationseinträge. Die lokale Entwicklungsumgebung bleibt von der Produktänderung getrennt.
