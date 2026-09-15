<script lang="ts">
	import {
		SelectContent,
		SelectError,
		SelectItem,
		SelectLabel,
		SelectRoot,
		SelectTrigger,
		SelectValue,
		SelectViewport
	} from "$lib/components/ui/select";
	interface Props {
		label: string;
		value: string | null;
		placeholder?: string;
		options: { value: string; label: string }[];
		error?: string;
		disabled?: boolean;
		onChange: (value: string | null) => void;
	}
	let { label, value, placeholder = "Choose an option", options, error, disabled = false, onChange }: Props = $props();
</script>

<SelectRoot type="single" {value} invalid={Boolean(error)} {disabled} onValueChange={onChange} data-component="search-select">
	<SelectLabel>{label}</SelectLabel>
	<SelectTrigger
		><SelectValue {placeholder}>{options.find((option) => option.value === value)?.label ?? placeholder}</SelectValue
		></SelectTrigger
	>
	<SelectContent
		><SelectViewport>
			{#each options as option (option.value)}
				<SelectItem value={option.value} textValue={option.label}>{option.label}</SelectItem>
			{/each}
		</SelectViewport></SelectContent
	>
	{#if error}<SelectError>{error}</SelectError>{/if}
</SelectRoot>
