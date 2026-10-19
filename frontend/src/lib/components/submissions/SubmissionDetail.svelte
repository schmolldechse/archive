<script lang="ts">
	import { untrack, tick } from "svelte";
	import { invalidate } from "$app/navigation";
	import {
		SnapshotModerationStatus as Status,
		type ReviewSnapshotRequest,
		type SnapshotSubmissionDetailResponse,
		type SnapshotHistoryResponse
	} from "$api";
	import { apiUrl } from "$lib/api-url";
	import { processingLabel } from "$lib/submission-status";
	import ArrowLeft from "@lucide/svelte/icons/arrow-left";
	import ArrowRight from "@lucide/svelte/icons/arrow-right";
	import CircleAlert from "@lucide/svelte/icons/circle-alert";
	import Check from "@lucide/svelte/icons/check";
	import X from "@lucide/svelte/icons/x";
	import Button from "$lib/components/ui/Button.svelte";
	import Badge from "$lib/components/ui/Badge.svelte";
	import ArchiveTimestamp from "$lib/components/site/ArchiveTimestamp.svelte";
	import CopySourceButton from "$lib/components/archive/CopySourceButton.svelte";
	import SnapshotTagList from "$lib/components/archive/SnapshotTagList.svelte";
	import { InputDescription, InputLabel, InputRoot, InputTextarea } from "$lib/components/ui/input";
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
	let reviewing = $state(false);
	let reviewDecision = $state<typeof Status.APPROVED | typeof Status.REJECTED | null>(null);
	let reviewReason = $state("");
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
	/** Keeps capture quality visually independent from the snapshot's moderation decision. */
	const captureVariant = $derived.by(() => {
		if (detail.processingStatus === "FAILED" || detail.processingStatus === "ABORTED") return "critical" as const;
		if (detail.processingStatus !== "COMPLETED") return "info" as const;
		return detail.snapshot.quality === "COMPLETE" ? ("success" as const) : ("warning" as const);
	});
	const canResubmit = $derived(
		!data.admin && detail.processingStatus === "COMPLETED" && detail.moderation.status === Status.REJECTED
	);
	const canModerate = $derived(
		data.admin &&
			detail.snapshot.quality !== null &&
			(detail.moderation.status === Status.PENDING || detail.moderation.status === Status.RESUBMITTED)
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

	/** Submits an ETag-protected moderation decision through the existing administrator API. */
	async function submitReview(decision: typeof Status.APPROVED | typeof Status.REJECTED) {
		if (!canModerate || reviewing || saving || reloading || conflict || blocked) return;
		reviewing = true;
		reviewDecision = decision;
		message = "";
		success = "";
		const body: ReviewSnapshotRequest = { decision, reason: reviewReason.trim() || null };
		try {
			const response = await fetch(apiUrl(`${base}/review`), {
				method: "POST",
				credentials: "include",
				cache: "no-store",
				redirect: "error",
				headers: { "Content-Type": "application/json", "If-Match": etag },
				body: JSON.stringify(body)
			});
			if (response.status === 412) {
				conflict = true;
				message = "This snapshot has changed. Reload it before recording a decision.";
				return;
			}
			if (response.status === 401 || response.status === 403) {
				blocked = true;
				message = "Your moderation access has changed. Sign in again or return to the snapshot list.";
				return;
			}
			if (!response.ok || !response.headers.get("etag")) {
				const problem = await response.json().catch(() => null);
				message =
					problem?.detail ??
					(response.status === 409
						? "This snapshot is no longer awaiting a review decision. Reload it to see the current status."
						: "The review decision could not be saved. Please try again.");
				if (response.status === 409) conflict = true;
				return;
			}
			detail = await response.json();
			etag = response.headers.get("etag")!;
			reviewReason = "";
			success =
				decision === Status.APPROVED
					? "Snapshot approved and published in the public archive."
					: "Snapshot rejected and added to its history.";
			await invalidate("app:submissions");
		} catch {
			message = "The review service is temporarily unavailable. Please try again.";
		} finally {
			reviewing = false;
			reviewDecision = null;
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
		<div class="detail-status">
			<SnapshotModerationStatus status={detail.moderation.status} />
			<Badge variant={captureVariant}>{technicalLabel}</Badge>
		</div>
		{#if detail.snapshot.sourceUrl}<div class="detail-source">
				<a href={detail.snapshot.sourceUrl} target="_blank" rel="noreferrer" title={detail.snapshot.sourceUrl}
					>{detail.snapshot.sourceUrl}</a
				><CopySourceButton value={detail.snapshot.sourceUrl} label={`Copy source URL for ${detail.snapshot.title}`} />
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
						<dd>
							<Badge variant={detail.snapshot.quality === "COMPLETE" ? "success" : "warning"} size="compact"
								>{detail.snapshot.quality === "COMPLETE" ? "Complete" : "Incomplete"}</Badge
							>
						</dd>
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
				<div class="record-tags">
					<SnapshotTagList tags={detail.snapshot.tags} />
				</div>{/if}
			{#if detail.snapshot.contentUrl}<Button href={`/snapshots/${detail.snapshot.id}`}
					>Open archived page<ArrowRight aria-hidden="true" /></Button
				>{/if}
		</section>
		{#if detail.snapshot.screenshotUrl}<div class="detail-preview">
				<h2>Screenshot preview</h2>
				{#key detail.snapshot.screenshotUrl}<PrivateSnapshotPreview
						path={detail.snapshot.screenshotUrl}
						title={detail.snapshot.title}
					/>{/key}
			</div>{/if}
	</div>
	{#if canModerate}<section class="review-actions" aria-labelledby="review-actions-title">
			<div class="review-actions__intro">
				<h2 id="review-actions-title">Review this snapshot</h2>
				<p>Approval publishes this capture in the public archive. Rejection records a decision in the snapshot history.</p>
			</div>
			<div class="review-actions__form">
				<InputRoot id="snapshot-review-note" hasDescription disabled={reviewing || reloading}>
					<InputLabel>Decision note <span>(optional)</span></InputLabel>
					<InputTextarea bind:value={reviewReason} maxlength={2048} rows={4} placeholder="Add context for the submitter." />
					<InputDescription>Visible to the submitter and moderators. Maximum 2,048 characters.</InputDescription>
				</InputRoot>
				<div class="review-actions__buttons">
					<Button
						variant="secondary"
						class="review-reject"
						onclick={() => submitReview(Status.REJECTED)}
						disabled={reviewing || conflict || blocked || reloading || saving}
						loading={reviewing && reviewDecision === Status.REJECTED}
						loadingLabel="Rejecting snapshot"><X aria-hidden="true" />Reject snapshot</Button
					>
					<Button
						variant="primary"
						onclick={() => submitReview(Status.APPROVED)}
						disabled={reviewing || conflict || blocked || reloading || saving}
						loading={reviewing && reviewDecision === Status.APPROVED}
						loadingLabel="Approving snapshot"><Check aria-hidden="true" />Approve snapshot</Button
					>
				</div>
			</div>
		</section>{/if}
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
	.detail-source {
		display: grid;
		grid-template-columns: minmax(0, 1fr) 2.75rem;
		align-items: center;
		gap: 0.5rem;
		width: fit-content;
		max-width: 100%;
	}
	.detail-source a {
		min-width: 0;
		font: 400 0.8125rem/1.55 var(--font-record);
		overflow-wrap: anywhere;
		word-break: break-word;
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
		margin-bottom: 1.5rem;
	}
	.detail-preview {
		min-width: 0;
	}
	.detail-preview > h2 {
		margin-bottom: 1.25rem;
	}
	.review-actions {
		display: grid;
		grid-template-columns: minmax(14rem, 0.8fr) minmax(0, 1.2fr);
		align-items: start;
		gap: 2rem;
		border-block: 1px solid var(--border-trace);
		padding-block: 2rem;
	}
	.review-actions__intro > h2 {
		margin-bottom: 0.75rem;
	}
	.review-actions__intro > p {
		max-width: 40ch;
		margin: 0;
		color: var(--marginal-note);
		font-size: 0.9375rem;
	}
	.review-actions__form {
		display: grid;
		gap: 1rem;
	}
	.review-actions__buttons {
		display: flex;
		flex-wrap: wrap;
		gap: 0.75rem;
	}
	.review-actions__buttons :global(.review-reject) {
		border-color: var(--time-marker);
		color: var(--time-marker);
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
		.review-actions,
		.resubmit-section {
			grid-template-columns: 1fr;
			gap: 1.5rem;
		}
		.detail-preview {
			grid-row: 1;
		}
	}
</style>
