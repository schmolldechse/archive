<script lang="ts">
	import type { SnapshotSubmissionDetailResponse } from "$api";
	import Breadcrumb from "$lib/components/ui/breadcrumb/Breadcrumb.svelte";
	import BreadcrumbItem from "$lib/components/ui/breadcrumb/BreadcrumbItem.svelte";
	import BreadcrumbLink from "$lib/components/ui/breadcrumb/BreadcrumbLink.svelte";
	import BreadcrumbList from "$lib/components/ui/breadcrumb/BreadcrumbList.svelte";
	import BreadcrumbPage from "$lib/components/ui/breadcrumb/BreadcrumbPage.svelte";
	import BreadcrumbSeparator from "$lib/components/ui/breadcrumb/BreadcrumbSeparator.svelte";

	let {
		detail,
		admin,
		currentPage
	}: {
		detail: SnapshotSubmissionDetailResponse;
		admin: boolean;
		currentPage?: string;
	} = $props();
	const collectionHref = $derived(admin ? "/admin/snapshots" : "/account/submissions");
	const collectionLabel = $derived(admin ? "All snapshots" : "My submissions");
	const detailHref = $derived(`${collectionHref}/${detail.snapshot.id}`);
</script>

<Breadcrumb aria-label="Snapshot navigation" class="snapshot-breadcrumb" data-snapshot-breadcrumb>
	<BreadcrumbList>
		<BreadcrumbItem><BreadcrumbLink href={collectionHref}>{collectionLabel}</BreadcrumbLink></BreadcrumbItem>
		<BreadcrumbSeparator />
		{#if currentPage}
			<BreadcrumbItem>
				<BreadcrumbLink href={detailHref} data-snapshot-title>{detail.snapshot.title}</BreadcrumbLink>
			</BreadcrumbItem>
			<BreadcrumbSeparator />
			<BreadcrumbItem><BreadcrumbPage>{currentPage}</BreadcrumbPage></BreadcrumbItem>
		{:else}
			<BreadcrumbItem><BreadcrumbPage data-snapshot-title>{detail.snapshot.title}</BreadcrumbPage></BreadcrumbItem>
		{/if}
	</BreadcrumbList>
</Breadcrumb>

<style>
	:global(.snapshot-breadcrumb) {
		margin-block: 1.25rem 1.5rem;
	}
	:global(.snapshot-breadcrumb [data-snapshot-title]) {
		overflow-wrap: anywhere;
	}
</style>
