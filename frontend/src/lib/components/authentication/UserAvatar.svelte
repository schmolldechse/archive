<script lang="ts">
	import type { UserResponse } from "$lib/api";
	let { user, size = "small" }: { user: UserResponse; size?: "small" | "large" } = $props();
	let failedUrl = $state<string | null>(null);
	const initials = $derived.by(() => {
		const words = user.displayName.trim().split(/\s+/).filter(Boolean);
		const segments = new Intl.Segmenter(undefined, { granularity: "grapheme" });
		const first = (word: string) => Array.from(segments.segment(word))[0]?.segment || "";
		return (
			words.length > 1 ? first(words[0]) + first(words[words.length - 1]) : first(words[0] || user.handle)
		).toLocaleUpperCase();
	});
</script>

<span class="avatar" data-size={size} aria-hidden="true">
	{#if user.profileImageUrl && failedUrl !== user.profileImageUrl}
		<img
			src={user.profileImageUrl}
			alt=""
			referrerpolicy="no-referrer"
			onerror={() => {
				failedUrl = user.profileImageUrl;
			}}
		/>
	{:else}
		<span>{initials}</span>
	{/if}
</span>

<style>
	.avatar {
		display: inline-flex;
		flex: none;
		width: 2rem;
		height: 2rem;
		align-items: center;
		justify-content: center;
		overflow: hidden;
		border: 1px solid var(--border-trace);
		border-radius: var(--radius-control);
		background: var(--archive-layer);
		color: var(--reading-ink);
		font: 600 0.875rem var(--font-interface);
	}
	.avatar[data-size="large"] {
		width: 5rem;
		height: 5rem;
		font-size: 1.75rem;
	}
	img {
		width: 100%;
		height: 100%;
		object-fit: cover;
	}
</style>
