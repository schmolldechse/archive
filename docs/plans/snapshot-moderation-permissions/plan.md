# Rollen, individuelle Berechtigungen und Snapshot-Moderation

**Status:** Lösungsvorschlag zur Freigabe; keine Implementierung.

**Stand:** 2026-10-17.

**Basis:** `master` / `origin/master`, Commit `0abec2f6f77b07cfc88b2f5b8f2e5edfde1c13bc`.

**Planungsbranch:** `docs/snapshot-moderation-permissions`.

## 1. Auftrag und verbindliche Entscheidungen

Diese Erweiterung umfasst ausschließlich zwei Rollen (`User`, `Admin`), persönliche Rechte mit erlaubenden und einschränkenden Ausnahmen, die Genehmigung von Snapshots, Publikation ohne vorherige manuelle Genehmigung und eine abstrakte Snapshot-Historie. Hinzu kommen die dafür benötigten Seiten und Zugriffsregeln.

Im Interview wurden folgende Punkte verbindlich geklärt:

1. Die Benutzerseite zeigt **nur die eigenen Übermittlungen**, niemals die anderer Benutzer oder anonyme Übermittlungen.
2. **Der angemeldete Ersteller** darf seinen abgelehnten Snapshot erneut einreichen. Ein Prüfer erhält dieses Recht nicht allein durch seine Prüferberechtigung.
3. Anonyme Besucher können weiterhin Snapshots übermitteln. Eine Wiedervorlage desselben Snapshots ist nur für angemeldete Ersteller möglich; anonym ist eine neue Übermittlung über das bestehende Formular möglich.
4. Bereits vorhandene öffentliche Snapshots werden als **genehmigt** übernommen.
5. Eine Ablehnung darf nach einer erneuten Einreichung von einer Genehmigung abgelöst werden. Die frühere Ablehnung bleibt in der Historie erhalten.
6. Implementiert wird erst nach ausdrücklicher Freigabe dieses Vorschlags. Dieser Commit enthält ausschließlich Planung und isolierte HTML-Mockups.

Nicht vorgesehen sind zusätzliche Rollen, ein Rollenbaukasten, Projektberechtigungen, Benachrichtigungen, neue Anmeldeanbieter, Bearbeitung bereits archivierter Inhalte, Massenmoderation, eine allgemeine Audit-Plattform oder das Zurückziehen bereits genehmigter Snapshots. Die bestehenden technischen Capture- und Upload-Verfahren bleiben die Grundlage.

## 2. Befund im bestehenden Projekt

Die folgenden Befunde beziehen sich auf den oben genannten Basis-Commit:

| Bereich          | Bestehende Umsetzung                                                                             | Konsequenz für die Erweiterung                                                    |
| ---------------- | ------------------------------------------------------------------------------------------------ | --------------------------------------------------------------------------------- |
| Backend          | ASP.NET Core API, `Archive.Core`, separater `Archive.Worker`, EF Core/PostgreSQL                 | Controller, Mapper, Core-Modelle und konkrete Repositories fortführen             |
| Benutzer         | `Entities/Authentication/ArchiveUser.cs`, externe Identitäten, Cookie-Session                    | Lokalen Benutzer um Rollenzuordnungen und persönliche Ausnahmen ergänzen          |
| Rechte           | `AddAuthorization()` vorhanden, noch kein fachlicher Berechtigungskatalog                        | Zentralen Resolver und kleine Authorization-Handler ergänzen                      |
| Eigentümer       | `Snapshot.CreatedByUserId` ist nullable; nur abgeschlossene Registrierungen werden zugeordnet    | Eigentümer serverseitig übernehmen; anonyme Einreichungen bleiben ohne Eigentümer |
| Übermittlung     | `SnapshotsController.Processing.cs`, URL und Datei, anonym erlaubt                               | Beide Eingänge erhalten dieselben Moderationsregeln                               |
| Veröffentlichung | `SnapshotRepository.Search.cs` sowie Content/Screenshot prüfen im Wesentlichen `Quality != null` | Technisches Ergebnis und öffentliche Freigabe ausdrücklich trennen                |
| Detailzugriff    | `GetByIdAsync` liefert auch noch nicht fertiggestellte Metadaten                                 | Öffentliche Details auf genehmigte, fertige Snapshots beschränken                 |
| Worker           | `SnapshotProcessingRepository.Worker.cs` setzt Ergebnisfelder in `PublishAsync`                  | Fertigstellung speichert das Ergebnis; Freigabe wird separat bestimmt             |
| Verarbeitung     | `processing.snapshot_processing` ist flüchtig; Cleanup löscht erfolglose Snapshots               | Übermittlung und Historie dauerhaft erhalten; nur flüchtige Daten bereinigen      |
| Status           | Öffentliche Status-/SSE-Endpunkte; technisch fertig wird derzeit aus `Quality` abgeleitet        | Technischen Status weiterhin von Moderation unterscheiden                         |
| Frontend         | Svelte 5/SvelteKit, `(site)`-Layout, Auth-Kontext, wiederverwendbare UI-Komponenten              | Bestehende Muster verwenden, keine neue UI-Bibliothek                             |
| Vorschau         | Öffentlicher Snapshot-Viewer und SvelteKit-Content-Proxy                                         | Private Prüfvorschau braucht einen eigenen geschützten Datenpfad                  |
| API-Typen        | `frontend/openapi-ts.config.ts` liest `http://localhost:5200/openapi/v1.json`                    | Nach späteren Vertragsänderungen aus laufendem Backend neu generieren             |

Wichtige Ausgangsdateien:

- [Snapshot](../../../backend/Archive.Core/Entities/Snapshot.cs)
- [Snapshot-Repository](../../../backend/Archive.Core/Repositories/SnapshotRepository/SnapshotRepository.cs)
- [Suche](../../../backend/Archive.Core/Repositories/SnapshotRepository/SnapshotRepository.Search.cs)
- [Verarbeitung und Cleanup](../../../backend/Archive.Core/Repositories/SnapshotProcessingRepository/)
- [Authentifizierung](../../../backend/Archive.Api/Authentication/ArchiveAuthenticationExtension.cs)
- [Übermittlungsformular](<../../../frontend/src/routes/(site)/archive/new/+page.svelte>)
- [Designsystem](../../../.agents/skills/designing-editorial-interfaces/references/design-system.md)

Die zwei vorgefundenen unversionierten Dateien `deploy/production/local.compose.yml` und `deploy/production/start-local.sh` gehören nicht zu dieser Planung und werden nicht aufgenommen.

## 3. Rollen und Rechte

### 3.1 Rollenmodell

Die beiden Rollen sind ein fest definierter Katalog, keine frei editierbaren Datenbankobjekte:

- `User`: Grundrolle aller lokalen Benutzer, einschließlich bestehender Konten.
- `Admin`: zusätzlich zuweisbare und entziehbare Rolle für Administration.

Ein Benutzer besitzt mehrere Rollenzuordnungen, derzeit ausschließlich `User` und optional `Admin`. `User` ist verpflichtend und wird beim Erstellen eines Kontos zugeordnet. Die Oberfläche erlaubt das Vergeben und Entziehen von `Admin`; die Grundrolle wird als fest angezeigt. Damit bleibt nach Entzug von `Admin` ein normaler Benutzer zurück. Dies ist ein konkreter Umsetzungsvorschlag für die gewünschten zwei Rollen.

### 3.2 Kleiner Berechtigungskatalog

| Schlüssel                            | Englische UI-Bezeichnung      | User | Admin |
| ------------------------------------ | ----------------------------- | ---- | ----- |
| `snapshots.review`                   | Review snapshots              | Nein | Ja    |
| `snapshots.publish_without_approval` | Publish without approval      | Nein | Ja    |
| `users.assign_roles`                 | Manage user roles             | Nein | Ja    |
| `users.assign_permissions`           | Manage individual permissions | Nein | Ja    |

`Review snapshots` umfasst Lesen der Prüfwarteschlange, geschützte Prüfvorschau, Genehmigen und Ablehnen. Die beiden Aktionen benötigen keine getrennten Berechtigungsschlüssel. `Publish without approval` gilt nur für neue eigene Übermittlungen mit explizit gewählter Direktpublikation, nicht für beliebige fremde oder bereits abgelehnte Snapshots.

Die eigene Übermittlungsliste und die eigene Wiedervorlage sind an Anmeldung, abgeschlossene Registrierung und Eigentümerschaft gebunden. Dafür wird kein zusätzliches administrierbares Recht eingeführt. Öffentliches Lesen genehmigter Snapshots und anonymes Übermitteln bleiben ohne Konto möglich.

Die Benutzerverwaltung erfordert zusätzlich zur jeweiligen Verwaltungsberechtigung die Rolle `Admin`. Ein `User` mit persönlich erlaubtem `snapshots.review` kann dagegen die Prüferseite nutzen. Dadurch kann Moderation delegiert werden, ohne auch Benutzerverwaltung zu delegieren. Ein `User` mit einem Verwaltungs-Override bleibt durch die fehlende Admin-Rolle ausgeschlossen; die Oberfläche zeigt dieses zusätzliche Erfordernis ausdrücklich an.

### 3.3 Persönliche Ausnahmen

Pro `(UserId, Permission)` gibt es höchstens einen Eintrag mit `Allow` oder `Deny`. Kein Eintrag bedeutet `Inherit`.

Die Auswertung erfolgt pro Berechtigung in dieser Reihenfolge:

1. Nicht authentifizierter oder nicht vorhandener Benutzer: kein geschütztes Recht.
2. Persönliches `Deny`: verweigern, auch für Admins.
3. Persönliches `Allow`: erlauben.
4. Mindestens eine zugeordnete Rolle gewährt das Recht: erlauben.
5. Sonst: verweigern.

Die zusätzlichen Bedingungen Anmeldung, Registrierung, Eigentümerschaft und gegebenenfalls Admin-Rolle werden daneben geprüft. Ein Allow ersetzt diese Bedingungen nicht. Es gibt keinen Admin-Sonderweg, der ein persönliches Deny ignoriert.

| Fall                                              | Ergebnis                                                                                       |
| ------------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| User, `snapshots.review = Allow`                  | Darf prüfen                                                                                    |
| Admin, `snapshots.review = Deny`                  | Darf nicht prüfen                                                                              |
| Admin, kein Override                              | Erhält das Rollenrecht                                                                         |
| Override entfernen                                | Zur Rollenregel zurückkehren; nicht automatisch verbieten                                      |
| Admin mit persönlichem Allow verliert Admin-Rolle | Persönliche Ausnahmen bleiben sichtbar bestehen; Verwaltungsaktionen benötigen weiterhin Admin |

Die UI zeigt für jedes Recht **Role default**, **Individual setting** (`Inherit`, `Allow`, `Deny`) und **Effective access**. Ein abschließender Satz nennt die Ursache, etwa `Denied by individual setting`.

### 3.4 Technische Autorisierung

Ein `PermissionResolver` in `Archive.Core` liest lokale Rollen und Ausnahmen. Die API bindet ihn über einen kleinen Requirement/Handler an benannte Policies. Kein generischer Policy-Interpreter und kein Austausch des vorhandenen Login-Systems.

Cookie-Claims enthalten weiterhin die Identität. Rollen und Rechte werden für geschützte Requests aktuell aus der Datenbank geladen und höchstens innerhalb desselben Requests wiederverwendet. Entzug wirkt somit beim nächsten Request und benötigt keine erneute Anmeldung. Die Session-Antwort enthält Rollen und effektiv erlaubte Rechte für die Navigation, ist aber keine vertrauenswürdige Entscheidungsquelle für Mutationen.

Bei Direktpublikation muss der Worker den maßgeblichen Zugriffsstand innerhalb der Fertigstellungstransaktion gegen parallele Zugriffsänderungen absichern. Eine früher geladene Policy-Antwort allein reicht dafür nicht aus. Rollen-/Override-Änderungen und die finale Bypass-Prüfung koordinieren sich über die Zugriffsänderungssperre; immer Zugriffsstand vor Snapshot und Processing sperren. Die Sperre wird nur während der kurzen Datenbankentscheidung gehalten, niemals während Capture oder Objekt-Upload. Bei zeitlich überlappenden Operationen bestimmt der Commit-/Sperrablauf, ob die Freigabe vor oder nach dem Rechteentzug lag. Normale Review-Requests prüfen ihre aktuellen Rechte pro Request; bereits laufende Entscheidungen können ihren vor dem Entzug autorisierten Request abschließen.

Auch lang laufende private Vorschau-/Stream-Zugriffe müssen die Berechtigung bei jedem neuen Ressourcenrequest prüfen. Bei nicht erreichbarer Rechtequelle wird Zugriff nicht erteilt; die API meldet einen Dienstfehler statt anonyme Daten zu liefern.

## 4. Snapshot-Zustände und Veröffentlichung

### 4.1 Zwei unabhängige Dimensionen

**Verarbeitung:** vorhandene Zustände `QUEUED`, `RUNNING`, `COMPLETED`, `FAILED`, `CANCELLED`, `DISCARDED`, `ABORTED`.

**Moderation:** neue Zustände `PENDING`, `APPROVED`, `REJECTED`, `RESUBMITTED`.

Ein fertiges Capture ist noch keine öffentliche Freigabe. Die zentrale Regel lautet:

```text
IsPublic = ModerationStatus == APPROVED
           AND Quality != null
           AND ContentPrefix != null
           AND CaptureCompletedAt != null
```

`Quality` bleibt eine Aussage über das technische Ergebnis. `ApprovedAt` ist der Freigabezeitpunkt und ersetzt keinen Capture-Zeitpunkt. Die öffentliche Suche behält ihre bisherige zeitliche Sortierung nach Snapshot-Erstellung; verspätete Genehmigung schreibt Archivzeiten nicht um.

### 4.2 Zustandsübergänge

| Ausgang                              | Aktion                                | Voraussetzung                                                                                   | Ziel          | Historie                          |
| ------------------------------------ | ------------------------------------- | ----------------------------------------------------------------------------------------------- | ------------- | --------------------------------- |
| Neu                                  | Übermitteln                           | Gültige URL oder Datei, anonym möglich                                                          | `PENDING`     | Submitted, Runde 1                |
| `PENDING` / `RESUBMITTED`            | Genehmigen                            | Effektives Review-Recht; Ergebnis fertig                                                        | `APPROVED`    | Approved, manuell                 |
| `PENDING` / `RESUBMITTED`            | Ablehnen                              | Effektives Review-Recht; Ergebnis fertig                                                        | `REJECTED`    | Rejected, optionale Begründung    |
| `REJECTED`                           | Erneut einreichen                     | Angemeldeter ursprünglicher Ersteller; Ergebnis noch vorhanden                                  | `RESUBMITTED` | Resubmitted, nächste Runde        |
| `RESUBMITTED`                        | Erneut ablehnen                       | Wie oben                                                                                        | `REJECTED`    | Neue Entscheidung zur neuen Runde |
| `PENDING`, erstmalige Fertigstellung | Ohne manuelle Genehmigung publizieren | Direktpublikation bei Übermittlung gewählt; Ersteller besitzt das Recht auch bei Fertigstellung | `APPROVED`    | Approved, Permission bypass       |

`REJECTED → APPROVED` ist nicht erlaubt. Die Wiedervorlage muss zuerst als eigener Zustand gespeichert werden. Abgelehnte Snapshots bleiben mit ihren Ergebnisdateien erhalten. Die Wiedervorlage verwendet dieselbe Snapshot-ID und dieselben archivierten Inhalte, startet keinen neuen Capture und ändert keine Metadaten. Eine geänderte Quelle oder Datei wird als neuer Snapshot über das bestehende Formular übermittelt.

Die Prüfwarteschlange enthält standardmäßig technisch fertige `PENDING`- und `RESUBMITTED`-Snapshots. Ein zusätzlicher Filter zeigt abgelehnte Entscheidungen lesend. Abgelehnte Einträge bieten für Prüfer keine direkte Freigabe und keinen Wiedervorlageknopf. Nur der Ersteller sieht `Resubmit for review` in seiner eigenen Liste.

Technisch fehlgeschlagene oder abgebrochene Übermittlungen bleiben im eigenen Register sichtbar. Sie werden weder öffentlich noch manuell genehmigt und lassen sich nicht als fertiger Snapshot wiedervorlegen. `Archive a page` beginnt bei Bedarf eine neue Übermittlung; ein Retry-System wird nicht ergänzt.

### 4.3 Publizieren ohne vorherige Genehmigung

Das bestehende Formular erhält ausschließlich für effektiv berechtigte Benutzer die standardmäßig deaktivierte Option `Publish without review`. Ohne Auswahl gilt die normale Prüfung, auch für Admins. Beide Übermittlungsformate behandeln die Option gleich.

- Der Server liest Benutzer-ID und Rechte selbst; ein Client kann keinen Freigabestatus setzen.
- Eine unberechtigte Direktpublikationsanforderung ergibt `403`; sie wird nicht stillschweigend ausgeführt oder heruntergestuft.
- Die bewusste Anforderung wird bei Übermittlung gespeichert.
- Erst bei erfolgreicher Fertigstellung wird das Recht des ursprünglichen Erstellers erneut geprüft. Bei zwischenzeitlichem Entzug bleibt der Snapshot `PENDING` und privat.
- Genehmigung, Ergebnisdaten und Historieneintrag werden gemeinsam festgeschrieben. Das Freigabeereignis trägt `PermissionBypass`, keine erfundene manuelle Entscheidung.
- Bei fehlgeschlagener Rechteabfrage bleibt die technische Fertigstellung möglich, aber es erfolgt keine Direktfreigabe. Eine manuelle Prüfung bleibt möglich.
- Bei `RESUBMITTED` gibt es keinen Direktpublikationsweg. Eine Ablehnung kann mit diesem Recht nicht umgangen werden.

Damit liefern öffentliche Endpunkte weiterhin ausschließlich `APPROVED`, auch wenn keine Person vorher auf `Approve` geklickt hat.

## 5. Datenmodell und abstrakte Historie

### 5.1 Authentifizierung

| Entität                  | Wesentliche Felder und Regeln                                                                                         |
| ------------------------ | --------------------------------------------------------------------------------------------------------------------- |
| `UserRole`               | `UserId`, `Role`; zusammengesetzter eindeutiger Schlüssel; nur User/Admin                                             |
| `UserPermissionOverride` | `UserId`, `Permission`, `Effect`; eindeutiger Schlüssel `(UserId, Permission)`; nur bekannte Schlüssel und Allow/Deny |
| `ArchiveUser`            | Navigationen zu Rollen/Ausnahmen; `AccessVersion` als explizite Version für Verwaltungsänderungen                     |

Rollen-Defaults und Rechtekatalog liegen typisiert im Core. Eine zusätzliche Tabelle mit frei änderbaren Rollenrechten ist für diese zwei Rollen unnötig. Metadaten für die Verwaltungsoberfläche kommen aus demselben Katalog.

### 5.2 Snapshot-Ergänzungen

| Feld                              | Zweck                                                                                 |
| --------------------------------- | ------------------------------------------------------------------------------------- |
| `ModerationStatus`                | Aktueller fachlicher Status                                                           |
| `ModerationVersion`               | Explizite Version für konkurrierende Entscheidungen/Wiedervorlagen                    |
| `SubmissionNumber`                | Prüfungsrunde, beginnend mit 1                                                        |
| `LastSubmittedAt`                 | Letzte Einreichung; Wiedervorlage ändert nicht `CreatedAt`                            |
| `ApprovedAt`                      | Zeitpunkt wirksamer Freigabe, sonst null                                              |
| `PublishWithoutApprovalRequested` | Bei initialer Übermittlung autorisierte Direktpublikationsabsicht                     |
| `ProcessingOutcome`               | Nullable, dauerhaftes terminales Ergebnis aus dem bestehenden `ProcessingStatus`-Enum |
| `ProcessingFinishedAt`            | Terminalzeit auch bei Fehler/Abbruch                                                  |

`ProcessingOutcome` darf nur terminale Werte enthalten. Während aktiver Verarbeitung ist es null und die vorhandene Processing-Tabelle liefert Queue-/Fortschrittsdaten. Nach Cleanup bleibt der terminale Ausgang am Snapshot erhalten. Die API leitet daraus einen einheitlichen lesbaren Verarbeitungszustand ab; es werden keine zwei aktiven Zustandsmaschinen gepflegt.

### 5.3 Echte abstrakte Entität

Vorgesehen ist eine **abstrakte C#-Klasse** `SnapshotHistoryEntry`. Sie ist nicht direkt instanziierbar. Zwei konkrete Entitäten erben davon:

```text
SnapshotHistoryEntry (abstract)
├── SnapshotSubmissionHistoryEntry
└── SnapshotDecisionHistoryEntry
```

Gemeinsame Felder:

- `Id`, `SnapshotId`, `Sequence`, `OccurredAt` in UTC.
- `SubmissionNumber` zur Zuordnung zur Prüfungsrunde.
- `ActorKind`: `User`, `Anonymous` oder `System`.
- Nullable `ActorUserId`; keine E-Mail, externe Provider-ID oder IP-Adresse.

`SnapshotSubmissionHistoryEntry` beschreibt `Submitted` oder `Resubmitted`. Bei der ersten anonymen Einreichung steht `ActorKind = Anonymous`; bei einer Wiedervorlage ist der Akteur der angemeldete Eigentümer.

`SnapshotDecisionHistoryEntry` enthält Ausgangs-/Zielstatus, optionalen Ablehnungsgrund und `DecisionSource = Manual | PermissionBypass | LegacyMigration`. Ein Ablehnungsgrund ist auf 2.048 Zeichen begrenzt, wird getrimmt, als Klartext behandelt und bei leerem Inhalt als null gespeichert. Genehmigungen benötigen keinen Grund.

EF Core bildet die Hierarchie per **Table per Hierarchy** in `archive.snapshot_history` mit einem stabilen Discriminator ab. Eine Tabelle, ein Index für die zeitliche Folge und konkrete Typen reichen aus; Event Sourcing wird nicht eingeführt. Die aktuelle Snapshot-Zeile bleibt die Quelle für Leseabfragen, die Historie erklärt ihre Übergänge.

### 5.4 Integrität und Aufbewahrung

- Historie wird ausschließlich angehängt. Keine Update-/Delete-Endpunkte.
- `(SnapshotId, Sequence)` ist eindeutig; die Reihenfolge wird unter Snapshot-Zeilensperre vergeben, nicht allein aus Zeitstempeln abgeleitet.
- Statusänderung, Versionsinkrement und Historieneintrag erfolgen in derselben Datenbanktransaktion.
- Historie besitzt keine Ablaufzeit. Verarbeitungscleanup darf Snapshot und Historie nicht mehr löschen.
- Bei späterer Benutzerlöschung bleibt der Eintrag erhalten; Actor-FK wird null und die Oberfläche zeigt `Former user`. ActorKind verhindert Verwechslung mit einer anonymen Einreichung.
- Die Benutzerliste zeigt Historie und Ablehnungsgründe nur für eigene Snapshots. Prüfer erhalten sie über den geschützten Moderationspfad. Öffentliche DTOs enthalten keine privaten Entscheidungen.
- Indexvorschläge: `(CreatedByUserId, LastSubmittedAt, Id)`, `(ModerationStatus, LastSubmittedAt, Id)`, `(SnapshotId, Sequence)`; tatsächliche Querypläne bei Implementierung prüfen.
- PostgreSQL-Enum-Mapping wie bei bestehenden Enums in Runtime- und Design-Time-Konfiguration ergänzen. Die Migrationshistorie bleibt in `public.__EFMigrationsHistory`.

## 6. Öffentliche und geschützte Lesewege

### 6.1 Öffentliche Sichtbarkeit vollständig anwenden

Ein expliziter, zentraler Query-Ausdruck für `IsPublic` wird in öffentlichen Repository-Methoden wiederverwendet. Kein globaler EF-Filter, der private Prüfabfragen oder den Worker unbeabsichtigt beeinflusst.

| Zugriff                                  | Verhalten für nicht genehmigte Snapshots                                      |
| ---------------------------------------- | ----------------------------------------------------------------------------- |
| `GET /api/snapshots`                     | Nicht in Treffern, Gesamtzahlen, jüngsten Zeiten oder Jahresverteilung        |
| `GET /api/snapshots/{id}`                | `404`, auch wenn auf demselben Request ein Admin angemeldet ist               |
| `GET /api/snapshots/{id}/content/{path}` | `404`, einschließlich Startseite und Originaldatei                            |
| `GET /api/snapshots/{id}/screenshot`     | `404`                                                                         |
| `/snapshots/{id}`                        | Kein öffentlicher Viewer für private Einträge                                 |
| Bestehender Frontend-Content-Proxy       | Darf den Backend-Filter nicht umgehen                                         |
| Objektablage                             | Bucket und Ursprungszugang bleiben privat; keine öffentlichen Rohobjekt-Links |

Die öffentliche API wird nicht abhängig von der Session in einen privaten Lesepfad verwandelt. Geschützte DTOs geben private Vorschau-URLs nur bei ausreichendem Review-Recht zurück. Auch Ersteller erhalten keine öffentliche URL vor Freigabe.

Private Antworten: `Cache-Control: private, no-store`, kein Shared Cache, keine öffentlich cachebaren Weiterleitungen zu Prüfinhalten. Authentifizierte API-Endpunkte verwenden die vorhandene eingeschränkte Frontend-CORS-Policy. Mutationen prüfen den konfigurierten Frontend-Origin; CORS allein ersetzt keinen Schutz gegen fremde Formular-POSTs.

Der konkrete Origin-/CSRF-Schutz für neue geschützte Mutationen muss serverseitig umgesetzt werden; aus der vorhandenen CORS-Konfiguration allein lässt sich kein solcher Schutz ableiten. SvelteKit-Serveraktionen dürfen nur an den fest konfigurierten Backend-Origin weiterleiten und müssen ihren eigenen Origin-Schutz beibehalten.

### 6.2 Bestehendes anonymes Formular

Die URL-/Dateiübermittlung bleibt anonym erlaubt. Öffentliche technische Status-/SSE-Antworten dürfen wie bisher nur den Verarbeitungsfortschritt einer bekannten ID enthalten: kein Titel, keine Quell-URL, keine Moderationsentscheidung, kein Grund, kein Autor und keine privaten Inhalts-URLs. Dies ist eine begrenzte technische Empfangsbestätigung, kein Zugriff auf den Übermittlungsbereich.

`COMPLETED` bedeutet künftig ausschließlich `Capture complete`; die Oberfläche darf daraus keine Publikation ableiten. Das Formular zeigt nach Abschluss `Capture complete. Publication requires approval.` und bei berechtigt gewählter Direktpublikation `Publication will proceed if your permission is still active.` Ein öffentlicher Viewer-Link erscheint erst, wenn der öffentliche Detailendpunkt tatsächlich einen genehmigten Snapshot liefert.

Der bisherige Metadatenabruf aus `archive-submission.ts`/`loadMetadata` über den öffentlichen Detailendpunkt wird angepasst: während der Einreichung stammen Titel und Quelle aus dem lokalen Entwurf; nach Wiederaufruf erhalten nur angemeldete Eigentümer eigene Metadaten über den privaten Eigentümerendpunkt. Anonyme Besucher sehen bei Wiederaufruf nur die technische Bestätigung. Die Liste bleibt vollständig geschützt.

Der bestehende technische Cancel-Workflow endet weiterhin vor dem Speichern eines fertigen Ergebnisses. Er wird nicht zum Zurückziehen von `PENDING`- oder `APPROVED`-Snapshots erweitert. Eine zusätzliche anonyme Eigentums- oder Recovery-Token-Infrastruktur gehört nicht zu diesem Auftrag.

## 7. Geplante API-Verträge

Alle folgenden Pfade und DTO-Namen sind **Vorschläge**, keine bereits vorhandenen Verträge. GUIDs und vorhandene JSON-/ProblemDetails-Konventionen werden beibehalten.

### 7.1 Eigene Übermittlungen

| Methode / Pfad                             | Schutz                                 | Inhalt                                                                        |
| ------------------------------------------ | -------------------------------------- | ----------------------------------------------------------------------------- |
| `GET /api/user/submissions`                | Anmeldung + vollständige Registrierung | Ausschließlich `CreatedByUserId == currentUser.Id`, paginiert                 |
| `GET /api/user/submissions/{id}`           | Derselbe Eigentümer                    | Eigene Metadaten, Verarbeitung, Moderation, Version, Runde, erlaubte Aktionen |
| `GET /api/user/submissions/{id}/history`   | Derselbe Eigentümer                    | Stabile, paginierte Historie                                                  |
| `POST /api/user/submissions/{id}/resubmit` | Derselbe Eigentümer                    | `{ expectedVersion }`; nur aus REJECTED und bei fertigem Ergebnis             |

Die Benutzer-ID stammt ausschließlich aus der Session. Kein frei wählbarer `userId`-Parameter. Fremde IDs ergeben `404`; ein Admin erhält über diese Pfade ebenfalls nur eigene Daten.

`SubmissionResponse` enthält `id`, `title`, `sourceUrl`, `sourceType`, `createdAt`, `lastSubmittedAt`, `processingStatus`, `moderationStatus`, `submissionNumber`, `version`, `latestRejectionReason`, `canResubmit` und ein nur bei Freigabe gesetztes `publicSnapshotUrl`. Keine privaten Inhalts-URLs für normale Benutzer.

Listenfilter: Text, Moderationsstatus; Verarbeitung als gesondertes Feld/optional eigener Filter. Standard `lastSubmittedAt desc, id desc`, Seitengröße 20, maximal 100. Ein lesbarer Leerzustand ersetzt keine fehlgeschlagene Anfrage.

### 7.2 Moderation

| Methode / Pfad                                      | Schutz             | Inhalt                                                                          |
| --------------------------------------------------- | ------------------ | ------------------------------------------------------------------------------- |
| `GET /api/moderation/snapshots`                     | `snapshots.review` | Alle prüfbaren Übermittlungen, einschließlich anonymer; Statusfilter            |
| `GET /api/moderation/snapshots/{id}`                | `snapshots.review` | Metadaten, technische Qualität, Version, Runde und erlaubte Aktionen            |
| `GET /api/moderation/snapshots/{id}/history`        | `snapshots.review` | Vollständige Entscheidungsfolge, paginiert                                      |
| `GET /api/moderation/snapshots/{id}/content/{path}` | `snapshots.review` | Geschützte gespeicherte Ressourcen mit bestehender CSP/Sandbox/Range-Behandlung |
| `GET /api/moderation/snapshots/{id}/screenshot`     | `snapshots.review` | Geschütztes Bild                                                                |
| `POST /api/moderation/snapshots/{id}/approve`       | `snapshots.review` | `{ expectedVersion }`                                                           |
| `POST /api/moderation/snapshots/{id}/reject`        | `snapshots.review` | `{ expectedVersion, reason? }`                                                  |

Standardreihenfolge der Queue: `lastSubmittedAt asc, id asc`, damit alte und erneut eingereichte Übermittlungen nachvollziehbar abgearbeitet werden. Bereits entschiedene Einträge sind über Filter lesbar. Die eigentliche Änderung erfolgt nur über die beiden klaren Commands; kein universelles `PATCH status`.

Private HTML-Vorschau und Ressourcen laufen über explizite gleichursprüngliche SvelteKit-Proxy-Endpunkte unter `/api/moderation/snapshots/...`, die die Session ausschließlich an den konfigurierten Backend-Origin weiterreichen. Alle Ressourcen im gespeicherten HTML/CSS müssen den privaten Prefix verwenden; die im Worker gespeicherten Verweise dürfen nicht auf öffentliche Content-Pfade zurückfallen. Bestehende absolut gespeicherte öffentliche Content-URLs müssen im privaten Auslieferungspfad auf den privaten Prefix abgebildet werden, einschließlich CSS-URLs und eingebetteter Frames. Dies benötigt eine gezielte Prüfung der Normalisierung und gespeicherter Beispiele, keinen frei konfigurierbaren Netzwerkproxy. Wenn eine Asset-Art nicht sicher abgebildet werden kann, wird sie weggelassen; es wird kein öffentlicher Zugang geöffnet.

Die Sandbox blockiert weiterhin Skripte und Formularaktionen. Die Vorschau erhält dieselbe restriktive CSP wie öffentliche Archivseiten. Für Fonts im opaque-origin iframe darf der private Ressourcenproxy die notwendige Asset-CORS-Antwort liefern, aber erst nach eigener Session- und Rechteprüfung. Direkte Backend- und Proxy-Requests müssen ohne Berechtigung scheitern.

### 7.3 Rollen und individuelle Rechte

| Methode / Pfad                                                       | Schutz                                                | Inhalt                                                                    |
| -------------------------------------------------------------------- | ----------------------------------------------------- | ------------------------------------------------------------------------- |
| `GET /api/admin/users`                                               | Admin + mindestens eines der beiden Verwaltungsrechte | Suche nach Handle/Anzeigename; paginierte minimale Benutzerdaten          |
| `GET /api/admin/users/{id}/access`                                   | Wie oben                                              | Rollen, Ausnahmen, effektive Rechte, AccessVersion, bearbeitbare Bereiche |
| `GET /api/admin/permissions`                                         | Wie oben                                              | Fester Katalog, UI-Namen und Rollen-Defaults                              |
| `PUT /api/admin/users/{id}/roles/admin`                              | Admin + `users.assign_roles`                          | `{ assigned, expectedVersion }`                                           |
| `PUT /api/admin/users/{id}/permissions/{key}`                        | Admin + `users.assign_permissions`                    | `{ effect: Allow                                                          | Deny, expectedVersion }` |
| `DELETE /api/admin/users/{id}/permissions/{key}?expectedVersion=...` | Admin + `users.assign_permissions`                    | Ausnahme entfernen, wieder erben                                          |

Kein Bearbeiten von User-Grundrolle, unbekannten Rollen oder unbekannten Schlüsseln. Nutzer mit nur einem Verwaltungsrecht sehen den jeweils anderen Bereich lesend. Die UI speichert Rollen und persönliche Ausnahmen über getrennte, klar bezeichnete Aktionen; jede Antwort liefert den neuen Access-Stand. Änderungen an mehreren Ausnahmen werden im Mockup einzeln gespeichert, damit Fehler nicht als vollständig gespeicherter Satz erscheinen.

`AuthenticationSessionResponse` wird um Rollen und effektiv erlaubte Berechtigungsschlüssel ergänzt. Neue DTOs werden über Mapper aus Core-Modellen erstellt; Persistenzentitäten werden nicht als API-Antworten serialisiert. History-DTOs verwenden einen dokumentierten Discriminator und konkrete Varianten entsprechend der abstrakten Entität.

### 7.4 Fehler und Nebenläufigkeit

- `401`: keine gültige Session.
- `403`: fehlendes Recht oder notwendige Admin-Rolle; unvollständige Registrierung mit konkretem Hinweis.
- `404`: fremder eigener Snapshot, fehlende Ressource oder nicht öffentlicher Snapshot über öffentliche Pfade.
- `409`: veraltete Version, unzulässiger Übergang oder Schutz des letzten voll handlungsfähigen Admins.
- `400`: ungültiger Filter, unbekannter Rechtewert oder zu lange Begründung.
- `503`: erforderliche Datenquelle vorübergehend nicht verfügbar.

Mutationen sperren die betroffene Snapshot-Zeile und vergleichen `expectedVersion`. Keine verlorenen Entscheidungen: Bei zwei parallelen Genehmigungen/Ablehnungen gewinnt genau eine. Der andere Client lädt nach `409` den aktuellen Stand. Nur eine erfolgreiche Mutation erzeugt Historie.

Der Worker verwendet bei Zugriff auf beide Tabellen weiterhin die vorhandene Sperrreihenfolge Snapshot → Processing. Wiederholte Fertigstellung darf keine zweite Freigabe und keine doppelten Historieneinträge erzeugen. Ein terminaler Verarbeitungsstatus ist nach Cleanup aus dem Snapshot lesbar.

Die bisherigen internen Bezeichnungen `PublishAsync`, `SnapshotPublication` und `ProcessingCompletion.Published` meinen heute überwiegend das gespeicherte Capture-Ergebnis. Bei der Umsetzung werden ihre Namen/Kommentare und alle Aufrufer gezielt auf diese Bedeutung abgestimmt, beispielsweise `CompleteCaptureAsync` und `ResultStored`. Insbesondere darf die Worker-Fehlerbereinigung fertig gespeicherte, aber noch nicht genehmigte Ergebnisobjekte nicht löschen. Bestehende API-Beschreibungen wie `Quality: null until publication` werden auf technische Fertigstellung korrigiert; die öffentlichen Ressourcen-URLs bleiben von der tatsächlichen Freigabe abhängig.

## 8. Frontend-Routen und Mockups

Es sind genau **drei neue Frontend-Seiten** vorgesehen:

| Route              | Titel           | Zugang                                           | HTML-Mockup                                          |
| ------------------ | --------------- | ------------------------------------------------ | ---------------------------------------------------- |
| `/submissions`     | My submissions  | Angemeldeter, vollständig registrierter Benutzer | [submissions.html](mockups/submissions.html)         |
| `/admin/snapshots` | Snapshot review | Effektives `snapshots.review`                    | [snapshot-review.html](mockups/snapshot-review.html) |
| `/admin/users`     | User access     | Admin + mindestens ein Verwaltungsrecht          | [user-access.html](mockups/user-access.html)         |

Die Seiten werden im bestehenden `(site)`-Layout angelegt. Details/Historie stehen aufklappbar innerhalb der Liste; Prüfvorschau und Benutzereditor liegen auf derselben Seite. Keine weiteren Detailseiten, Dashboard-Landingpage oder zusätzlichen Routen. Filter und Auswahl können über Queryparameter erhalten bleiben.

### 8.1 My submissions

- Nur eigene Einträge, mit Titel, vollständiger kopierbarer Quell-URL, Einreichungszeit, technischem Status, Moderation und Runde.
- Filter `All`, `Pending`, `Approved`, `Rejected`, `Resubmitted`.
- Aufklappbare `History` mit Zeitpunkt, Aktion und optionalem Ablehnungsgrund.
- `View snapshot` nur bei öffentlicher Freigabe.
- `Resubmit for review` nur bei abgelehntem, fertig verarbeitetem eigenen Snapshot; Bestätigung nennt die erneute Prüfung desselben unveränderten Inhalts.
- Bei Wiedervorlage aktualisieren sich Status, Runde, Zeitpunkt und Historie. Der bisherige Ablehnungsgrund bleibt als historische Entscheidung sichtbar, wird nicht als aktuelle Ablehnung der neuen Runde dargestellt.
- Kein Besitzanspruch auf anonyme Einreichungen nach späterer Anmeldung.

### 8.2 Snapshot review

- Registerliste mit `Pending`, `Resubmitted`, `Rejected`, `Approved` und Auswahl eines Eintrags.
- Bei Auswahl Metadaten, Qualität, geschützte Vorschau und Entscheidungsfolge.
- `Approve` als primäre Aktion; `Reject` öffnet einen begrenzten Bereich mit `Reason (optional)` und eindeutiger Bestätigung.
- Abgelehnte Einträge erläutern `Only the submitter can resubmit this snapshot.`; keine direkte Freigabeaktion.
- Beim Abschluss wandert der Eintrag aus der offenen Queue; Erfolgsmeldung und Fokus folgen dem Ergebnis.
- Selbstprüfung ist im Vorschlag erlaubt, weil kein Vier-Augen-Prinzip beauftragt wurde. Sie wird wie jede andere manuelle Entscheidung protokolliert.

### 8.3 User access

- Benutzerliste mit Suche; Auswahl öffnet Rollen und Berechtigungen im selben Dokument.
- `User` als Grundrolle und `Admin` als zuweisbare Rolle.
- Jede Berechtigung zeigt Default, Auswahl `Inherit / Allow / Deny`, wirksamen Zugang und eine eigene `Save`-Aktion.
- `Inherit` entfernt einen persönlichen Eintrag. Entzug einer ererbten Berechtigung erfolgt über `Deny`; das Entfernen eines Allow alleine reicht dafür nicht.
- Änderungen sind erst nach explizitem Speichern wirksam; bei Fehlern bleiben Eingaben und Serverstatus unterscheidbar.
- Schutz vor dem Entzug des letzten voll handlungsfähigen Admins erscheint als konkrete Validierungsnachricht.

### 8.4 Vorhandene Seiten

`/archive/new` erhält die oben beschriebene Direktpublikationsoption und korrekte Abschlussmeldungen. Navigation/Account-Menü verlinken `My submissions`, `Snapshot review` und `User access` ausschließlich bei passenden Voraussetzungen. `/snapshots/{id}` bleibt ein öffentlicher Viewer. Bestehende Login-/Registrierungsweiterleitungen werden wiederverwendet; kein neuer Login-Screen.

Die Server-Loads prüfen Zugang und Eigentümerschaft; das Verstecken von Links ist kein Zugriffsschutz. API-Fehler werden auch nach dem initialen Laden behandelt, insbesondere nach einem Rechteentzug.

### 8.5 Gestaltungs- und Zustandsvorgaben

Die Mockups übernehmen Palette, Schriftfamilien, Markenform, flachen Header und Registerstruktur des produktiven Designs. `Newsreader` für Überschriften, `Source Sans 3` für Bedienung, `IBM Plex Mono` für URLs und Zeiten. Keine KPI-Kacheln oder dekorativen Effekte.

- Light/Dark mit semantisch gleichwertigen Tokens; archivierte Originalinhalte bleiben unverändert.
- Status mit Symbol und Klartext; aktive Navigation zusätzlich mit Linie/Gewicht.
- Sichtbare Labels, Fokus mit 3px-Outline, mindestens 44px hohe Hauptbedienelemente, URLs umbrechen und kopierbar halten.
- Desktop: strukturierte Zeilen; mobil: dieselben Datensätze untereinander, keine horizontal abgeschnittene Tabelle.
- Zustände: initiales Laden, leere Liste, kein Filtertreffer, Servicefehler mit Retry, fehlende Berechtigung, Konflikt bei Mutation, Mutation läuft, gespeichert.
- UI-Sprache durchgehend Englisch. Keine unnötigen technischen Hinweise in produktiven Nutzerflüssen.

Die HTML-Dateien sind isolierte Designprototypen mit Beispieldaten. Ihre Buttons ändern ausschließlich lokalen Demo-Zustand. Es existiert keine Backend-Verbindung und keine produktive Svelte-Route. [preview.html](mockups/preview.html) ist eine reine Prüfoberfläche für feste iframe-Breiten, **keine geplante Frontend-Route**. Gemeinsam genutzte CSS-/JS-Dateien gehören nur zu den Mockups.

## 9. Migration, Initialisierung und Auslieferung

### 9.1 Geordnete Migration

1. Neue Rollen-/Override-Tabellen, Historie und Snapshot-Felder ergänzen; Constraints und Indizes anlegen.
2. Bestehenden Konten `User` zuordnen. Niemand wird allein durch Reihenfolge, Namen oder erste Anmeldung Admin.
3. Bisher öffentlich verfügbare fertige Snapshots mit Ergebnisdaten auf `APPROVED` setzen. `ProcessingOutcome = COMPLETED`; `ApprovedAt` ist der dokumentierte Übernahmezeitpunkt, nicht ein erfundener historischer Prüfzeitpunkt.
4. Pro Bestandssnapshot einen System-Übernahmeeintrag mit `LegacyMigration` anlegen. Wo ein initialer Submitted-Eintrag aus `CreatedAt` rekonstruiert wird, die Rekonstruktion ausdrücklich kennzeichnen; keine vermeintliche menschliche Altentscheidung erfinden. Einfacher Vorschlag: nur den belegten Übernahmeeintrag schreiben, Runde 1.
5. Nicht fertige vorhandene Einreichungen auf `PENDING` setzen. Noch vorhandene terminale Processing-Ergebnisse übernehmen. Bereits vom alten Cleanup gelöschte Übermittlungen können nicht rekonstruiert werden.
6. Vor Aktivierung die neue Worker-/Cleanup-Semantik zusammen mit der API ausrollen. Während Migration und Wechsel alte API/Worker anhalten; kein gemischter Betrieb, der private Datensätze öffentlich ausliefert oder erfolglose Übermittlungen weiter löscht.

Migrationsschritte werden auf einer Kopie mit anonymen, registrierten, fertigen, fehlgeschlagenen und laufenden Datensätzen überprüft. Neue Enum-Werte/-Mappings in `Injection` und `DataContextFactory` konsistent halten. Bestehende Migrationen und die Migrationshistorienkonfiguration werden nicht umgeschrieben.

### 9.2 Erster Admin und Handlungsfähigkeit

Vor Inbetriebnahme wird ein vorhandener, vollständig registrierter Benutzer anhand seiner **lokalen UUID** bewusst als erster Admin benannt. Ein eng begrenzter einmaliger Initialisierungsschritt fügt die Rolle nur hinzu, wenn noch kein Admin existiert, und wird danach entfernt/deaktiviert. Keine öffentliche Bootstrap-Route und keine Berechtigung über einen frei eingegebenen Handle. Die konkrete UUID wird erst zur Umsetzung/Auslieferung benötigt und nicht in diesen Plan geschrieben.

Rollen-/Override-Mutationen prüfen innerhalb einer serialisierten Transaktion, dass mindestens ein vollständig registrierter Admin beide Benutzerverwaltungsrechte effektiv behält. Dafür genügt eine kleine globale Transaktionssperre für Zugriffsänderungen, die auch die finale Direktpublikationsprüfung koordiniert; manuelle Snapshot-Moderation benötigt sie nicht. So verhindern auch gleichzeitige Demotionen bzw. Deny-Änderungen einen vollständigen administrativen Ausschluss. Dies betrifft auch Selbständerungen.

### 9.3 Cleanup und Rollback

Cleanup entfernt weiterhin abgelaufene Processing-Zeilen und temporäre Uploadobjekte. Vorher wird ein fehlendes terminales Ergebnis dauerhaft gesichert. Snapshot-Metadaten, Historie und fertig gespeicherte Inhalte bleiben auch nach Ablehnung erhalten. Wartende oder laufende Vorgänge werden nicht bereinigt. Speicherbedarf abgelehnter Inhalte ist bewusst Teil der geforderten späteren Genehmigbarkeit; automatische Löschung ist hier nicht vorgesehen.

Ein Code-Rollback auf die alte API wäre nach Einführung der Moderation unsicher, weil sie `Quality != null` wieder als öffentliche Freigabe behandeln würde. Bei Rücknahme zuerst öffentliche Auslieferung sperren und Datenbankstand/Backup abgestimmt wiederherstellen; kein blindes Downgrade bei weiter öffentlich erreichbarem alten Code. Datenmigration und Sicherung sind Teil der späteren Auslieferung, nicht dieses Planungscommits.

## 10. Umsetzungspakete nach Freigabe

| Reihenfolge | Paket                  | Konkretes Ergebnis                                                                                      |
| ----------- | ---------------------- | ------------------------------------------------------------------------------------------------------- |
| 1           | Domain und Migration   | Fester Rollen-/Rechtekatalog, Overrides, Moderationsfelder, abstrakte Historie, kontrollierter Backfill |
| 2           | Rechteauflösung        | Resolver, API-Policies, aktuelle Session-Rechte, erster Admin und Schutz letzter Admin                  |
| 3           | Speicherung und Worker | Atomare Übergänge/Historie, dauerhaftes Processing-Ergebnis, Direktpublikation, sicherer Cleanup        |
| 4           | Öffentliche Lesewege   | Gemeinsame IsPublic-Regel auf Suche, Zahlen, Details, Content und Screenshot                            |
| 5           | Private API            | Eigentümerliste, Wiedervorlage, Review-Commands/-Vorschau, Benutzerverwaltung und Konflikte             |
| 6           | OpenAPI/Frontend-Typen | Backend starten, Live-Spezifikation prüfen, Generator ausführen und Änderungen sichten                  |
| 7           | Frontend               | Drei Seiten auf bestehenden UI-Mustern; private Vorschau; bestehendes Formular/Navigation anpassen      |
| 8           | Verifikation           | Fachliche Integrationschecks, Browserprüfung, Migrationsprobe und dokumentierte Abnahme                 |

Bestehende Repository-Result-Modelle, Mapper und partielle Repositories fortführen. Zusätzliche Dateien nur dort, wo sie klare fachliche Verantwortung abbilden. Kein generisches Repository, MediatR, CQRS-Framework oder neuer Eventbus.

Neuer C#-Code erhält aussagekräftige `///`-Dokumentation für Verträge und fachliche Regeln. `//`/`/* ... */` erläutern insbesondere Deny-Priorität, Transaktionsgrenzen, Sperrreihenfolge, Freigabe und Cleanup. Im Frontend werden Zustandsübergänge und nicht offensichtliche Accessibility-/Proxy-Details kommentiert. Kommentare erklären Zweck und Invarianten, nicht jede triviale Zuweisung.

### Verbindlicher Generatorablauf bei späteren Vertragsänderungen

Aus dem Backend-Verzeichnis die API mit der vorhandenen Development-Konfiguration starten, beispielsweise `dotnet run --project Archive.Api`, sodass `http://localhost:5200/openapi/v1.json` verfügbar ist. Anschließend im Frontend-Verzeichnis:

```sh
bun --bun run generate:api
bun --bun run check
bun --bun run build
```

`@hey-api/openapi-ts` und `openapi-ts.config.ts` behalten ihren bisherigen Zugriff auf die laufende OpenAPI-URL. Keine Dateispezifikation als Ersatz, keine Fetch-Umgehung und keine manuellen Änderungen an generierten Typen. Bestehende Transformer nur fachlich ergänzen, wenn die neuen Varianten es benötigen.

In diesem Planungsstand werden keine API-Verträge verändert. Deshalb werden weder Backend noch Generator für Vertragsgenerierung gestartet.

## 11. Abnahmekriterien und spätere Prüfungen

| Nr. | Szenario                                                  | Erwartetes Ergebnis                                                                 |
| --- | --------------------------------------------------------- | ----------------------------------------------------------------------------------- |
| A1  | Gast übermittelt URL oder Datei                           | Angenommen, technisch verarbeitet, ohne Freigabe nicht öffentlich                   |
| A2  | Gast ruft `/submissions` oder private API auf             | Anmeldung erforderlich; keine Liste/Gründe/Inhalte                                  |
| A3  | Benutzer A ruft eigene und fremde IDs auf                 | Nur eigene Liste/Details/Historie; fremde IDs liefern 404                           |
| A4  | Anonyme Einreichung, danach Anmeldung                     | Keine nachträgliche Zuordnung und keine Wiedervorlage dieses Eintrags               |
| A5  | Fertiger PENDING-Snapshot                                 | Fehlt in Suche, Summen, Verteilung, Details, Screenshot, Content und Frontend-Proxy |
| A6  | Admin genehmigt                                           | APPROVED plus Historie; alle öffentlichen Pfade liefern das fertige Ergebnis        |
| A7  | Admin lehnt mit/ohne Grund ab                             | REJECTED; Grund optional; Eigentümer sieht die Entscheidung                         |
| A8  | Eigentümer legt abgelehnten Snapshot erneut vor           | RESUBMITTED, gleiche ID/Inhalte, nächste Runde, frühere Ablehnung bleibt            |
| A9  | Prüfer versucht direkte Freigabe eines REJECTED-Snapshots | 409; kein neuer Historieneintrag                                                    |
| A10 | Fremder Benutzer/Prüfer versucht Wiedervorlage            | Nicht erlaubt, auch mit Review-Recht                                                |
| A11 | RESUBMITTED wird genehmigt/erneut abgelehnt               | Neue Entscheidung gilt; frühere Runden bleiben nachvollziehbar                      |
| A12 | User mit Review-Allow / Admin mit Review-Deny             | Erster darf prüfen; zweiter nicht, auch bei direktem API-Aufruf                     |
| A13 | Allow/Deny wird entfernt                                  | Rollenregel gilt sofort wieder, ohne Neuanmeldung                                   |
| A14 | Berechtigter wählt Direktpublikation                      | Nach erfolgreichem Capture APPROVED mit PermissionBypass-Historie                   |
| A15 | Direktrecht wird vor Capture-Ende entzogen                | Technisch fertig, PENDING und privat                                                |
| A16 | Unberechtigter sendet Direktflag manuell                  | 403, keine autorisierte Direktpublikation                                           |
| A17 | Zwei Entscheidungen oder zwei Wiedervorlagen parallel     | Genau eine Änderung, andere 409, keine doppelte Historie                            |
| A18 | Cleanup nach Erfolg/Fehler/Ablehnung                      | Flüchtige Daten weg, eigene Übermittlung/Historie/Outcome weiter verfügbar          |
| A19 | Rechte werden bei aktiver Session entzogen                | Nächster geschützter Request und neue Vorschauressourcen scheitern                  |
| A20 | Letzte Admin-Rechte parallel entziehen                    | Mindestens ein administrativ handlungsfähiger Admin bleibt                          |
| A21 | Bestandsmigration                                         | Zuvor öffentliche fertige Snapshots genehmigt, Systemherkunft erkennbar             |
| A22 | Prüfvorschau direkt ohne Recht oder über Asset-URL        | Zugriff verweigert, auch ohne Navigation und auch über den Proxy                    |
| A23 | Neues Konto oder Entzug von Admin                         | User-Grundrolle bleibt erhalten                                                     |
| A24 | Capturing/Fehler ohne Ergebnis wird genehmigt             | 409; keine öffentliche Freigabe                                                     |

Diese Regeln benötigen gezielte Backend-/Datenbank-Integrationstests, insbesondere Eigentümerfilter, öffentliche Queryfilter, Deny-Priorität und konkurrierende Transaktionen. Dafür PostgreSQL mit realem EF-Mapping nutzen; ein In-Memory-Provider beweist weder Sperren noch SQL-Verhalten. Neue Tests werden erst im Implementierungsschritt ergänzt, im vorhandenen Projektstil oder einem kleinen passenden Testprojekt nach Sichtung der dann vorhandenen Infrastruktur.

Frontend prüfen mit `check` und `build`, danach im integrierten Browser: Desktop, 768px, 390px und 320px, Light/Dark, 200% Textvergrößerung, Tastaturfokus, lange URLs, Filter-/Leer-/Fehlerzustände sowie konkrete Freigabe-, Ablehnungs-, Wiedervorlage- und Rechteänderungsabläufe. Archived content darf durch Dark Mode nicht verfärbt werden.

## 12. Verifikation dieses Planungsstands

- `git fetch origin` erfolgreich; lokaler `master` und `origin/master` identisch, jeweils `0abec2f`. Kein Pull erforderlich.
- Separater Branch direkt von `master` erstellt.
- Umfang ausschließlich dieses Dokument und zugehörige isolierte Mockups; keine produktiven Quellen, Verträge, Migrationen oder Konfigurationen geändert.
- Browserprüfungen und verbleibende Einschränkungen werden nach Sichtung der HTML-Prototypen im zugehörigen [Prüfprotokoll](verification.md) festgehalten.
- Die Freigabe dieses Plans ist Voraussetzung für den Beginn der Implementierung. Die fachlichen Entscheidungen aus Abschnitt 1 sind durch das Interview bereits geklärt; eine erneute Nachfrage dazu ist nicht vorgesehen.
