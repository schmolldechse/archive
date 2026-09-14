<script lang="ts">
	import type { ToastPriority } from "./types";

	interface ToastAnnouncerProps {
		priority: ToastPriority;
		message: string;
		sequence: number;
	}

	let { priority, message, sequence }: ToastAnnouncerProps = $props();
</script>

<div
	role={priority === "high" ? "alert" : "status"}
	aria-atomic="true"
	class="toast-announcer"
	data-toast-announcer
	data-priority={priority}
>
	{#key sequence}<span>{message}</span>{/key}
</div>

<style>
	.toast-announcer {
		position: absolute;
		width: 1px;
		height: 1px;
		margin: -1px;
		overflow: hidden;
		clip: rect(0, 0, 0, 0);
		clip-path: inset(50%);
		white-space: nowrap;
	}
</style>
