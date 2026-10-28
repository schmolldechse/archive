<script lang="ts">
	let {
		id = "snapshot-private",
		checked = $bindable(false),
		disabled = false
	}: {
		id?: string;
		checked?: boolean;
		disabled?: boolean;
	} = $props();
</script>

<div class="snapshot-visibility" data-component="snapshot-visibility-field" data-private={checked}>
	<input {id} type="checkbox" bind:checked {disabled} />
	<label for={id}>
		<strong>Keep this snapshot private</strong>
		<span>Only you and authorized archive staff can open it. It won’t appear in the public register or search.</span>
	</label>
</div>

<style>
	.snapshot-visibility {
		display: grid;
		grid-template-columns: auto minmax(0, 1fr);
		align-items: start;
		gap: 0.75rem;
		border-block: 1px solid var(--border-trace);
		padding-block: 1rem;
	}
	input {
		width: 1.25rem;
		height: 1.25rem;
		margin: 0.125rem 0 0;
		accent-color: var(--register-mark);
	}
	input:focus-visible {
		outline: 3px solid var(--register-mark);
		outline-offset: 2px;
	}
	label {
		display: grid;
		min-height: 44px;
		cursor: pointer;
	}
	label strong {
		font-weight: 600;
	}
	label span {
		color: var(--marginal-note);
		font-size: 0.875rem;
		line-height: 1.43;
	}
	input:disabled + label {
		cursor: not-allowed;
		opacity: 0.65;
	}
</style>
