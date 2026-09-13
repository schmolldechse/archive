<script module lang="ts">
	import { defineMeta } from "@storybook/addon-svelte-csf";

	import ButtonMeta from "$lib/components/ui/Button.svelte";

	const { Story } = defineMeta({
		title: "Components/Button",
		component: ButtonMeta,
		parameters: {
			docs: {
				description: {
					component:
						"A reusable action primitive that preserves native button and link semantics while sharing one visual language."
				}
			}
		}
	});
</script>

<script lang="ts">
	import ArrowLeft from "@lucide/svelte/icons/arrow-left";
	import ArrowRight from "@lucide/svelte/icons/arrow-right";

	import Button from "$lib/components/ui/Button.svelte";

	let activationCount = $state(0);
	let boundAction = $state<HTMLButtonElement | HTMLAnchorElement | null>(null);
	let lastActivation = $state("None");

	const recordActivation = (element: "Button" | "Anchor") => {
		activationCount += 1;
		lastActivation = element;
	};
</script>

<Story name="Variants" asChild>
	<div class="story-row">
		<Button variant="primary">Primary action</Button>
		<Button variant="secondary">Secondary action</Button>
		<Button variant="text">Text action</Button>
	</div>
</Story>

<Story name="Element Types" asChild>
	<div class="story-stack">
		<section>
			<p class="story-label">Native button</p>
			<Button>Action</Button>
		</section>
		<section>
			<p class="story-label">Native anchor</p>
			<Button href="#button-story-destination">Open destination</Button>
		</section>
	</div>
</Story>

<Story name="Sizes" asChild>
	<div class="story-row">
		<Button size="default" variant="primary">Default</Button>
		<Button size="large" variant="primary">Large</Button>
	</div>
</Story>

<Story name="States" asChild>
	<div class="story-stack">
		<section>
			<p class="story-label">Idle</p>
			<div class="story-row">
				<Button variant="primary">Action</Button>
				<Button href="#button-story-destination">Open destination</Button>
			</div>
		</section>
		<section>
			<p class="story-label">Disabled</p>
			<div class="story-row">
				<Button variant="primary" disabled>Disabled action</Button>
				<Button href="#button-story-destination" disabled>Disabled destination</Button>
			</div>
		</section>
		<section>
			<p class="story-label">Loading</p>
			<div class="story-row">
				<Button variant="primary" loading loadingLabel="Loading action">Action</Button>
				<Button href="#button-story-destination" loading loadingLabel="Loading destination">Open destination</Button>
			</div>
		</section>
	</div>
</Story>

<Story name="Content Composition" asChild>
	<div class="story-stack">
		<section>
			<p class="story-label">Text only</p>
			<Button>Continue</Button>
		</section>
		<section>
			<p class="story-label">Leading icon</p>
			<Button><ArrowLeft aria-hidden="true" /> Previous</Button>
		</section>
		<section>
			<p class="story-label">Trailing icon</p>
			<Button variant="primary">Continue <ArrowRight aria-hidden="true" /></Button>
		</section>
		<section class="story-constrained">
			<p class="story-label">Long wrapping label</p>
			<Button style="width: 100%;">Long label that wraps without losing its complete accessible text</Button>
		</section>
	</div>
</Story>

<Story name="Interaction Semantics" asChild>
	<div class="story-stack">
		<p class="story-note">
			Use Enter or Space to activate the native button. The native link activates with Enter and retains its own navigation
			semantics.
		</p>
		<div class="story-row">
			<Button bind:ref={boundAction} variant="primary" onclick={() => recordActivation("Button")}>Action</Button>
			<Button href="#button-story-destination" onclick={() => recordActivation("Anchor")}>Open destination</Button>
			<Button variant="text" onclick={() => boundAction?.focus()}>Focus action</Button>
		</div>
		<div class="story-register" aria-live="polite">
			<span>Activations</span>
			<code>{activationCount}</code>
			<span>Last element</span>
			<code>{lastActivation}</code>
			<span>{boundAction ? "Reference connected" : "Reference unavailable"}</span>
		</div>
	</div>
</Story>

<Story name="Native Attributes" asChild>
	<div class="story-stack">
		<section>
			<p class="story-label">Explicit submit button</p>
			<form onsubmit={(event) => event.preventDefault()}>
				<Button type="submit" name="intent" value="continue" variant="primary">Submit</Button>
			</form>
		</section>
		<section>
			<p class="story-label">Download link</p>
			<Button href="data:text/plain;charset=utf-8,Example%20download" download="example.txt">Download</Button>
		</section>
		<section>
			<p class="story-label">External-target link</p>
			<Button href="https://example.com" target="_blank" rel="noreferrer">Open destination</Button>
		</section>
	</div>
</Story>

<div id="button-story-destination" class="story-destination">Destination</div>

<style>
	.story-row {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: 0.75rem;
	}

	.story-stack {
		display: grid;
		gap: 2rem;
		max-width: 48rem;
	}

	.story-label,
	.story-note,
	.story-register,
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
		display: flex;
		flex-wrap: wrap;
		align-items: baseline;
		gap: 0.5rem 1rem;
		color: var(--marginal-note);
		font-size: 0.875rem;
	}

	.story-register code {
		color: var(--reading-ink);
		font-family: var(--font-record);
	}

	.story-constrained {
		width: 12rem;
	}

	.story-destination {
		margin-block-start: 3rem;
		border-block-start: 1px solid var(--border-trace);
		padding-block-start: 1rem;
		color: var(--marginal-note);
		font-size: 0.875rem;
	}
</style>
