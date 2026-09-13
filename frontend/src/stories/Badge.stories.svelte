<script module lang="ts">
	import { defineMeta } from "@storybook/addon-svelte-csf";

	import BadgeMeta from "$lib/components/ui/Badge.svelte";

	const { Story } = defineMeta({
		title: "Components/Badge",
		component: BadgeMeta,
		parameters: {
			docs: {
				description: {
					component:
						"A non-interactive label for compact categorical or status metadata. Visible text always carries the meaning; status variants pair it with a distinct decorative icon."
				}
			}
		}
	});
</script>

<script lang="ts">
	import Tag from "@lucide/svelte/icons/tag";

	import Badge from "$lib/components/ui/Badge.svelte";
</script>

<Story name="Variants" asChild>
	<div class="story-row">
		<Badge>Neutral</Badge>
		<Badge variant="info">Information</Badge>
		<Badge variant="success">Complete</Badge>
		<Badge variant="warning">Warning</Badge>
		<Badge variant="critical">Critical</Badge>
	</div>
</Story>

<Story name="Sizes" asChild>
	<div class="story-stack">
		<section>
			<p class="story-label">Compact</p>
			<div class="story-row">
				<Badge size="compact">Label</Badge>
				<Badge size="compact" variant="success">Complete</Badge>
			</div>
		</section>
		<section>
			<p class="story-label">Default</p>
			<div class="story-row">
				<Badge>Label</Badge>
				<Badge variant="success">Complete</Badge>
			</div>
		</section>
	</div>
</Story>

<Story name="Icon Override" asChild>
	<div class="story-row">
		<Badge variant="info">
			{#snippet icon()}<Tag />{/snippet}
			Category label
		</Badge>
		<Badge>
			{#snippet icon()}<Tag />{/snippet}
			Neutral with icon
		</Badge>
	</div>
</Story>

<Story name="Content Resilience" asChild>
	<div class="story-stack">
		<section>
			<p class="story-label">Content types</p>
			<div class="story-row">
				<Badge>8</Badge>
				<Badge variant="info">Long label that remains fully visible</Badge>
				<Badge variant="success">12,480</Badge>
			</div>
		</section>
		<section class="story-constrained">
			<p class="story-label">Constrained width</p>
			<Badge variant="warning">Long label wraps instead of being truncated</Badge>
		</section>
	</div>
</Story>

<Story name="Static and Live Context Guidance" asChild>
	<div class="story-grid">
		<section>
			<p class="story-label">Static by default</p>
			<Badge variant="success">Complete</Badge>
			<p class="story-note">No status role or live-region behavior is added by the component.</p>
		</section>
		<section>
			<p class="story-label">Explicit live-region container</p>
			<Badge variant="info" role="status" aria-live="polite">Updated value</Badge>
			<p class="story-note">Forward live-region attributes only when this element genuinely contains announced updates.</p>
		</section>
	</div>
</Story>

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

	.story-grid {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(min(100%, 16rem), 1fr));
		gap: 2rem;
		max-width: 48rem;
	}

	.story-grid section {
		border-block-start: 1px solid var(--border-trace);
		padding-block-start: 1rem;
	}

	.story-label,
	.story-note {
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
		max-width: 32rem;
		margin: 0.75rem 0 0;
		color: var(--marginal-note);
		font-size: 0.875rem;
		line-height: 1.5;
	}

	.story-constrained {
		width: 10rem;
	}
</style>
