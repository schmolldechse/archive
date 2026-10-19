<script lang="ts">
	import PageError from "$lib/components/errors/PageError.svelte";
	import BrandMark from "$lib/components/site/BrandMark.svelte";
</script>

<!-- Unmatched routes have no site layout or authentication context. -->
<div class="root-error">
	<header class="root-error__header">
		<BrandMark variant="primary" aria-label="Archive home" />
	</header>
	<PageError standalone />
</div>

<style>
	.root-error {
		display: flex;
		min-height: 100svh;
		flex-direction: column;
	}
	.root-error__header {
		width: min(100%, 78rem);
		margin-inline: auto;
		padding: 2rem clamp(1.25rem, 6vw, 4rem) 0;
	}
</style>
