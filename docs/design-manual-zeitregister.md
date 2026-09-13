# Zeitregister — Design Manual für ein modernes Webarchiv

Version 1.0 · 9. September 2026

## 1. Designidee und visuelle Leitprinzipien

**Zeitregister** versteht das Webarchiv als digitalen Lesesaal: einen Ort, an dem gespeicherte Zustände nicht spektakulär inszeniert, sondern präzise erschlossen werden. Die Atmosphäre ist ruhig, verlässlich und konzentriert. Warme, papiernahe Grundtöne geben dem Thema Erinnerung eine menschliche Qualität; klare Linien, tabellarische Ziffern und eine kontrollierte Informationsdichte vermitteln technische Sorgfalt.

Zeit erscheint nicht als dekorative Uhr oder nostalgisches Motiv. Sie wird durch Registerlinien, fortlaufende Nummern, wiederkehrende Datumsformate, vertikale Einschnitte und übereinanderliegende Informationsschichten sichtbar. Ein Snapshot wirkt wie ein belastbarer Datensatz mit Provenienz, nicht wie eine beliebige Inhaltskarte.

### Leitprinzipien

1. **Lesen vor Staunen.** Inhalt, Quelle und Zeitpunkt erhalten Vorrang vor visueller Effekthascherei.
2. **Zeit als Struktur.** Achsen, Taktmarken, Jahresbänder und Datumsnotationen bilden das wiederkehrende Motiv.
3. **Editoriale Hierarchie.** Serifenschrift markiert Bedeutung und Erinnerung; Sans Serif organisiert Bedienung; Monospace belegt technische Genauigkeit.
4. **Flächen bleiben ruhig.** Wenige Farbebenen, feine Konturen und kaum Schatten erzeugen Tiefe ohne Kartenstapel-Ästhetik.
5. **Zustände sind beweisbar.** Auswahl, Fokus, Fehler und Erfolg werden immer durch mindestens zwei Signale vermittelt.
6. **Dauerhaft statt modisch.** Keine Verläufe als Dekoration, kein Glassmorphism, keine Neonfarben und keine dauernd laufende Animation.

Im Unterschied zu gewöhnlichen SaaS-Dashboards arbeitet Zeitregister nicht mit einer Ansammlung gleichgewichtiger KPI-Karten. Die Oberfläche folgt eher einer Publikation: große Einstiege, klare Register, horizontale Leselinien, wenige begrenzte Module und eine sichtbare Beziehung zwischen Quelle, Zeitpunkt und archiviertem Objekt.

---

## 2. Light Mode — „Lesesaal“

Die zentrale Palette umfasst fünf Farben. Transparenzen und Mischfarben dürfen daraus abgeleitet werden; zusätzliche Statusfarben bleiben sparsame Hilfsfarben und gehören nicht zur Markenpalette.

### Morgenblatt

- **HEX:** `#F7F4EC`
- **RGB:** `247, 244, 236`
- **Charakter:** Warmes, mineralisches Off-White; wirkt hochwertiger und weniger steril als Reinweiß.
- **Einsatz:** Große Leseflächen, Seitenraum, Formulare, Vorschauumgebungen und helle Typografie auf gefüllten Aktionen.
- **Kombinationen:** Ideal mit Chroniktinte; Registerblau und Zeitkupfer funktionieren darauf als Text, Linie oder kompakte Füllung.

### Aktenstaub

- **HEX:** `#E5DFD2`
- **RGB:** `229, 223, 210`
- **Charakter:** Gedämpftes Beige-Grau mit der Anmutung geordneter Papierlagen, ohne nostalgisch zu werden.
- **Einsatz:** Sekundäre Flächen, Tabellenköpfe, Hover-Bereiche, Filtergruppen, inaktive Kalenderfelder und ruhige Trennzonen.
- **Kombinationen:** Chroniktinte liefert maximale Lesbarkeit; Registerblau eignet sich für Links und aktive Zeichen; Zeitkupfer nur gezielt für markierte Zeitpunkte.

### Chroniktinte

- **HEX:** `#18201F`
- **RGB:** `24, 32, 31`
- **Charakter:** Fast schwarzes, leicht grünliches Anthrazit; sachlich, tief und weniger hart als reines Schwarz.
- **Einsatz:** Fließtext, Überschriften, Icons, Hauptkonturen, sekundäre Buttons und technische Daten.
- **Kombinationen:** Primär auf Morgenblatt und Aktenstaub. Nicht als gleichgewichtige Fläche neben Registerblau einsetzen, da beide visuell zu dicht werden.

### Registerblau

- **HEX:** `#2E52C7`
- **RGB:** `46, 82, 199`
- **Charakter:** Präzises, leicht gedämpftes Kobaltblau; digital und eindeutig, aber nicht neonhaft.
- **Einsatz:** Hauptaktionen, Links, Fokusindikatoren, aktive Navigation, ausgewählte Filter und interaktive Timeline-Marken.
- **Kombinationen:** Als Text oder Füllung mit Morgenblatt; als Text auch auf Aktenstaub. Nicht für längere Textpassagen oder großflächige Dekoration verwenden.

### Zeitkupfer

- **HEX:** `#A43C2E`
- **RGB:** `164, 60, 46`
- **Charakter:** Dunkles Kupferrot; erinnert subtil an Markierungen, Siegel und historische Einschnitte.
- **Einsatz:** Ausgewählter Snapshot, aktueller Zeitpunkt, kritische Abweichung, kleine Zahlenmarken und punktuelle Hervorhebung.
- **Kombinationen:** Mit Morgenblatt oder Aktenstaub. Nicht direkt mit Registerblau kombinieren und nie gleichzeitig für mehrere konkurrierende Aktionen einsetzen.

---

## 3. Dark Mode — „Nachtmagazin“

Der Dark Mode ist keine Invertierung. Er besitzt tiefere, leicht farbige Schwarztöne und hellere Akzente, damit Hierarchie und Farberkennung bei geringer Umgebungshelligkeit erhalten bleiben.

### Nachtmagazin

- **HEX:** `#0B1113`
- **RGB:** `11, 17, 19`
- **Charakter:** Tiefes Blau-Schwarz mit ruhiger, räumlicher Wirkung.
- **Einsatz:** Große Grundflächen, Leseraum um Vorschauen und dunkle Archivansichten.
- **Kombinationen:** Lichtnotiz für Text; Registerblau für Interaktion; Zeitkupfer für einzelne Zeitmarken.

### Kohlenfalz

- **HEX:** `#172125`
- **RGB:** `23, 33, 37`
- **Charakter:** Kühle, leicht aufgehellte Kohle; trennt Ebenen ohne sichtbaren „Card“-Effekt.
- **Einsatz:** Suchfelder, Tabellenbereiche, Auswahlzeilen, Kalenderzellen und Metadatenleisten.
- **Kombinationen:** Lichtnotiz, Registerblau und Zeitkupfer. Gegen Nachtmagazin nur über Fläche und Kontur, nicht über Textbedeutung unterscheiden.

### Lichtnotiz

- **HEX:** `#ECE9DF`
- **RGB:** `236, 233, 223`
- **Charakter:** Gedämpftes, warmes Hellgrau; erhält die Papierassoziation ohne blendendes Weiß.
- **Einsatz:** Fließtext, Überschriften, Icons und helle Linien mit reduzierter Deckkraft.
- **Kombinationen:** Vor allem mit Nachtmagazin und Kohlenfalz. Nicht als Text auf den hellen Akzentfarben verwenden.

### Registerblau

- **HEX:** `#8EA7FF`
- **RGB:** `142, 167, 255`
- **Charakter:** Aufgehellte Fortsetzung des Kobaltblaus; bleibt eindeutig blau, ohne wie Neon zu leuchten.
- **Einsatz:** Links, Fokus, aktive Navigation, Timeline-Interaktion und gefüllte Hauptaktionen mit Nachtmagazin als Beschriftung.
- **Kombinationen:** Auf Nachtmagazin und Kohlenfalz. Nicht mit Lichtnotiz als benachbarte Kleinschrift kombinieren, weil die Helligkeiten zu ähnlich sind.

### Zeitkupfer

- **HEX:** `#FF9A7A`
- **RGB:** `255, 154, 122`
- **Charakter:** Helles, warmes Kupferkorall; sichtbar, aber weniger aggressiv als Signalrot.
- **Einsatz:** Ausgewählter Snapshot, aktueller Zeitpunkt, relevante Abweichung und punktuelle Statusmarkierung.
- **Kombinationen:** Mit Nachtmagazin oder Kohlenfalz. Nicht auf Lichtnotiz und nicht großflächig einsetzen.

---

## 4. Kontrast- und Accessibility-Konzept

Zeitregister zielt mindestens auf **WCAG 2.2 Level AA**. Normaler Text erreicht mindestens `4.5:1`, große Schrift mindestens `3:1`, erkennbare UI-Grenzen und nichttextliche Zustände mindestens `3:1`. Die produktinterne Vorgabe ist strenger: Fließtext, Links und Buttons sollen nach Möglichkeit `5:1` oder mehr erreichen. Die Schwellen folgen [WCAG 2.2, Kriterium 1.4.3](https://www.w3.org/TR/WCAG22/#contrast-minimum) und [1.4.11](https://www.w3.org/TR/WCAG22/#non-text-contrast).

### Exemplarische Kontrastwerte

| Anwendungsfall | Light Mode | Verhältnis | Dark Mode | Verhältnis |
|---|---|---:|---|---:|
| Fließtext und Überschriften | Chroniktinte auf Morgenblatt | **15.10:1** | Lichtnotiz auf Nachtmagazin | **15.66:1** |
| Sekundärtext | abgeleitete Nebennotiz `#5F6461` auf Morgenblatt | **5.49:1** | abgeleitete Nebennotiz `#ADADA6` auf Nachtmagazin | **8.43:1** |
| Link | Registerblau auf Morgenblatt | **6.07:1** | Registerblau auf Nachtmagazin | **8.26:1** |
| Gefüllter Hauptbutton | Morgenblatt auf Registerblau | **6.07:1** | Nachtmagazin auf Registerblau | **8.26:1** |
| Eingabetext | Chroniktinte auf Aktenstaub | **12.50:1** | Lichtnotiz auf Kohlenfalz | **13.50:1** |
| Eingabekontur / UI-Grenze | Randspur `#8C8E8A` auf Morgenblatt | **3.01:1** | Randspur `#5E615E` auf Nachtmagazin | **3.03:1** |
| Markierter Archivzeitpunkt | Morgenblatt auf Zeitkupfer | **5.85:1** | Nachtmagazin auf Zeitkupfer | **9.20:1** |
| Fokusindikator | Registerblau auf Aktenstaub | **5.02:1** | Registerblau auf Kohlenfalz | **7.12:1** |

### Zustände

- **Links:** Farbe plus dauerhafte Unterstreichung im Fließtext; in Navigation zusätzlich Positionslinie oder Schriftgewicht.
- **Hover:** Tonwertänderung, Unterstreichung oder Konturverdichtung. Kein wichtiges Detail erscheint ausschließlich bei Hover.
- **Focus:** `3px` durchgehende Registerblau-Kontur mit `2px` Abstand. Sie bleibt unverdeckt und ist nicht nur ein Schatten. Damit wird auch die strengere Empfehlung für deutlich sichtbaren Fokus berücksichtigt; siehe [W3C Focus Appearance](https://www.w3.org/WAI/WCAG22/Understanding/focus-appearance.html).
- **Selected:** Zeitkupfer oder Registerblau plus Formänderung, vertikale Marke, Häkchen und `aria-selected="true"`.
- **Fehler:** Icon, klare Überschrift, konkrete Korrektur, Zeitkupfer-Kontur und `aria-describedby`; nie nur ein roter Rand.
- **Erfolg:** Häkchen, Text „Archivierung abgeschlossen“, Erhaltungsgrün als Hilfsfarbe und optionaler Zeitstempel.
- **Targets:** Interaktive Ziele werden bevorzugt mindestens `44 × 44 px` groß; die WCAG-AA-Mindestanforderung für Zielgrößen und Abstände bleibt die Untergrenze.
- **Bewegung:** Zustandswechsel `120–180 ms`; keine Schleifen; `prefers-reduced-motion` schaltet nicht notwendige Übergänge ab.
- **Zoom und Reflow:** Funktionsfähig bei 200 % Textvergrößerung und bis 320 CSS-Pixel Breite. Lange URLs umbrechen mit `overflow-wrap: anywhere` und bleiben zusätzlich kopierbar.

Hilfsfarben für Statusmeldungen sind **Erhaltungsgrün** (`#276A4A` / dunkel `#7DC9A1`) und **Warnocker** (`#7B5600` / dunkel `#FFD174`). Sie werden nur zusammen mit Icon und Text verwendet und sind keine Markenakzente.

---

## 5. Beziehung zwischen Light und Dark Mode

| Gemeinsame Bedeutung | Light Mode | Dark Mode | Veränderung |
|---|---|---|---|
| ruhiger Gesamtraum | Morgenblatt | Nachtmagazin | warmes Papier wird zu blau-schwarzem Leseraum |
| zweite Informationslage | Aktenstaub | Kohlenfalz | hell abgesetzte Schicht wird zu leicht angehobener dunkler Fläche |
| lesende Stimme | Chroniktinte | Lichtnotiz | fast schwarze Tinte wird zu gedämpfter Lichtschrift |
| Interaktion und Navigation | Registerblau `#2E52C7` | Registerblau `#8EA7FF` | gleicher Farbcharakter, angehobene Helligkeit |
| gespeicherter Zeitpunkt | Zeitkupfer `#A43C2E` | Zeitkupfer `#FF9A7A` | gleicher warmer Gegenpol, höhere Leuchtkraft |

Im Light Mode tragen große helle Flächen etwa 75–80 % des Bildes; dunkle Typografie erzeugt das Gewicht. Im Dark Mode übernehmen Nachtmagazin und Kohlenfalz zusammen denselben Flächenanteil, während Lichtnotiz sparsamer eingesetzt wird. Akzentflächen werden im Dark Mode kleiner, dafür heller. Dadurch wirken beide Modi wie dieselbe Publikation unter unterschiedlichem Licht, nicht wie fotografische Negative.

---

## 6. Schriftwahl

### Source Sans 3

- **Kategorie:** Humanistische Sans Serif, variable Open-Source-Schrift
- **Rolle:** Interface, Fließtext, Navigation, Formulare, Tabellen und Statusmeldungen
- **Schnitte:** 400 Regular, 600 Semibold, optional 700 Bold für kurze Kennzahlen
- **Eigenschaften:** Offene Formen, große x-Höhe, ruhige Laufweite, gute Differenzierung ähnlicher Zeichen
- **Begründung:** Sie wurde für UI-Umgebungen entwickelt, trägt dichte Ergebnislisten ohne technisch-kalte Wirkung und harmoniert mit einer Serifenschrift. [Offizielles Source-Sans-Projekt](https://github.com/adobe-fonts/source-sans)
- **Fallback:** `"Segoe UI"`, `Inter`, `Arial`, `sans-serif`

### Newsreader

- **Kategorie:** Zeitgenössische Serifenschrift, variable Open-Source-Schrift
- **Rolle:** Display, H1, H2, redaktionelle Einleitungen und bedeutende Snapshot-Titel
- **Schnitte:** 400 Regular, 500 Medium, 600 Semibold; Kursiv nur für Zitate oder historische Anmerkungen
- **Eigenschaften:** Für kontinuierliches Lesen am Bildschirm entwickelt, markante aber kontrollierte Serifen, humaner Kontrast, variable optische Größen
- **Begründung:** Sie bringt die kulturelle Dimension eines Archivs ein, ohne Zeitungsklischee oder Retrogefühl. Das Projekt beschreibt sie ausdrücklich als Schrift für inhaltsreiche Bildschirmumgebungen. [Offizielles Newsreader-Projekt](https://github.com/productiontype/Newsreader)
- **Fallback:** `Iowan Old Style`, `Palatino Linotype`, `Georgia`, `serif`

### IBM Plex Mono

- **Kategorie:** Monospace, Open Source
- **Rolle:** URLs, Zeitstempel, IDs, Checksums, Dateigrößen, Versions- und Integritätsdaten
- **Schnitte:** 400 Regular, 500 Medium, 600 Semibold
- **Eigenschaften:** Deutlich unterscheidbare Zeichen, technische Präzision, tabellarischer Rhythmus und umfangreiche Familie
- **Begründung:** Plex verbindet menschliche und maschinelle Lesbarkeit und wurde für UI sowie technische Medien ausgelegt. [Offizielles IBM-Plex-Projekt](https://github.com/IBM/plex)
- **Fallback:** `ui-monospace`, `SFMono-Regular`, `Consolas`, `Liberation Mono`, `monospace`

### Empfohlene Font Stacks

```css
--font-editorial: "Newsreader", "Iowan Old Style", "Palatino Linotype", Georgia, serif;
--font-interface: "Source Sans 3", "Segoe UI", Inter, Arial, sans-serif;
--font-record: "IBM Plex Mono", ui-monospace, "SFMono-Regular", Consolas, "Liberation Mono", monospace;
```

Für Datenschutz, Stabilität und reproduzierbare Archivansichten sollten die WOFF2-Dateien selbst gehostet und auf tatsächlich verwendete Schnitte bzw. Zeichensätze reduziert werden.

---

## 7. Typografisches System

| Stufe | Schrift | Größe Desktop | Weight | Line Height | Letter Spacing |
|---|---|---:|---:|---:|---:|
| Display / Hero | Newsreader | `clamp(3rem, 7vw, 4.5rem)` | 500 | `1.06` | `-0.025em` |
| H1 | Newsreader | `3rem` | 500 | `1.08` | `-0.020em` |
| H2 | Newsreader | `2rem` | 500 | `1.18` | `-0.015em` |
| H3 | Source Sans 3 | `1.375rem` | 600 | `1.27` | `-0.005em` |
| Body | Source Sans 3 | `1.0625rem` | 400 | `1.59` | `0` |
| Small | Source Sans 3 | `0.875rem` | 400 / 600 | `1.43` | `0` |
| Meta | Source Sans 3 | `0.75rem` | 600 | `1.33` | `0.08em` |
| URL / Code / Timestamp | IBM Plex Mono | `0.8125rem` | 400 / 500 | `1.54` | `-0.01em` |

Regeln:

- Display fällt auf kleinen Viewports auf `3rem`, H1 auf `2.25rem`, H2 auf `1.75rem` zurück.
- Fließtextzeilen liegen idealerweise bei 55–75 Zeichen.
- UI-Texte bleiben linksbündig; Zahlenkolonnen verwenden `font-variant-numeric: tabular-nums`.
- URLs dürfen mehrzeilig werden. Ellipsis ist nur zulässig, wenn die vollständige URL per Kopieraktion und zugänglicher Beschriftung erreichbar bleibt.
- Zeitstempel folgen einem stabilen Muster: `2026-09-09 · 14:32:08 UTC`.
- Meta-Text darf in Versalien stehen, bleibt aber kurz und wird nicht unter 12 px gesetzt.

---

## 8. UI- und Komponentenstil

### Navigation

Eine flache, horizontale Kopfzeile mit Wortmarke, wenigen Hauptzielen und klarer aktiver Positionslinie. Keine schwebende App-Leiste. Mobile Navigation darf in eine beschriftete Menüschaltfläche wechseln.

### URL-Suche

- Persistentes Label über dem Feld: „Webadresse durchsuchen“.
- Eingabehöhe 56 px, Radius 6 px, sichtbare 1-px-Kontur.
- Eingegebene URLs in IBM Plex Mono; Placeholder in Source Sans 3.
- Hauptaktion als kompakte Registerblau-Fläche, nicht als übergroßer Hero-Button.
- Validierung nennt das Problem und ein Beispiel für ein gültiges Format.

### Suchergebnisse und Snapshots

- Primär als gegliederte Liste mit horizontalen Linien, nicht als gleichförmiges Kartenraster.
- Reihenfolge: Titel, URL, Archivzeitpunkt, Qualitäts-/Integritätsangabe, Vorschauaktion.
- Auswahl erhält links eine 3-px-Zeitkupfer-Markierung, ein Häkchen und eine leicht getönte Fläche.
- Vorschaubilder sind unterstützend; Metadaten bleiben auch ohne Bild vollständig verständlich.

### Kalender

- Flaches Monatsraster ohne 42 einzelne Karten.
- Tage mit Snapshots erhalten Punkt plus Anzahl; mehrere Dichten werden über Punktgröße und Zahl erklärt.
- Der ausgewählte Tag nutzt Zeitkupfer, einen Innenring und `aria-current="date"`.
- Monatswechsel sind beschriftete Pfeilaktionen mit mindestens 44 px Zielgröße.

### Timeline

- Eine dünne Registerlinie trägt Jahresgruppen und Zeitmarken.
- Snapshots sind 8–12 px große Punkte mit großzügigem unsichtbarem Trefferbereich.
- Auswahl erzeugt eine senkrechte Zeitkupfer-Linie, vergrößerten Punkt und vollständig ausgeschriebenen Zeitstempel.
- Dichte Zeiträume werden gebündelt; Zoom oder Filter dürfen angeboten werden, sobald Punkte nicht mehr unterscheidbar sind.

### URL und Metadaten

- URLs stehen in IBM Plex Mono, brechen kontrolliert und besitzen eine sichtbare „Kopieren“-Aktion.
- Metadaten als semantische Definitionsliste; Labels kurz, Werte dominant.
- IDs und Prüfsummen dürfen visuell verkürzt werden, müssen aber vollständig kopierbar bleiben.

### Buttons

- **Hauptaktion:** Registerblau gefüllt, kontrastierende Beschriftung, 44 px Mindesthöhe.
- **Sekundäraktion:** transparent mit deutlicher Randspur.
- **Textaktion:** unterstrichener Link mit optionalem Richtungspfeil.
- Pro Bediengruppe nur eine Hauptaktion. Icon-only nur für gelernte Funktionen und immer mit zugänglichem Namen.

### Formulare

Labels bleiben sichtbar. Hilfe steht vor Fehlertext. Required wird textlich und semantisch markiert. Fokus ersetzt Fehlerzustand nicht; beide Signale dürfen gleichzeitig sichtbar sein.

### Status, Leerzustand und Fehler

- Meldungen sind flache Blöcke mit Icon, Titel, Erklärung und optionaler Aktion.
- Erfolg nutzt Häkchen plus Erhaltungsgrün; Warnung Dreieck plus Warnocker; Fehler Ausrufezeichen plus Zeitkupfer.
- Leerzustände zeigen eine kleine lineare Illustration aus Registerlinien oder leeren Zeitmarken, eine Aussage und höchstens eine Hauptaktion.
- Texte vermeiden Schuldzuweisung: „Diese URL besitzt noch keinen Snapshot“ statt „Keine Daten“.

### Archivierte Webseitenvorschau

- Eine feste Informationsleiste steht **oberhalb** der archivierten Seite, nicht als halbtransparente Überlagerung.
- Sie enthält Quelle, Snapshot-Zeit, Integritätsstatus, vorherigen/nächsten Snapshot und „Original öffnen“.
- Der Inhalt der archivierten Seite wird nicht durch den Dark Mode umgefärbt. Ein neutraler Rahmen trennt Originalinhalt und Archivoberfläche.
- Externe Navigation aus dem Snapshot wird sichtbar gekennzeichnet; Sicherheits- und Sandboxgrenzen werden in verständlicher Sprache erklärt.

---

## 9. Visuelle Identität

### Eckenradien

- `2 px`: Zeitmarken, kleine Labels und technische Einsätze
- `6 px`: Eingaben, Buttons, Listenzeilen und Standardmodule
- `10 px`: größere Vorschauflächen und Dialoge
- `999 px`: ausschließlich kompakte Statuschips oder Zähler

Die visuelle Grundhaltung bleibt eher rechtwinklig als weich. Große 24–40-px-Radien sind ausgeschlossen.

### Linien und Konturen

1 px für Struktur, 2 px für aktive Segmente, 3 px für Fokus und ausgewählte Zeitspuren. Horizontale Linien gliedern stärker als geschlossene Kästen. Keine dekorativen Doppelkonturen.

### Schatten

Standardflächen bleiben schattenlos. Menüs und Vorschaufenster dürfen `0 8px 24px` mit 10–14 % dunkler Transparenz erhalten. Schatten zeigen räumliche Überlagerung, nie bloß Wichtigkeit.

### Abstände

4-px-Grundraster: `4, 8, 12, 16, 24, 32, 48, 64, 96`. Datenreiche Listen arbeiten mit 12–16 px vertikal; redaktionelle Abschnitte mit 48–96 px.

### Icons

Lineare Icons mit 1.5–2 px Strich, 16/20/24 px Größen. Die Form bleibt geometrisch und offen. Geeignete Motive: Suche, Kalender, Uhr, Kopieren, externe Quelle, Prüfsumme, Warnung. Keine gefüllten 3D-Icons oder unterschiedlichen Illustrationsstile.

### Illustrationen

Abstrakte Schichten, Registerlinien, Ausschnitte und Punktfolgen. Keine Sanduhren, vergilbten Schriftrollen, Filmrollen oder plakative Retro-Computer.

### Diagramme

Neutrale Achsen, maximal ein primäres Registerblau und Zeitkupfer für Auswahl. Direkte Beschriftung vor Legenden. Rasterlinien bleiben dünn; Information wird zusätzlich über Form, Muster oder Text codiert.

### Hover und Focus

Hover verschiebt keine Layouts und hebt Elemente nicht pseudo-räumlich an. Geeignet sind Unterstreichung, Aktenstaub-/Kohlenfalz-Fläche oder Konturverdichtung. Focus ist stets ein klarer Registerblau-Ring; `outline: none` ohne gleichwertigen Ersatz ist verboten.

---

## 10. Design Tokens / CSS-Beispiel

Die Quelltokens tragen die Farbnamen. Fünf thematische Brückentokens verbinden beide Modi, ohne sich ausschließlich nach UI-Funktionen zu benennen.

```css
:root {
  color-scheme: light;

  /* Light palette */
  --morgenblatt: #f7f4ec;
  --aktenstaub: #e5dfd2;
  --chroniktinte: #18201f;
  --registerblau: #2e52c7;
  --zeitkupfer: #a43c2e;

  /* Zeitregister bridge */
  --zeitraum: var(--morgenblatt);
  --speicherlage: var(--aktenstaub);
  --lesespur: var(--chroniktinte);
  --registerzeichen: var(--registerblau);
  --zeitmarke: var(--zeitkupfer);
  --nebennotiz: #5f6461;
  --randspur: #8c8e8a;

  --font-editorial: "Newsreader", "Iowan Old Style", Georgia, serif;
  --font-interface: "Source Sans 3", "Segoe UI", Arial, sans-serif;
  --font-record: "IBM Plex Mono", ui-monospace, Consolas, monospace;

  --space-1: 0.25rem;
  --space-2: 0.5rem;
  --space-3: 0.75rem;
  --space-4: 1rem;
  --space-6: 1.5rem;
  --space-8: 2rem;
  --space-12: 3rem;
  --space-16: 4rem;

  --radius-mark: 2px;
  --radius-control: 6px;
  --radius-preview: 10px;
  --shadow-overlay: 0 8px 24px rgb(24 32 31 / 12%);
}

[data-theme="dark"] {
  color-scheme: dark;

  /* Dark palette */
  --nachtmagazin: #0b1113;
  --kohlenfalz: #172125;
  --lichtnotiz: #ece9df;
  --registerblau: #8ea7ff;
  --zeitkupfer: #ff9a7a;

  --zeitraum: var(--nachtmagazin);
  --speicherlage: var(--kohlenfalz);
  --lesespur: var(--lichtnotiz);
  --registerzeichen: var(--registerblau);
  --zeitmarke: var(--zeitkupfer);
  --nebennotiz: #adada6;
  --randspur: #5e615e;
  --shadow-overlay: 0 8px 24px rgb(0 0 0 / 28%);
}

body {
  background: var(--zeitraum);
  color: var(--lesespur);
  font-family: var(--font-interface);
}

a {
  color: var(--registerzeichen);
  text-decoration-thickness: 0.08em;
  text-underline-offset: 0.16em;
}

.archive-field {
  min-height: 3.5rem;
  border: 1px solid var(--randspur);
  border-radius: var(--radius-control);
  background: var(--speicherlage);
  color: var(--lesespur);
}

.archive-field:focus-visible,
button:focus-visible,
a:focus-visible {
  outline: 3px solid var(--registerzeichen);
  outline-offset: 2px;
}

.snapshot[aria-selected="true"] {
  border-inline-start: 3px solid var(--zeitmarke);
  background: var(--speicherlage);
}

.record-value {
  font-family: var(--font-record);
  font-variant-numeric: tabular-nums;
  overflow-wrap: anywhere;
}

@media (prefers-reduced-motion: reduce) {
  *, *::before, *::after {
    scroll-behavior: auto !important;
    transition-duration: 0.01ms !important;
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
  }
}
```

---

## 11. Do’s & Don’ts

### Do

- Zeit durch Raster, Taktung, Datum und Provenienz sichtbar machen.
- Große redaktionelle Überschriften mit ruhigen, datenreichen Listen verbinden.
- Monospace gezielt für maschinenlesbare Werte einsetzen.
- URLs umbrechen und vollständig kopierbar halten.
- Eine Auswahl gleichzeitig über Farbe, Form und Text kennzeichnen.
- Trennlinien und Weißraum vor zusätzlichen Containern verwenden.
- Light und Dark mit identischer Hierarchie, aber eigener Lichtlogik gestalten.
- Archivierte Inhalte klar von der Bedienoberfläche trennen.

### Don’t

- Keine Sepiafilter, Papiertexturen oder Retro-Requisiten.
- Keine Glassmorphism-Flächen, Neonränder oder dekorativen Verläufe.
- Keine Startseite aus gleichförmigen KPI- und Feature-Karten.
- Keine extremen Rundungen, schwebenden Elemente oder langen Schatten.
- Keine Farbcodierung ohne Icon, Text oder Formsignal.
- Keine dauerhaft pulsierenden Timeline-Punkte.
- Keine abgeschnittene URL ohne Zugang zum vollständigen Wert.
- Keine Umfärbung der archivierten Webseite im Dark Mode.

---

## 12. Referenzzustand für das Interface-Mockup

- **Suchaufgabe:** „Eine URL aus dem Internetarchiv suchen“
- **URL:** `https://example.com/notes/the-web-we-remember`
- **Domain:** `example.com`
- **Bestand:** `487 gespeicherte Snapshots`
- **Zeitraum:** `2013–2026`
- **Auswahl:** `17. März 2024 · 14:32:08 UTC`
- **Status:** „Vollständig · 126 Ressourcen · SHA-256 geprüft“
- **Darstellung:** identischer Informationsaufbau in Light und Dark; Morgenblatt/Nachtmagazin tragen den Raum, Registerblau zeigt Interaktion, Zeitkupfer markiert den ausgewählten Snapshot.

