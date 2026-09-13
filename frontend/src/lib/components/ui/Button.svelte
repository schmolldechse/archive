<script lang="ts">
	import type { Snippet } from "svelte";
	import type { HTMLAnchorAttributes, HTMLButtonAttributes } from "svelte/elements";

	type ButtonVariant = "primary" | "secondary" | "text";
	type ButtonSize = "default" | "large";
	type ButtonRef = HTMLButtonElement | HTMLAnchorElement | null;

	interface SharedButtonProps {
		variant?: ButtonVariant;
		size?: ButtonSize;
		disabled?: boolean;
		loading?: boolean;
		loadingLabel?: string;
		children: Snippet;
		ref?: ButtonRef;
		class?: HTMLButtonAttributes["class"];
	}

	type NativeButtonProps = Omit<HTMLButtonAttributes, "children" | "class" | "disabled" | "href" | "ref">;
	type NativeAnchorProps = Omit<HTMLAnchorAttributes, "children" | "class" | "disabled" | "href" | "ref" | "type">;

	type ButtonElementProps = SharedButtonProps &
		NativeButtonProps & {
			href?: undefined;
		};

	type AnchorElementProps = SharedButtonProps &
		NativeAnchorProps & {
			href: string;
			type?: never;
		};

	type ButtonProps = ButtonElementProps | AnchorElementProps;

	let {
		href,
		variant = "secondary",
		size = "default",
		disabled = false,
		loading = false,
		loadingLabel = "Loading",
		children,
		ref = $bindable(null),
		class: className,
		type = "button",
		onclick,
		role,
		tabindex,
		"aria-busy": ariaBusy,
		"aria-disabled": ariaDisabled,
		...restProps
	}: ButtonProps = $props();

	const isInactive = $derived(disabled || loading);
	const state = $derived(loading ? "loading" : disabled ? "disabled" : "idle");

	const handleButtonClick: NonNullable<HTMLButtonAttributes["onclick"]> = (event) => {
		if (isInactive) {
			event.preventDefault();
			return;
		}

		(onclick as HTMLButtonAttributes["onclick"])?.(event);
	};

	const handleAnchorClick: NonNullable<HTMLAnchorAttributes["onclick"]> = (event) => {
		if (isInactive) {
			event.preventDefault();
			event.stopImmediatePropagation();
			return;
		}

		(onclick as HTMLAnchorAttributes["onclick"])?.(event);
	};
</script>

{#if href !== undefined}
	<a
		{...restProps as NativeAnchorProps}
		bind:this={ref}
		href={isInactive ? undefined : href}
		class={["button", className]}
		role={isInactive ? "link" : role}
		tabindex={isInactive ? -1 : tabindex}
		aria-busy={loading ? "true" : ariaBusy}
		aria-disabled={isInactive ? "true" : ariaDisabled}
		data-component="button"
		data-element="anchor"
		data-variant={variant}
		data-size={size}
		data-state={state}
		data-disabled={isInactive ? "" : undefined}
		onclick={handleAnchorClick}
	>
		{#if loading}
			<span class="button__loading-label">{loadingLabel}</span>
		{:else}
			{@render children()}
		{/if}
	</a>
{:else}
	<button
		{...restProps as NativeButtonProps}
		bind:this={ref}
		type={type ?? "button"}
		disabled={isInactive}
		class={["button", className]}
		{role}
		{tabindex}
		aria-busy={loading ? "true" : ariaBusy}
		aria-disabled={ariaDisabled}
		data-component="button"
		data-element="button"
		data-variant={variant}
		data-size={size}
		data-state={state}
		data-disabled={isInactive ? "" : undefined}
		onclick={handleButtonClick}
	>
		{#if loading}
			<span class="button__loading-label">{loadingLabel}</span>
		{:else}
			{@render children()}
		{/if}
	</button>
{/if}

<style>
	.button {
		display: inline-flex;
		min-width: 2.75rem;
		max-width: 100%;
		align-items: center;
		justify-content: center;
		gap: 0.5rem;
		box-sizing: border-box;
		border: 1px solid transparent;
		border-radius: var(--radius-control);
		padding-inline: 1rem;
		font-family: var(--font-interface);
		font-size: 0.9375rem;
		font-weight: 600;
		line-height: 1.25rem;
		text-align: center;
		text-decoration: none;
		overflow-wrap: anywhere;
		cursor: pointer;
		transition:
			background-color 150ms ease,
			border-color 150ms ease,
			color 150ms ease,
			opacity 150ms ease,
			text-decoration-thickness 150ms ease;
	}

	button.button {
		appearance: none;
	}

	.button[data-size="default"] {
		min-height: 2.75rem;
		padding-block: 0.6875rem;
	}

	.button[data-size="large"] {
		min-height: 3.5rem;
		padding: 0.9375rem 1.25rem;
		font-size: 1rem;
		line-height: 1.5rem;
	}

	.button[data-variant="primary"] {
		background: var(--register-mark);
		color: var(--reading-room);
	}

	.button[data-variant="primary"]:hover:not([data-disabled]) {
		background: color-mix(in srgb, var(--register-mark) 88%, var(--reading-ink));
	}

	.button[data-variant="secondary"] {
		border-color: var(--border-trace);
		background: transparent;
		color: var(--reading-ink);
	}

	.button[data-variant="secondary"]:hover:not([data-disabled]) {
		border-color: var(--reading-ink);
		background: var(--archive-layer);
	}

	.button[data-variant="text"] {
		background: transparent;
		color: var(--register-mark);
		text-decoration-line: underline;
		text-decoration-thickness: 0.08em;
		text-underline-offset: 0.16em;
	}

	.button[data-variant="text"]:hover:not([data-disabled]) {
		background: color-mix(in srgb, var(--register-mark) 8%, transparent);
		text-decoration-thickness: 0.12em;
	}

	.button[data-disabled] {
		cursor: not-allowed;
		opacity: 0.58;
	}

	.button :global(svg) {
		width: 1.25rem;
		height: 1.25rem;
		flex: none;
		stroke-width: 1.75;
	}

	.button__loading-label {
		min-width: 0;
	}

	@media (prefers-reduced-motion: reduce) {
		.button {
			transition: none;
		}
	}
</style>
