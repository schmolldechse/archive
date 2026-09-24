<script module lang="ts">
	import { defineMeta } from "@storybook/addon-svelte-csf";

	import { InputRoot as InputRootMeta } from "$lib/components/ui/input";

	const { Story } = defineMeta({
		title: "Components/Input",
		component: InputRootMeta,
		parameters: {
			docs: {
				description: {
					component:
						"Composable labelled inputs for text, files, and longer notes, with deterministic relationships for supporting and validation text."
				}
			}
		}
	});
</script>

<script lang="ts">
	import Button from "$lib/components/ui/Button.svelte";
	import { InputControl, InputDescription, InputError, InputLabel, InputRoot, InputTextarea } from "$lib/components/ui/input";

	let boundValue = $state("Editable value");
	let boundRoot = $state<HTMLDivElement | null>(null);
	let boundControl = $state<HTMLInputElement | null>(null);
	let inputEvents = $state(0);
	let changeEvents = $state(0);
	let focusEvents = $state(0);
	let blurEvents = $state(0);
	let selectedFiles = $state<string[]>([]);
</script>

<Story name="Anatomy" asChild>
	<div class="story-grid">
		<section class="story-sample">
			<p class="story-label">Complete field</p>
			<InputRoot id="input-anatomy-complete" hasDescription>
				<InputLabel>Label</InputLabel>
				<InputControl placeholder="Placeholder" aria-describedby="input-anatomy-consumer-description" />
				<InputDescription>Supporting text explains the expected value.</InputDescription>
				<p id="input-anatomy-consumer-description" class="story-note">Additional consumer-provided context.</p>
			</InputRoot>
		</section>
		<section class="story-sample">
			<p class="story-label">Invalid field</p>
			<InputRoot id="input-anatomy-invalid" hasDescription invalid>
				<InputLabel>Label</InputLabel>
				<InputControl value="Unusable value" />
				<InputDescription>Supporting text is announced before the correction.</InputDescription>
				<InputError>Enter a value in the requested format.</InputError>
			</InputRoot>
		</section>
	</div>
</Story>

<Story name="Files and Multiline" asChild>
	<div class="story-grid">
		<InputRoot id="input-file" hasDescription required>
			<InputLabel>Attachment</InputLabel>
			<InputControl
				type="file"
				accept=".txt,.pdf"
				onFilesChange={(files) => (selectedFiles = files.map((file) => file.name))}
			/>
			<InputDescription>Choose a file or drop it onto the field.</InputDescription>
			<p class="story-note" aria-live="polite">{selectedFiles.length ? selectedFiles.join(", ") : "No file selected"}</p>
		</InputRoot>
		<InputRoot id="input-textarea" hasDescription>
			<InputLabel>Longer note</InputLabel>
			<InputTextarea placeholder="Write a note" rows={4} />
			<InputDescription>The field grows vertically when resized.</InputDescription>
		</InputRoot>
		<InputRoot id="input-file-invalid" invalid>
			<InputLabel>Invalid attachment</InputLabel>
			<InputControl type="file" />
			<InputError>Choose a supported file.</InputError>
		</InputRoot>
	</div>
</Story>

<Story name="Native Types" asChild>
	<div class="story-grid">
		<InputRoot id="input-type-text">
			<InputLabel>Text</InputLabel>
			<InputControl type="text" placeholder="Text value" />
		</InputRoot>
		<InputRoot id="input-type-search">
			<InputLabel>Search</InputLabel>
			<InputControl type="search" placeholder="Search value" />
		</InputRoot>
		<InputRoot id="input-type-url">
			<InputLabel>URL</InputLabel>
			<InputControl type="url" data-value-kind="record" value="https://example.com/reference" />
		</InputRoot>
		<InputRoot id="input-type-email">
			<InputLabel>Email</InputLabel>
			<InputControl type="email" placeholder="name@example.com" />
		</InputRoot>
		<InputRoot id="input-type-tel">
			<InputLabel>Telephone</InputLabel>
			<InputControl type="tel" placeholder="Telephone value" />
		</InputRoot>
		<InputRoot id="input-type-password">
			<InputLabel>Password</InputLabel>
			<InputControl type="password" placeholder="Password value" />
		</InputRoot>
		<InputRoot id="input-type-number">
			<InputLabel>Number</InputLabel>
			<InputControl type="number" value={12} />
		</InputRoot>
		<InputRoot id="input-type-date">
			<InputLabel>Date</InputLabel>
			<InputControl type="date" value="2026-09-11" />
		</InputRoot>
		<InputRoot id="input-type-time">
			<InputLabel>Time</InputLabel>
			<InputControl type="time" value="14:32" />
		</InputRoot>
		<InputRoot id="input-type-datetime">
			<InputLabel>Date and time</InputLabel>
			<InputControl type="datetime-local" value="2026-09-11T14:32" />
		</InputRoot>
	</div>
</Story>

<Story name="States" asChild>
	<div class="story-grid">
		<InputRoot id="input-state-default">
			<InputLabel>Default</InputLabel>
			<InputControl placeholder="Placeholder" />
		</InputRoot>
		<InputRoot id="input-state-populated">
			<InputLabel>Populated</InputLabel>
			<InputControl value="Entered value" />
		</InputRoot>
		<InputRoot id="input-state-required" required hasDescription>
			<InputLabel>Required field (required)</InputLabel>
			<InputControl />
			<InputDescription>This field must be completed.</InputDescription>
		</InputRoot>
		<InputRoot id="input-state-disabled" disabled>
			<InputLabel>Disabled</InputLabel>
			<InputControl value="Unavailable value" />
		</InputRoot>
		<InputRoot id="input-state-readonly">
			<InputLabel>Read only</InputLabel>
			<InputControl value="Read-only value" readonly />
		</InputRoot>
		<InputRoot id="input-state-invalid" invalid>
			<InputLabel>Invalid</InputLabel>
			<InputControl value="Incorrect value" />
			<InputError>Replace the value with the requested format.</InputError>
		</InputRoot>
		<InputRoot id="input-state-valid">
			<InputLabel>Valid without added status styling</InputLabel>
			<InputControl value="Accepted value" />
		</InputRoot>
	</div>
</Story>

<Story name="Value Binding" asChild>
	<div class="story-stack">
		<InputRoot id="input-value-binding" bind:ref={boundRoot} hasDescription data-story-value-binding>
			<InputLabel>Editable label</InputLabel>
			<InputControl
				bind:value={boundValue}
				bind:ref={boundControl}
				oninput={() => (inputEvents += 1)}
				onchange={() => (changeEvents += 1)}
				onfocus={() => (focusEvents += 1)}
				onblur={() => (blurEvents += 1)}
			/>
			<InputDescription>Type a value to update the readout.</InputDescription>
		</InputRoot>
		<div class="story-actions">
			<Button onclick={() => boundControl?.focus()}>Focus control</Button>
		</div>
		<div class="story-register" aria-live="polite">
			<span>Value</span>
			<code>{boundValue || "Empty"}</code>
			<span>Input events</span>
			<code>{inputEvents}</code>
			<span>Change events</span>
			<code>{changeEvents}</code>
			<span>Focus events</span>
			<code>{focusEvents}</code>
			<span>Blur events</span>
			<code>{blurEvents}</code>
			<span>{boundRoot ? "Root reference connected" : "Root reference unavailable"}</span>
			<span>{boundControl ? "Control reference connected" : "Control reference unavailable"}</span>
		</div>
	</div>
</Story>

<Story name="Native Attributes" asChild>
	<div class="story-grid">
		<InputRoot id="input-attributes-autocomplete">
			<InputLabel>Autocomplete</InputLabel>
			<InputControl name="label" autocomplete="name" placeholder="Label" />
		</InputRoot>
		<InputRoot id="input-attributes-pattern" hasDescription>
			<InputLabel>Numeric reference</InputLabel>
			<InputControl inputmode="numeric" pattern="[0-9]*" maxlength={8} data-value-kind="record" />
			<InputDescription>Use up to eight digits.</InputDescription>
		</InputRoot>
		<InputRoot id="input-attributes-range" hasDescription>
			<InputLabel>Bounded number</InputLabel>
			<InputControl type="number" min={1} max={12} step={1} value={6} />
			<InputDescription>Enter a number from 1 through 12.</InputDescription>
		</InputRoot>
	</div>
</Story>

<Story name="Content Resilience" asChild>
	<div class="story-constrained">
		<InputRoot id="input-content-resilience" hasDescription invalid>
			<InputLabel>A long label remains visible and wraps without colliding with the input or its supporting text</InputLabel>
			<InputControl
				type="url"
				data-value-kind="record"
				value="https://example.com/a/complete/reference/value/that-remains-copyable"
			/>
			<InputDescription>
				Long supporting text reflows naturally in a constrained area and continues to explain the expected value at increased
				text sizes.
			</InputDescription>
			<InputError>Enter a complete URL beginning with https:// and keep the full reference available for review.</InputError>
		</InputRoot>
	</div>
</Story>

<style>
	.story-grid {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(min(100%, 18rem), 1fr));
		gap: 2rem;
		max-width: 64rem;
	}

	.story-stack {
		display: grid;
		max-width: 36rem;
		gap: 1.5rem;
	}

	.story-sample {
		border-block-end: 1px solid var(--border-trace);
		padding-block-end: 1.5rem;
	}

	.story-label,
	.story-note,
	.story-register {
		font-family: var(--font-interface);
	}

	.story-label {
		margin: 0 0 0.75rem;
		color: var(--marginal-note);
		font-size: 0.75rem;
		font-weight: 600;
		letter-spacing: 0.08em;
		line-height: 1.33;
		text-transform: uppercase;
	}

	.story-actions {
		display: flex;
		flex-wrap: wrap;
		gap: 0.75rem;
	}

	.story-note {
		margin: 0;
		border-inline-start: 2px solid var(--register-mark);
		padding-inline-start: 0.75rem;
		color: var(--marginal-note);
		font-size: 0.875rem;
		line-height: 1.43;
	}

	.story-register {
		display: flex;
		flex-wrap: wrap;
		align-items: baseline;
		gap: 0.5rem 1rem;
		color: var(--marginal-note);
		font-size: 0.875rem;
		line-height: 1.43;
	}

	.story-register code {
		max-width: 100%;
		color: var(--reading-ink);
		font-family: var(--font-record);
		overflow-wrap: anywhere;
	}

	.story-constrained {
		width: 18rem;
		max-width: 100%;
	}
</style>
