# Plan: dauerhafte Kompatibilität von Snapshot-Manifesten

## Status und Freigabegrenze

Der Lösungsvorschlag wurde nach dem Interview ausdrücklich genehmigt. Die Umsetzung erfolgt auf der eigenständigen
Branch `codex/snapshot-manifest-compatibility`, die vor Implementierungsbeginn vom mit `origin/master` abgeglichenen
Commit `35666c7` erstellt wurde. Laufzeitkompatibilität und Migrationswerkzeug bleiben auf den hier beschriebenen Umfang
begrenzt; Produktionsdaten werden durch Build, Tests oder Deployment nicht automatisch verändert.

## Ziel

Ein Update der geschriebenen Manifestversion darf bereits gespeicherte, bekannte Manifestversionen nicht mehr pauschal unlesbar machen. Das System soll:

- neue Snapshots weiterhin ausschließlich im aktuellen Format (derzeit Version 4) schreiben;
- bekannte Manifestversionen 1 bis 4 unverändert aus dem Objektspeicher lesen können;
- jede bekannte Version explizit validieren und in ein gemeinsames internes Laufzeitmodell überführen;
- mit einem separaten C#-Konsolenprogramm alle aktiven älteren Generationen kontrolliert auf den aktuellen Manifestvertrag migrieren können;
- Migration, Wiederherstellung und endgültige Bereinigung über ein Journal und immutable Generationen sicher voneinander trennen;
- unbekannte, beschädigte oder sicherheitswidrige Manifeste weiterhin geschlossen ablehnen;
- die Kompatibilität jeder unterstützten Version mit dauerhaften Fixture- und Auslieferungstests absichern;
- keine öffentliche API, Datenbankmigration oder neue Benutzeroberfläche benötigen.

## Bestandsaufnahme

### Aktueller Auslieferungspfad

`SnapshotRepository.OpenContentAsync` liest `${ContentPrefix}/manifest.json`, deserialisiert unmittelbar in `SnapshotManifest` und akzeptiert anschließend nur:

```csharp
manifest.Version == SnapshotManifest.CurrentVersion
```

`SnapshotManifest.CurrentVersion` ist aktuell `4`. Damit werden ältere Versionen unabhängig davon verworfen, ob ihre Inhalte sicher auf den heutigen Auslieferungsvertrag abgebildet werden könnten. Die Manifestklasse ist außerdem zugleich persistierter Schreibvertrag und Laufzeitmodell. Diese Kopplung macht einen Versionswechsel unnötig riskant.

Der Repository-Pfad benötigt für die Auslieferung im Wesentlichen:

- den sicheren relativen Pfad und den dazugehörigen Objektschlüssel;
- Inhaltstyp und Browserrolle (`root-document`, `nested-document`, `asset`);
- die ursprüngliche URL zur Laufzeitabbildung erfasster Ressourcen;
- Normalizer-Version und Diagnose für gegebenenfalls notwendige In-Memory-Normalisierung;
- bei neueren Manifesten den gespeicherten Hash als starken HTTP-Validator.

Provenienz- und Diagnosefelder werden weiterhin bewahrt, sind aber nicht alle unmittelbar für die Auswahl eines angeforderten Objekts erforderlich.

### Historische Verträge

Die Git-Historie und die frühere Migrationsimplementierung ergeben folgende Verträge:

| Version | Entstehung | Wesentliche Struktur | Laufzeitrelevante Einschränkung |
| --- | --- | --- | --- |
| 1 | URL-Capture | `htmlObjectKey`, `screenshotObjectKey`, Ressourcen mit `url`, `contentType`, `objectKey`; `resourceReferenceMode` nur in späteren Ausprägungen | keine Pfade, Rollen oder Ressourcen-Hashes; keine Interaktionsmetadaten |
| 2 | HTML-/MHTML-/WebArchive-Import | Version 1 plus Quellobjekt, Quelltyp, Original-URL, Quellgröße/-Hash und Missing-URL-Diagnostik | ebenfalls keine Rollen oder Ressourcen-Hashes; mehrere Ausprägungen innerhalb derselben Versionsnummer |
| 3 | erster interaktiver Manifestvertrag | `provenance`, `replay`, vollständige Ressourcen mit Pfad, Rolle und SHA-256 | semantisch nahe an Version 4; Metadaten heißen `replay` und enthalten zusätzlich eine optionale Replay-Version |
| 4 | aktueller Snapshotvertrag | `provenance`, `interaction`, vollständige Ressourcen mit Pfad, Rolle und SHA-256 | aktueller Schreibvertrag |

Beim Wechsel auf Version 4 existierte kurzzeitig `Archive.SnapshotMigrator`. Das Werkzeug erzeugte eine neue Generation, berechnete Rollen und Hashes, normalisierte ältere Dokumente, schaltete `Snapshot.ContentPrefix` atomar um und wurde anschließend wieder entfernt. Dieser einmalige operative Ablauf erklärt, weshalb die heutige Anwendung keine dauerhafte Lesekompatibilität enthält.

### Produktionsbeobachtung

Das öffentliche Register unter <https://archive.voldechse.wtf> zeigt zur Analysezeit drei Snapshots. Die Captures vom 26. September 2026 (`30edb1dc-ae9e-4b4c-b467-111ef56ab4fc`) und 28. September 2026 (`a445d1b0-dec8-428d-90ef-ed9196360982`) sowie der Capture vom 26. Oktober 2026 (`bb0b8522-96b8-4175-be47-3e9b2777bdc3`) lassen sich über ihre öffentlichen `contentUrl`s öffnen.

Die öffentliche API exponiert weder `manifest.json` noch dessen Versionsnummer. Deshalb kann über die Produktionsoberfläche allein nicht verifiziert werden, welche aktive Generation beziehungsweise Manifestversion hinter den älteren Captures liegt. Für exakte Regression-Fixtures werden anonymisierte Kopien der tatsächlich aktiven älteren Manifeste oder eine Versionszuordnung der Snapshot-IDs benötigt. Zugangsdaten zum Objektspeicher sollen dafür nicht in das Repository oder in Tests übernommen werden.

## Vorgeschlagene Architektur

### 1. Persistierten Vertrag und Laufzeitmodell trennen

`SnapshotManifest` bleibt der strikte aktuelle Schreibvertrag für Version 4. Seine Pflichtfelder werden nicht aufgeweicht und der Worker schreibt unverändert `SnapshotManifest.CurrentVersion`.

Neu entsteht ein internes Laufzeitmodell, beispielsweise `SnapshotContentManifest`. Es enthält nur die sicher validierten Informationen, die der Auslieferungspfad benötigt. Insbesondere unterscheidet es zwischen:

- einem vorhandenen, verifizierbaren SHA-256 aus Version 3/4;
- einem fehlenden starken Validator in Version 1/2;
- bereits aktuell normalisierten Dokumenten;
- Dokumenten, die vor der Antwort zwingend mit dem aktuellen `SnapshotDocumentNormalizer` normalisiert werden müssen.

Damit muss die Laufzeit nicht so tun, als besäßen alte Manifeste Felder, die damals nie gespeichert wurden.

### 2. Version vor der typisierten Deserialisierung erkennen

Ein neuer interner `SnapshotManifestReader` liest das JSON zunächst als `JsonDocument`, verlangt eine ganzzahlige `version` und delegiert danach ausschließlich an den Adapter dieser Version:

```text
manifest.json
    -> Versionsdiskriminator
        -> V1-Adapter
        -> V2-Adapter
        -> V3-Adapter
        -> V4-Adapter
    -> validiertes SnapshotContentManifest
    -> bestehende Ressourcenauflösung
```

Es gibt keinen optimistischen Fallback auf die neueste DTO-Klasse. Version `0`, negative Werte und Versionen größer als 4 werden mit einer versionsbezogenen Warnung abgelehnt. Das verhindert, dass ein zukünftiger Vertrag versehentlich teilweise als Version 4 interpretiert wird.

### 3. Versionsspezifische Abbildung

#### Version 4

- Deserialisierung in den bestehenden strikten Vertrag.
- Direkte Abbildung von `interaction`, `provenance` und Ressourcen.
- SHA-256 bleibt als starker Validator verfügbar.
- Das aktuelle Verhalten bleibt unverändert.

#### Version 3

- Eigene DTO für den historischen `replay`-Block.
- `replay.normalizer`, `containsExecutableContent` und `diagnostic` werden in die heutigen Interaktionsmetadaten überführt.
- Die optionale damalige Replay-Version wird nicht zur Ressourcenauswahl verwendet.
- Pfade, Rollen, Objektschlüssel und SHA-256 werden vollständig erhalten.
- Dokumente mit `archive-snapshot-v1` oder `archive-snapshot-v2` werden wie heute in-memory auf den aktuellen Normalizer gebracht; unbekannte Normalizer bleiben nicht ausführbar und werden abgelehnt.

#### Version 1 und 2

- Historische DTOs modellieren nur tatsächlich vorhandene Felder; `resourceReferenceMode` und nicht sicherheitskritische Diagnosefelder bleiben optional, um beide bekannten Ausprägungen derselben Versionsnummer zu lesen.
- `htmlObjectKey` muss exakt `${ContentPrefix}/index.html` sein und wird zum einzigen `root-document`.
- Für jede Manifestressource wird der relative Pfad nur dann aus `objectKey` abgeleitet, wenn der Schlüssel exakt unter dem aktiven `ContentPrefix` liegt und `SnapshotResourcePath.Normalize` ihn akzeptiert.
- Rollen werden konservativ über die etablierte `SnapshotResourceRoles.Infer`-Logik bestimmt.
- `screenshotObjectKey` und `sourceObjectKey` werden nicht zu browserlesbaren Ressourcen. Damit bleibt die heutige Trennung zwischen Vorschau, Quellmaterial und Manifest-Allowlist erhalten.
- Ressourcen ohne sicheren Pfad, mit doppeltem Pfad, leerem Inhaltstyp oder außerhalb der aktiven Generation führen zu einer geschlossenen Ablehnung des Manifests.
- Alte HTML-Dokumente gelten nie als bereits normalisiert. Vor der Auslieferung werden sie im Speicher mit dem aktuellen `SnapshotDocumentNormalizer`, der aus dem Manifest aufgebauten URL-zu-Pfad-Abbildung und dem jeweiligen Dokumentpfad normalisiert. Dadurch erhalten sie aktuellen Shim, Basis-Pfad und Sicherheitsbereinigung, ohne gespeicherte Bytes zu verändern.
- Da Version 1/2 keinen Ressourcen-Hash enthält, darf kein SHA-256 erfunden werden. Auf einem Stand mit Byte-Range/ETag-Unterstützung werden solche Ressourcen ohne starken `If-Range`-Validator vollständig ausgeliefert; eine Range-Optimierung bleibt Version 3/4 vorbehalten, sofern nicht später bewusst ein speicherseitiger Validator eingeführt wird.

### 4. Zentrale Sicherheitsvalidierung

Nach der versionsspezifischen Abbildung gelten gemeinsame Invarianten:

- genau ein `index.html` mit Rolle `root-document`;
- keine doppelten Pfade;
- nur sichere relative Pfade ohne leere Segmente, `.`/`..`, Backslashes oder Steuerzeichen;
- jeder browserlesbare Objektschlüssel entspricht exakt `${ContentPrefix}/${Path}`;
- nur bekannte Rollen;
- Dokumentrollen nur für HTML/XHTML; aktive Formate wie SVG/XML bleiben Assets und können nicht als Dokument navigiert werden;
- Quellobjekt, Screenshot und Manifest sind nicht Teil der Content-Allowlist;
- eine angeforderte Ressource muss weiterhin genau einmal im normalisierten Laufzeitmodell vorkommen und im Objektspeicher existieren.

Die bestehende Sichtbarkeits-, Moderations-, Capability-, CSP- und Snapshot-Origin-Logik wird nicht verändert.

### 5. Regeln für zukünftige Manifestversionen

Ein Versionssprung gilt erst als vollständig, wenn gleichzeitig vorhanden sind:

1. der neue persistierte Schreibvertrag;
2. ein expliziter Reader/Adapter in das Laufzeitmodell;
3. Regression-Fixtures und Auslieferungstests für alle weiterhin unterstützten Versionen;
4. eine aktualisierte Support-Matrix in der Dokumentation;
5. eine bewusste Entscheidung für jedes Feld, das nicht verlustfrei abbildbar ist.

Der Writer erzeugt immer nur die aktuelle Version. Reader bleiben additiv. Eine alte Version wird nicht nebenbei entfernt; eine spätere Abkündigung benötigt eine eigene begründete Aufgabe und einen überprüfbaren Migrationspfad.

### 6. Einmaliges Migrationswerkzeug

Zusätzlich zur Lesekompatibilität entsteht das eigenständige .NET-Konsolenprojekt `Archive.SnapshotMigrator`. Es verwendet die bestehende Core-Konfiguration für PostgreSQL und S3-kompatiblen Objektspeicher, ist aber weder Bestandteil des API- noch des Worker-Prozesses. Das Werkzeug kontaktiert niemals die ursprüngliche Website eines Snapshots.

Die dauerhafte Lesekompatibilität bleibt trotz Migration erforderlich:

- sie hält alte Generationen während Rollout und Restore lesbar;
- sie verhindert, dass ein unvollständig migrierter Bestand die Anwendung blockiert;
- sie etabliert das gewünschte Muster für künftige Manifestversionen;
- sie erlaubt die sichere Diagnose eines älteren Manifests, bevor persistente Änderungen erfolgen.

#### Kommandozeilenvertrag

Genau eine Aktion und genau ein Selektor sind erforderlich:

```text
Archive.SnapshotMigrator --dry-run (--snapshot <uuid> | --all) [--json]
Archive.SnapshotMigrator --migrate (--snapshot <uuid> | --all) [--json]
Archive.SnapshotMigrator --restore (--snapshot <uuid> | --all) [--json]
Archive.SnapshotMigrator --clean   (--snapshot <uuid> | --all) --confirm [--json]
```

- `--dry-run` liest und validiert Daten, schreibt aber weder Datenbank noch Objektspeicher.
- `--migrate` erzeugt und validiert eine neue Generation im aktuellen Manifestformat und aktiviert sie anschließend atomar.
- `--restore` reaktiviert die im Journal vermerkte ursprüngliche Generation, sofern deren vollständiger Objektbestand noch vorhanden ist.
- `--clean` löscht ausschließlich die inaktive Generation und danach das Journal. Die Aktion ist irreversibel und verlangt deshalb immer `--confirm`.
- `--json` liefert eine stabile maschinenlesbare Ausgabe für Runbooks und Deployment-Automatisierung; die normale Ausgabe bleibt knapp und menschenlesbar.
- `--help`/`-h` zeigt Hilfe ohne Verbindungsaufbau.

Unbekannte Argumente, mehrere Aktionen, mehrere Selektoren, eine ungültige UUID oder `--confirm` außerhalb von `--clean` werden vor dem Hostaufbau abgelehnt. Vorgesehene Exitcodes sind `0` für vollständigen Erfolg, `1` für fachlich blockierte/inkonsistente Snapshots und `2` für ungültige CLI- oder Startkonfiguration. Die Verarbeitung von `--all` erfolgt bewusst deterministisch und zunächst sequenziell; Parallelisierung wäre für ein einmaliges Betriebswerkzeug kein notwendiger Mehrwert.

#### Immutable Zielgeneration und Journal

Die Migration überschreibt keine bestehende Generation. Sie schreibt nach:

```text
snapshots/{snapshotId}/generations/manifest-v4/{migrationId}/...
```

Der reversible Zustand liegt getrennt unter:

```text
snapshot-migrations/manifest-v4/{snapshotId}.json
```

Das Journal enthält mindestens Snapshot- und Migrations-ID, Status (`prepared`, `applied`, `restored`), ursprünglichen und migrierten Prefix, beide Objektlisten, ursprüngliche und neue Größen-/Ressourcenmetadaten sowie Zeitpunkte. Zugangsdaten oder Manifestinhalte werden darin nicht dupliziert.

Der Ablauf von `--migrate` ist:

1. Datenbankzeile, aktiven Prefix, vorhandenes Journal und Manifest bewerten.
2. Objekte, Pfade, Größen- und Ressourcenlimits vollständig prüfen.
3. Ressourcen in eine eindeutige neue Generation kopieren beziehungsweise Dokumente und JavaScript mit den aktuellen Core-Normalizern neu erzeugen.
4. Rollen, Inhaltstypen und SHA-256 für jede browserlesbare Ressource berechnen.
5. Existiert kein eigenständiges Quellobjekt, das unveränderte bisherige `index.html` als `source.legacy.html` bewahren.
6. Manifest V4 schreiben und die vorbereitete Generation einschließlich Hashes erneut vollständig validieren.
7. Journal als `prepared` schreiben.
8. `Snapshot.ContentPrefix`, `StorageBytes` und `ResourceCount` über ein bedingtes Datenbankupdate nur dann umschalten, wenn der ursprüngliche Prefix unverändert ist.
9. Journal als `applied` markieren.

Scheitert die Vorbereitung vor dem Journal, werden ausschließlich Objekte unter dem neu erzeugten eindeutigen Migrationsprefix aufgeräumt. Scheitert oder konkurriert die Aktivierung nach `prepared`, bleibt die neue Generation inaktiv und das Journal macht den Zustand explizit; ein blindes Überschreiben findet nicht statt.

#### Zustandsmodell und Idempotenz

`--dry-run` und die anderen Aktionen verwenden dieselbe Bewertung:

| Zustand | Bedeutung |
| --- | --- |
| `Current` | Aktive Generation besitzt ein vollständig gültiges aktuelles Manifest. |
| `Migratable` | Eine aktuelle Generation kann sicher vorbereitet werden. |
| `Pending` | Eine vorbereitete Generation wurde nicht aktiviert; vor einem neuen Versuch ist eine bewusste Bereinigung erforderlich. |
| `Restored` | Die ursprüngliche Generation ist wieder aktiv; die migrierte Generation kann bereinigt werden. |
| `Blocked` | Pflichtobjekte fehlen oder Pfad-/Größen-/Ressourcenlimits werden verletzt. |
| `Inconsistent` | Datenbank, Manifest, Hashes, Objektbestand oder Journal widersprechen sich. |
| `Missing` | Der Snapshot besitzt keine abgeschlossene aktive Inhaltsgeneration. |

`--migrate` überspringt `Current` erfolgreich und ist damit idempotent. `--clean --all` darf erst beginnen, wenn jeder gespeicherte Snapshot als `Current` validiert wurde; dadurch kann die globale Bereinigung nicht versehentlich die letzte rücksetzbare Generation eines problematischen Bestands entfernen.

#### Manifestziel und Versionssemantik

Das empfohlene Migrationsziel bleibt Version 4, solange sich dessen JSON-Vertrag nicht ändert. Die Manifestversionsnummer bezeichnet den Schemacontract, nicht den Zeitpunkt einer Migration, eine Anwendungsversion oder einen operativen Release-Status. Eine reine Kennzeichnung als „release“ rechtfertigt daher keinen Sprung auf Version 5.

Die Herkunft der migrierten Generation ist bereits nachvollziehbar durch:

- den Generation-Prefix `generations/manifest-v4/...`;
- das Migrationsjournal;
- einen definierten `provenance.documentState` wie `legacy-migrated`;
- eine eindeutige Normalizer-Version im `interaction`-Block.

Version 5 wäre nur dann sachlich korrekt, wenn die Umsetzung einen neuen persistierten Vertrag einführt, beispielsweise ein verpflichtendes strukturiertes `producer`-/`formatRelease`-Objekt. Ein solches Feld würde anschließend ebenfalls einen V5-Reader, V5-Fixtures und eine explizite V4-zu-V5-Abbildung erfordern. Ohne konkrete fachliche Nutzung wird dieses zusätzliche Schemaelement nicht empfohlen.

## Betroffene Komponenten

Voraussichtliche, aufgabenbezogene Änderungen:

- `backend/Archive.Core/Storage/SnapshotManifest.cs`
  - aktueller V4-Schreibvertrag bleibt bestehen;
  - allenfalls gemeinsame Versionskonstanten, ohne Pflichtfelder zu lockern.
- `backend/Archive.Core/Storage/SnapshotManifestReader.cs` (neu)
  - Versionsdiskriminierung, DTOs/Adapter und gemeinsame Validierung.
- `backend/Archive.Core/Storage/SnapshotContentManifest.cs` (neu oder eng beim Reader)
  - internes, versionsneutrales Laufzeitmodell.
- `backend/Archive.Core/Repositories/SnapshotRepository/SnapshotRepository.cs`
  - direkte V4-Deserialisierung durch den Reader ersetzen;
  - bestehende Ressourcenauflösung auf das Laufzeitmodell umstellen.
- `backend/Archive.Core/Properties/AssemblyInfo.cs` (nur falls nötig)
  - gezielte `InternalsVisibleTo("Archive.Tests")`-Freigabe, damit interne Parserlogik ohne öffentliche Produkt-API getestet werden kann.
- `backend/Archive.SnapshotMigrator/Archive.SnapshotMigrator.csproj` (neu)
  - schlankes `net10.0`-Konsolenprojekt mit Referenz auf `Archive.Core`, ohne zusätzliche CLI-Abhängigkeit.
- `backend/Archive.SnapshotMigrator/Program.cs` und `SnapshotMigratorOptions.cs` (neu)
  - Hostaufbau, Cancellation über `Ctrl+C`, strikte Argumentvalidierung und Exitcodes.
- `backend/Archive.SnapshotMigrator/SnapshotMigrationService.cs`, `SnapshotMigrationModels.cs` und `SnapshotMigrationNormalizer.cs` (neu)
  - Bewertung, Migration, Restore, Clean, Journal und Wiederverwendung der aktuellen Normalisierungslogik.
- `backend/Archive.SnapshotMigrator/Dockerfile` (neu)
  - mehrstufiges .NET-10-Image, `Release`-Publish ohne AppHost und nicht privilegierter Runtime-Benutzer.
- `backend/Archive.slnx`
  - Migrator-Projekt in die bestehende Solution aufnehmen.
- `deploy/production/snapshot-migrator.compose.yml` (neu)
  - ausschließlich der One-shot-Service; Nutzung des bestehenden externen `archive`-Netzwerks sowie derselben PostgreSQL-/Objektspeicher-Konfiguration wie API und Worker.
- `backend/Archive.Tests/Fixtures/SnapshotManifests/` (neu)
  - repräsentative, bereinigte Fixtures für V1 URL, V2 Import, V3 und V4.
- `backend/Archive.Tests/SnapshotManifestReaderTests.cs` (neu)
  - Versionsabbildung und Ablehnung ungültiger Verträge.
- `backend/Archive.Tests/SnapshotMigratorOptionsTests.cs`, `SnapshotMigrationNormalizerTests.cs` und fokussierte Service-Tests (neu)
  - CLI-Matrix, Normalisierung, Zustandsübergänge, Idempotenz, bedingte Aktivierung und sichere Bereinigung.
- gegebenenfalls ein schlanker Repository-Auslieferungstest mit In-Memory-Objektspeicher für End-to-End-Auflösung und In-Memory-Normalisierung.
- `docs/interactive-snapshots.md`
  - Support-Matrix, Schreib-/Leseregel und Fail-Closed-Verhalten dokumentieren.
- `docs/snapshot-migration.md` und `docs/deployment.md`
  - Betriebsanleitung, Zustände, exakte Produktionsreihenfolge, Restore-Fenster und Compose-Aufrufe.

Nicht vorgesehen sind Änderungen an Frontend, generierten API-Typen, HTTP-Verträgen, Datenbankschema oder Worker-Erfassung. Das Migrationswerkzeug verändert bei ausdrücklicher Ausführung gespeicherte Produktionsobjekte und den aktiven `ContentPrefix`, nicht jedoch während Build, Deployment oder API-Start. Da keine neue Route oder eigenständige Ansicht entsteht, ist kein HTML-Mockup erforderlich.

## Test- und Prüfkonzept

### Parser- und Adaptertests

- je ein positives Fixture für Version 1, 2, 3 und 4;
- beide bekannten optionalen `resourceReferenceMode`-Ausprägungen von Version 1/2;
- V3-Überführung von `replay` nach `interaction`;
- V1/V2-Pfadableitung und Rollen-Inferenz;
- fehlende oder nicht ganzzahlige Version;
- unbekannte zukünftige Version;
- fehlendes Root-Dokument;
- Root-Objektschlüssel außerhalb des aktiven Prefixes;
- doppelte oder unsichere Pfade;
- fehlende Pflichtfelder und ungültige Rollen;
- Ausschluss von Screenshot und Quellobjekt aus der Allowlist.

### Auslieferungstests

- `index.html` und ein Asset lassen sich für jede Version öffnen;
- V1/V2-Dokumente werden in-memory normalisiert und enthalten danach den aktuellen Snapshot-Shim;
- V3-Dokumente mit unterstütztem alten Normalizer werden aktualisiert;
- unbekannter Normalizer und unbekannte Manifestversion ergeben `NotFound` und eine aussagekräftige Warnung;
- Sichtbarkeits- und Capability-Grenzen bleiben unverändert;
- falls Byte Ranges bei Implementierungsbeginn in `master` vorhanden sind: V3/V4 behalten Range und starke ETags, V1/V2 fallen ohne erfundenen Hash sicher auf Vollantworten zurück.

### Migratortests

- vollständige CLI-Aktions-/Selektor-Matrix einschließlich Hilfe, JSON, Bestätigung und Exitcodes;
- `--dry-run` erzeugt garantiert keine Writes;
- Migration einer V1-, V2- und V3-Generation erzeugt jeweils ein valides V4-Manifest mit Hashes, Rollen, normalisierten Dokumenten und bewahrtem Quellmaterial;
- bereits valides V4 wird idempotent übersprungen;
- Fehler vor `prepared`, Konkurrenz beim bedingten Datenbankupdate sowie abgebrochene Aktivierung hinterlassen eindeutig bewertbare Zustände;
- Restore verlangt vollständige Originalobjekte und schaltet nur vom erwarteten migrierten Prefix zurück;
- Clean löscht ausschließlich die journalisierte inaktive Objektliste, niemals über eine neu berechnete breite Prefix-Auswahl;
- `--clean --all` verweigert die Ausführung, solange ein Snapshot nicht `Current` ist;
- Manifest- und Objekthashes werden vor Aktivierung sowie vor dem Löschen der Restore-Quelle erneut geprüft.

### Projektprüfungen

Nach Implementierung:

```powershell
dotnet build backend/Archive.slnx -m:1
dotnet test backend/Archive.slnx -m:1
docker compose --project-directory . -f deploy/production/snapshot-migrator.compose.yml config
docker build -f backend/Archive.SnapshotMigrator/Dockerfile backend
```

Zusätzlich werden die drei öffentlichen Produktions-Snapshots über Frontend und direkte `contentUrl` erneut geprüft. Mit bereitgestellten anonymisierten Produktionsmanifesten wird verifiziert, dass deren Version, Feldvariante und Ressourcenpfade von den Fixtures tatsächlich abgedeckt sind. Es werden keine Produktionsobjekte geschrieben oder migriert.

## Rollout und Auswirkungen

Die Anwendungskompatibilität und das Migrationswerkzeug werden gemeinsam gebaut, aber operativ getrennt eingesetzt:

1. Aktuellen Hauptbranch, Konfiguration, Datenbanksicherung und verfügbaren Objektspeicherplatz prüfen.
2. Worker pausieren, damit während der Bestandsmigration keine neue aktive Generation entsteht.
3. Migrator-Image aus exakt derselben Release-Revision wie die Anwendung bauen.
4. `--dry-run --all --json` ausführen und alle Zustände außerhalb `Current`/`Migratable` klären.
5. Die Anwendung mit dem kompatiblen Reader ausrollen, damit auch ursprüngliche oder wiederhergestellte Generationen lesbar bleiben.
6. `--migrate --all` ausführen.
7. Erneut `--dry-run --all --json`; jeder gespeicherte Snapshot muss `Current` sein.
8. Worker wieder starten und mindestens je einen neuen Capture sowie die bekannten älteren Produktions-Snapshots prüfen.
9. Journale und ursprüngliche Generationen für ein festgelegtes Beobachtungs-/Restore-Fenster behalten.
10. Erst danach `--clean --all --confirm` ausführen und abschließend nochmals `--dry-run --all` prüfen.

Bestehende V4-Captures behalten ihren gespeicherten Vertrag und Auslieferungspfad. Vor der Migration können V1/V2 beim Dokumentabruf mehr CPU benötigen, weil HTML in-memory normalisiert wird. Nach erfolgreicher V4-Migration besitzen alle Ressourcen berechnete Hashes und dieselbe normale Auslieferungsfunktionalität. Fehler werden versions- und zustandsbezogen protokolliert, ohne Manifestinhalt, Capability oder sensible Konfiguration vollständig ins Log zu schreiben.

## Verworfene Alternativen

### Nur `Version <= CurrentVersion` akzeptieren

Ungeeignet, weil unterschiedliche JSON-Strukturen dann stillschweigend in die aktuelle DTO deserialisiert würden. Fehlende sicherheitsrelevante Felder könnten als Standardwerte erscheinen.

### Alte Manifeste beim ersten Abruf überschreiben

Ungeeignet, weil ein Lesezugriff unerwartet persistente Daten verändert, Parallelitäts- und Rollback-Probleme erzeugt und die historische Provenienz verwischt.

### Ausschließlich ein einmaliges Migrationswerkzeug bauen

Eine Migration rüstet Hashes und volle Feature-Parität nach, löst allein aber nicht das wiederkehrende Architekturproblem beim nächsten Versionssprung. Ohne kompatiblen Reader wäre außerdem ein Restore auf eine alte Generation nach dem API-Rollout wieder unlesbar. Deshalb sind Reader und Werkzeug komplementär.

### Die Migration als Manifest V5 kennzeichnen, ohne den Vertrag zu ändern

Ungeeignet, weil die Versionsnummer ihre Bedeutung als Schemacontract verlieren würde. Operative Herkunft gehört in Provenienz und Journal. Ein V5-Vertrag ist nur bei einer tatsächlichen, dokumentierten Schemaänderung sinnvoll.

### Eine DTO mit vielen optionalen Feldern für alle Versionen

Ungeeignet, weil Versionssemantik, Validierung und Pflichtfelder miteinander verschwimmen. Explizite Adapter machen Kompatibilitätsentscheidungen sichtbar und testbar.

## Interviewentscheidungen

1. **Entschieden:** Dauerhafte direkte Lesekompatibilität und ein zusätzliches einmaliges Migrationswerkzeug werden gemeinsam umgesetzt. Das Werkzeug bietet mindestens `--dry-run`, `--migrate`, `--restore` und `--clean`, ein Dockerfile sowie eine separate Compose-Datei.
2. **Entschieden:** Manifest V4 bleibt unverändert der Zielvertrag. Es fehlt aktuell kein dauerhaft benötigtes Schemafeld; Release- und Migrationsstatus werden nicht in die Schemanummer verlagert.
3. **Entschieden:** V1/V2-Dokumente werden vor und während der Migration beim Lesen mit dem aktuellen Normalizer und Shim ausgeliefert; die heutige Isolation gilt versionsunabhängig.
4. **Prüfgrenze:** Die öffentliche Produktions-API exponiert keine Manifestversion. Die Regression-Fixtures werden deshalb aus den historischen Verträgen des Repositories abgeleitet und die öffentlich erreichbaren älteren Snapshots werden als Auslieferungsprüfung verwendet. Ein später bereitgestelltes anonymisiertes Produktionsmanifest kann ohne Architekturänderung als weiteres Fixture ergänzt werden.

Diese Entscheidungen wurden vor Implementierungsbeginn festgeschrieben. Die ausdrückliche Freigabe erfolgte am
30. Oktober 2026 mit der Entscheidung, V4 ohne zusätzliches Feld als Zielvertrag beizubehalten.
