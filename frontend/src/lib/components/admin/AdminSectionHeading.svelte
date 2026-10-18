<script lang="ts">
	let { title, description }: { title: string; description: string } = $props();
</script>

<header class="section-heading">
	<h1>{title}</h1>
	<p>{description}</p>
</header>

<style>
	.section-heading {
		margin-block: 2.5rem 2rem;
	}
	h1 {
		margin: 0;
		font-size: clamp(2.5rem, 4.5vw, 3.5rem);
		font-weight: 500;
		line-height: 1.12;
		letter-spacing: -0.025em;
		overflow-wrap: anywhere;
	}
	p {
		max-width: 42rem;
		margin: 0.5rem 0 0;
		color: var(--marginal-note);
	}
	@media (max-width: 42rem) {
		.section-heading {
			margin-block: 2rem 1.5rem;
		}
	}
</style>
