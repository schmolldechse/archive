<script lang="ts">
	import type { HTMLInputAttributes } from "svelte/elements";
	/** Internal native checkbox shared by the header and individual rows. */
	interface Props extends Omit<HTMLInputAttributes, "type" | "children" | "aria-label" | "checked" | "indeterminate"> {
		label: string;
		checked: boolean;
		indeterminate?: boolean;
	}
	let { label, checked, indeterminate = false, class: className, ...restProps }: Props = $props();
</script>

<label class={["table-selection", className]} data-table-selection>
	<input {...restProps} type="checkbox" {checked} {indeterminate} aria-label={label} />
</label>

<style>
	.table-selection {
		display: inline-flex;
		min-inline-size: 2.75rem;
		min-block-size: 2.75rem;
		align-items: center;
		justify-content: center;
		cursor: pointer;
	}
	.table-selection:has(:disabled) {
		cursor: not-allowed;
	}
	input {
		inline-size: 1.125rem;
		block-size: 1.125rem;
		margin: 0;
		accent-color: var(--register-mark);
		cursor: inherit;
	}
	input:disabled {
		opacity: 0.55;
	}
</style>
