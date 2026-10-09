<script lang="ts">
	import { untrack } from "svelte";
	import type { UserResponse } from "$lib/api";
	import { AuthenticationApiError, updateProfile } from "$lib/authentication/api";
	import { getAuthenticationContext } from "$lib/authentication/context.svelte";
	import { InputRoot, InputLabel, InputControl, InputDescription, InputError } from "$lib/components/ui/input";
	import Button from "$lib/components/ui/Button.svelte";
	import UserAvatar from "./UserAvatar.svelte";
	let { user }: { user: UserResponse } = $props();
	const authentication = getAuthenticationContext();
	let displayName = $state<string | number | undefined>(untrack(() => user.displayName));
	let handle = $state<string | number | undefined>(untrack(() => user.handle));
	let busy = $state(false);
	let error = $state("");
	let fieldErrors = $state<Record<string, string[]>>({});
	let message = $state("");
	async function saveProfile(): Promise<void> {
		if (busy) return;
		busy = true;
		error = "";
		message = "";
		fieldErrors = {};
		try {
			const updated = await updateProfile({ displayName: String(displayName ?? ""), handle: String(handle ?? "") });
			authentication.user = updated;
			displayName = updated.displayName;
			handle = updated.handle;
			message = "Your profile has been saved.";
		} catch (failure) {
			if (failure instanceof AuthenticationApiError && failure.status === 401) {
				authentication.user = null;
				return;
			}
			error = failure instanceof Error ? failure.message : "The profile could not be saved. Please try again.";
			if (failure instanceof AuthenticationApiError) fieldErrors = failure.problem?.errors || {};
		} finally {
			busy = false;
		}
	}
</script>

<section class="account-section" aria-labelledby="profile-details-title">
	<h2 id="profile-details-title">Profile details</h2>
	<form
		class="account-form"
		onsubmit={(event) => {
			event.preventDefault();
			void saveProfile();
		}}
	>
		<InputRoot required disabled={busy} invalid={Boolean(fieldErrors.displayName)}>
			<InputLabel>Display name</InputLabel>
			<InputControl name="displayName" bind:value={displayName} maxlength={64} autocomplete="nickname" />
			{#if fieldErrors.displayName}<InputError>{fieldErrors.displayName.join(" ")}</InputError>{/if}
		</InputRoot>
		<InputRoot required hasDescription disabled={busy} invalid={Boolean(fieldErrors.handle)}>
			<InputLabel>Handle</InputLabel>
			<InputControl
				name="handle"
				bind:value={handle}
				minlength={3}
				maxlength={32}
				autocomplete="username"
				autocapitalize="none"
				spellcheck={false}
			/>
			<InputDescription
				>3–32 letters or digits, with hyphens or underscores between them. Your handle is unique and can be changed.</InputDescription
			>
			{#if fieldErrors.handle}<InputError>{fieldErrors.handle.join(" ")}</InputError>{/if}
		</InputRoot>
		<div class="account-actions">
			<Button type="submit" variant="primary" disabled={busy} loading={busy} loadingLabel="Saving…">Save profile</Button>
		</div>
	</form>
</section>

<section class="account-section" aria-labelledby="profile-image-title">
	<h2 id="profile-image-title">Profile image</h2>
	<div class="profile-preview">
		<UserAvatar {user} size="large" />
		<p>
			{user.profileImageUrl
				? "Provided by your sign-in provider."
				: "Your initials appear when no provider image is available."}
		</p>
	</div>
</section>

{#if error}<p class="account-error" role="alert">{error}</p>{/if}
{#if message}<p class="account-status" role="status">{message}</p>{/if}

<style>
	.profile-preview {
		display: flex;
		align-items: center;
		gap: 1.25rem;
		margin-bottom: 1.5rem;
	}
	.profile-preview p {
		margin: 0;
	}
</style>
