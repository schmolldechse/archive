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

<svelte:head
	><title>{data.access.displayName} · Users · Archive</title><meta name="robots" content="noindex, nofollow" /></svelte:head
>
<Breadcrumb
	><BreadcrumbList
		><BreadcrumbItem><BreadcrumbLink href="/">Archive</BreadcrumbLink></BreadcrumbItem><BreadcrumbSeparator /><BreadcrumbItem
			><BreadcrumbLink href="/admin">Administration</BreadcrumbLink></BreadcrumbItem
		><BreadcrumbSeparator /><BreadcrumbItem><BreadcrumbLink href={data.usersHref}>Users</BreadcrumbLink></BreadcrumbItem
		><BreadcrumbSeparator /><BreadcrumbItem><BreadcrumbPage>@{data.access.handle}</BreadcrumbPage></BreadcrumbItem
		></BreadcrumbList
	></Breadcrumb
>
{#key data.access.handle}<UserAccessEditor {data} />{/key}
