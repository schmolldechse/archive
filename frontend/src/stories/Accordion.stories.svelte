<script module lang="ts">
	import { defineMeta } from "@storybook/addon-svelte-csf";

	import { AccordionRoot as AccordionRootMeta } from "$lib/components/ui/accordion";

	const { Story } = defineMeta({
		title: "Components/Accordion",
		component: AccordionRootMeta,
		parameters: {
			docs: {
				description: {
					component:
						"A composable disclosure group for one or several expandable sections. Headings preserve document structure while triggers expose native button behavior."
				}
			}
		}
	});
</script>

<script lang="ts">
	import {
		AccordionContent,
		AccordionHeader,
		AccordionItem,
		AccordionRoot,
		AccordionTrigger
	} from "$lib/components/ui/accordion";

	let boundSingle = $state<string | null>("label");
	let boundMultiple = $state<string[]>(["label", "supporting"]);
	let singleChanges = $state(0);
	let multipleChanges = $state(0);
</script>

{#snippet StandardItems()}
	<AccordionItem value="label">
		<AccordionHeader>
			<AccordionTrigger>Label</AccordionTrigger>
		</AccordionHeader>
		<AccordionContent>
			<p>Supporting text explains the first section without introducing feature-specific content.</p>
		</AccordionContent>
	</AccordionItem>
	<AccordionItem value="supporting">
		<AccordionHeader>
			<AccordionTrigger>Supporting text</AccordionTrigger>
		</AccordionHeader>
		<AccordionContent>
			<p>A second panel demonstrates the relationship between adjacent items.</p>
		</AccordionContent>
	</AccordionItem>
	<AccordionItem value="details">
		<AccordionHeader>
			<AccordionTrigger>Details</AccordionTrigger>
		</AccordionHeader>
		<AccordionContent>
			<p>Additional detail remains subordinate to the heading that reveals it.</p>
		</AccordionContent>
	</AccordionItem>
{/snippet}

<Story name="Single" asChild>
	<div class="story-stack">
		<section>
			<p class="story-label">Collapsible</p>
			<AccordionRoot type="single" value="label">
				{@render StandardItems()}
			</AccordionRoot>
		</section>
		<section>
			<p class="story-label">Non-collapsible</p>
			<AccordionRoot type="single" value="label" collapsible={false}>
				{@render StandardItems()}
			</AccordionRoot>
		</section>
	</div>
</Story>

<Story name="Multiple" asChild>
	<div class="story-grid">
		<section>
			<p class="story-label">Zero open</p>
			<AccordionRoot type="multiple" value={[]}>
				{@render StandardItems()}
			</AccordionRoot>
		</section>
		<section>
			<p class="story-label">One open</p>
			<AccordionRoot type="multiple" value={["label"]}>
				{@render StandardItems()}
			</AccordionRoot>
		</section>
		<section>
			<p class="story-label">Several open</p>
			<AccordionRoot type="multiple" value={["label", "supporting"]}>
				{@render StandardItems()}
			</AccordionRoot>
		</section>
	</div>
</Story>

<Story name="Bound Values" asChild>
	<div class="story-stack">
		<section>
			<div class="story-register">
				<span>Single value</span>
				<code>{boundSingle ?? "null"}</code>
				<span>{singleChanges} changes</span>
			</div>
			<AccordionRoot
				type="single"
				bind:value={boundSingle}
				onValueChange={() => (singleChanges += 1)}
			>
				{@render StandardItems()}
			</AccordionRoot>
		</section>
		<section>
			<div class="story-register">
				<span>Multiple values</span>
				<code>{boundMultiple.length ? boundMultiple.join(", ") : "[]"}</code>
				<span>{multipleChanges} changes</span>
			</div>
			<AccordionRoot
				type="multiple"
				bind:value={boundMultiple}
				onValueChange={() => (multipleChanges += 1)}
			>
				{@render StandardItems()}
			</AccordionRoot>
		</section>
	</div>
</Story>

<Story name="Disabled States" asChild>
	<div class="story-stack">
		<section>
			<p class="story-label">Disabled item</p>
			<AccordionRoot type="single" value="label">
				<AccordionItem value="label">
					<AccordionHeader><AccordionTrigger>Enabled and open</AccordionTrigger></AccordionHeader>
					<AccordionContent><p>This content remains available.</p></AccordionContent>
				</AccordionItem>
				<AccordionItem value="disabled" disabled>
					<AccordionHeader><AccordionTrigger>Disabled</AccordionTrigger></AccordionHeader>
					<AccordionContent><p>The disabled item retains its current state.</p></AccordionContent>
				</AccordionItem>
			</AccordionRoot>
		</section>
		<section>
			<p class="story-label">Disabled root</p>
			<AccordionRoot type="multiple" value={["label"]} disabled>
				{@render StandardItems()}
			</AccordionRoot>
		</section>
		<section>
			<p class="story-label">Open, non-collapsible trigger</p>
			<AccordionRoot type="single" value="label" collapsible={false}>
				{@render StandardItems()}
			</AccordionRoot>
		</section>
	</div>
</Story>

<Story name="Heading Levels" asChild>
	<div class="story-stack">
		<section>
			<p class="story-label">Root heading level 2</p>
			<AccordionRoot type="single" value="label" headingLevel={2}>
				{@render StandardItems()}
			</AccordionRoot>
		</section>
		<section>
			<p class="story-label">Root level 4 with one level 5 override</p>
			<AccordionRoot type="single" value="details" headingLevel={4}>
				<AccordionItem value="label">
					<AccordionHeader><AccordionTrigger>Level 4</AccordionTrigger></AccordionHeader>
					<AccordionContent><p>Visual treatment is independent of heading level.</p></AccordionContent>
				</AccordionItem>
				<AccordionItem value="details">
					<AccordionHeader level={5}><AccordionTrigger>Level 5 override</AccordionTrigger></AccordionHeader>
					<AccordionContent><p>The override follows the surrounding document hierarchy.</p></AccordionContent>
				</AccordionItem>
			</AccordionRoot>
		</section>
	</div>
</Story>

<Story name="Mounted Content" asChild>
	<div class="story-stack">
		<p class="story-note">
			Inspect the DOM to compare mounting. Use browser Find for “Searchable reference phrase” to reveal
			the final panel.
		</p>
		<section>
			<p class="story-label">Unmounted by default</p>
			<AccordionRoot type="single" value={null}>
				<AccordionItem value="default">
					<AccordionHeader><AccordionTrigger>Default content</AccordionTrigger></AccordionHeader>
					<AccordionContent><p>Absent from the DOM while closed.</p></AccordionContent>
				</AccordionItem>
			</AccordionRoot>
		</section>
		<section>
			<p class="story-label">Force mounted</p>
			<AccordionRoot type="single" value={null}>
				<AccordionItem value="mounted">
					<AccordionHeader><AccordionTrigger>Mounted content</AccordionTrigger></AccordionHeader>
					<AccordionContent forceMount><p>Mounted with the native hidden attribute while closed.</p></AccordionContent>
				</AccordionItem>
			</AccordionRoot>
		</section>
		<section>
			<p class="story-label">Hidden until found</p>
			<AccordionRoot type="single" value={null}>
				<AccordionItem value="findable">
					<AccordionHeader><AccordionTrigger>Browser-findable content</AccordionTrigger></AccordionHeader>
					<AccordionContent hiddenUntilFound>
						<p>Searchable reference phrase opens this panel through the beforematch event.</p>
					</AccordionContent>
				</AccordionItem>
			</AccordionRoot>
		</section>
	</div>
</Story>

<Story name="Content Resilience" asChild>
	<div class="story-constrained">
		<AccordionRoot type="multiple" value={["long", "structured"]}>
			<AccordionItem value="long">
				<AccordionHeader>
					<AccordionTrigger>
						A long label wraps across several lines without colliding with the expansion indicator
					</AccordionTrigger>
				</AccordionHeader>
				<AccordionContent>
					<p>
						Long supporting text reflows naturally within a constrained width and remains readable at
						increased text sizes.
					</p>
				</AccordionContent>
			</AccordionItem>
			<AccordionItem value="structured">
				<AccordionHeader><AccordionTrigger>Structured panel</AccordionTrigger></AccordionHeader>
				<AccordionContent region>
					<dl>
						<dt>Label</dt>
						<dd>Supporting text</dd>
					</dl>
					<label for="accordion-story-field">Focusable field</label>
					<input id="accordion-story-field" value="Editable value" />
					<a href="#accordion-story-link">Focusable link</a>
				</AccordionContent>
			</AccordionItem>
		</AccordionRoot>
	</div>
</Story>

<Story name="Keyboard Navigation" asChild>
	<div class="story-stack">
		<p class="story-note">
			Focus a trigger, then use Arrow keys to move, Home for the first trigger, and End for the last.
			Enter and Space retain native button behavior; Tab follows document order.
		</p>
		<AccordionRoot type="single" value="label" keyboardNavigation loop>
			{@render StandardItems()}
		</AccordionRoot>
	</div>
</Story>

<style>
	.story-stack {
		display: grid;
		gap: 3rem;
		max-width: 48rem;
	}

	.story-grid {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(min(100%, 16rem), 1fr));
		gap: 2rem;
	}

	.story-label,
	.story-register,
	.story-note {
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

	.story-register {
		display: flex;
		flex-wrap: wrap;
		gap: 0.5rem 1rem;
		align-items: baseline;
		margin-block-end: 1rem;
		color: var(--marginal-note);
		font-size: 0.875rem;
	}

	.story-register code {
		color: var(--reading-ink);
		font-family: var(--font-record);
	}

	.story-note {
		margin: 0;
		border-inline-start: 2px solid var(--register-mark);
		padding-inline-start: 1rem;
		color: var(--marginal-note);
		line-height: 1.5;
	}

	.story-constrained {
		max-width: 22rem;
	}

	.story-constrained dl {
		display: grid;
		grid-template-columns: auto 1fr;
		gap: 0.5rem 1rem;
	}

	.story-constrained dt {
		color: var(--marginal-note);
		font-weight: 600;
	}

	.story-constrained dd {
		margin: 0;
	}

	.story-constrained label,
	.story-constrained a {
		display: block;
		margin-block-start: 1rem;
	}

	.story-constrained input {
		box-sizing: border-box;
		width: 100%;
		min-height: 2.75rem;
		margin-block-start: 0.25rem;
		border: 1px solid var(--border-trace);
		border-radius: var(--radius-control);
		background: var(--archive-layer);
		padding-inline: 0.75rem;
		color: var(--reading-ink);
	}
</style>
