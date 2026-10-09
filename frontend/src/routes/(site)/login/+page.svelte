<script lang="ts">
	import { page } from "$app/state";
	import { getAuthenticationContext } from "$lib/authentication/context.svelte";
	import Button from "$lib/components/ui/Button.svelte";
	import "$lib/authentication/pages.css";
	const authentication = getAuthenticationContext();
	const returnUrl = $derived(page.url.searchParams.get("returnUrl") || "/account");
	const failed = $derived(page.url.searchParams.has("error"));
</script>

<svelte:head><title>Sign in — Archive</title><meta name="robots" content="noindex" /></svelte:head>

<main id="main-content" class="account-page">
	<p class="account-kicker">Your account</p>
	<h1>Sign in to the archive</h1>
	<p>You can browse and archive pages without an account. Signing in gives you a profile of your own.</p>
	{#if failed}<p class="account-error" role="alert">
			Sign-in could not be completed. You can try again or continue browsing.
		</p>{/if}
	{#if authentication.status === "loading"}
		<p role="status">Loading sign-in options…</p>
	{:else if authentication.status === "unavailable"}
		<p class="account-error" role="alert">The sign-in service is temporarily unavailable.</p>
		<Button onclick={() => authentication.refresh()}>Try again</Button>
	{:else if authentication.user}
		<p>You are signed in as {authentication.user.displayName}.</p>
		<Button href="/account" variant="primary">Your profile</Button>
	{:else if authentication.providers.length}
		<div class="account-actions">
			{#each authentication.providers as provider (provider.key)}
				<form method="post" action={provider.loginUrl} data-sveltekit-reload>
					<input type="hidden" name="returnUrl" value={returnUrl} />
					<Button type="submit" variant="primary">Continue with {provider.displayName}</Button>
				</form>
			{/each}
		</div>
	{:else}
		<p>Sign-in is not available yet. The archive is still open to everyone.</p>
	{/if}
	<p class="account-back"><a href="/">Continue browsing</a></p>
</main>
