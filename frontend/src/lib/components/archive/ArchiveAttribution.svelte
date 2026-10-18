<script lang="ts">
	import type { PublicUserResponse } from "$api";
	import Avatar from "$lib/components/ui/Avatar.svelte";

	let { creator }: { creator: PublicUserResponse | null | undefined } = $props();
	const initials = $derived(
		creator?.displayName
			.trim()
			.split(/\s+/u)
			.slice(0, 2)
			.map((part) => Array.from(part)[0] ?? "")
			.join("")
			.toUpperCase()
	);
</script>

<div class="archive-attribution" data-component="archive-attribution" data-state={creator ? "attributed" : "unattributed"}>
	<p class="archive-attribution__label">Submitted by</p>
	{#if creator}
		<div class="archive-attribution__identity">
			<Avatar src={creator.profileImageUrl} {initials} decorative />
			<div class="archive-attribution__text">
				<p class="archive-attribution__name">{creator.displayName}</p>
				<p class="archive-attribution__handle">@{creator.handle}</p>
			</div>
		</div>
	{:else}
		<p class="archive-attribution__unknown">No submitter recorded</p>
	{/if}
</div>

<style>
	.archive-attribution {
		min-width: 0;
		font-family: var(--font-interface);
		line-height: 1.5;
		overflow-wrap: anywhere;
	}
	.archive-attribution p {
		margin: 0;
	}
	.archive-attribution .archive-attribution__label {
		margin-bottom: 0.5rem;
		color: var(--marginal-note);
		font-size: 0.75rem;
		font-weight: 600;
		letter-spacing: 0.08em;
		line-height: 1rem;
		text-transform: uppercase;
	}
	.archive-attribution__identity {
		display: flex;
		min-height: 2.75rem;
		align-items: center;
		gap: 0.75rem;
	}
	.archive-attribution__text {
		min-width: 0;
	}
	.archive-attribution__name {
		color: var(--reading-ink);
		font-size: 1.125rem;
		font-weight: 600;
		line-height: 1.5rem;
	}
	.archive-attribution__handle {
		color: var(--marginal-note);
		font-size: 0.8125rem;
		line-height: 1.25rem;
	}
	.archive-attribution__unknown {
		display: flex;
		min-height: 2.75rem;
		align-items: center;
		color: var(--marginal-note);
		font-size: 0.875rem;
		line-height: 1.25rem;
	}
</style>
