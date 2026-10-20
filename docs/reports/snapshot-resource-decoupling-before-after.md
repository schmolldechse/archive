# Fehleranalyse und Verifikation: Ressourcen aus privaten Snapshots

**Stand:** 20. Oktober 2026
**Branch:** `codex/fix/pending-snapshot-stylesheets`
**Basis:** `master` und `origin/master` standen vor Arbeitsbeginn beide auf `fd63cc4`.

## Zusammenfassung

Der gemeldete Fehler ließ sich mit dem ausstehenden W3C-Snapshot des angemeldeten Eigentümers nachvollziehen. Vor der Änderung lud die private Vorschau das HTML-Dokument, aber die darin referenzierten Stylesheets und Bilder wurden nicht über dieselbe private Zugriffsroute geladen. Nach der Entkopplung werden die Ressourcen in der Eigentümeransicht und in der Admin-Vorschau geladen. Ein Browser-Neuladen bestätigte für den W3C-Snapshot drei verfügbare Stylesheets mit 752, 159 und 124 CSS-Regeln sowie 16 von 16 geladenen Bildern.

Die Migration lief zunächst über 41 vorhandene Snapshots: Einer benötigte eine Umstellung, 40 verwendeten bereits relative Ressourcenpfade. Nach der Umstellung bestätigte ein weiterer Dry-Run für 54 Snapshots `would-migrate=0`, `already-relative=54`, `failed=0`. Die alten Objektquellen blieben erhalten; der separate Aufräum-Dry-Run fand keine löschbaren Objekte.

Die Verifikation mit den fünf gewünschten Webseiten hat eine wesentliche Grenze: Die URL-Aufnahmen sind echte Anfragen an W3C, NASA, WHO, NIST und die Europäische Kommission. Die HTML- und WebArchive-Uploads sind dagegen kontrollierte Format-Fixtures mit diesen Ursprungs-URLs, keine vollständigen Kopien der jeweiligen Webseiten. Alle fünf WebArchive-Fixtures wurden vollständig importiert und geladen. Die fünf HTML-Fixtures blieben erwartungsgemäß unvollständig, weil sie auf nicht mitgelieferte externe Ressourcen verwiesen. Von den fünf echten URL-Aufnahmen wurden W3C vollständig und NASA unvollständig erfasst; WHO, NIST und die Europäische Kommission beendeten die Verarbeitung ohne gespeicherten Capture. Diese Einschränkungen sind Capture-/Eingabedaten-Ergebnisse und keine Bestätigung, dass diese fünf echten Seiten in allen Formaten vollständig archiviert wurden.

## Reproduktion des gemeldeten Fehlers

### Eigentümeransicht

Der angemeldete Benutzer `ollogollo` reichte `https://www.w3.org/` ein. Die Aufnahme blieb ausstehend und erhielt die ID `84af9bac-5b5b-4413-aa27-b5f2de367dc5`. Der Benutzer öffnete anschließend:

```text
http://localhost:5173/snapshots/84af9bac-5b5b-4413-aa27-b5f2de367dc5
```

Der Frontend-Loader konnte die privaten Metadaten über die Eigentümer-API abrufen und setzte das Capture-Dokument in einen iframe. Das HTML war erreichbar; im Vorher-Lauf gab es für die Stylesheets keine erfolgreichen Anfragen und es wurden 0 von 28 referenzierten Bildern geladen. Die Vorschau war dadurch sichtbar unformatiert bzw. unvollständig.

### Moderationsansicht

Mit der vom Benutzer autorisierten, temporären Admin-Rolle für `ollogollo` ließ sich derselbe ausstehende Snapshot über die Moderationsoberfläche öffnen. Die Seite zeigte `Pending review`, `Capture complete` und die Review-Schaltflächen. Die Freigabe wurde nicht ausgelöst. Die Admin-Metadaten lieferten eine Vorschau-URL unter `/api/admin/snapshots/{id}/content/...`; auch dieser Zugriff musste die Ressourcen desselben ausstehenden Captures laden können.

Nach dem Admin-Test wurde die Rolle wieder auf `USER` gesetzt und in PostgreSQL verifiziert. Die Moderationsseite ist nicht mehr zugänglich.

## Ursache

Die bisherige URL-Aufnahme schrieb Ressourcenverweise in das HTML und in CSS als absolute Pfade der öffentlichen API, z. B. `/api/snapshots/{id}/content/assets/{hash}`. Die private Detailantwort eines ausstehenden Snapshots zeigte dagegen auf die Eigentümer- bzw. Admin-Content-Route. Diese Route konnte das private HTML ausliefern, während referenzierte Ressourcen weiterhin die öffentliche Route trafen. Der öffentliche Snapshot-Endpunkt gibt ausschließlich freigegebene Snapshots zurück; daher scheiterten die privaten Ressourcenanforderungen.

Zusätzlich verwendete das Frontend `sandbox=""` und `referrerpolicy="no-referrer"`. Der Sandbox-Iframe hatte damit einen opaken Ursprung. Das erschwerte den Cookie-basierten Zugriff auf private Unterressourcen; ohne Referer ließ sich außerdem der anfragende Snapshot nicht bestimmen. Ein unbedingtes Vertrauen auf `Sec-Fetch-Site: same-origin` wäre ebenfalls unsicher, weil ein archiviertes Dokument dann Ressourcen eines anderen ausstehenden Snapshots anfordern könnte.

## Umsetzung

- Neue Captures und importierte HTML-, MHTML- und WebArchive-Dateien verwenden das Manifestformat `relative-v1`. HTML-, CSS-, `srcset`- und Frame-Verweise werden in den gemeinsamen Snapshot-Objektpfad normalisiert, statt auf den öffentlichen Snapshot-Endpunkt zu zeigen.
- Eigentümer- und Admin-Content-Endpunkte liefern private Ressourcen im jeweiligen Berechtigungsumfang. Vorschau-Tickets sind signiert, kurzlebig und an Snapshot-ID sowie Scope gebunden. Fehlende bzw. gekürzte Referer akzeptiert die API nur zusammen mit einem gültigen Ticket für genau die angeforderte Snapshot-ID und den Scope. Explizite Referer-Pfade auf andere Snapshot-IDs oder den anderen Scope werden abgewiesen.
- Der Vorschau-Iframe erlaubt Same-Origin-Zugriff für die Cookie-gebundenen Ressourcen. Die API-CSP behält die Sandbox-Einschränkungen für Skripte, Formulare und Netzwerkverbindungen bei; die Frontend-Header verhindern, dass die App selbst in fremde Frames eingebettet wird.
- Das neue Projekt `Archive.SnapshotMigration` kann vorhandene Manifeste und Objektpfade prüfen und migrieren. Es kopiert zuerst, validiert die Quelle und den Speicherumfang und wechselt den aktiven Pfad anschließend kontrolliert. Alte Quellen werden durch die Migration nicht entfernt.

Die maßgeblichen Änderungen liegen in [SnapshotSubmissionControllerBase.cs](../../backend/Archive.Api/Controllers/SnapshotSubmissionControllerBase.cs), [BrowserCaptureEngine.cs](../../backend/Archive.Worker/Capture/Browser/BrowserCaptureEngine.cs), [OfflinePageNormalizer.cs](../../backend/Archive.Worker/Import/OfflinePageNormalizer.cs) und dem Projekt [Archive.SnapshotMigration](../../backend/Archive.SnapshotMigration/).

## Migrationsläufe

| Lauf | Untersucht | Migriert / vorgesehen | Bereits relativ | Fehler | Ergebnis |
|---|---:|---:|---:|---:|---|
| Dry-Run vor dem ersten Apply | 41 | 1 | 40 | 0 | Eine Legacy-Ablage musste umgeschrieben werden. |
| Apply | 41 | 1 | 40 | 0 | Eine Snapshot-Ablage wurde umgestellt; alte Quellobjekte blieben erhalten. |
| Dry-Run nach den Verifikationsaufnahmen | 54 | 0 | 54 | 0 | Keine weitere Migration ausstehend. |
| Aufräum-Dry-Run | 24 | 0 löschbar | — | 0 | Keine Objekte gelöscht. |

## Vorher-Nachher-Prüfungen

| Prüffall | Vorher | Nachher |
|---|---|---|
| Eigentümer öffnet den privaten W3C-Snapshot | HTML vorhanden; 0/28 Bilder und keine geladenen Stylesheets im protokollierten Vorher-Lauf. | iframe zeigt den Inhalt; 3 Stylesheets sind im Dokument geladen und enthalten 752, 159 bzw. 124 Regeln; 16/16 Bilder haben `naturalWidth > 0`. |
| Admin öffnet denselben ausstehenden Snapshot | Private Vorschau verwendete dieselbe nicht entkoppelte Ressourcenstruktur. | Metadaten und Content laufen über `/api/admin/...`; dieselben 3 Stylesheets und 16/16 Bilder laden. Snapshot bleibt `Pending review`; keine Entscheidung gespeichert. |
| Fehlender oder gekürzter Referer | Nicht snapshot-spezifisch prüfbar; ein bloßes `same-origin` wäre zu weit gefasst. | Fallback benötigt ein gültiges, signiertes Ticket für exakt die angeforderte ID und den Scope. Ein vollständiger Referer auf einen anderen Snapshot wird abgewiesen. |
| Alte Objektpfade | Legacy-Pfade waren an die öffentliche `/api/snapshots/...`-Route gekoppelt. | Migration kopiert und prüft Objekte und stellt den relativen Ressourcenpfad her; anschließender Dry-Run findet keine weiteren Kandidaten. |

Ein unabhängiger Reviewer prüfte den Referer-Fallback zunächst und fand einen P2-Befund: `Sec-Fetch-Site: same-origin` allein band die Anfrage nicht an einen Snapshot. Dieser Befund wurde behoben. Die unabhängige Nachprüfung bestätigte danach, dass Snapshot-ID und Scope im Fallback geprüft werden und explizite Content-Referer auf eine andere Snapshot-ID zurückgewiesen werden. Der Reviewer änderte keine Dateien und führte keine Tests aus.

## Webseite- und Formatmatrix

Alle Einreichungen blieben im Moderationsstatus `PENDING`; es wurde keine Testaufnahme veröffentlicht.

### Echte URL-Aufnahmen

| Webseite | ID | URL | Capture-Ergebnis |
|---|---|---|---|
| W3C | `3646c8aa-c5ab-4015-9a26-82868d021b41` | [www.w3.org](https://www.w3.org/) | `COMPLETE`, 26 Ressourcen, 1.235.382 gespeicherte Bytes. |
| NASA | `e08be8e3-d0e7-41d7-af7c-500d38b26e97` | [www.nasa.gov](https://www.nasa.gov/) | `INCOMPLETE`, 114 Ressourcen, 29.579.280 gespeicherte Bytes. |
| WHO | `75fc93d0-c2a6-4dfb-b01e-656eea9daa97` | [www.who.int](https://www.who.int/) | Verarbeitung beendet, kein gespeicherter Capture-Qualitätsstatus. |
| NIST | `fcee788e-2029-46cb-88ab-ac2bb67f0022` | [www.nist.gov](https://www.nist.gov/) | Verarbeitung beendet, kein gespeicherter Capture-Qualitätsstatus. |
| Europäische Kommission | `fc4f9129-b4e1-4242-b1da-7c89597ba8e4` | [commission.europa.eu](https://commission.europa.eu/) | Verarbeitung beendet, kein gespeicherter Capture-Qualitätsstatus. |

### HTML-Uploads

Die folgenden Uploads waren lokale, kontrollierte HTML-Fixtures. Die im Snapshot hinterlegte Ursprungs-URL ordnet sie der jeweiligen Webseite zu; der HTML-Inhalt ist keine gespeicherte Kopie der Live-Webseite. Jedes Fixture enthält nicht mitgelieferte externe Ressourcen. Daher wurde es als `INCOMPLETE` markiert und nicht als vollständige Webseitenkopie gewertet.

| Webseite / Ursprungs-URL | ID | Ergebnis |
|---|---|---|
| W3C | `4d194c16-4e74-4c45-994f-6d2c65b8e3f0` | `INCOMPLETE`; externe Bild-/Frame-Abhängigkeit fehlt. |
| NASA | `a241ae17-cd65-4966-9b2a-a9711ecc7dbb` | `INCOMPLETE`; externe Bild-/Frame-Abhängigkeit fehlt. |
| WHO | `914709c8-8619-43a3-877f-f9e87be8a63e` | `INCOMPLETE`; externe Bild-/Frame-Abhängigkeit fehlt. |
| NIST | `733e7c5a-d194-457c-9139-ef1a32d904c7` | `INCOMPLETE`; externe Bild-/Frame-Abhängigkeit fehlt. |
| Europäische Kommission | `94408c8b-3ef1-473e-bdc2-8e088ec305ce` | `INCOMPLETE`; externe Bild-/Frame-Abhängigkeit fehlt. |

### WebArchive-Uploads

Auch diese Eingaben waren kontrollierte WebArchive-Fixtures mit HTML-Hauptdokument, eingebettetem Stylesheet, Bild und Frame. Sie testen den Decoder, die Ressourcenmigration und den privaten Pfad; sie sind keine vollständigen WebArchive-Dateien der jeweiligen Live-Webseiten. Alle fünf Fixtures wurden als `COMPLETE` gespeichert. Im Browser waren pro Fixture das Stylesheet und Bild geladen sowie der eingebettete Frame-Inhalt vorhanden.

| Webseite / Ursprungs-URL | ID | Ergebnis |
|---|---|---|
| W3C | `9c0b104c-7ac8-40ba-90ee-cb0fcc23c87f` | `COMPLETE`; Stylesheet, Bild und Frame vorhanden. |
| NASA | `ddfb84d7-5ad6-482a-b274-cba91b423692` | `COMPLETE`; Stylesheet, Bild und Frame vorhanden. |
| WHO | `862aee4a-60de-48d6-9e3c-80fdd77c8c06` | `COMPLETE`; Stylesheet, Bild und Frame vorhanden. |
| NIST | `52c80384-dc22-4028-8d0f-63d6030f5893` | `COMPLETE`; Stylesheet, Bild und Frame vorhanden. |
| Europäische Kommission | `2c142796-178b-48a5-8a72-a43c0253e1f9` | `COMPLETE`; Stylesheet, Bild und Frame vorhanden. |

In einem zusätzlichen Lauf wurden auch fünf kontrollierte MHTML-Fixtures geprüft. Sie blieben wegen bewusst nicht eingebetteter externer Abhängigkeiten unvollständig. Die WebArchive-Fixtures decken die gebündelten Ressourcen in diesem Format ab.

## Build- und Umgebungsnachweise

- API, Worker und Migrationsprojekt wurden erfolgreich gebaut. API: 0 Warnungen, 0 Fehler. Beim Worker meldete NuGet bekannte Advisories für `System.Security.Cryptography.Pkcs` 10.0.0 (GHSA-2fmv-rv2c-59x7 und GHSA-846f-f764-76mx). Beim Migrationsprojekt konnte NuGet während des Builds den Vulnerability-Index unter `api.nuget.org` nicht erreichen (`NU1900`); der Build selbst war erfolgreich.
- Der Docker-Produktionsbuild des Frontends wurde erfolgreich erstellt. `pnpm run check` konnte lokal wegen eines Dateisystem-`EPERM` beim Zugriff auf den Package-Store nicht verlässlich abgeschlossen werden.
- `git diff --check` war erfolgreich; Git meldete lediglich die bestehende LF/CRLF-Konvertierung für `frontend/src/hooks.server.ts`.
- Der lokale PostgreSQL-Container sowie die Datenbank- und MinIO-Volumes waren bereits vorhanden. Sie wurden für die Migration der bestehenden Snapshots und für diese Tests weiterverwendet und nicht gelöscht. Damit war dies kein vollständig frischer Datenbank-/Objektspeicherlauf. Die vorgefundenen Snapshots waren zugleich der verlangte Migrationsbestand.

## Grenzen und Folgeschritt für eine vollständige Live-Formatabdeckung

Der Fehler für bereits gespeicherte Ressourcen ist mit dem W3C-Eigentümer- und Adminpfad sowie mit dem WebArchive-Ressourcen-Fixture behoben verifiziert. Die gewünschte Prüfung „jede Webseite als URL, HTML und WebArchive mit vollständigem Seiteninhalt“ ist mit den erfassten Daten noch nicht erfüllt: Es fehlen echte HTML- und WebArchive-Exporte der fünf Live-Seiten; drei URL-Aufnahmen konnten außerdem keinen vollständigen Capture liefern. Für eine vollständige Inhaltsprüfung müssen diese Quellen als tatsächlich selbstenthaltende HTML-Dateien bzw. echte WebArchive-Dateien erneut bereitgestellt oder exportiert werden. Das betrifft die Testdatenabdeckung, nicht den bereits verifizierten privaten Ressourcenpfad.
