<script lang="ts">
	import SnapshotTags from "./SnapshotTags.svelte";

	interface SnapshotTagListProps {
		tags: string[];
	}

	let { tags }: SnapshotTagListProps = $props();
</script>

{#if tags.length}
	<!-- Keep archive tag spacing and list semantics consistent across snapshot views. -->
	<ul class="snapshot-tag-list" aria-label="Snapshot tags">
		{#each tags as tag (tag)}<SnapshotTags {tag} />{/each}
	</ul>
{/if}

<style>
	.snapshot-tag-list {
		display: flex;
		flex-wrap: wrap;
		gap: 0.5rem 0.75rem;
		margin: 0.75rem 0 0;
		padding: 0;
		list-style: none;
		font-size: 0.75rem;
	}
</style>
