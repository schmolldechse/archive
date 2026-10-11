<script module lang="ts">
	import { defineMeta } from "@storybook/addon-svelte-csf";
	import Avatar, { type AvatarSize } from "$lib/components/ui/Avatar.svelte";
	import portraitA from "./avatar-portrait-a.jpg?no-inline";
	import portraitB from "./avatar-portrait-b.jpg?no-inline";
	import transparentImage from "./avatar-transparent.svg?no-inline";
	import invalidImage from "./avatar-invalid.txt?url&no-inline";

	const sizes: AvatarSize[] = ["compact", "default", "large"];
	const sources = [
		{ value: "portraitA", label: "Image A", src: portraitA },
		{ value: "portraitB", label: "Image B", src: portraitB },
		{ value: "invalid", label: "Invalid image", src: invalidImage },
		{ value: "none", label: "None", src: undefined }
	];
	const sourceFor = (value: string) => sources.find((source) => source.value === value)?.src;

	const { Story } = defineMeta({
		title: "Components/Avatar",
		component: Avatar,
		tags: ["autodocs"],
		parameters: {
			controls: { disable: true },
			docs: {
				description: {
					component:
						"A circular identity image with a fine outline, a neutral surface, and a proportional placeholder. Use alt for an informative image, or decorative beside visible identity text and inside a named control. Fallback priority: named snippet, explicit initials, then icon. Image state stays internal. Use the toolbar to inspect both themes; photographs retain their original colors."
				}
			}
		},
		argTypes: {
			size: {
				control: "select",
				options: sizes,
				description: "Compact 32px, default 40px, large 64px; grows with enlarged text."
			},
			src: {
				control: "select",
				options: ["portraitA", "portraitB", "transparent", "invalid", "none"],
				mapping: { portraitA, portraitB, transparent: transparentImage, invalid: invalidImage, none: undefined },
				description: "Local image source. Missing or whitespace-only values make no request."
			},
			initials: {
				control: "text",
				description: "One or two explicit grapheme clusters. No name parsing or automatic uppercase."
			},
			alt: { control: "text", description: "Required non-empty identity label when decorative is false." },
			decorative: { control: "boolean", description: "Hide the whole avatar from assistive technology; omit alt." },
			fallback: {
				control: false,
				description: "Presentational named snippet receiving { state, initials }. No focusable content."
			},
			imageProps: {
				control: false,
				description:
					"Native srcset, sizes, loading, decoding, fetchpriority, crossorigin, referrerpolicy, onload, onerror. Defaults: eager, async, auto, no-referrer, no crossorigin. Callbacks receive actual DOM events, including currentTarget; cached completion before hydration does not synthesize an event."
			}
		}
	});
</script>

<script lang="ts">
	import Building2 from "@lucide/svelte/icons/building-2";
	import Badge from "$lib/components/ui/Badge.svelte";
	import Button from "$lib/components/ui/Button.svelte";
	import {
		DropdownMenuRoot,
		DropdownMenuTrigger,
		DropdownMenuContent,
		DropdownMenuItem
	} from "$lib/components/ui/dropdown-menu";

	let selectedSource = $state("portraitA");
	let imageVersion = $state(0);
	let loadCount = $state(0);
	let errorCount = $state(0);
	let lastImageEvent = $state("None");
	let activationCount = $state(0);
	let lastAction = $state("None");
</script>

{#snippet sample(label: string, src?: string | null, initials?: string, size: AvatarSize = "default")}
	<div class="story-sample">
		<Avatar {src} {initials} {size} alt={label} />
		<span class="story-caption">{label}</span>
	</div>
{/snippet}

<Story
	name="Playground"
	args={{ src: "portraitA", alt: "Label A", initials: "LA", size: "default", decorative: false }}
	parameters={{ controls: { disable: false, include: ["src", "size", "initials", "alt", "decorative"] } }}
>
	{#snippet template(args)}
		<div class="story-stack">
			<div class="story-identity">
				{#if args.decorative}
					<Avatar src={args.src} size={args.size} initials={args.initials} decorative />
					<span>Label A</span>
				{:else}
					<Avatar src={args.src} size={args.size} initials={args.initials} alt={args.alt ?? "Label A"} />
				{/if}
			</div>
			<p class="story-note">
				Explore the public props with Controls. Decorative mode adds visible identity text and omits alt.
			</p>
		</div>
	{/snippet}
</Story>

<Story name="Appearance" asChild>
	<div class="story-stack">
		{#each sizes as size (size)}
			<section class="story-section">
				<h2 class="story-label">{size}</h2>
				<div class="story-row">
					{@render sample("Image A", portraitA, undefined, size)}
					{@render sample("Image B", portraitB, undefined, size)}
					{@render sample("Initials", undefined, "LA", size)}
					{@render sample("Placeholder", undefined, undefined, size)}
				</div>
			</section>
		{/each}
		<section class="story-section">
			<h2 class="story-label">Custom content and surfaces</h2>
			<div class="story-row">
				<div class="story-sample">
					<Avatar alt="Label B" initials="LB" size="large">
						{#snippet fallback({ state, initials })}<Building2
								aria-hidden="true"
								data-image-state={state}
								data-initials={initials}
							/>{/snippet}
					</Avatar>
					<span class="story-caption">Snippet before initials</span>
				</div>
				{@render sample("Transparent image", transparentImage, undefined, "large")}
				<div class="story-surface story-sample">
					<Avatar initials="LA" size="large" decorative /><span class="story-caption">Secondary surface</span>
				</div>
			</div>
		</section>
		<p class="story-note">
			Use the light/dark toolbar control to inspect both themes. Images keep their colors and use a centered cover crop, a
			circular outline, a neutral surface, and a 60% placeholder. No loading animation or image fade.
		</p>
	</div>
</Story>

<Story name="Image Behavior" asChild>
	<div class="story-stack">
		<section class="story-section">
			<h2 class="story-label">Source changes and native callbacks</h2>
			<div class="story-row story-controls">
				<label class="story-field">
					<span>Image source</span>
					<select bind:value={selectedSource}>
						{#each sources as source (source.value)}<option value={source.value}>{source.label}</option>{/each}
					</select>
				</label>
				<Button onclick={() => (imageVersion += 1)}>Remount image</Button>
			</div>
			<div class="story-identity">
				{#key imageVersion}
					<Avatar
						src={sourceFor(selectedSource)}
						alt="Label A"
						initials="LA"
						size="large"
						imageProps={{
							onload: (event) => {
								loadCount += 1;
								lastImageEvent = `${event.type} · ${event.currentTarget.tagName}`;
							},
							onerror: (event) => {
								errorCount += 1;
								lastImageEvent = `${event.type} · ${event.currentTarget.tagName}`;
							}
						}}
					/>
				{/key}
				<span>Label A</span>
			</div>
			<dl class="story-register">
				<dt>Load events</dt>
				<dd>{loadCount}</dd>
				<dt>Error events</dt>
				<dd>{errorCount}</dd>
				<dt>Last image event</dt>
				<dd>{lastImageEvent}</dd>
			</dl>
			<p class="story-note">
				Choose A → B → A, Invalid image, and None. For visible loading and in-flight changes, disable cache and throttle the
				network in browser developer tools. The fallback appears immediately; inspect data-state for idle, loading, loaded, or
				error. Removed image instances cannot update the current result. Cached completion before hydration updates state
				without a synthetic callback.
			</p>
		</section>
		<section class="story-section">
			<h2 class="story-label">Missing and invalid sources</h2>
			<div class="story-row">
				{@render sample("Undefined", undefined, "LA")}
				{@render sample("Null", null, "LA")}
				{@render sample("Empty", "", "LA")}
				{@render sample("Whitespace", "   ", "LA")}
				{@render sample("Invalid", invalidImage, "LA")}
			</div>
		</section>
		<section class="story-section">
			<h2 class="story-label">Native image options</h2>
			<Avatar
				src={portraitA}
				alt="Label A"
				initials="LA"
				imageProps={{
					srcset: `${portraitA} 256w`,
					sizes: "40px",
					loading: "lazy",
					decoding: "async",
					fetchpriority: "auto",
					crossorigin: "anonymous",
					referrerpolicy: "no-referrer"
				}}
			/>
			<p class="story-note">
				Native srcset and sizes select the image candidate. This example also forwards loading, decoding, fetchpriority,
				crossorigin, and referrerpolicy. No preload element, timer, or separate image request is created.
			</p>
		</section>
	</div>
</Story>

<Story name="Composition" asChild>
	<div class="story-stack">
		<section class="story-section">
			<h2 class="story-label">Informative and decorative</h2>
			<div class="story-row">
				<div class="story-sample">
					<Avatar src={portraitA} alt="Label A" aria-describedby="avatar-name-note" />
					<span class="story-caption">Informative image</span>
				</div>
				<div class="story-identity"><Avatar initials="LB" decorative /><span>Label B</span></div>
			</div>
			<p id="avatar-name-note" class="story-note">
				An informative avatar is one named image, including when fallback content is shown. Beside an existing visible identity,
				decorative avoids a repeated announcement.
			</p>
		</section>
		<section class="story-section">
			<h2 class="story-label">Actions belong to the wrapper</h2>
			<div class="story-row">
				<div class="story-sample">
					<Button class="story-avatar-action" aria-label="Open details for Label A" onclick={() => (activationCount += 1)}
						><Avatar src={portraitA} size="compact" decorative /></Button
					>
					<span class="story-caption">Button</span>
				</div>
				<div class="story-sample">
					<Button class="story-avatar-action" href="#avatar-link-destination" aria-label="View Label B"
						><Avatar src={portraitB} size="compact" decorative /></Button
					>
					<span class="story-caption">Link</span>
				</div>
				<div class="story-sample">
					<DropdownMenuRoot>
						<DropdownMenuTrigger class="story-avatar-action" aria-label="Open options for Label A"
							><Avatar src={portraitA} size="compact" decorative /></DropdownMenuTrigger
						>
						<DropdownMenuContent>
							<DropdownMenuItem onclick={() => (lastAction = "Action A")}>Action A</DropdownMenuItem>
							<DropdownMenuItem onclick={() => (lastAction = "Action B")}>Action B</DropdownMenuItem>
						</DropdownMenuContent>
					</DropdownMenuRoot>
					<span class="story-caption">Menu trigger</span>
				</div>
				<div class="story-sample">
					<Button class="story-avatar-action" disabled aria-label="Unavailable action for Label A"
						><Avatar initials="LA" size="compact" decorative /></Button
					>
					<span class="story-caption">Disabled</span>
				</div>
			</div>
			<dl class="story-register">
				<dt>Button activations</dt>
				<dd>{activationCount}</dd>
				<dt>Last menu action</dt>
				<dd>{lastAction}</dd>
			</dl>
			<p class="story-note">
				Inspect hover and the 3px focus outline with Tab. Buttons support Enter and Space; links support Enter. Use ArrowDown to
				open the menu, arrows to move, Enter to select, and Escape to close. Wrappers provide at least 44 × 44px targets,
				accessible action names, and disabled behavior.
			</p>
			<div id="avatar-link-destination" class="story-destination">Link destination</div>
		</section>
	</div>
</Story>

<Story name="Content and Layout" asChild>
	<div class="story-stack">
		<section class="story-section">
			<h2 class="story-label">Initials and fallback content</h2>
			<div class="story-row">
				{@render sample("One initial", undefined, "L")}
				{@render sample("CJK", undefined, "山田", "compact")}
				{@render sample("Combined", undefined, "A\u0301B", "compact")}
				{@render sample("Emoji", undefined, "👩‍💻")}
				{@render sample("Whitespace", undefined, "   ")}
				{@render sample("Too long", undefined, "LONG LABEL")}
			</div>
			<p class="story-note">
				Provide one or two grapheme clusters. Excess content stays inside the circle. Initials are not the accessible name; alt
				retains the complete identity.
			</p>
		</section>
		<section class="story-section story-constrained">
			<h2 class="story-label">Narrow containers</h2>
			<ul class="story-list">
				<li>
					<Avatar src={portraitA} decorative />
					<div class="story-text">
						<p>Label A</p>
						<p class="story-note">Supporting text</p>
					</div>
					<Badge size="compact">Label</Badge>
				</li>
				<li>
					<Avatar initials="LB" decorative />
					<div class="story-text">
						<p>Long label with enough room to wrap on a small screen</p>
						<p class="story-note">Supporting text also wraps</p>
					</div>
				</li>
				<li>
					<Avatar src={invalidImage} initials="LC" decorative />
					<div class="story-text">
						<p>Label C</p>
						<p class="story-note">Image unavailable; identity retained</p>
					</div>
				</li>
			</ul>
			<div class="story-identity story-narrow">
				<Avatar initials="LA" decorative />
				<div class="story-text"><p>LongUnbrokenLabelWithoutSpacesThatStillWraps</p></div>
			</div>
			<div class="story-identity" dir="rtl">
				<Avatar initials="مث" decorative />
				<div class="story-text"><p>تسمية طويلة تلتف داخل المساحة المتاحة</p></div>
			</div>
			<Avatar
				alt="A deliberately long accessible identity label that stays complete without changing the image dimensions"
				initials="LA"
				title="Native title"
				data-example="native-attributes"
			/>
		</section>
		<section class="story-section">
			<h2 class="story-label">200% font sizing</h2>
			<div class="story-row">
				{#each sizes as size (size)}
					<div class="story-sample">
						<Avatar
							alt="Label A"
							initials="山田"
							{size}
							style={`font-size: ${size === "compact" ? 1.5 : size === "large" ? 2.5 : 1.75}rem`}
						/>
						<span class="story-caption">{size}</span>
					</div>
				{/each}
			</div>
			<p class="story-note">
				This exercise doubles the computed font size with a native style attribute. Also inspect real browser text enlargement,
				reduced motion, 320px reflow, and 400% browser zoom from 1280px. Both dimensions grow together to retain the circular
				shape.
			</p>
		</section>
	</div>
</Story>

<style>
	.story-stack {
		display: grid;
		gap: 2rem;
		max-inline-size: 48rem;
	}
	.story-section {
		display: grid;
		gap: 1rem;
		border-block-start: 1px solid var(--border-trace);
		padding-block-start: 1rem;
	}
	.story-row {
		display: flex;
		flex-wrap: wrap;
		align-items: start;
		gap: 1.5rem;
	}
	.story-sample {
		display: grid;
		justify-items: start;
		align-content: start;
		gap: 0.75rem;
	}
	.story-caption,
	.story-note {
		color: var(--marginal-note);
		font-family: var(--font-interface);
		font-size: 0.875rem;
		line-height: 1.5;
	}
	.story-caption {
		max-inline-size: 9rem;
		overflow-wrap: anywhere;
	}
	.story-note {
		max-inline-size: 40rem;
		margin: 0;
		overflow-wrap: anywhere;
	}
	.story-label {
		margin: 0;
		color: var(--marginal-note);
		font-family: var(--font-interface);
		font-size: 0.75rem;
		font-weight: 600;
		line-height: 1.5;
		letter-spacing: 0.08em;
		text-transform: uppercase;
	}
	.story-identity {
		display: flex;
		align-items: center;
		gap: 0.75rem;
	}
	.story-text {
		min-inline-size: 0;
		flex: 1;
		overflow-wrap: anywhere;
	}
	.story-text p {
		margin: 0;
	}
	.story-text .story-note {
		margin-block-start: 0.25rem;
	}
	.story-constrained {
		inline-size: 100%;
		max-inline-size: 20rem;
	}
	.story-narrow {
		max-inline-size: 12rem;
	}
	.story-controls {
		align-items: end;
	}
	.story-field {
		display: grid;
		gap: 0.5rem;
		inline-size: 100%;
		max-inline-size: 16rem;
		font-size: 0.875rem;
		font-weight: 600;
	}
	.story-field select {
		min-block-size: 2.75rem;
		min-inline-size: 0;
		border: 1px solid var(--border-trace);
		border-radius: var(--radius-control);
		background: var(--archive-layer);
		padding: 0.5rem 0.75rem;
		color: var(--reading-ink);
		font: inherit;
	}
	.story-register {
		display: grid;
		grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
		gap: 0.5rem 1rem;
		max-inline-size: 24rem;
		margin: 0;
		border-block-start: 1px solid var(--border-trace);
		padding-block-start: 1rem;
		font-size: 0.875rem;
	}
	.story-register dt {
		color: var(--marginal-note);
	}
	.story-register dd {
		margin: 0;
		font-variant-numeric: tabular-nums;
		overflow-wrap: anywhere;
	}
	.story-destination {
		border-block-start: 1px solid var(--border-trace);
		padding-block-start: 1rem;
		font-size: 0.875rem;
	}
	.story-list {
		display: grid;
		gap: 1rem;
		margin: 0;
		padding: 0;
		list-style: none;
	}
	.story-list li {
		display: flex;
		flex-wrap: wrap;
		align-items: start;
		gap: 0.75rem;
		border-block-end: 1px solid var(--border-trace);
		padding-block-end: 1rem;
	}
	.story-list .story-text {
		flex-basis: 6rem;
	}
	.story-surface {
		background: var(--archive-layer);
		padding: 1rem;
	}
	:global(.story-avatar-action[data-component="button"][data-size]) {
		min-inline-size: 2.75rem;
		min-block-size: 2.75rem;
		padding: 0.25rem;
		border-color: transparent;
		border-radius: 50%;
	}
</style>
