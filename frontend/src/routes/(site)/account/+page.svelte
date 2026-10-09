<script lang="ts">
	import { getAuthenticationContext } from "$lib/authentication/context.svelte";
	import ProfileEditor from "$lib/components/authentication/ProfileEditor.svelte";
	import Button from "$lib/components/ui/Button.svelte";
	import "$lib/authentication/pages.css";
	const authentication = getAuthenticationContext();
</script>

<svelte:head><title>Your profile — Archive</title><meta name="robots" content="noindex" /></svelte:head>

<main id="main-content" class="account-page">
	<p class="account-kicker">Your account</p>
	<h1>Your profile</h1>
	{#if authentication.status === "loading"}
		<p role="status">Loading your profile…</p>
	{:else if authentication.status === "unavailable"}
		<p class="account-error" role="alert">Your profile is temporarily unavailable.</p>
		<Button onclick={() => authentication.refresh()}>Try again</Button>
	{:else if authentication.user}
		{#if authentication.user.handle}
			<p>Signed in as @{authentication.user.handle}.</p>
		{:else}
			<p>Your profile setup is still incomplete.</p>
			<Button href="/account/setup">Set up your profile</Button>
		{/if}
		{#key authentication.user.id}<ProfileEditor user={authentication.user} />{/key}
		<div class="account-section"><Button onclick={() => authentication.signOut()}>Sign out</Button></div>
	{:else}
		<p>Sign in to view and edit your profile.</p>
		<Button href="/login?returnUrl=%2Faccount" variant="primary">Sign in</Button>
	{/if}
</main>
