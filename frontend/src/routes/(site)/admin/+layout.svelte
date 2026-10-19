<script lang="ts">
	import { page } from "$app/state";
	import ArrowLeft from "@lucide/svelte/icons/arrow-left";
	import AdminSectionNavigation from "$lib/components/admin/AdminSectionNavigation.svelte";
	import type { LayoutProps } from "./$types";
	let { data, children }: LayoutProps = $props();
</script>

<main id="main-content" class="admin-shell">
	<header class="admin-context">
		<p>Administration</p>
		<a href="/"><ArrowLeft aria-hidden="true" />Back to archive</a>
	</header>
	<AdminSectionNavigation sections={data.sections} pathname={page.url.pathname} />
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
