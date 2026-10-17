<script lang="ts">
	import Button from "$lib/components/ui/Button.svelte";
</script>

<svelte:head><title>Administration · Archive</title><meta name="robots" content="noindex, nofollow" /></svelte:head>
<section class="admin-empty" aria-labelledby="admin-empty-title">
	<h1 id="admin-empty-title">No sections available</h1>
	<p>Return to the archive to continue browsing.</p>
	<Button href="/">Back to archive</Button>
</section>

<style>
	.admin-empty {
		padding-block: 4rem;
	}
	h1 {
		margin: 0;
		font-size: clamp(2rem, 4vw, 3rem);
		font-weight: 500;
		line-height: 1.15;
	}
	p {
		margin-block: 1rem 1.5rem;
		color: var(--marginal-note);
	}
</style>
