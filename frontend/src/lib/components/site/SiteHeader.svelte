<script lang="ts">
	import Menu from "@lucide/svelte/icons/menu";
	import X from "@lucide/svelte/icons/x";
	import { page } from "$app/state";

	import ThemeToggle from "./ThemeToggle.svelte";
	import BrandMark from "./BrandMark.svelte";

	let menuOpen = $state(false);

	function closeMenu(): void {
		menuOpen = false;
	}
</script>

<header class="site-header" data-component="site-header">
	<div class="site-header__inner">
		<BrandMark />

		<nav
			id="primary-navigation"
			class="primary-navigation"
			class:primary-navigation--open={menuOpen}
			aria-label="Primary navigation"
		>
			<a href="/#browse" aria-current={page.url.pathname === "/" ? "page" : undefined} onclick={closeMenu}>Browse</a>
			<a href="/#about" onclick={closeMenu}>About</a>
			<a
				class="primary-navigation__archive"
				href="/archive/new"
				aria-current={page.url.pathname === "/archive/new" ? "page" : undefined}
				onclick={closeMenu}>Archive a page</a
			>
		</nav>

		<div class="site-header__actions">
			<button
				class="menu-toggle"
				type="button"
				aria-controls="primary-navigation"
				aria-expanded={menuOpen}
				onclick={() => (menuOpen = !menuOpen)}
			>
				{#if menuOpen}
					<X aria-hidden="true" />
				{:else}
					<Menu aria-hidden="true" />
				{/if}
				<span>Menu</span>
			</button>
			<ThemeToggle />
		</div>
	</div>
</header>

<style>
	.site-header {
		position: relative;
		z-index: 20;
		border-bottom: 1px solid var(--border-trace);
		background: var(--reading-room);
	}

	.site-header__inner {
		display: grid;
		width: min(100%, 86rem);
		min-height: 5.75rem;
		grid-template-columns: minmax(12rem, 1fr) auto auto;
		align-items: stretch;
		margin-inline: auto;
		padding-inline: clamp(1rem, 4vw, 4rem);
	}

	.site-header :global(.brand-mark) {
		padding-inline-end: 1.5rem;
	}

	.primary-navigation {
		display: flex;
		align-items: stretch;
	}

	.primary-navigation a {
		position: relative;
		display: inline-flex;
		min-height: 2.75rem;
		align-items: center;
		border: 0;
		background: transparent;
		padding-inline: 1.125rem;
		color: var(--reading-ink);
		font-size: 0.9375rem;
		font-weight: 600;
		text-decoration: none;
		cursor: pointer;
	}

	.primary-navigation a:hover {
		background: var(--archive-layer);
	}

	.primary-navigation a[aria-current="page"]::after {
		position: absolute;
		inset: auto 1.125rem -1px;
		height: 3px;
		background: var(--register-mark);
		content: "";
	}

	.primary-navigation .primary-navigation__archive {
		color: var(--register-mark);
	}

	.site-header__actions {
		display: flex;
		align-items: center;
		gap: 0.25rem;
		padding-inline-start: 0.75rem;
	}

	.menu-toggle {
		display: none;
		min-width: 2.75rem;
		min-height: 2.75rem;
		align-items: center;
		justify-content: center;
		gap: 0.5rem;
		border: 1px solid transparent;
		border-radius: var(--radius-control);
		background: transparent;
		padding-inline: 0.75rem;
		color: var(--reading-ink);
		font-size: 0.875rem;
		font-weight: 600;
		cursor: pointer;
	}

	.menu-toggle:hover {
		border-color: var(--border-trace);
		background: var(--archive-layer);
	}

	.menu-toggle :global(svg) {
		width: 1.3rem;
		height: 1.3rem;
		stroke-width: 1.75;
	}

	@media (max-width: 48rem) {
		.site-header__inner {
			min-height: 4.75rem;
			grid-template-columns: 1fr auto;
			align-items: center;
		}

		.primary-navigation {
			position: absolute;
			top: calc(100% + 1px);
			right: clamp(1rem, 4vw, 4rem);
			left: clamp(1rem, 4vw, 4rem);
			display: none;
			border: 1px solid var(--border-trace);
			background: var(--reading-room);
			box-shadow: var(--shadow-overlay);
		}

		.primary-navigation--open {
			display: grid;
		}

		.primary-navigation a {
			justify-content: flex-start;
			border-bottom: 1px solid var(--border-trace);
			padding: 0.875rem 1.125rem;
			text-align: left;
		}

		.primary-navigation > :last-child {
			border-bottom: 0;
		}

		.primary-navigation a[aria-current="page"]::after {
			inset: 0.5rem auto 0.5rem -1px;
			width: 3px;
			height: auto;
		}

		.site-header__actions {
			padding-inline-start: 0;
		}

		.menu-toggle {
			display: inline-flex;
		}
	}

	@media (max-width: 34rem) {
		.menu-toggle span {
			display: none;
		}
	}
</style>
