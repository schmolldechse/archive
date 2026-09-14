<script lang="ts">
	import type { Snippet } from "svelte";

	import { setToastRecordContext } from "./toast-context.svelte";
	import type { ToastRecord } from "./types";

	interface ToastItemProps {
		record: ToastRecord;
		children: Snippet;
	}

	let { record, children }: ToastItemProps = $props();

	setToastRecordContext({
		get record() {
			return record;
		}
	});
</script>

{@render children()}
