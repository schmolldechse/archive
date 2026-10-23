<script lang="ts">
	import SnapshotEditPage from "$lib/components/submissions/SnapshotEditPage.svelte";
	import type { PageProps } from "./$types";

	let { data }: PageProps = $props();
</script>

{#key data.etag}<SnapshotEditPage {data} />{/key}
