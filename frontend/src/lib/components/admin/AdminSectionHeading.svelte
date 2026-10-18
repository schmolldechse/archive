<script lang="ts">
	type AdminSection = "users" | "snapshots";

	let { title, description, section }: { title: string; description: string; section: AdminSection } = $props();
</script>

<header class="section-heading">
	<div>
		<h1>{title}</h1>
		<p>{description}</p>
	</div>
	<!-- Register lines distinguish each directory without competing with its records. -->
	{#if section === "users"}
		<svg class="section-mark" viewBox="0 0 176 104" fill="none" aria-hidden="true" focusable="false">
			<g stroke="currentColor" stroke-width="1">
				<path d="M24 8v88M16 16h144M16 48h144M16 80h144" />
				<circle cx="44" cy="32" r="4" /><circle cx="44" cy="64" r="4" /><circle cx="44" cy="96" r="4" />
				<path d="M60 28h72M60 36h48M60 60h56M60 68h80M60 92h72M60 100h40" />
			</g>
			<path class="section-mark-accent" d="M16 48h16" stroke-width="3" />
		</svg>
	{:else}
		<svg class="section-mark" viewBox="0 0 176 104" fill="none" aria-hidden="true" focusable="false">
			<g stroke="currentColor" stroke-width="1">
				<path d="M16 16h144M16 48h144M16 80h144" />
				<path d="M32 8v88M88 8v88M144 8v88" />
				<rect x="40" y="24" width="40" height="16" rx="2" />
				<rect x="96" y="56" width="40" height="16" rx="2" />
				<circle cx="52" cy="32" r="3" /><path d="m62 36 6-7 8 7" />
				<circle cx="108" cy="64" r="3" /><path d="m118 68 6-7 8 7" />
			</g>
			<path class="section-mark-accent" d="M88 48h56" stroke-width="3" />
		</svg>
	{/if}
</header>

<style>
	.section-heading {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 2rem;
		margin-block: 2.5rem 2rem;
	}
	h1 {
		margin: 0;
		font-size: clamp(2.5rem, 4.5vw, 3.5rem);
		font-weight: 500;
		line-height: 1.12;
		letter-spacing: -0.025em;
		overflow-wrap: anywhere;
	}
	p {
		max-width: 42rem;
		margin: 0.5rem 0 0;
		color: var(--marginal-note);
	}
	.section-mark {
		width: 10rem;
		height: auto;
		flex: none;
		color: var(--border-trace);
	}
	.section-mark-accent {
		stroke: var(--register-mark);
	}
	@media (max-width: 42rem) {
		.section-heading {
			margin-block: 2rem 1.5rem;
		}
		.section-mark {
			display: none;
		}
	}
</style>
