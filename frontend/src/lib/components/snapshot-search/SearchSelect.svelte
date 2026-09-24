<script lang="ts">
	import {
		SelectContent,
		SelectDescription,
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
		description?: string;
		requirement?: "optional" | "required";
		value: string | null;
		class?: string;
		placeholder?: string;
		options: { value: string; label: string }[];
		error?: string;
		disabled?: boolean;
		onChange: (value: string | null) => void;
	}
	let {
		label,
		description,
		requirement = "optional",
		value,
		class: className,
		placeholder = "Choose an option",
		options,
		error,
		disabled = false,
		onChange
	}: Props = $props();
</script>

<SelectRoot
	type="single"
	{value}
	invalid={Boolean(error)}
	{disabled}
	class={className}
	onValueChange={onChange}
	data-component="search-select"
>
	<SelectLabel>{label} <span class="field-qualifier">{requirement === "required" ? "Required" : "Optional"}</span></SelectLabel>
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
	{#if description}<SelectDescription>{description}</SelectDescription>{/if}
	{#if error}<SelectError>{error}</SelectError>{/if}
</SelectRoot>
