<script lang="ts">
	import { page } from "$app/state";
	import UserSearch from "$lib/components/admin/UserSearch.svelte";
	import UserTable from "$lib/components/admin/UserTable.svelte";
	import type { PageProps } from "./$types";
	let { data }: PageProps = $props();
</script>

<svelte:head><title>Users · Administration · Archive</title><meta name="robots" content="noindex, nofollow" /></svelte:head>
<header class="users-heading">
	<div>
		<h1>Users</h1>
		<p>Manage roles and individual permissions.</p>
	</div>
	<!-- A quiet register motif connects the directory to the archive's visual language. -->
	<svg class="directory-mark" viewBox="0 0 176 104" fill="none" aria-hidden="true" focusable="false">
		<g stroke="currentColor" stroke-width="1">
			<path d="M24 8v88M16 16h144M16 48h144M16 80h144" />
			<circle cx="44" cy="32" r="4" /><circle cx="44" cy="64" r="4" /><circle cx="44" cy="96" r="4" />
			<path d="M60 28h72M60 36h48M60 60h56M60 68h80M60 92h72M60 100h40" />
		</g>
		<path class="directory-mark-accent" d="M16 48h16" stroke-width="3" />
	</svg>
</header>

{#key page.url.search}<UserSearch filters={data.filters} />{/key}
<UserTable
	users={data.users}
	filtered={Boolean(data.filters.q || data.filters.role)}
	canManage={data.admin.roles || data.admin.permissions}
/>

<style>
	.users-heading {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 2rem;
		margin-block: 2.5rem 2rem;
	}
	h1 {
		margin: 0;
		font-size: clamp(2.5rem, 4.5vw, 3.5rem);
		font-weight: 500;
		line-height: 1.12;
		letter-spacing: -0.025em;
		overflow-wrap: anywhere;
	}
	.users-heading p {
		margin: 0.5rem 0 0;
		color: var(--marginal-note);
	}
	.directory-mark {
		width: 10rem;
		height: auto;
		flex: none;
		color: var(--border-trace);
	}
	.directory-mark-accent {
		stroke: var(--register-mark);
	}
	@media (max-width: 42rem) {
		.users-heading {
			margin-block: 2rem 1.5rem;
		}
		.directory-mark {
			display: none;
		}
	}
</style>
