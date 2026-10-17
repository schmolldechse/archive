<script lang="ts">
	import { ArchiveRole } from "$api";
	import ShieldCheck from "@lucide/svelte/icons/shield-check";
	import UserRound from "@lucide/svelte/icons/user-round";

	let {
		role,
		disabled,
		onRoleChange
	}: {
		role: ArchiveRole;
		disabled: boolean;
		onRoleChange: (role: ArchiveRole) => void;
	} = $props();
	const roles = [
		{ value: ArchiveRole.USER, label: "User", description: "No administrative access by default.", icon: UserRound },
		{ value: ArchiveRole.ADMIN, label: "Admin", description: "Administrative access by default.", icon: ShieldCheck }
	];
</script>

<!-- Native radios expose the two exclusive roles without requiring a menu or custom keyboard handling. -->
<fieldset class="role-assignment" {disabled}>
	<legend>Assigned role <span class="field-qualifier">Required</span></legend>
	<div class="role-options">
		{#each roles as option (option.value)}
			<label class="role-option" data-selected={role === option.value}>
				<input
					type="radio"
					name="account-role"
					value={option.value}
					checked={role === option.value}
					required
					aria-labelledby={`role-${option.value}-label`}
					aria-describedby={`role-${option.value}-description`}
					onchange={() => onRoleChange(option.value)}
				/>
				<span class="role-copy">
					<span id={`role-${option.value}-label`} class="role-name"><option.icon aria-hidden="true" />{option.label}</span>
					<span id={`role-${option.value}-description`} class="role-description">{option.description}</span>
				</span>
			</label>
		{/each}
	</div>
</fieldset>

<style>
	.role-assignment {
		min-width: 0;
		margin: 0;
		border: 0;
		padding: 0;
	}
	legend {
		margin-bottom: 0.75rem;
		padding: 0;
		font-size: 0.9375rem;
		font-weight: 600;
		line-height: 1.4;
	}
	.role-options {
		display: grid;
		grid-template-columns: repeat(2, minmax(0, 1fr));
		gap: 0.75rem;
	}
	.role-option {
		display: flex;
		align-items: start;
		gap: 0.75rem;
		min-width: 0;
		/* Fit the content while preserving the minimum pointer target. */
		min-height: 2.75rem;
		padding: 0.75rem 1rem;
		border: 1px solid var(--border-trace);
		border-radius: var(--radius-control);
		background: var(--reading-room);
		cursor: pointer;
	}
	.role-option[data-selected="true"] {
		border-color: var(--register-mark);
		background: color-mix(in srgb, var(--register-mark) 5%, var(--reading-room));
	}
	.role-option:hover {
		border-color: var(--reading-ink);
	}
	input {
		flex: none;
		width: 1.125rem;
		height: 1.125rem;
		margin: 0.1875rem 0 0;
		accent-color: var(--register-mark);
		scroll-margin-block: 8rem;
	}
	.role-copy {
		display: grid;
		min-width: 0;
		gap: 0.375rem;
	}
	.role-name {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		font-size: 1rem;
		font-weight: 600;
	}
	.role-name :global(svg) {
		flex: none;
		width: 1.125rem;
		height: 1.125rem;
		color: var(--marginal-note);
	}
	.role-description {
		color: var(--marginal-note);
		font-size: 0.875rem;
		line-height: 1.4;
	}
	.role-assignment:disabled .role-option {
		cursor: not-allowed;
	}
	.role-assignment:disabled .role-option:not([data-selected="true"]) {
		opacity: 0.62;
	}
	@media (max-width: 32rem) {
		.role-options {
			grid-template-columns: minmax(0, 1fr);
		}
	}
</style>
