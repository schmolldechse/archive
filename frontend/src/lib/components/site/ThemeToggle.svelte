<script lang="ts">
	import Moon from "@lucide/svelte/icons/moon";
	import Sun from "@lucide/svelte/icons/sun";
	import { onMount } from "svelte";
	import type { HTMLButtonAttributes } from "svelte/elements";

	type Theme = "light" | "dark";
	type NativeButtonProps = Omit<HTMLButtonAttributes, "children" | "type">;

	interface ThemeToggleProps extends NativeButtonProps {
		ref?: HTMLButtonElement | null;
	}

	let { ref = $bindable(null), class: className, disabled = false, onclick, ...restProps }: ThemeToggleProps = $props();

	let theme = $state<Theme>("light");

	onMount(() => {
		theme = document.documentElement.dataset.theme === "dark" ? "dark" : "light";
	});

	function setTheme(nextTheme: Theme): void {
		theme = nextTheme;
		document.documentElement.dataset.theme = nextTheme;

		try {
			window.localStorage.setItem("archiv-theme", nextTheme);
		} catch {
			// Theme switching remains available when storage is blocked.
		}
	}

	const handleClick: NonNullable<HTMLButtonAttributes["onclick"]> = (event) => {
		onclick?.(event);
		if (event.defaultPrevented || disabled) return;

		setTheme(theme === "dark" ? "light" : "dark");
	};
</script>

<button
	{...restProps}
	bind:this={ref}
	type="button"
	{disabled}
	class={["theme-toggle", className]}
	aria-label={theme === "dark" ? "Switch to light mode" : "Switch to dark mode"}
	aria-pressed={theme === "dark"}
	data-component="theme-toggle"
	data-theme={theme}
	onclick={handleClick}
>
	{#if theme === "dark"}
		<Moon aria-hidden="true" />
	{:else}
		<Sun aria-hidden="true" />
	{/if}
</button>

<style>
	.theme-toggle {
		display: inline-flex;
		width: 2.75rem;
		height: 2.75rem;
		align-items: center;
		justify-content: center;
		border: 1px solid transparent;
		border-radius: var(--radius-control);
		background: transparent;
		color: var(--reading-ink);
		cursor: pointer;
		transition:
			border-color 150ms ease,
			background-color 150ms ease;
	}

	.theme-toggle:hover:not(:disabled) {
		border-color: var(--border-trace);
		background: var(--archive-layer);
	}

	.theme-toggle:disabled {
		cursor: not-allowed;
		opacity: 0.58;
	}

	.theme-toggle :global(svg) {
		width: 1.3rem;
		height: 1.3rem;
		stroke-width: 1.75;
	}

	@media (prefers-reduced-motion: reduce) {
		.theme-toggle {
			transition: none;
		}
	}
</style>
