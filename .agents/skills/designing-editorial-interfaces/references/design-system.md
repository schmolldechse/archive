# Editorial Archive Design System

## Purpose and Visual Principles

Design the web archive as a digital reading room: a place where preserved states are not staged as spectacle but made precise and understandable. Warm, paper-adjacent foundations give memory a human quality; clear rules, tabular numerals, and controlled information density convey technical care.

Time is structural rather than decorative. Express it through register lines, continuous numbering, stable date formats, vertical markers, year bands, and layered information. A snapshot should feel like a reliable record with provenance, not a generic content card.

1. **Reading before spectacle.** Content, source, and timestamp take priority.
2. **Time as structure.** Axes, tick marks, year bands, and date notation form the recurring motif.
3. **Editorial hierarchy.** Serif type conveys significance and memory; sans serif organizes interaction; monospace demonstrates technical precision.
4. **Quiet surfaces.** Few color planes, fine outlines, and minimal shadow create depth without a stack-of-cards aesthetic.
5. **Provable states.** Selection, focus, error, and success always use at least two signals.
6. **Durable over fashionable.** Avoid decorative gradients, glassmorphism, neon, and continuous animation.

Treat the interface more like a publication than a conventional SaaS dashboard: strong openings, clear registers, horizontal reading lines, a few bounded modules, and a visible relationship between source, time, and archived object.

## Color System

The brand palette has five colors per theme. Derive transparent and mixed values from them. Status colors are restrained utilities, not brand accents.

### Light Theme: Reading Room

#### Morning Paper

- **Value:** `#F7F4EC` / `rgb(247 244 236)`
- **Character:** Warm mineral off-white; refined and less sterile than pure white.
- **Use:** Main reading surfaces, page background, forms, preview surroundings, and light text on filled actions.
- **Pairing:** Chronicle Ink for text; Register Blue and Time Copper for text, rules, or compact fills.

#### Archive Dust

- **Value:** `#E5DFD2` / `rgb(229 223 210)`
- **Character:** Muted beige-gray suggesting ordered paper layers without nostalgia.
- **Use:** Secondary surfaces, table headers, hover areas, filter groups, inactive calendar cells, and quiet separators.
- **Pairing:** Chronicle Ink for maximum readability, Register Blue for links and active marks, and Time Copper only for a selected time.

#### Chronicle Ink

- **Value:** `#18201F` / `rgb(24 32 31)`
- **Character:** Near-black, subtly green charcoal; deep without the harshness of pure black.
- **Use:** Body copy, headings, icons, primary outlines, secondary buttons, and technical data.
- **Pairing:** Primarily Morning Paper and Archive Dust. Do not place large Chronicle Ink and Register Blue surfaces side by side; both carry too much visual weight.

#### Register Blue

- **Value:** `#2E52C7` / `rgb(46 82 199)`
- **Character:** Precise, slightly muted cobalt; digital and unambiguous without neon intensity.
- **Use:** Primary actions, links, focus indicators, active navigation, selected filters, and interactive timeline marks.
- **Pairing:** Use Morning Paper on filled controls; use as text on Morning Paper or Archive Dust. Avoid long passages and large decorative fills.

#### Time Copper

- **Value:** `#A43C2E` / `rgb(164 60 46)`
- **Character:** Dark copper red, recalling annotations, seals, and historical cuts.
- **Use:** Selected snapshot, current time, critical deviation, small number marks, and isolated emphasis.
- **Pairing:** Morning Paper or Archive Dust. Do not pair directly with Register Blue or use for multiple competing actions.

### Dark Theme: Night Stacks

Dark mode is not an inversion. Its subtly colored blacks and lighter accents preserve hierarchy and color recognition in low ambient light.

#### Night Stacks

- **Value:** `#0B1113` / `rgb(11 17 19)`
- **Character:** Deep blue-black with calm spatial depth.
- **Use:** Large foundations, space around previews, and dark archive views.
- **Pairing:** Light Note for text, Register Blue for interaction, and Time Copper for isolated time marks.

#### Charcoal Fold

- **Value:** `#172125` / `rgb(23 33 37)`
- **Character:** Cool, slightly lifted charcoal that separates layers without an obvious card effect.
- **Use:** Search fields, table areas, selected rows, calendar cells, and metadata strips.
- **Pairing:** Light Note, Register Blue, and Time Copper. Distinguish it from Night Stacks through surface and outline, not text meaning.

#### Light Note

- **Value:** `#ECE9DF` / `rgb(236 233 223)`
- **Character:** Muted warm light gray that preserves the paper association without glare.
- **Use:** Body copy, headings, icons, and pale rules at reduced opacity.
- **Pairing:** Night Stacks and Charcoal Fold. Do not use as text on the light accent colors.

#### Register Blue

- **Value:** `#8EA7FF` / `rgb(142 167 255)`
- **Character:** A lighter continuation of cobalt blue; clearly blue without appearing neon.
- **Use:** Links, focus, active navigation, timeline interaction, and filled primary actions with Night Stacks text.
- **Pairing:** Night Stacks and Charcoal Fold. Avoid adjacent small Light Note text because their lightness is too similar.

#### Time Copper

- **Value:** `#FF9A7A` / `rgb(255 154 122)`
- **Character:** Light copper coral; visible but less aggressive than signal red.
- **Use:** Selected snapshot, current time, relevant deviation, and isolated status marks.
- **Pairing:** Night Stacks or Charcoal Fold. Do not use on Light Note or across large surfaces.

### Status and Derived Colors

- **Marginal Note:** `#5F6461` light / `#ADADA6` dark for secondary text.
- **Border Trace:** `#8C8E8A` light / `#5E615E` dark for controls and non-text boundaries.
- **Preservation Green:** `#276A4A` light / `#7DC9A1` dark for success.
- **Warning Ochre:** `#7B5600` light / `#FFD174` dark for warnings.

Use status colors only with an icon and text.

### Theme Relationship

| Shared meaning | Light | Dark | Change |
|---|---|---|---|
| Calm overall space | Morning Paper | Night Stacks | Warm paper becomes a blue-black reading room |
| Secondary information layer | Archive Dust | Charcoal Fold | Light separated layer becomes a slightly raised dark surface |
| Reading voice | Chronicle Ink | Light Note | Near-black ink becomes muted light writing |
| Interaction and navigation | Register Blue `#2E52C7` | Register Blue `#8EA7FF` | Same hue character, increased lightness |
| Preserved point in time | Time Copper `#A43C2E` | Time Copper `#FF9A7A` | Same warm counterpoint, increased brightness |

In light mode, large light surfaces occupy roughly 75–80% of the view. In dark mode, Night Stacks and Charcoal Fold together take the same share while Light Note is used more sparingly. Dark accents are smaller but brighter. Both themes should feel like the same publication under different light.

## Accessibility and Contrast

Target at least WCAG 2.2 Level AA: `4.5:1` for normal text, `3:1` for large text, and `3:1` for recognizable UI boundaries and non-text states. Prefer `5:1` or higher for body copy, links, and buttons. Use [WCAG 2.2 Contrast Minimum](https://www.w3.org/TR/WCAG22/#contrast-minimum), [Non-text Contrast](https://www.w3.org/TR/WCAG22/#non-text-contrast), and [Focus Appearance](https://www.w3.org/WAI/WCAG22/Understanding/focus-appearance.html) as the authoritative criteria.

| Use | Light | Ratio | Dark | Ratio |
|---|---|---:|---|---:|
| Body and headings | Chronicle Ink on Morning Paper | **15.10:1** | Light Note on Night Stacks | **15.66:1** |
| Secondary text | Marginal Note on Morning Paper | **5.49:1** | Marginal Note on Night Stacks | **8.43:1** |
| Link | Register Blue on Morning Paper | **6.07:1** | Register Blue on Night Stacks | **8.26:1** |
| Filled primary button | Morning Paper on Register Blue | **6.07:1** | Night Stacks on Register Blue | **8.26:1** |
| Input text | Chronicle Ink on Archive Dust | **12.50:1** | Light Note on Charcoal Fold | **13.50:1** |
| Input border | Border Trace on Morning Paper | **3.01:1** | Border Trace on Night Stacks | **3.03:1** |
| Selected archive time | Morning Paper on Time Copper | **5.85:1** | Night Stacks on Time Copper | **9.20:1** |
| Focus indicator | Register Blue on Archive Dust | **5.02:1** | Register Blue on Charcoal Fold | **7.12:1** |

Apply these state rules:

- **Links:** Color plus persistent underline in body text; navigation also uses a position rule or weight.
- **Hover:** Tone change, underline, or stronger outline. Never reveal essential information only on hover.
- **Focus:** Continuous `3px` Register Blue outline with `2px` offset. Keep it unobscured and do not implement it only as shadow.
- **Selected:** Register Blue or Time Copper plus a shape change, vertical mark, check, and `aria-selected="true"` where applicable.
- **Error:** Icon, clear heading, concrete correction, Time Copper outline, and `aria-describedby`; never only a red border.
- **Success:** Check, completion text, Preservation Green, and optional timestamp.
- **Targets:** Prefer at least `44 × 44px` interactive targets.
- **Motion:** Use `120–180ms` state transitions, no loops, and remove nonessential motion under `prefers-reduced-motion`.
- **Reflow:** Support 200% text enlargement and widths down to 320 CSS pixels. Wrap long URLs with `overflow-wrap: anywhere` and keep them copyable.

## Typography

### Families and Roles

- **[Source Sans 3](https://github.com/adobe-fonts/source-sans):** Interface, body copy, navigation, forms, tables, and status messages. Use 400 Regular, 600 Semibold, and optionally 700 Bold for brief figures. Fallback: `"Segoe UI", Inter, Arial, sans-serif`.
- **[Newsreader](https://github.com/productiontype/Newsreader):** Display, H1, H2, editorial introductions, and important snapshot titles. Use 400 Regular, 500 Medium, and 600 Semibold; reserve italics for quotations or historical notes. Fallback: `"Iowan Old Style", "Palatino Linotype", Georgia, serif`.
- **[IBM Plex Mono](https://github.com/IBM/plex):** URLs, timestamps, IDs, checksums, file sizes, versions, and integrity data. Use 400 Regular, 500 Medium, and 600 Semibold. Fallback: `ui-monospace, "SFMono-Regular", Consolas, "Liberation Mono", monospace`.

Self-host WOFF2 files and subset them to the weights and character sets actually used for privacy, stability, and reproducible archive views.

```css
--font-editorial: "Newsreader", "Iowan Old Style", "Palatino Linotype", Georgia, serif;
--font-interface: "Source Sans 3", "Segoe UI", Inter, Arial, sans-serif;
--font-record: "IBM Plex Mono", ui-monospace, "SFMono-Regular", Consolas, "Liberation Mono", monospace;
```

### Type Scale

| Level | Family | Desktop size | Weight | Line height | Letter spacing |
|---|---|---:|---:|---:|---:|
| Display / Hero | Newsreader | `clamp(3rem, 7vw, 4.5rem)` | 500 | `1.06` | `-0.025em` |
| H1 | Newsreader | `3rem` | 500 | `1.08` | `-0.020em` |
| H2 | Newsreader | `2rem` | 500 | `1.18` | `-0.015em` |
| H3 | Source Sans 3 | `1.375rem` | 600 | `1.27` | `-0.005em` |
| Body | Source Sans 3 | `1.0625rem` | 400 | `1.59` | `0` |
| Small | Source Sans 3 | `0.875rem` | 400 / 600 | `1.43` | `0` |
| Meta | Source Sans 3 | `0.75rem` | 600 | `1.33` | `0.08em` |
| URL / code / timestamp | IBM Plex Mono | `0.8125rem` | 400 / 500 | `1.54` | `-0.01em` |

- On small viewports, reduce Display to `3rem`, H1 to `2.25rem`, and H2 to `1.75rem`.
- Keep body lines near 55–75 characters.
- Left-align interface copy and use `font-variant-numeric: tabular-nums` for numeric columns.
- Allow multiline URLs. Use ellipsis only when the full value remains available through an accessible copy action.
- Use stable timestamps such as `2026-09-09 · 14:32:08 UTC`.
- Meta text may be uppercase when brief, but never below 12px.

## Layout and Visual Identity

### Geometry, Rules, and Spacing

- **Radii:** `2px` for time marks and technical labels; `6px` for inputs, buttons, list rows, and standard modules; `10px` for previews and dialogs; `999px` only for compact status chips and counters. Avoid 24–40px radii.
- **Rules:** `1px` for structure, `2px` for active segments, and `3px` for focus and selected time traces. Horizontal rules should organize more often than closed boxes. Avoid decorative double outlines.
- **Shadows:** Default surfaces are shadowless. Menus and preview windows may use `0 8px 24px` at 10–14% dark opacity. Shadow means spatial overlap, never importance.
- **Spacing:** Use the 4px scale `4, 8, 12, 16, 24, 32, 48, 64, 96`. Dense lists use 12–16px vertically; editorial sections use 48–96px.
- **Icons:** Use open, geometric line icons with 1.5–2px strokes at 16, 20, or 24px. Keep one illustration style.
- **Illustrations:** Use abstract layers, register lines, crops, and point sequences. Avoid hourglasses, yellowed scrolls, film reels, and conspicuous retro computers.
- **Charts:** Use neutral axes, at most one primary Register Blue, and Time Copper for selection. Prefer direct labels to legends and supplement color with shape, pattern, or text.

Hover must not shift layout or pseudo-lift elements. Use underlines, Archive Dust or Charcoal Fold surfaces, or stronger outlines. Never remove the focus outline without an equivalent visible replacement.

## Component Guidance

### Navigation

Use a flat horizontal header with a wordmark, a few primary destinations, and a clear active-position rule. Avoid a floating app bar. Mobile navigation may collapse into a labeled menu button.

### URL Search

- Keep a persistent label above the field.
- Use a 56px input height, 6px radius, and visible 1px border.
- Render entered URLs in IBM Plex Mono and placeholders in Source Sans 3.
- Keep the Register Blue primary action compact rather than turning it into an oversized hero button.
- Validation should name the problem and show one valid format.

### Search Results and Snapshots

- Prefer a structured list with horizontal rules over a uniform card grid.
- Order information as title, URL, archive time, quality or integrity, then preview action.
- Mark selection with a 3px Time Copper line at the start edge, a check, and a lightly tinted surface.
- Treat thumbnails as supporting material; metadata must remain understandable without them.

### Calendar

- Use a flat month grid rather than 42 separate cards.
- Mark days with snapshots using a point plus count; explain density through point size and number.
- Mark the selected day with Time Copper, an inner ring, and `aria-current="date"`.
- Use labeled arrow actions with at least 44px targets for month changes.

### Timeline

- Place year groups and time marks on a thin register line.
- Render snapshot points at 8–12px with generous invisible hit areas.
- Show selection with a vertical Time Copper line, an enlarged point, and a fully written timestamp.
- Cluster dense periods; introduce zoom or filtering once points are no longer distinguishable.

### URLs and Metadata

- Render URLs in IBM Plex Mono, wrap them predictably, and provide a visible Copy action.
- Use semantic definition lists for metadata, with brief labels and dominant values.
- IDs and checksums may be shortened visually only when their complete value is copyable.

### Buttons and Forms

- **Primary:** Filled Register Blue, contrasting label, minimum 44px height.
- **Secondary:** Transparent with a clear Border Trace outline.
- **Text action:** Underlined link, optionally with a directional arrow.
- Allow one primary action per control group. Use icon-only controls only for familiar actions and always provide an accessible name.
- Keep labels visible. Place help before error text. Mark required fields in text and semantics. Focus and error signals may remain visible together.

### Status, Empty, and Error States

Use flat message blocks with icon, title, explanation, and optional action. Success uses a check and Preservation Green; warning uses a triangle and Warning Ochre; error uses an exclamation mark and Time Copper. Empty states may use a small linear illustration of register lines or empty time marks, one statement, and no more than one primary action. Avoid blaming language.

### Archived Webpage Preview

- Place a fixed information bar above the archived page, never as a translucent overlay.
- Include source, snapshot time, integrity status, previous and next snapshot, and an “Open original” action.
- Do not recolor archived content for dark mode. Separate the original page from archive controls with a neutral frame.
- Mark external navigation visibly and explain security and sandbox boundaries in plain language.

## Tokens and CSS Pattern

```css
:root {
  color-scheme: light;

  --morning-paper: #f7f4ec;
  --archive-dust: #e5dfd2;
  --chronicle-ink: #18201f;
  --register-blue: #2e52c7;
  --time-copper: #a43c2e;

  --reading-room: var(--morning-paper);
  --archive-layer: var(--archive-dust);
  --reading-ink: var(--chronicle-ink);
  --register-mark: var(--register-blue);
  --time-marker: var(--time-copper);
  --marginal-note: #5f6461;
  --border-trace: #8c8e8a;
  --preservation-green: #276a4a;
  --warning-ochre: #7b5600;

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

  --night-stacks: #0b1113;
  --charcoal-fold: #172125;
  --light-note: #ece9df;
  --register-blue: #8ea7ff;
  --time-copper: #ff9a7a;

  --reading-room: var(--night-stacks);
  --archive-layer: var(--charcoal-fold);
  --reading-ink: var(--light-note);
  --register-mark: var(--register-blue);
  --time-marker: var(--time-copper);
  --marginal-note: #adada6;
  --border-trace: #5e615e;
  --preservation-green: #7dc9a1;
  --warning-ochre: #ffd174;
  --shadow-overlay: 0 8px 24px rgb(0 0 0 / 28%);
}

body {
  background: var(--reading-room);
  color: var(--reading-ink);
  font-family: var(--font-interface);
}

a {
  color: var(--register-mark);
  text-decoration-thickness: 0.08em;
  text-underline-offset: 0.16em;
}

.archive-field {
  min-height: 3.5rem;
  border: 1px solid var(--border-trace);
  border-radius: var(--radius-control);
  background: var(--archive-layer);
  color: var(--reading-ink);
}

.archive-field:focus-visible,
button:focus-visible,
a:focus-visible {
  outline: 3px solid var(--register-mark);
  outline-offset: 2px;
}

.snapshot[aria-selected="true"] {
  border-inline-start: 3px solid var(--time-marker);
  background: var(--archive-layer);
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

## Do and Avoid

### Do

- Make time visible through grids, rhythm, dates, and provenance.
- Pair large editorial headings with quiet, data-rich lists.
- Reserve monospace for machine-readable values.
- Wrap URLs and keep them fully copyable.
- Mark selection through color, form, and text.
- Prefer rules and whitespace to additional containers.
- Give light and dark themes identical hierarchy with distinct lighting logic.
- Separate archived content clearly from archive controls.

### Avoid

- Sepia filters, paper textures, retro props, decorative gradients, glass surfaces, and neon edges.
- Homepages made from uniform KPI and feature cards.
- Extreme rounding, floating elements, and long shadows.
- Color-only meaning or continuously pulsing timeline points.
- Truncated URLs without access to the complete value.
- Recoloring archived webpages in dark mode.
