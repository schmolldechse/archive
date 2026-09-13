<script module lang="ts">
	import { defineMeta } from "@storybook/addon-svelte-csf";

	import { TabsRoot as TabsRootMeta } from "$lib/components/ui/tabs";

	const { Story } = defineMeta({
		title: "Components/Tabs",
		component: TabsRootMeta,
		parameters: {
			docs: {
				description: {
					component:
						"A composable tab set for switching between peer panels, with automatic or manual activation and horizontal or vertical keyboard navigation."
				}
			}
		}
	});
</script>

<script lang="ts">
	import Button from "$lib/components/ui/Button.svelte";
	import { TabsList, TabsPanel, TabsRoot, TabsTrigger } from "$lib/components/ui/tabs";

	let manualValue = $state("label");
	let manualFocus = $state("None");
	let controlledValue = $state("label");
	let controlledChanges = $state(0);
	let unmountedValue = $state("first");
	let mountedValue = $state("first");

	const handleManualBlur = (event: FocusEvent): void => {
		if (
			!(event.relatedTarget instanceof Element) ||
			!event.relatedTarget.closest('[role="tablist"][aria-label="Manual activation example"]')
		) {
			manualFocus = "None";
		}
	};
</script>

<Story name="Automatic Activation" asChild>
	<div class="story-stack">
		<p class="story-note">
			Focus a tab, then use Left and Right. Focus and selection move together because every panel is immediately available.
		</p>
		<TabsRoot>
			<TabsList aria-label="Automatic activation example">
				<TabsTrigger value="label">Label</TabsTrigger>
				<TabsTrigger value="supporting">Supporting text</TabsTrigger>
				<TabsTrigger value="details">Details</TabsTrigger>
			</TabsList>
			<TabsPanel value="label"><p>Neutral content for the first peer panel.</p></TabsPanel>
			<TabsPanel value="supporting"><p>Supporting text for the second peer panel.</p></TabsPanel>
			<TabsPanel value="details"><p>Additional detail for the third peer panel.</p></TabsPanel>
		</TabsRoot>
	</div>
</Story>

<Story name="Manual Activation" asChild>
	<div class="story-stack">
		<p class="story-note">
			Arrow keys move focus without changing the panel. Press Enter or Space to activate it; focus stops at either end because
			looping is disabled.
		</p>
		<div class="story-register" aria-live="polite">
			<span>Focused</span>
			<code>{manualFocus}</code>
			<span>Selected</span>
			<code>{manualValue}</code>
		</div>
		<TabsRoot activationMode="manual" bind:value={manualValue} loop={false}>
			<TabsList aria-label="Manual activation example">
				<TabsTrigger value="label" onfocus={() => (manualFocus = "Label")} onblur={handleManualBlur}>Label</TabsTrigger>
				<TabsTrigger value="supporting" onfocus={() => (manualFocus = "Supporting text")} onblur={handleManualBlur}>
					Supporting text
				</TabsTrigger>
				<TabsTrigger value="details" onfocus={() => (manualFocus = "Details")} onblur={handleManualBlur}>Details</TabsTrigger>
			</TabsList>
			<TabsPanel value="label"><p>The selected value remains stable while focus moves.</p></TabsPanel>
			<TabsPanel value="supporting"><p>This panel appears only after explicit activation.</p></TabsPanel>
			<TabsPanel value="details"><p>Manual activation suits content with a meaningful loading cost.</p></TabsPanel>
		</TabsRoot>
	</div>
</Story>

<Story name="Orientations" asChild>
	<div class="story-grid">
		<section>
			<p class="story-label">Horizontal</p>
			<TabsRoot value="label">
				<TabsList aria-label="Horizontal tabs">
					<TabsTrigger value="label">Label</TabsTrigger>
					<TabsTrigger value="details">Details</TabsTrigger>
				</TabsList>
				<TabsPanel value="label"><p>Left and Right move between horizontal tabs.</p></TabsPanel>
				<TabsPanel value="details"><p>The active position is marked along the bottom edge.</p></TabsPanel>
			</TabsRoot>
		</section>
		<section>
			<p class="story-label">Vertical</p>
			<TabsRoot value="label" orientation="vertical">
				<TabsList aria-label="Vertical tabs">
					<TabsTrigger value="label">Label</TabsTrigger>
					<TabsTrigger value="supporting">Supporting text</TabsTrigger>
					<TabsTrigger value="details">Details</TabsTrigger>
				</TabsList>
				<TabsPanel value="label"><p>Up and Down move between vertical tabs.</p></TabsPanel>
				<TabsPanel value="supporting"><p>The register line follows the logical start edge.</p></TabsPanel>
				<TabsPanel value="details"><p>Home and End work in either orientation.</p></TabsPanel>
			</TabsRoot>
		</section>
	</div>
</Story>

<Story name="Controlled Value" asChild>
	<div class="story-stack">
		<div class="story-actions">
			<Button onclick={() => (controlledValue = "label")}>Select label</Button>
			<Button onclick={() => (controlledValue = "details")}>Select details</Button>
		</div>
		<div class="story-register" aria-live="polite">
			<span>Value</span>
			<code>{controlledValue}</code>
			<span>User changes</span>
			<code>{controlledChanges}</code>
		</div>
		<TabsRoot bind:value={controlledValue} onValueChange={() => (controlledChanges += 1)}>
			<TabsList aria-label="Controlled value example">
				<TabsTrigger value="label">Label</TabsTrigger>
				<TabsTrigger value="supporting">Supporting text</TabsTrigger>
				<TabsTrigger value="details">
					{#snippet children({ selected })}
						Details{selected ? " · Selected" : ""}
					{/snippet}
				</TabsTrigger>
			</TabsList>
			<TabsPanel value="label"><p>External controls and tab activation share one source value.</p></TabsPanel>
			<TabsPanel value="supporting"><p>The binding exposes the current tab identity.</p></TabsPanel>
			<TabsPanel value="details"><p>The callback reports accepted user activation once.</p></TabsPanel>
		</TabsRoot>
	</div>
</Story>

<Story name="Disabled Triggers" asChild>
	<div class="story-stack">
		<section>
			<p class="story-label">Disabled item</p>
			<TabsRoot value="label">
				<TabsList aria-label="Tabs with a disabled item">
					<TabsTrigger value="label">Label</TabsTrigger>
					<TabsTrigger value="disabled" disabled>Disabled</TabsTrigger>
					<TabsTrigger value="details">Details</TabsTrigger>
				</TabsList>
				<TabsPanel value="label"><p>Arrow navigation skips the disabled trigger.</p></TabsPanel>
				<TabsPanel value="disabled"><p>This panel cannot be activated by its disabled trigger.</p></TabsPanel>
				<TabsPanel value="details"><p>Enabled tabs remain available.</p></TabsPanel>
			</TabsRoot>
		</section>
		<section>
			<p class="story-label">Disabled root</p>
			<TabsRoot value="label" disabled>
				<TabsList aria-label="Disabled tab set">
					<TabsTrigger value="label">Selected</TabsTrigger>
					<TabsTrigger value="details">Details</TabsTrigger>
				</TabsList>
				<TabsPanel value="label"><p>The selected panel remains visible while every trigger is disabled.</p></TabsPanel>
				<TabsPanel value="details"><p>Inactive content remains unavailable.</p></TabsPanel>
			</TabsRoot>
		</section>
	</div>
</Story>

<Story name="Mounting" asChild>
	<div class="story-grid">
		<section>
			<p class="story-label">Unmounted by default</p>
			<TabsRoot bind:value={unmountedValue}>
				<TabsList aria-label="Unmounted panels example">
					<TabsTrigger value="first">First</TabsTrigger>
					<TabsTrigger value="second">Second</TabsTrigger>
				</TabsList>
				<TabsPanel value="first">
					<label for="tabs-unmounted-field">Editable value</label>
					<input id="tabs-unmounted-field" value="Reset after unmounting" />
				</TabsPanel>
				<TabsPanel value="second"><p>The inactive first panel is absent from the DOM.</p></TabsPanel>
			</TabsRoot>
		</section>
		<section>
			<p class="story-label">Force mounted</p>
			<TabsRoot bind:value={mountedValue}>
				<TabsList aria-label="Force-mounted panels example">
					<TabsTrigger value="first">First</TabsTrigger>
					<TabsTrigger value="second">Second</TabsTrigger>
				</TabsList>
				<TabsPanel value="first" forceMount>
					<label for="tabs-mounted-field">Editable value</label>
					<input id="tabs-mounted-field" value="Preserved while hidden" />
				</TabsPanel>
				<TabsPanel value="second" forceMount><p>Inactive panels stay mounted with the hidden attribute.</p></TabsPanel>
			</TabsRoot>
		</section>
	</div>
</Story>

<Story name="Content Resilience" asChild>
	<div class="story-constrained">
		<TabsRoot>
			<TabsList aria-label="Content resilience example">
				<TabsTrigger value="long">
					A long label wraps across several lines without losing its complete accessible text
				</TabsTrigger>
				<TabsTrigger value="record"><span class="record-value">2026-09-09 · 14:32 UTC</span></TabsTrigger>
				<TabsTrigger value="details">Details</TabsTrigger>
			</TabsList>
			<TabsPanel value="long">
				<p>
					Long panel content reflows naturally within a constrained width. The component does not truncate labels or impose a
					decorative panel container, so reading structure remains under consumer control.
				</p>
			</TabsPanel>
			<TabsPanel value="record">
				<dl>
					<dt>Label</dt>
					<dd>Supporting text</dd>
					<dt>Identifier</dt>
					<dd class="record-value">placeholder-record-0001</dd>
				</dl>
			</TabsPanel>
			<TabsPanel value="details">
				<a href="#tabs-story-destination">Focusable destination</a>
				<p>A focusable first element prevents the panel itself from adding a redundant Tab stop.</p>
			</TabsPanel>
		</TabsRoot>
	</div>
</Story>

<div id="tabs-story-destination" class="story-destination">Destination</div>

<style>
	.story-stack {
		display: grid;
		max-width: 48rem;
		gap: 1.5rem;
	}

	.story-grid {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(min(100%, 22rem), 1fr));
		gap: 3rem;
	}

	.story-actions,
	.story-register {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: 0.5rem 1rem;
	}

	.story-label,
	.story-note,
	.story-register,
	.story-constrained,
	.story-destination {
		font-family: var(--font-interface);
	}

	.story-label {
		margin: 0 0 0.75rem;
		color: var(--marginal-note);
		font-size: 0.75rem;
		font-weight: 600;
		letter-spacing: 0.08em;
		text-transform: uppercase;
	}

	.story-note {
		margin: 0;
		border-inline-start: 2px solid var(--register-mark);
		padding-inline-start: 1rem;
		color: var(--marginal-note);
		line-height: 1.5;
	}

	.story-register {
		color: var(--marginal-note);
		font-size: 0.875rem;
	}

	.story-register code {
		color: var(--reading-ink);
		font-family: var(--font-record);
	}

	.story-constrained {
		max-width: 24rem;
	}

	.story-grid input {
		box-sizing: border-box;
		width: 100%;
		min-height: 2.75rem;
		margin-block-start: 0.25rem;
		border: 1px solid var(--border-trace);
		border-radius: var(--radius-control);
		background: var(--archive-layer);
		padding-inline: 0.75rem;
		color: var(--reading-ink);
	}

	.story-constrained dl {
		display: grid;
		grid-template-columns: auto minmax(0, 1fr);
		gap: 0.5rem 1rem;
	}

	.story-constrained dt {
		color: var(--marginal-note);
		font-weight: 600;
	}

	.story-constrained dd {
		min-width: 0;
		margin: 0;
		overflow-wrap: anywhere;
	}

	.story-destination {
		margin-block-start: 3rem;
		border-block-start: 1px solid var(--border-trace);
		padding-block-start: 1rem;
		color: var(--marginal-note);
		font-size: 0.875rem;
	}
</style>
