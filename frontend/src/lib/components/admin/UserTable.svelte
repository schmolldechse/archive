<script lang="ts">
	import { page } from "$app/state";
	import ArrowRight from "@lucide/svelte/icons/arrow-right";
	import ArrowLeftRight from "@lucide/svelte/icons/arrow-left-right";
	import SearchX from "@lucide/svelte/icons/search-x";
	import ShieldCheck from "@lucide/svelte/icons/shield-check";
	import UserRound from "@lucide/svelte/icons/user-round";
	import type { UserListResponse } from "$api";
	import Avatar from "$lib/components/ui/Avatar.svelte";
	import Badge from "$lib/components/ui/Badge.svelte";
	import Button from "$lib/components/ui/Button.svelte";
	import {
		TableRoot,
		TableViewport,
		TableCaption,
		TableHeader,
		TableBody,
		TableRow,
		TableHead,
		TableCell
	} from "$lib/components/ui/table";
	import { pageHref } from "$lib/admin/navigation";
	import AdminPagination from "./AdminPagination.svelte";

	let { users, filtered, canManage }: { users: UserListResponse; filtered: boolean; canManage: boolean } = $props();
	const total = $derived(Number(users.total));
	const current = $derived(Number(users.page));
	const size = $derived(Number(users.pageSize));
	const range = $derived(
		total === 0
			? "0 users"
			: `${((current - 1) * size + 1).toLocaleString()}–${Math.min(current * size, total).toLocaleString()} of ${total.toLocaleString()} users`
	);
</script>

<section class="user-directory" aria-labelledby="user-directory-title">
	<div class="directory-heading">
		<h2 id="user-directory-title">{filtered ? "Search results" : "All users"}</h2>
		<p role="status" aria-atomic="true">{range}</p>
	</div>
	<p class="table-scroll-hint"><ArrowLeftRight aria-hidden="true" />Scroll to view all columns.</p>
	<!-- The API supplies a page of results; the native Table parts preserve server pagination. -->
	<TableViewport aria-labelledby="user-directory-title">
		<TableRoot class="user-table">
			<TableCaption class="sr-only">{filtered ? "Users matching the current filters" : "Registered users"}</TableCaption>
			<TableHeader>
				<TableRow
					><TableHead>User</TableHead><TableHead class="role-column">Role</TableHead><TableHead
						class="access-column"
						align="end">Access</TableHead
					></TableRow
				>
			</TableHeader>
			<TableBody>
				{#each users.items as user (user.handle)}
					{@const href = `/admin/users/${encodeURIComponent(user.handle!)}${page.url.search}`}
					<TableRow>
						<TableCell>
							<a class="user-identity" {href}>
								<Avatar
									src={user.profileImageUrl}
									initials={user.displayName
										?.split(/\s+/)
										.map((part) => part[0])
										.slice(0, 2)
										.join("")}
									decorative
								/>
								<span class="user-name"
									><strong>{user.displayName}</strong><span class="record-value">@{user.handle}</span></span
								>
							</a>
						</TableCell>
						<TableCell>
							<Badge variant={user.role === "ADMIN" ? "info" : "neutral"}>
								{#snippet icon()}{#if user.role === "ADMIN"}<ShieldCheck aria-hidden="true" />{:else}<UserRound
											aria-hidden="true"
										/>{/if}{/snippet}
								{user.role === "ADMIN" ? "Admin" : "User"}
							</Badge>
						</TableCell>
						<TableCell align="end">
							<Button {href} variant="text" aria-label={`${canManage ? "Manage" : "View"} access for @${user.handle}`}>
								{canManage ? "Manage access" : "View access"}<ArrowRight aria-hidden="true" />
							</Button>
						</TableCell>
					</TableRow>
				{:else}
					<TableRow>
						<TableCell colspan={3}>
							<div class="directory-empty">
								<SearchX aria-hidden="true" />
								<h3>{filtered ? "No users found" : "No users yet"}</h3>
								<p>{filtered ? "Try another handle, name or role." : "Registered users will appear here."}</p>
								{#if filtered}<Button href="/admin/users">Clear search</Button>{/if}
							</div>
						</TableCell>
					</TableRow>
				{/each}
			</TableBody>
		</TableRoot>
	</TableViewport>
	{#if total > size}
		<div class="directory-pagination">
			<AdminPagination
				{total}
				pageSize={size}
				currentPage={current}
				href={(next) => pageHref(page.url, "page", next)}
				label="User pages"
			/>
		</div>
	{/if}
</section>

<style>
	.user-directory {
		margin-top: 2.5rem;
	}
	.directory-heading {
		display: flex;
		flex-wrap: wrap;
		align-items: baseline;
		justify-content: space-between;
		gap: 0.5rem 1.5rem;
		margin-bottom: 1rem;
	}
	h2 {
		margin: 0;
		font-size: 1.75rem;
		font-weight: 500;
		line-height: 1.2;
	}
	.directory-heading p {
		margin: 0;
		color: var(--marginal-note);
		font-size: 0.875rem;
		font-variant-numeric: tabular-nums;
	}
	.user-directory :global(.user-table) {
		min-width: 34rem;
		table-layout: fixed;
	}
	.user-directory :global([data-table-header]) {
		border-block: 1px solid var(--border-trace);
	}
	.user-directory :global([data-table-head]) {
		color: var(--marginal-note);
		font-size: 0.8125rem;
	}
	.user-directory :global(.role-column) {
		width: 8rem;
	}
	.user-directory :global(.access-column) {
		width: 12rem;
	}
	.user-directory :global([data-table-cell]) {
		padding-block: 1rem;
	}
	.user-identity {
		display: flex;
		align-items: center;
		gap: 1rem;
		min-height: 3rem;
		width: fit-content;
		max-width: 100%;
		color: var(--reading-ink);
		text-decoration: none;
	}
	.user-identity:hover strong {
		color: var(--register-mark);
		text-decoration: underline;
		text-underline-offset: 0.16em;
	}
	.user-name {
		display: grid;
		gap: 0.25rem;
		min-width: 0;
		overflow-wrap: anywhere;
	}
	.user-name .record-value {
		color: var(--marginal-note);
		font-size: 0.8125rem;
	}
	.directory-pagination {
		display: flex;
		justify-content: end;
		margin-top: 1.5rem;
	}
	.directory-empty {
		display: grid;
		justify-items: start;
		gap: 0.75rem;
		padding: 2rem 0;
	}
	.directory-empty > :global(svg) {
		width: 1.5rem;
		height: 1.5rem;
		color: var(--marginal-note);
	}
	.directory-empty h3 {
		margin: 0;
		font-size: 1.125rem;
		font-weight: 600;
	}
	.directory-empty p {
		margin: 0 0 0.5rem;
		color: var(--marginal-note);
		font-size: 0.9375rem;
	}
	.table-scroll-hint {
		display: none;
	}
	@media (max-width: 42rem) {
		.table-scroll-hint {
			display: flex;
			align-items: center;
			gap: 0.5rem;
			margin: 0 0 0.5rem;
			color: var(--marginal-note);
			font-size: 0.8125rem;
		}
		.table-scroll-hint :global(svg) {
			width: 1rem;
			height: 1rem;
			flex: none;
		}
	}
</style>
