<script lang="ts">
	import SubmissionDetail from "$lib/components/submissions/SubmissionDetail.svelte";
	import type { PageProps } from "./$types";
	let { data }: PageProps = $props();
</script>

<svelte:head
	><title>{data.detail.snapshot.title} · Moderation · Archive</title><meta
		name="robots"
		content="noindex, nofollow"
	/></svelte:head
>
{#key data.detail.snapshot.id}<SubmissionDetail {data} />{/key}
