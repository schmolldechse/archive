<script lang="ts">
	import { page } from "$app/state";
	import SubmissionRegister from "$lib/components/submissions/SubmissionRegister.svelte";
	import type { PageProps } from "./$types";
	let { data }: PageProps = $props();
</script>

<svelte:head><title>My submissions · Archive</title><meta name="robots" content="noindex, nofollow" /></svelte:head>
<main id="main-content" class="own-submissions">
	{#key page.url.search}<SubmissionRegister submissions={data.submissions} filters={data.filters} review={false} />{/key}
</main>

<style>
	.own-submissions {
		width: min(100%, 78rem);
		margin-inline: auto;
		padding: 0 clamp(1rem, 4vw, 3rem) 4rem;
		min-height: 65vh;
	}
</style>
