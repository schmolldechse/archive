# Rollen, individuelle Berechtigungen und Snapshot-Moderation

**Status:** Überarbeiteter Lösungsvorschlag zur Freigabe; keine Implementierung.

**Stand:** 2026-10-17.

**Basis:** `master` / `origin/master`, Commit `0abec2f6f77b07cfc88b2f5b8f2e5edfde1c13bc`.

**Planungsbranch:** `docs/snapshot-moderation-permissions`.

## 1. Auftrag und verbindliche Entscheidungen

Der Umfang umfasst zwei Rollen (`User`, `Admin`), persönliche Rechte mit erlaubenden und einschränkenden Ausnahmen, Snapshot-Genehmigungen, Publikation ohne vorherige manuelle Genehmigung sowie eine abstrakte, typisierte Snapshot-Historie. Hinzu kommen genau die dafür benötigten Seiten und Zugriffsregeln.

Verbindliche Entscheidungen aus dem Interview und der anschließenden Überarbeitung:

1. Die Benutzerseite zeigt **nur eigene Übermittlungen**, niemals fremde oder anonyme Übermittlungen.
2. Nur der **angemeldete ursprüngliche Ersteller** darf einen abgelehnten Snapshot erneut einreichen. Ein Review-Recht ersetzt keine Eigentümerschaft.
3. Gäste können weiterhin übermitteln. Für Gäste ist eine neue Übermittlung möglich, aber keine Wiedervorlage desselben Snapshots.
4. Bereits öffentliche Snapshots werden als **genehmigter Bestand** übernommen.
5. Vor einer Genehmigung nach Ablehnung muss eine Wiedervorlage als eigener Status gespeichert werden. Frühere Entscheidungen bleiben sichtbar.
6. Es kommen **keine Versionsspalten, Änderungszähler oder entsprechenden API-Vergleichstoken** hinzu.
7. Am Snapshot kommt ausschließlich der fachliche Moderationsstatus hinzu. Einreichungsdaten, Publikationsabsicht und Verarbeitungsausgang werden aus bestehenden Daten bzw. der Historie gelesen.
8. Die Historie erhält eine JSONB-Spalte mit je Eintragsart typisiertem, abstraktem Inhalt; keine zusätzliche Tabellenvererbung.
9. Die Benutzerseite liegt unter **`/account/submissions`**.
10. Die Implementierung beginnt erst nach ausdrücklicher Freigabe. Dieser Branch enthält ausschließlich Planung und isolierte Mockups.

Nicht vorgesehen sind weitere Rollen, ein Rollenbaukasten, Projekte, Benachrichtigungen, neue Login-Anbieter, Änderung archivierter Inhalte, Massenmoderation, eine allgemeine Audit-Plattform oder das Zurückziehen bereits genehmigter Snapshots.

## 2. Befund im vorhandenen Projekt

| Bereich             | Ausgangslage                                                                                     | Geplante Konsequenz                                                                    |
| ------------------- | ------------------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------- |
| Backend             | ASP.NET Core API, `Archive.Core`, separater Worker, EF Core/PostgreSQL                           | Bestehende Controller, Core-Modelle, Mapper und Repositories fortführen                |
| Benutzer            | Lokale `ArchiveUser`, externe Identitäten und Cookie-Session                                     | Navigationen für Rollen und individuelle Ausnahmen ergänzen                            |
| Autorisierung       | `AddAuthorization()` ohne fachlichen Rechtekatalog                                               | Kleiner zentraler Resolver und benannte Policies                                       |
| Eigentümer          | Nullable `Snapshot.CreatedByUserId`; nur vollständig registrierte Konten werden zugeordnet       | Bestehende Zuordnung serverseitig verwenden; keine spätere Übernahme anonymer Einträge |
| Öffentliche Daten   | Suche, Inhalt und Screenshot verwenden im Wesentlichen `Quality != null`                         | Technisches Ergebnis und Freigabe trennen                                              |
| Öffentliche Details | `GetByIdAsync` liefert auch unfertige Metadaten                                                  | Nur genehmigte und fertige Snapshots öffentlich liefern                                |
| Verarbeitung        | `processing.snapshot_processing` wird bereinigt; erfolglose Snapshots werden bislang mitgelöscht | Snapshot und Historie behalten; terminalen Ausgang in der Historie sichern             |
| Frontend            | Svelte 5/SvelteKit, `(site)`-Layout, Auth-Kontext und wiederverwendbare UI                       | Vorhandene Muster und Designsprache verwenden                                          |
| Archiv-Viewer       | Relativer iframe-Pfad über einen SvelteKit-Content-Proxy                                         | Öffentlichen Proxy beibehalten; Begründung in Abschnitt 6.3                            |
| Vorschau            | URL-Capture und Dateiimport erzeugen bereits Screenshots                                         | Moderationsvorschau mit Screenshot und Metadaten, ohne zusätzlichen Content-Proxy      |
| API-Typen           | Generator liest `http://localhost:5200/openapi/v1.json`                                          | Spätere Vertragsänderungen aus dem laufenden Backend generieren                        |

Ausgangsdateien:

- [Snapshot](../../../backend/Archive.Core/Entities/Snapshot.cs)
- [Snapshot-Repository](../../../backend/Archive.Core/Repositories/SnapshotRepository/SnapshotRepository.cs)
- [Öffentliche Suche](../../../backend/Archive.Core/Repositories/SnapshotRepository/SnapshotRepository.Search.cs)
- [Verarbeitung und Cleanup](../../../backend/Archive.Core/Repositories/SnapshotProcessingRepository/)
- [Authentifizierung](../../../backend/Archive.Api/Authentication/ArchiveAuthenticationExtension.cs)
- [Übermittlungsformular](<../../../frontend/src/routes/(site)/archive/new/+page.svelte>)
- [Archiv-Viewer](../../../frontend/src/routes/snapshots/[snapshotId]/+page.svelte)
- [Öffentlicher Content-Proxy](../../../frontend/src/routes/api/snapshots/[snapshotId]/content/[...path]/+server.ts)
- [API-Content-Auslieferung](../../../backend/Archive.Api/Controllers/SnapshotsController.cs)
- [Produktionskonfiguration](../../../deploy/production/.env.example)
- [Designsystem](../../../.agents/skills/designing-editorial-interfaces/references/design-system.md)

Die vorgefundenen unversionierten Dateien `deploy/production/local.compose.yml` und `deploy/production/start-local.sh` werden nicht geändert oder aufgenommen. Die Suche nach weiteren Markdown-Plänen im Repository ergab keine zusätzlichen Planungsdateien mit den verworfenen Versionsfeldern; das zugehörige Prüfprotokoll und alle Mockups werden ebenfalls angepasst.

## 3. Rollen und individuelle Berechtigungen

### 3.1 Rollenmodell

Der Katalog besteht aus `User` als verpflichtender Grundrolle und `Admin` als zusätzlich zuweisbarer/entziehbarer Rolle. Bestehende und neue lokale Konten erhalten `User`; niemand wird automatisch durch die erste Anmeldung Admin. Nach Entzug von `Admin` bleibt ein normales Benutzerkonto bestehen.

Rollen sind fest definiert. Eine Tabelle mit frei editierbaren Rollendefinitionen oder Rollenrechten wird nicht benötigt.

### 3.2 Berechtigungskatalog

| Schlüssel                            | Englische Bezeichnung         | User | Admin |
| ------------------------------------ | ----------------------------- | ---- | ----- |
| `snapshots.review`                   | Review snapshots              | Nein | Ja    |
| `snapshots.publish_without_approval` | Publish without approval      | Nein | Ja    |
| `users.assign_roles`                 | Manage user roles             | Nein | Ja    |
| `users.assign_permissions`           | Manage individual permissions | Nein | Ja    |

`Review snapshots` umfasst Prüfwarteschlange, Metadaten, Historie, geschützte Screenshots und Genehmigen/Ablehnen. `Publish without approval` gilt für die eigene ursprüngliche Übermittlung mit bewusst gewählter Direktpublikation. Eine Ablehnung kann damit nicht umgangen werden.

Eigene Liste und Wiedervorlage sind durch Anmeldung, abgeschlossene Registrierung und Eigentümerschaft geschützt. Dafür wird kein weiterer administrierbarer Berechtigungsschlüssel eingeführt. Öffentliches Lesen genehmigter Snapshots und anonymes Übermitteln bleiben möglich.

Benutzerverwaltung erfordert neben dem jeweiligen Verwaltungsrecht die Rolle `Admin`. Ein `User` mit persönlichem Review-Allow kann dagegen die Moderationsseite nutzen. Ein Verwaltungs-Allow alleine verleiht einem User keine Admin-Rolle; die UI nennt diese zusätzliche Voraussetzung.

### 3.3 Persönliche Ausnahmen

Pro `(UserId, Permission)` existiert höchstens ein Eintrag mit `Allow` oder `Deny`. Kein Eintrag bedeutet `Inherit`.

Die Auswertung erfolgt in dieser Reihenfolge:

1. Ungültige oder fehlende Identität: kein geschütztes Recht.
2. Persönliches `Deny`: verweigern, auch für Admins.
3. Persönliches `Allow`: erlauben.
4. Rollenrecht vorhanden: erlauben.
5. Andernfalls: verweigern.

Zusätzliche Bedingungen wie Eigentümerschaft, vollständige Registrierung und erforderliche Admin-Rolle gelten weiterhin. Es gibt keinen Admin-Sonderweg, der Deny übergeht.

| Beispiel                       | Ergebnis                                                                        |
| ------------------------------ | ------------------------------------------------------------------------------- |
| User mit Review-Allow          | Darf prüfen                                                                     |
| Admin mit Review-Deny          | Darf nicht prüfen                                                               |
| Admin ohne Ausnahme            | Rollenrecht gilt                                                                |
| Persönliche Ausnahme entfernen | Rollenregel gilt wieder                                                         |
| Admin-Rolle entziehen          | Individuelle Ausnahmen bleiben bestehen; Admin-Voraussetzungen gelten weiterhin |

Die UI zeigt `Role default`, `Individual setting` und `Effective access` mit Ursache. `Inherit` entfernt den Override. Ein ererbtes Recht wird mit `Deny` eingeschränkt; das Entfernen eines Allow ist nicht dasselbe wie ein Verbot.

### 3.4 Technische Einbindung

Ein kleiner `PermissionResolver` in `Archive.Core` und passende API-Policies ergänzen die vorhandene Cookie-Authentifizierung. Rollen und Ausnahmen werden aktuell aus der Datenbank gelesen und höchstens innerhalb eines Requests wiederverwendet. Es gibt weder dauerhafte Rechte-Claims noch einen Berechtigungs-Versionsdienst. Entzug wirkt beim nächsten Request ohne Neuanmeldung.

Die Session-Antwort enthält Rollen und effektiv erlaubte Rechte für Navigation und sichtbare Aktionen. Der Server prüft jede geschützte Aktion selbst. Bei Ausfall der Rechtequelle wird kein geschützter Zugriff gewährt.

Für Rollenänderungen, den Schutz des letzten Admins und die finale Direktpublikation werden kurze Datenbanktransaktionen verwendet. Gemeinsame Sperrreihenfolge: Zugriffsänderungssperre, soweit benötigt, dann Snapshot, dann Processing. Keine Sperre bleibt während Capture oder Objekt-Upload offen. Bereits laufende, zuvor autorisierte Review-Requests können ihren Request abschließen; neue Requests prüfen den entzogenen Zugriff.

## 4. Verarbeitung, Moderation und Publikation

### 4.1 Getrennte fachliche Zustände

Die vorhandenen Verarbeitungszustände bleiben: `QUEUED`, `RUNNING`, `COMPLETED`, `FAILED`, `CANCELLED`, `DISCARDED`, `ABORTED`.

Neu ist ausschließlich `Snapshot.ModerationStatus` mit `PENDING`, `APPROVED`, `REJECTED`, `RESUBMITTED`.

```text
IsPublic = ModerationStatus == APPROVED
           AND Quality != null
           AND ContentPrefix != null
           AND CaptureCompletedAt != null
```

`Quality` beschreibt das technische Ergebnis. Archivzeiten werden durch eine spätere Freigabe nicht umgeschrieben. Der Freigabezeitpunkt steht am Historieneintrag; eine weitere Snapshot-Spalte dafür entfällt.

### 4.2 Zulässige Übergänge

| Ausgang                      | Aktion                                | Voraussetzung                                                                     | Ziel          | Historie                    |
| ---------------------------- | ------------------------------------- | --------------------------------------------------------------------------------- | ------------- | --------------------------- |
| Neu                          | Übermitteln                           | Gültige URL/Datei, auch anonym                                                    | `PENDING`     | Submitted                   |
| `PENDING` oder `RESUBMITTED` | Genehmigen                            | Review-Recht, fertiges Ergebnis                                                   | `APPROVED`    | Approved, manuell           |
| `PENDING` oder `RESUBMITTED` | Ablehnen                              | Review-Recht, fertiges Ergebnis                                                   | `REJECTED`    | Rejected, optionaler Grund  |
| `REJECTED`                   | Erneut einreichen                     | Angemeldeter ursprünglicher Ersteller, Ergebnis vorhanden                         | `RESUBMITTED` | Resubmitted                 |
| Initiales `PENDING`          | Ohne manuelle Genehmigung publizieren | Erfolgreicher Capture, initialer Direktwunsch und aktuell vorhandenes Direktrecht | `APPROVED`    | Approved, Permission bypass |

`REJECTED → APPROVED` ist nicht zulässig. Wiedervorlage verwendet dieselbe ID und dieselben Inhalte, beginnt keinen neuen Capture und ändert keine Metadaten. Eine geänderte Quelle/Datei wird als neuer Snapshot übermittelt.

Die Queue zeigt standardmäßig fertige `PENDING`- und `RESUBMITTED`-Snapshots. Abgelehnte und genehmigte Einträge sind lesend filterbar. Prüfer erhalten keinen Wiedervorlageknopf für fremde Übermittlungen. Eine nummerierte Prüfungsrunde wird weder gespeichert noch angezeigt; die Historie zeigt die Abfolge.

Erfolglose oder abgebrochene Übermittlungen bleiben in der eigenen Liste. Ohne fertiges Ergebnis sind sie nicht genehmigbar und nicht als unveränderter fertiger Snapshot wiedervorlegbar. Ein neuer technischer Versuch beginnt über das vorhandene Formular als neue Übermittlung.

### 4.3 Publikation ohne manuelle Prüfung

Das Formular zeigt nur bei effektivem Direktrecht die standardmäßig deaktivierte Option `Publish without review`. Ohne Auswahl erfolgt die reguläre Prüfung, auch bei Admins.

- Der Server ermittelt Identität und Rechte selbst. Unberechtigter Direktwunsch ergibt `403`.
- Der initiale Submitted-Eintrag enthält in seinen JSONB-Daten `publicationMode = Review` oder `Direct`. Kein zusätzliches Snapshot-Feld.
- Bei Fertigstellung liest der Worker diese initialen Daten und prüft das Direktrecht erneut. Wiedervorlagen erhalten diesen Weg nicht.
- Wurde das Recht vor der maßgeblichen Transaktion entzogen, bleibt das Ergebnis `PENDING` und privat.
- Rechteänderung und abschließende Bypass-Prüfung werden über dieselbe kurze Zugriffsänderungssperre koordiniert. Bei überlappenden Vorgängen entscheidet die Transaktion, ob die Freigabe vor oder nach dem Entzug lag.
- Ergebnis, terminaler Historieneintrag, Freigabestatus und Freigabeeintrag werden atomar gespeichert. Wiederholung darf keine zweite Freigabe erzeugen.
- Ist eine belastbare Rechteprüfung nicht möglich, wird ausschließlich das Ergebnis gespeichert; manuelle Prüfung bleibt möglich.

Auch Direktpublikation ergibt `APPROVED`, trägt aber `PermissionBypass` als Herkunft anstatt einer erfundenen manuellen Prüfung. Öffentliche Antworten liefern weiterhin ausschließlich genehmigte Snapshots.

## 5. Vereinfachtes Datenmodell und JSONB-Historie

### 5.1 Rollen und Ausnahmen

| Entität                  | Daten und Regeln                                                         |
| ------------------------ | ------------------------------------------------------------------------ |
| `UserRole`               | `UserId`, `Role`; eindeutiger Schlüssel; ausschließlich User/Admin       |
| `UserPermissionOverride` | `UserId`, `Permission`, `Effect`; eindeutig pro Benutzer und Recht       |
| `ArchiveUser`            | Navigationen zu Rollen und Ausnahmen; keine zusätzlichen Kontrollspalten |

Rollen-Defaults und Rechtekatalog liegen typisiert im Core. Der vorhandene Benutzer-Zeitstempel wird nicht als verstecktes Vergleichstoken verwendet.

### 5.2 Snapshot und abgeleitete Lesedaten

Am Snapshot wird **nur `ModerationStatus` ergänzt**. Weitere benötigte Angaben entstehen durch gezielte Projektion:

| Information                        | Quelle                                                                                  |
| ---------------------------------- | --------------------------------------------------------------------------------------- |
| Letzte Einreichung                 | Neuester Submitted-/Resubmitted-Eintrag; beim Bestand ohne Eintrag `Snapshot.CreatedAt` |
| Freigabezeitpunkt und Herkunft     | Approved-Eintrag                                                                        |
| Bewusster Direktpublikationswunsch | Initialer Submitted-JSONB-Inhalt                                                        |
| Fortschritt und Queueposition      | Bestehendes `SnapshotProcessing` während seiner Aufbewahrung                            |
| Dauerhafter technischer Ausgang    | ProcessingFinished-Eintrag mit vorhandenem `ProcessingStatus`                           |
| Zeitpunkt dieses Ausgangs          | `OccurredAt` desselben Eintrags                                                         |
| Aktueller Ablehnungsgrund          | Zur aktuellen Ablehnung gehöriger Rejected-Eintrag                                      |

Leseantworten dürfen daraus berechnete Angaben enthalten. Diese werden nicht als Snapshot-Spalten gespiegelt. Bei Wiedervorlage bleibt ein früherer Grund historisch erhalten, ist aber kein aktueller Ablehnungsgrund des neuen Status.

### 5.3 Eine Historientabelle, abstrakte Eintragsdaten

Vorgesehen ist eine konkrete Persistenzentität `SnapshotHistoryEntry` in `archive.snapshot_history`. Die Abstraktion liegt im JSONB-Inhalt:

```text
SnapshotHistoryEntry
└── Data : SnapshotHistoryData (abstract, stored as JSONB)
    ├── SubmittedHistoryData
    ├── ResubmittedHistoryData
    ├── ApprovedHistoryData
    ├── RejectedHistoryData
    └── ProcessingFinishedHistoryData
```

Gemeinsame Spalten:

- `Id`: automatisch erzeugter numerischer Primärschlüssel, zugleich stabile Reihenfolge. Kein zusätzlicher Sequenz- oder Änderungszähler.
- `SnapshotId`: zugehöriger Snapshot.
- `OccurredAt`: UTC-Zeitpunkt.
- `ActorKind`: User, Anonymous oder System.
- Nullable `ActorUserId`: lokaler Benutzer; keine E-Mail, externe Provider-ID oder IP-Adresse.
- `Data`: erforderliche JSONB-Spalte mit dem konkreten Eintragsinhalt.

| JSONB-Typ                       | Inhalt                                                                     |
| ------------------------------- | -------------------------------------------------------------------------- |
| `SubmittedHistoryData`          | `type = submitted`, `publicationMode = Review/Direct`                      |
| `ResubmittedHistoryData`        | `type = resubmitted`; Akteur/Zeit stehen bereits an der Entität            |
| `ApprovedHistoryData`           | `type = approved`, `source = Manual/PermissionBypass/LegacyMigration`      |
| `RejectedHistoryData`           | `type = rejected`, optional `reason`                                       |
| `ProcessingFinishedHistoryData` | `type = processing_finished`, terminaler `status` aus dem vorhandenen Enum |

Ein Grund wird getrimmt, als Klartext behandelt, auf 2.048 Zeichen begrenzt und bei leerem Inhalt null. Terminale Daten enthalten nur COMPLETED, FAILED, CANCELLED, DISCARDED oder ABORTED; Fortschrittsänderungen erzeugen keine Historienflut.

Beispiele für Data; gemeinsame Metadaten werden nicht darin dupliziert:

```json
{ "type": "submitted", "publicationMode": "Review" }
```

```json
{ "type": "rejected", "reason": "Please review the source attribution." }
```

```json
{ "type": "processing_finished", "status": "FAILED" }
```

Die Speicherung verwendet eine lokale EF-Konfiguration für JSONB und einen kleinen Konverter mit zentralem `System.Text.Json`-Serializer. Der fachliche Property-Typ bleibt `SnapshotHistoryData`; SQL-Abfragen auf JSON-Schlüssel werden bei Bedarf gezielt im konkreten Repository ausgeführt. Npgsql unterstützt JSONB als String-Mapping, bei dem die Anwendung die Serialisierung übernimmt. [Npgsql: JSON Mapping](https://www.npgsql.org/efcore/mapping/json.html#string-mapping)

Die Payload-Hierarchie wird über explizite `JsonDerivedType`-Varianten und den Discriminator `type` serialisiert. Unbekannte Typkennungen werden als kontrollierter Lesefehler behandelt. [Microsoft: JSON polymorphism](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/polymorphism)

Weil JSONB die Schlüsselreihenfolge verändern kann, sieht der Serializer `AllowOutOfOrderMetadataProperties` vor. Diese Option gehört zum lokalen Serializer des Konverters; eine globale Umstellung der Datenbankverbindungen ist dafür nicht vorgesehen. [Npgsql: Polymorphic JSON mapping](https://www.npgsql.org/doc/types/json.html#polymorphic-json-mapping)

Keine TPH-/TPT-Hierarchie, zusätzliche Typspalte oder generische Ereignisplattform. Der Discriminator steht einmal im JSONB-Inhalt. Der aktuelle Moderationsstatus bleibt für Freigabe und Queue am Snapshot; die Historie erklärt Übergänge und bewahrt den Verarbeitungsausgang. Daraus entsteht kein allgemeines Event-Sourcing-Modell.

### 5.4 Integrität und Abfragen

- Historie wird ausschließlich angehängt. Keine Update-/Delete-Endpunkte.
- Moderationsmutation und Eintrag werden gemeinsam unter Snapshot-Zeilensperre gespeichert.
- Der Primärschlüssel wird innerhalb dieses Ablaufs vergeben. Damit bleibt die Reihenfolge pro Snapshot auch bei gleichen Zeiten stabil.
- Fertigstellung schreibt vor dem Cleanup genau einen terminalen Eintrag. Wiederholung erkennt den terminalen Zustand unter derselben Sperre.
- Nach Cleanup steht der Ausgang in der Historie. Bei Erfolg liefern zusätzlich die vorhandenen Ergebnisfelder den Erfolgsnachweis.
- Eigentümer sehen eigene Historie, Prüfer den Moderationspfad. Öffentliche DTOs enthalten keine privaten Entscheidungen.
- Bei späterer Benutzerlöschung bleibt die Historie erhalten; Actor-FK wird null, ActorKind User erlaubt `Former user`.
- Indexvorschläge: vorhandene Snapshot-Indizes verwenden, dazu Eigentümerfilter, Moderationsstatus und `(SnapshotId, Id)`. Einen Ausdrucksindex auf JSON-Typkennung erst bei nachgewiesenem Bedarf ergänzen.
- Listenprojektionen lesen Historienwerte datenbankseitig mit ihrer Seite. Kein vollständiges Laden aller Historien und kein Request pro Zeile.
- Neue Enum-Mappings in Runtime- und Design-Time-Konfiguration konsistent ergänzen. Migrationshistorie bleibt `public.__EFMigrationsHistory`.

## 6. Öffentliche Daten und Content-Auslieferung

### 6.1 Öffentliche Sichtbarkeit

Ein expliziter zentraler IsPublic-Query-Ausdruck wird in öffentlichen Repository-Methoden verwendet. Kein globaler EF-Filter, der Worker oder private Abfragen versteckt beeinflusst.

| Zugriff                                  | Nicht genehmigter Snapshot                                          |
| ---------------------------------------- | ------------------------------------------------------------------- |
| Liste/Suche                              | Fehlt in Treffern, Gesamtzahl, jüngsten Zeiten und Jahresverteilung |
| Öffentliche Details                      | 404, auch bei angemeldetem Admin                                    |
| Öffentlicher Content inkl. Originaldatei | 404                                                                 |
| Öffentlicher Screenshot                  | 404                                                                 |
| Viewer und bestehender Content-Proxy     | Keine private Auslieferung; Backend-Freigabe bleibt maßgeblich      |
| Objektablage                             | Privat; keine frei erreichbaren Rohobjekt-Links                     |

Die öffentliche API wird nicht je nach Session zum privaten Lesepfad. Private Antworten sind private/no-store. Geschützte APIs verwenden die eingeschränkte Frontend-CORS-Policy. Cookie-Mutationen benötigen serverseitigen Origin-/CSRF-Schutz; CORS alleine genügt nicht. SvelteKit-Serveraktionen dürfen nur den konfigurierten Backend-Origin ansprechen.

### 6.2 Anonyme Übermittlung

URL-/Dateiübermittlung bleibt anonym. Status-/SSE-Antworten einer bekannten ID enthalten nur technische Empfangs- und Fortschrittsdaten: keine Quell-URL, privaten Metadaten, Gründe oder Inhalts-URLs. Die bestehende zeitliche Aufbewahrung wird nicht zum anonymen Historienzugriff erweitert.

COMPLETED bedeutet `Capture complete`, nicht automatische Publikation. Nach regulärem Abschluss erscheint `Capture complete. Publication requires approval.` Ein Viewer-Link wird erst nach tatsächlich erfolgreichem öffentlichen Detailabruf angezeigt.

Während der Einreichung kommen Titel/Quelle aus dem lokalen Entwurf. Beim Wiederaufruf können angemeldete Eigentümer private Metadaten abrufen; Gäste sehen nur technischen Status. Cancel endet vor Speicherung des fertigen Ergebnisses und wird nicht zum Zurückziehen fertiger Snapshots erweitert. Keine neue anonyme Besitz- oder Recovery-Infrastruktur.

### 6.3 Prüfung des bestehenden Frontend-Content-Proxys

**Entscheidung: Der vorhandene öffentliche Proxy bleibt für den derzeitigen Aufbau erforderlich. Die zusätzlich geplante private Proxy-Strecke entfällt.** Dies bewertet den konkreten Code; ein Content-Proxy ist keine grundsätzliche Voraussetzung jedes Archivsystems.

| Befund                                                                                                           | Bedeutung                                                                     |
| ---------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------- |
| Viewer setzt contentHref auf relativen `/api/snapshots/.../content/index.html`-Pfad                              | Lädt heute ausdrücklich die SvelteKit-Route                                   |
| API liefert `frame-ancestors 'self'`                                                                             | Direkter API-iframe im anders-originären Frontend ist damit nicht freigegeben |
| Development nutzt Ports 5173 und 5200; Produktionsvorlage trennt archive.example.com und api.archive.example.com | Identische Origins können nicht vorausgesetzt werden                          |
| Gespeichertes HTML/CSS enthält root-relative Content-Pfade                                                       | Startseite und Assets müssen konsistent vom richtigen Origin kommen           |
| Proxy setzt CSP auf Frontend-Origin und erlaubt opaque-origin Font-Anfragen                                      | Übernimmt konkrete Auslieferungsaufgaben                                      |
| Proxy ergänzt `/archived-scrollbar.css`                                                                          | Ersatzloses Löschen verändert auch bestehende Darstellung                     |

Der Proxy prüft keine eigene Moderationsfreigabe; er darf nur den Backend-Endpunkt verwenden, der IsPublic durchsetzt. Keine Session-abhängige Öffnung dieses öffentlichen Pfads. Sandbox, CSP und nosniff bleiben erhalten.

Direktbezug vom API-Origin müsste Viewer, erlaubte Frame-Ancestors, Ressourcen, Fonts und Scrollbar-Einbindung gemeinsam ändern. Für diese Erweiterung ergibt das keinen geringeren notwendigen Änderungsumfang; deshalb wird der verwendete öffentliche Proxy nicht ersatzlos entfernt.

Die Moderation verwendet stattdessen **vorhandene Screenshots plus Metadaten und Historie**. URL-Capture und Offline-Import schreiben bereits screenshot.png. Dafür sind kein privater HTML-iframe, keine Umschreibung gespeicherter URLs und kein neuer SvelteKit-Content-Proxy nötig. Der geschützte Screenshot wird aus dem angemeldeten Hauptdokument mit Credentials abgerufen und als kurzlebige Blob-URL angezeigt; beim Wechsel wird diese freigegeben. Fehlende Vorschau erhält einen klaren Leerzustand bei weiterhin lesbaren Metadaten. Vollständiger privater HTML-Inhalt wird hier nicht zusätzlich ausgeliefert.

## 7. Vorgeschlagene API-Verträge

Die Pfade/DTOs sind Planung. GUIDs, JSON-Namen und ProblemDetails-Konventionen bleiben wie im Projekt. Keine Vergleichsnummern oder Parallelitäts-Header.

### 7.1 Eigene Übermittlungen

| Methode / Pfad                             | Schutz                                   | Inhalt                                                         |
| ------------------------------------------ | ---------------------------------------- | -------------------------------------------------------------- |
| `GET /api/user/submissions`                | Anmeldung und vollständige Registrierung | Nur CreatedByUserId des aktuellen Benutzers, paginiert         |
| `GET /api/user/submissions/{id}`           | Derselbe Eigentümer                      | Metadaten, Verarbeitung, Moderation, erlaubte Aktionen         |
| `GET /api/user/submissions/{id}/history`   | Derselbe Eigentümer                      | Stabile paginierte Historie                                    |
| `POST /api/user/submissions/{id}/resubmit` | Derselbe Eigentümer                      | Kein zusätzlicher Body; nur aus REJECTED mit fertigem Ergebnis |

Kein auswählbarer Benutzer-ID-Parameter. Fremde IDs ergeben 404, auch bei einem Admin über den Eigentümerpfad.

SubmissionResponse enthält id, title, sourceUrl, sourceType, createdAt, aus der Historie abgeleitetes lastSubmittedAt, processingStatus, moderationStatus, latestRejectionReason, canResubmit und nur bei Freigabe publicSnapshotUrl. processingStatus ist nullable, wenn für einen Bestandseintrag kein verlässlicher Ausgang mehr vorliegt. Keine erfundenen Zustände oder privaten Inhalts-URLs für normale Benutzer.

Filter: Text und Moderationsstatus; technische Verarbeitung separat anzeigen. Sortierung lastSubmittedAt desc, id desc, Standard 20/maximal 100 Einträge. Ein Dienstfehler ist keine leere Liste.

### 7.2 Moderation

| Methode / Pfad                                  | Schutz           | Inhalt                                              |
| ----------------------------------------------- | ---------------- | --------------------------------------------------- |
| `GET /api/moderation/snapshots`                 | snapshots.review | Einreichungen inkl. anonymer Einträge, Statusfilter |
| `GET /api/moderation/snapshots/{id}`            | snapshots.review | Metadaten, Qualität, erlaubte Aktionen              |
| `GET /api/moderation/snapshots/{id}/history`    | snapshots.review | Historie                                            |
| `GET /api/moderation/snapshots/{id}/screenshot` | snapshots.review | Geschütztes Bild; private, no-store                 |
| `POST /api/moderation/snapshots/{id}/approve`   | snapshots.review | Kein zusätzlicher Body                              |
| `POST /api/moderation/snapshots/{id}/reject`    | snapshots.review | `{ reason? }`                                       |

Queue: lastSubmittedAt asc, id asc; Entscheidungen über Filter lesbar. Kein universeller Status-Patch, zusätzlicher privater Content-Endpunkt oder Frontend-Content-Proxy. Screenshots ohne Session/Recht scheitern auch bei bekannter URL; die öffentliche Route bleibt vor Freigabe geschlossen.

### 7.3 Benutzerverwaltung

| Methode / Pfad                                   | Schutz                                    | Inhalt                                                    |
| ------------------------------------------------ | ----------------------------------------- | --------------------------------------------------------- |
| `GET /api/admin/users`                           | Admin und mindestens ein Verwaltungsrecht | Suche nach Handle/Name, minimale paginierte Daten         |
| `GET /api/admin/users/{id}/access`               | Wie oben                                  | Rollen, Ausnahmen, wirksame Rechte, bearbeitbare Bereiche |
| `GET /api/admin/permissions`                     | Wie oben                                  | Katalog und Rollen-Defaults                               |
| `PUT /api/admin/users/{id}/roles/admin`          | Admin und users.assign_roles              | `{ assigned }`                                            |
| `PUT /api/admin/users/{id}/permissions/{key}`    | Admin und users.assign_permissions        | `{ effect }`, Allow oder Deny                             |
| `DELETE /api/admin/users/{id}/permissions/{key}` | Admin und users.assign_permissions        | Persönliche Ausnahme entfernen                            |

User-Grundrolle und unbekannte Schlüssel sind nicht bearbeitbar. Nur ein Verwaltungsrecht erlaubt nur den entsprechenden Bereich. Jede kleine Aktion verändert exakt eine Rolle/Ausnahme und liefert den aktuellen Stand. Kein ersetzendes Speichern des gesamten Rechtebestands. Session-Daten erhalten Rollen und effektive Rechte; Core-Modelle werden über Mapper auf DTOs abgebildet. History-DTOs enthalten gemeinsame Metadaten und dokumentierte JSON-Datenvarianten.

### 7.4 Fehler und Nebenläufigkeit ohne Versionsinfrastruktur

- 401: keine gültige Anmeldung.
- 403: fehlendes Recht, erforderliche Admin-Rolle oder Registrierung.
- 404: fremder eigener Snapshot, fehlende oder nicht öffentliche Ressource.
- 409: aktuell unzulässiger Übergang oder Schutz des letzten handlungsfähigen Admins.
- 400: ungültige Eingabe oder zu lange Begründung.
- 503: benötigte Datenquelle nicht erreichbar.

Snapshot-Mutationen verwenden vorhandene Zeilensperren: aktuellen Status und Voraussetzungen innerhalb der Transaktion lesen, prüfen, ändern, Historie hinzufügen, committen. Bei unmittelbar konkurrierenden Entscheidungen aus einem offenen Zustand setzt sich eine durch; die nächste erkennt den entschiedenen Zustand und liefert 409. Doppelte unmittelbare Wiedervorlage erzeugt keinen weiteren Eintrag. Worker behalten Snapshot → Processing als Sperrreihenfolge.

**Grenze der Vereinfachung:** Ohne Vergleichstoken gibt es keine allgemeine Erkennung veralteter Browseransichten. Ist ein Snapshot nach zwischenzeitlicher Ablehnung/Wiedervorlage wieder prüfbar, gilt ein zulässiger Request für seinen aktuellen Zustand. Kein versteckter Timestamp-, Historien-ID- oder ETag-Vergleich. Die UI lädt bei Auswahl und nach Aktionen neu, verspricht damit aber keinen atomaren Altansichtsvergleich.

Bei zwei Änderungen derselben Ausnahme gilt die zuletzt erfolgreich gespeicherte zulässige Änderung. Unabhängige Änderungen überschreiben einander nicht, weil jede Anfrage nur ihren Eintrag betrifft. Der letzte handlungsfähige Admin wird gemeinsam mit der Mutation in einer serialisierten Zugriffstransaktion geprüft.

PublishAsync, SnapshotPublication und ProcessingCompletion.Published werden auf ihre Bedeutung als gespeichertes Capture-Ergebnis geprüft, etwa CompleteCaptureAsync/ResultStored. Fertige private Ergebnisobjekte dürfen nicht von der Worker-Fehlerbereinigung gelöscht werden. API-Beschreibungen wie Quality: null until publication werden auf technische Fertigstellung korrigiert.

## 8. Frontend-Seiten und Mockups

Genau drei neue Seiten:

| Route                  | Titel           | Zugang                                             | Mockup                                                       |
| ---------------------- | --------------- | -------------------------------------------------- | ------------------------------------------------------------ |
| `/account/submissions` | My submissions  | Angemeldeter, vollständig registrierter Eigentümer | [account-submissions.html](mockups/account-submissions.html) |
| `/admin/snapshots`     | Snapshot review | Effektives Review-Recht                            | [snapshot-review.html](mockups/snapshot-review.html)         |
| `/admin/users`         | User access     | Admin und mindestens ein Verwaltungsrecht          | [user-access.html](mockups/user-access.html)                 |

Implementierungsorte: `(site)/account/submissions`, `(site)/admin/snapshots`, `(site)/admin/users`. Kein weiterer Account-Index. `/account/complete` bleibt bestehen. Der ursprünglich vorgeschlagene Top-Level-Pfad wird nicht eingeführt und braucht keine Weiterleitung.

### 8.1 My submissions

- Eigene Einträge mit Titel, vollständiger kopierbarer URL, letzter Einreichung, Verarbeitung und Moderation.
- All/Pending/Approved/Rejected/Resubmitted als Filter.
- Aufklappbare History mit Ereignis, Zeit und optionalem Grund; keine Rundennummern.
- View snapshot nur bei Freigabe.
- Resubmit for review nur für fertige abgelehnte eigene Snapshots; Bestätigung nennt unveränderte Inhalte.
- Nach Wiedervorlage Status/Zeitpunkt aktualisieren, Historie ergänzen und aktuellen Ablehnungsgrund ausblenden. Alte Entscheidungen bleiben sichtbar.
- Kein nachträglicher Besitzanspruch auf anonyme Einreichungen.

### 8.2 Snapshot review

- Queue mit Filtern für offene und entschiedene Einträge.
- Metadaten, Qualität, vorhandener Screenshot und Historie auf derselben Seite.
- Approve als primäre Aktion; Reject öffnet Reason (optional) mit expliziter Bestätigung.
- Bei Rejected: Only the submitter can resubmit this snapshot. Kein Approve-/Resubmit-Weg.
- Nach Entscheidung Queue/Status/Historie aktualisieren; Fokus und Meldung folgen dem Ergebnis.
- Selbstprüfung erlaubt, da kein Vier-Augen-Prinzip beauftragt wurde.

### 8.3 User access

- Benutzerliste und Editor im selben Dokument.
- User als Grundrolle; Admin zuweisbar/entziehbar.
- Default, Inherit/Allow/Deny, wirksamer Zugang und eigene Save-Aktion je Recht.
- Erst Speichern wirkt; Fehler erhalten Eingaben und kennzeichnen den ungespeicherten Stand.
- Schutz des letzten Admins als konkrete Validierung; kein Versionskonflikt-Dialog.

### 8.4 Vorhandene Seiten und Gestaltung

/archive/new erhält Direktpublikationsoption und korrekte Abschlussmeldungen. Das Account-Menü führt zu /account/submissions; Review und User access erscheinen nur bei passenden Voraussetzungen. /snapshots/{id} bleibt der öffentliche Viewer mit vorhandenem Proxy. Server-Loads/APIs prüfen Zugang; Navigation ersetzt keine Autorisierung.

Editorial-Design bleibt maßgeblich: Newsreader, Source Sans 3, IBM Plex Mono, ruhige Register, Regeln und Metadaten. Status hat Symbol und Text, Fokus eine 3px-Outline; Hauptaktionen sind mindestens 44px hoch. URLs bleiben vollständig zugänglich. Mobile Listen fließen um. Light/Dark verändern keine archivierten Originalfarben.

Zustände: Laden, leere Liste, kein Treffer, Fehler/Retry, Rechteentzug, fachlich nicht mehr mögliche Aktion, laufende Mutation, gespeichert. UI bleibt Englisch.

HTML-Dateien sind isolierte Prototypen mit Beispieldaten und lokalen DOM-Aktionen. [preview.html](mockups/preview.html) ist eine Prüfhilfe für Seite, Breite, Theme und Vergrößerung, keine vierte Seite. Screenshots werden statisch illustriert; der sichere Bildabruf wird erst später implementiert.

## 9. Migration, Cleanup und Auslieferung

### 9.1 Migration

1. Rollen-/Override-Tabellen und eine Historientabelle mit JSONB anlegen. Am Snapshot nur Moderationsstatus ergänzen.
2. Konten User zuordnen, keine automatische Admin-Auswahl.
3. Bereits öffentliche fertige Snapshots auf APPROVED setzen; Capture-Zeiten erhalten.
4. Je Bestandssnapshot einen System-Freigabeeintrag mit LegacyMigration zum Übernahmezeitpunkt schreiben. Keine menschliche Altentscheidung oder Einreichungsabsicht erfinden. Vorhandene Ergebnisfelder belegen den Erfolg.
5. Vorhandene unfertige Einträge auf PENDING setzen. Terminale Processing-Daten einmalig in ProcessingFinished-Einträge übernehmen; bei laufenden Vorgängen nichts Terminales erfinden.
6. Worker/Cleanup umstellen. Alte API/Worker während Migration und Wechsel anhalten; kein gemischter Betrieb mit bisheriger Freigabe oder Löschung erfolgloser Einträge.

Fehlende Altdaten werden ohne belastbare Grundlage nicht rekonstruiert. Gelöschte Übermittlungen bleiben nicht wiederherstellbar. Migrationsprobe auf einer Datenbankkopie mit anonymen, registrierten, fertigen, fehlgeschlagenen und laufenden Einträgen. Alte Migrationen/History-Konfiguration nicht umschreiben.

### 9.2 Erster Admin

Ein vorhandenes vollständig registriertes Konto wird anhand seiner lokalen UUID bewusst als erster Admin benannt. Begrenzte einmalige Initialisierung nur ohne bestehenden Admin, danach deaktivieren. Keine öffentliche Bootstrap-Route oder Zuweisung anhand frei eingegebener Handles.

Änderungen dürfen nicht den letzten vollständig registrierten Admin entfernen, der beide Verwaltungsrechte effektiv besitzt. Prüfung und Mutation geschehen in derselben kurzen serialisierten Zugriffstransaktion, auch bei gleichzeitigen Änderungen/Selbständerungen. Keine neuen Benutzer-Kontrollspalten.

### 9.3 Cleanup und Rücknahme

Worker, Cancel und Wiederherstellung sichern terminale Daten atomar in der Historie. Erst danach darf Cleanup abgelaufene Processing-Zeilen und temporäre Uploadobjekte entfernen; niemals Snapshot/Historie. Vorhandene terminale Zeilen ohne Historie werden vor Entfernung einmalig gesichert. Wartende/laufende Vorgänge bleiben unberührt.

Fertige Ergebnisdateien bleiben bei Pending und Rejected erhalten, damit spätere Genehmigung derselben Inhalte möglich bleibt. Keine automatische Löschung abgelehnter Inhalte.

Die alte API interpretiert Quality != null als Publikation. Bei Rücknahme zuerst öffentliche Auslieferung sperren und Datenbank/Backup abgestimmt wiederherstellen; kein öffentlich weiterlaufendes blindes Downgrade.

## 10. Umsetzung nach Freigabe

| Reihenfolge | Paket             | Ergebnis                                                                |
| ----------- | ----------------- | ----------------------------------------------------------------------- |
| 1           | Domain/Migration  | Rollen, Overrides, Moderationsstatus, JSONB-Historie, Bestandsübernahme |
| 2           | Rechte            | Resolver, Policies, Session-Rechte, erster/letzter Admin                |
| 3           | Verarbeitung      | Atomare Historie, Wiedervorlage, Direktpublikation, Cleanup             |
| 4           | Öffentliche Daten | Freigaberegel für Suche, Zahlen, Details, Content, Screenshot           |
| 5           | Private API       | Eigentümerdaten, Review-Bilder/Entscheidungen, Benutzerverwaltung       |
| 6           | OpenAPI           | Laufendes Backend, Live-Spezifikation, generierte Typen prüfen          |
| 7           | Frontend          | Drei Seiten, Account-Navigation, Formular, Screenshot-Vorschau          |
| 8           | Abnahme           | Integrationstests, Browser, Migrationsprobe                             |

Bestehende Repositories, Result-Modelle und Mapper verwenden. Kein generisches Repository, MediatR, CQRS-Framework oder Eventbus. `///`, `//` und `/* ... */` dokumentieren insbesondere Deny-Priorität, JSONB-Varianten, Übergänge, Sperrreihenfolge und Cleanup.

Nach späteren API-Änderungen die API mit bestehender Development-Konfiguration auf localhost:5200 starten und im Frontend ausführen:

```sh
bun --bun run generate:api
bun --bun run check
bun --bun run build
```

Der Zugriff von @hey-api/openapi-ts auf http://localhost:5200/openapi/v1.json bleibt unverändert. Keine Dateispezifikation oder manuell geänderten generierten Typen. Jetzt ändern sich keine Verträge, daher weder Backend-Start noch API-Generierung für diese Planung.

## 11. Abnahme und spätere Prüfungen

| Nr. | Szenario                                | Erwartung                                                             |
| --- | --------------------------------------- | --------------------------------------------------------------------- |
| A1  | Gast übermittelt URL/Datei              | Verarbeitung möglich, ohne Freigabe privat                            |
| A2  | Gast ruft Account-Liste/private API auf | Anmeldung erforderlich, keine privaten Daten                          |
| A3  | Eigene/fremde IDs lesen                 | Nur eigene Daten; fremde IDs 404                                      |
| A4  | Anmeldung nach anonymer Übermittlung    | Keine Zuordnung oder Wiedervorlage                                    |
| A5  | Fertiger PENDING-Snapshot               | Fehlt in öffentlichen Daten und Proxy                                 |
| A6  | Genehmigung                             | APPROVED plus Historie, öffentliche Auslieferung                      |
| A7  | Ablehnung mit/ohne Grund                | REJECTED, optionaler Grund sichtbar                                   |
| A8  | Eigene Wiedervorlage                    | Gleiche ID/Inhalte, RESUBMITTED, Verlauf erhalten                     |
| A9  | Genehmigung direkt aus REJECTED         | 409, keine Freigabehistorie                                           |
| A10 | Fremde Wiedervorlage                    | Nicht erlaubt                                                         |
| A11 | Entscheidung nach Wiedervorlage         | Neue Entscheidung, frühere Ereignisse erhalten                        |
| A12 | User-Allow/Admin-Deny                   | Persönliche Ausnahme wirkt in beide Richtungen                        |
| A13 | Ausnahme entfernen                      | Rollenregel beim nächsten Request                                     |
| A14 | Berechtigter Direktwunsch               | Initiale JSONB-Absicht, später APPROVED mit PermissionBypass          |
| A15 | Direktrecht vor Ende entzogen           | Ergebnis gespeichert, PENDING                                         |
| A16 | Unberechtigter Direktwunsch             | 403                                                                   |
| A17 | Unmittelbar parallele Entscheidungen    | Status verhindert doppelte Mutation/Historie                          |
| A18 | Cleanup nach Erfolg/Fehler/Ablehnung    | Snapshot/Historie/technischer Ausgang erhalten                        |
| A19 | Rechteentzug bei aktiver Session        | Nächster geschützter Request scheitert                                |
| A20 | Letzte Admin-Rechte parallel entziehen  | Handlungsfähiger Admin bleibt                                         |
| A21 | Bestand                                 | Genehmigt, Systemherkunft erkennbar                                   |
| A22 | Privater Screenshot direkt ohne Recht   | Zugriff verweigert, keine öffentliche Ersatz-URL                      |
| A23 | Konto erstellen/Admin entziehen         | Grundrolle bleibt                                                     |
| A24 | Ohne Ergebnis genehmigen                | 409                                                                   |
| A25 | JSONB-Varianten speichern/lesen         | Typisierte Daten auch nach Schlüsselumsortierung korrekt              |
| A26 | Gleiches Recht parallel ändern          | Zuletzt gespeicherte zulässige Änderung gilt, andere Rechte unberührt |
| A27 | Alte Ansicht nach mehreren Übergängen   | Aktueller Zustand entscheidet; kein behaupteter Versionsschutz        |

PostgreSQL-Integrationstests prüfen Eigentümerfilter, Freigabefilter, JSONB-Roundtrip, Deny-Priorität, atomare Historie und Sperren. In-Memory ersetzt dies nicht. Tests erst bei Implementierung im dann vorhandenen Projektstil.

Frontend: check/build und integrierter Browser für Desktop/768/390/320px, Light/Dark, Vergrößerung, Tastatur, lange URLs, Leer-/Fehlerzustände und Aktionen. Öffentlichen Proxy inklusive Fonts, Originaldateien, Range, CSS und Frames auf Regression prüfen. Private Bildvorschau darf keine HTML-Auslieferung öffnen.

## 12. Planungsprüfung

- Ursprünglicher Fetch: master und origin/master identisch auf 0abec2f; separater Branch erstellt.
- Überarbeitung auf demselben Branch, ohne Produktivänderungen.
- API, Migration, Nebenläufigkeit und Tests ohne Versionsfelder, Rundenzähler oder Snapshot-Duplikate für Historienwerte.
- Mockups und [Prüfprotokoll](verification.md) spiegeln Account-Route und vereinfachte Historie wider.
- Implementierung wartet auf ausdrückliche Freigabe.
