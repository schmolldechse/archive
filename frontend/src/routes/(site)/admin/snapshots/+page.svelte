<script lang="ts">
	import { page } from "$app/state";
	import AdminSectionHeading from "$lib/components/admin/AdminSectionHeading.svelte";
	import SubmissionFilters from "$lib/components/submissions/SubmissionFilters.svelte";
	import SnapshotInventory from "$lib/components/submissions/SnapshotInventory.svelte";
	import AdminPagination from "$lib/components/admin/AdminPagination.svelte";
	import { pageHref } from "$lib/admin/navigation";
	import type { PageProps } from "./$types";
	let { data }: PageProps = $props();
	const total = $derived(Number(data.snapshots.total));
</script>

<svelte:head><title>Snapshots · Administration · Archive</title><meta name="robots" content="noindex, nofollow" /></svelte:head>
<AdminSectionHeading title="Snapshots" description="Browse snapshot records across capture and moderation states." />
{#key page.url.search}
	<section class="snapshot-register" aria-labelledby="snapshot-register-title">
		<SubmissionFilters filters={data.filters} review={false} filterQuality resetHref="/admin/snapshots" />
		<div class="register-caption">
			<h2 id="snapshot-register-title">All snapshot records</h2>
			<p role="status">{total.toLocaleString()} {total === 1 ? "snapshot" : "snapshots"}</p>
		</div>
		{#if data.snapshots.items.length}
			<SnapshotInventory items={data.snapshots.items} />
		{:else}
			<p class="empty-register">
				{data.filters.q || data.filters.status || data.filters.quality
					? "No snapshots match these filters."
					: "No snapshots have been submitted yet."}
			</p>
		{/if}
		<div class="register-pagination">
			<AdminPagination
				{total}
				pageSize={Number(data.snapshots.pageSize)}
				currentPage={Number(data.snapshots.page)}
				href={(value) => pageHref(page.url, "page", value)}
				label="Snapshot pages"
			/>
		</div>
	</section>
{/key}

<style>
	.register-caption {
		display: flex;
		flex-wrap: wrap;
		justify-content: space-between;
		gap: 0.5rem;
		align-items: baseline;
		padding-top: 2rem;
	}
	h2 {
		margin: 0;
		font-size: 1.5rem;
		font-weight: 500;
	}
	.register-caption p,
	.empty-register {
		color: var(--marginal-note);
	}
	.empty-register {
		padding-block: 2.5rem;
		border-top: 1px solid var(--border-trace);
	}
	.register-pagination {
		padding-block: 1.5rem;
	}
</style>
