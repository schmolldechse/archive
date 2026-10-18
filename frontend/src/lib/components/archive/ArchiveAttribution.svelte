<script lang="ts">
	import type { PublicUserResponse } from "$api";
	import Avatar from "$lib/components/ui/Avatar.svelte";

	let {
		creator,
		layout = "default",
		label = "Submitted by"
	}: { creator: PublicUserResponse | null | undefined; layout?: "default" | "compact"; label?: string | null } = $props();
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

<div
	class="archive-attribution"
	data-component="archive-attribution"
	data-state={creator ? "attributed" : "anonymous"}
	data-layout={layout}
>
	{#if label}<p class="archive-attribution__label">{label}</p>{/if}
	{#if creator}
		<div class="archive-attribution__identity">
			<Avatar src={creator.profileImageUrl} {initials} decorative />
			<div class="archive-attribution__text">
				<p class="archive-attribution__name">{creator.displayName}</p>
				<p class="archive-attribution__handle">@{creator.handle}</p>
			</div>
		</div>
	{:else}
		<div class="archive-attribution__identity">
			<Avatar decorative size={layout === "compact" ? "compact" : "default"} />
			<div class="archive-attribution__text">
				<p class="archive-attribution__name">Anonymous</p>
				<p class="archive-attribution__handle">No account attached</p>
			</div>
		</div>
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
	.archive-attribution[data-layout="compact"] .archive-attribution__label {
		margin-bottom: 0.25rem;
	}
	.archive-attribution[data-layout="compact"] .archive-attribution__identity {
		min-height: 2rem;
		gap: 0.625rem;
	}
	.archive-attribution[data-layout="compact"] .archive-attribution__name {
		font-size: 0.9375rem;
		line-height: 1.125rem;
	}
	.archive-attribution[data-layout="compact"] .archive-attribution__handle {
		font-size: 0.75rem;
		line-height: 1rem;
	}
</style>
