<script lang="ts">
	import { onDestroy, untrack } from "svelte";

	import { createToastRootAttachment } from "./toast-attachment.svelte";
	import { getToastProviderContext, getToastRecordContext, setToastItemContext } from "./toast-context.svelte";
	import type { ToastRootProps } from "./types";

	const generatedId = $props.id();
	let {
		children,
		ref = $bindable(null),
		class: className,
		id = generatedId,
		tabindex = -1,
		"aria-labelledby": ariaLabelledby,
		"aria-describedby": ariaDescribedby,
		onkeydown,
		...restProps
	}: ToastRootProps = $props();

	const provider = getToastProviderContext("ToastRoot");
	const toastRecord = getToastRecordContext("ToastRoot");
	const resolvedId = $derived(id ?? generatedId);
	const record = $derived(toastRecord.record);
	let titleIdGetters = $state<Array<() => string>>([]);
	let descriptionIdGetters = $state<Array<() => string>>([]);

	const registerId = (
		getters: () => Array<() => string>,
		getId: () => string,
		assign: (value: Array<() => string>) => void
	) => {
		assign([...getters(), getId]);
		return () => assign(getters().filter((entry) => entry !== getId));
	};

	const toastId = untrack(() => record.id);
	const unregisterRoot = provider.registerRoot(toastId, () => ref);
	const rootAttachment = createToastRootAttachment({
		finalizeDismiss: () => provider.finalizeDismiss(record.id),
		getDuration: () => provider.effectiveDuration(record.id),
		getPauseWhenPageHidden: () => provider.pauseWhenPageHidden,
		getRevision: () => record.revision,
		getState: () => provider.getState(record.id),
		onTimeout: () => provider.dismiss(record.id),
		setPaused: (paused) => provider.setPaused(record.id, paused)
	});

	setToastItemContext({
		get defaultDescriptionId() {
			return `${resolvedId}-description`;
		},
		get defaultTitleId() {
			return `${resolvedId}-title`;
		},
		get descriptionId() {
			return descriptionIdGetters.at(-1)?.();
		},
		get duration() {
			return provider.effectiveDuration(record.id);
		},
		get paused() {
			return provider.isPaused(record.id);
		},
		get record() {
			return record;
		},
		get rootId() {
			return resolvedId;
		},
		get state() {
			return provider.getState(record.id);
		},
		dismiss: () => provider.dismiss(record.id),
		registerDescriptionId(getId) {
			return registerId(
				() => descriptionIdGetters,
				getId,
				(value) => (descriptionIdGetters = value)
			);
		},
		registerTitleId(getId) {
			return registerId(
				() => titleIdGetters,
				getId,
				(value) => (titleIdGetters = value)
			);
		}
	});

	onDestroy(unregisterRoot);

	const handleKeydown: NonNullable<ToastRootProps["onkeydown"]> = (event) => {
		onkeydown?.(event);
		if (event.defaultPrevented || event.key !== "Escape" || !record.dismissible) return;

		event.preventDefault();
		event.stopPropagation();
		if (provider.dismiss(record.id)) queueMicrotask(() => provider.restoreFocus(record.id));
	};
</script>

<!-- svelte-ignore a11y_no_noninteractive_tabindex (F8 focuses the message without changing its list-item semantics.) -->
<li
	{...restProps}
	bind:this={ref}
	id={resolvedId}
	class={["toast-root", className]}
	{tabindex}
	aria-labelledby={ariaLabelledby ?? titleIdGetters.at(-1)?.()}
	aria-describedby={ariaDescribedby ?? descriptionIdGetters.at(-1)?.()}
	data-component="toast"
	data-toast-root
	data-id={record.id}
	data-variant={record.variant}
	data-priority={record.priority}
	data-state={provider.getState(record.id)}
	data-dismissible={record.dismissible ? "" : undefined}
	data-paused={provider.isPaused(record.id) ? "" : undefined}
	data-timed={provider.effectiveDuration(record.id) !== null ? "" : undefined}
	onkeydown={handleKeydown}
	{@attach rootAttachment}
>
	{@render children()}
</li>

<style>
	.toast-root {
		box-sizing: border-box;
		width: 100%;
		border: 1px solid var(--border-trace);
		border-inline-start-width: 3px;
		border-radius: var(--radius-control);
		background: var(--archive-layer);
		box-shadow: var(--overlay-shadow);
		color: var(--reading-ink);
		font-family: var(--font-interface);
		opacity: 1;
		transform: translateY(0);
		transition:
			opacity 150ms ease,
			transform 150ms ease;
	}

	.toast-root[data-variant="neutral"] {
		border-inline-start-color: var(--reading-ink);
	}

	.toast-root[data-variant="info"] {
		border-inline-start-color: var(--register-mark);
	}

	.toast-root[data-variant="success"] {
		border-inline-start-color: var(--preservation-green);
	}

	.toast-root[data-variant="warning"] {
		border-inline-start-color: var(--warning-ochre);
	}

	.toast-root[data-variant="error"] {
		border-inline-start-color: var(--time-marker);
	}

	.toast-root[data-state="exiting"] {
		opacity: 0;
		transform: translateY(0.25rem);
		pointer-events: none;
	}

	@starting-style {
		.toast-root[data-state="visible"] {
			opacity: 0;
			transform: translateY(0.25rem);
		}
	}

	@media (prefers-reduced-motion: reduce) {
		.toast-root {
			transition: opacity 0.01ms linear;
		}

		.toast-root[data-state="exiting"] {
			transform: none;
		}

		@starting-style {
			.toast-root[data-state="visible"] {
				transform: none;
			}
		}
	}
</style>
