<script lang="ts">
	import { page } from "$app/state";
	import type { LayoutProps } from "./$types";
	let { data, children }: LayoutProps = $props();
</script>

<main id="main-content" class="admin-shell">
	<div class="admin-masthead">
		<a href="/admin" class="admin-wordmark">Administration</a>
		{#if data.admin.users}
			<nav aria-label="Administration sections">
				<a href="/admin/users" aria-current={page.url.pathname.startsWith("/admin/users") ? "page" : undefined}>Users</a>
			</nav>
		{/if}
	</div>
	{@render children()}
</main>

<style>
	.admin-shell {
		width: min(100%, 86rem);
		min-height: 65vh;
		margin-inline: auto;
		padding: 0 clamp(1rem, 4vw, 4rem) 4rem;
	}
	.admin-masthead {
		display: flex;
		align-items: center;
		gap: 3rem;
		border-bottom: 1px solid var(--border-trace);
		margin-bottom: 1.5rem;
	}
	.admin-wordmark {
		padding-block: 1rem;
		color: var(--reading-ink);
		font-weight: 600;
		text-decoration: none;
	}
	.admin-masthead nav {
		display: flex;
		align-self: stretch;
	}
	.admin-masthead nav a {
		display: flex;
		align-items: center;
		padding: 1rem;
		color: var(--marginal-note);
		border-bottom: 3px solid transparent;
		margin-bottom: -1px;
		text-decoration: none;
	}
	.admin-masthead nav a[aria-current] {
		color: var(--register-mark);
		border-bottom-color: var(--register-mark);
		font-weight: 600;
	}
	.admin-shell :global([data-component="breadcrumb"] a) {
		display: inline-flex;
		align-items: center;
		min-height: 2.75rem;
	}
</style>
