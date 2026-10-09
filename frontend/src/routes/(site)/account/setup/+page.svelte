<script lang="ts">
	import { getAuthenticationContext } from "$lib/authentication/context.svelte";
	import ProfileEditor from "$lib/components/authentication/ProfileEditor.svelte";
	import Button from "$lib/components/ui/Button.svelte";
	import "$lib/authentication/pages.css";
	const authentication = getAuthenticationContext();
</script>

<svelte:head><title>Set up your profile — Archive</title><meta name="robots" content="noindex" /></svelte:head>

<main id="main-content" class="account-page">
	<p class="account-kicker">Welcome to Archive</p>
	<h1>Set up your profile</h1>
	{#if authentication.status === "loading"}
		<p role="status">Loading your profile…</p>
	{:else if authentication.status === "unavailable"}
		<p class="account-error" role="alert">Your profile is temporarily unavailable.</p>
		<Button onclick={() => authentication.refresh()}>Try again</Button>
	{:else if authentication.user}
		<p>Choose your display name and a unique handle, then save your profile. You can change both later.</p>
		<p>Your profile image comes from your sign-in provider. If no image is available, your initials appear instead.</p>
		{#key authentication.user.id}<ProfileEditor user={authentication.user} />{/key}
		<div class="account-section">
			<Button href="/" variant="primary" disabled={!authentication.user.handle}>Continue to the archive</Button>
			{#if !authentication.user.handle}<p>Save your display name and handle to finish setting up your profile.</p>{/if}
		</div>
	{:else}
		<p>Sign in to set up your profile.</p>
		<Button href="/login?returnUrl=%2Faccount%2Fsetup" variant="primary">Sign in</Button>
	{/if}
</main>
