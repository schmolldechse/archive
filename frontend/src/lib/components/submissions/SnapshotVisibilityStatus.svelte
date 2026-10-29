<script lang="ts">
	import Globe2 from "@lucide/svelte/icons/globe-2";
	import LockKeyhole from "@lucide/svelte/icons/lock-keyhole";
	import Badge from "$lib/components/ui/Badge.svelte";

	let { isPrivate, size = "compact" }: { isPrivate: boolean; size?: "compact" | "default" } = $props();
</script>

<Badge variant="neutral" {size} data-visibility={isPrivate ? "private" : "public"}>
	{#snippet icon()}
		{#if isPrivate}<LockKeyhole />{:else}<Globe2 />{/if}
	{/snippet}
	{isPrivate ? "Private" : "Public"}
</Badge>
