<script lang="ts">
	import { page } from "$app/state";
	import ArrowLeft from "@lucide/svelte/icons/arrow-left";
	import type { LayoutProps } from "./$types";
	let { data, children }: LayoutProps = $props();
</script>

<main id="main-content" class="admin-shell">
	<header class="admin-context">
		<p>Administration</p>
		<a href="/"><ArrowLeft aria-hidden="true" />Back to archive</a>
	</header>
	{#if data.sections.length > 1}
		<nav class="admin-sections" aria-label="Administration sections">
			{#each data.sections as section (section.key)}
				<a
					href={section.href}
					aria-current={page.url.pathname === section.href
						? "page"
						: page.url.pathname.startsWith(`${section.href}/`)
							? "location"
							: undefined}>{section.label}</a
				>
			{/each}
		</nav>
	{/if}
	{@render children()}
</main>

<style>
	.admin-shell {
		width: min(100%, 86rem);
		min-height: 65vh;
		margin-inline: auto;
		padding: 1rem clamp(1rem, 4vw, 4rem) 4rem;
	}
	.admin-context {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		justify-content: space-between;
		gap: 1rem;
		border-bottom: 1px solid var(--border-trace);
		padding-block: 0.5rem;
		font-size: 0.875rem;
	}
	.admin-context p {
		margin: 0;
		font-weight: 600;
	}
	.admin-context a {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		min-height: 2.75rem;
		color: var(--marginal-note);
	}
	.admin-context :global(svg) {
		width: 1rem;
		height: 1rem;
		flex: none;
	}
	.admin-sections {
		display: flex;
		flex-wrap: wrap;
		gap: 0 1.5rem;
		border-bottom: 1px solid var(--border-trace);
	}
	.admin-sections a {
		display: flex;
		align-items: center;
		min-height: 3.25rem;
		padding: 0.75rem 0.25rem;
		color: var(--marginal-note);
		border-bottom: 2px solid transparent;
		text-decoration: none;
		font-size: 0.9375rem;
	}
	.admin-sections a:hover {
		color: var(--reading-ink);
		text-decoration: underline;
	}
	.admin-sections a[aria-current] {
		color: var(--register-mark);
		border-bottom-color: var(--register-mark);
		font-weight: 600;
	}
	.admin-shell :global([data-component="breadcrumb"] a) {
		display: inline-flex;
		align-items: center;
		min-height: 2.75rem;
	}
	@media (max-width: 24rem) {
		.admin-context {
			font-size: 0.8125rem;
		}
		.admin-context a {
			gap: 0.375rem;
		}
	}
</style>
