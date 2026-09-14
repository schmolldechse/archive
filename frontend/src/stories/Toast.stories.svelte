<script module lang="ts">
	import { defineMeta } from "@storybook/addon-svelte-csf";

	import { ToastProvider as ToastProviderMeta } from "$lib/components/ui/toast";

	const { Story } = defineMeta({
		title: "Components/Toast",
		component: ToastProviderMeta,
		parameters: {
			docs: {
				description: {
					component:
						"A composable notification stack with instance-bound control, chronological queueing, stable live regions, optional actions, adjustable timing, and keyboard focus access."
				}
			}
		}
	});
</script>

<script lang="ts">
	import Button from "$lib/components/ui/Button.svelte";
	import {
		ToastClose,
		ToastDescription,
		ToastProvider,
		ToastRoot,
		ToastTitle,
		ToastViewport,
		createToastController,
		type ToastController,
		type ToastPosition,
		type ToastPriority,
		type ToastRecord,
		type ToastVariant
	} from "$lib/components/ui/toast";

	const variantsController = createToastController();
	const announcementController = createToastController();
	const contentController = createToastController();
	const actionController = createToastController();
	const timingController = createToastController();
	const queueController = createToastController();
	const updateController = createToastController();
	const resilienceController = createToastController();
	const keyboardController = createToastController();
	const positionControllers: Record<ToastPosition, ToastController> = {
		"top-start": createToastController(),
		"top-end": createToastController(),
		"bottom-start": createToastController(),
		"bottom-end": createToastController()
	};

	let selectedPriority = $state<ToastPriority>("normal");
	let actionActivations = $state(0);
	let pendingOutcome = $state("Idle");
	let handledError = $state("None");
	let updateRevision = $state(0);

	const variantLabels: Record<ToastVariant, string> = {
		neutral: "Neutral message",
		info: "Information available",
		success: "Operation completed",
		warning: "Review recommended",
		error: "Operation could not complete"
	};

	for (const position of Object.keys(positionControllers) as ToastPosition[]) {
		positionControllers[position].add({
			id: `position-${position}`,
			title: position,
			description: "Logical start and end follow the document direction.",
			variant: "info"
		});
	}

	resilienceController.add({
		id: "long-content",
		title: "A deliberately long notification title wraps without losing information",
		description:
			"Supporting text remains complete at a narrow mobile width and continues onto as many lines as needed for the outcome to stay understandable.",
		variant: "warning"
	});

	function addVariant(variant: ToastVariant): void {
		variantsController.add({
			title: variantLabels[variant],
			description: `Visual variant: ${variant}. Announcement priority: ${selectedPriority}.`,
			variant,
			priority: selectedPriority
		});
	}

	function addActionMessage(): void {
		actionController.add({
			title: "Action available",
			description: "The optional action is a shortcut to functionality available elsewhere.",
			variant: "info",
			action: {
				label: "Apply action",
				onAction: () => {
					actionActivations += 1;
				}
			}
		});
	}

	function addPendingAction(): void {
		actionController.add({
			id: "pending-action",
			title: "Action can become pending",
			description: "The action exposes busy state while the close control remains available.",
			variant: "info",
			action: {
				label: "Complete action",
				async onAction() {
					pendingOutcome = "Pending";
					await new Promise((resolve) => setTimeout(resolve, 1200));
					pendingOutcome = "Completed";
				}
			}
		});
	}

	function addHandledActionError(): void {
		actionController.add({
			id: "handled-action-error",
			title: "Action error example",
			description: "The application decides how a rejected operation is reported.",
			variant: "warning",
			action: {
				label: "Try action",
				dismissOnAction: false,
				async onAction() {
					try {
						await Promise.reject(new Error("Demonstration failure"));
					} catch {
						handledError = "Handled by application";
						actionController.update("handled-action-error", {
							title: "Action could not complete",
							description: "The application converted the failure into explicit, durable feedback.",
							variant: "error"
						});
					}
				}
			}
		});
	}

	function fillQueue(): void {
		for (let index = 1; index <= 4; index += 1) {
			queueController.add({
				id: `queued-${index}`,
				title: `Message ${index}`,
				description: index <= 2 ? "Visible in chronological order." : "Waiting in the FIFO queue."
			});
		}
	}

	function updateStableMessage(): void {
		updateRevision += 1;
		updateController.update("stable-message", {
			title: "Existing message updated",
			description: `Revision ${updateRevision} preserves the original identifier.`,
			variant: "info"
		});
	}
</script>

<Story name="Variants and Priority" asChild>
	<div class="story-stack">
		<label class="story-field">
			<span>Announcement priority</span>
			<select bind:value={selectedPriority}>
				<option value="normal">Normal / polite</option>
				<option value="high">High / assertive</option>
			</select>
		</label>
		<div class="story-actions">
			{#each Object.keys(variantLabels) as variant}
				<Button onclick={() => addVariant(variant as ToastVariant)}>Add {variant}</Button>
			{/each}
		</div>
		<p class="story-note">Variant controls icon and accent; priority independently controls announcement urgency.</p>
		<ToastProvider controller={variantsController}>
			{#snippet children(_controller)}<ToastViewport />{/snippet}
		</ToastProvider>
	</div>
</Story>

<Story name="Announcement Priorities" asChild>
	<div class="story-stack">
		<div class="story-actions">
			<Button
				onclick={() =>
					announcementController.add({
						title: "Background update complete",
						description: "Normal priority uses the persistent status region.",
						priority: "normal",
						variant: "success"
					})}>Add polite message</Button
			>
			<Button
				onclick={() =>
					announcementController.add({
						title: "Immediate attention required",
						description: "High priority uses the persistent alert region and remains visible by default.",
						priority: "high",
						variant: "error"
					})}>Add assertive message</Button
			>
		</div>
		<p class="story-note">Use high priority sparingly; visual error styling does not automatically make a message assertive.</p>
		<ToastProvider controller={announcementController}>
			{#snippet children(_controller)}<ToastViewport />{/snippet}
		</ToastProvider>
	</div>
</Story>

<Story name="Content and Composition" asChild>
	<div class="story-stack">
		<div class="story-actions">
			<Button onclick={() => contentController.add({ title: "Title only" })}>Add title only</Button>
			<Button
				onclick={() =>
					contentController.add({
						title: "Title with supporting text",
						description: "Description content wraps and remains available in full.",
						variant: "info"
					})}>Add description</Button
			>
		</div>
		<ToastProvider controller={contentController} label="Composition examples">
			{#snippet children(_controller)}
				<ToastViewport>
					{#snippet children(record: ToastRecord)}
						<ToastRoot class="story-custom-toast">
							{#snippet children()}
								<div>
									<ToastTitle>{record.title}</ToastTitle>
									{#if record.description}<ToastDescription>{record.description}</ToastDescription>{/if}
								</div>
								{#if record.dismissible}<ToastClose>Dismiss</ToastClose>{/if}
							{/snippet}
						</ToastRoot>
					{/snippet}
				</ToastViewport>
			{/snippet}
		</ToastProvider>
	</div>
</Story>

<Story name="Actions and Dismissibility" asChild>
	<div class="story-stack">
		<div class="story-actions">
			<Button onclick={addActionMessage}>Add one-action message</Button>
			<Button
				onclick={() =>
					actionController.add({
						title: "Persistent record",
						description: "This message has no close control and requires programmatic dismissal.",
						dismissible: false
					})}>Add non-dismissible</Button
			>
			<Button onclick={addPendingAction}>Add pending action</Button>
			<Button onclick={addHandledActionError}>Add handled action error</Button>
		</div>
		<div class="story-register">
			<span>Action activations</span><code>{actionActivations}</code>
			<span>Pending outcome</span><code>{pendingOutcome}</code>
			<span>Error outcome</span><code>{handledError}</code>
		</div>
		<ToastProvider controller={actionController} limit={4}>
			{#snippet children(_controller)}<ToastViewport />{/snippet}
		</ToastProvider>
	</div>
</Story>

<Story name="Timing and Pause" asChild>
	<div class="story-stack">
		<div class="story-actions">
			<Button onclick={() => timingController.add({ title: "Persistent by default", duration: null })}>Add persistent</Button>
			<Button onclick={() => timingController.add({ title: "Explicit eight-second message", duration: 8000 })}>Add timed</Button
			>
			<Button onclick={() => timingController.add({ title: "Provider default duration" })}>Add provider-timed</Button>
			<Button
				onclick={() =>
					timingController.add({
						title: "Action remains persistent",
						action: { label: "Acknowledge", onAction: () => undefined }
					})}>Add persistent action</Button
			>
		</div>
		<p class="story-note">
			Hover a timed surface or move focus to one of its controls to pause with the remaining time preserved. Hiding the page
			also pauses timers.
		</p>
		<ToastProvider controller={timingController} defaultDuration={6000}>
			{#snippet children(_controller)}<ToastViewport />{/snippet}
		</ToastProvider>
	</div>
</Story>

<Story name="Queue and Promotion" asChild>
	<div class="story-stack">
		<div class="story-actions">
			<Button onclick={fillQueue}>Add four messages</Button>
			<Button onclick={() => queueController.dismiss("queued-1")}>Dismiss first visible</Button>
		</div>
		<p class="story-note">The limit is two. Dismissing the oldest visible message promotes exactly one queued message.</p>
		<ToastProvider controller={queueController} limit={2}>
			{#snippet children(_controller)}<ToastViewport />{/snippet}
		</ToastProvider>
	</div>
</Story>

<Story name="Updates and Programmatic Dismissal" asChild>
	<div class="story-stack">
		<div class="story-actions">
			<Button
				onclick={() =>
					updateController.add({
						id: "stable-message",
						title: "Stable message",
						description: "Its identifier can be updated or deduplicated."
					})}>Add stable ID</Button
			>
			<Button onclick={updateStableMessage}>Update stable ID</Button>
			<Button
				onclick={() =>
					updateController.add({
						id: "stable-message",
						title: "Duplicate ID became an update",
						description: "No second record was created.",
						variant: "success"
					})}>Add duplicate ID</Button
			>
			<Button onclick={() => updateController.dismiss("stable-message")}>Dismiss by ID</Button>
			<Button onclick={() => updateController.dismissAll()}>Dismiss all</Button>
		</div>
		<ToastProvider controller={updateController}>
			{#snippet children(_controller)}<ToastViewport />{/snippet}
		</ToastProvider>
	</div>
</Story>

<Story name="Logical Positions" asChild>
	<div class="story-position-grid">
		{#each Object.entries(positionControllers) as [position, controller], index}
			<section class="story-position">
				<span class="story-label">{position}</span>
				<ToastProvider {controller}>
					{#snippet children(_controller)}
						<ToastViewport position={position as ToastPosition} hotkey={`F${index + 8}`} class="story-position__viewport" />
					{/snippet}
				</ToastProvider>
			</section>
		{/each}
	</div>
</Story>

<Story name="Content Resilience" asChild>
	<div class="story-mobile-stage">
		<p class="story-note">The viewport is constrained to a narrow mobile measure without truncating essential content.</p>
		<ToastProvider controller={resilienceController}>
			{#snippet children(_controller)}<ToastViewport class="story-mobile-viewport" />{/snippet}
		</ToastProvider>
	</div>
</Story>

<Story name="Keyboard and Live Regions" asChild>
	<div class="story-keyboard-layout">
		<div class="story-guide">
			<dl>
				<dt><kbd>F8</kbd></dt>
				<dd>Focus the newest visible message without moving focus when it appears.</dd>
				<dt><kbd>Tab</kbd> <kbd>Shift</kbd> + <kbd>Tab</kbd></dt>
				<dd>Use normal document navigation for action and close controls.</dd>
				<dt><kbd>Esc</kbd></dt>
				<dd>Dismiss the focused message when allowed and restore the remembered origin.</dd>
			</dl>
			<p>
				Normal messages use a stable <code>status</code> region; high-priority messages use a separate stable <code>alert</code> region.
			</p>
		</div>
		<div class="story-stack">
			<Button
				onclick={() =>
					keyboardController.add({
						title: "Keyboard practice message",
						description: "Press F8, then Tab through the controls or press Escape.",
						variant: "info",
						action: { label: "Example action", onAction: () => undefined, dismissOnAction: false }
					})}>Add keyboard message</Button
			>
			<Button>Next focusable control</Button>
		</div>
		<ToastProvider controller={keyboardController} label="Keyboard practice notifications">
			{#snippet children(_controller)}<ToastViewport hotkey="F8" />{/snippet}
		</ToastProvider>
	</div>
</Story>

<style>
	.story-stack,
	.story-mobile-stage {
		display: grid;
		max-width: 52rem;
		gap: 1.5rem;
		font-family: var(--font-interface);
	}

	.story-actions,
	.story-register {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: 0.75rem;
	}

	.story-note,
	.story-guide p {
		margin: 0;
		border-inline-start: 2px solid var(--register-mark);
		padding-inline-start: 0.75rem;
		color: var(--marginal-note);
		font-size: 0.875rem;
		line-height: 1.43;
	}

	.story-field {
		display: grid;
		max-width: 18rem;
		gap: 0.5rem;
		color: var(--reading-ink);
		font-size: 0.875rem;
		font-weight: 600;
	}

	.story-field select {
		min-height: 2.75rem;
		border: 1px solid var(--border-trace);
		border-radius: var(--radius-control);
		background: var(--archive-layer);
		padding-inline: 0.75rem;
		color: var(--reading-ink);
	}

	.story-register {
		color: var(--marginal-note);
		font-size: 0.875rem;
	}

	.story-register code,
	.story-guide code {
		color: var(--reading-ink);
		font-family: var(--font-record);
	}

	:global([data-toast-root].story-custom-toast) {
		display: flex;
		align-items: start;
		justify-content: space-between;
		gap: 1rem;
		padding: 0.75rem;
	}

	.story-position-grid {
		display: grid;
		grid-template-columns: repeat(2, minmax(0, 1fr));
		gap: 1rem;
		font-family: var(--font-interface);
	}

	.story-position {
		position: relative;
		min-height: 18rem;
		overflow: hidden;
		border: 1px solid var(--border-trace);
		background: var(--reading-room);
	}

	.story-label {
		position: absolute;
		inset-block-start: 50%;
		inset-inline-start: 50%;
		color: var(--marginal-note);
		font-family: var(--font-record);
		font-size: 0.75rem;
		transform: translate(-50%, -50%);
	}

	.story-position :global(.story-position__viewport[data-toast-viewport]) {
		position: absolute;
		width: min(18rem, calc(100% - 2rem));
		max-height: calc(100% - 2rem);
	}

	.story-mobile-stage {
		position: relative;
		min-height: 28rem;
		max-width: 22rem;
		border-inline: 1px solid var(--border-trace);
		padding: 1rem;
	}

	.story-mobile-stage :global(.story-mobile-viewport[data-toast-viewport]) {
		position: absolute;
		width: calc(100% - 1.5rem);
		inset: auto 0.75rem 0.75rem;
	}

	.story-keyboard-layout {
		display: grid;
		grid-template-columns: minmax(20rem, 1fr) minmax(16rem, 22rem);
		gap: 2rem;
		max-width: 64rem;
		font-family: var(--font-interface);
	}

	.story-guide {
		border-block: 1px solid var(--border-trace);
		padding-block: 1rem;
	}

	.story-guide dl {
		display: grid;
		grid-template-columns: minmax(10rem, auto) 1fr;
		gap: 0.75rem 1rem;
		margin: 0 0 1rem;
	}

	.story-guide dt {
		font-weight: 600;
	}

	.story-guide dd {
		margin: 0;
		color: var(--marginal-note);
	}

	.story-guide kbd {
		border: 1px solid var(--border-trace);
		border-radius: var(--radius-mark);
		background: var(--archive-layer);
		padding: 0.125rem 0.375rem;
		font-family: var(--font-record);
		font-size: 0.75rem;
	}

	@media (max-width: 48rem) {
		.story-position-grid,
		.story-keyboard-layout {
			grid-template-columns: 1fr;
		}

		.story-guide dl {
			grid-template-columns: 1fr;
			gap: 0.25rem;
		}

		.story-guide dd:not(:last-child) {
			margin-block-end: 0.75rem;
		}
	}
</style>
