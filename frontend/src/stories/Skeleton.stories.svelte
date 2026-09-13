<script module lang="ts">
	import { defineMeta } from "@storybook/addon-svelte-csf";

	import SkeletonMeta from "$lib/components/ui/Skeleton.svelte";

	const { Story } = defineMeta({
		title: "Components/Skeleton",
		component: SkeletonMeta,
		parameters: {
			docs: {
				description: {
					component:
						"A static, non-interactive placeholder that preserves layout while content loads. Busy-state semantics and any textual announcement belong to the containing region."
				}
			}
		}
	});
</script>

<script lang="ts">
	import Skeleton from "$lib/components/ui/Skeleton.svelte";

	let skeletonReference = $state<HTMLSpanElement | null>(null);
</script>

<Story name="Variants" asChild>
	<div class="story-variant-grid">
		<section>
			<p class="story-label">Text</p>
			<Skeleton variant="text" width="14rem" />
		</section>
		<section>
			<p class="story-label">Block</p>
			<Skeleton width="14rem" />
		</section>
		<section>
			<p class="story-label">Circle</p>
			<Skeleton variant="circle" width={48} />
		</section>
	</div>
</Story>

<Story name="Sizing" asChild>
	<div class="story-stack">
		<section>
			<p class="story-label">Class-based dimensions</p>
			<Skeleton class="story-class-sized" />
		</section>
		<section>
			<p class="story-label">String dimensions</p>
			<Skeleton width="min(100%, 18rem)" height="3.5rem" />
		</section>
		<section>
			<p class="story-label">Numeric dimensions</p>
			<Skeleton width={144} height={56} />
		</section>
	</div>
</Story>

<Story name="Bindable Reference" asChild>
	<div class="story-stack">
		<section>
			<p class="story-label">Measurement hook</p>
			<Skeleton bind:ref={skeletonReference} width="12rem" height="3rem" />
			<p class="story-note story-reference-note">{skeletonReference ? "Reference connected" : "Reference unavailable"}</p>
		</section>
	</div>
</Story>

<Story name="Composed Layout" asChild>
	<article class="story-composition" aria-label="Placeholder composition">
		<Skeleton class="story-thumbnail" />
		<div class="story-content">
			<Skeleton variant="text" width="65%" height={24} />
			<div class="story-lines">
				<Skeleton variant="text" width="100%" />
				<Skeleton variant="text" width="92%" />
				<Skeleton variant="text" width="76%" />
			</div>
		</div>
	</article>
</Story>

<Story name="Busy Region" asChild>
	<section class="story-busy-region" aria-busy="true" aria-describedby="skeleton-loading-status">
		<p id="skeleton-loading-status" class="story-status">Loading content</p>
		<div class="story-lines">
			<Skeleton variant="text" width="72%" />
			<Skeleton variant="text" width="100%" />
			<Skeleton variant="text" width="84%" />
		</div>
		<p class="story-note">The containing region exposes the busy state; each placeholder remains hidden.</p>
	</section>
</Story>

<Story name="Responsive Layout" asChild>
	<div class="story-responsive-examples">
		<section>
			<p class="story-label">Narrow container</p>
			<div class="story-viewport story-viewport--narrow">
				<div class="story-responsive-composition">
					<Skeleton class="story-responsive-thumbnail" />
					<div class="story-lines">
						<Skeleton variant="text" width="68%" height={20} />
						<Skeleton variant="text" />
						<Skeleton variant="text" width="82%" />
					</div>
				</div>
			</div>
		</section>
		<section>
			<p class="story-label">Wide container</p>
			<div class="story-viewport story-viewport--wide">
				<div class="story-responsive-composition">
					<Skeleton class="story-responsive-thumbnail" />
					<div class="story-lines">
						<Skeleton variant="text" width="68%" height={20} />
						<Skeleton variant="text" />
						<Skeleton variant="text" width="82%" />
					</div>
				</div>
			</div>
		</section>
	</div>
</Story>

<style>
	.story-variant-grid,
	.story-responsive-examples {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(min(100%, 14rem), 1fr));
		gap: 2rem;
		max-width: 56rem;
	}

	.story-variant-grid section,
	.story-stack section,
	.story-responsive-examples section {
		min-width: 0;
	}

	.story-stack,
	.story-content,
	.story-lines,
	.story-busy-region {
		display: grid;
	}

	.story-stack {
		gap: 2rem;
		max-width: 48rem;
	}

	.story-content {
		min-width: 0;
		align-content: center;
		gap: 1.5rem;
	}

	.story-lines {
		gap: 0.625rem;
	}

	:global(.story-class-sized) {
		width: min(100%, 16rem);
		height: 5rem;
	}

	.story-composition {
		display: grid;
		grid-template-columns: minmax(8rem, 12rem) minmax(0, 1fr);
		gap: 1.5rem;
		max-width: 48rem;
		margin: 0;
		border-block: 1px solid var(--border-trace);
		padding-block: 1.5rem;
	}

	:global(.story-thumbnail) {
		height: 8rem;
	}

	.story-busy-region {
		gap: 1rem;
		max-width: 36rem;
		border-block-start: 1px solid var(--border-trace);
		padding-block-start: 1rem;
	}

	.story-label,
	.story-status,
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

	.story-status,
	.story-note {
		margin: 0;
		line-height: 1.5;
	}

	.story-status {
		color: var(--reading-ink);
		font-weight: 600;
	}

	.story-note {
		color: var(--marginal-note);
		font-size: 0.875rem;
	}

	.story-reference-note {
		margin-block-start: 0.75rem;
	}

	.story-viewport {
		container-type: inline-size;
		max-width: 100%;
		border: 1px solid var(--border-trace);
		border-radius: var(--radius-control);
		padding: 1rem;
	}

	.story-viewport--narrow {
		width: 18rem;
	}

	.story-viewport--wide {
		width: 36rem;
	}

	.story-responsive-composition {
		display: grid;
		gap: 1rem;
	}

	:global(.story-responsive-thumbnail) {
		height: 7rem;
	}

	@container (min-width: 28rem) {
		.story-responsive-composition {
			grid-template-columns: 9rem minmax(0, 1fr);
			align-items: center;
		}

		:global(.story-responsive-thumbnail) {
			height: 6rem;
		}
	}

	@media (max-width: 40rem) {
		.story-composition {
			grid-template-columns: 1fr;
		}
	}
</style>
