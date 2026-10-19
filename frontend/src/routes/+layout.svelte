<script lang="ts">
	import "../app.css";
	import { page } from "$app/state";
	import type { LayoutProps } from "./$types";

	let { children }: LayoutProps = $props();
</script>

<!-- Root errors outside the site layout also need a document title. -->
<svelte:head>
	{#if page.error}<title>Page unavailable · Archive</title>{/if}
</svelte:head>

{@render children()}
