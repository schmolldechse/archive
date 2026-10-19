<script lang="ts">
	import SubmissionDetail from "$lib/components/submissions/SubmissionDetail.svelte";
	import type { PageProps } from "./$types";
	let { data }: PageProps = $props();
</script>

<svelte:head
	><title>{data.detail.snapshot.title} · My submissions · Archive</title><meta
		name="robots"
		content="noindex, nofollow"
	/></svelte:head
>
<main id="main-content" class="own-submission">
	{#key data.detail.snapshot.id}<SubmissionDetail {data} />{/key}
</main>

<style>
	.own-submission {
		width: min(100%, 78rem);
		margin-inline: auto;
		padding: 0 clamp(1rem, 4vw, 3rem) 4rem;
		min-height: 65vh;
	}
</style>
