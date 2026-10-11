<script lang="ts">
	import ArrowRight from "@lucide/svelte/icons/arrow-right";
	import LogOut from "@lucide/svelte/icons/log-out";
	import X from "@lucide/svelte/icons/x";
	import type { AuthenticatedArchiveUser } from "$api";
	import { page } from "$app/state";
	import { apiUrl } from "$lib/auth/api";
	import { completeHref } from "$lib/auth/return-url";
	import Avatar from "$lib/components/ui/Avatar.svelte";
	import Button from "$lib/components/ui/Button.svelte";
	import {
		DialogRoot,
		DialogTrigger,
		DialogContent,
		DialogTitle,
		DialogDescription,
		DialogClose
	} from "$lib/components/ui/dialog";

	let { user, onopen }: { user: AuthenticatedArchiveUser; onopen?: () => void } = $props();
	let open = $state(false);
	let closeButton = $state<HTMLButtonElement | null>(null);
</script>

<div
	class="site-account"
	data-component="site-account"
	data-registration={user.isRegistrationComplete ? "complete" : "incomplete"}
>
	<DialogRoot
		bind:open
		modal={false}
		onOpenChange={(value) => {
			if (value) onopen?.();
		}}
	>
		<DialogTrigger
			class="site-account__trigger"
			aria-label={user.isRegistrationComplete && user.displayName ? `Open account for ${user.displayName}` : "Open account"}
		>
			<Avatar src={user.isRegistrationComplete ? user.profileImageUrl : null} size="compact" decorative />
		</DialogTrigger>
		<DialogContent
			class="site-account__panel"
			size="small"
			position="trigger"
			align="end"
			sideOffset={12}
			closeOnOutsidePointer
			initialFocus={() => closeButton}
		>
			<div class="account-heading">
				<div class="account-profile">
					{#if user.isRegistrationComplete}
						<Avatar src={user.profileImageUrl} decorative />
					{/if}
					<div class="account-profile__text">
						<DialogTitle class="site-account__title">{user.isRegistrationComplete ? user.displayName : "Account"}</DialogTitle>
						{#if user.isRegistrationComplete}
							<p class="account-profile__handle">@{user.handle}</p>
						{/if}
					</div>
				</div>
				<DialogClose bind:ref={closeButton} class="site-account__close" aria-label="Close account">
					<X aria-hidden="true" />
				</DialogClose>
			</div>

			{#if !user.isRegistrationComplete}
				<div class="account-registration">
					<DialogDescription class="site-account__description">
						Your registration is not complete yet. Continue to finish setting up your account.
					</DialogDescription>
					<Button
						variant="primary"
						class="site-account__continue"
						href={completeHref(page.url.pathname + page.url.search)}
						onclick={() => (open = false)}
					>
						Continue registration<ArrowRight aria-hidden="true" />
					</Button>
				</div>
			{/if}

			<form method="POST" action={apiUrl("/api/auth/logout")} class="account-logout">
				<button type="submit"><LogOut aria-hidden="true" />Sign out</button>
			</form>
		</DialogContent>
	</DialogRoot>
</div>

<style>
	.site-account {
		display: flex;
	}

	.site-account :global(.site-account__trigger) {
		width: 2.75rem;
		height: 2.75rem;
		padding: 0.25rem;
		border-color: transparent;
		border-radius: var(--radius-control);
	}

	.site-account :global(.site-account__trigger:hover:not([data-disabled])),
	.site-account :global(.site-account__trigger[aria-expanded="true"]) {
		border-color: var(--border-trace);
		background: var(--archive-layer);
	}

	.site-account :global(.site-account__panel) {
		width: min(19rem, calc(100vw - 1.5rem));
		padding: 0;
		overflow-x: hidden;
	}

	.account-heading {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 0.75rem;
		padding: 0.75rem 0.5rem 0.75rem 1rem;
	}

	.site-account :global(.site-account__title) {
		font: 600 1rem / 1.4 var(--font-interface);
		letter-spacing: normal;
		overflow-wrap: anywhere;
	}

	.site-account :global(.site-account__close) {
		border-color: transparent;
		padding: 0.5rem;
		color: var(--marginal-note);
	}

	.site-account :global(.site-account__close:hover:not([data-disabled])) {
		border-color: var(--border-trace);
		background: var(--archive-layer);
	}

	.account-profile {
		display: flex;
		align-items: flex-start;
		min-width: 0;
		gap: 0.75rem;
	}

	.account-profile__text {
		min-width: 0;
	}

	.account-profile__handle {
		margin: 0.125rem 0 0;
		color: var(--marginal-note);
		font: 400 0.75rem / 1.5 var(--font-record);
		overflow-wrap: anywhere;
	}

	.account-registration {
		padding: 0 1rem 1rem;
	}

	.account-logout :global(svg) {
		width: 1.125rem;
		height: 1.125rem;
		flex: none;
		stroke-width: 1.75;
	}

	.site-account :global(.site-account__description) {
		margin: 0 0 1rem;
		font-size: 0.9375rem;
		line-height: 1.5;
	}

	.site-account :global(.site-account__continue) {
		width: 100%;
		justify-content: space-between;
		text-align: start;
	}

	.account-logout {
		border-top: 1px solid color-mix(in srgb, var(--border-trace) 45%, transparent);
		padding: 0.25rem 0.5rem;
	}

	.account-logout button {
		display: flex;
		width: 100%;
		min-height: 2.75rem;
		align-items: center;
		gap: 0.75rem;
		border: 1px solid transparent;
		border-radius: var(--radius-control);
		background: transparent;
		padding: 0.5rem;
		color: var(--reading-ink);
		font: 600 0.9375rem / 1.5 var(--font-interface);
		text-align: start;
		cursor: pointer;
		transition: background-color 150ms ease;
	}

	.account-logout button:hover {
		background: var(--archive-layer);
	}

	.account-logout :global(svg) {
		color: var(--marginal-note);
	}

	@media (max-width: 34rem) {
		.site-account :global(.site-account__panel) {
			width: calc(100vw - 1.5rem);
			max-width: none;
		}
	}
</style>
