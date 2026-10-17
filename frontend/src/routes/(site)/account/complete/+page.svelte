<script lang="ts">
	import { enhance } from "$app/forms";
	import type { SubmitFunction } from "@sveltejs/kit";
	import { tick, untrack } from "svelte";
	import ArrowRight from "@lucide/svelte/icons/arrow-right";
	import CircleAlert from "@lucide/svelte/icons/circle-alert";
	import { completeHref } from "$lib/auth/return-url";
	import Avatar from "$lib/components/ui/Avatar.svelte";
	import Button from "$lib/components/ui/Button.svelte";
	import { InputRoot, InputLabel, InputControl, InputDescription, InputError } from "$lib/components/ui/input";
	import HandleGreeting from "$lib/components/site/account/HandleGreeting.svelte";
	import type { PageProps } from "./$types";

	let { data, form }: PageProps = $props();
	let displayName = $state(untrack(() => form?.values.displayName ?? data.user?.displayName ?? ""));
	let handle = $state(untrack(() => form?.values.handle ?? data.user?.handle ?? ""));
	let saving = $state(false);
	let submissionError = $state<string | null>(null);
	const previewHandle = $derived(handle.trim().toLowerCase());
	const previewName = $derived(displayName.trim());
	const initials = $derived(Array.from(previewName)[0]?.toUpperCase());
	const nameError = $derived(displayName === form?.values.displayName ? form.errors.displayName : null);
	const handleError = $derived(handle === form?.values.handle ? form.errors.handle : null);
	const message = $derived(submissionError ?? form?.message);

	const submit: SubmitFunction = ({ cancel, formElement }) => {
		if (saving) return cancel();
		saving = true;
		submissionError = null;
		return async ({ result, update }) => {
			try {
				if (result.type === "error") {
					submissionError = "Your profile couldn’t be saved. Please try again.";
				} else {
					await update({ reset: false });
				}
			} finally {
				saving = false;
			}
			if (result.type === "failure") {
				await tick();
				formElement.querySelector<HTMLInputElement>('[aria-invalid="true"]')?.focus();
			}
		};
	};
</script>

<svelte:head>
	<title>Set up your profile — ARCHIV</title>
	<meta name="robots" content="noindex, nofollow" />
</svelte:head>

<main id="main-content" class="complete-page">
	<div class="complete-content">
		{#if data.user}
			<div class="profile-layout">
				<header class="profile-introduction">
					<HandleGreeting handle={previewHandle} />
					<div class="profile-preview">
						<p class="preview-label">Profile preview</p>
						<div class="preview-identity">
							<Avatar decorative src={data.user.profileImageUrl} {initials} />
							<div class="preview-text">
								<p class="preview-name" class:placeholder={!previewName} dir="auto">{previewName || "Your display name"}</p>
								<p class="preview-handle" class:placeholder={!previewHandle}>@{previewHandle || "your_handle"}</p>
							</div>
						</div>
					</div>
				</header>

				<form method="POST" use:enhance={submit} aria-busy={saving}>
					<h2>Your profile</h2>
					<p class="introduction">You can change these later.</p>
					<div class="profile-fields">
						<InputRoot required hasDescription invalid={Boolean(handleError)} disabled={saving}>
							<InputLabel>Handle <span class="field-qualifier">Required</span></InputLabel>
							<InputControl
								name="handle"
								bind:value={handle}
								autocomplete="username"
								autocapitalize="none"
								spellcheck={false}
								minlength={4}
								maxlength={32}
								pattern={"[A-Za-z0-9][A-Za-z0-9_\\-]{2,30}[A-Za-z0-9]"}
								title="4–32 letters or numbers, with hyphens or underscores between them."
								data-value-kind="record"
							/>
							<InputDescription>4–32 letters or numbers, with - or _ between them. Saved in lowercase.</InputDescription>
							{#if handleError}<InputError>{handleError}</InputError>{/if}
						</InputRoot>
						<InputRoot required hasDescription invalid={Boolean(nameError)} disabled={saving}>
							<InputLabel>Display name <span class="field-qualifier">Required</span></InputLabel>
							<InputControl name="displayName" bind:value={displayName} autocomplete="nickname" minlength={6} maxlength={64} />
							<InputDescription>6–64 characters. Spaces are fine.</InputDescription>
							{#if nameError}<InputError>{nameError}</InputError>{/if}
						</InputRoot>
					</div>
					{#if message}
						<p class="form-error" role="alert"><CircleAlert size={20} aria-hidden="true" /><span>{message}</span></p>
					{/if}
					<div class="form-actions">
						<Button type="submit" variant="primary" size="large" loading={saving} loadingLabel="Saving…">
							Save and continue <ArrowRight aria-hidden="true" />
						</Button>
						<Button href={data.returnUrl} variant="text" disabled={saving}>Do this later</Button>
					</div>
				</form>
			</div>
		{:else}
			<p class="section-label">Your account</p>
			<h1>Account unavailable.</h1>
			<p class="introduction">We couldn’t load your session. Please try again.</p>
			<div class="form-actions">
				<Button href={completeHref(data.returnUrl)} data-sveltekit-reload variant="primary">Try again</Button>
				<Button href={data.returnUrl} variant="text">Back to the archive</Button>
			</div>
		{/if}
	</div>
</main>

<style>
	.complete-page {
		display: grid;
		min-height: calc(100svh - 5.75rem);
		align-content: center;
		padding: 4rem 1.5rem 6rem;
	}
	.complete-content {
		width: min(100%, 58rem);
		min-width: 0;
		margin-inline: auto;
	}
	.section-label {
		margin: 0;
		color: var(--marginal-note);
		font-size: 0.75rem;
		font-weight: 600;
		letter-spacing: 0.08em;
		text-transform: uppercase;
	}
	.profile-layout {
		display: grid;
		grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
		gap: 4rem;
		align-items: start;
	}
	.profile-introduction {
		min-width: 0;
	}
	h1,
	h2 {
		margin: 0 0 0.75rem;
		font: 500 3rem / 1.08 var(--font-editorial);
		letter-spacing: -0.02em;
		overflow-wrap: anywhere;
	}
	h2 {
		font-size: 2rem;
	}
	.introduction {
		margin: 0;
		color: var(--marginal-note);
	}
	.profile-preview {
		margin-top: 3rem;
		border-block: 1px solid color-mix(in srgb, var(--border-trace) 50%, transparent);
		padding-block: 1rem;
	}
	.preview-label {
		margin: 0 0 1rem;
		color: var(--marginal-note);
		font-size: 0.875rem;
	}
	.preview-identity {
		display: flex;
		align-items: center;
		gap: 0.75rem;
	}
	.preview-text {
		min-width: 0;
	}
	.preview-name {
		margin: 0;
		font-weight: 600;
		overflow-wrap: anywhere;
	}
	.preview-handle {
		margin: 0.25rem 0 0;
		font: 400 0.8125rem / 1.54 var(--font-record);
		overflow-wrap: anywhere;
	}
	.placeholder {
		color: var(--marginal-note);
	}
	form {
		min-width: 0;
	}
	.profile-fields {
		display: grid;
		gap: 1.5rem;
		margin-top: 2rem;
	}
	.form-actions {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: 0.5rem 0.75rem;
		margin-top: 2rem;
	}
	.form-error {
		display: flex;
		align-items: start;
		gap: 0.75rem;
		margin: 1.5rem 0 0;
		border-inline-start: 2px solid var(--time-marker);
		padding-inline-start: 0.75rem;
		font-size: 0.9375rem;
	}
	.form-error :global(svg) {
		flex: none;
		margin-top: 0.125rem;
		color: var(--time-marker);
	}
	@media (max-width: 48rem) {
		.complete-page {
			min-height: calc(100svh - 4.75rem);
			align-content: start;
			padding: 2rem 1rem 4rem;
		}
		.complete-content {
			max-width: 32rem;
		}
		.profile-layout {
			grid-template-columns: minmax(0, 1fr);
			gap: 2rem;
		}
		.profile-preview {
			margin-top: 1.5rem;
		}
	}
	@media (max-width: 24rem) {
		.form-actions {
			align-items: stretch;
			flex-direction: column;
		}
	}
</style>
