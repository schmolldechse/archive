# Wiederverwendbare Tabellenkomponenten: Lösungsplan

**Status:** Am 17. Oktober 2026 genehmigt und umgesetzt. Die Abschnitte 1–14 enthalten den genehmigten Lösungsplan; Abschnitt 15 dokumentiert die Umsetzung.

**Stand:** 17. Oktober 2026 · Repository-Ausgangsstand: `e38dac2`.

Die ursprüngliche Planungsänderung wurde mit `c3ac000` separat committed. Die anschließend genehmigte Implementierung ergänzt Komponenten und Dokumentations-Stories ohne neue Abhängigkeiten oder Änderungen an bestehenden Feature-Seiten.

## 1. Ziel und vereinbarter Umfang

Das Archiv erhält eine eigene, wiederverwendbare Tabellenfamilie für Svelte 5. Sie verbindet native HTML-Tabellen mit einer generischen `DataTable`, die Sortierung, Filterung, Pagination und Zeilenauswahl selbst umsetzt. Eine zusätzliche Bibliothek wird dafür nicht installiert. Fremde Implementierungen werden nicht übernommen.

Im Interview wurden folgende Entscheidungen getroffen:

| Entscheidung      | Festlegung für die erste Version                                              |
| ----------------- | ----------------------------------------------------------------------------- |
| Funktionsumfang   | Datentabelle mit eigener Sortierung, Filterung, Pagination und Zeilenauswahl. |
| Datenverarbeitung | Ausschließlich lokal auf dem vollständig übergebenen Datenbestand.            |
| „Select all“      | Nur auswählbare Zeilen der aktuell sichtbaren Seite.                          |

Ergänzende Festlegungen dieses Vorschlags:

- Eine aktive Sortierspalte mit aufsteigender, absteigender und ursprünglicher Reihenfolge.
- Globale Textsuche sowie optionale Text- und Auswahlfilter pro Datenspalte.
- Mehrfachauswahl mit stabilen Zeilen-IDs; Auswahl bleibt bei Seitenwechseln erhalten.
- Eine Standarddarstellung mit sichtbaren Beschriftungen und englischen Bedienungstexten.
- Datenverarbeitung und Darstellung bleiben getrennt, ohne eine allgemeine Tabellen-Engine oder ein Plugin-System nachzubauen.

Diese Ergänzungen gehören zum vorgeschlagenen Genehmigungsumfang. Sie sind keine bereits implementierten Eigenschaften.

## 2. Befund im Repository

| Bereich             | Befund und Folgerung                                                                                                                                                                                                                                 |
| ------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Framework           | `frontend/package.json` deklariert Svelte `5.57.0`, SvelteKit `2.70.3`, Tailwind CSS `4.3.3` und TypeScript `^5.9.3`. Moderne Runes, Snippets und typisierte native Attribute sind die Grundlage.                                                    |
| UI-Bibliothek       | Unter `frontend/src/lib/components/ui/` existiert keine allgemeine Tabellenfamilie. `calendar/CalendarGrid.svelte` verwendet eine Tabelle als Kalender-Grid und besitzt eigene Datums- und Tastaturlogik; daraus wird keine Datentabelle abgeleitet. |
| Wiederverwendung    | `Button.svelte`, `Badge.svelte`, `Skeleton.svelte` sowie die Familien `input`, `select`, `pagination` und `dropdown-menu` sind vorhanden. Neue Tabellen verwenden diese Bausteine, soweit die jeweilige Funktion sie benötigt.                       |
| Pagination          | `PaginationRoot` arbeitet mit einsbasierten Seiten, `totalItems`, `pageSize`, `page` und `onPageChange`. Es berechnet Navigation und Bereiche, schneidet aber keine Datensätze zu. Die Datentabelle ergänzt ausschließlich diesen Datenzuschnitt.    |
| Auswahl             | Eine allgemeine Checkbox-Komponente existiert nicht. Für Tabellen reicht eine kleine interne Komponente um ein natives `input[type="checkbox"]`; eine neue allgemeine Formularfamilie ist nicht Teil dieses Auftrags.                                |
| Design              | `frontend/src/app.css` enthält bereits semantische Farben, Schriften, Fokusregeln, Themes und Regeln für reduzierte Bewegung. Neue globale Tokens oder Schriftpakete sind nicht erforderlich.                                                        |
| Storybook           | Svelte CSF mit `defineMeta` und `*.stories.svelte` ist eingerichtet. `frontend/.storybook/preview.ts` lädt `app.css` und schaltet Themes über `data-theme`.                                                                                          |
| Bestehende Features | Die Benutzerverwaltung verwendet eine strukturierte Liste und serverseitige Suche/Pagination über `+page.server.ts`. Sie übergibt nur eine Ergebnisseite und ist deshalb kein geeigneter unmittelbarer Verbraucher dieser lokalen Datentabelle.      |

Maßgebliche lokale Vorgaben sind [Building Reusable UI Components](../../.agents/skills/building-reusable-ui-components/SKILL.md), [Designing Editorial Interfaces](../../.agents/skills/designing-editorial-interfaces/SKILL.md) und dessen [Designsystem](../../.agents/skills/designing-editorial-interfaces/references/design-system.md).

## 3. Einordnung der Referenzen

Die [shadcn-Referenz](https://ui.shadcn.com/docs/components/base/table) zeigt die Komposition semantischer Tabellenteile. Für diesen Vorschlag ist daran die Nähe zur HTML-Struktur relevant. React-Komponenten, Klassenlisten und Quellcode werden nicht übernommen.

Die [TanStack-Übersicht](https://tanstack.com/table/latest/docs/overview) beschreibt die Trennung von Datenlogik, Zustand und Darstellung. Diese Trennung dient als Architekturprinzip. Die eigene Umsetzung beschränkt sich auf die vereinbarten lokalen Funktionen; sie übernimmt weder TanStack-APIs noch dessen Funktionsumfang oder Implementierung.

Die Entscheidung für native Tabellen mit eigenständigen Bedienelementen folgt dem [WAI Table Pattern](https://www.w3.org/WAI/ARIA/apg/patterns/table/). Sortierbare Spalten verwenden einen Button im Spaltenkopf und `aria-sort` am aktiven Header, entsprechend dem [WAI-Beispiel für sortierbare Tabellen](https://www.w3.org/WAI/ARIA/apg/patterns/table/examples/sortable-table/).

## 4. Aufbau und Verantwortlichkeiten

### 4.1 Semantische Tabellenbausteine

Die zustandslosen Bausteine bilden HTML direkt ab. Sie sind auch ohne `DataTable` verwendbar, etwa für eine kleine statische Vergleichstabelle oder Tabellen mit manuell zusammengesetzten Zellen.

| Komponente      | HTML und Verantwortung                                                                  |
| --------------- | --------------------------------------------------------------------------------------- |
| `TableRoot`     | `table`; native Attribute, Basisdarstellung und stabiler Komponentenmarker.             |
| `TableViewport` | `div`; begrenzter horizontaler Scrollbereich mit zugänglichem Namen und Tastaturzugang. |
| `TableCaption`  | `caption`; sichtbare Beschreibung der Tabelle.                                          |
| `TableHeader`   | `thead`; Gruppe der Kopfzeilen.                                                         |
| `TableBody`     | `tbody`; Gruppe der Datenzeilen.                                                        |
| `TableFooter`   | `tfoot`; optionaler, ausdrücklich vom Verbraucher bereitgestellter Tabellenfuß.         |
| `TableRow`      | `tr`; Zeilenstruktur und ein visueller Auswahlzustand. Kein eingebauter Zeilenklick.    |
| `TableHead`     | `th`; Spalten- oder Zeilenüberschrift mit explizitem `scope`.                           |
| `TableCell`     | `td`; Inhalt, logische Textausrichtung und optional technische Typografie.              |

Die Struktur bleibt gültiges HTML: Caption vor Kopf und Körper; Zeilen innerhalb von Abschnittselementen; Zellen unmittelbar innerhalb von Zeilen. Der Viewport steht außerhalb der Tabelle. Toolbar, Ergebnisstatus und Pagination stehen außerhalb von `table`.

Die Bausteine brauchen keinen gemeinsamen reaktiven Zustand. Es wird deshalb kein Context, keine Registrierung von Kindkomponenten und kein zusätzlicher Controller eingeführt. Ihre Eltern-Kind-Beziehungen werden dokumentiert; die Bausteine korrigieren ungültiges Verbraucher-Markup nicht durch DOM-Umbauten.

### 4.2 Generische `DataTable<T>`

`DataTable` komponiert die Tabellenbausteine und vorhandene UI-Primitives. Sie verantwortet:

- Standard-Toolbar mit globaler Suche, aktivierten Spaltenfiltern und „Clear filters“;
- Spaltenüberschriften und Sortieraktionen;
- lokale Filterung, stabile Sortierung und Seitenausschnitt;
- optionale Checkbox-Spalte, Auswahlstatus und „Clear selection“;
- Ergebnisbereich, Seitengröße und vorhandene Pagination-Komponenten;
- Lade-, Leer-, Kein-Treffer- und Fehlerdarstellung.

API-Aufrufe, Routing, Berechtigungen, persistente Speicherung, Fachmodelle und fachliche Sammelaktionen gehören zum Verbraucher außerhalb von `components/ui`. Ein Snippet kann beispielsweise eine bestehende `Button`- oder `DropdownMenu`-Komponente in einer Zelle rendern, ohne deren Geschäftslogik in die Tabelle zu verschieben.

### 4.3 Kleines Modul für das lokale Datenmodell

`data-table-model.ts` enthält reine, typisierte Funktionen für Validierung, Zustandsübergänge, Filterung, Vergleich, Seitenausschnitt und Auswahlmengen. Es importiert keine Browser- oder Svelte-Laufzeit-APIs und verändert weder Zeilen noch Spalten noch übergebene Arrays.

`DataTable.svelte` hält genau einen Quellzustand. Gefilterte Zeilen, sortierte Zeilen, aktuelle Seite, Ergebnisanzahl und effektive Auswahl werden daraus mit `$derived` beziehungsweise `$derived.by` berechnet. Es gibt keine zweite Zustandskopie im Datenmodell und keine Synchronisierung mit `$effect`.

## 5. Geplante öffentliche API

Die folgenden Tabellen beschreiben den Vertrag. Sie stellen noch keinen implementierten Quellcode dar.

### 5.1 Native Bausteine

- Props werden aus den jeweiligen Typen in `svelte/elements` abgeleitet: beispielsweise `HTMLTableAttributes`, `HTMLThAttributes`, `HTMLTdAttributes` und `HTMLAttributes<HTMLTableRowElement>`. Für die übrigen Tags kann `SvelteHTMLElements` verwendet werden.
- `children` ist ein typisiertes Svelte-Snippet. Inhalt wird über `{@render ...}` ausgegeben.
- Native Attribute, Ereignis-Callbacks, `class`, ARIA und zusätzliche Verbraucher-`data-*` werden an das jeweilige native Element weitergereicht.
- Eigene Props, die native Namen ersetzen, werden im nativen Basistyp ausdrücklich mit `Omit` ausgenommen.
- `TableHead` erhält standardmäßig `scope="col"`; Zeilenüberschriften verwenden `scope="row"`. Native `colspan`, `rowspan`, `headers` und `id` bleiben verfügbar.
- `TableHead` und `TableCell` unterstützen `align="start" | "end"`; `TableCell` zusätzlich `valueKind="text" | "record"`. Standard ist normaler Text am logischen Anfang.
- `TableRow` unterstützt `selected?: boolean` ausschließlich als Darstellungshilfe. Die tatsächliche Auswahlsemantik liefert die native Checkbox.
- `TableViewport` erwartet einen zugänglichen Namen über `aria-label` oder `aria-labelledby`. Er ist mit `tabindex="0"` per Tastatur erreichbar und als benannter Bereich erkennbar.
- DOM-Refs werden erst aufgenommen, wenn ein konkreter Fokus- oder Integrationsbedarf vorliegt. Es wird kein vorsorglicher Ref für jedes Element eingeführt.

Verbraucherklassen werden mit den Basisklassen zusammengeführt. Komponentenmarker und tatsächlich abgeleitete Zustandsattribute bleiben maßgeblich; ihr Schutz darf andere native Attribute nicht verschlucken.

### 5.2 `DataTable<T>`

| Prop              | Vertrag                                                                                                                                                                           |
| ----------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `data`            | Vollständiger lokaler Datenbestand als `readonly T[]`. Bereits serverseitig paginierte Teilmengen erfüllen diesen Vertrag nicht.                                                  |
| `columns`         | Nicht leere, geordnete Liste typisierter `DataTableColumn<T>`.                                                                                                                    |
| `getRowId`        | Pflichtfunktion von `T` zu einer nicht leeren, im Datenbestand eindeutigen und dauerhaft stabilen String-ID. Array-Indizes sind kein Ersatz.                                      |
| `label`           | Pflichttext für die sichtbare Caption und den Namen des Scrollbereichs.                                                                                                           |
| `state`           | Optionaler vollständiger `DataTableState`; mit `bind:state` anschließbar. Ohne Übergabe gehört der Standardzustand der Komponente.                                                |
| `onStateChange`   | Optionale Benachrichtigung mit dem vollständigen nächsten Zustand und einem typisierten Änderungsgrund.                                                                           |
| `selection`       | Optionales Objekt mit `getLabel(row): string` für zugängliche Zeilennamen und optional `canSelect(row): boolean`. Ohne dieses Objekt gibt es keine Auswahlspalte.                 |
| `searchable`      | Aktiviert die globale Suche; Standard `true`. Durchsucht nur dafür freigegebene Datenspalten.                                                                                     |
| `pageSizeOptions` | Positive, eindeutige Seitengrößen; Standard `10`, `25`, `50`. Eine extern gesetzte gültige aktuelle Größe bleibt als zusätzliche Option bedienbar.                                |
| `loading`         | Optionaler externer Ladezustand; Standard `false`. Löst keine Datenabfrage aus.                                                                                                   |
| `error`           | Optionaler englischer Fehlertext; Standard `null`. Datenbeschaffung und Fehlerbehebung bleiben beim Verbraucher.                                                                  |
| `actions`         | Optionales Toolbar-Snippet mit effektivem Zustand, ausgewählten IDs und den aktuell vorhandenen ausgewählten Zeilen.                                                              |
| `emptyContent`    | Optionales Inhaltssnippet für einen leeren Datenbestand oder fehlende Filtertreffer; erhält den jeweiligen Grund.                                                                 |
| `errorContent`    | Optionales Inhaltssnippet für den Fehlerbereich, etwa mit einer vom Verbraucher verantworteten Wiederholungsaktion.                                                               |
| `tableAttributes` | Typisierte native Attribute für die innere Tabelle. Inhalt, interner Marker und intern verwaltete Zustandsattribute werden ausgenommen; Beschreibungs-IDs werden zusammengeführt. |
| Native Root-Props | Native Attribute, Ereignisse, `class`, ARIA und zusätzliche `data-*` beziehen sich auf den äußeren `div` der Datentabelle.                                                        |

Das äußere Element und die innere Tabelle erhalten eindeutig getrennte Attribute. Ein am äußeren Element gesetztes `id` wird nicht zusätzlich auf die Tabelle kopiert. Interne Beziehungen verwenden SSR-stabile IDs aus `$props.id()`.

`bind:state` ist der vorgesehene Anschluss an externen Zustand. `onStateChange` dient zur Benachrichtigung und darf nicht als zweiter, konkurrierender Speicher verwendet werden. Ein ausschließlich von Callback-Antworten kontrollierter Modus mit asynchroner Zustandsfreigabe ist nicht Bestandteil dieser Version.

### 5.3 Spaltendefinition

| Feld            | Bedeutung                                                                                                                                                                            |
| --------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `id`            | Eindeutige, stabile String-ID für Zustand, Filter und Render-Schlüssel.                                                                                                              |
| `label`         | Verständlicher englischer Spaltenname; auch bei eigener Zellausgabe vorhanden.                                                                                                       |
| `getValue(row)` | Zugriff auf den unverformatierten Vergleichs- und Suchwert: `string`, endliche `number`, `null` oder `undefined`.                                                                    |
| `cell`          | Optionales `Snippet<[T]>` für typisierte eigene Zellinhalte. Ohne Snippet wird der Wert als Text ausgegeben; fehlende Werte erhalten eine zugänglich erläuterte neutrale Markierung. |
| `sort`          | Optional `"text"`, `"number"` oder ein typisierter Zeilenvergleich `(left: T, right: T) => number`. Ohne Angabe ist die Spalte nicht sortierbar.                                     |
| `searchable`    | Optionaler Ausschluss aus der globalen Suche. Datenspalten werden standardmäßig durchsucht.                                                                                          |
| `filter`        | Optional Textfilter oder Auswahlfilter mit expliziten `{ value, label }`-Optionen. Kein automatisches Ableiten fachlicher Filtermöglichkeiten.                                       |
| `align`         | `"start"` oder `"end"`; Zahlen können am Ende ausgerichtet werden.                                                                                                                   |
| `valueKind`     | `"text"` oder `"record"`; Monospace bleibt technischen Werten vorbehalten.                                                                                                           |

Es gibt Daten- und reine Darstellungsspalten als typisierte Union. Eine reine Darstellungsspalte benötigt ein `cell`-Snippet, aber keinen `getValue`-Accessor. Sie kann nicht versehentlich an Sortierung, Suche oder Filterung teilnehmen. Das eignet sich beispielsweise für eine Aktionsspalte.

Datumssortierung verwendet numerische Zeitwerte aus `getValue`, während das Zell-Snippet den lesbaren Zeitpunkt rendert. Objekte und formatierte Währungen werden nicht implizit in Such- oder Sortierwerte umgewandelt. Für abweichende Fachregeln kann ein expliziter Vergleich bereitgestellt werden.

Mehrstufige Header, dynamische Spaltenmodelle und eine separate generische Typvariable pro Zellwert werden für diese Version nicht benötigt. `T` bleibt in Accessors, Snippets und Auswahl-Callbacks durchgehend erhalten; `any` wird dafür nicht eingeführt.

## 6. Zustand und Datenablauf

### 6.1 Eine Zustandsquelle

`DataTableState` enthält ausschließlich folgende Quellwerte:

| Feld            | Standard und Bedeutung                                                                    |
| --------------- | ----------------------------------------------------------------------------------------- |
| `sorting`       | `null` oder `{ columnId, direction }` mit der Richtung `"ascending"` oder `"descending"`. |
| `query`         | Leerer String; Eingabe der globalen Suche.                                                |
| `columnFilters` | Leeres Record von Spalten-ID zu String; leere Werte entsprechen keinem aktiven Filter.    |
| `page`          | `1`; einsbasiert passend zur vorhandenen Pagination.                                      |
| `pageSize`      | `25`; positive ganze Zahl.                                                                |
| `selectedIds`   | Leeres String-Array ohne Duplikate. Speicherung über IDs statt Zeilenreferenzen.          |

Eine kleine exportierte Funktion `createDataTableState(overrides?)` erstellt vollständige, voneinander unabhängige Anfangszustände. Sie erleichtert gebundene Verbraucher und explizite Resets, ohne einen Store oder eine Controller-Klasse einzuführen.

Alle akzeptierten Bedienaktionen erzeugen einen neuen vollständigen Zustand. Die Komponente weist diesen einmal zu und meldet ihn anschließend höchstens einmal über `onStateChange`. Ohne wirksame Änderung erfolgt kein Callback. Die Gründe sind feste englische Tokens: `sort`, `search`, `filter`, `page`, `page-size`, `selection`, `clear-filters` und `clear-selection`.

### 6.2 Reihenfolge der Berechnung

1. Spaltendefinitionen, Zeilen-IDs und Zustand auf gültige Integrationswerte prüfen.
2. Globale Suche und aktive Spaltenfilter auf den vollständigen Datenbestand anwenden.
3. Die gefilterten Zeilen stabil sortieren; ohne Sortierung gilt ihre ursprüngliche Eingabereihenfolge.
4. Ergebnisanzahl, Seitenanzahl und effektive Seite ableiten.
5. Den Seitenausschnitt bilden und nur diese Zeilen rendern.
6. Effektive Auswahl sowie Header-Checkbox aus IDs, vorhandenem Datenbestand, Auswahlberechtigung und sichtbarer Seite ableiten.

Die Ausgangsdaten werden nie mit `sort` oder anderen verändernden Operationen bearbeitet. Eine stabile sekundäre Ordnung nach ursprünglicher Eingabeposition verhindert springende Zeilen bei gleichen Vergleichswerten. Array-Positionen dienen nur diesem Gleichstandsvergleich, niemals der Identität.

### 6.3 Sortierung

- Aktivierung derselben Spalte wechselt von unsortiert zu aufsteigend, dann absteigend und zurück zu unsortiert. Eine andere Spalte beginnt aufsteigend.
- Textvergleiche verwenden einen expliziten `Intl.Collator("en", { numeric: true, sensitivity: "base" })`. Dadurch hängt die Standardordnung nicht von der Browser-Sprache ab.
- Numerische Sortierung vergleicht numerische Werte. Nicht endliche Zahlen erfüllen den Accessor-Vertrag nicht.
- Fehlende Werte stehen in beiden Richtungen am Ende; ihre Position darf nicht versehentlich durch das Umkehren des gesamten Vergleichsergebnisses wechseln.
- Ein eigener Vergleich muss deterministisch und seiteneffektfrei sein. Die Tabelle wendet Richtung und stabilen Gleichstandsvergleich an.
- Genau der aktive Spaltenkopf erhält `aria-sort="ascending"` oder `"descending"`. Auf anderen Headern wird das Attribut weggelassen.

### 6.4 Filterung

- Globale Suche ist eine Teilstringsuche über ausdrücklich durchsuchbare Werte. Ein Treffer in einer dieser Spalten genügt.
- Textfilter einer Spalte verwenden dieselbe Teilstring-Regel. Auswahlfilter vergleichen den unformatierten Stringwert mit einer expliziten Option. Optionswerte sind eindeutig und nicht leer; der leere Filterwert steht für „All“.
- Globale Suche und einzelne Spaltenfilter werden durch UND verknüpft.
- Eingaben werden für den Vergleich getrimmt und ohne Beachtung der Groß-/Kleinschreibung verglichen. Die Eingabe selbst bleibt im Zustand erhalten. Es gibt keine implizite Akzententfernung oder unscharfe Suche.
- Fehlende Werte werden als leerer Suchtext behandelt. Zahlen werden über ihre unverformatierten Stringwerte durchsucht; sichtbare Formatierung und Datenwert bleiben getrennt.
- „Clear filters“ setzt Suchtext und Spaltenfilter zurück. Sortierung und Seitengröße bleiben erhalten.
- Textänderungen wirken unmittelbar auf lokale Daten. Debouncing, Timer und zusätzliche Synchronisierungszustände werden nicht eingebaut.

### 6.5 Pagination und Übergänge

| Aktion                                       | Auswirkung auf die Seite      | Auswirkung auf die Auswahl                                                              |
| -------------------------------------------- | ----------------------------- | --------------------------------------------------------------------------------------- |
| Sortierung ändern                            | Zurück zu Seite `1`.          | Beibehalten.                                                                            |
| Suchtext oder Spaltenfilter ändern           | Zurück zu Seite `1`.          | Vollständig leeren, damit die neue Ergebnismenge mit einer eindeutigen Auswahl beginnt. |
| „Clear filters“                              | Zurück zu Seite `1`.          | Vollständig leeren.                                                                     |
| Seite wechseln                               | Gültige Zielseite übernehmen. | Seitenübergreifend beibehalten.                                                         |
| Seitengröße ändern                           | Zurück zu Seite `1`.          | Beibehalten.                                                                            |
| Einzelne Zeile oder aktuelle Seite auswählen | Seite beibehalten.            | Nur die betroffenen auswählbaren IDs ändern.                                            |
| „Clear selection“                            | Seite beibehalten.            | Auch auf anderen Seiten ausgewählte IDs entfernen.                                      |

Die vorhandene Pagination wird im Button-Modus verwendet. Es gibt keine URL-Verwaltung und keinen Link-Modus innerhalb der ersten `DataTable`-Version. `totalItems` entspricht intern der gefilterten Ergebnisanzahl und ist kein extern übergebbarer Server-Gesamtwert.

Bei null Treffern bleibt die angeforderte Quellseite konzeptionell `1`; es gibt keine sichtbare Datenseite. Die Pagination-Familie kann intern `0` als effektive leere Seite verwenden. Die Anzeige lautet `0–0 of 0`, und Seitenaktionen sind nicht verfügbar. Dieser Wert wird nicht als `page=0` in den öffentlichen Zustand geschrieben.

Bei schrumpfenden Daten wird die effektive Seite auf die letzte gültige Seite begrenzt. Der Datenwechsel allein verändert keine gebundenen Props und löst keinen Benutzer-Callback aus. Die nächste akzeptierte Bedienaktion übernimmt den normalisierten Zustand. Ein Verbraucher kann bei einem fachlichen Datenwechsel Daten und Zustand explizit gemeinsam zurücksetzen.

### 6.6 Zeilenauswahl

- Die erste Spalte enthält native Checkboxen mit Namen wie „Select Label A“. Ein Zeilenklick wählt nichts aus; enthaltene Links und Buttons behalten ihr Verhalten.
- „Select all rows on this page“ wirkt ausschließlich auf die aktuell sichtbaren und auswählbaren Zeilen nach Filterung, Sortierung und Pagination.
- Die Kopf-Checkbox ist aktiviert, wenn alle auswählbaren Zeilen dieser Seite ausgewählt sind. Sie ist gemischt, wenn nur ein Teil ausgewählt ist, und deaktiviert, wenn keine auswählbare Zeile vorhanden ist.
- Aktivierung einer gemischten Kopf-Checkbox ergänzt die übrigen auswählbaren IDs dieser Seite. Aktivierung einer vollständig ausgewählten Seite entfernt nur deren IDs; andere Seiten bleiben ausgewählt.
- `canSelect` kann einzelne Zeilen ausschließen. Diese erhalten eine deaktivierte Checkbox und einen erkennbaren nicht verfügbaren Zustand.
- Die Gesamtanzeige „N selected“ zählt die effektive Auswahl im gesamten aktuellen Datenbestand. Eine ergänzende Angabe „M on this page“ macht den Seitenbezug klar. „Clear selection“ bleibt erreichbar, solange eine effektive Auswahl besteht.
- Intern kann ein abgeleitetes `Set` Mitgliedschaften beschleunigen. Der öffentliche Zustand bleibt ein serialisierbares Array; dieses wird weder direkt mutiert noch mit doppelten IDs befüllt.

IDs, die im vollständig übergebenen Datenbestand fehlen oder aktuell nicht auswählbar sind, zählen nicht zur effektiven Auswahl und werden nicht an Aktions-Snippets als ausgewählte Zeilen übergeben. Beim nächsten Benutzerübergang wird dieser bereinigte Zustand übernommen.

Eine externe Datenänderung allein schreibt die angeforderten IDs nicht zurück. Erscheint dieselbe stabile ID vor dem nächsten Übergang wieder, gilt sie wieder als derselbe angeforderte Datensatz. Für einen anderen fachlichen Datenbestand muss der Verbraucher den Zustand ausdrücklich zurücksetzen. Dieser Vertrag vermeidet versteckte Synchronisierung und wird in Storybook dokumentiert.

### 6.7 Ungültige Integrationswerte

Leere oder doppelte Zeilen-/Spalten-IDs, eine leere Spaltenliste, Seitengrößen außerhalb positiver ganzer Zahlen, nicht unterstützte Filterwerte sowie Sortier-/Filterverweise auf ungeeignete oder unbekannte Spalten führen zu konkreten englischen Entwicklerfehlern. Sie werden nicht stillschweigend durch Index-IDs oder erratene Regeln ersetzt.

Gültige, aber nach einer Datenänderung zu große Seiten werden wie oben beschrieben begrenzt. Unsortierte oder nicht filternde Spalten bleiben ohne irreführende Bedienelemente. Werden Spaltendefinitionen extern ausgetauscht, muss der Verbraucher inkompatible Sortier- und Filterzustände gleichzeitig zurücksetzen.

## 7. Gestaltung und responsives Verhalten

### 7.1 Darstellung im vorhandenen Designsystem

| Aspekt      | Festlegung                                                                                                                                                                                                   |
| ----------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Flächen     | `reading-room` als Grundlage, `archive-layer` für Tabellenkopf und zurückhaltende Zustandsflächen.                                                                                                           |
| Text        | `reading-ink` für Inhalte, `marginal-note` für unterstützende Angaben.                                                                                                                                       |
| Struktur    | Feine horizontale Trennlinien mit `border-trace`; keine zusätzliche Kartenfläche, Schatten oder dekorativen Verläufe.                                                                                        |
| Interaktion | `register` für Aktionen, Links und Fokus. Allgemeine Datenauswahl erhält keine zeitliche Bedeutung durch Time Copper.                                                                                        |
| Auswahl     | Sichtbare Checkbox mit Häkchen beziehungsweise Mischzustand, dezente Fläche und Register-Markierung an der Anfangskante. Die Markierung verändert die Zellbreiten nicht.                                     |
| Typografie  | Source Sans 3 für Tabellen und Bedienelemente; IBM Plex Mono nur bei `valueKind="record"`. Zahlen erhalten tabellarische Ziffern. Newsreader bleibt übergeordneten redaktionellen Überschriften vorbehalten. |
| Rhythmus    | Standardzellen mit 12–16px vertikalem Abstand auf dem vorhandenen 4px-Raster. Buttons und Checkbox-Labelbereiche behalten mindestens 44 × 44px Bedienfläche.                                                 |
| Zustände    | Hover verändert Fläche oder Unterstreichung ohne Layoutsprung. Fokus behält die globale durchgehende 3px-Kontur mit 2px Abstand.                                                                             |

Die Umsetzung nutzt Tailwind CSS v4 mit vorhandenen semantischen Tokens. Kleine komponentenbezogene CSS-Regeln dürfen Tabellenlayout und verschachtelte Zustände ausdrücken, analog zu vorhandenen Primitives. Neue Paletten, konkurrierende Fonts und globale Tabellenregeln werden nicht eingeführt.

Light und Dark verwenden dieselben semantischen Rollen. Die Tabelle bleibt schattenlos und überwiegend rechteckig. Es gibt zunächst keine Varianten für kompakte Dichte, Zebra-Streifen oder dekorative Rahmen.

### 7.2 Desktop und Mobile

- Die Tabelle nutzt den verfügbaren Platz und intrinsische Spaltenbreiten; es gibt keine feste Desktop-Mindestbreite für alle Verbraucher.
- Lange Texte und URLs können innerhalb der Zellen mit `overflow-wrap: anywhere` umbrechen. Die vollständigen Werte bleiben zugänglich und kopierbar.
- Falls Spalten gemeinsam mehr Breite benötigen, scrollt ausschließlich `TableViewport` horizontal. Der äußere Bereich verwendet `min-inline-size: 0` und eine begrenzte Maximalbreite, damit das Dokument selbst nicht überläuft.
- Toolbar, Filter, Auswahlstatus und Pagination brechen außerhalb des Scrollbereichs um. Auf schmalen Geräten stehen beschriftete Felder untereinander; Bedienelemente werden nicht auf unbedienbare Breiten zusammengedrückt.
- Spalten werden auf kleinen Geräten weder verborgen noch in visuell umsortierte Karten verwandelt. Tabellenüberschriften und Beziehungen bleiben erhalten.
- Fokusmarkierungen innerhalb des Scrollbereichs erhalten genügend Abstand und dürfen an dessen Kanten nicht abgeschnitten werden. Der Scrollbereich selbst hat einen sichtbaren Fokus.
- Schmale Tabellen müssen bei 320 CSS-Pixeln ohne horizontales Scrollen auskommen können; breite echte Datentabellen dürfen innerhalb ihres benannten Bereichs scrollen. 200% Textvergrößerung darf keine Inhalte oder Aktionen verlieren lassen.

Sticky Header, eingefrorene Spalten und automatische Overflow-Beobachtung werden zunächst nicht benötigt. Damit entfallen zusätzliche Messzustände und Observer.

## 8. Barrierefreiheit und Interaktion

Die Tabelle behält native Tabellenrollen. Sie erhält kein `role="grid"`, keine Pfeiltastennavigation zwischen Zellen und kein Roving-Tabindex. Buttons, Checkboxen, Eingaben und Links sind normale Tabstopps; statische Zellen sind nicht fokussierbar. Dies entspricht dem [WAI-Tabellenmuster](https://www.w3.org/WAI/ARIA/apg/patterns/table/).

- Sichtbare Caption und korrekte Spalten-/Zeilenüberschriften beschreiben die Inhalte. Zusätzliche Beschreibungs-IDs werden mit Verbraucher-IDs zusammengeführt.
- Sortieraktionen verwenden den vorhandenen `Button` mit `type="button"`. Spaltenname, Richtungsanzeige und ein kurzer zugänglicher Hinweis erklären die nächste Aktion. Icons sind dekorativ; ausschließlich direkte Lucide-Modulimporte werden verwendet.
- Der Sortierzustand gehört an `th`, entsprechend dem [WAI-Sortierbeispiel](https://www.w3.org/WAI/ARIA/apg/patterns/table/examples/sortable-table/). Er wird nicht nur durch Farbe oder ein Icon transportiert.
- Native Checkboxen liefern Auswahlsemantik, Space-Bedienung und Mischzustand. `checked` und `indeterminate` werden aus dem effektiven Zustand gespeist. Svelte unterstützt diese Eigenschaften; es wird dafür kein spiegelnder Effekt eingeführt.
- `aria-selected` wird nicht auf Zeilen einer normalen HTML-Tabelle gesetzt. `data-selected` dient dem Styling; der native Checkbox-Zustand bleibt semantisch maßgeblich.
- Suchfelder und Auswahlfilter haben sichtbare Labels. Native Eingabeereignisse und Texterfassung bleiben erhalten; keine globale Tastaturbehandlung fängt Zeichen ab.
- Eine kompakte Region mit `role="status"` meldet Ergebnisbereich und Auswahländerungen höflich. Der gesamte Tabellenkörper ist keine Live-Region. Häufige doppelte Meldungen werden vermieden.
- Eingaben behalten beim Filtern ihren Fokus; stabile Zeilen- und Spalten-Keys vermeiden unnötiges Neuerzeugen. Ein weggeblendeter Pagination-Button darf den Fokus nicht verlieren lassen; die Datentabelle verwendet deshalb eine stabil vorhandene, bei Bedarf deaktivierte Pagination-Leiste.
- Kontrast wird in beiden Themes anhand des Designsystems geprüft: mindestens 4,5:1 für normalen Text sowie 3:1 für relevante nicht textuelle Zustände. Fokus und Auswahl dürfen nicht nur über Farbe erkennbar sein.
- Bewegung bleibt auf kurze vorhandene Zustandsübergänge begrenzt; `prefers-reduced-motion` wird respektiert. Es gibt keine laufende Skeleton-Animation.

### Daten- und Rückmeldungszustände

| Zustand             | Geplante Darstellung                                                                                                                                                                   |
| ------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Bereit              | Daten, Ergebnisbereich und verwendbare Bedienelemente.                                                                                                                                 |
| Leerer Datenbestand | Eine semantische Zeile mit einer Zelle über alle sichtbaren Spalten: „No data available“.                                                                                              |
| Keine Filtertreffer | Gleiche gültige Struktur mit „No results found“; „Clear filters“ bleibt außerhalb der Tabelle bedienbar.                                                                               |
| Laden               | `aria-busy="true"`, verständlicher Ladetext und dekorative vorhandene Skeletons. Die internen Datenaktionen sind deaktiviert; der Ladeinhalt ersetzt die Datenzeilen.                  |
| Fehler              | Flacher Bereich mit Icon, „Unable to display data“ und konkretem Fehlertext; optionales Verbraucher-Snippet für eine Aktion. Der Fehler wird nicht als leerer Datenbestand ausgegeben. |

Fehler hat Vorrang vor Laden; Laden hat Vorrang vor Leer-/Ergebnisdarstellung. Der `colspan` von Rückmeldungszeilen berücksichtigt die optionale Auswahlspalte. Tabellenkopf und äußere Bedienelemente bleiben strukturell stabil. Bei extern ausgelösten Lade-/Fehlerwechseln wird Fokus nicht ungefragt verschoben; Verbraucheraktionen und manuelle Browserprüfung berücksichtigen den Fall, dass eine zuvor fokussierte Zelle entfällt.

## 9. Stabile Hooks und Dokumentation

Geplante Hooks sind `data-component="table"` am nativen Tabellenroot, `data-component="data-table"` am äußeren Datentabellenroot sowie `data-table-viewport`, `data-table-header`, `data-table-body`, `data-table-footer`, `data-table-row`, `data-table-head`, `data-table-cell` und `data-table-selection` an den entsprechenden Elementen.

Die Datentabelle erhält `data-state="ready" | "loading" | "empty" | "no-results" | "error"`. Zeilen können `data-selected` und `data-disabled` tragen; Header `data-sort="ascending" | "descending" | "none"`. Ergänzende Zustandswerte bleiben stabile englische Tokens. Diese Hooks ersetzen keine nativen Attribute und kein ARIA.

Öffentliche Typen und nicht offensichtliche Regeln werden mit präzisen englischen `/** ... */`-Kommentaren dokumentiert. Lokale Erläuterungen wie die Behandlung fehlender Sortierwerte oder der Seitenbezug der Header-Checkbox verwenden kurze `//`-Kommentare. Offensichtliche Zuweisungen werden nicht kommentiert. Storybook erklärt API, Verantwortungsgrenzen, Snippets und Zustandsverhalten aus Verbrauchersicht.

## 10. Vorgesehene Dateien

Die folgende Struktur wird erst nach Freigabe angelegt:

```text
frontend/src/lib/components/ui/table/
  DataTable.svelte
  TableRoot.svelte
  TableViewport.svelte
  TableCaption.svelte
  TableHeader.svelte
  TableBody.svelte
  TableFooter.svelte
  TableRow.svelte
  TableHead.svelte
  TableCell.svelte
  TableSelectionControl.svelte
  data-table-model.ts
  types.ts
  index.ts

frontend/src/stories/table/
  Table.stories.svelte
  DataTable.stories.svelte
  DataTableExample.svelte
```

`TableSelectionControl` kapselt intern ausschließlich die wiederholte native Checkbox mit Label, Mischzustand und Bedienfläche. Sie wird nicht als zusätzliche allgemeine Checkbox-API exportiert. `index.ts` exportiert die Tabellenbausteine, `DataTable`, die benötigten öffentlichen Typen und `createDataTableState`.

`DataTableExample` ist eine neutrale Story-Hilfe mit eigenem Zustand je Instanz. Sie verhindert, dass verschiedene Dokumentationsbeispiele versehentlich denselben Zustand teilen. Weitere Ordner, Context-Dateien, abstrakte Basisklassen oder vorsorgliche Controller werden nicht angelegt. Falls das Rendering später tatsächlich eine klarere Aufteilung benötigt, erfolgt eine begründete kleine Extraktion innerhalb derselben Familie.

`package.json`, Lockdateien, `app.css`, Storybook-Konfiguration und bestehende Feature-Seiten müssen für diesen Vorschlag nicht geändert werden.

## 11. Storybook als Fähigkeitsdokumentation

Die Stories verwenden `defineMeta` aus `@storybook/addon-svelte-csf`, die Titel `Components/Table` und `Components/DataTable` sowie neutrale Daten wie `Label A`, `Supporting text` und `row-a`. Es werden keine realistischen Archivdatensätze, Backend-Fixtures oder Produktabläufe ergänzt.

| Story-Gruppe                    | Nachzuweisende Fähigkeit                                                                                        |
| ------------------------------- | --------------------------------------------------------------------------------------------------------------- |
| `Table / Structure`             | Caption, Header, Körper, optionaler Fuß und Zeilenüberschriften.                                                |
| `Table / Cell content`          | Text, numerische Ausrichtung, technische Werte und vorhandene Badges/Buttons als Inhalt.                        |
| `Table / Constrained width`     | Lange Inhalte, vollständige URL, Umbruch und benannter horizontaler Scrollbereich.                              |
| `DataTable / Local data`        | Vollständiger lokaler Datenbestand mit Standardpagination.                                                      |
| `DataTable / Sorting`           | Beide Richtungen, Rückkehr zur Eingabereihenfolge, gleiche und fehlende Werte, nicht sortierbare Aktionsspalte. |
| `DataTable / Filters`           | Globale Suche, Text- und Auswahlfilter, kombinierte Filter und Rücksetzen.                                      |
| `DataTable / Pagination`        | Erste/letzte Seite, Seitengrößen, eine Ergebnisseite, leeres Ergebnis und Begrenzung nach Datenwechsel.         |
| `DataTable / Selection`         | Einzel-, Seiten- und seitenübergreifende Auswahl, Mischzustand, ausgeschlossene Zeile und vollständiges Leeren. |
| `DataTable / State binding`     | `bind:state`, Callback-Grund und externe Änderung ohne zusätzlichen Callback.                                   |
| `DataTable / Data changes`      | Neue Datenreferenz, entfernte/erneut vorhandene IDs, aktualisierte Auswahlberechtigung und expliziter Reset.    |
| `DataTable / Content snippets`  | Eigene Zellen, zusätzliche Toolbar-Aktionen und angepasste Leer-/Fehlertexte.                                   |
| `DataTable / Feedback`          | Initiales Laden, Fehler, leerer Bestand und keine Filtertreffer.                                                |
| `DataTable / Responsive layout` | Schmale Toolbar, breite Tabelle, lange Labels und alle weiterhin erreichbaren Aktionen.                         |

Alle Gruppen bleiben über die bestehende Theme-Umschaltung in Light und Dark prüfbar. Kurze Hinweise dokumentieren Tab-, Enter- und Space-Bedienung sowie Fokuszustände. Es gibt keine `play`-Funktionen, Testfälle oder neuen Testabhängigkeiten.

## 12. Umsetzungsschritte nach Genehmigung

1. **Vertrag und Typen:** Familienordner anlegen, öffentliche Props, Spaltenunion, Zustand, Änderungsgründe und konkrete Fehlertexte definieren. Die vereinbarten lokalen Grenzen als API-Kommentare festhalten.
2. **Native Bausteine:** Tabelle, Viewport, Abschnittselemente und Zellen umsetzen. Native Attribute und Snippet-Komposition vollständig weiterreichen; zunächst anhand der Struktur-Stories prüfen.
3. **Lokales Datenmodell:** Validierung, Filterung, stabile Sortierung, Pagination und Auswahlmengen als reine Funktionen ergänzen. Grenzfälle anhand neutraler, manuell bedienbarer Stories sichtbar machen.
4. **Datentabelle:** Eine Zustandsquelle, vorhandene Such-/Auswahlfelder, Buttons, Pagination und interne Checkbox verbinden. Alle Übergänge atomar umsetzen und nach außen dokumentieren.
5. **Darstellung und Rückmeldungen:** Tokens, Umbruch, Auswahlmarkierungen, Lade-/Leer-/Fehlerzustände und beide Themes ausarbeiten. Fachliche Inhalte weiterhin ausschließlich über Props und Snippets erhalten.
6. **Dokumentation und Prüfung:** Story-Gruppen vervollständigen, Svelte-/TypeScript-Prüfung und Storybook-Build ausführen, integrierten Browser für visuelle und manuelle Interaktionsprüfung verwenden und Befunde beheben.
7. **Abschluss:** Diff auf versehentliche Feature-, Test- und Dependency-Änderungen prüfen. Nur zusammengehörige Dateien stagen und mit kurzen englischen semantischen Commit-Nachrichten committen.

Sinnvolle spätere Commit-Grenzen sind `feat: add semantic table primitives` und `feat: add local data table controls`, jeweils einschließlich der zugehörigen Dokumentations-Stories. Diese Bezeichnungen beschreiben die geplante Implementierung; der vorliegende Plan wird separat mit `docs:` committed.

## 13. Validierung und Abnahmekriterien

### 13.1 Aktuelle Planungsphase

Nur die Markdown-Datei wird auf konsistente Entscheidungen, nachvollziehbare Quellen, gültige lokale Verweise, Formatierung und einen sauberen Diff geprüft. Es werden keine Tests angelegt, ausgeführt, verändert oder gelöscht. Da keine Svelte-, Style- oder Build-Dateien verändert werden, gehören Compilerlauf und Browserprüfung nicht zur Validierung dieser Dokumentationsänderung.

### 13.2 Nach der späteren Implementierung

Die Vorgaben des Komponenten-Skills gelten vollständig:

1. Falls Svelte-MCP-Werkzeuge verfügbar sind, relevante Abschnitte mit `list-sections` und `get-documentation` lesen und jede geänderte `.svelte`-Datei mit `svelte-autofixer` für Svelte 5 prüfen. In dieser Planungssitzung waren keine entsprechenden MCP-Werkzeuge verfügbar; die offiziellen Svelte-Dokumentationen wurden als Grundlage verwendet.
2. Ohne Svelte MCP die [offizielle Svelte-Dokumentation zu TypeScript und Wrappern](https://svelte.dev/docs/svelte/typescript) sowie [Snippets](https://svelte.dev/docs/svelte/snippet) heranziehen und den vorhandenen Compiler verwenden. Keine Abhängigkeit allein für zusätzlichen Werkzeugzugriff installieren.
3. In `frontend` `bun run check` und anschließend `bun run build:storybook` ausführen. Diese Prüfungen ersetzen keine manuelle visuelle Kontrolle; sie sind keine Aufforderung, Tests hinzuzufügen oder auszuführen.
4. Die tatsächlich gerenderten Stories im integrierten Browser bei 320px und 390px Breite, 768px sowie mindestens 1280px Desktopbreite prüfen. Beide Themes, 200% Textvergrößerung und reduzierte Bewegung berücksichtigen.
5. Tab, Shift+Tab, Enter und Space manuell bedienen. Sortierung, Filterkombinationen, Seitengrenzen, gemischte Auswahl, Fokus nach Datenwechsel und vollständig kopierbare lange Werte nachvollziehen. Zugängliche Namen und Checkbox-Zustände anhand der Browserinformationen prüfen; ein tatsächlicher Screenreader-Check wird nur als durchgeführt gemeldet, wenn er wirklich erfolgt ist.
6. Visuelle Befunde beheben und ausschließlich betroffene Checks erneut ausführen. Im Abschluss die tatsächlich durchgeführten Prüfungen und verbleibende Einschränkungen benennen.

Die spätere Implementierung ist abnahmefähig, wenn:

- alle vier vereinbarten Datenfunktionen auf dem vollständigen lokalen Datenbestand korrekt zusammenspielen;
- Seitenauswahl keine IDs anderer Seiten unbeabsichtigt ergänzt oder entfernt;
- Inputdaten unverändert bleiben und keine Identität aus Array-Indizes entsteht;
- die wirksamen Zustände aus einer Quelle ableitbar sind und keine spiegelnden Effekte benötigen;
- native Attribute, Snippets, Verbraucherklassen und zusätzliche `data-*` erhalten bleiben;
- beide Themes, Fokus, Beschriftungen, Auswahl und Status ohne reine Farbabhängigkeit verständlich sind;
- bei 320px keine globale horizontale Überbreite entsteht und alle Aktionen erreichbar bleiben;
- die beiden vorgeschriebenen Build-/Typprüfungen bestehen und die Browserprüfung keine offenen visuellen Fehler zeigt;
- alle neuen öffentlichen Regeln professionell dokumentiert sind und keine zusätzliche Bibliothek oder Teständerung eingeführt wurde.

## 14. Bewusste Grenzen und spätere Erweiterungen

Die erste Version enthält keine serverseitige Verarbeitung, keinen Remote-/Manual-Modus, keine API-Anbindung und keine Migration bestehender Listen. Insbesondere darf die serverseitig paginierte Benutzerverwaltung nicht einfach ihre aktuelle Teilmenge an die lokale Datentabelle übergeben, weil Such-, Sortier- und Ergebnisangaben dann nur diese Teilmenge betreffen würden.

Nicht vorgesehen sind Mehrspaltensortierung, Auswahl aller gefilterten Seiten, virtuelle Zeilen, Gruppierung, Baumdaten, bearbeitbare Zellen, Drag-and-drop, Spaltenbreitenänderung, Spalten-Pinning, Export oder persistenter Zustand. Diese Funktionen werden erst bei einem konkreten Bedarf gesondert entworfen.

Lokale Filterung bearbeitet den vollständigen Bestand, Sortierung typischerweise eine gefilterte Menge mit O(n log n); gerendert wird nur die aktuelle Seite. Es wird keine ungemessene Obergrenze oder Leistungszusage für große Datensätze gemacht. Sollte ein späterer Verbraucher große oder ausschließlich serverseitig verfügbare Bestände benötigen, ist dafür ein eigener genehmigter Verarbeitungsvertrag erforderlich.

Mit der Genehmigung dieses Plans beginnt die beschriebene lokale Implementierung einschließlich Storybook und Browserprüfung. Bis dahin bleibt die Änderung auf dieses Dokument beschränkt.

## 15. Umsetzungsstand nach Genehmigung

Die Familie unter `frontend/src/lib/components/ui/table/` enthält die nativen Tabellenbausteine, die generische `DataTable<T>`, die interne native Checkbox sowie das reine lokale Datenmodell. Die API entspricht dem genehmigten Umfang: globale Suche, Text- und Auswahlfilter, stabile Einzelspaltensortierung, Pagination und Mehrfachauswahl auf der sichtbaren Seite. Zustand kann als vollständiges Objekt gebunden werden.

Die Galerien `Components/Table` und `Components/DataTable` dokumentieren die Fähigkeiten. Ein zusätzliches Defaults-Beispiel zeigt die interne Zustandsverwaltung ohne Bindung und ohne Auswahlspalte. [Die Verbraucherdokumentation](../../frontend/src/stories/table/README.md) enthält ein vollständiges Einstiegsbeispiel und erläutert Zustand, Spalten, Snippets und native Attribute.

Die visuelle Prüfung im integrierten Browser hat zu zwei gezielten Anpassungen geführt: Sortierbuttons besitzen eine lesbare Mindestbreite, und der Tabellen-Viewport begrenzt auch positionierte Inhalte durch einen eigenen Positionierungskontext. Das Beispiel für lange URLs gibt seiner eigenen Wertedarstellung eine sinnvolle Mindestbreite. Diese Inhaltsbreite wird nicht pauschal auf alle Tabellenzellen übertragen.

Durchgeführte Prüfung:

- `bun run check`: keine Svelte-/TypeScript-Fehler oder Warnungen.
- `bun run build:storybook`: erfolgreicher Build; allgemeine Hinweise des Bundlers zu Chunkgrößen.
- Integrierter Browser: Light und Dark, 320px und 390px Mobile, 768px Tablet, 1280px Desktop sowie 200 Prozent Storybook-Vorschauvergrößerung.
- Manuelle Bedienung: Sortierzyklus einschließlich fehlender Werte und stabiler Gleichstände, kombinierte globale/Spaltenfilter, Seitengrößen, Seitenwechsel, einzelne und gemischte Seitenauswahl, ausgeschlossene Zeilen, externer Reset und Callback-Anzahl.
- Datenwechsel: entfernte und erneut vorhandene IDs, geänderte Auswahlberechtigung und begrenzte effektive Seite bei schrumpfendem Datenbestand.
- Rückmeldungen: Laden, Fehler, leerer Datenbestand und keine Treffer; zugängliche Namen und native Checkbox-Zustände anhand der Browserinformationen.
- Tastatur: Enter für Sortierung, Space für Checkboxen und Pfeiltasten zum horizontalen Scrollen mit sichtbarem Fokus.

Die vorhandenen globalen Regeln für reduzierte Bewegung werden weiterverwendet; neue Animationen wurden nicht eingeführt. Eine separate Systemumschaltung für reduzierte Bewegung und ein Screenreader-Durchlauf wurden nicht durchgeführt. Die Vergrößerungsprüfung erfolgte über Storybook, nicht über eine Betriebssystem-Textskalierung.

Es wurden keine Tests angelegt, ausgeführt, verändert oder gelöscht. Die Implementierung enthält keine neue externe Bibliothek, keinen serverseitigen Verarbeitungsmodus und keine Migration der bestehenden Benutzerverwaltung.
