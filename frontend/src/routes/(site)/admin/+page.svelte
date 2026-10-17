<script lang="ts">
	import {
		Breadcrumb,
		BreadcrumbList,
		BreadcrumbItem,
		BreadcrumbLink,
		BreadcrumbSeparator,
		BreadcrumbPage
	} from "$lib/components/ui/breadcrumb";
</script>

<svelte:head><title>Administration · Archive</title><meta name="robots" content="noindex, nofollow" /></svelte:head>
<Breadcrumb
	><BreadcrumbList
		><BreadcrumbItem><BreadcrumbLink href="/">Archive</BreadcrumbLink></BreadcrumbItem><BreadcrumbSeparator /><BreadcrumbItem
			><BreadcrumbPage>Administration</BreadcrumbPage></BreadcrumbItem
		></BreadcrumbList
	></Breadcrumb
>
<header class="admin-page-heading"><h1>Administration</h1></header>
<div class="admin-empty">
	<h2>No sections available</h2>
	<p>Your account can open the portal, but has no access to its sections.</p>
</div>

<style>
	.admin-page-heading {
		margin-block: 1.5rem 2rem;
	}
	.admin-page-heading h1 {
		margin: 0;
		font-size: clamp(2.25rem, 4.5vw, 3.5rem);
		font-weight: 500;
		line-height: 1.12;
		letter-spacing: -0.025em;
		overflow-wrap: anywhere;
	}
	.admin-empty {
		padding-block: 3rem;
		color: var(--marginal-note);
	}
	.admin-empty h2 {
		margin: 0;
		color: var(--reading-ink);
		font-weight: 500;
	}
	.admin-empty p {
		margin-top: 0.75rem;
	}
</style>
