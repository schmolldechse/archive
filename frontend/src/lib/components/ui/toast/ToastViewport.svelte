<script lang="ts">
	import Bell from "@lucide/svelte/icons/bell";
	import CircleAlert from "@lucide/svelte/icons/circle-alert";
	import CircleCheck from "@lucide/svelte/icons/circle-check";
	import Info from "@lucide/svelte/icons/info";
	import TriangleAlert from "@lucide/svelte/icons/triangle-alert";

	import ToastAction from "./ToastAction.svelte";
	import ToastAnnouncer from "./ToastAnnouncer.svelte";
	import ToastClose from "./ToastClose.svelte";
	import ToastDescription from "./ToastDescription.svelte";
	import ToastItem from "./ToastItem.svelte";
	import ToastRoot from "./ToastRoot.svelte";
	import ToastTitle from "./ToastTitle.svelte";
	import { createToastViewportAttachment } from "./toast-attachment.svelte";
	import { getToastProviderContext } from "./toast-context.svelte";
	import type { ToastPriority, ToastRecord, ToastViewportProps } from "./types";

	let {
		position = "bottom-end",
		hotkey = "F8",
		children: renderToast,
		ref = $bindable(null),
		class: className,
		"aria-label": ariaLabel,
		...restProps
	}: ToastViewportProps = $props();

	const provider = getToastProviderContext("ToastViewport");
	let normalMessage = $state("");
	let highMessage = $state("");
	let normalSequence = $state(0);
	let highSequence = $state(0);

	function announce(priority: ToastPriority, message: string): void {
		if (priority === "high") {
			highMessage = message;
			highSequence += 1;
		} else {
			normalMessage = message;
			normalSequence += 1;
		}
	}

	const viewportAttachment = createToastViewportAttachment({
		announce,
		finalizeUnpresentedDismissals: provider.finalizeUnpresentedDismissals,
		focusNewest() {
			const newest = [...provider.visibleRecords].reverse().find((record) => provider.getState(record.id) === "visible");
			if (!newest) return;

			const root = provider.getRoot(newest.id);
			if (!root) return;
			provider.rememberFocusOrigin(document.activeElement instanceof HTMLElement ? document.activeElement : null);
			try {
				root.focus({ preventScroll: true });
			} catch {
				root.focus();
			}
		},
		getHotkey: () => hotkey,
		getVisibleRecords: () => provider.visibleRecords
	});
</script>

{#snippet defaultToastContent(record: ToastRecord)}
	<div class="toast-default">
		{#if record.variant === "info"}
			<Info class="toast-default__icon" aria-hidden="true" />
		{:else if record.variant === "success"}
			<CircleCheck class="toast-default__icon" aria-hidden="true" />
		{:else if record.variant === "warning"}
			<TriangleAlert class="toast-default__icon" aria-hidden="true" />
		{:else if record.variant === "error"}
			<CircleAlert class="toast-default__icon" aria-hidden="true" />
		{:else}
			<Bell class="toast-default__icon" aria-hidden="true" />
		{/if}

		<div class="toast-default__content">
			<ToastTitle />
			{#if record.description}<ToastDescription />{/if}
			{#if record.action}<ToastAction />{/if}
		</div>

		{#if record.dismissible}<ToastClose />{/if}
	</div>
{/snippet}

<ToastAnnouncer priority="normal" message={normalMessage} sequence={normalSequence} />
<ToastAnnouncer priority="high" message={highMessage} sequence={highSequence} />

<ol
	{...restProps}
	bind:this={ref}
	class={["toast-viewport", className]}
	aria-label={ariaLabel ?? provider.label}
	data-toast-viewport
	data-position={position}
	data-paused={provider.paused ? "" : undefined}
	data-count={provider.visibleRecords.length}
	{@attach viewportAttachment}
>
	{#each provider.visibleRecords as record (record.id)}
		<ToastItem {record}>
			{#snippet children()}
				{#if renderToast}
					{@render renderToast(record)}
				{:else}
					<ToastRoot>
						{#snippet children()}{@render defaultToastContent(record)}{/snippet}
					</ToastRoot>
				{/if}
			{/snippet}
		</ToastItem>
	{/each}
</ol>

<style>
	.toast-viewport {
		position: fixed;
		z-index: 60;
		display: flex;
		width: min(26rem, calc(100vw - 2rem));
		max-height: calc(100dvh - 2rem);
		flex-direction: column;
		gap: 0.75rem;
		box-sizing: border-box;
		margin: 0;
		overflow: visible auto;
		padding: 0;
		list-style: none;
		pointer-events: none;
	}

	.toast-viewport[data-position="top-start"] {
		inset-block-start: max(1rem, env(safe-area-inset-top));
		inset-inline-start: max(1rem, env(safe-area-inset-left));
	}

	.toast-viewport[data-position="top-end"] {
		inset-block-start: max(1rem, env(safe-area-inset-top));
		inset-inline-end: max(1rem, env(safe-area-inset-right));
	}

	.toast-viewport[data-position="bottom-start"] {
		inset-block-end: max(1rem, env(safe-area-inset-bottom));
		inset-inline-start: max(1rem, env(safe-area-inset-left));
	}

	.toast-viewport[data-position="bottom-end"] {
		inset-block-end: max(1rem, env(safe-area-inset-bottom));
		inset-inline-end: max(1rem, env(safe-area-inset-right));
	}

	.toast-viewport :global([data-toast-root]) {
		pointer-events: auto;
	}

	.toast-default {
		display: grid;
		grid-template-columns: auto minmax(0, 1fr) auto;
		align-items: start;
		gap: 0.75rem;
		padding: 0.75rem;
	}

	.toast-default__content {
		display: grid;
		min-width: 0;
		gap: 0.25rem;
	}

	.toast-default__content :global([data-toast-action]) {
		justify-self: start;
		margin-block-start: 0.5rem;
	}

	.toast-default__icon {
		width: 1.25rem;
		height: 1.25rem;
		margin-block-start: 0.0625rem;
		stroke-width: 1.75;
	}

	:global([data-toast-root][data-variant="neutral"]) .toast-default__icon {
		color: var(--reading-ink);
	}

	:global([data-toast-root][data-variant="info"]) .toast-default__icon {
		color: var(--register-mark);
	}

	:global([data-toast-root][data-variant="success"]) .toast-default__icon {
		color: var(--preservation-green);
	}

	:global([data-toast-root][data-variant="warning"]) .toast-default__icon {
		color: var(--warning-ochre);
	}

	:global([data-toast-root][data-variant="error"]) .toast-default__icon {
		color: var(--time-marker);
	}

	@media (max-width: 30rem) {
		.toast-viewport {
			width: auto;
			inset-inline: max(0.75rem, env(safe-area-inset-left)) max(0.75rem, env(safe-area-inset-right));
		}
	}
</style>
