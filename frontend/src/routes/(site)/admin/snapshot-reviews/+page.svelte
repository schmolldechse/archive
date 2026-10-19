<script lang="ts">
	import AdminSectionHeading from "$lib/components/admin/AdminSectionHeading.svelte";
	import SubmissionRegister from "$lib/components/submissions/SubmissionRegister.svelte";
	import type { PageProps } from "./$types";
	let { data }: PageProps = $props();
</script>

<svelte:head
	><title>Snapshot reviews · Administration · Archive</title><meta name="robots" content="noindex, nofollow" /></svelte:head
>
<AdminSectionHeading title="Snapshot reviews" description="Approve or reject captured snapshots awaiting a decision." />
<SubmissionRegister submissions={data.submissions} filters={data.filters} review showHeading={false} />
