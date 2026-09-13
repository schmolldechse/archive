<script module lang="ts">
	import { defineMeta } from "@storybook/addon-svelte-csf";

	import { DropdownMenuRoot as DropdownMenuRootMeta } from "$lib/components/ui/dropdown-menu";

	const { Story } = defineMeta({
		title: "Components/Dropdown Menu",
		component: DropdownMenuRootMeta,
		parameters: {
			docs: {
				description: {
					component:
						"A composable command menu with actions, links, labelled groups, persistent choices, roving focus, typeahead, collision-aware positioning, and nested submenus."
				}
			}
		}
	});
</script>

<script lang="ts">
	import Button from "$lib/components/ui/Button.svelte";
	import {
		DropdownMenuCheckboxGroup,
		DropdownMenuCheckboxItem,
		DropdownMenuContent,
		DropdownMenuGroup,
		DropdownMenuItem,
		DropdownMenuLabel,
		DropdownMenuRadioGroup,
		DropdownMenuRadioItem,
		DropdownMenuRoot,
		DropdownMenuSeparator,
		DropdownMenuSub,
		DropdownMenuSubContent,
		DropdownMenuSubTrigger,
		DropdownMenuTrigger,
		type DropdownMenuAlign,
		type DropdownMenuSide,
		type MenuOpenReason
	} from "$lib/components/ui/dropdown-menu";

	let actionCount = $state(0);
	let persistentCount = $state(0);
	let preventedSelections = $state(0);
	let checkboxValues = $state(["option-a"]);
	let checkboxChanges = $state(0);
	let preventedCheckboxSelections = $state(0);
	let radioValue = $state("option-b");
	let radioChanges = $state(0);
	let preventedRadioSelections = $state(0);
	let controlledOpen = $state(false);
	let controlledReason = $state<MenuOpenReason | "None">("None");
	let controlledChanges = $state(0);
	let controlledSubOpen = $state(false);
	let controlledSubReason = $state<MenuOpenReason | "None">("None");
	let controlledSubChanges = $state(0);

	const longItems = Array.from({ length: 14 }, (_, index) => `Item ${String(index + 1).padStart(2, "0")}`);
	const placements: Array<{ side: DropdownMenuSide; align: DropdownMenuAlign }> = [
		{ side: "bottom", align: "start" },
		{ side: "right", align: "end" },
		{ side: "top", align: "center" },
		{ side: "left", align: "start" }
	];

	const recordControlledChange = (_open: boolean, reason: MenuOpenReason) => {
		controlledChanges += 1;
		controlledReason = reason;
	};

	const recordControlledSubChange = (_open: boolean, reason: MenuOpenReason) => {
		controlledSubChanges += 1;
		controlledSubReason = reason;
	};
</script>

<Story name="Actions and Links" asChild>
	<div class="story-stack">
		<p class="story-note">Actions, native links, disabled entries, separators, and persistent commands share one menu.</p>
		<DropdownMenuRoot>
			<DropdownMenuTrigger>Open menu</DropdownMenuTrigger>
			<DropdownMenuContent>
				<DropdownMenuItem onclick={() => (actionCount += 1)}>
					<span>Action</span><span class="story-shortcut">Enter</span>
				</DropdownMenuItem>
				<DropdownMenuItem href="#dropdown-menu-link-destination">Link item</DropdownMenuItem>
				<DropdownMenuItem
					href="#dropdown-menu-prevented-destination"
					onSelect={(event) => {
						preventedSelections += 1;
						event.preventDefault();
					}}
				>
					Prevented link
				</DropdownMenuItem>
				<DropdownMenuItem disabled>Disabled item</DropdownMenuItem>
				<DropdownMenuSeparator />
				<DropdownMenuItem closeOnSelect={false} onclick={() => (persistentCount += 1)}>
					Persistent action
				</DropdownMenuItem>
			</DropdownMenuContent>
		</DropdownMenuRoot>
		<div class="story-register" aria-live="polite">
			<span>Action selections</span><code>{actionCount}</code>
			<span>Persistent selections</span><code>{persistentCount}</code>
			<span>Prevented selections</span><code>{preventedSelections}</code>
		</div>
		<DropdownMenuRoot disabled>
			<DropdownMenuTrigger>Disabled root trigger</DropdownMenuTrigger>
			<DropdownMenuContent><DropdownMenuItem>Unavailable action</DropdownMenuItem></DropdownMenuContent>
		</DropdownMenuRoot>
		<div id="dropdown-menu-link-destination" class="story-destination">Link destination</div>
		<div id="dropdown-menu-prevented-destination" class="story-destination">Prevented link destination</div>
	</div>
</Story>

<Story name="Labelled Groups" asChild>
	<div class="story-stack">
		<DropdownMenuRoot>
			<DropdownMenuTrigger>Open grouped menu</DropdownMenuTrigger>
			<DropdownMenuContent>
				<DropdownMenuGroup>
					<DropdownMenuLabel>First group</DropdownMenuLabel>
					<DropdownMenuItem>First action</DropdownMenuItem>
					<DropdownMenuItem>Second action</DropdownMenuItem>
				</DropdownMenuGroup>
				<DropdownMenuSeparator />
				<DropdownMenuGroup>
					<DropdownMenuLabel>Second group</DropdownMenuLabel>
					<DropdownMenuItem>Supporting action</DropdownMenuItem>
					<DropdownMenuItem>Additional action</DropdownMenuItem>
				</DropdownMenuGroup>
			</DropdownMenuContent>
		</DropdownMenuRoot>
	</div>
</Story>

<Story name="Checkbox Choices" asChild>
	<div class="story-stack">
		<p class="story-note">Checkbox items remain open by default so several choices can be changed in one visit.</p>
		<DropdownMenuRoot>
			<DropdownMenuTrigger>Open checkbox choices</DropdownMenuTrigger>
			<DropdownMenuContent>
				<DropdownMenuGroup>
					<DropdownMenuLabel>Choices</DropdownMenuLabel>
					<DropdownMenuCheckboxGroup
						bind:value={checkboxValues}
						onValueChange={() => (checkboxChanges += 1)}
					>
						<DropdownMenuCheckboxItem value="option-a">
							{#snippet children({ checked })}<span>Option A{checked ? " — selected" : ""}</span>{/snippet}
						</DropdownMenuCheckboxItem>
						<DropdownMenuCheckboxItem value="option-b">
							{#snippet children({ checked })}<span>Option B{checked ? " — selected" : ""}</span>{/snippet}
						</DropdownMenuCheckboxItem>
						<DropdownMenuCheckboxItem value="option-c" disabled>
							{#snippet children()}<span>Option C unavailable</span>{/snippet}
						</DropdownMenuCheckboxItem>
						<DropdownMenuCheckboxItem
							value="option-d"
							onSelect={(event) => {
								preventedCheckboxSelections += 1;
								event.preventDefault();
							}}
						>
							{#snippet children()}<span>Protected choice</span>{/snippet}
						</DropdownMenuCheckboxItem>
					</DropdownMenuCheckboxGroup>
				</DropdownMenuGroup>
			</DropdownMenuContent>
		</DropdownMenuRoot>
		<div class="story-register" aria-live="polite">
			<span>Selected</span><code>{checkboxValues.join(", ") || "None"}</code>
			<span>Changes</span><code>{checkboxChanges}</code>
			<span>Prevented</span><code>{preventedCheckboxSelections}</code>
		</div>
	</div>
</Story>

<Story name="Radio Choices" asChild>
	<div class="story-stack">
		<p class="story-note">A radio selection updates one value and closes the complete menu by default.</p>
		<DropdownMenuRoot>
			<DropdownMenuTrigger>Open radio choices</DropdownMenuTrigger>
			<DropdownMenuContent>
				<DropdownMenuGroup>
					<DropdownMenuLabel>Single choice</DropdownMenuLabel>
					<DropdownMenuRadioGroup bind:value={radioValue} onValueChange={() => (radioChanges += 1)}>
						<DropdownMenuRadioItem value="option-a">
							{#snippet children({ checked })}<span>Option A{checked ? " — selected" : ""}</span>{/snippet}
						</DropdownMenuRadioItem>
						<DropdownMenuRadioItem value="option-b">
							{#snippet children({ checked })}<span>Option B{checked ? " — selected" : ""}</span>{/snippet}
						</DropdownMenuRadioItem>
						<DropdownMenuRadioItem
							value="option-c"
							onSelect={(event) => {
								preventedRadioSelections += 1;
								event.preventDefault();
							}}
						>
							{#snippet children()}<span>Protected choice</span>{/snippet}
						</DropdownMenuRadioItem>
					</DropdownMenuRadioGroup>
				</DropdownMenuGroup>
			</DropdownMenuContent>
		</DropdownMenuRoot>
		<div class="story-register" aria-live="polite">
			<span>Selected</span><code>{radioValue}</code>
			<span>Changes</span><code>{radioChanges}</code>
			<span>Prevented</span><code>{preventedRadioSelections}</code>
		</div>
	</div>
</Story>

<Story name="Submenus" asChild>
	<div class="story-stack">
		<p class="story-note">Pointer movement or the logical forward arrow opens a submenu; Escape closes one level.</p>
		<DropdownMenuRoot loop>
			<DropdownMenuTrigger>Open nested menu</DropdownMenuTrigger>
			<DropdownMenuContent>
				<DropdownMenuItem>Direct action</DropdownMenuItem>
				<DropdownMenuSub>
					<DropdownMenuSubTrigger>First submenu</DropdownMenuSubTrigger>
					<DropdownMenuSubContent>
						<DropdownMenuItem>Nested action</DropdownMenuItem>
						<DropdownMenuSub>
							<DropdownMenuSubTrigger>Second submenu</DropdownMenuSubTrigger>
							<DropdownMenuSubContent>
								<DropdownMenuItem>Deep action</DropdownMenuItem>
								<DropdownMenuItem>Additional deep action</DropdownMenuItem>
							</DropdownMenuSubContent>
						</DropdownMenuSub>
					</DropdownMenuSubContent>
				</DropdownMenuSub>
				<DropdownMenuSub>
					<DropdownMenuSubTrigger>Sibling submenu</DropdownMenuSubTrigger>
					<DropdownMenuSubContent>
						<DropdownMenuItem>Sibling action</DropdownMenuItem>
					</DropdownMenuSubContent>
				</DropdownMenuSub>
			</DropdownMenuContent>
		</DropdownMenuRoot>
		<section class="story-rtl" dir="rtl">
			<p class="story-label">Right-to-left direction</p>
			<DropdownMenuRoot>
				<DropdownMenuTrigger>Open RTL menu</DropdownMenuTrigger>
				<DropdownMenuContent>
					<DropdownMenuSub>
						<DropdownMenuSubTrigger>Logical inline-end submenu</DropdownMenuSubTrigger>
						<DropdownMenuSubContent>
							<DropdownMenuItem>RTL nested action</DropdownMenuItem>
						</DropdownMenuSubContent>
					</DropdownMenuSub>
					<DropdownMenuItem>RTL direct action</DropdownMenuItem>
				</DropdownMenuContent>
			</DropdownMenuRoot>
		</section>
	</div>
</Story>

<Story name="Controlled Open State" asChild>
	<div class="story-stack">
		<div class="story-row">
			<Button variant="primary" onclick={() => (controlledOpen = true)}>Open programmatically</Button>
			<Button
				onclick={() => {
					controlledOpen = true;
					controlledSubOpen = true;
				}}
			>
				Open with submenu
			</Button>
			<Button
				onclick={() => {
					controlledSubOpen = false;
					controlledOpen = false;
				}}
			>
				Close programmatically
			</Button>
		</div>
		<div class="story-register" aria-live="polite">
			<span>State</span><code>{controlledOpen ? "open" : "closed"}</code>
			<span>Reason</span><code>{controlledReason}</code>
			<span>Changes</span><code>{controlledChanges}</code>
			<span>Submenu</span><code>{controlledSubOpen ? "open" : "closed"}</code>
			<span>Submenu reason</span><code>{controlledSubReason}</code>
			<span>Submenu changes</span><code>{controlledSubChanges}</code>
		</div>
		<DropdownMenuRoot bind:open={controlledOpen} onOpenChange={recordControlledChange}>
			<DropdownMenuTrigger>Toggle with trigger</DropdownMenuTrigger>
			<DropdownMenuContent>
				<DropdownMenuItem>First action</DropdownMenuItem>
				<DropdownMenuSub bind:open={controlledSubOpen} onOpenChange={recordControlledSubChange}>
					<DropdownMenuSubTrigger>Controlled submenu</DropdownMenuSubTrigger>
					<DropdownMenuSubContent>
						<DropdownMenuItem>Controlled nested action</DropdownMenuItem>
					</DropdownMenuSubContent>
				</DropdownMenuSub>
			</DropdownMenuContent>
		</DropdownMenuRoot>
	</div>
</Story>

<Story name="Positioning" asChild>
	<div class="story-position-boundary">
		{#each placements as placement}
			<div class="story-placement" data-side={placement.side}>
				<DropdownMenuRoot>
					<DropdownMenuTrigger>{placement.side} · {placement.align}</DropdownMenuTrigger>
					<DropdownMenuContent side={placement.side} align={placement.align} collisionPadding={12}>
						<DropdownMenuItem>Positioned action</DropdownMenuItem>
						<DropdownMenuItem>Supporting action</DropdownMenuItem>
					</DropdownMenuContent>
				</DropdownMenuRoot>
			</div>
		{/each}
	</div>
</Story>

<Story name="Content Resilience" asChild>
	<div class="story-resilience">
		<p class="story-note">The menu constrains width and height while preserving complete, wrapping labels and typeahead text.</p>
		<DropdownMenuRoot typeahead>
			<DropdownMenuTrigger>Open constrained menu</DropdownMenuTrigger>
			<DropdownMenuContent class="story-narrow-menu">
				<DropdownMenuItem textValue="Alpha">
					A deliberately long label that wraps across several lines without being truncated
				</DropdownMenuItem>
				<DropdownMenuSeparator />
				{#each longItems as item}
					<DropdownMenuItem textValue={item}>{item} with supporting label</DropdownMenuItem>
				{/each}
			</DropdownMenuContent>
		</DropdownMenuRoot>
	</div>
</Story>

<Story name="Keyboard Guide" asChild>
	<div class="story-stack">
		<div class="story-guide">
			<dl>
				<dt><kbd>↓</kbd> <kbd>↑</kbd></dt><dd>Move among enabled items</dd>
				<dt><kbd>Home</kbd> <kbd>End</kbd></dt><dd>Move to the first or last item</dd>
				<dt><kbd>A–Z</kbd></dt><dd>Match the buffered item text</dd>
				<dt><kbd>→</kbd> <kbd>←</kbd></dt><dd>Open or close one submenu level</dd>
				<dt><kbd>Esc</kbd></dt><dd>Close the current menu and return focus</dd>
				<dt><kbd>Tab</kbd></dt><dd>Close without trapping focus</dd>
			</dl>
		</div>
		<DropdownMenuRoot loop>
			<DropdownMenuTrigger>Try keyboard navigation</DropdownMenuTrigger>
			<DropdownMenuContent>
				<DropdownMenuItem textValue="Alpha">Alpha item</DropdownMenuItem>
				<DropdownMenuItem textValue="Bravo">Bravo item</DropdownMenuItem>
				<DropdownMenuItem disabled>Disabled item</DropdownMenuItem>
				<DropdownMenuSub>
					<DropdownMenuSubTrigger textValue="Charlie">Charlie submenu</DropdownMenuSubTrigger>
					<DropdownMenuSubContent>
						<DropdownMenuItem>Nested item</DropdownMenuItem>
					</DropdownMenuSubContent>
				</DropdownMenuSub>
			</DropdownMenuContent>
		</DropdownMenuRoot>
		<div class="story-boundary-example">
			<p class="story-label">No loop and no typeahead</p>
			<DropdownMenuRoot loop={false} typeahead={false}>
				<DropdownMenuTrigger>Open bounded navigation</DropdownMenuTrigger>
				<DropdownMenuContent>
					<DropdownMenuItem>First boundary item</DropdownMenuItem>
					<DropdownMenuItem>Last boundary item</DropdownMenuItem>
				</DropdownMenuContent>
			</DropdownMenuRoot>
		</div>
		<Button>Next focusable control</Button>
	</div>
</Story>

<style>
	.story-stack,
	.story-resilience {
		display: grid;
		gap: 1.5rem;
		max-width: 48rem;
		font-family: var(--font-interface);
	}

	.story-resilience {
		width: min(100%, 20rem);
	}

	.story-row,
	.story-register {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: 0.75rem;
	}

	.story-note {
		margin: 0;
		border-inline-start: 2px solid var(--register-mark);
		padding-inline-start: 1rem;
		color: var(--marginal-note);
		line-height: 1.5;
	}

	.story-label {
		margin: 0 0 0.75rem;
		color: var(--marginal-note);
		font-size: 0.75rem;
		font-weight: 600;
		letter-spacing: 0.08em;
		text-transform: uppercase;
	}

	.story-rtl,
	.story-boundary-example {
		border-block-start: 1px solid var(--border-trace);
		padding-block-start: 1rem;
	}

	.story-register {
		color: var(--marginal-note);
		font-size: 0.875rem;
	}

	.story-register code {
		color: var(--reading-ink);
		font-family: var(--font-record);
	}

	.story-shortcut {
		margin-inline-start: auto;
		color: var(--marginal-note);
		font-family: var(--font-record);
		font-size: 0.75rem;
	}

	.story-destination {
		border-block-start: 1px solid var(--border-trace);
		padding-block-start: 1rem;
		color: var(--marginal-note);
	}

	.story-position-boundary {
		display: grid;
		min-height: 34rem;
		grid-template-columns: repeat(2, minmax(0, 1fr));
		grid-template-rows: repeat(2, 1fr);
		gap: 2rem;
		border: 1px solid var(--border-trace);
		padding: 1rem;
	}

	.story-placement {
		display: flex;
	}

	.story-placement[data-side="right"],
	.story-placement[data-side="bottom"] {
		align-items: flex-start;
	}

	.story-placement[data-side="left"],
	.story-placement[data-side="top"] {
		align-items: flex-end;
	}

	.story-placement[data-side="left"],
	.story-placement[data-side="right"] {
		justify-content: center;
	}

	.story-placement[data-side="top"] {
		justify-content: flex-end;
	}

	.story-guide {
		border-block: 1px solid var(--border-trace);
		padding-block: 1rem;
	}

	.story-guide dl {
		display: grid;
		grid-template-columns: minmax(7rem, auto) 1fr;
		gap: 0.75rem 1rem;
		margin: 0;
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

	:global(.story-narrow-menu) {
		width: 15rem;
		max-height: 18rem;
	}

	@media (max-width: 32rem) {
		.story-position-boundary {
			grid-template-columns: 1fr;
			grid-template-rows: repeat(4, minmax(8rem, 1fr));
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
