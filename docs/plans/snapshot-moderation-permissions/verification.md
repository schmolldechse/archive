# Prüfprotokoll der Planung und HTML-Mockups

Stand: 2026-10-17. Dieses Protokoll bewertet ausschließlich die Planungsartefakte, keine implementierte Anwendung.

## Repository und Umfang

- `git fetch origin` erfolgreich.
- `git rev-list --left-right --count master...origin/master`: `0 0`.
- Beide Referenzen: `0abec2f6f77b07cfc88b2f5b8f2e5edfde1c13bc`.
- Planungsbranch `docs/snapshot-moderation-permissions` direkt von `master` erstellt.
- Keine Änderungen an produktiven Backend-/Frontend-Quellen, API-Verträgen, generierten Typen, Migrationen oder Konfigurationen.
- Vorhandene lokale Dateien in `deploy/production/` bleiben unversioniert und außerhalb des Commits.

## HTML-Mockups

| Geplante Route         | Mockup                                             |
| ---------------------- | -------------------------------------------------- |
| `/account/submissions` | [My submissions](mockups/account-submissions.html) |
| `/admin/snapshots`     | [Snapshot review](mockups/snapshot-review.html)    |
| `/admin/users`         | [User access](mockups/user-access.html)            |

[preview.html](mockups/preview.html) bietet eine gemeinsame Vorschau mit auswählbarer Seite, Breite, Theme und Inhaltsvergrößerung. Diese Hilfsdatei ist keine zusätzliche Produktseite. Die Mockups laden vorhandene lokale Schriftdateien aus `frontend/node_modules`; ohne installierte Frontend-Abhängigkeiten greifen die definierten Systemschriften. Keine externen Fonts oder Bilddienste.

Die integrierte Browserprüfung erfolgte über einen temporären Node-HTTP-Server auf `127.0.0.1:4178`. Dieser liefert ausschließlich die Mockup-Dateien und ihre lokalen Fonts aus. Die erste Vorschau aus dem eingeschränkten Prozess war nicht erreichbar; nach lokalem Serverstart mit den erforderlichen Ausführungsrechten konnten die Seiten geladen und geprüft werden. An Netzwerk-/Firewall-Einstellungen wurde nichts geändert.

## Darstellungsprüfung

Die folgenden Kombinationen wurden im integrierten Browser in festen iframe-Breiten geprüft. Die Seite selbst erhält keine gerätespezifische Ersatzansicht; ihre CSS-Media-Queries reagieren auf die jeweilige Frame-Breite.

| Seite           | 1440px Light/Dark          | 768px Light/Dark           | 390px Light/Dark           | 320px Light/Dark           |
| --------------- | -------------------------- | -------------------------- | -------------------------- | -------------------------- |
| My submissions  | Kein horizontaler Überlauf | Kein horizontaler Überlauf | Kein horizontaler Überlauf | Kein horizontaler Überlauf |
| Snapshot review | Kein horizontaler Überlauf | Kein horizontaler Überlauf | Kein horizontaler Überlauf | Kein horizontaler Überlauf |
| User access     | Kein horizontaler Überlauf | Kein horizontaler Überlauf | Kein horizontaler Überlauf | Kein horizontaler Überlauf |

In allen 24 Kombinationen entsprach `documentElement.scrollWidth` der verfügbaren `clientWidth`. Wegen der vertikalen Scrollbar lagen die tatsächlich verfügbaren Inhaltsbreiten bei 1423, 751, 373 und 303 Pixeln. Zusätzlich wurde die Darstellung direkt im 1280px-Browserfenster kontrolliert.

Visuell kontrolliert:

- Desktop-Register und die mobile Anordnung von Listen, Metadaten, Formularen und Aktionen.
- Lange URL mit Queryparametern: vollständiger Umbruch und sichtbare Copy-Aktion.
- Dark-Mode-Rechteverwaltung: erlaubte und verweigerte Rechte zusätzlich mit Text/Symbol.
- Sichtbare Fokusumrandung beim Wechsel von `Approve` zu `Reject` per Tab.
- Die gespeicherte Beispielseite in der Prüfvorschau behält ihren weißen Originalhintergrund auch im dunklen Archivrahmen.
- 200% Inhaltsvergrößerung über CSS `zoom` bei 1440px Frame-Breite für alle drei Seiten: kein horizontaler Überlauf. Dies ist eine Prototypprüfung der Vergrößerung, kein vollständiger Ersatz für spätere Browserzoom-/Textzoom- und Screenreader-Tests der implementierten Anwendung.

## Geprüfte Demo-Abläufe

1. **Eigene Wiedervorlage:** Filter `Rejected`, Bestätigung öffnen, `Confirm resubmission`, anschließend Filter `Resubmitted`. Ergebnis: gleiche Beispielübermittlung, Status Resubmitted, neue Einreichung in der Historie; die ursprüngliche Ablehnung samt Grund bleibt erhalten. Der aktuelle Eintrag zeigt keinen aktuellen Ablehnungsgrund und keine Rundennummer.
2. **Ablehnung:** `Reject`, optionalen Grund eingeben, bestätigen. Ergebnis: Rejected und Entscheidung in der lokalen Historie; kein direkter Genehmigungsbutton mehr.
3. **Genehmigung:** anonyme Beispielübermittlung auswählen, `Approve`. Ergebnis: Approved und Publikationsbestätigung; Anonymität verhindert die Moderation nicht.
4. **Persönliche Einschränkung:** Sam besitzt Admin, aber `Publish without approval = Deny`. Ergebnis: `Denied · Individual setting`.
5. **Persönliche Erweiterung:** Jamie besitzt nur User und `Review snapshots = Allow`. Ergebnis: `Allowed · Individual setting`. Ein Wechsel auf Deny zeigt die Einschränkung.
6. **Filter:** Text- und Statusfilter aktualisieren die sichtbaren Beispielzeilen und den Leerzustand.

Diese Abläufe ändern ausschließlich den lokalen DOM-Zustand. Ein Reload setzt Beispieldaten zurück. Sie beweisen keine Backend-Autorisierung, Transaktionssicherheit oder Speicherung. Die produktionsbezogenen Abnahmekriterien stehen im [Plan](plan.md#11-abnahmekriterien-und-spätere-prüfungen).

## Statische Kontrolle und Grenzen

- JavaScript-Syntax mit `node --check` geprüft.
- Alle Planungsdateien mit dem bereits installierten Prettier formatiert und auf Formatierung geprüft.
- Git-Diff auf Whitespace-Fehler und auf den beauftragten Dateiumfang geprüft.
- Lokale Markdown-Verweise und lokale CSS-/JS-/Font-Verweise auf vorhandene Ziele kontrolliert.
- Der integrierte Browser protokollierte bei Frame-Wechseln zwei `MutationObserver.observe`-Fehler ohne Zuordnung zu einer Mockup-Datei. Die eigenen Mockup-Skripte verwenden keinen MutationObserver; Ursache nicht abschließend geklärt. Die oben beschriebenen Abläufe und Darstellungsprüfungen konnten ausgeführt werden.
- Links zu bestehenden Produktseiten (`Browse`, `Archive a page`, beispielhafter öffentlicher Viewer) zeigen deren vorgesehene Zielpfade. Der isolierte Vorschau-Server stellt diese Produktseiten nicht bereit.
- Die statische Beispielvorschau illustriert einen bereits aufgenommenen Screenshot. Der geschützte Bildabruf und Rechteentzug sind später an der implementierten Anwendung zu prüfen. Es ist kein zusätzlicher privater HTML-iframe oder Content-Proxy mehr geplant.
- Die Rechteanzeige ist eine lokale Vorschau auf die gewählte Kombination; Speichern zeigt nur eine Demo-Bestätigung. Fachliche Zustandsprüfung, Schutz des letzten Admins und getrennte Serverbestätigungen sind im Plan beschrieben.
- Keine Backend-Tests, kein API-Generator und kein produktiver Frontend-Build: Produktivcode und API-Verträge wurden nicht geändert.

## Überarbeitung nach Rückmeldung

- Die Suche im Repository umfasst weitere Markdown-Dateien sowie alle Dateien dieses Planungsverzeichnisses. Außer dem vorliegenden Plan und seinem Prüfprotokoll wurden keine weiteren Pläne mit den verworfenen Feldern gefunden.
- Versionsspalten und Versionsparameter sind auch aus API-Beschreibung, Migration, Nebenläufigkeit und Abnahmekriterien entfernt. Keine versteckten Zeitstempel-, ETag- oder Historien-ID-Vergleiche als Ersatz.
- Am Snapshot ist nur der Moderationsstatus neu. Auch Freigabezeit und letzte Einreichung werden aus der Historie gelesen. Es gibt keinen Rundenzähler; der vorhandene technische Queue-Zähler bleibt fachlich unberührt.
- Der konkrete Modellvorschlag ist eine JSONB-Spalte mit abstrakten, explizit registrierten Datenvarianten. Gemeinsame Metadaten stehen an einer einzelnen konkreten Persistenzentität. Keine TPH-/TPT-Hierarchie und keine doppelte Typspalte.
- Die Aufteilung wurde im Interview bestätigt. Der anschließend angeforderte [Praxisabgleich](history-design-review.md) vergleicht offizielle EF-/Npgsql-Mapping-Verfahren, benennt die Grenze von Complex-Type-Vererbung und begründet den lokalen Konverter mit unveränderlichen Payloads. Er ergänzt keine Implementierung.
- Terminale Verarbeitungsergebnisse und Direktpublikationsabsicht werden als passende Historieneinträge erhalten. Sie werden nicht zusätzlich an Snapshot gespiegelt.
- Der vorhandene öffentliche Content-Proxy wurde anhand von Viewer, API-CSP, gespeicherten URL-Pfaden und Deployment-Vorlage geprüft. Im aktuellen Aufbau bleibt er erforderlich; Abschnitt 6.3 nennt die konkreten Abhängigkeiten. Die zuvor vorgeschlagene zusätzliche private Proxy-Strecke entfällt zugunsten vorhandener Screenshots.
- Die Route ist `/account/submissions`; Mockup-Datei und sämtliche Verknüpfungen wurden in `account-submissions.html` umbenannt. Die Account-Einordnung erscheint im Breadcrumb. Der produktive Router wurde nicht verändert.
- Die 24 Breiten-/Theme-Kombinationen wurden nach der Überarbeitung erneut im integrierten Browser gemessen: kein horizontaler Überlauf.
- Wiedervorlage ohne Rundennummern sowie Genehmigung und Ablehnung im Demo-Zustand erneut geprüft. Frühere Entscheidungen bleiben erhalten; der aktuelle Ablehnungsgrund entfällt bei Wiedervorlage.
- Die Grenze ohne allgemeinen Schutz vor veralteten Ansichten und das Verhalten konkurrierender Einzeländerungen sind in Abschnitt 7.4 bewusst beschrieben. Die Prototypen prüfen diese Datenbankeigenschaften nicht.
