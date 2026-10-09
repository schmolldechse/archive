<script lang="ts">
	import "../app.css";
	import { onMount } from "svelte";
	import { setAuthenticationContext } from "$lib/authentication/context.svelte";
	import type { LayoutProps } from "./$types";

	let { children }: LayoutProps = $props();
	const authentication = setAuthenticationContext();
	onMount(() => {
		void authentication.refresh();
		const refresh = () => {
			void authentication.refresh();
		};
		window.addEventListener("focus", refresh);
		return () => window.removeEventListener("focus", refresh);
	});
</script>

{@render children()}
