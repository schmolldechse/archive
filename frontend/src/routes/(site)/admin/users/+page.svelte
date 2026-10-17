<script lang="ts">
	import { page } from "$app/state";
	import ArrowRight from "@lucide/svelte/icons/arrow-right";
	import Avatar from "$lib/components/ui/Avatar.svelte";
	import Button from "$lib/components/ui/Button.svelte";
	import AdminPagination from "$lib/components/admin/AdminPagination.svelte";
	import { pageHref } from "$lib/admin/navigation";
	import type { PageProps } from "./$types";
	let { data }: PageProps = $props();
	const total = $derived(Number(data.users.total));
	const current = $derived(Number(data.users.page));
	const size = $derived(Number(data.users.pageSize));
</script>

<svelte:head><title>Users · Administration · Archive</title><meta name="robots" content="noindex, nofollow" /></svelte:head>
<header class="admin-page-heading">
	<h1>Users</h1>
	<p>Manage roles and individual permissions.</p>
</header>
<form method="GET" class="admin-search" role="search" aria-label="Search users">
	<label class="admin-search-field"
		>Search users<input
			name="q"
			value={data.filters.q}
			maxlength="128"
			placeholder="Handle, name or role"
			type="search"
		/></label
	>
	<label
		>Role<select name="role" value={data.filters.role}
			><option value="">All roles</option><option value="USER">User</option><option value="ADMIN">Admin</option></select
		></label
	>
	<div class="admin-search-actions">
		<Button type="submit" variant="primary">Search</Button>
		{#if data.filters.q || data.filters.role}<Button href="/admin/users" variant="text">Clear</Button>{/if}
	</div>
</form>
<div class="admin-register" aria-hidden="true">
	<span>User</span><span>Role</span><span class="admin-access-label">Access</span>
</div>
<ul class="admin-user-list" aria-label="Users">
	{#each data.users.items as user (user.handle)}
		<li>
			<a href={`/admin/users/${encodeURIComponent(user.handle!)}${page.url.search}`}>
				<span class="admin-user-identity">
					<Avatar
						src={user.profileImageUrl}
						initials={user.displayName
							?.split(/\s+/)
							.map((part) => part[0])
							.slice(0, 2)
							.join("")}
						decorative
					/>
					<span class="admin-user-name"
						><strong>{user.displayName}</strong><span class="record-value">@{user.handle}</span></span
					>
				</span>
				<span class="admin-role" data-admin={user.role === "ADMIN"}>{user.role === "ADMIN" ? "Admin" : "User"}</span>
				<span class="admin-user-action"><span>Manage access</span><ArrowRight aria-hidden="true" /></span>
			</a>
		</li>
	{/each}
</ul>
{#if !data.users.items.length}<div class="admin-empty">
		<h2>{total ? "No users on this page" : "No users found"}</h2>
		<p>{total ? "Choose a previous page to continue." : "Try another handle, name or role."}</p>
	</div>{/if}
<div class="admin-pagination">
	<p>
		{total === 0
			? "0 users"
			: `${((current - 1) * size + 1).toLocaleString()}–${Math.min(current * size, total).toLocaleString()} of ${total.toLocaleString()} users`}
	</p>
	<AdminPagination
		{total}
		pageSize={size}
		currentPage={current}
		href={(next) => pageHref(page.url, "page", next)}
		label="User pages"
	/>
</div>

<style>
	.admin-page-heading {
		margin-block: 2.5rem 2rem;
	}
	.admin-page-heading h1 {
		margin: 0;
		font-size: clamp(2.5rem, 4.5vw, 3.5rem);
		font-weight: 500;
		line-height: 1.12;
		letter-spacing: -0.025em;
		overflow-wrap: anywhere;
	}
	.admin-page-heading p {
		margin: 0.5rem 0 0;
		color: var(--marginal-note);
	}
	.admin-search {
		display: grid;
		grid-template-columns: minmax(0, 1fr) 11rem auto;
		align-items: end;
		gap: 1rem;
		padding-block: 1.5rem 2rem;
		border-top: 1px solid var(--border-trace);
	}
	.admin-search-field {
		min-width: 0;
	}
	.admin-search-actions {
		display: flex;
		align-items: center;
		flex-wrap: wrap;
		gap: 0.75rem;
	}
	.admin-register {
		display: grid;
		grid-template-columns: minmax(0, 1fr) 7rem 9rem;
		gap: 1.5rem;
		align-items: center;
		border-block: 1px solid var(--border-trace);
		padding: 0.75rem 1rem;
		color: var(--marginal-note);
		font-size: 0.875rem;
	}
	.admin-access-label {
		justify-self: end;
	}
	.admin-user-list {
		padding: 0;
		margin: 0;
		list-style: none;
	}
	.admin-user-list li {
		border-bottom: 1px solid color-mix(in srgb, var(--border-trace) 45%, transparent);
	}
	.admin-user-list a {
		display: grid;
		grid-template-columns: minmax(0, 1fr) 7rem 9rem;
		align-items: center;
		gap: 1.5rem;
		min-height: 5.5rem;
		padding: 1rem;
		color: var(--reading-ink);
		text-decoration: none;
	}
	.admin-user-list a:hover {
		background: var(--archive-layer);
	}
	.admin-user-list a:hover strong {
		color: var(--register-mark);
		text-decoration: underline;
		text-underline-offset: 0.16em;
	}
	.admin-user-name {
		display: grid;
		gap: 0.125rem;
		min-width: 0;
		overflow-wrap: anywhere;
	}
	.admin-user-name .record-value {
		font-size: 0.8125rem;
		color: var(--marginal-note);
	}
	.admin-user-identity {
		display: flex;
		align-items: center;
		gap: 1rem;
		min-width: 0;
	}
	.admin-user-action {
		display: inline-flex;
		align-items: center;
		justify-self: end;
		gap: 0.75rem;
		color: var(--register-mark);
		font-size: 0.875rem;
		font-weight: 600;
	}
	.admin-user-list :global(svg) {
		width: 1.125rem;
		height: 1.125rem;
		color: var(--marginal-note);
	}
	.admin-role {
		justify-self: start;
		display: inline-flex;
		align-items: center;
		gap: 0.5rem;
		font-size: 0.875rem;
	}
	.admin-role[data-admin="true"]::before {
		content: "";
		width: 0.375rem;
		height: 0.375rem;
		background: var(--register-mark);
	}
	.admin-pagination {
		display: flex;
		align-items: center;
		justify-content: space-between;
		flex-wrap: wrap;
		gap: 1rem;
		padding-top: 1.5rem;
	}
	.admin-pagination p {
		margin: 0;
		color: var(--marginal-note);
		font-size: 0.875rem;
		font-variant-numeric: tabular-nums;
	}
	.admin-empty {
		padding-block: 3rem;
		color: var(--marginal-note);
	}
	.admin-empty h2 {
		margin: 0;
		color: var(--reading-ink);
		font-weight: 500;
	}
	.admin-empty p {
		margin-top: 0.75rem;
	}
	label {
		display: block;
		font-size: 0.875rem;
		font-weight: 600;
	}
	input:not([type="hidden"]),
	select {
		display: block;
		width: 100%;
		min-height: 2.75rem;
		margin-top: 0.375rem;
		border: 1px solid var(--border-trace);
		border-radius: var(--radius-control);
		padding: 0.5rem 0.75rem;
		background: var(--reading-room);
		color: var(--reading-ink);
		font-size: 1rem;
	}
	select {
		min-width: 8.5rem;
	}
	select:disabled {
		background: var(--archive-layer);
		opacity: 0.75;
		cursor: not-allowed;
	}
	@media (max-width: 42rem) {
		.admin-search {
			grid-template-columns: minmax(0, 1fr) auto;
		}
		.admin-search-field {
			grid-column: 1 / -1;
		}
		.admin-search-actions {
			gap: 0.5rem;
		}
		.admin-user-list a,
		.admin-register {
			gap: 0.75rem;
			grid-template-columns: minmax(0, 1fr) 3rem 1rem;
			padding-inline: 0;
		}
		.admin-access-label,
		.admin-user-action > span {
			display: none;
		}
		.admin-user-identity {
			gap: 0.75rem;
		}
		.admin-page-heading {
			margin-block: 2rem 1.5rem;
		}
	}
	@media (max-width: 26rem) {
		.admin-search {
			grid-template-columns: minmax(0, 1fr);
		}
		.admin-search-actions {
			justify-content: end;
		}
	}
</style>
