<script lang="ts">
	import X from "@lucide/svelte/icons/x";
	import Tag from "@lucide/svelte/icons/tag";
	import Badge from "$lib/components/ui/Badge.svelte";
	import { InputControl, InputDescription, InputError, InputLabel, InputRoot } from "$lib/components/ui/input";
	import { normalizeTags } from "./tags";

	let {
		id,
		label,
		listLabel,
		description,
		requirement,
		tags,
		error,
		onChange
	}: {
		id: string;
		label: string;
		listLabel: string;
		description?: string;
		requirement?: "optional" | "required";
		tags: string[];
		error?: string;
		onChange: (tags: string[]) => void;
	} = $props();

	let input = $state("");

	function commit(values: string[]): void {
		const nextTags = normalizeTags([...tags, ...values]);
		if (nextTags.length !== tags.length || nextTags.some((tag, index) => tag !== tags[index])) onChange(nextTags);
	}

	function commitInput(): void {
		if (input.trim()) commit([input]);
		input = "";
	}

	function handleInput(event: Event): void {
		const value = (event.currentTarget as HTMLInputElement).value;
		const parts = value.split(",");

		if (parts.length === 1) {
			input = value;
			return;
		}

		input = parts.pop() ?? "";
		commit(parts);
	}

	function handleKeydown(event: KeyboardEvent): void {
		if (event.key === "Enter" || event.key === ",") {
			event.preventDefault();
			commitInput();
			return;
		}

		if (event.key === "Backspace" && !input && tags.length) {
			event.preventDefault();
			onChange(tags.slice(0, -1));
		}
	}
</script>

<InputRoot {id} invalid={Boolean(error)} hasDescription={Boolean(description)} class="tag-editor">
	<InputLabel
		>{label}
		{#if requirement}<span class="field-qualifier">{requirement === "required" ? "Required" : "Optional"}</span
			>{/if}</InputLabel
	>
	<InputControl
		name="tagInput"
		value={input}
		autocomplete="off"
		placeholder="e.g. local history"
		oninput={handleInput}
		onkeydown={handleKeydown}
		onblur={commitInput}
	/>
	{#if tags.length}
		<ul class="tag-list" aria-label={listLabel}>
			{#each tags as tag (tag)}
				<li>
					<Badge variant="info" class="tag-badge">
						{#snippet icon()}<Tag />{/snippet}
						<span class="tag-value">{tag}</span>
						<button
							type="button"
							class="remove-tag"
							aria-label={`Remove tag ${tag}`}
							onclick={() => onChange(tags.filter((candidate) => candidate !== tag))}
						>
							<X aria-hidden="true" />
						</button>
					</Badge>
				</li>
			{/each}
		</ul>
	{/if}
	{#if description}<InputDescription>{description}</InputDescription>{/if}
	{#if error}<InputError>{error}</InputError>{/if}
</InputRoot>

<style>
	.tag-list {
		display: flex;
		flex-wrap: wrap;
		gap: 0.5rem;
		margin: 0;
		padding: 0;
		list-style: none;
	}

	.tag-list li {
		display: flex;
		min-width: 0;
	}

	:global(.tag-badge) {
		padding-inline-end: 0.1875rem;
	}

	:global(.tag-badge .badge__label) {
		display: inline-flex;
		min-width: 0;
		align-items: center;
		gap: 0.25rem;
		line-height: 1.25rem;
	}

	.tag-value {
		min-width: 0;
		overflow-wrap: anywhere;
	}

	.remove-tag {
		position: relative;
		display: inline-flex;
		width: 1.25rem;
		height: 1.25rem;
		flex: none;
		align-items: center;
		justify-content: center;
		appearance: none;
		border: 0;
		border-radius: 999px;
		background: transparent;
		color: currentColor;
		cursor: pointer;
		transition: background-color 150ms ease;
	}

	.remove-tag::before {
		position: absolute;
		inset: -0.25rem;
		content: "";
	}

	.remove-tag:hover {
		background: color-mix(in srgb, var(--register-mark) 14%, transparent);
	}

	.remove-tag:focus-visible {
		outline: 2px solid var(--register-mark);
		outline-offset: 1px;
	}

	.remove-tag :global(svg) {
		width: 1rem;
		height: 1rem;
		stroke-width: 1.75;
	}

	@media (prefers-reduced-motion: reduce) {
		.remove-tag {
			transition: none;
		}
	}
</style>
