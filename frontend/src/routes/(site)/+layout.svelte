<script lang="ts">
	import { apiUrl } from "$lib/api-url";
	import SiteFooter from "$lib/components/site/SiteFooter.svelte";
	import SiteHeader from "$lib/components/site/SiteHeader.svelte";
	import type { LayoutProps } from "./$types";
	import { setAuthContext } from "$lib/auth/context";

	let { children, data }: LayoutProps = $props();

	setAuthContext({
		get session() {
			return data.session;
		}
	});

	const healthUrl = apiUrl("/healthz");
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
