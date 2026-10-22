# Lösungsvorschlag: Snapshot-Bearbeitung und erweiterbare Historie

- Status: implementiert und lokal verifiziert
- Arbeitsbranch: `codex/feat/snapshot-editing`
- Ausgangsbasis: lokaler `master` nach Fast-Forward auf `origin/master` (`efdbef5`)

## 1. Zielbild

Angemeldete Benutzer können die Metadaten ihrer eigenen Snapshots bearbeiten. Änderungen an Titel, Beschreibung und Tags beeinflussen den Moderationsstatus nicht; ein bereits genehmigter Snapshot bleibt öffentlich. Eine Änderung der Quell-URL durch den Eigentümer setzt einen gespeicherten Snapshot auf `RESUBMITTED` und entzieht ihm damit bis zur erneuten Freigabe die öffentliche Sichtbarkeit.

Benutzer mit `admin.access` und `admin.snapshots.access` können dieselben Felder für jeden Snapshot bearbeiten. Ihre Änderungen verändern den Moderationsstatus nicht. Sie können außerdem die Zuordnung zu einem Benutzer entfernen oder auf einen anderen vollständig registrierten Benutzer übertragen.

Die öffentliche Preview unter `/snapshots/{snapshotId}` behält ihren schwebenden Informations-Button. Der Dialog dahinter wird zu einem Snapshot-Datensatz mit den Tabs **Info** und **History**. Die Historie wird nicht länger durch einen zentralen Block aus `switch`-Anweisungen dargestellt, sondern über eine Registry aus Eventtyp, Version und einer jeweils passenden Svelte-Komponente.

Alle Änderungen werden atomar gespeichert, über den bereits vorhandenen starken ETag gegen verlorene Updates abgesichert und als unveränderliche History-Events protokolliert.

## 2. Abgrenzung und getroffene Produktentscheidungen

1. **Eine URL-Änderung startet keine neue Archivierung.** Sie korrigiert die Provenienz des bereits gespeicherten Inhalts. Ein erneuter Capture wäre ein eigener Workflow mit neuer Processing-Queue, Objekt-Lifecycle und Fehlerbehandlung und ist nicht Bestandteil dieser Erweiterung.
2. **Die Quell-URL kann deshalb nur bei einem gespeicherten Snapshot geändert werden** (`Quality != null`). Während `QUEUED`/`RUNNING` sowie bei einem fehlgeschlagenen Capture wird die Änderung mit `409 snapshot_source_not_editable` abgelehnt. Titel, Beschreibung, Tags und die administrative Zuordnung bleiben auch in diesen Zuständen editierbar.
3. **Ein Eigentümer-URL-Update setzt jeden Status außer `RESUBMITTED` auf `RESUBMITTED`.** Das gilt auch für `PENDING` und `REJECTED`; bei bereits `RESUBMITTED` bleibt der Status unverändert, das URL-Event wird trotzdem geschrieben.
4. **Administrative Änderungen behalten den aktuellen Status bei.** Ein genehmigter Snapshot bleibt auch bei einer administrativen URL-Änderung genehmigt. Ein abgelehnter oder ausstehender Snapshot wird durch das Editieren nicht automatisch genehmigt.
5. **Es entsteht keine neue Berechtigung.** `admin.snapshots.access` deckt bereits Lesen und Moderieren aller Snapshots ab und wird um Bearbeitung sowie Zuordnungsänderung erweitert. Beschreibung und Dokumentation dieser Berechtigung werden entsprechend präzisiert.
6. **Eigentümerschaft wird im HTTP-Vertrag über den Handle adressiert**, nicht über interne Benutzer-IDs. `createdByHandle: null` entfernt die Zuordnung; ein String weist einen vollständig registrierten Benutzer zu.
7. **No-op-Updates erzeugen kein Event.** Nach Normalisierung identische Werte liefern `200` und denselben ETag zurück.
8. **Öffentliche Historie ist eine bereinigte Projektion.** Moderationsnotizen werden nie öffentlich ausgegeben. Ablehnungsentscheidungen werden in der öffentlichen Historie nicht gezeigt. Eigentümer und Administratoren erhalten weiterhin die vollständige für sie erlaubte Historie.
9. **Die Oberfläche bleibt vollständig englisch.** Technische Dokumentation und dieser Vorschlag können deutsch sein.

## 3. Verhaltensmatrix

| Akteur | Änderung | Statuswirkung | History-Event | Öffentliche Wirkung |
| --- | --- | --- | --- | --- |
| Eigentümer | Titel, Beschreibung und/oder Tags | unverändert | `snapshot.details_updated` | bei `APPROVED` sofort sichtbar |
| Eigentümer | Quell-URL | `RESUBMITTED` | `snapshot.source_updated` | Snapshot ist bis zur Freigabe nicht öffentlich |
| Eigentümer | Details und Quell-URL in einem Save | wie oben, atomar | beide Events in definierter Reihenfolge | keine teilweise sichtbare Änderung |
| Administrator | Titel, Beschreibung, Tags und/oder Quell-URL | unverändert | Details- und/oder Source-Event | bei `APPROVED` sofort sichtbar |
| Administrator | Benutzerzuordnung | unverändert | `snapshot.attribution_updated` | öffentliche Attribution wird bei `APPROVED` sofort aktualisiert |
| beliebig | nur normalisierungsäquivalente Werte | unverändert | keines | keine |

Die Eventreihenfolge innerhalb eines kombinierten administrativen Updates ist `snapshot.details_updated`, `snapshot.source_updated`, `snapshot.attribution_updated`. Alle Events und die Snapshot-Zeile werden in derselben Datenbanktransaktion gespeichert.

## 4. HTTP-API

### 4.1 Eigene Snapshots bearbeiten

`PUT /api/users/snapshots/{id}`

Voraussetzungen:

- authentifizierter, vollständig registrierter Benutzer;
- der Snapshot gehört dem Benutzer;
- `Origin` entspricht dem konfigurierten Frontend;
- genau ein starker `If-Match`-Wert aus `GET /api/users/snapshots/{id}`.

Der Request ist eine vollständige Repräsentation der editierbaren Felder:

```json
{
  "sourceUrl": "https://example.org/article",
  "title": "Corrected title",
  "description": "Optional description",
  "tags": ["history", "standards"]
}
```

Antworten:

- `200` mit `SnapshotSubmissionDetailResponse` und neuem bzw. unverändertem ETag;
- `400` für Feldvalidierung oder ungültigen `If-Match`;
- `401`/`403` für Sitzung bzw. Origin;
- `404` für fremde oder nicht vorhandene Snapshots;
- `409` für eine nicht editierbare Source-URL;
- `412` bei veraltetem ETag;
- `428` ohne `If-Match`.

### 4.2 Snapshots administrativ bearbeiten

`PUT /api/admin/snapshots/{id}`

Der Vertrag erweitert dieselben Pflichtfelder um die vollständige gewünschte Attribution:

```json
{
  "sourceUrl": "https://example.org/article",
  "title": "Corrected title",
  "description": null,
  "tags": ["history"],
  "createdByHandle": "archivist"
}
```

`createdByHandle: null` entfernt die Zuordnung. Ein nicht vorhandener oder unvollständig registrierter Handle erzeugt einen `400`-Validierungsfehler am Feld `createdByHandle`. Die übrigen Preconditions und Antworten entsprechen dem Eigentümer-Endpunkt.

### 4.3 Kandidaten für die Attribution suchen

`GET /api/admin/snapshots/owners?q={query}&page=1&pageSize=10`

Dieser schmale, über `admin.snapshots.access` geschützte Endpunkt liefert nur Handle, Anzeigename und öffentliche Profilbild-URL vollständig registrierter Benutzer. Er ist bewusst unabhängig von `/api/admin/users`, da ein Snapshot-Moderator nicht zwingend `admin.users.access` besitzt. Die Suche ist auf 10 Ergebnisse pro Interaktion begrenzt und durchsucht Handle sowie Anzeigename.

### 4.4 Öffentliche Snapshot-Historie

`GET /api/snapshots/{id}/history?page=1&pageSize=25`

Der Endpunkt existiert nur für gespeicherte, aktuell `APPROVED` Snapshots. Er nutzt den bestehenden `SnapshotHistoryResponse`-Envelope und liefert nur öffentlich geeignete Events:

- `snapshot.submitted`;
- `snapshot.stored`;
- `snapshot.resubmitted`;
- `snapshot.moderated` ausschließlich für `APPROVED`, ohne `reason`;
- `snapshot.details_updated`;
- `snapshot.source_updated`;
- `snapshot.attribution_updated`;
- bekannte Processing-Events, sofern sie zu einem später genehmigten Snapshot gehören.

Unbekannte Typen und Versionen werden öffentlich standardmäßig nicht freigegeben. Dadurch wird das bestehende Fail-Closed-Prinzip des Eigentümer-Endpunkts auch auf die öffentliche Darstellung übertragen.

Die öffentliche Projektion setzt `actor` grundsätzlich auf `null`. Damit veröffentlicht die Preview weder Moderatorenidentitäten noch nach einem Owner-Transfer die Identität eines früheren Eigentümers. Actor-Attribution bleibt den geschützten Owner- und Admin-Historien vorbehalten.

### 4.5 OpenAPI und generierte Typen

Neu oder geändert werden die Requests, Owner-Suche, Endpunkte, Fehlerantworten und History-Dokumentation. Nach Start der lokalen API wird deshalb zwingend aus `frontend/` ausgeführt:

```powershell
bun --bun run generate:api
```

`frontend/openapi-ts.config.ts` und der Zugriff von `@hey-api/openapi-ts` auf `http://localhost:5200/openapi/v1.json` bleiben unverändert. Die generierten Änderungen unter `frontend/src/lib/api/` werden zusammen mit den Backend-Verträgen geprüft und committed.

## 5. Domänenmodell, Validierung und Transaktion

### 5.1 Gemeinsames Update-Modell

In `Archive.Core` entsteht ein normalisiertes `SnapshotUpdate`-Modell für URL, Titel, Beschreibung und Tags sowie ein administratives Modell für die optionale Attribution. Es verwendet dieselben Grenzen wie die Erstellung:

- URL: HTTP/HTTPS, normalisiert durch `UrlNormalizer`, maximal 4.096 Zeichen;
- Titel: getrimmt, erforderlich, maximal 512 Zeichen, keine Steuerzeichen;
- Beschreibung: getrimmt, leere Werte werden `null`, maximal 2.048 Zeichen;
- Tags: führende `#` und Rand-Whitespace entfernen, lowercase, eindeutig, maximal 30 Werte mit je 128 Zeichen und ohne Steuerzeichen;
- Handle: über `UserProfileValidation.NormalizeHandle`, danach exakte Suche eines vollständig registrierten Benutzers.

Die Request-Validierung wird aus der bestehenden Creation-spezifischen Hilfsklasse in eine gemeinsam nutzbare Snapshot-Feldvalidierung extrahiert. Die Erstellungsendpunkte behalten exakt ihr bisheriges Verhalten.

### 5.2 Repository-Ablauf

`ISnapshotSubmissionRepository` erhält getrennte Methoden für Owner- und Admin-Updates. Beide führen folgende Schritte in einer Transaktion aus:

1. Actor-Zeile mit Shared Lock laden; beim Admin die effektiven Berechtigungen innerhalb der Transaktion erneut prüfen.
2. Snapshot mit `FOR UPDATE` sperren.
3. Eigentümerschaft beziehungsweise globale Admin-Berechtigung prüfen.
4. aktuelle History-Sequenz lesen und den scope-spezifischen ETag vergleichen;
5. bei administrativer Zuweisung den Zielbenutzer validieren;
6. normalisierte Diffs bestimmen;
7. Source-URL-Regel gegen den Capture-Zustand prüfen;
8. Snapshot-Felder und Tag-Beziehungen aktualisieren;
9. History-Events mit Actor-ID anhängen;
10. genau einmal `SaveChangesAsync`, danach Commit.

Die bestehenden Tag-Zeilen werden wie bei der Erstellung konfliktfrei per `INSERT ... ON CONFLICT DO NOTHING` wiederverwendet. Es werden nur die `snapshot_tags`-Beziehungen ersetzt; globale, eventuell von anderen Snapshots genutzte Tags werden nicht gelöscht.

Der vorhandene ETag ist weiterhin geeignet: Er enthält die History-Sequenz. Jede tatsächliche Änderung erzeugt mindestens ein Event und damit einen neuen ETag. Zusätzliche Metadaten müssen nicht in den Hash aufgenommen werden.

### 5.3 Ergebnis- und Fehlermodell

Ein eigenes `SnapshotUpdateResult` bildet mindestens `Updated`, `Unchanged`, `Invalid`, `NotFound`, `Forbidden`, `Stale` und `SourceNotEditable` ab. Das vermeidet die semantisch falsche Wiederverwendung von `SnapshotReviewOutcome`. Controller übersetzen die Ergebnisse konsistent in Problem Details mit stabilen Fehlercodes.

### 5.4 Keine Datenbankmigration erwartet

Alle editierbaren Spalten, die nullable Owner-FK mit `ON DELETE SET NULL`, Tag-Beziehungen und die versionierte JSONB-Historie existieren bereits. Für den vorgesehenen Umfang ist daher keine EF-Core-Migration nötig. Sollte die Implementierung wider Erwarten das Schema ändern, wird die Migration separat begründet und zusammen mit `DataContextModelSnapshot.cs` geprüft.

## 6. History-Events

### 6.1 Neue Events

#### `snapshot.details_updated`, Version 1

```json
{
  "fields": ["TITLE", "DESCRIPTION", "TAGS"],
  "addedTags": ["standards"],
  "removedTags": ["draft"]
}
```

`fields` enthält nur tatsächlich geänderte Felder in stabiler Reihenfolge. Für Titel und Beschreibung werden bewusst keine alten Textinhalte dauerhaft dupliziert; die Historie dokumentiert den Änderungsakt, ohne entfernte sensible oder fehlerhafte Texte dauerhaft weiterzuveröffentlichen. Tag-Deltas sind kurz, strukturiert und für die Oberfläche unmittelbar hilfreich.

#### `snapshot.source_updated`, Version 1

```json
{
  "previousSourceUrl": "https://example.org/old",
  "sourceUrl": "https://example.org/new",
  "previousStatus": "APPROVED",
  "status": "RESUBMITTED",
  "reviewRequired": true
}
```

Owner- und Admin-Historien erhalten beide URLs. Die öffentliche Projektion entfernt `previousSourceUrl` und zeigt nur, dass die Quelle geändert wurde, den aktuellen Link und gegebenenfalls die erneute Prüfung. So bleibt ein versehentlich eingetragener alter Link nicht öffentlich erhalten.

#### `snapshot.attribution_updated`, Version 1

Die persistierte Eventnutzlast enthält `previousUserId` und `userId` als nullable interne UUIDs. Damit bleibt die historische Referenz stabil, wenn ein Benutzer seinen Handle ändert. `null` steht für „Anonymous / no account attached“.

Diese interne Form ist niemals Teil des HTTP-Vertrags: Beim Lesen löst das Repository die UUIDs gegen das aktuelle öffentliche Benutzerprofil auf. Die API ersetzt die interne Nutzlast vollständig durch `previousUser` und `user` vom Typ `PublicUserResponse`, der Handle, Anzeigename, Rolle und öffentliche Profilbild-URL, aber keine interne ID enthält. Die öffentliche Projektion zeigt nur eine weiterhin aktuelle Attribution und unterscheidet über `removed` und `redacted` zwischen einer ausdrücklich entfernten und einer aus Datenschutzgründen nicht aufgelösten historischen Zuordnung; Admin- und Owner-Historie dürfen vorheriges und neues öffentliches Profil zeigen. Auch unbekannte Eventversionen dürfen die interne Roh-Nutzlast nicht passieren.

### 6.2 Bestehende Events bereinigen

Bei der Bestandsaufnahme ist eine Inkonsistenz aufgefallen: Das Frontend erwartet für `snapshot.processing_finished` ein Feld `status`, der aktuelle Backend-Payload ist jedoch leer. Neue Processing-Abschlüsse werden daher als Version 2 mit dem tatsächlichen terminalen `ProcessingStatus` geschrieben. Version 1 bleibt lesbar und erhält eine generische Darstellung.

Die aktuelle Frontend-Logik prüft bei `snapshot.moderated` außerdem ein nicht vorhandenes Feld `source`. Diese Sonderfälle werden entfernt; Version 1 wird ausschließlich anhand der tatsächlich vorhandenen Felder `previousStatus`, `status` und `reason` dargestellt. Bestehende Daten werden nicht umgeschrieben.

### 6.3 Stabile Typbezeichner

Eventnamen werden in `SnapshotHistoryTypes` zentralisiert und sowohl von Payload-Klassen als auch von Repository-Filtern genutzt. Neue Payload-Klassen bleiben kleine, versionierte Records mit professioneller XML-Dokumentation. Das bestehende Größenlimit von 32 KiB bleibt unverändert.

## 7. Frontend-Architektur

### 7.1 Event-Registry statt zentraler Fallunterscheidung

Die bisherige Kombination aus `frontend/src/lib/submission-history.ts` und mehreren `switch`-Blöcken in `SnapshotHistory.svelte` wird ersetzt durch:

```text
frontend/src/lib/components/
├── submissions/SnapshotHistory.svelte
└── history/
├── SnapshotHistoryEvent.svelte
├── history-events.ts
└── events/
    ├── HistorySummary.svelte
    ├── HistoryCapture.svelte
    ├── HistoryModerated.svelte
    ├── HistoryDetailsUpdated.svelte
    ├── HistorySourceUpdated.svelte
    └── HistoryAttributionUpdated.svelte
```

`history-events.ts` ordnet `type + version` einer Komponente, Kategorie und Tonalität zu und enthält enge Runtime-Typeguards für das weiterhin offene `data`-Objekt. Die Timeline-Komponente verantwortet nur Reihenfolge, Zeit, Actor, Rail und Pagination; Event-Komponenten verantworten Überschrift und Zusatzinformationen. Ein unbekannter Typ oder eine unbekannte Version wird neutral als „Snapshot event“ angezeigt und niemals als Raw JSON ausgegeben.

Diese Struktur macht einen neuen Eventtyp zu einer lokalen Ergänzung aus Payload-Guard, Komponente und einem Registry-Eintrag, ohne die zentrale Timeline umzubauen.

### 7.2 Preview-Dialog mit Tabs

`SnapshotInformationDialog.svelte` wird zu einem allgemeineren `SnapshotRecordDialog.svelte`:

- der bestehende Button mit Info-Icon bleibt der Auslöser;
- Dialogtitel: **Snapshot record**;
- horizontale Tabs aus der vorhandenen UI-Bibliothek;
- Tab **Info** mit `Info`-Icon und dem vorhandenen `SnapshotMetadata`-Inhalt;
- Tab **History** mit `History`-Icon und einer kompakten Variante der gemeinsamen Timeline;
- aktive Auswahl durch Register Blue, 3-px-Unterkante, Icon und `aria-selected`, nicht nur durch Farbe;
- die sichtbare History wird gemeinsam mit dem Snapshot serverseitig über exakt denselben Scope geladen;
- ältere Seiten werden über **Load older events** angehängt, statt die Preview-URL mit Pagination-Parametern neu zu laden;
- Loading-, Empty- und Error-State sind fokussierbar, knapp und ohne dekorative Animation;
- der vollständige Link bleibt umbrechbar und kopierbar.

`+page.server.ts` merkt sich beim Auflösen des Snapshots zusätzlich den wirksamen Scope (`public`, `users` oder `admin`) und lädt die passende History serverseitig. Private Requests leiten das Session-Cookie ausschließlich an den konfigurierten API-Origin weiter; ein Scope-Fallback im Browser findet nicht statt.

### 7.3 Bearbeitungsformular

`SnapshotEditForm.svelte` wird auf der bestehenden Owner- und Admin-Detailseite wiederverwendet. Es nutzt vorhandene Input-Komponenten, `TagEditor`, `snapshot-validation.ts` und ein sekundäres **Edit snapshot**-Control mit Pencil-Icon.

- Editiermodus ist explizit und bietet **Cancel** und **Save changes**.
- Bei einer Owner-URL-Änderung zeigt ein Warning-Ochre-Hinweis mit Icon und Text die Moderationswirkung. Der primäre Button heißt dynamisch **Save and request review**.
- Der Hinweis sagt ausdrücklich, dass der archivierte Inhalt nicht erneut erfasst wird.
- Beim Admin bleibt der Button **Save changes**; ein kurzer Hinweis erklärt, dass der Review-Status unverändert bleibt.
- `409`, `412`, Auth-Fehler und Validierungsfehler werden differenziert behandelt.
- Nach Erfolg werden Detail, ETag und History lokal aktualisiert oder über `invalidate("app:submissions")` neu geladen; Fokus wandert zur Statusmeldung.
- Die Überschrift und URL im Seitentitel werden nach einem erfolgreichen Save aktualisiert.

Für Administratoren ergänzt das gemeinsame Formular eine zugängliche Kandidatenliste mit sichtbarer aktueller Auswahl und **Remove attribution**. Es werden keine internen IDs angezeigt oder versendet.

## 8. Gestaltung und Barrierefreiheit

Die Umsetzung folgt dem Skill `designing-editorial-interfaces` und dem vorhandenen Designsystem:

- keine neuen Farbwerte; semantische Tokens aus `app.css` für beide Themes;
- Newsreader nur für editoriale Titel, Source Sans 3 für Formulare und Navigation, IBM Plex Mono für URLs, Zeiten und technische Werte;
- History als Registerlinie mit Zeitpunkten und horizontalen Regeln statt als Kartenraster;
- ruhige, rechteckige Flächen, maximal vorhandene Radien und nur der bestehende Overlay-Schatten;
- Register Blue für Interaktion und aktiven Tab, Time Copper nur für Review-relevante Abweichungen;
- Status immer durch Text, Icon/Form und Farbe;
- mindestens 44 × 44 px große Ziele, 3-px-Fokusoutline mit Offset;
- funktionsfähiger Reflow ab 320 CSS-Pixel und bei 200 % Textzoom;
- vollständige, kopierbare URLs mit `overflow-wrap: anywhere`;
- `prefers-reduced-motion` bleibt wirksam;
- die archivierte Webseite im `iframe` wird auch im Dark Mode niemals eingefärbt.

Visuelle Prüfung erfolgt streng in Light und Dark Mode, jeweils mindestens bei Desktop- und Mobile-Größe. Zusätzlich werden Tastaturbedienung, Fokusreihenfolge, Dialog-Fokusfalle, Tab-Pfeiltasten, lange URLs, lange Titel, 30 Tags, leere History und paginierte History geprüft.

## 9. Voraussichtlich betroffene Dateien

### Backend

- `backend/Archive.Core/Models/Snapshots/` – Update-Modelle, Result-Typ und gemeinsame Validierung;
- `backend/Archive.Core/Models/Snapshots/History/` – drei neue Events und Processing-v2;
- `backend/Archive.Core/Repositories/SnapshotSubmissionRepository/` – transaktionale Owner-/Admin-Updates, Owner-Suche und private History;
- `backend/Archive.Core/Repositories/SnapshotRepository/` – fail-closed öffentliche History;
- `backend/Archive.Api/Contracts/Snapshots/Submission/` – Update-Requests und Owner-Suchantwort;
- `backend/Archive.Api/Controllers/User/SnapshotsController.cs` – Owner-`PUT`;
- `backend/Archive.Api/Controllers/Admin/SnapshotsController.cs` – Admin-`PUT` und Owner-Suche;
- `backend/Archive.Api/Controllers/SnapshotsController.cs` – öffentliche History;
- `backend/Archive.Api/OpenApi/SnapshotSubmissionOperationTransformer.cs` – ETag-Dokumentation auch für beide `PUT`-Operationen;
- `backend/Archive.Core/Authorization/PermissionCatalog.cs` und Architekturdokumentation – präzisierte Bedeutung von `admin.snapshots.access`.

### Frontend

- `frontend/src/lib/api/` – ausschließlich generiert;
- `frontend/src/routes/snapshots/[snapshotId]/+page.server.ts` und `+page.svelte` – expliziter History-Scope;
- `frontend/src/lib/components/archive/SnapshotInformationDialog.svelte` – Ablösung durch Tab-Dialog;
- `frontend/src/lib/components/history/` – Registry, Timeline und Event-Komponenten;
- `frontend/src/lib/components/submissions/SubmissionDetail.svelte` – Edit-Workflow;
- `frontend/src/lib/components/submissions/SnapshotEditForm.svelte` – gemeinsames Formular;
- `frontend/src/lib/components/tags/TagEditor.svelte` – wiederverwendbarer Disabled-Zustand während Speichervorgängen.

### Dokumentation

- `docs/architecture.md` – Update-Endpunkte, Berechtigungswirkung, Moderationsübergang und öffentliche History;
- `docs/development.md` – manuelle Testmatrix, sofern die Kommandos oder Abläufe ergänzt werden müssen.

## 10. Implementierungsreihenfolge

1. History-Typkonstanten, neue Payloads und Processing-v2 einführen.
2. gemeinsame Update-Validierung und Core-Result-Typen ergänzen.
3. transaktionale Repository-Updates einschließlich Tags, Owner-Auflösung, ETag und Events implementieren.
4. öffentliche, private und administrative History-Projektionen absichern.
5. API-Verträge und Controller-Endpunkte ergänzen, OpenAPI-Dokumentation anpassen.
6. API lokal starten und mit `bun --bun run generate:api` die Frontend-Typen neu erzeugen.
7. Frontend-Event-Registry und komponentenspezifische Renderer erstellen.
8. Preview-Dialog auf **Info**/**History** umstellen.
9. gemeinsames Edit-Formular und administrative Owner-Suche integrieren.
10. Dokumentation, Storybook und manuelle Testfälle vervollständigen.
11. Build-, Lint-, Browser- und End-to-End-Prüfung durchführen.

## 11. Lokale Testumgebung nach Freigabe

### 11.1 PostgreSQL und MinIO in WSL

Vor dem Start werden in WSL vorhandene Container und Volumes anhand exakter Namen und Compose-Labels geprüft. Ausschließlich eindeutig diesem Projekt zugeordnete Ressourcen wie `archive-dev-postgres`, `archive-dev-minio` und `archive-dev_*` dürfen entfernt werden; ein globales Docker-Prune ist ausgeschlossen.

Danach werden PostgreSQL 18 und MinIO in WSL mit expliziten lokalen Namen, eigenen Volumes und an Windows gebundenen Ports gestartet. Vorgesehene Erreichbarkeit:

| Service | Windows-Endpunkt |
| --- | --- |
| PostgreSQL | `localhost:5432` |
| MinIO API | `http://localhost:9000` |
| MinIO Console | `http://localhost:9001` |

Der Bucket wird vor Start der Anwendungen erzeugt. API, Worker und Frontend laufen wie vorgegeben direkt unter Windows. Lokale Datenbank-, Storage- und OAuth-Werte werden nur in ignorierten Development-Dateien oder Prozessvariablen gesetzt und weder in Git noch in Logs oder diese Dokumentation übernommen.

### 11.2 Anmeldung und Rollen

Sobald die geschützten Owner-/Admin-Flows getestet werden können, ist eine Anmeldung mit dem Google-Testkonto erforderlich. An diesem Punkt wird der Benutzer ausdrücklich zur Anmeldung aufgefordert. Das Testkonto wird nur in der lokalen, austauschbaren Datenbank zum Admin gemacht; für Owner-Transfer-Tests wird bei Bedarf ein zweites vollständig registriertes lokales Konto benötigt.

## 12. Archivierungs-Testdaten

Nach Freigabe werden mindestens diese drei seriösen, öffentlich erreichbaren Quellen mit steigender Komplexität verwendet:

| Komplexität | Quelle | Begründung |
| --- | --- | --- |
| einfach | [IANA Example Domains](https://www.iana.org/help/example-domains) | kleine, überwiegend statische Referenzseite einer Internet-Infrastrukturorganisation |
| mittel | [W3C WCAG 2.2](https://www.w3.org/TR/WCAG22/) | umfangreiches Standarddokument mit Navigation, Ankern, Tabellen und zahlreichen internen Links |
| komplex | [Python Tutorial](https://docs.python.org/3/tutorial/index.html) | umfangreiche offizielle Dokumentation mit Navigation, Querverweisen, Syntaxdarstellung und zahlreichen Ressourcen |

Für jede Quelle wird dieselbe Seite in drei Eingangsformen archiviert, also insgesamt mindestens neun Snapshots:

1. direkte URL-Erfassung durch den Worker;
2. MHTML-Datei aus Chromium/CDP;
3. WebArchive-Datei als Apple-WebArchive-Property-List mit Hauptressource und zulässigen Subresources.

Vor dem Capture werden Erreichbarkeit, Robots-Regeln und Nutzbarkeit ohne Anmeldung geprüft. Falls eine Quelle den automatisierten Abruf ausdrücklich ausschließt oder technisch blockiert, wird sie durch eine vergleichbar seriöse offizielle Quelle ersetzt und der Grund im Testprotokoll dokumentiert. Die erzeugten MHTML-/WebArchive-Dateien sind lokale Testartefakte und werden nur committed, wenn sie klein, lizenzrechtlich vertretbar und als dauerhafte Fixture wirklich notwendig sind; standardmäßig bleiben sie unversioniert.

### 12.1 Ergebnis der lokalen Archivierungsprüfung

Am 23. Oktober 2026 wurden alle drei Quellen als URL, MHTML und WebArchive eingereicht. Alle neun Verarbeitungen erreichten `COMPLETED`. Die generierten Dateiimporte bleiben als ignorierte lokale Testartefakte außerhalb des Git-Index. Zusätzlich wurden Owner- und Admin-Bearbeitung, erneute Freigabe, Attributionswechsel einschließlich Entfernung, ein absichtlich erzeugter ETag-Konflikt sowie die öffentliche History-Projektion geprüft. Die öffentliche Antwort enthielt weder interne Benutzer-IDs noch frühere Quell-URLs; nicht mehr aktuelle Attributionsziele wurden explizit als redigiert projiziert.

## 13. Verifikation und Abnahmekriterien

### 13.1 Automatisierte statische Prüfungen

```powershell
dotnet build backend/Archive.slnx

Set-Location frontend
bun run check
bun run lint
bun run build
bun run build:storybook
```

Da das Repository derzeit keine automatisierten Testprojekte besitzt, wird die Repository-weite Build-Prüfung durch gezielte manuelle API- und Browser-Szenarien ergänzt. Falls im Zuge der Implementierung eine Testbasis eingeführt wird, soll sie auf Repository- und Serializer-Invarianten fokussieren und nicht als umfangreiche Nebenmigration ausarten.

### 13.2 API- und Domänenszenarien

- Owner ändert nur Titel/Beschreibung/Tags eines `APPROVED` Snapshots: Status und öffentliche Sichtbarkeit bleiben erhalten.
- Owner ändert URL eines `APPROVED` Snapshots: Status wird `RESUBMITTED`, öffentliche Metadaten, Screenshot, Content und History liefern bis zur Freigabe `404`.
- Owner ändert URL in `PENDING`, `REJECTED` und `RESUBMITTED`: erwarteter Status und Eventinhalt stimmen.
- Source-Änderung ohne gespeicherten Capture liefert `409`, andere Metadaten bleiben editierbar.
- fremder Benutzer erhält `404`; anonyme Requests erhalten `401`.
- veralteter ETag liefert `412` und speichert weder Teildaten noch Event.
- normalisierungsäquivalentes Update erzeugt weder Event noch neuen ETag.
- Admin-Änderungen an URL und Details eines `APPROVED` Snapshots behalten `APPROVED`.
- Admin entfernt die Attribution: bisheriger Eigentümer verliert den privaten Zugriff.
- Admin weist einen neuen Eigentümer zu: neuer Eigentümer erhält Zugriff auf Detail, Content und zulässige History.
- unbekannter Owner-Handle liefert einen feldbezogenen `400`-Fehler.
- kombinierte Änderungen sind vollständig atomar und die Eventreihenfolge ist stabil.
- öffentliche History enthält keine Rejection-Events, Moderationsnotizen, vorherigen URLs oder vorherigen Owner.
- unbekannte Events und Versionen werden privat neutral dargestellt und öffentlich nicht ausgegeben.

### 13.3 Browser- und UX-Szenarien

- Preview-Dialog und Tabs per Maus, Touch und Tastatur bedienen;
- Fokus kehrt beim Schließen zum Info-Button zurück;
- Pfeiltasten, Home/End und `aria-selected` der Tabs funktionieren;
- History wird scope-sicher serverseitig geladen und zeigt bei mehr als 100 Events transparent an, dass nur die neuesten Einträge sichtbar sind;
- Owner-Warnung ändert sich nur bei tatsächlicher URL-Differenz;
- Admin-Owner-Suche ist mit Tastatur und Screenreader-Beschriftungen bedienbar;
- lange URL, maximaler Titel, lange Beschreibung und 30 Tags brechen ohne horizontales Scrollen um;
- Light/Dark, 320 px Mobile, typisches Smartphone, Tablet und Desktop prüfen;
- 200 % Textzoom, sichtbare Fokusrahmen, Kontrast und Reduced Motion prüfen;
- archivierter Inhalt bleibt visuell und sicherheitstechnisch vom Archive-Chrome getrennt.

### 13.4 Formatmatrix

Alle neun Test-Snapshots werden nach Capture auf Screenshot, Startdokument, lokale Styles/Bilder/Fonts, Navigation innerhalb des Snapshot-Scopes, Ressourcenanzahl und Content-Security-Policy geprüft. Anschließend werden die Bearbeitungs- und History-Flows mindestens einmal je Eingangsformat wiederholt.

## 14. Commit-Plan für die spätere Implementierung

Die Implementierung soll in überprüfbaren semantischen Commits erfolgen, beispielsweise:

1. `feat: add snapshot update contracts and history events`
2. `feat: add snapshot editing workflows`
3. `refactor: render snapshot history by event type`
4. `docs: document snapshot editing and verification`

Generierte API-Typen gehören in denselben Commit wie der öffentliche Vertrag, der sie verändert. Temporäre Konfiguration, OAuth-Daten, MHTML-/WebArchive-Testdownloads, Datenbank-Volumes und MinIO-Objekte werden nicht committed.

## 15. Freigabegrenze

Dieser Branch enthält zunächst ausschließlich diesen Lösungsvorschlag. Keine API-, Datenbank-, Frontend-, Container- oder Konfigurationsänderung wird vor einer ausdrücklichen Implementierungsfreigabe vorgenommen.
