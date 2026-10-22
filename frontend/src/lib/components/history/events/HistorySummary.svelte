<script lang="ts">
	import type { SnapshotHistoryItemResponse } from "$api";

	let { item: _item }: { item: SnapshotHistoryItemResponse } = $props();
</script>

<!-- Unknown events intentionally keep their stable envelope visible without exposing untrusted raw JSON. -->
