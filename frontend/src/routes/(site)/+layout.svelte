<script lang="ts">
	import { env } from "$env/dynamic/public";
	import SiteFooter from "$lib/components/site/SiteFooter.svelte";
	import SiteHeader from "$lib/components/site/SiteHeader.svelte";
	import type { LayoutProps } from "./$types";

	let { children }: LayoutProps = $props();
	const healthUrl = `${(env.PUBLIC_API_BASE_URL || "http://localhost:5200").replace(/\/$/, "")}/healthz`;
</script>

<a class="skip-link" href="#main-content">Skip to the archive</a>
<SiteHeader />
{@render children()}
<SiteFooter {healthUrl} />

<style>
	.skip-link {
		position: fixed;
		z-index: 100;
		top: 0.75rem;
		left: 0.75rem;
		transform: translateY(-160%);
		border-radius: var(--radius-control);
		background: var(--register-mark);
		padding: 0.625rem 0.875rem;
		color: var(--reading-room);
		font-weight: 600;
	}

	.skip-link:focus {
		transform: translateY(0);
	}
</style>
