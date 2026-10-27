<script lang="ts">
	import Minus from "@lucide/svelte/icons/minus";
	import Plus from "@lucide/svelte/icons/plus";
	import Scan from "@lucide/svelte/icons/scan";
	import CopySourceButton from "$lib/components/archive/CopySourceButton.svelte";
	import Button from "$lib/components/ui/Button.svelte";
	import { ToggleGroupItem, ToggleGroupRoot } from "$lib/components/ui/toggle-group";
	import type { SnapshotPageTreeResponse } from "$api";
	import { Chart } from "layerchart";
	import { Tree } from "layerchart/hierarchy";
	import { Svg } from "layerchart/svg";
	import { hierarchy, type HierarchyPointLink, type HierarchyPointNode } from "d3-hierarchy";
	import type { ChartState } from "layerchart";
	import { onMount } from "svelte";
	import { buildSnapshotPageTree, pageDisplayTitle, pageRouteLabel, type SnapshotPageNode } from "./snapshot-page-tree";

	let { tree }: { tree: SnapshotPageTreeResponse } = $props();
	const model = $derived(buildSnapshotPageTree(tree));
	const chartHierarchy = $derived(model ? hierarchy(model.root, (node) => node.children) : null);
	let selectedPath = $state<string | null>(null);
	let view = $state<"diagram" | "outline">("diagram");
	let chartContext = $state<ChartState<SnapshotPageNode> | undefined>();
	const currentSelectedPath = $derived(selectedPath ?? model?.root.page.path ?? "");
	const selected = $derived(model?.nodes.find((node) => node.page.path === currentSelectedPath) ?? model?.root ?? null);

	const linkPath = (link: HierarchyPointLink<SnapshotPageNode>): string => {
		const midpoint = (link.source.y + link.target.y) / 2;
		return `M ${link.source.y} ${link.source.x} C ${midpoint} ${link.source.x}, ${midpoint} ${link.target.x}, ${link.target.y} ${link.target.x}`;
	};

	const selectNode = (node: SnapshotPageNode): void => {
		selectedPath = node.page.path;
	};

	const nodeLabel = (node: HierarchyPointNode<SnapshotPageNode>): string => {
		const prefix = node.depth === 0 ? "Root page" : `Page at depth ${node.depth}`;
		return `${prefix}: ${pageDisplayTitle(node.data.page)}. ${pageRouteLabel(node.data.page.sourceUrl)}. ${node.data.children.length} child pages.`;
	};

	onMount(() => {
		if (window.matchMedia("(max-width: 42rem)").matches) {
			view = "outline";
		}
	});
</script>

{#if model && chartHierarchy}
	<section class="page-tree" aria-label={`Captured page tree with ${model.nodes.length} pages`}>
		<div class="page-tree__toolbar">
			<div class="page-tree__summary">
				<strong>{model.nodes.length.toLocaleString()} captured {model.nodes.length === 1 ? "page" : "pages"}</strong>
				<span>{model.maxDepth === 0 ? "Root only" : `${model.maxDepth} ${model.maxDepth === 1 ? "level" : "levels"} deep`}</span
				>
			</div>
			<div class="page-tree__controls">
				{#if view === "diagram"}
					<div class="page-tree__zoom" role="group" aria-label="Diagram zoom">
						<Button
							variant="secondary"
							aria-label="Zoom out"
							title="Zoom out"
							onclick={() => chartContext?.transform.zoomOut()}
						>
							<Minus aria-hidden="true" />
						</Button>
						<Button variant="secondary" aria-label="Zoom in" title="Zoom in" onclick={() => chartContext?.transform.zoomIn()}>
							<Plus aria-hidden="true" />
						</Button>
						<Button
							variant="secondary"
							aria-label="Fit all pages"
							title="Fit all pages"
							onclick={() => chartContext?.transform.reset()}
						>
							<Scan aria-hidden="true" />
						</Button>
					</div>
				{/if}
				<ToggleGroupRoot type="single" bind:value={view} selectionRequired aria-label="Page tree view">
					<ToggleGroupItem value="diagram">Diagram</ToggleGroupItem>
					<ToggleGroupItem value="outline">Outline</ToggleGroupItem>
				</ToggleGroupRoot>
			</div>
		</div>

		<div class:hidden={view !== "diagram"} class="page-tree__diagram" aria-label="Interactive page tree diagram">
			<Chart
				data={chartHierarchy}
				bind:context={chartContext}
				padding={{ top: 42, right: 94, bottom: 42, left: 94 }}
				transform={{ mode: "canvas", scrollMode: "scale", scaleExtent: [0.6, 2.4], inertia: true }}
			>
				<Svg title="Captured pages arranged by first-discovery parent">
					<Tree hierarchy={chartHierarchy} orientation="horizontal">
						{#snippet children({ nodes, links })}
							<g class="page-tree__links" aria-hidden="true">
								{#each links as link (link.target.data.page.path)}
									<path d={linkPath(link)} />
								{/each}
							</g>
							{#each nodes as node (node.data.page.path)}
								<foreignObject x={node.y - 82} y={node.x - 30} width="164" height="60">
									<button
										type="button"
										class:root-node={node.depth === 0}
										class:selected-node={currentSelectedPath === node.data.page.path}
										aria-pressed={currentSelectedPath === node.data.page.path}
										aria-label={nodeLabel(node)}
										onclick={() => selectNode(node.data)}
									>
										<span class="page-tree__node-kicker">{node.depth === 0 ? "Root" : `Depth ${node.depth}`}</span>
										<span class="page-tree__node-route">{pageRouteLabel(node.data.page.sourceUrl)}</span>
									</button>
								</foreignObject>
							{/each}
						{/snippet}
					</Tree>
				</Svg>
			</Chart>
			<p class="page-tree__gesture-note">Drag to pan. Scroll or pinch to zoom.</p>
		</div>

		<div class:hidden={view !== "outline"} class="page-tree__outline">
			<ul>
				{#each model.nodes as node (node.page.path)}
					<li style={`--tree-depth: ${node.depth}`}>
						<button type="button" aria-pressed={currentSelectedPath === node.page.path} onclick={() => selectNode(node)}>
							<span class="page-tree__outline-marker" aria-hidden="true"></span>
							<span>
								<strong>{pageDisplayTitle(node.page)}</strong>
								<small>{node.depth === 0 ? "Root" : `Depth ${node.depth}`} · {pageRouteLabel(node.page.sourceUrl)}</small>
							</span>
						</button>
					</li>
				{/each}
			</ul>
		</div>

		{#if selected}
			<div class="page-tree__selection" aria-live="polite">
				<p class="page-tree__selection-label">Selected page</p>
				<h3>{pageDisplayTitle(selected.page)}</h3>
				<div class="page-tree__source">
					<a href={selected.page.sourceUrl} target="_blank" rel="noreferrer">{selected.page.sourceUrl}</a>
					<CopySourceButton value={selected.page.sourceUrl} label={`Copy source URL for ${pageDisplayTitle(selected.page)}`} />
				</div>
				<dl>
					<div>
						<dt>Archived path</dt>
						<dd>{selected.page.path}</dd>
					</div>
					<div>
						<dt>Depth</dt>
						<dd>{selected.depth}</dd>
					</div>
					<div>
						<dt>Child pages</dt>
						<dd>{selected.children.length}</dd>
					</div>
				</dl>
			</div>
		{/if}
	</section>
{:else}
	<div class="page-tree__invalid" role="status">The captured page hierarchy could not be displayed.</div>
{/if}

<style>
	.page-tree {
		border: 1px solid var(--border-trace);
		background: var(--reading-room);
	}
	.page-tree__toolbar {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 1rem;
		border-bottom: 1px solid var(--border-trace);
		padding: 0.75rem;
	}
	.page-tree__summary {
		display: grid;
		gap: 0.125rem;
	}
	.page-tree__summary strong {
		font-size: 0.875rem;
		font-weight: 600;
	}
	.page-tree__summary span,
	.page-tree__gesture-note {
		color: var(--marginal-note);
		font-size: 0.75rem;
	}
	.page-tree__controls,
	.page-tree__zoom {
		display: flex;
		align-items: center;
		gap: 0.5rem;
	}
	.page-tree__zoom :global(.button) {
		width: 2.75rem;
		padding-inline: 0;
	}
	.page-tree__zoom :global(svg) {
		width: 1.125rem;
		height: 1.125rem;
	}
	.page-tree__diagram {
		position: relative;
		height: clamp(20rem, 52vh, 34rem);
		min-height: 20rem;
		background:
			linear-gradient(var(--border-trace) 1px, transparent 1px),
			linear-gradient(90deg, var(--border-trace) 1px, transparent 1px);
		background-size: 2rem 2rem;
		background-position: -1px -1px;
		cursor: grab;
		overscroll-behavior: contain;
		touch-action: pan-y;
	}
	.page-tree__diagram:active {
		cursor: grabbing;
	}
	.page-tree__diagram :global([data-chart]) {
		height: 100%;
	}
	.page-tree__links path {
		fill: none;
		stroke: var(--border-trace);
		stroke-width: 1.5;
		vector-effect: non-scaling-stroke;
	}
	foreignObject {
		overflow: visible;
	}
	foreignObject button {
		display: grid;
		width: 100%;
		height: 100%;
		box-sizing: border-box;
		align-content: center;
		gap: 0.125rem;
		border: 1px solid var(--register-mark);
		border-inline-start: 3px solid var(--register-mark);
		border-radius: 0;
		background: var(--reading-room);
		padding: 0.5rem 0.625rem;
		color: var(--reading-ink);
		text-align: left;
		cursor: pointer;
		box-shadow: 0 1px 0 color-mix(in srgb, var(--reading-ink) 8%, transparent);
		transition: 150ms ease;
	}
	foreignObject button:hover,
	foreignObject button:focus-visible,
	foreignObject button.selected-node {
		border-color: var(--reading-ink);
		box-shadow:
			inset 0 -3px 0 var(--register-mark),
			var(--overlay-shadow);
	}
	foreignObject button.root-node {
		border-color: var(--time-marker);
		border-inline-start-color: var(--time-marker);
	}
	foreignObject button.root-node.selected-node {
		box-shadow:
			inset 0 -3px 0 var(--time-marker),
			var(--overlay-shadow);
	}
	.page-tree__node-kicker {
		color: var(--marginal-note);
		font-size: 0.625rem;
		font-weight: 700;
		letter-spacing: 0.08em;
		text-transform: uppercase;
	}
	.page-tree__node-route {
		display: block;
		min-width: 0;
		font: 500 0.75rem/1.3 var(--font-record);
		white-space: nowrap;
		text-overflow: ellipsis;
		overflow: hidden;
	}
	.page-tree__gesture-note {
		position: absolute;
		right: 0.75rem;
		bottom: 0.5rem;
		margin: 0;
		background: color-mix(in srgb, var(--reading-room) 88%, transparent);
		padding: 0.25rem 0.375rem;
	}
	.page-tree__outline {
		max-height: 30rem;
		overflow: auto;
		padding: 0.75rem;
	}
	.page-tree__outline ul {
		display: grid;
		gap: 0.375rem;
		margin: 0;
		padding: 0;
		list-style: none;
	}
	.page-tree__outline li {
		padding-inline-start: min(calc(var(--tree-depth) * 1.5rem), 9rem);
	}
	.page-tree__outline button {
		display: grid;
		grid-template-columns: 0.75rem minmax(0, 1fr);
		align-items: start;
		gap: 0.625rem;
		width: 100%;
		border: 1px solid transparent;
		border-radius: 0;
		background: transparent;
		padding: 0.625rem;
		color: var(--reading-ink);
		text-align: left;
		cursor: pointer;
	}
	.page-tree__outline button:hover,
	.page-tree__outline button:focus-visible,
	.page-tree__outline button[aria-pressed="true"] {
		border-color: var(--border-trace);
		background: var(--archive-layer);
	}
	.page-tree__outline button[aria-pressed="true"] {
		box-shadow: inset 3px 0 0 var(--register-mark);
	}
	.page-tree__outline-marker {
		width: 0.625rem;
		height: 0.625rem;
		margin-top: 0.25rem;
		border: 2px solid var(--register-mark);
		background: var(--reading-room);
	}
	.page-tree__outline li:first-child .page-tree__outline-marker {
		border-color: var(--time-marker);
		border-radius: 50%;
	}
	.page-tree__outline strong,
	.page-tree__outline small {
		display: block;
		overflow-wrap: anywhere;
	}
	.page-tree__outline strong {
		font-size: 0.875rem;
	}
	.page-tree__outline small {
		margin-top: 0.125rem;
		color: var(--marginal-note);
		font: 400 0.75rem/1.4 var(--font-record);
	}
	.page-tree__selection {
		border-top: 1px solid var(--border-trace);
		padding: 1rem;
	}
	.page-tree__selection-label {
		margin: 0 0 0.375rem;
		color: var(--marginal-note);
		font-size: 0.6875rem;
		font-weight: 700;
		letter-spacing: 0.08em;
		text-transform: uppercase;
	}
	.page-tree__selection h3 {
		margin: 0;
		font: 500 1.25rem/1.3 var(--font-editorial);
		overflow-wrap: anywhere;
	}
	.page-tree__source {
		display: grid;
		grid-template-columns: minmax(0, 1fr) 2.75rem;
		align-items: center;
		gap: 0.5rem;
		max-width: 52rem;
		margin-top: 0.375rem;
	}
	.page-tree__source a {
		font: 400 0.75rem/1.5 var(--font-record);
		overflow-wrap: anywhere;
	}
	.page-tree__selection dl {
		display: flex;
		flex-wrap: wrap;
		gap: 0.75rem 2rem;
		margin: 1rem 0 0;
	}
	.page-tree__selection dl > div {
		min-width: 8rem;
	}
	.page-tree__selection dt {
		color: var(--marginal-note);
		font-size: 0.6875rem;
	}
	.page-tree__selection dd {
		margin: 0.125rem 0 0;
		font: 400 0.75rem/1.4 var(--font-record);
		overflow-wrap: anywhere;
	}
	.page-tree__invalid {
		border-inline-start: 3px solid var(--warning-ochre);
		background: color-mix(in srgb, var(--warning-ochre) 9%, transparent);
		padding: 1rem;
		font-size: 0.875rem;
	}
	.hidden {
		display: none;
	}
	@media (max-width: 42rem) {
		.page-tree__toolbar,
		.page-tree__controls {
			align-items: stretch;
			flex-direction: column;
		}
		.page-tree__zoom {
			justify-content: flex-end;
		}
		.page-tree__diagram {
			height: 24rem;
		}
		.page-tree__outline li {
			padding-inline-start: min(calc(var(--tree-depth) * 0.75rem), 3rem);
		}
	}
	@media (prefers-reduced-motion: reduce) {
		foreignObject button {
			transition: none;
		}
	}
</style>
