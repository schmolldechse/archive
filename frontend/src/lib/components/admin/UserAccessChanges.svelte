<script lang="ts">
	import { ArchiveRole, ArchivePermissionEffect, type PermissionDefinitionResponse, type UserPermissionResponse } from "$api";
	import { TableRoot, TableCaption, TableHeader, TableBody, TableRow, TableHead, TableCell } from "$lib/components/ui/table";

	let {
		savedRole,
		role,
		changes,
		catalog,
		overrides
	}: {
		savedRole: ArchiveRole;
		role: ArchiveRole;
		changes: UserPermissionResponse[];
		catalog: PermissionDefinitionResponse[];
		overrides: Record<string, ArchivePermissionEffect | null>;
	} = $props();

	/** Compare stored overrides, rather than presenting an unsaved value as effective access. */
	function overrideLabel(effect: ArchivePermissionEffect | null): string {
		return effect === null ? "Use role" : effect === ArchivePermissionEffect.ALLOW ? "Allow" : "Deny";
	}
</script>

<div class="access-changes">
	<TableRoot class="change-table">
		<TableCaption class="sr-only">Proposed changes to the role and personal permission overrides</TableCaption>
		<TableHeader
			><TableRow>
				<TableHead>Setting</TableHead><TableHead class="value-column">Current</TableHead><TableHead class="value-column"
					>New</TableHead
				>
			</TableRow></TableHeader
		>
		<TableBody>
			{#if role !== savedRole}<TableRow>
					<TableHead scope="row">Role</TableHead>
					<TableCell><span class="current-value">{savedRole === ArchiveRole.ADMIN ? "Admin" : "User"}</span></TableCell>
					<TableCell><strong>{role === ArchiveRole.ADMIN ? "Admin" : "User"}</strong></TableCell>
				</TableRow>{/if}
			{#each changes as change (change.key)}<TableRow>
					<TableHead scope="row">{catalog.find((item) => item.key === change.key)?.displayName ?? change.key}</TableHead>
					<TableCell><span class="current-value">{overrideLabel(change.override)}</span></TableCell>
					<TableCell><strong>{overrideLabel(overrides[change.key] ?? null)}</strong></TableCell>
				</TableRow>{/each}
		</TableBody>
	</TableRoot>
</div>

<style>
	.access-changes {
		margin-top: 1.5rem;
	}
	.access-changes :global(.change-table) {
		width: 100%;
		table-layout: fixed;
		font-size: 0.875rem;
		line-height: 1.4;
	}
	.access-changes :global(.value-column) {
		width: 24%;
	}
	.access-changes :global([data-table-header]) {
		border-block: 1px solid var(--border-trace);
	}
	.access-changes :global([data-table-head]),
	.access-changes :global([data-table-cell]) {
		padding: 0.875rem 0.5rem;
		vertical-align: top;
	}
	.access-changes :global([data-table-header] [data-table-head]) {
		color: var(--marginal-note);
		font-size: 0.75rem;
	}
	.current-value {
		color: var(--marginal-note);
	}
	strong {
		font-weight: 600;
	}
	@media (max-width: 32rem) {
		.access-changes :global(.value-column) {
			width: 26%;
		}
		.access-changes :global([data-table-head]),
		.access-changes :global([data-table-cell]) {
			padding-inline: 0.375rem;
		}
	}
</style>
