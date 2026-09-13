<script module lang="ts">
	import { defineMeta } from "@storybook/addon-svelte-csf";

	import BrandMarkMeta from "$lib/components/site/BrandMark.svelte";

	const { Story } = defineMeta({
		title: "Site/Brand Mark",
		component: BrandMarkMeta,
		parameters: {
			docs: {
				description: {
					component:
						"The responsive archive identity. It uses the horizontal lockup by default and switches to the symbol on narrow viewports."
				}
			}
		}
	});
</script>

<Story name="Responsive" asChild>
	<div class="story-surface">
		<BrandMarkMeta href="#responsive" />
	</div>
</Story>

<Story name="Variants" asChild>
	<div class="story-surface story-stack">
		<BrandMarkMeta href="#primary" variant="primary" aria-label="Primary brand mark" />
		<BrandMarkMeta href="#symbol" variant="symbol" aria-label="Brand symbol" />
		<BrandMarkMeta href="#wordmark" variant="wordmark" aria-label="Brand wordmark" />
	</div>
</Story>

<Story name="Light and Dark" asChild>
	<div class="theme-comparison">
		<div class="story-surface" data-theme="light">
			<span>Light</span>
			<BrandMarkMeta href="#light" variant="primary" aria-label="Primary brand mark on light background" />
		</div>
		<div class="story-surface story-surface--dark" data-theme="dark">
			<span>Dark</span>
			<BrandMarkMeta href="#dark" variant="primary" aria-label="Primary brand mark on dark background" />
		</div>
	</div>
</Story>

<style>
	.story-surface {
		display: flex;
		min-height: 8rem;
		align-items: center;
		gap: 2rem;
		background: var(--reading-room);
		padding: 2rem;
		color: var(--reading-ink);
	}

	.story-stack {
		flex-direction: column;
		align-items: flex-start;
	}

	.theme-comparison {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(18rem, 1fr));
	}

	.theme-comparison .story-surface {
		flex-direction: column;
		align-items: flex-start;
	}

	.story-surface > span {
		color: var(--marginal-note);
		font-size: 0.75rem;
		font-weight: 600;
		letter-spacing: 0.08em;
		text-transform: uppercase;
	}

	.story-surface--dark {
		--reading-room: #0b1113;
		--reading-ink: #ece9df;
		--time-marker: #ff9a7a;
		--marginal-note: #adada6;
	}
</style>
