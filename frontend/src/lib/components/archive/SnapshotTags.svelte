<script lang="ts">
	interface SnapshotTagsProps {
		tag: string;
	}

	let { tag }: SnapshotTagsProps = $props();
</script>

<!-- Keep a single tag label consistent across archive and review views. -->
<li class="snapshot-tag" data-component="snapshot-tag"><span aria-hidden="true">#</span>{tag}</li>

<style>
	.snapshot-tag {
		overflow-wrap: anywhere;
		color: var(--marginal-note);
	}

	.snapshot-tag span {
		color: var(--register-mark);
	}
</style>
