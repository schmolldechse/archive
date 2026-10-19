<script lang="ts">
	import type { SnapshotModerationStatus } from "$api";
	import Badge from "$lib/components/ui/Badge.svelte";
	let { status }: { status: SnapshotModerationStatus } = $props();
	const labels = { PENDING: "Pending review", APPROVED: "Approved", REJECTED: "Rejected", RESUBMITTED: "Resubmitted" };
	const variants = { PENDING: "warning", APPROVED: "success", REJECTED: "critical", RESUBMITTED: "info" } as const;
</script>

<Badge variant={variants[status] ?? "warning"} size="compact" data-status={status}>
	{labels[status] ?? "Review status unavailable"}
</Badge>
