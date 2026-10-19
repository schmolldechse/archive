<script lang="ts">
	import ArrowRight from "@lucide/svelte/icons/arrow-right";
	import KeyRound from "@lucide/svelte/icons/key-round";
	import { apiUrl } from "$lib/api-url";
	import { loginHref, withReturnUrl } from "$lib/auth/return-url";
	import LoginNotice from "./LoginNotice.svelte";
	import type { PageProps } from "./$types";

	let { data }: PageProps = $props();
</script>

<svelte:head>
	<title>Sign in · Archive</title>
	<meta name="robots" content="noindex, nofollow" />
	<meta name="description" content="Sign in to your archive account. Browsing and archiving remain open to everyone." />
</svelte:head>

<main id="main-content" class="login-page">
	<div class="login-content">
		<header>
			<p class="section-label">Account</p>
			<h1>Sign in.</h1>
			<p class="introduction">Choose a provider to continue.</p>
		</header>

		<section class="sign-in-options" aria-label="Sign-in providers">
			{#if !data.providers.length}
				<div class="provider-unavailable">
					<p>Sign-in is currently unavailable.</p>
					<a href={loginHref(data.returnUrl)} data-sveltekit-reload>Try again <ArrowRight size={18} aria-hidden="true" /></a>
				</div>
			{:else}
				<div class="providers">
					{#each data.providers as provider (provider.key)}
						<form
							method="POST"
							action={apiUrl(withReturnUrl(`/api/auth/login/${encodeURIComponent(provider.key)}`, data.returnUrl))}
						>
							<button class="provider-button" type="submit">
								{#if provider.key === "google"}
									<svg width="20" height="20" viewBox="0 0 48 48" aria-hidden="true">
										<path
											fill="#EA4335"
											d="M24 9.5c3.54 0 6.71 1.22 9.21 3.6l6.85-6.85C35.9 2.38 30.47 0 24 0 14.62 0 6.51 5.38 2.56 13.22l7.98 6.19C12.43 13.72 17.74 9.5 24 9.5Z"
										/>
										<path
											fill="#4285F4"
											d="M46.98 24.55c0-1.57-.15-3.09-.38-4.55H24v9.02h12.94c-.58 2.96-2.26 5.48-4.78 7.18l7.73 6c4.51-4.18 7.09-10.36 7.09-17.65Z"
										/>
										<path
											fill="#FBBC05"
											d="M10.53 28.59A14.41 14.41 0 0 1 9.75 24c0-1.59.27-3.13.75-4.59l-7.98-6.19A23.87 23.87 0 0 0 0 24c0 3.87.93 7.53 2.56 10.78l7.97-6.19Z"
										/>
										<path
											fill="#34A853"
											d="M24 48c6.48 0 11.93-2.13 15.91-5.8l-7.73-6c-2.15 1.45-4.92 2.3-8.18 2.3-6.26 0-11.57-4.22-13.47-9.91l-7.98 6.19C6.51 42.62 14.62 48 24 48Z"
										/>
									</svg>
								{:else}
									<KeyRound size={20} strokeWidth={1.75} aria-hidden="true" />
								{/if}
								<span>Continue with {provider.displayName}</span>
								<ArrowRight class="provider-arrow" size={18} aria-hidden="true" />
							</button>
						</form>
					{/each}
				</div>
			{/if}
			<p class="registration-note">On your first visit, you’ll choose a name and handle.</p>
		</section>

		<div class="guest-access">
			<a href={data.returnUrl}>Continue without signing in <ArrowRight size={18} aria-hidden="true" /></a>
			<p>You can browse and archive pages without an account.</p>
		</div>
	</div>
</main>

{#if data.notice}
	{#key data.notice}
		<LoginNotice notice={data.notice} />
	{/key}
{/if}

<style>
	.login-page {
		display: grid;
		min-height: calc(100svh - 5.75rem);
		align-content: center;
		padding: 4rem 1.5rem 6rem;
	}
	.login-content {
		width: min(100%, 28rem);
		margin-inline: auto;
	}
	.section-label {
		margin: 0 0 1rem;
		color: var(--marginal-note);
		font-size: 0.75rem;
		font-weight: 600;
		letter-spacing: 0.08em;
		text-transform: uppercase;
	}
	h1 {
		margin: 0 0 0.75rem;
		font: 500 3rem / 1.08 var(--font-editorial);
		letter-spacing: -0.02em;
	}
	.introduction {
		margin: 0;
		color: var(--marginal-note);
	}
	.sign-in-options {
		margin-top: 2rem;
	}
	.providers {
		display: grid;
		gap: 0.75rem;
	}
	.provider-button {
		display: flex;
		width: 100%;
		min-height: 3.5rem;
		align-items: center;
		gap: 0.875rem;
		border: 1px solid var(--border-trace);
		border-radius: var(--radius-control);
		background: transparent;
		padding: 0.875rem 1rem;
		color: var(--reading-ink);
		font: 600 1rem / 1.5 var(--font-interface);
		text-align: start;
		cursor: pointer;
		transition:
			background-color 150ms ease,
			border-color 150ms ease;
	}
	.provider-button:hover {
		border-color: var(--reading-ink);
		background: var(--archive-layer);
	}
	.provider-button span {
		flex: 1;
	}
	.provider-button :global(svg) {
		flex: none;
	}
	.provider-button :global(.provider-arrow) {
		color: var(--marginal-note);
	}
	.registration-note {
		margin: 1rem 0 0;
		color: var(--marginal-note);
		font-size: 0.875rem;
		line-height: 1.5;
	}
	.guest-access {
		margin-top: 2rem;
		border-top: 1px solid color-mix(in srgb, var(--border-trace) 50%, transparent);
		padding-top: 1.5rem;
	}
	.guest-access a,
	.provider-unavailable a {
		display: inline-flex;
		min-height: 2.75rem;
		align-items: center;
		gap: 0.75rem;
		font-weight: 600;
	}
	.guest-access a :global(svg),
	.provider-unavailable a :global(svg) {
		flex: none;
	}
	.guest-access p {
		margin: 0.25rem 0 0;
		color: var(--marginal-note);
		font-size: 0.875rem;
		line-height: 1.5;
	}
	.provider-unavailable p {
		margin: 0;
		color: var(--marginal-note);
		font-size: 0.9375rem;
	}
	@media (max-width: 48rem) {
		.login-page {
			min-height: calc(100svh - 4.75rem);
			align-content: start;
			padding: clamp(3rem, 10vh, 5rem) 1rem 4rem;
		}
		h1 {
			font-size: 2.5rem;
		}
	}
</style>
