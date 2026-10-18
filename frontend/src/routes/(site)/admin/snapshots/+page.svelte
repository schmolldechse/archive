<script lang="ts">
	import { page } from "$app/state";
	import AdminSectionHeading from "$lib/components/admin/AdminSectionHeading.svelte";
	import SubmissionRegister from "$lib/components/submissions/SubmissionRegister.svelte";
	import type { PageProps } from "./$types";
	let { data }: PageProps = $props();
</script>

<svelte:head
	><title>Snapshot moderation · Administration · Archive</title><meta name="robots" content="noindex, nofollow" /></svelte:head
>
<AdminSectionHeading title="Snapshots" description="Review captured pages before they enter the public archive." />
{#key page.url.search}<SubmissionRegister
		submissions={data.submissions}
		filters={data.filters}
		review
		showHeading={false}
	/>{/key}
