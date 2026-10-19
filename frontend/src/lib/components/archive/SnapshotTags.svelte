<script lang="ts">
	interface SnapshotTagsProps {
		tags: string[];
	}

	let { tags }: SnapshotTagsProps = $props();
</script>

<!-- Keep tag labels consistent anywhere an archived snapshot is summarized. -->
{#if tags.length}
	<ul class="snapshot-tags" aria-label="Snapshot tags" data-component="snapshot-tags">
		{#each tags as tag (tag)}
			<li><span aria-hidden="true">#</span>{tag}</li>
		{/each}
	</ul>
{/if}

<style>
	.snapshot-tags {
		display: flex;
		flex-wrap: wrap;
		gap: 0.5rem 0.75rem;
		margin: 0.75rem 0 0;
		padding: 0;
		list-style: none;
		color: var(--marginal-note);
		font-size: 0.75rem;
	}

	.snapshot-tags li {
		overflow-wrap: anywhere;
	}

	.snapshot-tags li span {
		color: var(--register-mark);
	}
</style>
