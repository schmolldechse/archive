<script module lang="ts">
	import { defineMeta } from "@storybook/addon-svelte-csf";

	import SnapshotTagsMeta from "$lib/components/archive/SnapshotTags.svelte";

	const { Story } = defineMeta({
		title: "Archive/SnapshotTags",
		component: SnapshotTagsMeta,
		parameters: {
			docs: {
				description: {
					component: "A quiet, wrapping list of archive tags. The hash mark is a visual cue; each tag remains readable as text."
				}
			}
		}
	});
</script>

<script lang="ts">
	import SnapshotTags from "$lib/components/archive/SnapshotTags.svelte";
</script>

<Story name="Tags">
	<SnapshotTags tags={["history", "reference", "long-tag-name-that-wraps-without-truncation"]} />
</Story>
