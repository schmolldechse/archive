<script lang="ts">
	import Camera from "@lucide/svelte/icons/camera";
	import UsersRound from "@lucide/svelte/icons/users-round";

	type AdminSection = { key: "users" | "snapshots"; label: string; href: string };
	let { sections, pathname }: { sections: readonly AdminSection[]; pathname: string } = $props();
</script>

{#if sections.length > 1}
	<nav class="admin-sections" aria-label="Administration sections">
		{#each sections as section (section.key)}
			<a
				href={section.href}
				aria-current={pathname === section.href ? "page" : pathname.startsWith(`${section.href}/`) ? "location" : undefined}
			>
				{#if section.key === "users"}<UsersRound aria-hidden="true" />{:else}<Camera aria-hidden="true" />{/if}
				<span>{section.label}</span>
			</a>
		{/each}
	</nav>
{/if}

<style>
	.admin-sections {
		display: flex;
		gap: 0.25rem;
		border-bottom: 1px solid var(--border-trace);
	}
	.admin-sections a {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		min-height: 3.25rem;
		padding: 0.75rem 1rem;
		color: var(--marginal-note);
		border-bottom: 2px solid transparent;
		text-decoration: none;
		font-size: 0.9375rem;
	}
	.admin-sections a:hover {
		color: var(--reading-ink);
		text-decoration: underline;
	}
	.admin-sections a[aria-current] {
		color: var(--register-mark);
		border-bottom-color: var(--register-mark);
		background: color-mix(in srgb, var(--register-mark) 7%, transparent);
		font-weight: 600;
	}
	.admin-sections a :global(svg) {
		width: 1.125rem;
		height: 1.125rem;
		flex: none;
		stroke-width: 1.75;
	}
	@media (max-width: 24rem) {
		.admin-sections a {
			flex: 1;
			justify-content: center;
			padding-inline: 0.5rem;
		}
	}
</style>
