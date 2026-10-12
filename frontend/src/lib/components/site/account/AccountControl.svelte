<script lang="ts">
	import { page } from "$app/state";
	import { invalidate } from "$app/navigation";
	import { getAuthContext } from "$lib/auth/context";
	import { loginHref } from "$lib/auth/return-url";
	import AccountDialog from "./AccountDialog.svelte";
	let { onopen }: { onopen?: () => void } = $props();
	const account = getAuthContext();
</script>

<div class="account-control" data-component="account-control">
	{#if account.session === null}
		<button class="session-retry" type="button" onclick={() => invalidate("app:auth")}>Retry account</button>
	{:else if account.session.user}
		<AccountDialog user={account.session.user} {onopen} />
	{:else}
		<a class="sign-in-link" href={loginHref(page.url.pathname + page.url.search)} onclick={onopen}>Sign in</a>
	{/if}
</div>

<style>
	.account-control {
		display: flex;
		min-width: 2.75rem;
		min-height: 2.75rem;
		align-items: center;
		justify-content: center;
	}
	.sign-in-link,
	.session-retry {
		display: inline-flex;
		min-height: 2.75rem;
		align-items: center;
		border: 1px solid transparent;
		border-radius: var(--radius-control);
		background: transparent;
		padding-inline: 0.75rem;
		color: var(--reading-ink);
		font: 600 0.9375rem var(--font-interface);
		text-decoration: none;
		cursor: pointer;
		transition:
			border-color 150ms ease,
			background-color 150ms ease;
	}
	.sign-in-link:hover,
	.session-retry:hover {
		border-color: var(--border-trace);
		background: var(--archive-layer);
	}
	.session-retry {
		color: var(--marginal-note);
	}
</style>
