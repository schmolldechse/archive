<script lang="ts">
	import { untrack } from "svelte";
	import { beforeNavigate, invalidate } from "$app/navigation";
	import { page } from "$app/state";
	import {
		ArchiveRole,
		ArchivePermissionEffect,
		type UserAccessResponse,
		type PermissionDefinitionResponse,
		type AuthenticationSessionResponse,
		type ProblemDetails,
		type UpdateUserAccessRequest
	} from "$api";
	import { apiUrl } from "$lib/api-url";
	import { pageHref } from "$lib/admin/navigation";
	import Button from "$lib/components/ui/Button.svelte";
	import AdminPagination from "./AdminPagination.svelte";
	import {
		DialogRoot,
		DialogContent,
		DialogTitle,
		DialogDescription,
		DialogActions,
		DialogClose
	} from "$lib/components/ui/dialog";

	let {
		data
	}: {
		data: {
			access: UserAccessResponse;
			etag: string;
			catalog: PermissionDefinitionResponse[];
			capabilities: { roles: boolean; permissions: boolean };
			permissionQ: string;
			permissionPage: number;
			permissionPageSize: number;
			permissionTotal: number;
			permissionItems: PermissionDefinitionResponse[];
			usersHref: string;
			session: AuthenticationSessionResponse | null;
		};
	} = $props();

	// Keep the original edit baseline across permission-page navigation. Only a save or explicit reload replaces it.
	let saved = $state.raw(untrack(() => data.access));
	let etag = $state(untrack(() => data.etag));
	let role = $state(untrack(() => data.access.role));
	let overrides = $state<Record<string, ArchivePermissionEffect | null>>(
		untrack(() => Object.fromEntries(data.access.permissions.map((item) => [item.key, item.override])))
	);
	let confirmation = $state(false);
	let saving = $state(false);
	let reloading = $state(false);
	let conflict = $state(false);
	let blocked = $state(false);
	let message = $state("");
	let success = $state("");
	let saveButton = $state<HTMLButtonElement | HTMLAnchorElement | null>(null);
	let saveStatus = $state<HTMLDivElement | null>(null);
	const ownAccount = $derived(data.session?.user?.handle === saved.handle);
	const canRole = $derived(data.capabilities.roles && !ownAccount && !blocked && !saving && !reloading && !conflict);
	const canPermissions = $derived(
		data.capabilities.permissions && !ownAccount && !blocked && !saving && !reloading && !conflict
	);
	const changes = $derived(saved.permissions.filter((item) => (overrides[item.key] ?? null) !== item.override));
	const dirty = $derived(role !== saved.role || changes.length > 0);
	const context = $derived(new URL(data.usersHref, "https://archive.invalid").searchParams);
	const permissionResetHref = $derived(
		pageHref(new URL(page.url.pathname + (context.size ? `?${context}` : ""), page.url.origin), "permissionPage", 1)
	);

	beforeNavigate((navigation) => {
		if (dirty && navigation.to?.url.pathname !== page.url.pathname && !window.confirm("Discard unsaved access changes?"))
			navigation.cancel();
	});

	function setBaseline(access: UserAccessResponse, tag: string) {
		saved = access;
		etag = tag;
		role = access.role;
		overrides = Object.fromEntries(access.permissions.map((item) => [item.key, item.override]));
		conflict = false;
		blocked = false;
		message = "";
	}
	function reset() {
		setBaseline(saved, etag);
		success = "";
	}
	function overrideLabel(value: ArchivePermissionEffect | null) {
		return value === null ? "Use role" : value === ArchivePermissionEffect.ALLOW ? "Allow" : "Deny";
	}

	async function reloadAccess() {
		if (dirty && !window.confirm("Discard your draft and load the latest access settings?")) return;
		reloading = true;
		try {
			const response = await fetch(apiUrl(`/api/admin/users/${encodeURIComponent(saved.handle)}/access`), {
				credentials: "include",
				cache: "no-store",
				redirect: "error",
				signal: AbortSignal.timeout(10000)
			});
			if (!response.ok || !response.headers.get("etag")) throw new Error();
			setBaseline(await response.json(), response.headers.get("etag")!);
			success = "";
		} catch {
			message = "Access could not be reloaded. Your draft has been kept.";
		} finally {
			reloading = false;
		}
	}

	async function save() {
		if (!dirty || saving || reloading || conflict || ownAccount || blocked) return;
		saving = true;
		confirmation = false;
		message = "";
		success = "";
		const body: UpdateUserAccessRequest = {};
		if (role !== saved.role) body.role = role;
		if (changes.length)
			body.permissionOverrides = changes.map((item) => ({ permission: item.key, effect: overrides[item.key] ?? null }));
		try {
			const response = await fetch(apiUrl(`/api/admin/users/${encodeURIComponent(saved.handle)}/access`), {
				method: "PATCH",
				credentials: "include",
				redirect: "error",
				signal: AbortSignal.timeout(10000),
				headers: { "content-type": "application/json", "if-match": etag, accept: "application/json" },
				body: JSON.stringify(body)
			});
			if (response.status === 412) {
				conflict = true;
				message = "Access has changed since you opened this account. Reload access before saving again.";
				return;
			}
			if (response.status === 401 || response.status === 403) {
				blocked = true;
				message = "Your access has changed. You can no longer save this draft.";
				await invalidate("app:auth");
				return;
			}
			if (!response.ok) {
				const problem: ProblemDetails | null = await response.json().catch(() => null);
				message = problem?.detail ?? "Access could not be saved. Please try again.";
				return;
			}
			const updated: UserAccessResponse = await response.json();
			const newTag = response.headers.get("etag");
			if (!newTag) {
				conflict = true;
				message = "The save completed, but access could not be validated. Reload access before editing again.";
				return;
			}
			setBaseline(updated, newTag);
			success = `Access updated for @${updated.handle}.`;
		} catch {
			message = "The save could not be confirmed. Reload access before trying again.";
			conflict = true;
		} finally {
			saving = false;
		}
	}
</script>

<svelte:window
	onbeforeunload={(event) => {
		if (dirty) {
			event.preventDefault();
			event.returnValue = "";
		}
	}}
/>

<header class="admin-page-heading admin-profile-heading">
	<div>
		<h1>{saved.displayName}</h1>
		<p class="record-value">@{saved.handle}</p>
	</div>
	<span class="admin-role" data-admin={saved.role === ArchiveRole.ADMIN}
		>{saved.role === ArchiveRole.ADMIN ? "Admin" : "User"}</span
	>
</header>
{#if ownAccount}<p class="admin-notice">
		Your own access is read-only. Ask another administrator to make changes.
	</p>{:else if !data.capabilities.roles && !data.capabilities.permissions}<p class="admin-notice">
		You have read-only access to user administration.
	</p>{/if}
<div class="admin-access-layout">
	<section class="admin-role-section" aria-labelledby="role-heading">
		<div class="admin-section-intro">
			<h2 id="role-heading">Role</h2>
			<p>Sets the default permissions for this account.</p>
		</div>
		<div class="admin-role-control">
			<label for="user-role">Assigned role</label>
			<select id="user-role" bind:value={role} disabled={!canRole} onchange={() => (success = "")}
				><option value={ArchiveRole.USER}>User</option><option value={ArchiveRole.ADMIN}>Admin</option></select
			>
			<p class="admin-hint">Personal overrides remain in place when the role changes.</p>
		</div>
	</section>
	<section class="admin-permissions-section" aria-labelledby="permissions-heading">
		<div class="admin-section-heading">
			<h2 id="permissions-heading">Permissions</h2>
			<span>{data.catalog.length} permissions</span>
		</div>
		<p class="admin-hint">Use role keeps the default. Allow or Deny takes precedence for this account.</p>
		<form method="GET" class="admin-search admin-permission-search" role="search" aria-label="Search permissions">
			{#each [...context] as [key, value]}<input type="hidden" name={key} {value} />{/each}
			<label class="admin-search-field"
				>Search permissions<input
					name="permissionQ"
					value={data.permissionQ}
					maxlength="128"
					type="search"
					placeholder="Name or permission key"
				/></label
			>
			<Button type="submit">Search</Button>
			{#if data.permissionQ}<Button href={permissionResetHref} variant="text">Clear</Button>{/if}
		</form>
		<div class="admin-permission-register">
			<span>Permission</span><span>Saved access</span><span>Personal override</span>
		</div>
		<ul class="admin-permission-list" aria-label="Permission settings">
			{#each data.permissionItems as definition (definition.key)}
				{@const permission = saved.permissions.find((item) => item.key === definition.key)!}
				<li>
					<div class="admin-permission-description">
						<label for={`permission-${definition.key}`}>{definition.displayName}</label><code>{definition.key}</code>
						<p>{definition.description}</p>
					</div>
					<div class="admin-permission-access" data-allowed={permission.allowed}>
						<strong>{permission.allowed ? "Allowed" : "Not allowed"}</strong><span
							>{permission.override === null ? "From role" : "Personal override"}</span
						>
					</div>
					<div class="admin-permission-control">
						<label class="admin-mobile-label" for={`permission-${definition.key}`}>Personal override</label><select
							id={`permission-${definition.key}`}
							value={overrides[definition.key] ?? ""}
							disabled={!canPermissions}
							onchange={(event) => {
								overrides[definition.key] =
									event.currentTarget.value === "" ? null : (event.currentTarget.value as ArchivePermissionEffect);
								success = "";
							}}
							><option value="">Use role</option><option value={ArchivePermissionEffect.ALLOW}>Allow</option><option
								value={ArchivePermissionEffect.DENY}>Deny</option
							></select
						>
					</div>
				</li>
			{/each}
		</ul>
		{#if !data.permissionItems.length}<div class="admin-empty">
				<h3>No permissions found</h3>
				<p>Try another name or permission key.</p>
				<Button href={permissionResetHref} variant="text">Show all permissions</Button>
			</div>{/if}
		<div class="admin-pagination">
			<p class="admin-hint">
				{data.permissionTotal === 0
					? "0 permissions"
					: `${(data.permissionPage - 1) * data.permissionPageSize + 1}–${Math.min(data.permissionPage * data.permissionPageSize, data.permissionTotal)} of ${data.permissionTotal} permissions`}
			</p>
			<AdminPagination
				total={data.permissionTotal}
				pageSize={data.permissionPageSize}
				currentPage={data.permissionPage}
				href={(next) => pageHref(page.url, "permissionPage", next)}
				label="Permission pages"
			/>
		</div>
	</section>
</div>
<div class="admin-save-bar">
	<div aria-live="polite" tabindex="-1" bind:this={saveStatus}>
		{#if saving}<p>Saving changes…</p>{:else if message}<p class="admin-save-error" role="alert">
				{message}
			</p>{:else if success}<p class="admin-save-success">
				✓ {success}
			</p>{:else}<p>
				{dirty
					? `${changes.length + Number(role !== saved.role)} unsaved ${changes.length + Number(role !== saved.role) === 1 ? "change" : "changes"}`
					: "No unsaved changes"}
			</p>{/if}
	</div>
	<div class="admin-save-actions">
		{#if conflict}<Button onclick={reloadAccess} loading={reloading}>Reload access</Button>{:else}<Button
				onclick={reset}
				disabled={!dirty || saving || reloading}>Reset</Button
			><Button
				variant="primary"
				bind:ref={saveButton}
				onclick={() => (confirmation = true)}
				disabled={!dirty || ownAccount || blocked || reloading}
				loading={saving}
				loadingLabel="Saving…">Save changes</Button
			>{/if}
	</div>
</div>

<DialogRoot bind:open={confirmation}>
	<DialogContent
		size="medium"
		class="admin-confirmation"
		returnFocus={() => (saveButton instanceof HTMLButtonElement && !saveButton.disabled ? saveButton : saveStatus)}
	>
		<DialogTitle>Update access</DialogTitle>
		<DialogDescription>Save these changes for @{saved.handle}?</DialogDescription>
		<ul class="admin-confirm-changes">
			{#if role !== saved.role}<li>
					<strong>Role</strong><span
						>{saved.role === ArchiveRole.ADMIN ? "Admin" : "User"} → {role === ArchiveRole.ADMIN ? "Admin" : "User"}</span
					>
				</li>{/if}{#each changes as change (change.key)}<li>
					<strong>{data.catalog.find((item) => item.key === change.key)?.displayName}</strong><span
						>{overrideLabel(change.override)} → {overrideLabel(overrides[change.key] ?? null)}</span
					>
				</li>{/each}
		</ul>
		<DialogActions
			><DialogClose>Cancel</DialogClose><Button variant="primary" onclick={save}>Save changes</Button></DialogActions
		>
	</DialogContent>
</DialogRoot>

<style>
	/* Restore native modal centering inside the site's reset stylesheet. */
	:global(.admin-confirmation[data-modal="true"]) {
		margin: auto;
	}
	.admin-page-heading {
		margin-block: 1.5rem 2.5rem;
	}
	.admin-page-heading h1 {
		margin: 0;
		font-size: clamp(2.25rem, 4vw, 3rem);
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
		display: flex;
		align-items: end;
		gap: 1rem;
		margin-block: 1.5rem;
	}
	.admin-search-field {
		flex: 1;
		min-width: 0;
	}
	.admin-role {
		justify-self: start;
		display: inline-flex;
		align-items: center;
		gap: 0.5rem;
		font-size: 0.875rem;
		border: 1px solid var(--border-trace);
		padding: 0.375rem 0.75rem;
		border-radius: var(--radius-control);
		flex: none;
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
		margin-top: 1.5rem;
	}
	.admin-pagination p {
		margin: 0;
		font-variant-numeric: tabular-nums;
	}
	.admin-profile-heading {
		display: flex;
		justify-content: space-between;
		align-items: center;
		gap: 1rem;
	}
	.admin-profile-heading .record-value {
		font-size: 0.875rem;
	}
	.admin-access-layout {
		display: grid;
		gap: 2.5rem;
	}
	.admin-role-section,
	.admin-permissions-section {
		border-top: 1px solid var(--border-trace);
		padding-top: 2rem;
		min-width: 0;
	}
	.admin-role-section {
		display: grid;
		grid-template-columns: minmax(0, 1fr) minmax(14rem, 22rem);
		gap: 2rem;
	}
	.admin-access-layout h2 {
		margin: 0;
		font-size: 1.875rem;
		font-weight: 500;
		line-height: 1.2;
		overflow-wrap: anywhere;
	}
	.admin-section-intro p {
		color: var(--marginal-note);
		font-size: 0.9375rem;
		margin-block: 0.75rem 0;
	}
	.admin-role-section .admin-hint,
	.admin-hint {
		color: var(--marginal-note);
		font-size: 0.875rem;
		line-height: 1.5;
	}
	.admin-role-section .admin-hint {
		margin-block: 0.75rem 0;
	}
	.admin-section-heading {
		display: flex;
		justify-content: space-between;
		align-items: center;
	}
	.admin-section-heading > span {
		font: 400 0.8125rem var(--font-record);
		color: var(--marginal-note);
	}
	.admin-permission-search {
		max-width: 40rem;
		margin-block: 1.5rem 2rem;
	}
	.admin-permission-register,
	.admin-permission-list > li {
		display: grid;
		grid-template-columns: minmax(0, 1fr) 8.5rem 11rem;
		gap: 2rem;
	}
	.admin-permission-register {
		border-block: 1px solid var(--border-trace);
		padding-block: 0.625rem;
		color: var(--marginal-note);
		font-size: 0.8125rem;
	}
	.admin-permission-list {
		list-style: none;
		padding: 0;
		margin: 0;
	}
	.admin-permission-list > li {
		padding-block: 1.5rem;
		border-bottom: 1px solid color-mix(in srgb, var(--border-trace) 45%, transparent);
		align-items: start;
	}
	.admin-permission-description label {
		font-size: 1rem;
	}
	.admin-permission-description code {
		display: block;
		margin-top: 0.25rem;
		font: 400 0.75rem / 1.5 var(--font-record);
		color: var(--marginal-note);
		overflow-wrap: anywhere;
	}
	.admin-permission-description p {
		margin: 0.625rem 0 0;
		color: var(--marginal-note);
		font-size: 0.875rem;
		line-height: 1.5;
		max-width: 70ch;
	}
	.admin-permission-access {
		display: grid;
		gap: 0.25rem;
		padding-top: 0.125rem;
		font-size: 0.875rem;
	}
	.admin-permission-access strong {
		font-weight: 600;
	}
	.admin-permission-access[data-allowed="true"] strong {
		color: var(--preservation-green);
	}
	.admin-permission-access span {
		font-size: 0.75rem;
		color: var(--marginal-note);
	}
	.admin-permission-control select {
		margin: 0;
	}
	.admin-mobile-label {
		display: none !important;
	}
	.admin-notice {
		border-inline-start: 3px solid var(--register-mark);
		padding: 0.5rem 1rem;
		margin-block: 0 2rem;
		font-size: 0.9375rem;
	}
	.admin-save-bar {
		position: sticky;
		bottom: 0;
		z-index: 5;
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 1rem;
		border-top: 1px solid var(--border-trace);
		background: var(--reading-room);
		padding-block: 1rem;
		margin-top: 2rem;
	}
	.admin-save-bar p {
		font-size: 0.9375rem;
		margin: 0;
	}
	.admin-save-actions {
		display: flex;
		align-items: center;
		flex-wrap: wrap;
		gap: 0.75rem;
		flex: none;
	}
	.admin-save-error {
		color: var(--time-marker);
	}
	.admin-save-success {
		color: var(--preservation-green);
	}
	.admin-empty {
		padding-block: 3rem;
		color: var(--marginal-note);
	}
	.admin-empty h3 {
		margin: 0;
		color: var(--reading-ink);
		font-weight: 500;
	}
	.admin-empty p {
		margin-top: 0.75rem;
	}
	.admin-confirm-changes {
		list-style: none;
		padding: 0;
		margin: 1.5rem 0;
		max-height: 50vh;
		overflow: auto;
	}
	.admin-confirm-changes li {
		display: grid;
		gap: 0.25rem;
		padding-block: 0.75rem;
		border-bottom: 1px solid var(--border-trace);
		font-size: 0.9375rem;
	}
	.admin-confirm-changes span {
		color: var(--marginal-note);
	}
	.admin-confirm-changes li:last-child {
		border-bottom: 0;
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
		scroll-margin-block: 6rem;
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
		.admin-role-section {
			grid-template-columns: minmax(0, 1fr);
			gap: 1.5rem;
		}
		.admin-profile-heading {
			align-items: start;
		}
		.admin-profile-heading > div {
			min-width: 0;
		}
		.admin-permission-search {
			flex-wrap: wrap;
		}
		.admin-permission-search .admin-search-field {
			flex-basis: 100%;
		}
		.admin-permission-register {
			display: none;
		}
		.admin-permission-list {
			border-top: 1px solid var(--border-trace);
		}
		.admin-permission-list > li {
			grid-template-columns: minmax(0, 1fr) minmax(8.5rem, 45%);
			gap: 1rem;
		}
		.admin-permission-description {
			grid-column: 1 / -1;
		}
		.admin-permission-access {
			align-self: end;
			padding-block: 0.5rem;
		}
		.admin-mobile-label {
			display: block !important;
			margin-bottom: 0.375rem;
		}
		.admin-save-bar {
			flex-direction: column;
			align-items: stretch;
		}
		.admin-save-actions {
			justify-content: end;
		}
		.admin-section-heading > span {
			font-size: 0.75rem;
		}
		.admin-section-heading {
			gap: 1rem;
			flex-wrap: wrap;
		}
	}
	@media (max-width: 26rem) {
		.admin-permission-list > li {
			grid-template-columns: minmax(0, 1fr);
		}
		.admin-save-bar {
			gap: 0.5rem;
		}
		.admin-save-bar p {
			font-size: 0.875rem;
		}
	}
	@media (max-height: 32rem) {
		.admin-save-bar {
			position: static;
		}
	}
</style>
