<script lang="ts">
	import { untrack } from "svelte";
	import { beforeNavigate, invalidate } from "$app/navigation";
	import { page } from "$app/state";
	import {
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
	import Avatar from "$lib/components/ui/Avatar.svelte";
	import AdminPagination from "./AdminPagination.svelte";
	import UserPermissionSearch from "./UserPermissionSearch.svelte";
	import UserPermissionTable from "./UserPermissionTable.svelte";
	import UserRoleAssignment from "./UserRoleAssignment.svelte";
	import UserAccessChanges from "./UserAccessChanges.svelte";
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
	const changeCount = $derived(changes.length + Number(role !== saved.role));
	const dirty = $derived(changeCount > 0);
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

<section class="account-overview" aria-label="Account access">
	<header class="account-heading">
		<Avatar
			size="default"
			decorative
			initials={saved.displayName
				.split(/\s+/)
				.map((part) => part[0])
				.slice(0, 2)
				.join("")}
		/>
		<div class="account-identity">
			<h1>{saved.displayName}</h1>
		</div>
	</header>
	<UserRoleAssignment
		{role}
		disabled={!canRole}
		onRoleChange={(value) => {
			role = value;
			success = "";
		}}
	/>
</section>
{#if ownAccount}<p class="admin-notice">
		Your own access is read-only. Ask another administrator to make changes.
	</p>{:else if !data.capabilities.roles && !data.capabilities.permissions}<p class="admin-notice">
		You have read-only access to user administration.
	</p>{/if}
<section class="admin-permissions-section" aria-labelledby="permissions-heading">
	<div class="admin-section-heading">
		<h2 id="permissions-heading">Permissions</h2>
		<p role="status" aria-atomic="true">
			{data.permissionTotal <= data.permissionPageSize
				? `${data.permissionTotal} ${data.permissionTotal === 1 ? "permission" : "permissions"}`
				: `${(data.permissionPage - 1) * data.permissionPageSize + 1}–${Math.min(data.permissionPage * data.permissionPageSize, data.permissionTotal)} of ${data.permissionTotal} permissions`}
		</p>
	</div>
	<p class="admin-hint">Use role follows the default. Allow and Deny apply only to this account.</p>
	{#key data.permissionQ}<UserPermissionSearch query={data.permissionQ} {context} resetHref={permissionResetHref} />{/key}
	<UserPermissionTable
		definitions={data.permissionItems}
		permissions={saved.permissions}
		{overrides}
		disabled={!canPermissions}
		resetHref={permissionResetHref}
		onOverrideChange={(key, effect) => {
			overrides[key] = effect;
			success = "";
		}}
	/>
	{#if data.permissionTotal > data.permissionPageSize}
		<div class="admin-pagination">
			<AdminPagination
				total={data.permissionTotal}
				pageSize={data.permissionPageSize}
				currentPage={data.permissionPage}
				href={(next) => pageHref(page.url, "permissionPage", next)}
				label="Permission pages"
			/>
		</div>
	{/if}
</section>
<div class="admin-save-bar">
	<div aria-live="polite" tabindex="-1" bind:this={saveStatus}>
		{#if saving}<p>Saving changes…</p>{:else if message}<p class="admin-save-error" role="alert">
				{message}
			</p>{:else if success}<p class="admin-save-success">
				✓ {success}
			</p>{:else}<p>
				{dirty ? `${changeCount} unsaved ${changeCount === 1 ? "change" : "changes"}` : "No unsaved changes"}
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
		<DialogTitle>Confirm access changes</DialogTitle>
		<DialogDescription
			>{changeCount}
			{changeCount === 1 ? "change" : "changes"} for <span class="record-value">@{saved.handle}</span>.</DialogDescription
		>
		<UserAccessChanges savedRole={saved.role} {role} {changes} catalog={data.catalog} {overrides} />
		<DialogActions
			><DialogClose>Keep editing</DialogClose><Button variant="primary" onclick={save}>Save changes</Button></DialogActions
		>
	</DialogContent>
</DialogRoot>

<style>
	/* Restore native modal centering inside the site's reset stylesheet. */
	:global(.admin-confirmation[data-modal="true"]) {
		margin: auto;
	}
	.account-overview {
		display: grid;
		grid-template-columns: minmax(0, 0.9fr) minmax(0, 1.1fr);
		align-items: center;
		gap: 2rem 3rem;
		margin-block: 1.75rem 2rem;
		padding-bottom: 2rem;
		border-bottom: 1px solid var(--border-trace);
	}
	.account-heading {
		display: flex;
		align-items: center;
		gap: 1rem;
		min-width: 0;
	}
	.account-identity {
		min-width: 0;
	}
	.account-heading h1 {
		margin: 0;
		font-size: clamp(1.75rem, 3vw, 2.25rem);
		font-weight: 500;
		line-height: 1.12;
		letter-spacing: -0.025em;
		overflow-wrap: anywhere;
	}
	.admin-pagination {
		display: flex;
		align-items: center;
		justify-content: end;
		flex-wrap: wrap;
		gap: 1rem;
		margin-top: 1.5rem;
	}
	.admin-permissions-section {
		min-width: 0;
	}
	.admin-permissions-section h2 {
		margin: 0;
		font-size: 1.875rem;
		font-weight: 500;
		line-height: 1.2;
		overflow-wrap: anywhere;
	}
	.admin-hint {
		color: var(--marginal-note);
		font-size: 0.875rem;
		line-height: 1.5;
	}
	.admin-section-heading {
		display: flex;
		justify-content: space-between;
		align-items: baseline;
		flex-wrap: wrap;
		gap: 0.5rem 1.5rem;
	}
	.admin-section-heading > p {
		margin: 0;
		font-size: 0.875rem;
		font-variant-numeric: tabular-nums;
		color: var(--marginal-note);
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
	@media (max-width: 56rem) {
		.account-overview {
			grid-template-columns: minmax(0, 1fr);
			gap: 1.5rem;
		}
	}
	@media (max-width: 42rem) {
		.admin-save-bar {
			flex-direction: column;
			align-items: stretch;
		}
		.admin-save-actions {
			justify-content: end;
		}
	}
	@media (max-width: 26rem) {
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
