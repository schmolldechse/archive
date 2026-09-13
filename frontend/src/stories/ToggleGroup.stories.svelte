<script module lang="ts">
	import { defineMeta } from "@storybook/addon-svelte-csf";

	import { ToggleGroupRoot as ToggleGroupRootMeta } from "$lib/components/ui/toggle-group";

	const { Story } = defineMeta({
		title: "Components/Toggle Group",
		component: ToggleGroupRootMeta,
		parameters: {
			docs: {
				description: {
					component:
						"A labelled set of related toggle buttons with single or multiple selection, optional required selection, and configurable focus management."
				}
			}
		}
	});
</script>

<script lang="ts">
	import { ToggleGroupItem, ToggleGroupRoot } from "$lib/components/ui/toggle-group";

	let optionalSingle = $state<string | null>("label");
	let requiredSingle = $state<string | null>("selected");
	let horizontalValue = $state<string | null>("label");
	let verticalValue = $state<string | null>("supporting");
	let rovingFocusLabel = $state("None");
	let ordinaryFocusLabel = $state("None");
	let boundedFocusLabel = $state("None");
	let boundSingle = $state<string | null>(null);
	let boundMultiple = $state<string[]>(["label"]);
	let singleChangeCount = $state(0);
	let multipleChangeCount = $state(0);
</script>

<Story name="Single Selection" asChild>
	<div class="story-grid">
		<section class="story-section">
			<p class="story-label">Optional selection</p>
			<ToggleGroupRoot type="single" bind:value={optionalSingle} aria-label="Optional single selection">
				<ToggleGroupItem value="label">Label</ToggleGroupItem>
				<ToggleGroupItem value="supporting">Supporting text</ToggleGroupItem>
				<ToggleGroupItem value="details">Details</ToggleGroupItem>
			</ToggleGroupRoot>
			<p class="story-register">Value <code>{optionalSingle ?? "none"}</code></p>
		</section>

		<section class="story-section">
			<p class="story-label">Required selection</p>
			<ToggleGroupRoot type="single" bind:value={requiredSingle} selectionRequired aria-label="Required single selection">
				<ToggleGroupItem value="selected">Selected</ToggleGroupItem>
				<ToggleGroupItem value="supporting">Supporting text</ToggleGroupItem>
				<ToggleGroupItem value="details">Details</ToggleGroupItem>
			</ToggleGroupRoot>
			<p class="story-register">Value <code>{requiredSingle ?? "none"}</code></p>
		</section>
	</div>
</Story>

<Story name="Multiple Selection" asChild>
	<div class="story-stack">
		<section class="story-section">
			<p class="story-label">Zero selected</p>
			<ToggleGroupRoot type="multiple" value={[]} aria-label="Multiple selection with no initial value">
				<ToggleGroupItem value="label">Label</ToggleGroupItem>
				<ToggleGroupItem value="supporting">Supporting text</ToggleGroupItem>
				<ToggleGroupItem value="details">Details</ToggleGroupItem>
			</ToggleGroupRoot>
		</section>

		<section class="story-section">
			<p class="story-label">One selected</p>
			<ToggleGroupRoot type="multiple" value={["supporting"]} aria-label="Multiple selection with one initial value">
				<ToggleGroupItem value="label">Label</ToggleGroupItem>
				<ToggleGroupItem value="supporting">Supporting text</ToggleGroupItem>
				<ToggleGroupItem value="details">Details</ToggleGroupItem>
			</ToggleGroupRoot>
		</section>

		<section class="story-section">
			<p class="story-label">Several selected</p>
			<ToggleGroupRoot type="multiple" value={["label", "details"]} aria-label="Multiple selection with several initial values">
				<ToggleGroupItem value="label">Label</ToggleGroupItem>
				<ToggleGroupItem value="supporting">Supporting text</ToggleGroupItem>
				<ToggleGroupItem value="details">Details</ToggleGroupItem>
			</ToggleGroupRoot>
		</section>
	</div>
</Story>

<Story name="Orientations" asChild>
	<div class="story-grid">
		<section class="story-section">
			<p class="story-label">Horizontal</p>
			<p class="story-note">Left and Right move focus. Home and End jump to the sequence boundaries.</p>
			<ToggleGroupRoot type="single" bind:value={horizontalValue} aria-label="Horizontal toggle group">
				<ToggleGroupItem value="label">Label</ToggleGroupItem>
				<ToggleGroupItem value="supporting">Supporting text</ToggleGroupItem>
				<ToggleGroupItem value="details">Details</ToggleGroupItem>
			</ToggleGroupRoot>
		</section>

		<section class="story-section">
			<p class="story-label">Vertical</p>
			<p class="story-note">Up and Down move focus. Selection changes only after activation.</p>
			<ToggleGroupRoot type="single" bind:value={verticalValue} orientation="vertical" aria-label="Vertical toggle group">
				<ToggleGroupItem value="label">Label</ToggleGroupItem>
				<ToggleGroupItem value="supporting">Supporting text</ToggleGroupItem>
				<ToggleGroupItem value="details">Details</ToggleGroupItem>
			</ToggleGroupRoot>
		</section>
	</div>
</Story>

<Story name="Focus Modes" asChild>
	<div class="story-grid">
		<section class="story-section">
			<p class="story-label">Roving focus</p>
			<p class="story-note">Tab enters once; arrow keys move among enabled items and skip Disabled.</p>
			<ToggleGroupRoot type="single" aria-label="Roving focus example">
				<ToggleGroupItem value="label" onfocus={() => (rovingFocusLabel = "Label")}>Label</ToggleGroupItem>
				<ToggleGroupItem value="disabled" disabled onfocus={() => (rovingFocusLabel = "Disabled")}>Disabled</ToggleGroupItem>
				<ToggleGroupItem value="details" onfocus={() => (rovingFocusLabel = "Details")}>Details</ToggleGroupItem>
			</ToggleGroupRoot>
			<p class="story-register">Focused <code>{rovingFocusLabel}</code></p>
		</section>

		<section class="story-section">
			<p class="story-label">Ordinary Tab order</p>
			<p class="story-note">Every enabled item is a Tab stop; arrow keys retain their native behavior.</p>
			<ToggleGroupRoot type="single" rovingFocus={false} aria-label="Ordinary tab order example">
				<ToggleGroupItem value="label" onfocus={() => (ordinaryFocusLabel = "Label")}>Label</ToggleGroupItem>
				<ToggleGroupItem value="supporting" onfocus={() => (ordinaryFocusLabel = "Supporting text")}
					>Supporting text</ToggleGroupItem
				>
				<ToggleGroupItem value="details" onfocus={() => (ordinaryFocusLabel = "Details")}>Details</ToggleGroupItem>
			</ToggleGroupRoot>
			<p class="story-register">Focused <code>{ordinaryFocusLabel}</code></p>
		</section>

		<section class="story-section">
			<p class="story-label">Non-looping focus</p>
			<p class="story-note">At either boundary, the matching arrow key keeps focus on the current item.</p>
			<ToggleGroupRoot type="single" loop={false} aria-label="Non-looping roving focus example">
				<ToggleGroupItem value="first" onfocus={() => (boundedFocusLabel = "First")}>First</ToggleGroupItem>
				<ToggleGroupItem value="second" onfocus={() => (boundedFocusLabel = "Second")}>Second</ToggleGroupItem>
				<ToggleGroupItem value="last" onfocus={() => (boundedFocusLabel = "Last")}>Last</ToggleGroupItem>
			</ToggleGroupRoot>
			<p class="story-register">Focused <code>{boundedFocusLabel}</code></p>
		</section>
	</div>
</Story>

<Story name="Group and Toolbar Semantics" asChild>
	<div class="story-grid">
		<section class="story-section">
			<p class="story-label">Group</p>
			<ToggleGroupRoot type="multiple" aria-label="Neutral filter controls">
				<ToggleGroupItem value="label">Label</ToggleGroupItem>
				<ToggleGroupItem value="supporting">Supporting text</ToggleGroupItem>
				<ToggleGroupItem value="details">Details</ToggleGroupItem>
			</ToggleGroupRoot>
		</section>

		<section class="story-section">
			<p class="story-label">Toolbar</p>
			<ToggleGroupRoot type="multiple" semanticRole="toolbar" value={["emphasis"]} aria-label="Neutral tool controls">
				<ToggleGroupItem value="emphasis">Emphasis</ToggleGroupItem>
				<ToggleGroupItem value="annotation">Annotation</ToggleGroupItem>
				<ToggleGroupItem value="reference">Reference</ToggleGroupItem>
			</ToggleGroupRoot>
		</section>
	</div>
</Story>

<Story name="States" asChild>
	<div class="story-stack">
		<section class="story-section">
			<p class="story-label">Selected, unselected, and item disabled</p>
			<ToggleGroupRoot type="multiple" value={["selected"]} aria-label="Individual item states">
				<ToggleGroupItem value="selected" class="stateful-item">
					{#snippet children({ pressed })}
						<span>Selected</span>
						<span class="state-word" aria-hidden="true">{pressed ? "On" : "Off"}</span>
					{/snippet}
				</ToggleGroupItem>
				<ToggleGroupItem value="unselected">Unselected</ToggleGroupItem>
				<ToggleGroupItem value="disabled" disabled>Disabled</ToggleGroupItem>
			</ToggleGroupRoot>
		</section>

		<section class="story-section">
			<p class="story-label">Root disabled</p>
			<ToggleGroupRoot type="single" value="selected" disabled aria-label="Disabled toggle group">
				<ToggleGroupItem value="selected">Selected</ToggleGroupItem>
				<ToggleGroupItem value="unselected">Unselected</ToggleGroupItem>
			</ToggleGroupRoot>
		</section>

		<section class="story-section story-constrained">
			<p class="story-label">Long labels</p>
			<ToggleGroupRoot type="single" aria-label="Long label example">
				<ToggleGroupItem value="long">
					A long label wraps across several lines without losing its complete accessible text
				</ToggleGroupItem>
				<ToggleGroupItem value="details">Details</ToggleGroupItem>
			</ToggleGroupRoot>
		</section>
	</div>
</Story>

<Story name="Bound Values" asChild>
	<div class="story-grid">
		<section class="story-section">
			<p class="story-label">Single value</p>
			<ToggleGroupRoot
				type="single"
				bind:value={boundSingle}
				onValueChange={() => (singleChangeCount += 1)}
				aria-label="Bound single value"
			>
				<ToggleGroupItem value="label">Label</ToggleGroupItem>
				<ToggleGroupItem value="details">Details</ToggleGroupItem>
			</ToggleGroupRoot>
			<div class="story-register" aria-live="polite">
				<span>Value</span>
				<code>{boundSingle ?? "none"}</code>
				<span>Accepted changes</span>
				<code>{singleChangeCount}</code>
			</div>
		</section>

		<section class="story-section">
			<p class="story-label">Array value</p>
			<ToggleGroupRoot
				type="multiple"
				bind:value={boundMultiple}
				onValueChange={() => (multipleChangeCount += 1)}
				aria-label="Bound multiple value"
			>
				<ToggleGroupItem value="label">Label</ToggleGroupItem>
				<ToggleGroupItem value="supporting">Supporting text</ToggleGroupItem>
				<ToggleGroupItem value="details">Details</ToggleGroupItem>
			</ToggleGroupRoot>
			<div class="story-register" aria-live="polite">
				<span>Values</span>
				<code>{boundMultiple.length === 0 ? "none" : boundMultiple.join(", ")}</code>
				<span>Accepted changes</span>
				<code>{multipleChangeCount}</code>
			</div>
		</section>
	</div>
</Story>

<style>
	.story-stack,
	.story-section {
		display: grid;
	}

	.story-stack {
		max-width: 52rem;
		gap: 2rem;
	}

	.story-grid {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(min(100%, 22rem), 1fr));
		gap: 3rem;
	}

	.story-section {
		align-content: start;
		justify-items: start;
		gap: 0.75rem;
	}

	.story-label,
	.story-note,
	.story-register {
		font-family: var(--font-interface);
	}

	.story-label {
		margin: 0;
		color: var(--marginal-note);
		font-size: 0.75rem;
		font-weight: 600;
		letter-spacing: 0.08em;
		text-transform: uppercase;
	}

	.story-note {
		max-width: 36rem;
		margin: 0;
		border-inline-start: 2px solid var(--register-mark);
		padding-inline-start: 1rem;
		color: var(--marginal-note);
		line-height: 1.5;
	}

	.story-register {
		display: flex;
		flex-wrap: wrap;
		gap: 0.5rem 1rem;
		margin: 0;
		color: var(--marginal-note);
		font-size: 0.875rem;
	}

	.story-register code {
		color: var(--reading-ink);
		font-family: var(--font-record);
	}

	.story-constrained {
		max-width: 22rem;
	}

	:global(.stateful-item) {
		gap: 0.5rem;
	}

	.state-word {
		font-family: var(--font-record);
		font-size: 0.75rem;
		font-weight: 500;
		text-transform: uppercase;
	}
</style>
