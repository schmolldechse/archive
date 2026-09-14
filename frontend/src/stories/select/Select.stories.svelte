<script module lang="ts">
	import { defineMeta } from "@storybook/addon-svelte-csf";

	import { SelectRoot as SelectRootMeta } from "$lib/components/ui/select";

	const { Story } = defineMeta({
		title: "Components/Select",
		component: SelectRootMeta,
		parameters: {
			docs: {
				description: {
					component:
						"A composable single- or multiple-value listbox with typeahead, collision-aware positioning, labelled field relationships, and hidden-input form serialization. Required state is an application validation contract because hidden inputs do not provide native select constraint validation."
				}
			}
		}
	});
</script>

<script lang="ts">
	import Button from "$lib/components/ui/Button.svelte";
	import {
		SelectContent,
		SelectDescription,
		SelectError,
		SelectGroup,
		SelectGroupLabel,
		SelectItem,
		SelectLabel,
		SelectRoot,
		SelectSeparator,
		SelectTrigger,
		SelectValue,
		SelectViewport,
		type SelectChangeSource
	} from "$lib/components/ui/select";

	const neutralOptions = [
		"Alpha",
		"Bravo",
		"Charlie",
		"Delta",
		"Echo",
		"Foxtrot",
		"Golf",
		"Hotel",
		"India",
		"Juliett",
		"Kilo",
		"Lima"
	];

	let singleValue = $state<string | null>(null);
	let multipleValue = $state<string[]>(["option-b", "option-d"]);
	let groupedValue = $state<string | null>("option-c");
	let controlledValue = $state<string | null>("option-b");
	let controlledOpen = $state(false);
	let controlledSource = $state<SelectChangeSource | "none">("none");
	let controlledValueChanges = $state(0);
	let controlledOpenChanges = $state(0);
	let typeaheadValue = $state<string | null>(null);
	let longValue = $state<string | null>(null);

	function recordControlledValue(value: string | null, source: SelectChangeSource): void {
		controlledValue = value;
		controlledSource = source;
		controlledValueChanges += 1;
	}
</script>

<Story name="Single Selection" asChild>
	<div class="story-stack">
		<SelectRoot type="single" bind:value={singleValue} name="single-option">
			<SelectLabel>Label</SelectLabel>
			<SelectTrigger><SelectValue placeholder="Select an option" /></SelectTrigger>
			<SelectContent>
				<SelectViewport>
					<SelectItem value="option-a">{#snippet children()}Option A{/snippet}</SelectItem>
					<SelectItem value="option-b">{#snippet children()}Option B{/snippet}</SelectItem>
					<SelectItem value="option-c">{#snippet children()}Option C{/snippet}</SelectItem>
				</SelectViewport>
			</SelectContent>
		</SelectRoot>
		<div class="story-register" aria-live="polite"><span>Selected value</span><code>{singleValue ?? "null"}</code></div>
	</div>
</Story>

<Story name="Multiple Selection" asChild>
	<div class="story-stack">
		<p class="story-note">Activating an option toggles it without closing the list. Values remain in option document order.</p>
		<SelectRoot type="multiple" bind:value={multipleValue} name="multiple-option">
			<SelectLabel>Multiple label</SelectLabel>
			<SelectTrigger><SelectValue placeholder="Select options" /></SelectTrigger>
			<SelectContent>
				<SelectViewport>
					<SelectItem value="option-a">{#snippet children()}Option A{/snippet}</SelectItem>
					<SelectItem value="option-b">{#snippet children()}Option B{/snippet}</SelectItem>
					<SelectItem value="option-c">{#snippet children()}Option C{/snippet}</SelectItem>
					<SelectItem value="option-d">{#snippet children()}Option D{/snippet}</SelectItem>
				</SelectViewport>
			</SelectContent>
		</SelectRoot>
		<div class="story-register" aria-live="polite">
			<span>Ordered values</span><code>{JSON.stringify(multipleValue)}</code>
		</div>
	</div>
</Story>

<Story name="Groups and Separators" asChild>
	<div class="story-stack">
		<SelectRoot type="single" bind:value={groupedValue}>
			<SelectLabel>Grouped label</SelectLabel>
			<SelectTrigger><SelectValue /></SelectTrigger>
			<SelectContent>
				<SelectViewport>
					<SelectGroup>
						<SelectGroupLabel>First group</SelectGroupLabel>
						<SelectItem value="option-a">{#snippet children()}Option A{/snippet}</SelectItem>
						<SelectItem value="option-b">{#snippet children()}Option B{/snippet}</SelectItem>
					</SelectGroup>
					<SelectSeparator />
					<SelectGroup>
						<SelectGroupLabel>Second group</SelectGroupLabel>
						<SelectItem value="option-c">{#snippet children()}Option C{/snippet}</SelectItem>
						<SelectItem value="option-d">{#snippet children()}Option D{/snippet}</SelectItem>
					</SelectGroup>
				</SelectViewport>
			</SelectContent>
		</SelectRoot>
	</div>
</Story>

<Story name="Field States" asChild>
	<div class="story-grid">
		<SelectRoot type="single" required>
			<SelectLabel>Required label (required)</SelectLabel>
			<SelectTrigger><SelectValue /></SelectTrigger>
			<SelectDescription>This selection must be completed before submission.</SelectDescription>
			<SelectContent>
				<SelectViewport>
					<SelectItem value="option-a">{#snippet children()}Option A{/snippet}</SelectItem>
					<SelectItem value="option-b">{#snippet children()}Option B{/snippet}</SelectItem>
				</SelectViewport>
			</SelectContent>
		</SelectRoot>

		<SelectRoot type="single" required invalid>
			<SelectLabel>Invalid label (required)</SelectLabel>
			<SelectTrigger><SelectValue /></SelectTrigger>
			<SelectDescription>Choose one of the available options.</SelectDescription>
			<SelectError>Select an option before continuing.</SelectError>
			<SelectContent>
				<SelectViewport>
					<SelectItem value="option-a">{#snippet children()}Option A{/snippet}</SelectItem>
					<SelectItem value="option-b">{#snippet children()}Option B{/snippet}</SelectItem>
				</SelectViewport>
			</SelectContent>
		</SelectRoot>

		<SelectRoot type="single" value="option-a" disabled>
			<SelectLabel>Disabled label</SelectLabel>
			<SelectTrigger><SelectValue /></SelectTrigger>
			<SelectDescription>This field is unavailable.</SelectDescription>
			<SelectContent>
				<SelectViewport>
					<SelectItem value="option-a">{#snippet children()}Option A{/snippet}</SelectItem>
				</SelectViewport>
			</SelectContent>
		</SelectRoot>
	</div>
</Story>

<Story name="Availability and Empty Selection" asChild>
	<div class="story-grid">
		<SelectRoot type="single" allowDeselect value={null}>
			<SelectLabel>Empty selection</SelectLabel>
			<SelectTrigger><SelectValue placeholder="No option selected" /></SelectTrigger>
			<SelectContent>
				<SelectViewport>
					<SelectItem value="option-a">{#snippet children()}Option A{/snippet}</SelectItem>
					<SelectItem value="option-b">{#snippet children()}Option B{/snippet}</SelectItem>
				</SelectViewport>
			</SelectContent>
		</SelectRoot>

		<SelectRoot type="single" value="option-b">
			<SelectLabel>Disabled option</SelectLabel>
			<SelectTrigger><SelectValue /></SelectTrigger>
			<SelectContent>
				<SelectViewport>
					<SelectItem value="option-a">{#snippet children()}Option A{/snippet}</SelectItem>
					<SelectItem value="option-b">{#snippet children()}Option B{/snippet}</SelectItem>
					<SelectItem value="option-c" disabled>{#snippet children()}Unavailable option{/snippet}</SelectItem>
					<SelectItem value="option-d">{#snippet children()}Option D{/snippet}</SelectItem>
				</SelectViewport>
			</SelectContent>
		</SelectRoot>
	</div>
</Story>

<Story name="Bound Value and Open State" asChild>
	<div class="story-stack">
		<div class="story-actions">
			<Button onclick={() => (controlledOpen = true)}>Open programmatically</Button>
			<Button onclick={() => (controlledOpen = false)}>Close programmatically</Button>
			<Button onclick={() => (controlledValue = null)}>Clear value</Button>
		</div>
		<SelectRoot
			type="single"
			bind:value={controlledValue}
			bind:open={controlledOpen}
			onValueChange={recordControlledValue}
			onOpenChange={() => (controlledOpenChanges += 1)}
		>
			<SelectLabel>Controlled label</SelectLabel>
			<SelectTrigger><SelectValue /></SelectTrigger>
			<SelectContent>
				<SelectViewport>
					<SelectItem value="option-a">{#snippet children()}Option A{/snippet}</SelectItem>
					<SelectItem value="option-b">{#snippet children()}Option B{/snippet}</SelectItem>
					<SelectItem value="option-c">{#snippet children()}Option C{/snippet}</SelectItem>
				</SelectViewport>
			</SelectContent>
		</SelectRoot>
		<div class="story-register" aria-live="polite">
			<span>Value</span><code>{controlledValue ?? "null"}</code>
			<span>Open</span><code>{String(controlledOpen)}</code>
			<span>Source</span><code>{controlledSource}</code>
			<span>Value changes</span><code>{controlledValueChanges}</code>
			<span>Open changes</span><code>{controlledOpenChanges}</code>
		</div>
	</div>
</Story>

<Story name="Typeahead" asChild>
	<div class="story-stack">
		<p class="story-note">Open the list and type a prefix. Repeated characters cycle through matching enabled options.</p>
		<SelectRoot type="single" bind:value={typeaheadValue} loop typeahead>
			<SelectLabel>Typeahead label</SelectLabel>
			<SelectTrigger><SelectValue /></SelectTrigger>
			<SelectContent>
				<SelectViewport>
					{#each neutralOptions as option}
						<SelectItem value={option.toLocaleLowerCase()} textValue={option}>
							{#snippet children()}{option} option{/snippet}
						</SelectItem>
					{/each}
				</SelectViewport>
			</SelectContent>
		</SelectRoot>
		<div class="story-register" aria-live="polite"><span>Selected value</span><code>{typeaheadValue ?? "null"}</code></div>
	</div>
</Story>

<Story name="Content Resilience" asChild>
	<div class="story-constrained">
		<SelectRoot type="single" bind:value={longValue}>
			<SelectLabel>A long label remains visible within the constrained field width</SelectLabel>
			<SelectTrigger><SelectValue placeholder="Select a wrapping option" /></SelectTrigger>
			<SelectDescription>Complete labels wrap instead of being truncated.</SelectDescription>
			<SelectContent class="story-long-content">
				<SelectViewport class="story-long-viewport">
					<SelectItem value="long-featured-alpha" textValue="Alpha">
						{#snippet children()}A deliberately long option label that wraps across multiple lines without losing information{/snippet}
					</SelectItem>
					{#each neutralOptions as option}
						<SelectItem value={`long-${option.toLocaleLowerCase()}`} textValue={option}>
							{#snippet children()}{option} option with supporting neutral wording{/snippet}
						</SelectItem>
					{/each}
				</SelectViewport>
			</SelectContent>
		</SelectRoot>
	</div>
</Story>

<Story name="Positioning and Scrolling" asChild>
	<div class="story-positioning">
		<div class="story-edge story-edge--start">
			<SelectRoot type="single">
				<SelectLabel>Start edge</SelectLabel>
				<SelectTrigger><SelectValue /></SelectTrigger>
				<SelectContent side="bottom" align="start">
					<SelectViewport>
						<SelectItem value="option-a">{#snippet children()}Option A{/snippet}</SelectItem>
						<SelectItem value="option-b">{#snippet children()}Option B{/snippet}</SelectItem>
					</SelectViewport>
				</SelectContent>
			</SelectRoot>
		</div>
		<div class="story-edge story-edge--end">
			<SelectRoot type="single">
				<SelectLabel>End edge</SelectLabel>
				<SelectTrigger><SelectValue /></SelectTrigger>
				<SelectContent side="top" align="end">
					<SelectViewport>
						<SelectItem value="option-c">{#snippet children()}Option C{/snippet}</SelectItem>
						<SelectItem value="option-d">{#snippet children()}Option D{/snippet}</SelectItem>
					</SelectViewport>
				</SelectContent>
			</SelectRoot>
		</div>
		<div class="story-scroll-container">
			<div class="story-scroll-spacer">Scroll to the field below.</div>
			<SelectRoot type="single">
				<SelectLabel>Label inside scrolling container</SelectLabel>
				<SelectTrigger><SelectValue /></SelectTrigger>
				<SelectContent collisionPadding={12}>
					<SelectViewport>
						{#each neutralOptions.slice(0, 6) as option}
							<SelectItem value={`scroll-${option.toLocaleLowerCase()}`} textValue={option}>
								{#snippet children()}{option} option{/snippet}
							</SelectItem>
						{/each}
					</SelectViewport>
				</SelectContent>
			</SelectRoot>
		</div>
	</div>
</Story>

<Story name="Keyboard Reference" asChild>
	<div class="story-keyboard-layout">
		<div class="story-guide">
			<dl>
				<dt><kbd>Enter</kbd> <kbd>Space</kbd></dt><dd>Open or select the highlighted option</dd>
				<dt><kbd>↓</kbd> <kbd>↑</kbd></dt><dd>Open or move among enabled options</dd>
				<dt><kbd>Home</kbd> <kbd>End</kbd></dt><dd>Move to the first or last enabled option</dd>
				<dt><kbd>A–Z</kbd></dt><dd>Match the buffered option text</dd>
				<dt><kbd>Esc</kbd></dt><dd>Close without changing selection and return focus</dd>
				<dt><kbd>Tab</kbd></dt><dd>Close and continue normal document order</dd>
			</dl>
		</div>
		<div class="story-stack">
			<SelectRoot type="multiple" loop>
				<SelectLabel>Keyboard practice</SelectLabel>
				<SelectTrigger><SelectValue placeholder="Select options" /></SelectTrigger>
				<SelectContent>
					<SelectViewport>
						<SelectItem value="alpha" textValue="Alpha">{#snippet children()}Alpha option{/snippet}</SelectItem>
						<SelectItem value="bravo" textValue="Bravo">{#snippet children()}Bravo option{/snippet}</SelectItem>
						<SelectItem value="blocked" disabled>{#snippet children()}Unavailable option{/snippet}</SelectItem>
						<SelectItem value="charlie" textValue="Charlie">{#snippet children()}Charlie option{/snippet}</SelectItem>
					</SelectViewport>
				</SelectContent>
			</SelectRoot>
			<Button>Next focusable control</Button>
		</div>
	</div>
</Story>

<style>
	.story-stack,
	.story-constrained {
		display: grid;
		max-width: 36rem;
		gap: 1.5rem;
		font-family: var(--font-interface);
	}

	.story-grid {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(min(100%, 18rem), 1fr));
		gap: 2rem;
		max-width: 64rem;
	}

	.story-constrained {
		width: 18rem;
		max-width: 100%;
	}

	.story-note {
		margin: 0;
		border-inline-start: 2px solid var(--register-mark);
		padding-inline-start: 0.75rem;
		color: var(--marginal-note);
		font-size: 0.875rem;
		line-height: 1.43;
	}

	.story-actions,
	.story-register {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: 0.75rem;
	}

	.story-register {
		color: var(--marginal-note);
		font-family: var(--font-interface);
		font-size: 0.875rem;
	}

	.story-register code {
		color: var(--reading-ink);
		font-family: var(--font-record);
		overflow-wrap: anywhere;
	}

	:global(.story-long-content) {
		width: 18rem;
		max-width: calc(100vw - 1.5rem);
	}

	:global(.story-long-viewport) {
		max-height: 16rem;
	}

	.story-positioning {
		display: grid;
		min-height: 40rem;
		grid-template-columns: repeat(2, minmax(0, 1fr));
		grid-template-rows: 1fr auto;
		gap: 2rem;
		border: 1px solid var(--border-trace);
		padding: 1rem;
		font-family: var(--font-interface);
	}

	.story-edge {
		display: flex;
		width: min(100%, 18rem);
	}

	.story-edge--start {
		align-items: flex-start;
		justify-self: start;
	}

	.story-edge--end {
		align-items: flex-end;
		justify-self: end;
	}

	.story-scroll-container {
		display: grid;
		height: 14rem;
		grid-column: 1 / -1;
		gap: 1rem;
		overflow: auto;
		border-block: 1px solid var(--border-trace);
		padding: 1rem;
	}

	.story-scroll-spacer {
		display: grid;
		min-height: 12rem;
		place-items: center;
		color: var(--marginal-note);
	}

	.story-keyboard-layout {
		display: grid;
		grid-template-columns: minmax(18rem, 1fr) minmax(16rem, 22rem);
		gap: 2rem;
		max-width: 64rem;
		font-family: var(--font-interface);
	}

	.story-guide {
		border-block: 1px solid var(--border-trace);
		padding-block: 1rem;
	}

	.story-guide dl {
		display: grid;
		grid-template-columns: minmax(10rem, auto) 1fr;
		gap: 0.75rem 1rem;
		margin: 0;
	}

	.story-guide dt {
		font-weight: 600;
	}

	.story-guide dd {
		margin: 0;
		color: var(--marginal-note);
	}

	.story-guide kbd {
		border: 1px solid var(--border-trace);
		border-radius: var(--radius-mark);
		background: var(--archive-layer);
		padding: 0.125rem 0.375rem;
		font-family: var(--font-record);
		font-size: 0.75rem;
	}

	@media (max-width: 44rem) {
		.story-positioning,
		.story-keyboard-layout {
			grid-template-columns: 1fr;
		}

		.story-positioning {
			grid-template-rows: repeat(2, minmax(12rem, auto)) auto;
		}

		.story-scroll-container {
			grid-column: auto;
		}

		.story-guide dl {
			grid-template-columns: 1fr;
			gap: 0.25rem;
		}

		.story-guide dd:not(:last-child) {
			margin-block-end: 0.75rem;
		}
	}
</style>
