<script lang="ts">
	import Check from "@lucide/svelte/icons/check";
	import Copy from "@lucide/svelte/icons/copy";
	import { onDestroy } from "svelte";
	import Button from "$lib/components/ui/Button.svelte";

	let { value, label = "Copy source URL" }: { value: string; label?: string } = $props();
	let copied = $state(false);
	let copyFailed = $state(false);
	let resetTimer: ReturnType<typeof setTimeout> | undefined;
	let previousValue: string | undefined;

	onDestroy(() => clearTimeout(resetTimer));
	$effect(() => {
		if (previousValue === undefined) {
			previousValue = value;
			return;
		}
		if (value === previousValue) return;
		previousValue = value;
		clearTimeout(resetTimer);
		copied = false;
		copyFailed = false;
	});

	async function copySource(): Promise<void> {
		clearTimeout(resetTimer);
		const target = value;
		try {
			await navigator.clipboard.writeText(target);
			if (value !== target) return;
			copied = true;
			copyFailed = false;
		} catch {
			if (value !== target) return;
			copied = false;
			copyFailed = true;
		}
		resetTimer = setTimeout(() => {
			copied = false;
			copyFailed = false;
		}, 1800);
	}
</script>

<Button
	variant="text"
	class="copy-source-button"
	title={copied ? "Source URL copied" : copyFailed ? "Copy failed; try again" : label}
	aria-label={copied ? "Source URL copied" : copyFailed ? "Copy failed; try again" : label}
	onclick={copySource}
>
	{#if copied}<Check size={18} aria-hidden="true" />{:else}<Copy size={18} aria-hidden="true" />{/if}
</Button>
<span class="sr-only" role="status" aria-live="polite">{copied ? "Source URL copied" : copyFailed ? "Source URL could not be copied" : ""}</span>

<style>
	:global(.copy-source-button) {
		width: 2.75rem;
		height: 2.75rem;
		flex: none;
		padding: 0;
		text-decoration: none;
	}
	:global(.copy-source-button:hover) {
		text-decoration: none;
	}
	.sr-only {
		position: absolute;
		width: 1px;
		height: 1px;
		padding: 0;
		margin: -1px;
		overflow: hidden;
		clip: rect(0, 0, 0, 0);
		white-space: nowrap;
		border: 0;
	}
</style>
