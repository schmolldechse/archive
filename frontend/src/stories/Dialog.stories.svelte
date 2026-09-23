<script module lang="ts">
	import { defineMeta } from "@storybook/addon-svelte-csf";

	import { DialogRoot as DialogRootMeta } from "$lib/components/ui/dialog";

	const { Story } = defineMeta({
		title: "Components/Dialog",
		component: DialogRootMeta,
		parameters: {
			docs: {
				description: {
					component:
						"A composable native dialog family for focused modal and non-modal content, with predictable dismissal, labelling, and focus return."
				}
			}
		}
	});
</script>

<script lang="ts">
	import Button from "$lib/components/ui/Button.svelte";
	import {
		DialogActions,
		DialogClose,
		DialogContent,
		DialogDescription,
		DialogRoot,
		DialogTitle,
		DialogTrigger,
		type DialogOpenReason,
		type DialogSize
	} from "$lib/components/ui/dialog";

	let nonModalCount = $state(0);
	let controlledOpen = $state(false);
	let controlledChanges = $state(0);
	let controlledReason = $state<DialogOpenReason | "None">("None");
	let focusOpen = $state(false);
	let returnTarget = $state<HTMLButtonElement | HTMLAnchorElement | null>(null);
	let initialField = $state<HTMLInputElement | null>(null);

	const recordControlledChange = (open: boolean, reason: DialogOpenReason) => {
		controlledChanges += 1;
		controlledReason = reason;
		controlledOpen = open;
	};
</script>

{#snippet StandardDialogContent()}
	<DialogTitle>Dialog title</DialogTitle>
	<DialogDescription>Supporting text explains the focused task without replacing its content.</DialogDescription>
	<div class="story-body">
		<label for="dialog-story-label">Label</label>
		<input id="dialog-story-label" value="Editable value" />
	</div>
	<DialogActions>
		<DialogClose>Close</DialogClose>
		<Button variant="primary">Primary action</Button>
	</DialogActions>
{/snippet}

{#snippet SizedDialog(size: DialogSize, label: string)}
	<DialogRoot>
		<DialogTrigger>{label}</DialogTrigger>
		<DialogContent {size}>
			<DialogTitle>{label}</DialogTitle>
			<DialogDescription>This example documents the {size} content constraint.</DialogDescription>
			<div class="story-body">
				<p>
					Long structured content reflows without truncation. It remains readable when the viewport or text size is constrained
					and does not impose a feature-specific internal layout.
				</p>
				<dl>
					<dt>Label</dt>
					<dd>Supporting text</dd>
					<dt>Identifier</dt>
					<dd><code>dialog-size-example</code></dd>
				</dl>
			</div>
			<DialogActions><DialogClose>Close</DialogClose></DialogActions>
		</DialogContent>
	</DialogRoot>
{/snippet}

<Story name="Modal" asChild>
	<div class="story-stack">
		<p class="story-note">The document becomes inert while this dialog is open and keyboard focus remains inside.</p>
		<DialogRoot modal>
			<DialogTrigger>Open modal</DialogTrigger>
			<DialogContent>
				{@render StandardDialogContent()}
			</DialogContent>
		</DialogRoot>
	</div>
</Story>

<Story name="Non-modal" asChild>
	<div class="story-stack">
		<p class="story-note">The dialog follows its trigger while the outside action remains operable.</p>
		<div class="story-row">
			<DialogRoot modal={false}>
				<DialogTrigger>Open non-modal dialog</DialogTrigger>
				<DialogContent position="trigger" closeOnOutsidePointer>
					<DialogTitle>Non-modal dialog</DialogTitle>
					<DialogDescription>The surrounding document remains available.</DialogDescription>
					<div class="story-body"><a href="#dialog-non-modal-destination">Focusable outside destination</a></div>
					<DialogActions><DialogClose>Close</DialogClose></DialogActions>
				</DialogContent>
			</DialogRoot>
			<Button onclick={() => (nonModalCount += 1)}>Outside action</Button>
		</div>
		<div class="story-register" aria-live="polite">
			<span>Outside activations</span>
			<code>{nonModalCount}</code>
		</div>
		<div id="dialog-non-modal-destination" class="story-destination">Outside destination</div>
	</div>
</Story>

<Story name="Controlled State" asChild>
	<div class="story-stack">
		<div class="story-row">
			<Button variant="primary" onclick={() => (controlledOpen = true)}>Open programmatically</Button>
			<Button onclick={() => (controlledOpen = false)}>Close programmatically</Button>
		</div>
		<div class="story-register" aria-live="polite">
			<span>State</span>
			<code>{controlledOpen ? "open" : "closed"}</code>
			<span>Reason</span>
			<code>{controlledReason}</code>
			<span>Changes</span>
			<code>{controlledChanges}</code>
		</div>
		<DialogRoot bind:open={controlledOpen} onOpenChange={recordControlledChange}>
			<DialogTrigger>Open with trigger</DialogTrigger>
			<DialogContent>
				<DialogTitle>Controlled dialog</DialogTitle>
				<DialogDescription>The register reports every accepted state change and its reason.</DialogDescription>
				<DialogActions><DialogClose>Close with dialog action</DialogClose></DialogActions>
			</DialogContent>
		</DialogRoot>
	</div>
</Story>

<Story name="Initial and Return Focus" asChild>
	<div class="story-stack">
		<p class="story-note">
			Opening focuses the explicit field. Closing returns focus to the labelled target instead of the default trigger.
		</p>
		<DialogRoot bind:open={focusOpen}>
			<div class="story-row">
				<Button bind:ref={returnTarget} variant="primary" onclick={() => (focusOpen = true)}>Return-focus target</Button>
				<DialogTrigger>Default trigger</DialogTrigger>
			</div>
			<DialogContent
				initialFocus={() => initialField}
				returnFocus={() => (returnTarget instanceof HTMLButtonElement ? returnTarget : null)}
			>
				<DialogTitle>Focus callbacks</DialogTitle>
				<DialogDescription>The input and return button are supplied through explicit callbacks.</DialogDescription>
				<div class="story-body">
					<label for="dialog-story-initial-focus">Initial focus</label>
					<input id="dialog-story-initial-focus" bind:this={initialField} value="Focused on open" />
				</div>
				<DialogActions><DialogClose>Close and restore focus</DialogClose></DialogActions>
			</DialogContent>
		</DialogRoot>
	</div>
</Story>

<Story name="Dismissal Options" asChild>
	<div class="story-grid">
		<section>
			<p class="story-label">Dismissal enabled</p>
			<DialogRoot>
				<DialogTrigger>Open enabled example</DialogTrigger>
				<DialogContent>
					<DialogTitle>Dismissal enabled</DialogTitle>
					<DialogDescription>Press Escape or select the backdrop to close.</DialogDescription>
					<DialogActions><DialogClose>Close</DialogClose></DialogActions>
				</DialogContent>
			</DialogRoot>
		</section>
		<section>
			<p class="story-label">Dismissal disabled</p>
			<DialogRoot>
				<DialogTrigger>Open disabled example</DialogTrigger>
				<DialogContent closeOnEscape={false} closeOnOutsidePointer={false}>
					<DialogTitle>Explicit close required</DialogTitle>
					<DialogDescription>Escape and backdrop selection leave this dialog open.</DialogDescription>
					<DialogActions><DialogClose>Close explicitly</DialogClose></DialogActions>
				</DialogContent>
			</DialogRoot>
		</section>
	</div>
</Story>

<Story name="Content Sizes" asChild>
	<div class="story-row">
		{@render SizedDialog("small", "Small dialog")}
		{@render SizedDialog("medium", "Medium dialog")}
		{@render SizedDialog("large", "Large dialog")}
		{@render SizedDialog("viewport", "Viewport dialog")}
	</div>
</Story>

<Story name="Accessible Naming" asChild>
	<div class="story-grid">
		<section>
			<p class="story-label">Title relationship</p>
			<DialogRoot>
				<DialogTrigger>Open title-labelled dialog</DialogTrigger>
				<DialogContent>
					<DialogTitle level={3}>Title-based name</DialogTitle>
					<DialogDescription>Supporting text is connected as the accessible description.</DialogDescription>
					<DialogActions><DialogClose>Close</DialogClose></DialogActions>
				</DialogContent>
			</DialogRoot>
		</section>
		<section>
			<p class="story-label">Explicit label</p>
			<DialogRoot>
				<DialogTrigger>Open explicitly labelled dialog</DialogTrigger>
				<DialogContent aria-label="Explicit dialog label">
					<p class="story-body">This simple dialog receives its accessible name directly.</p>
					<DialogActions><DialogClose>Close</DialogClose></DialogActions>
				</DialogContent>
			</DialogRoot>
		</section>
	</div>
</Story>

<style>
	.story-stack {
		display: grid;
		gap: 1.5rem;
		max-width: 48rem;
	}

	.story-grid {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(min(100%, 18rem), 1fr));
		gap: 2rem;
		max-width: 56rem;
	}

	.story-grid section {
		border-block-start: 1px solid var(--border-trace);
		padding-block-start: 1rem;
	}

	.story-row,
	.story-register {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: 0.75rem;
	}

	.story-label,
	.story-note,
	.story-register,
	.story-body,
	.story-destination {
		font-family: var(--font-interface);
	}

	.story-label {
		margin: 0 0 0.75rem;
		color: var(--marginal-note);
		font-size: 0.75rem;
		font-weight: 600;
		letter-spacing: 0.08em;
		text-transform: uppercase;
	}

	.story-note {
		margin: 0;
		border-inline-start: 2px solid var(--register-mark);
		padding-inline-start: 1rem;
		color: var(--marginal-note);
		line-height: 1.5;
	}

	.story-register {
		color: var(--marginal-note);
		font-size: 0.875rem;
	}

	.story-register code,
	.story-body code {
		color: var(--reading-ink);
		font-family: var(--font-record);
	}

	.story-body {
		display: grid;
		gap: 0.5rem;
		margin-block-start: 1.5rem;
	}

	.story-body p {
		margin: 0;
	}

	.story-body input {
		min-height: 2.75rem;
		box-sizing: border-box;
		border: 1px solid var(--border-trace);
		border-radius: var(--radius-control);
		background: var(--archive-layer);
		padding-inline: 0.75rem;
		color: var(--reading-ink);
	}

	.story-body dl {
		display: grid;
		grid-template-columns: auto 1fr;
		gap: 0.5rem 1rem;
		margin: 1rem 0 0;
	}

	.story-body dt {
		color: var(--marginal-note);
		font-weight: 600;
	}

	.story-body dd {
		margin: 0;
	}

	.story-destination {
		border-block-start: 1px solid var(--border-trace);
		padding-block-start: 1rem;
		color: var(--marginal-note);
	}
</style>
