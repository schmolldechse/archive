<script lang="ts">
	import { untrack, tick } from "svelte";
	import { invalidate } from "$app/navigation";
	import {
		SnapshotModerationStatus as Status,
		type SnapshotSubmissionDetailResponse,
		type SnapshotHistoryResponse
	} from "$api";
	import { apiUrl } from "$lib/api-url";
	import { processingLabel } from "$lib/submission-status";
	import ArrowLeft from "@lucide/svelte/icons/arrow-left";
	import ArrowRight from "@lucide/svelte/icons/arrow-right";
	import CircleAlert from "@lucide/svelte/icons/circle-alert";
	import Check from "@lucide/svelte/icons/check";
	import Button from "$lib/components/ui/Button.svelte";
	import ArchiveTimestamp from "$lib/components/site/ArchiveTimestamp.svelte";
	import CopySourceButton from "$lib/components/archive/CopySourceButton.svelte";
	import SnapshotModerationStatus from "./SnapshotModerationStatus.svelte";
	import SnapshotHistory from "./SnapshotHistory.svelte";
	import PrivateSnapshotPreview from "./PrivateSnapshotPreview.svelte";
	let {
		data
	}: {
		data: {
			detail: SnapshotSubmissionDetailResponse;
			etag: string;
			history: SnapshotHistoryResponse;
			admin: boolean;
		};
	} = $props();
	let detail = $state.raw(untrack(() => data.detail));
	let etag = $state(untrack(() => data.etag));
	let saving = $state(false);
	let reloading = $state(false);
	let message = $state("");
	let success = $state("");
	let conflict = $state(false);
	let blocked = $state(false);
	let errorNotice = $state<HTMLDivElement | null>(null);
	let successNotice = $state<HTMLParagraphElement | null>(null);
	const base = $derived(`/api/${data.admin ? "admin" : "users"}/snapshots/${detail.snapshot.id}`);
	const backHref = $derived(data.admin ? "/admin/snapshots" : "/account/submissions");
	const technicalLabel = $derived(
		detail.processingStatus === "COMPLETED" && detail.snapshot.quality === "INCOMPLETE"
			? "Incomplete capture"
			: processingLabel(detail.processingStatus)
	);
	const isPublished = $derived(detail.processingStatus === "COMPLETED" && detail.moderation.status === Status.APPROVED);
	const canResubmit = $derived(
		!data.admin && detail.processingStatus === "COMPLETED" && detail.moderation.status === Status.REJECTED
	);

	/** Updates the local record only after the server acknowledges the renewed review request. */
	async function save() {
		if (saving || reloading || conflict || blocked) return;
		saving = true;
		message = "";
		success = "";
		try {
			const response = await fetch(apiUrl(`${base}/resubmit`), {
				method: "POST",
				credentials: "include",
				cache: "no-store",
				redirect: "error",
				headers: { "If-Match": etag }
			});
			if (response.status === 412) {
				conflict = true;
				message = "This snapshot has changed. Reload it before requesting another review.";
				return;
			}
			if (response.status === 401 || response.status === 403) {
				blocked = true;
				message = "Your access has changed. Sign in again or return to the archive.";
				return;
			}
			if (!response.ok || !response.headers.get("etag")) {
				const problem = await response.json().catch(() => null);
				message = problem?.detail ?? "The review request could not be submitted. Please try again.";
				if (response.status === 409) conflict = true;
				return;
			}
			detail = await response.json();
			etag = response.headers.get("etag")!;
			success = "Your snapshot has been submitted for review again.";
			await invalidate("app:submissions");
		} catch {
			message = "The submission service is temporarily unavailable. Please try again.";
		} finally {
			saving = false;
			await tick();
			if (message) errorNotice?.focus();
			else if (success) successNotice?.focus();
		}
	}

	async function reload() {
		reloading = true;
		message = "";
		try {
			const response = await fetch(apiUrl(base), { credentials: "include", cache: "no-store", redirect: "error" });
			if (!response.ok || !response.headers.get("etag")) throw new Error("Reload failed");
			detail = await response.json();
			etag = response.headers.get("etag")!;
			conflict = false;
			await invalidate("app:submissions");
		} catch {
			message = "The current snapshot could not be loaded. Please try again.";
		} finally {
			reloading = false;
		}
	}
</script>

<article class="submission-detail">
	<nav class="detail-path" aria-label="Submission navigation">
		<a href={backHref}><ArrowLeft aria-hidden="true" />{data.admin ? "All snapshots" : "My submissions"}</a>
	</nav>
	<header class="detail-heading">
		<p class="section-label">{data.admin ? "Snapshot record" : "Your submission"}</p>
		<h1>{detail.snapshot.title}</h1>
		<div class="detail-status"><SnapshotModerationStatus status={detail.moderation.status} /><span>{technicalLabel}</span></div>
		{#if detail.snapshot.sourceUrl}<div class="detail-source">
				<a href={detail.snapshot.sourceUrl} target="_blank" rel="noreferrer">{detail.snapshot.sourceUrl}</a><CopySourceButton
					value={detail.snapshot.sourceUrl}
				/>
			</div>{/if}
	</header>
	{#if message}<div bind:this={errorNotice} tabindex="-1" class="detail-message" role="alert">
			<CircleAlert aria-hidden="true" />
			<div>
				<p>{message}</p>
				{#if conflict}<Button onclick={reload} loading={reloading}>Reload snapshot</Button>{/if}
			</div>
		</div>{/if}
	{#if success}<p bind:this={successNotice} tabindex="-1" class="detail-success" role="status">
			<Check aria-hidden="true" />{success}
		</p>{/if}
	<div class="detail-columns">
		<section class="detail-record" aria-labelledby="detail-record-title">
			<h2 id="detail-record-title">Snapshot details</h2>
			<dl>
				{#if data.admin}<div class="snapshot-id">
						<dt>Snapshot ID</dt>
						<dd><code>{detail.snapshot.id}</code></dd>
					</div>{/if}
				<div>
					<dt>Submitted</dt>
					<dd><ArchiveTimestamp value={detail.snapshot.createdAt} /></dd>
				</div>
				<div>
					<dt>Review status updated</dt>
					<dd><ArchiveTimestamp value={detail.moderation.updatedAt} /></dd>
				</div>
				<div>
					<dt>Source type</dt>
					<dd>{detail.snapshot.sourceType}</dd>
				</div>
				{#if detail.snapshot.createdBy}<div>
						<dt>Submitted by</dt>
						<dd>@{detail.snapshot.createdBy.handle}</dd>
					</div>{/if}
				{#if detail.snapshot.quality}<div>
						<dt>Capture quality</dt>
						<dd>{detail.snapshot.quality === "COMPLETE" ? "Complete" : "Incomplete"}</dd>
					</div>{/if}
				{#if detail.snapshot.captureCompletedAt}<div>
						<dt>Capture ended</dt>
						<dd><ArchiveTimestamp value={detail.snapshot.captureCompletedAt} /></dd>
					</div>{/if}
				{#if detail.snapshot.storageBytes !== null}<div>
						<dt>Stored size</dt>
						<dd>{Number(detail.snapshot.storageBytes).toLocaleString()} bytes</dd>
					</div>{/if}
				{#if detail.snapshot.resourceCount !== null}<div>
						<dt>Resources</dt>
						<dd>{Number(detail.snapshot.resourceCount).toLocaleString()}</dd>
					</div>{/if}
			</dl>
			{#if detail.snapshot.description}<h3>Description</h3>
				<p class="record-description">{detail.snapshot.description}</p>{/if}
			{#if detail.snapshot.tags.length}<h3>Tags</h3>
				<ul class="record-tags">
					{#each detail.snapshot.tags as tag (tag)}<li>{tag}</li>{/each}
				</ul>{/if}
			{#if isPublished}<Button href={`/snapshots/${detail.snapshot.id}`}
					>Open archived page<ArrowRight aria-hidden="true" /></Button
				>
			{:else if detail.processingStatus === "COMPLETED"}<p class="private-note">
					This snapshot is available to its owner and moderators. It is not in the public archive.
				</p>
			{:else}<p class="private-note">A successful capture is required before this submission can be reviewed.</p>{/if}
		</section>
		{#if detail.snapshot.screenshotUrl}<div class="detail-preview">
				{#key detail.snapshot.screenshotUrl}<PrivateSnapshotPreview
						path={detail.snapshot.screenshotUrl}
						title={detail.snapshot.title}
					/>{/key}
			</div>{/if}
	</div>
	{#if canResubmit}<section class="resubmit-section" aria-labelledby="resubmit-title">
			<div>
				<h2 id="resubmit-title">Request another review</h2>
				<p>Request another review of this snapshot. The archived content will remain unchanged.</p>
			</div>
			<Button
				variant="primary"
				onclick={save}
				disabled={conflict || blocked || reloading}
				loading={saving}
				loadingLabel="Requesting review">Request another review</Button
			>
		</section>{/if}
	<div class="detail-history"><SnapshotHistory history={data.history} /></div>
</article>

<style>
	.detail-path {
		padding-top: 1.25rem;
	}
	.detail-path a {
		display: inline-flex;
		align-items: center;
		gap: 0.5rem;
		min-height: 2.75rem;
		font-size: 0.875rem;
		color: var(--marginal-note);
	}
	.detail-path :global(svg) {
		width: 1rem;
		height: 1rem;
	}
	.detail-heading {
		padding-block: 1.5rem 2rem;
	}
	.snapshot-id code {
		display: inline-block;
		max-width: 100%;
		font-family: var(--font-record);
		font-size: 0.8125rem;
		overflow-wrap: anywhere;
		word-break: break-word;
	}
	.section-label {
		text-transform: uppercase;
		letter-spacing: 0.08em;
		font-size: 0.75rem;
		font-weight: 600;
		color: var(--marginal-note);
		margin: 0 0 0.75rem;
	}
	h1 {
		font: 500 clamp(2rem, 4vw, 3rem)/1.15 var(--font-editorial);
		letter-spacing: -0.02em;
		margin: 0;
		overflow-wrap: anywhere;
	}
	.detail-status {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: 0.75rem 1.5rem;
		margin-block: 1rem;
	}
	.detail-status > span {
		color: var(--marginal-note);
		font-size: 0.875rem;
	}
	.detail-source {
		display: flex;
		align-items: flex-start;
		gap: 0.5rem;
	}
	.detail-source a {
		min-width: 0;
		font: 400 0.875rem/1.6 var(--font-record);
		overflow-wrap: anywhere;
	}
	.detail-columns {
		display: grid;
		grid-template-columns: minmax(16rem, 0.8fr) minmax(0, 1.2fr);
		gap: 2rem;
		border-top: 1px solid var(--border-trace);
		padding-block: 2rem;
	}
	.detail-record {
		min-width: 0;
	}
	h2 {
		margin: 0 0 1.25rem;
		font-size: 1.75rem;
		font-weight: 500;
	}
	dl {
		margin: 0 0 1.5rem;
		display: grid;
		gap: 1rem;
	}
	dl > div {
		display: grid;
		gap: 0.25rem;
	}
	dt {
		color: var(--marginal-note);
		font-size: 0.8125rem;
	}
	dd {
		margin: 0;
		font-size: 0.875rem;
		overflow-wrap: anywhere;
	}
	h3 {
		font: 600 0.875rem/1.4 var(--font-interface);
		margin: 1.5rem 0 0.5rem;
	}
	.record-description {
		white-space: pre-wrap;
		overflow-wrap: anywhere;
		font-size: 0.9375rem;
	}
	.record-tags {
		display: flex;
		flex-wrap: wrap;
		list-style: none;
		gap: 0.5rem;
		padding: 0;
		font-size: 0.8125rem;
		margin-bottom: 1.5rem;
	}
	.record-tags li {
		background: var(--archive-layer);
		padding: 0.25rem 0.5rem;
		overflow-wrap: anywhere;
	}
	.private-note {
		color: var(--marginal-note);
		font-size: 0.875rem;
		max-width: 45ch;
		margin: 1.5rem 0 0;
	}
	.detail-preview {
		min-width: 0;
	}
	.resubmit-section {
		display: grid;
		grid-template-columns: minmax(14rem, 0.8fr) minmax(0, 1.2fr);
		gap: 2rem;
		align-items: start;
		border-block: 1px solid var(--border-trace);
		padding-block: 2rem;
	}
	.resubmit-section > div > p {
		color: var(--marginal-note);
		font-size: 0.9375rem;
		max-width: 40ch;
	}
	.resubmit-section :global(.button) {
		justify-self: start;
	}
	.detail-history {
		padding-block: 2rem;
	}
	.detail-message {
		display: flex;
		gap: 0.75rem;
		padding: 1rem;
		border: 1px solid var(--time-marker);
		margin-bottom: 1.5rem;
	}
	.detail-message > :global(svg) {
		flex: none;
		color: var(--time-marker);
	}
	.detail-message p {
		margin: 0 0 0.75rem;
		overflow-wrap: anywhere;
	}
	.detail-success {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		color: var(--preservation-green);
		margin-block: 0 1.5rem;
	}
	.detail-success :global(svg) {
		width: 1.25rem;
		height: 1.25rem;
		flex: none;
	}
	@media (max-width: 48rem) {
		.detail-columns,
		.resubmit-section {
			grid-template-columns: 1fr;
			gap: 1.5rem;
		}
		.detail-preview {
			grid-row: 1;
		}
	}
</style>
