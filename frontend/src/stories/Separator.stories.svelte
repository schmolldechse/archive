<script module lang="ts">
	import { defineMeta } from "@storybook/addon-svelte-csf";

	import SeparatorMeta from "$lib/components/ui/Separator.svelte";

	const { Story } = defineMeta({
		title: "Components/Separator",
		component: SeparatorMeta,
		parameters: {
			docs: {
				description: {
					component:
						"A quiet horizontal or vertical rule for separating related regions. It can be purely decorative or expose structural separator semantics without adding interaction or spacing."
				}
			}
		}
	});
</script>

<script lang="ts">
	import Separator from "$lib/components/ui/Separator.svelte";

	let semanticSeparator = $state<HTMLDivElement | null>(null);
</script>

<Story name="Horizontal" asChild>
	<div class="story-content-groups">
		<section>
			<p class="story-label">Primary group</p>
			<p class="story-copy">Related content remains visually connected within its group.</p>
		</section>
		<Separator />
		<section>
			<p class="story-label">Supporting group</p>
			<p class="story-copy">The surrounding layout controls the space on either side of the rule.</p>
		</section>
	</div>
</Story>

<Story name="Vertical" asChild>
	<div class="story-vertical-row">
		<section>
			<p class="story-label">Previous</p>
			<p class="story-copy">First control group</p>
		</section>
		<Separator orientation="vertical" />
		<section>
			<p class="story-label">Next</p>
			<p class="story-copy">Second control group</p>
		</section>
	</div>
</Story>

<Story name="Semantic" asChild>
	<div class="story-stack">
		<section>
			<p class="story-label">Section A</p>
			<p class="story-copy">Content before the structural boundary.</p>
		</section>
		<Separator bind:ref={semanticSeparator} decorative={false} aria-label="Related sections" />
		<section>
			<p class="story-label">Section B</p>
			<p class="story-copy">Content after the structural boundary.</p>
		</section>
		<p class="story-note">{semanticSeparator ? "Reference connected" : "Reference unavailable"}</p>
	</div>
</Story>

<Story name="Decorative" asChild>
	<div class="story-stack">
		<p class="story-copy">Content before the default decorative rule.</p>
		<Separator />
		<p class="story-copy">The rule is hidden from assistive technology.</p>
	</div>
</Story>

<Story name="Constrained Layout" asChild>
	<div class="story-stack">
		<section class="story-width-narrow">
			<p class="story-label">Narrow container</p>
			<Separator />
		</section>
		<section class="story-width-wide">
			<p class="story-label">Wide container</p>
			<Separator />
		</section>
	</div>
</Story>

<style>
	.story-content-groups,
	.story-stack {
		display: grid;
		gap: 1.5rem;
		max-width: 48rem;
	}

	.story-content-groups section,
	.story-vertical-row section {
		min-width: 0;
	}

	.story-vertical-row {
		display: flex;
		height: 6rem;
		max-width: 48rem;
		align-items: stretch;
		gap: 1.5rem;
	}

	.story-vertical-row section {
		display: flex;
		flex: 1;
		flex-direction: column;
		justify-content: center;
	}

	.story-label,
	.story-copy,
	.story-note {
		font-family: var(--font-interface);
	}

	.story-label {
		margin: 0 0 0.5rem;
		color: var(--marginal-note);
		font-size: 0.75rem;
		font-weight: 600;
		letter-spacing: 0.08em;
		text-transform: uppercase;
	}

	.story-copy,
	.story-note {
		margin: 0;
		line-height: 1.5;
	}

	.story-copy {
		color: var(--reading-ink);
	}

	.story-note {
		color: var(--marginal-note);
		font-size: 0.875rem;
	}

	.story-width-narrow,
	.story-width-wide {
		display: grid;
		gap: 0.75rem;
	}

	.story-width-narrow {
		width: min(100%, 12rem);
	}

	.story-width-wide {
		width: 100%;
	}
</style>
