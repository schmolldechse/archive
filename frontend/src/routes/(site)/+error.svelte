<script lang="ts">
	import { page } from "$app/state";
	import Button from "$lib/components/ui/Button.svelte";
	import {
		Breadcrumb,
		BreadcrumbList,
		BreadcrumbItem,
		BreadcrumbLink,
		BreadcrumbSeparator,
		BreadcrumbPage
	} from "$lib/components/ui/breadcrumb";

	const title = $derived(
		page.status === 403 ? "Access unavailable" : page.status === 404 ? "Page unavailable" : "Something went wrong"
	);
</script>

<svelte:head><title>Page unavailable · Archive</title><meta name="robots" content="noindex, nofollow" /></svelte:head>
<main id="main-content" class="site-error">
	<Breadcrumb
		><BreadcrumbList
			><BreadcrumbItem><BreadcrumbLink href="/">Archive</BreadcrumbLink></BreadcrumbItem><BreadcrumbSeparator /><BreadcrumbItem
				><BreadcrumbPage>{title}</BreadcrumbPage></BreadcrumbItem
			></BreadcrumbList
		></Breadcrumb
	>
	<p class="record-value">{page.status}</p>
	<h1>{title}</h1>
	<p>{page.error?.message}</p>
	<Button href="/">Back to archive</Button>
</main>

<style>
	.site-error {
		width: min(100%, 86rem);
		min-height: 60vh;
		margin-inline: auto;
		padding: 2rem clamp(1rem, 4vw, 4rem) 4rem;
	}
	.record-value {
		margin-top: 2rem;
		color: var(--marginal-note);
	}
	h1 {
		margin-block: 0.5rem 1rem;
		font-size: clamp(2.25rem, 5vw, 3rem);
		font-weight: 500;
		line-height: 1.15;
	}
	main > p:last-of-type {
		margin-bottom: 1.5rem;
	}
</style>
