<script lang="ts">
	import Check from "@lucide/svelte/icons/check";
	import Minus from "@lucide/svelte/icons/minus";
	import ArrowLeftRight from "@lucide/svelte/icons/arrow-left-right";
	import SearchX from "@lucide/svelte/icons/search-x";
	import { ArchivePermissionEffect, type PermissionDefinitionResponse, type UserPermissionResponse } from "$api";
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

	let {
		definitions,
		permissions,
		overrides,
		disabled,
		resetHref,
		onOverrideChange
	}: {
		definitions: PermissionDefinitionResponse[];
		permissions: UserPermissionResponse[];
		overrides: Record<string, ArchivePermissionEffect | null>;
		disabled: boolean;
		resetHref: string;
		onOverrideChange: (key: string, effect: ArchivePermissionEffect | null) => void;
	} = $props();
</script>

<div class="permission-register">
	<p class="scroll-hint"><ArrowLeftRight aria-hidden="true" />Scroll to view access settings.</p>
	<!-- Saved access remains the server's evaluation until the draft is confirmed and saved. -->
	<TableViewport aria-label="Permission settings">
		<TableRoot class="permission-table">
			<TableCaption class="sr-only">Permissions, saved access and personal overrides</TableCaption>
			<TableHeader>
				<TableRow>
					<TableHead>Permission</TableHead>
					<TableHead class="access-column">Saved access</TableHead>
					<TableHead class="override-column">Personal override <span class="field-qualifier">Optional</span></TableHead>
				</TableRow>
			</TableHeader>
			<TableBody>
				{#each definitions as definition (definition.key)}
					{@const permission = permissions.find((item) => item.key === definition.key)!}
					{@const changed = (overrides[definition.key] ?? null) !== permission.override}
					<TableRow selected={changed}>
						<TableHead scope="row">
							<div class="permission-description">
								<label for={`permission-${definition.key}`}
									>{definition.displayName}<span class="sr-only"> personal override, optional</span></label
								>
								<code>{definition.key}</code>
								<p>{definition.description}</p>
							</div>
						</TableHead>
						<TableCell>
							<div class="saved-access" data-allowed={permission.allowed}>
								<strong
									>{#if permission.allowed}<Check aria-hidden="true" />{:else}<Minus
											aria-hidden="true"
										/>{/if}{permission.allowed ? "Allowed" : "Not allowed"}</strong
								>
								<span>{permission.override === null ? "From role" : "Personal override"}</span>
							</div>
						</TableCell>
						<TableCell>
							<select
								id={`permission-${definition.key}`}
								value={overrides[definition.key] ?? ""}
								{disabled}
								onchange={(event) =>
									onOverrideChange(
										definition.key,
										event.currentTarget.value === "" ? null : (event.currentTarget.value as ArchivePermissionEffect)
									)}
							>
								<option value="">Use role</option><option value={ArchivePermissionEffect.ALLOW}>Allow</option><option
									value={ArchivePermissionEffect.DENY}>Deny</option
								>
							</select>
							{#if changed}<span class="draft-marker">Unsaved change</span>{/if}
						</TableCell>
					</TableRow>
				{:else}
					<TableRow
						><TableCell colspan={3}>
							<div class="permission-empty">
								<SearchX aria-hidden="true" />
								<h3>No permissions found</h3>
								<p>Try another name or permission key.</p>
								<Button href={resetHref} variant="text">Show all permissions</Button>
							</div>
						</TableCell></TableRow
					>
				{/each}
			</TableBody>
		</TableRoot>
	</TableViewport>
</div>

<style>
	.permission-register {
		min-width: 0;
	}
	.permission-register :global(.permission-table) {
		min-width: 44rem;
		table-layout: fixed;
	}
	.permission-register :global([data-table-header]) {
		border-block: 1px solid var(--border-trace);
	}
	.permission-register :global([data-table-header] [data-table-head]) {
		color: var(--marginal-note);
		font-size: 0.8125rem;
	}
	.permission-register :global(.access-column) {
		width: 10rem;
	}
	.permission-register :global(.override-column) {
		width: 12rem;
	}
	.permission-register :global([data-table-body] [data-table-head]),
	.permission-register :global([data-table-cell]) {
		padding-block: 1.5rem;
		vertical-align: top;
	}
	.permission-description label {
		display: block;
		font-size: 1rem;
		font-weight: 600;
		line-height: 1.4;
	}
	.permission-description code {
		display: block;
		margin-top: 0.375rem;
		font: 400 0.75rem / 1.5 var(--font-record);
		color: var(--marginal-note);
		overflow-wrap: anywhere;
	}
	.permission-description p {
		margin: 0.75rem 0 0;
		max-width: 64ch;
		color: var(--marginal-note);
		font-size: 0.875rem;
		font-weight: 400;
		line-height: 1.5;
	}
	.saved-access {
		display: grid;
		gap: 0.375rem;
		padding-block: 0.75rem;
	}
	.saved-access strong {
		display: flex;
		align-items: center;
		gap: 0.375rem;
		font-size: 0.875rem;
		font-weight: 600;
	}
	.saved-access strong :global(svg) {
		width: 1rem;
		height: 1rem;
		flex: none;
	}
	.saved-access[data-allowed="true"] strong {
		color: var(--preservation-green);
	}
	.saved-access > span {
		font-size: 0.75rem;
		color: var(--marginal-note);
	}
	select {
		display: block;
		width: 100%;
		min-height: 3rem;
		border: 1px solid var(--border-trace);
		border-radius: var(--radius-control);
		padding: 0.625rem 0.75rem;
		background: var(--reading-room);
		color: var(--reading-ink);
		font: inherit;
		scroll-margin-block: 8rem;
	}
	select:disabled {
		opacity: 0.62;
		cursor: not-allowed;
	}
	.draft-marker {
		display: block;
		margin-top: 0.5rem;
		color: var(--register-mark);
		font-size: 0.75rem;
	}
	.permission-empty {
		display: grid;
		justify-items: start;
		gap: 0.75rem;
		padding-block: 1.5rem;
	}
	.permission-empty > :global(svg) {
		width: 1.5rem;
		height: 1.5rem;
		color: var(--marginal-note);
	}
	.permission-empty h3 {
		margin: 0;
		font-size: 1.125rem;
		font-weight: 600;
	}
	.permission-empty p {
		margin: 0;
		color: var(--marginal-note);
		font-size: 0.9375rem;
	}
	.scroll-hint {
		display: none;
	}
	@media (max-width: 52rem) {
		.scroll-hint {
			display: flex;
			align-items: center;
			gap: 0.5rem;
			margin: 0 0 0.5rem;
			font-size: 0.8125rem;
			color: var(--marginal-note);
		}
		.scroll-hint :global(svg) {
			width: 1rem;
			height: 1rem;
			flex: none;
		}
	}
</style>
