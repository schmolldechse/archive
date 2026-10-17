<script lang="ts">
	import Button from "$lib/components/ui/Button.svelte";
</script>

<svelte:head><title>Page unavailable · Archive</title><meta name="robots" content="noindex, nofollow" /></svelte:head>
<!-- Use the same neutral fallback for every error; HTTP response statuses remain unchanged. -->
<section class="admin-error" aria-labelledby="admin-error-title">
	<h1 id="admin-error-title">Page unavailable</h1>
	<p>Please try again later.</p>
	<Button href="/">Back to archive</Button>
</section>

<style>
	.admin-error {
		width: min(100% - 2rem, 60rem);
		margin: 4rem auto;
	}
	.admin-error h1 {
		font-size: 2.5rem;
	}
</style>
