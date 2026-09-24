<script module lang="ts">
	import { defineMeta } from "@storybook/addon-svelte-csf";

	import ProgressMeta from "$lib/components/ui/Progress.svelte";

	const { Story } = defineMeta({
		title: "Components/Progress",
		component: ProgressMeta,
		parameters: {
			docs: {
				description: {
					component:
						"A labelled progress indicator. One internal tween drives both the percentage and the fill. Set value to a new target and choose animationDuration in milliseconds; reduced motion completes immediately."
				}
			}
		}
	});
</script>

<script lang="ts">
	import Button from "$lib/components/ui/Button.svelte";
	import Progress from "$lib/components/ui/Progress.svelte";

	const storyId = $props.id();
	let durationSeconds = $state(3);
	let activeDuration = $state(3000);
	let value = $state(0);
	let run = $state(0);
	const validDuration = $derived(Number.isFinite(durationSeconds) && durationSeconds >= 0);

	function startProgress(): void {
		if (!validDuration) return;

		activeDuration = durationSeconds * 1000;
		value = 0;
		const currentRun = ++run;
		requestAnimationFrame(() => {
			requestAnimationFrame(() => {
				if (run === currentRun) value = 100;
			});
		});
	}
</script>

<Story name="Animated Progress" asChild>
	<div class="story-stack">
		{#key run}
			<Progress label="Current task" {value} animationDuration={activeDuration} />
		{/key}
		<div class="story-controls">
			<div class="story-field">
				<label for={`${storyId}-duration`}>Duration in seconds</label>
				<input
					id={`${storyId}-duration`}
					type="number"
					min="0"
					step="0.5"
					value={durationSeconds}
					oninput={(event) => (durationSeconds = event.currentTarget.valueAsNumber)}
				/>
			</div>
			<Button variant="secondary" onclick={startProgress} disabled={!validDuration}>
				{run === 0 ? "Start progress" : "Restart progress"}
			</Button>
		</div>
	</div>
</Story>

<Story name="States" asChild>
	<div class="story-stack">
		<Progress label="Not started" value={0} />
		<Progress label="Processing items" value={42} />
		<Progress label="Processing complete" value={100} />
		<Progress label="Unknown progress" value={null} />
	</div>
</Story>

<Story name="Range and Responsive Layout" asChild>
	<div class="story-stack">
		<Progress label="Processed items" min={0} max={10} value={3} aria-valuetext="3 of 10 items" />
		<div class="story-narrow">
			<Progress label="A longer progress label that wraps without being shortened" value={64} />
		</div>
	</div>
</Story>

<style>
	.story-stack {
		display: grid;
		inline-size: min(100%, 36rem);
		gap: 2rem;
	}

	.story-controls {
		display: flex;
		align-items: end;
		flex-wrap: wrap;
		gap: 0.75rem 1rem;
	}

	.story-field {
		display: grid;
		gap: 0.25rem;
		font-family: var(--font-interface);
		font-size: 0.875rem;
		font-weight: 600;
		line-height: 1.43;
	}

	.story-field input {
		box-sizing: border-box;
		inline-size: 8rem;
		min-block-size: 2.75rem;
		border: 1px solid var(--border-trace);
		border-radius: var(--radius-control);
		padding-inline: 0.75rem;
		background: var(--archive-layer);
		color: var(--reading-ink);
		font: inherit;
		font-variant-numeric: tabular-nums;
	}

	.story-field input:focus-visible {
		outline: 3px solid var(--register-mark);
		outline-offset: 2px;
	}

	.story-narrow {
		inline-size: min(100%, 18rem);
	}
</style>
