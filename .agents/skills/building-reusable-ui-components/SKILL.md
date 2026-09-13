---
name: building-reusable-ui-components
description: Use when creating, refactoring, or reviewing reusable Svelte 5 UI components or their Storybook documentation in this repository.
---

# Building Reusable UI Components

## Overview

Build semantic, composable Svelte 5 primitives with explicit APIs and predictable state. A component belongs in the shared UI library only when multiple features can reuse it without inheriting archive-specific data, routing, or business behavior.

**REQUIRED SUB-SKILL:** Use `designing-editorial-interfaces` before styling or visually reviewing a component. Its design system is the source of truth for layout, typography, color, focus, theme, and motion.

## Workflow

1. Inspect `frontend/package.json`, `frontend/src/app.css`, existing UI primitives, and Storybook conventions before adding an abstraction.
2. Search for an existing primitive to reuse or extend. Compose shared primitives instead of creating overlapping alternatives.
3. Define semantics and the public API before implementation. Keep source state in one place and derive consequences.
4. Choose the smallest valid file structure and implement only reusable behavior.
5. Add accessibility, stable `data-*` hooks, design-system styling, and documentation-only Storybook coverage.
6. Validate changed Svelte files with the Svelte MCP tools when available, then run `bun run check` and `bun run build:storybook` from `frontend`.

Do not create, run, modify, or delete tests. Storybook stories document capabilities; they are not tests.

## File and Naming Contract

| Concern | Requirement |
|---|---|
| Simple component | Place `PascalCase.svelte` directly in `frontend/src/lib/components/ui/`. |
| Component family | Put the complete family in `frontend/src/lib/components/ui/<component-name>/`, where the folder is lowercase kebab-case. Keep every component file and component symbol in PascalCase. |
| Family contents | Co-locate child components, `*-context.svelte.ts`, helpers, types, and `index.ts` barrel exports in the family folder. Do not create the folder or barrel before the family needs it. |
| Language | Use English for component names, exports, props, CSS classes, `data-*` names and values, errors, and Storybook titles. UI copy follows the consuming feature's language requirements. |
| Stories | Put simple stories at `frontend/src/stories/PascalCase.stories.svelte`. Put compound stories and their supporting files in `frontend/src/stories/<component-name>/`. Every story or story-only component `.svelte` file uses PascalCase. |

Example:

```text
frontend/src/lib/components/ui/
|-- Button.svelte
`-- accordion/
    |-- AccordionRoot.svelte
    |-- AccordionItem.svelte
    |-- AccordionTrigger.svelte
    |-- accordion-context.svelte.ts
    |-- helpers.ts
    `-- index.ts

frontend/src/stories/
|-- Button.stories.svelte
`-- accordion/
    `-- Accordion.stories.svelte
```

## Svelte 5 Contract

Use the Svelte version declared in `frontend/package.json` and only modern Svelte 5 APIs:

- Declare component inputs with typed `$props()` and bindable inputs with `$bindable()` only when two-way binding is part of the public contract.
- Keep mutable source state in `$state` and computed values in `$derived` or `$derived.by`.
- Use snippets and `{@render ...}` for composition. Do not use legacy slots.
- Use callback props and event properties such as `onclick`. Do not use `createEventDispatcher` or legacy `on:` directives.
- Type native wrapper props from `svelte/elements`. Forward applicable native attributes, event handlers, `class`, ARIA, and consumer `data-*` attributes to the semantic root element.
- Merge consumer classes without discarding the component's semantic base and state styles.
- Expose a bindable DOM reference only when consumers have a concrete focus, measurement, or integration need.
- Access browser globals only in event handlers or browser-safe lifecycle/integration paths. Preserve matching SSR and hydration markup.

### Effects Are an Escape Hatch

Do not derive, mirror, or synchronize component state with `$effect`. Use `$derived`, function bindings, event handlers, or explicit callbacks instead.

This keeps one source of truth, avoids update loops and ordering dependencies, remains predictable during SSR, and makes the component API easier to reason about. `$effect` runs in the browser after DOM updates, so it is appropriate only for an unavoidable imperative boundary such as a third-party widget, direct canvas or DOM integration, analytics, or an external subscription. Guard browser-only work and return cleanup when the integration allocates listeners, timers, observers, or instances.

## Context Pattern

Create context only for a real component family relationship that would otherwise require prop drilling. Put all context logic in `<component-name>-context.svelte.ts`. Use a module-local `Symbol(...)` with a unique, descriptive English label and typed set/get helpers.

```ts
import { getContext, setContext } from "svelte";

interface AccordionContext {
	activeValue: string | null;
	select(value: string): void;
}

const ACCORDION_CONTEXT_KEY = Symbol("ReusableUiAccordionContext");

export function setAccordionContext(value: AccordionContext): void {
	setContext(ACCORDION_CONTEXT_KEY, value);
}

export function getAccordionContext(): AccordionContext {
	const value = getContext<AccordionContext>(ACCORDION_CONTEXT_KEY);

	if (!value) {
		throw new Error("Accordion child components must be rendered inside AccordionRoot.");
	}

	return value;
}
```

Keep the symbol private. Context values containing reactive state should preserve their object identity; mutate their properties instead of replacing the object unexpectedly.

This repository intentionally keeps explicit Symbol-based context keys. Do not replace this convention with `createContext` inside component families.

## Reusable Component Contract

| Concern | Requirement |
|---|---|
| Boundary | Shared UI primitives contain presentation and generic interaction, not API calls, archive records, routes, or feature-specific copy. Feature components compose primitives outside `components/ui`. |
| Native semantics | Start with the correct HTML element. Prefer a native button, link, input, dialog, list, or disclosure behavior over recreating it with generic elements. |
| Buttons | Default reusable action buttons to `type="button"`. Submit behavior must be explicit. |
| Public API | Keep props small, typed, and orthogonal. Prefer semantic variants over styling flags and snippets over large sets of content props. |
| State hooks | Add a stable root marker such as `data-component` and meaningful state hooks such as `data-state`, `data-disabled`, `data-orientation`, or `data-variant`. Values are stable lowercase English tokens. |
| Accessibility | Native semantics and ARIA remain authoritative. `data-*` never replaces `disabled`, `aria-expanded`, `aria-selected`, `aria-controls`, accessible names, or keyboard behavior. |
| Icons | Import exclusively from direct Lucide modules, for example `import Camera from "@lucide/svelte/icons/camera";`. Never import icons from the `@lucide/svelte` barrel. If the package is missing and an icon is required, add `@lucide/svelte` as a frontend runtime dependency; do not substitute another icon package. |
| Styling | Use Tailwind CSS v4 and semantic tokens already defined in `frontend/src/app.css`. Do not introduce raw palette colors, competing fonts, or consumer-page layout into a primitive. |

Icon-only controls require an accessible name. Decorative icons use `aria-hidden="true"`; meaningful icons must not be the only carrier of status or meaning. Keep visible focus, disabled, error, selected, expanded, and pressed states distinguishable without color alone. Match documented keyboard interaction patterns and preserve at least 44px targets where the design system requires them.

## Storybook Contract

Use the repository's Svelte CSF setup with `defineMeta` from `@storybook/addon-svelte-csf` and `*.stories.svelte` files under `frontend/src/stories`.

Stories are neutral capability galleries. Use labels such as `Label`, `Supporting text`, `Selected`, `Long label`, and stable placeholder IDs instead of realistic archive records or product scenarios. Demonstrate only APIs the component actually supports.

Cover, when public and meaningful:

- every variant and size;
- default, hover/focus guidance, disabled, loading, selected, expanded, invalid, and empty states;
- callback, binding, and keyboard behavior;
- default and named snippet composition;
- long content, wrapped content, and constrained widths;
- light and dark theme behavior;
- compound-component combinations and required parent-child relationships.

Prefer one named story per capability group so differences remain scannable. Do not add `play` functions, interaction tests, test dependencies, or feature fixtures. Story-only child components follow the same PascalCase and lower-case family-folder rules as production components.

## Svelte Documentation Tools

When a Svelte MCP server is available:

1. Call `list-sections` to find the relevant Svelte 5 and SvelteKit documentation.
2. Call `get-documentation` for the exact APIs involved.
3. Run `svelte-autofixer` with Svelte 5 against every changed `.svelte` file, address applicable findings, and repeat until no actionable findings remain.

If the MCP is unavailable, consult the official Svelte documentation and rely on the project's installed compiler and `svelte-check`. Do not add or change project dependencies merely to gain MCP access.

Authoritative references:

- [Svelte 5 migration guide](https://svelte.dev/docs/svelte/v5-migration-guide)
- [Svelte context](https://svelte.dev/docs/svelte/context)
- [When not to use `$effect`](https://svelte.dev/docs/svelte/$effect#When-not-to-use-$effect)
- [Svelte best practices](https://svelte.dev/docs/svelte/best-practices)
- [Storybook for SvelteKit](https://storybook.js.org/docs/get-started/frameworks/sveltekit)

## Common Mistakes

- Creating a feature-specific component in `components/ui` because it might be reused once.
- Duplicating an existing primitive instead of extending or composing it.
- Using Svelte 4 syntax because Svelte 5 still accepts some legacy APIs.
- Mirroring props or derived values through `$effect`.
- Replacing semantic or ARIA state with `data-state`.
- Dropping native attributes, callbacks, consumer classes, or consumer `data-*` attributes in a wrapper.
- Importing icons from the Lucide barrel.
- Adding arbitrary colors or typography instead of the editorial design tokens.
- Co-locating stories with production components or presenting realistic feature examples instead of the public capability surface.

## Completion Check

Before delivery, confirm reuse and placement, PascalCase component naming, lowercase family folders, Svelte 5-only syntax, typed native prop forwarding, a single source of state, justified effects, SSR safety, stable `data-*` hooks, complete accessibility, direct Lucide imports, editorial design tokens, both themes, reduced motion, and matching documentation-only Storybook coverage. Run from `frontend`:

```bash
bun run check
bun run build:storybook
```
