<script lang="ts">
	import UserAccessEditor from "$lib/components/admin/UserAccessEditor.svelte";
	import {
		Breadcrumb,
		BreadcrumbList,
		BreadcrumbItem,
		BreadcrumbLink,
		BreadcrumbSeparator,
		BreadcrumbPage
	} from "$lib/components/ui/breadcrumb";
	import type { PageProps } from "./$types";
	let { data }: PageProps = $props();
</script>

<svelte:head><title>@{data.access.handle} · Archive</title><meta name="robots" content="noindex, nofollow" /></svelte:head>
<div class="admin-detail-path">
	<Breadcrumb
		><BreadcrumbList
			><BreadcrumbItem><BreadcrumbLink href={data.usersHref}>Users</BreadcrumbLink></BreadcrumbItem><BreadcrumbSeparator
			/><BreadcrumbItem><BreadcrumbPage>@{data.access.handle}</BreadcrumbPage></BreadcrumbItem></BreadcrumbList
		></Breadcrumb
	>
</div>
{#key data.access.handle}<UserAccessEditor {data} />{/key}

<style>
	.admin-detail-path {
		padding-top: 1.25rem;
	}
</style>
