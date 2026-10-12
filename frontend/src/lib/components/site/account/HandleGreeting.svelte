<script lang="ts">
	let { handle }: { handle: string } = $props();
	const greeting = $derived(handle ? `@${handle}!` : "there.");
</script>

<h1 aria-label="Set up your profile">
	<span class="hello" aria-hidden="true">Hello,</span>
	<span class="handle" class:empty={!handle} aria-hidden="true">
		{#each Array.from(greeting) as character, index (index)}
			{#key character}
				<span class="letter-window">
					<span class="letter" style:--delay="{Math.min(index, 8) * 10}ms">{character}</span>
				</span>
			{/key}
		{/each}
	</span>
</h1>

<style>
	h1 {
		margin: 0;
		font: 500 4.5rem / 1.06 var(--font-editorial);
		letter-spacing: -0.025em;
	}
	.hello,
	.handle {
		display: block;
	}
	.handle {
		min-height: 1.25em;
		margin-top: 0.125em;
		font-size: 0.625em;
		letter-spacing: -0.02em;
		overflow-wrap: anywhere;
	}
	.letter-window {
		display: inline-block;
		overflow: clip;
		padding-block: 0.12em;
		margin-block: -0.12em;
		vertical-align: bottom;
		perspective: 5em;
	}
	.letter {
		display: inline-block;
		transform-origin: 50% 100%;
		animation: letter-roll 280ms cubic-bezier(0.16, 1, 0.3, 1) var(--delay) backwards;
	}
	@keyframes letter-roll {
		from {
			opacity: 0;
			transform: translateY(90%) rotateX(-80deg);
		}
		to {
			opacity: 1;
			transform: translateY(0) rotateX(0);
		}
	}
	.empty {
		color: var(--marginal-note);
	}
	@media (max-width: 48rem) {
		h1 {
			font-size: 3.5rem;
		}
	}
	@media (prefers-reduced-motion: reduce) {
		.letter {
			animation: none;
		}
	}
</style>
