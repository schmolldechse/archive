# Worker nach Zuständigkeiten gliedern

Status: Am 16. Oktober 2026 genehmigt, umgesetzt und geprüft.

Themengebiet: unübersichtliche Worker-Struktur. Grundlage ist der aktuelle Quellbestand vom 16. Oktober 2026, ausgehend von `939bc04`.

## 1. Befund

Der Worker enthält 25 C#-Dateien mit 2.713 Zeilen, ohne generierte Build-Dateien. Davon liegen 14 Dateien unmittelbar in `Capture`, acht in `Import`, zwei im Projektstamm und eine in `Storage`.

Die Dateianzahl ist überschaubar. Das Problem sind vermischte Verantwortlichkeiten und teilweise versteckte Typen:

| Stelle | Konkretes Problem | Vorgeschlagene Änderung |
| --- | --- | --- |
| `Capture/` | Browseraufnahme, Zugriffskontrolle, robots.txt, Hostabstände, DNS-Prüfung und Bot-Signierung stehen nebeneinander. | Browser-, Crawl- und Netzwerkkomponenten gruppieren. |
| `ArchiveWorkerService.cs` | Die Hintergrundschleife und der vollständige Ablauf eines einzelnen Auftrags stehen in derselben Klasse. | Schleife und Einzelverarbeitung trennen. |
| `CaptureResult.cs` | Enthält auch `CapturedResource`, obwohl nur die Browseraufnahme diese Ressourcenform benötigt. | Browser-Ressource zur Browserkomponente verschieben. |
| `CaptureExceptions.cs` und `CapturePageAccess.cs` | Gemeinsame Verarbeitungsfehler und browserspezifische Zugriffsfehler sind vermischt; eine Exception steht am Ende der Zugriffsklasse. | Gemeinsame Fehler im Capture-Stamm, Zugriffsfehler zusammen bei Browser/Access halten. |
| `ImportedPage.cs` | Enthält neben Dokument- und Ressourcendaten auch Importlimits und das Decoderinterface. | Limits und Interface unter ihren eigenen Dateinamen auffindbar machen. |
| `Import/` | Ablauf, Normalisierung und vollständige Formatparser stehen nebeneinander. | Parser in `Decoders` zusammenhalten. |
| `Program.cs` | Enthält sowohl Hoststart als auch die gesamte Workerregistrierung samt Optionsnormalisierung. | Registrierung wie bereits im Core-Projekt in `Injection.cs` bündeln. |

Eine Suche nach Verwendungen außerhalb des Workerprojekts hat keine anderen C#-Konsumenten seiner Typen ergeben. Solution und Deployment referenzieren das Workerprojekt beziehungsweise seinen Dockerfile; diese Pfade bleiben bestehen.

## 2. Zielstruktur

```text
Archive.Worker/
├── Program.cs
├── Injection.cs
├── Processing/
│   ├── ArchiveWorkerService.cs
│   └── SnapshotProcessor.cs
├── Capture/
│   ├── CaptureResult.cs
│   ├── CaptureExceptions.cs
│   ├── Browser/
│   │   ├── BrowserCaptureEngine.cs
│   │   ├── ArchiveBrowserOptions.cs
│   │   ├── CapturedResource.cs
│   │   ├── CapturedResourceCollector.cs
│   │   └── Access/
│   │       ├── CapturePageAccess.cs
│   │       ├── CaptureAccessExceptions.cs
│   │       ├── CloudflareChallengeDetector.cs
│   │       └── BrowserRequestRouter.cs
│   ├── Crawling/
│   │   ├── CrawlPolicyService.cs
│   │   ├── CrawlPolicyOptions.cs
│   │   └── HostRateLimiter.cs
│   └── Network/
│       ├── PublicNetworkGuard.cs
│       ├── WebBotAuthSigner.cs
│       └── WebBotAuthOptions.cs
├── Import/
│   ├── OfflineImportEngine.cs
│   ├── OfflinePageNormalizer.cs
│   ├── ImportedPage.cs
│   ├── ImportLimits.cs
│   ├── ImportStream.cs
│   ├── IUploadedDocumentDecoder.cs
│   └── Decoders/
│       ├── HtmlDecoder.cs
│       ├── MhtmlDecoder.cs
│       ├── WebArchiveDecoder.cs
│       └── PropertyListReader.cs
└── Storage/
    └── SnapshotObjectWriter.cs
```

Projektdatei, Dockerfile, Konfigurationsdateien und `Properties/launchSettings.json` bleiben am bisherigen Ort. Alle Komponenten bleiben im bestehenden Workerprojekt.

`Capture` beschreibt die gemeinsame Aufnahme eines Archivs, unabhängig von URL oder Datei. Deshalb liegen `CaptureResult` und die allgemeinen Capture-Exceptions weiterhin unmittelbar dort. Playwright-spezifische Aufnahmebestandteile gehören nach `Capture/Browser`. Der Dateiimport benötigt von dieser Browserkomponente weiterhin die vorhandenen Browseroptionen für seinen lokalen Screenshot.

Die Ordner bestimmen die Namespaces, beispielsweise `Archive.Worker.Capture.Browser.Access` und `Archive.Worker.Import.Decoders`. `using`-Anweisungen werden entsprechend angepasst. Vorhandene öffentliche Typen behalten ihre Sichtbarkeit; der neue `SnapshotProcessor` wird `internal sealed`.

## 3. Hintergrundschleife und Einzelverarbeitung trennen

### ArchiveWorkerService

Der Hintergrunddienst übernimmt den Lebenszyklus des Workers:

1. Globale PostgreSQL-Lease erwerben und überwachen.
2. Unter dieser Lease unterbrochene Versuche wiederherstellen.
3. Die zurückgegebenen temporären Uploads über den Processor bereinigen lassen.
4. Den nächsten Auftrag beanspruchen und seine Verarbeitung vollständig abwarten.
5. Bei Lease-Verlust oder Prozessende die bestehende Abbruch- und Wartefolge ausführen.

`ProcessingLease.AcquireAsync`, `RecoverInterruptedAsync`, `TryClaimNextAsync` und die Monitorsteuerung bleiben in dieser Schleife. Die Claim- und Recovery-Aufrufe verwenden weiterhin die Datenbanksitzung, die die Lease hält. Die Vorgabe von systemweit höchstens einer aktiven Verarbeitung bleibt erhalten.

### SnapshotProcessor

Die folgenden bestehenden Bestandteile werden aus dem Dienst in diese konkrete Klasse verschoben:

- `ProcessAsync`: Fortschritt, Aufnahme, Veröffentlichung, Statusbehandlung und Bereinigung eines beanspruchten Auftrags.
- `CaptureAsync`: Auswahl von Browseraufnahme oder Dateiimport und Prüfung, ob der Auftrag weiterlaufen darf.
- `FinishUntilConfirmedAsync`: Bestätigung des endgültigen Verarbeitungszustands.
- `DeleteUploadAsync`: bestmögliche Bereinigung temporärer Uploads, auch für die Wiederherstellung aus der Dienstschleife nutzbar.
- `WithRepositoryAsync`: kurzlebige Scopes für die bestehenden Repositoryaufrufe.
- `CaptureProgress`: bleibt als private, verschachtelte Klasse beim Processor, weil nur dieser sie verwendet.

Es entstehen zwei Aufrufstellen des Hintergrunddienstes: `ProcessAsync` für einen Claim und `DeleteUploadAsync` für zurückgegebene Recovery-Uploads. Die bestehende Löschmethode wird damit einmal weiterverwendet.

Der Processor wird als Singleton registriert. Er hält keine EF-Entities oder laufenden Auftragszustände in Instanzfeldern. Er erhält wie bisher `IServiceScopeFactory`, die beiden Engines und den Object Store. Repositoryzugriffe behalten ihre eigenen kurzen Scopes; ein `DbContext` wird nicht im Singleton gehalten.

Die Reihenfolge der vorhandenen Operationen wird übernommen. Insbesondere bleiben Fortschritts-Flush, OwnerToken-Prüfung, Abbruchabwarten, Behandlung unklarer Veröffentlichung und die Entscheidung über das Löschen unveröffentlichter Objekte erhalten. Die Datenbank- und Leaseimplementierung im Core-Projekt wird dabei nicht verändert.

## 4. Capture und Import gezielt ordnen

### Gemeinsame Capture-Typen

`CaptureResult` enthält weiterhin genau die bisherigen Ergebnisfelder. `CaptureDiscardedException`, `CaptureFailedException` und `CaptureCancelledException` bleiben gemeinsam in `CaptureExceptions.cs`. Beide Engines und der Object Writer verwenden diese gemeinsamen Typen.

`CapturedResource` bekommt eine eigene Datei unter `Capture/Browser`. `CaptureAccessBlockedException` und `CaptureChallengeEncounteredException` werden zusammen in `Browser/Access/CaptureAccessExceptions.cs` abgelegt. Ihre Basistypen und ihre Behandlung bleiben gleich.

### Browser, Crawling und Netzwerk

- `Browser` enthält Aufnahme und Ressourcensammlung. `Access` hält Navigation, Challenge-Erkennung und Playwright-Routing zusammen, weil diese Komponenten gemeinsam den Seitenzugriff kontrollieren.
- `Crawling` enthält robots.txt-Auswertung und Warteabstände. Die eng zusammengehörenden privaten Parsertypen in `CrawlPolicyService` bleiben dort verschachtelt.
- `Network` enthält die HTTP-Zielprüfung und Bot-Signierung. Beide werden von Browserzugriff und robots.txt-Abruf verwendet; deshalb werden sie keinem dieser beiden Verbraucher untergeordnet.
- Optionsklassen bleiben bei ihren Komponenten. Die Konfigurationsschlüssel `Archive:Browser`, `Archive:CrawlPolicy` und `Archive:WebBotAuth` bleiben unverändert.

Browseraufnahme und Dateiimport erzeugen jeweils einen Browser. Die ähnlichen Startzeilen rechtfertigen hier keine gemeinsame Sitzungsklasse: Der Import rendert eingebettete Dateien mit deaktiviertem JavaScript und lokaler Routenfreigabe; die URL-Aufnahme hat eine eigene Netzprüfung, Signierung und Challenge-Phase. Die vorhandene Ressourcenfreigabe und Abbruchregistrierung bleiben beim jeweiligen Ablauf.

### Dateiimport

Die drei Decoder und der nur vom Webarchive-Decoder verwendete `PropertyListReader` ziehen nach `Import/Decoders`. Es gibt vorerst keinen zusätzlichen Unterordner pro Dateiformat.

`IUploadedDocumentDecoder` und `ImportLimits` erhalten jeweils eine eigene Datei im Import-Stamm. `ImportedPage` und `ImportedResource` bleiben als zusammengehöriges Dokumentmodell in `ImportedPage.cs`. Ebenso bleiben `NormalizedPage` und `NormalizedAsset` beim einzigen erzeugenden Normalizer.

`ImportStream` bleibt im Import-Stamm, weil ihn sowohl die Engine als auch die Decoder verwenden. Der vorhandene Decodervertrag und die Prüfung auf genau einen registrierten Decoder je unterstütztem Dateiformat bleiben erhalten.

## 5. Registrierung und Kommentare

`Injection.cs` erhält die Erweiterungsmethode `AddArchiveWorker(IServiceCollection, IConfiguration)`. Sie bündelt die bestehenden Optionsbindungen, deren Normalisierung und Validierung sowie die Workerregistrierungen einschließlich des neuen Processors. `AddArchiveCore` bleibt als eigener Aufruf im Host sichtbar.

`Program.cs` zeigt anschließend Hostaufbau, Core- und Workerregistrierung, explizite Startvalidierung, optionale Migration und Hoststart. Die Startvalidierung erfolgt weiterhin vor der Migration. Die bestehenden Singleton-Lebensdauern von Engines, Signer, Crawl-Service und Rate-Limiter bleiben erhalten.

Kommentare an verschobenen oder extrahierten Stellen werden auf sachlichen Inhalt geprüft. Hinweise auf notwendige Reihenfolgen, Playwright-Eigenheiten, Sperren und unklare Schreibbestätigungen bleiben erhalten. Überflüssige Ablaufbeschreibungen entfallen. Die neuen Namen und Ordner tragen die Erklärung der Zuständigkeiten; zusätzliche Architekturrechtfertigungen im Code sind nicht vorgesehen.

## 6. Umfang und fachliche Grenzen

Diese Änderung ordnet den bestehenden Ablauf und trennt die Verantwortung des Hintergrunddienstes. Sie erhält:

- Eingabeformate, Auswahl der Engine und Decoderverhalten;
- HTML-/CSS-Umschreibung, Framebehandlung und Ressourcenreihenfolge;
- Challenge-, Redirect- und Netzfreigaberegeln;
- Zeit-, Speicher-, Ressourcen- und Importlimits;
- Objektpfade, Originaldateien, beide Manifestformate und die bestehende Writerbereinigung;
- Fortschrittsmeldungen, Statusentscheidungen, Veröffentlichungs- und Abbruchreihenfolge.

Die vorhandenen Abstraktionen `IUploadedDocumentDecoder` und `SnapshotObjectWriter` decken ihre unterschiedlichen Implementierungen beziehungsweise den gemeinsamen Schreibablauf ab. Die Auswahl zwischen den beiden Engines bleibt eine direkte Fallunterscheidung. Zusätzliche Engineinterfaces, Vererbung oder eine erweiterbare Pipeline sind für diese zwei bekannten Wege nicht erforderlich.

Öffentliche HTTP-Verträge und Frontenddarstellung ändern sich nicht. Deshalb ist für diesen Vorschlag keine Frontend-Typgenerierung vorgesehen. Datenbankmodell, Projektentfernung, Beschreibungslimit und eine Vereinheitlichung der Referenzumschreibung gehören zu anderen Themengebieten.

## 7. Umsetzung und Prüfung nach Freigabe

1. Aktuellen Solution-Build als Ausgangspunkt prüfen und vorhandene Fehler gegebenenfalls getrennt dokumentieren.
2. Dateien entsprechend der Zielstruktur verschieben und Namespaces sowie Verwendungen aktualisieren. Die neue Struktur soll im Git-Diff als Umbenennung erkennbar bleiben.
3. Gemeinsam genutzte beziehungsweise browserspezifische Typen wie beschrieben zuordnen; unveränderte Felder, Basistypen und Signaturen prüfen.
4. Einzelverarbeitung in `SnapshotProcessor` extrahieren. Methodenrümpfe und Aufrufreihenfolge anhand des Ausgangsstands vergleichen; die Recovery bleibt an die vorhandene Lease gebunden.
5. Workerregistrierung nach `Injection.cs` verschieben. Optionen, Lebensdauern, Decoderregistrierung und die Validierung vor Migration gegenprüfen. Neue Namespace- und Loggerkategorien auch auf konfigurierte Filterverwendungen prüfen.
6. Die gesamte Solution bauen und den Worker im Release-Modus veröffentlichen. Damit werden projektübergreifende Referenzen, DI-relevante Typverwendungen, Compilerfehler und das Veröffentlichungsergebnis geprüft. Ein Build allein bestätigt keine erfolgreiche DI-Auflösung; die Registrierungen werden zusätzlich ausdrücklich durchgesehen.
7. Restliche Namespace- und Pfadverwendungen suchen und den vollständigen Diff auf unbeabsichtigte funktionale Änderungen prüfen. `git diff --check` ausführen.
8. Nur die zugehörigen Dateien in den Index aufnehmen und den genehmigten Umbau mit `refactor: organize worker responsibilities` committen.

Im aktuellen Repository gibt es kein eingechecktes Testprojekt. Für diese Verschiebung und die Übernahme vorhandener Methoden ist keine neue Testinfrastruktur eingeplant. Build, Release-Publish und der gezielte Vergleich des unveränderten Ablaufs bilden die Pflichtprüfung. Sie ersetzen keinen Lauf gegen PostgreSQL, Object Store und echte Browserziele; eine solche End-to-End-Prüfung wird nicht als durchgeführter Nachweis ausgegeben.

Abgenommen ist der Umbau, wenn jede Datei einen eindeutigen fachlichen Ort hat, der Hintergrunddienst den Workerlebenszyklus zeigt, der Processor den einzelnen Auftrag abwickelt und die genannten Verträge und Abläufe erhalten bleiben.

## 8. Einordnung der Praxis

Microsoft beschreibt Namespaces als Gruppierung zusammengehöriger Typen und nennt eine entsprechende Ordnerstruktur als übliche Konvention. Die konkrete oben vorgeschlagene Gliederung ist eine Entscheidung anhand dieses Workers, keine von .NET vorgeschriebene Architektur: [Namespaces and using directives](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/program-structure/namespaces).

Für `BackgroundService` dokumentiert Microsoft die Singleton-Lebensdauer und die Verwendung expliziter Scopes für kurzlebige Dienste. Deshalb bleiben die vorhandenen Repositoryscopes und die separate Lease-Sitzung erhalten. Die Delegation an einen konkreten Processor folgt dieser Verantwortungstrennung; der Processor benötigt selbst keinen langlebigen Datenbankkontext: [Use scoped services within a BackgroundService](https://learn.microsoft.com/en-us/dotnet/core/extensions/scoped-service).

## 9. Stand der Freigabe

Der Nutzer hat den Vorschlag am 16. Oktober 2026 genehmigt. Die Zielstruktur und die Extraktion des Processors sind umgesetzt. Der öffentliche Hintergrunddienst wird über eine Registrierungsfactory mit internem Konstruktor erzeugt, damit der Processor wie vorgesehen intern bleiben kann.

Ergebnisse der Prüfung:

- Solution-Build vor und nach dem Umbau erfolgreich, jeweils ohne Warnungen und Fehler.
- Release-Publish des Workers erfolgreich.
- Methodenrümpfe in 20 bestehenden Dateien automatisiert gegen `939bc04` verglichen: abgesehen von Namespace- und Importänderungen unverändert; die ausgegliederte Challenge-Exception wurde separat zugeordnet.
- Vollständige Einzelverarbeitung einschließlich Fortschrittshelfer sowie Lease-Schleife gegen den Ausgangsstand verglichen. Die einzigen Anpassungen sind die neuen Aufrufziele, Membernamen, die benötigte Methodensichtbarkeit und ein gekürzter Kommentar.
- Bestehende Options- und Engineregistrierungen sowie der Hoststart ab der Validierung vor Migration gegen den Ausgangsstand verglichen: unverändert.
- Tatsächliche Dienstauflösung mit `ValidateOnBuild` und `ValidateScopes` geprüft: Hintergrunddienst, Singleton-Processor und Engines, alle drei Decoder und das Repository im eigenen Scope erfolgreich aufgelöst. Auflösung des Scoped-Repositorys aus dem Root-Provider wird weiterhin abgewiesen.
- Optionsnormalisierung und Startvalidierung geprüft; ungültige Browser-, Crawl- und Bot-Auth-Optionen werden jeweils abgewiesen.
- Namespaces gegen die Ordnerstruktur geprüft; keine verbliebenen Verwendungen der verschobenen vollqualifizierten Typnamen oder konfigurierten Loggerfilter gefunden.

Die zusätzliche DI-Prüfung lief in einem vorübergehenden Konsolenprojekt im ignorierten Buildverzeichnis. Es wurde anschließend entfernt. Der Hintergrunddienst wurde dabei nicht gestartet; es gab keine Datenbankverbindung, Migration, Browseraufnahme oder Object-Store-Anfrage.

Der Implementierungscommit enthält ausschließlich den Workerumbau und dieses Planungsdokument. Bereits vorhandene Änderungen außerhalb dieses Themengebiets bleiben unberührt.
