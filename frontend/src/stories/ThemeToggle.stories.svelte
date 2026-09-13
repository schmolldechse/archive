<script module lang="ts">
	import { defineMeta } from "@storybook/addon-svelte-csf";

	import ThemeToggleMeta from "$lib/components/site/ThemeToggle.svelte";

	const { Story } = defineMeta({
		title: "Site/Theme toggle",
		component: ThemeToggleMeta,
		parameters: {
			docs: {
				description: {
					component: "A labelled, persistent control for switching the editorial reading-room theme."
				}
			}
		}
	});
</script>

<Story name="Interactive" asChild>
	<div class="story-row">
		<ThemeToggleMeta />
		<p>Activate the control to switch between light and dark reading-room themes.</p>
	</div>
</Story>

<Story name="Disabled" asChild>
	<div class="story-row">
		<ThemeToggleMeta disabled />
		<p>The native disabled state remains visibly distinct.</p>
	</div>
</Story>

<style>
	.story-row {
		display: flex;
		max-width: 34rem;
		align-items: center;
		gap: 1rem;
		background: var(--reading-room);
		padding: 2rem;
		color: var(--reading-ink);
	}

	.story-row p {
		margin: 0;
		color: var(--marginal-note);
		font-family: var(--font-interface);
	}
</style>
