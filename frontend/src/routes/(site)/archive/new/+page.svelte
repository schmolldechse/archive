<script lang="ts">
	import { replaceState } from "$app/navigation";
	import { page } from "$app/state";
	import { env } from "$env/dynamic/public";
	import { JobStatus, ProgressStep, type ArchiveResponse } from "$api";
	import ArrowRight from "@lucide/svelte/icons/arrow-right";
	import Check from "@lucide/svelte/icons/check";
	import FileText from "@lucide/svelte/icons/file-text";
	import Globe2 from "@lucide/svelte/icons/globe-2";
	import X from "@lucide/svelte/icons/x";
	import CopySourceButton from "$lib/components/archive/CopySourceButton.svelte";
	import Button from "$lib/components/ui/Button.svelte";
	import Progress from "$lib/components/ui/Progress.svelte";
	import {
		Breadcrumb,
		BreadcrumbItem,
		BreadcrumbLink,
		BreadcrumbList,
		BreadcrumbPage,
		BreadcrumbSeparator
	} from "$lib/components/ui/breadcrumb";
	import { InputControl, InputDescription, InputError, InputLabel, InputRoot, InputTextarea } from "$lib/components/ui/input";
	import { TabsList, TabsPanel, TabsRoot, TabsTrigger } from "$lib/components/ui/tabs";
	import { ToastProvider, ToastViewport, createToastController } from "$lib/components/ui/toast";
	import TagEditor from "$lib/components/tags/TagEditor.svelte";
	import LocalTimestamp from "$lib/components/site/LocalTimestamp.svelte";
	import {
		ArchiveHttpError,
		cancelArchive,
		createArchive,
		getArchive,
		loadArchiveSource,
		saveArchiveSource,
		validateDraft,
		type ArchiveDraft,
		type ArchiveFieldErrors,
		type ArchiveSource
	} from "$lib/archive-submission";
	import { onMount, tick } from "svelte";

	const apiBaseUrl = (env.PUBLIC_API_BASE_URL || "http://localhost:5200").replace(/\/$/, "");
	const jobIdPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
	const initialJobId = page.url.searchParams.get("job");
	const steps = [
		{ key: ProgressStep.SOURCE_CHECK, label: "Checking source" },
		{ key: ProgressStep.LOADING_PAGE, label: "Loading page" },
		{ key: ProgressStep.LOADING_DYNAMIC_CONTENT, label: "Loading dynamic content" },
		{ key: ProgressStep.SAVING_SNAPSHOT, label: "Saving snapshot" }
	];
	const terminalStatuses = new Set([
		JobStatus.COMPLETED,
		JobStatus.CANCELLED,
		JobStatus.FAILED,
		JobStatus.DISCARDED,
		JobStatus.ABORTED
	]);

	let draft = $state<ArchiveDraft>({
		source: "url",
		sourceUrl: "",
		file: null,
		originalLink: "",
		title: "",
		description: "",
		tags: []
	});
	let errors = $state<ArchiveFieldErrors>({});
	let archive = $state<ArchiveResponse | null>(null);
	let submittedTitle = $state("");
	let submittedSource = $state("");
	let submitting = $state(false);
	let restoring = $state(Boolean(initialJobId && jobIdPattern.test(initialJobId)));
	let cancelling = $state(false);
	let formError = $state<string | null>(null);
	let monitorError = $state<string | null>(null);
	let cancelError = $state<string | null>(null);
	const toast = createToastController();
	let eventSource: EventSource | null = null;
	let pollTimer: ReturnType<typeof setTimeout> | null = null;
	let monitorVersion = 0;
	let alive = false;

	const percent = $derived(Math.max(0, Math.min(100, Number(archive?.progressPercent ?? 0) || 0)));
	const terminal = $derived(archive !== null && terminalStatuses.has(archive.status));
	const canCancel = $derived(archive !== null && (archive.status === JobStatus.QUEUED || archive.status === JobStatus.RUNNING));
	const currentStep = $derived(
		archive?.status === JobStatus.QUEUED ? -1 : steps.findIndex((step) => step.key === archive?.progressStep)
	);
	const heading = $derived(
		archive?.status === JobStatus.COMPLETED
			? "Capture complete"
			: archive?.status === JobStatus.CANCELLED
				? "Capture cancelled"
				: archive && terminal
					? "Capture could not be completed"
					: "Capture in progress"
	);
	const progressDescription = $derived(
		archive?.status === JobStatus.QUEUED
			? `Your request is waiting${archive.queuePosition ? ` at position ${archive.queuePosition}` : ""}.`
			: archive?.status === JobStatus.COMPLETED
				? "The preserved page is ready to read in the archive."
				: archive?.status === JobStatus.CANCELLED
					? "The archive was cancelled. No new snapshot was published."
					: archive && terminal
						? archive.errorMessage || "The archive could not be completed."
						: "The archive is preserving the page and its resources."
	);

	function stopMonitoring(): void {
		monitorVersion += 1;
		eventSource?.close();
		eventSource = null;
		if (pollTimer) clearTimeout(pollTimer);
		pollTimer = null;
	}

	function applyArchive(next: ArchiveResponse): void {
		archive = next;
		monitorError = null;
		if (terminalStatuses.has(next.status)) stopMonitoring();
	}

	async function pollArchive(id: string, version: number): Promise<void> {
		try {
			const next = await getArchive(apiBaseUrl, id);
			if (version !== monitorVersion) return;
			applyArchive(next);
		} catch {
			if (version !== monitorVersion) return;
			monitorError = "The connection was interrupted. Checking again shortly.";
		}
		if (version === monitorVersion && archive && !terminalStatuses.has(archive.status)) {
			pollTimer = setTimeout(() => {
				void pollArchive(id, version);
			}, 2500);
		}
	}

	function startMonitoring(id: string): void {
		stopMonitoring();
		const version = monitorVersion;
		try {
			eventSource = new EventSource(`${apiBaseUrl}/api/archive/${encodeURIComponent(id)}/events`);
			eventSource.onmessage = (event) => {
				if (version !== monitorVersion) return;
				try {
					const next = JSON.parse(event.data) as ArchiveResponse;
					if (next.id === id) applyArchive(next);
				} catch {
					// A malformed event does not replace the last valid status.
				}
			};
			eventSource.onerror = () => {
				if (version !== monitorVersion) return;
				eventSource?.close();
				eventSource = null;
				void pollArchive(id, version);
			};
		} catch {
			void pollArchive(id, version);
		}
	}

	function updateJobInUrl(id: string | null): void {
		const url = new URL(page.url);
		if (id) url.searchParams.set("job", id);
		else url.searchParams.delete("job");
		replaceState(url, page.state);
	}

	function rememberSource(id: string): void {
		try {
			saveArchiveSource(window.sessionStorage, id, { title: submittedTitle, source: submittedSource });
		} catch {
			// The job link and progress remain available when browser storage is blocked.
		}
	}

	function restoreSource(id: string): void {
		try {
			const details = loadArchiveSource(window.sessionStorage, id);
			if (details) {
				submittedTitle = details.title;
				submittedSource = details.source;
			}
		} catch {
			// Display the server status without optional source metadata.
		}
	}

	onMount(() => {
		alive = true;
		const id = initialJobId;
		if (id && jobIdPattern.test(id)) {
			restoreSource(id);
			void getArchive(apiBaseUrl, id)
				.then((result) => {
					if (!alive) return;
					applyArchive(result);
					if (!terminalStatuses.has(result.status)) startMonitoring(id);
				})
				.catch(() => {
					if (!alive) return;
					formError = "This archive request could not be loaded. You can start a new one below.";
				})
				.finally(() => {
					if (alive) restoring = false;
				});
		}
		return () => {
			alive = false;
			stopMonitoring();
		};
	});

	function clearError(field: keyof ArchiveFieldErrors): void {
		if (!errors[field]) return;
		const next = { ...errors };
		delete next[field];
		errors = next;
	}

	function selectSource(source: string): void {
		draft.source = source as ArchiveSource;
		errors = {};
	}

	async function focusFirstError(): Promise<void> {
		await tick();
		document.querySelector<HTMLElement>('#archive-form [aria-invalid="true"]')?.focus();
	}

	async function submit(event: SubmitEvent): Promise<void> {
		event.preventDefault();
		if (submitting) return;
		errors = validateDraft(draft);
		formError = null;
		if (Object.keys(errors).length) {
			await focusFirstError();
			return;
		}
		submitting = true;
		try {
			const result = await createArchive(apiBaseUrl, draft);
			if (!alive) return;
			submittedTitle = draft.title.trim();
			submittedSource = draft.source === "url" ? draft.sourceUrl.trim() : (draft.file?.name ?? "");
			rememberSource(result.id);
			applyArchive(result);
			updateJobInUrl(result.id);
			if (!terminalStatuses.has(result.status)) startMonitoring(result.id);
			await tick();
			document.getElementById("progress-title")?.focus();
		} catch (error) {
			if (error instanceof ArchiveHttpError) {
				errors = error.fieldErrors;
				formError = error.message;
				if (Object.keys(errors).length) await focusFirstError();
			} else formError = "The archive service could not be reached. Please try again.";
		} finally {
			submitting = false;
		}
	}

	async function cancel(): Promise<void> {
		if (!archive || !canCancel || cancelling) return;
		cancelling = true;
		cancelError = null;
		try {
			const result = await cancelArchive(apiBaseUrl, archive.id);
			if (!alive) return;
			applyArchive(result);
		} catch (error) {
			if (!alive) return;
			cancelError = error instanceof ArchiveHttpError ? error.message : "The cancellation could not be sent. Please try again.";
			if (error instanceof ArchiveHttpError && error.status === 409) {
				try {
					const latest = await getArchive(apiBaseUrl, archive.id);
					if (alive) {
						applyArchive(latest);
						cancelError = null;
					}
				} catch {
					/* Preserve the last known status. */
				}
			}
		} finally {
			cancelling = false;
		}
	}

	function restart(): void {
		stopMonitoring();
		archive = null;
		draft = { source: "url", sourceUrl: "", file: null, originalLink: "", title: "", description: "", tags: [] };
		errors = {};
		submittedTitle = "";
		submittedSource = "";
		formError = null;
		monitorError = null;
		cancelError = null;
		updateJobInUrl(null);
		void tick().then(() => document.getElementById("source-url-control")?.focus());
	}

	function milestoneState(index: number): "done" | "active" | "waiting" | "stopped" {
		if (archive?.status === JobStatus.COMPLETED || index < currentStep) return "done";
		if (terminal && index === currentStep) return "stopped";
		if (archive?.status === JobStatus.RUNNING && index === currentStep) return "active";
		return "waiting";
	}
</script>

<svelte:head>
	<title>Archive a page — ARCHIV</title>
	<meta name="description" content="Preserve a public web page or a saved page file, and follow its capture progress." />
</svelte:head>

<ToastProvider controller={toast}>
	<main id="main-content" class="page-shell">
		<Breadcrumb class="page-breadcrumb">
			<BreadcrumbList>
				<BreadcrumbItem><BreadcrumbLink href="/">Archive</BreadcrumbLink></BreadcrumbItem>
				<BreadcrumbSeparator />
				<BreadcrumbItem><BreadcrumbPage>New capture</BreadcrumbPage></BreadcrumbItem>
			</BreadcrumbList>
		</Breadcrumb>
		<section class="intro" aria-labelledby="page-title">
			<p class="eyebrow">Contribute to the public record</p>
			<h1 id="page-title">Archive a page.</h1>
			<p class="lede">
				Preserve a public web page as it appears now, or add a saved copy from your device. Give it a clear title so the record
				can be found later.
			</p>
		</section>

		<div class="workspace">
			{#if restoring}
				<section class="main-column" aria-live="polite"><p>Loading archive progress…</p></section>
			{:else if archive}
				<section class="main-column" aria-labelledby="progress-title">
					<div class="progress-heading">
						<h2 id="progress-title" tabindex="-1">{heading}</h2>
						<span class="status-label" data-status={archive.status}>{archive.status.toLowerCase()}</span>
					</div>
					<div class="status-line" data-status={archive.status} role="status" aria-live="polite">
						<span class="status-symbol" aria-hidden="true"
							>{#if archive.status === JobStatus.COMPLETED}<Check size={17} />{:else if terminal}<X
									size={17}
								/>{:else}→{/if}</span
						>
						<span>{archive.message}</span>
					</div>
					<p class="progress-description">{progressDescription}</p>
					{#if monitorError}<p class="status-note" role="status">{monitorError}</p>{/if}
					<dl class="progress-source">
						{#if submittedTitle}<div>
								<dt>Title</dt>
								<dd>{submittedTitle}</dd>
							</div>{/if}
						{#if submittedSource}<div>
								<dt>Source</dt>
								<dd class="source-value">
									<span class="record-value">{submittedSource}</span>
									<CopySourceButton value={submittedSource} label="Copy source" />
								</dd>
							</div>{/if}
						<div>
							<dt>Archive ID</dt>
							<dd class="record-value">{archive.id}</dd>
						</div>
						<div>
							<dt>Started</dt>
							<dd><LocalTimestamp value={archive.createdAt} /></dd>
						</div>
					</dl>
					<Progress label="Capture progress" value={percent} animationDuration={160} />
					<ol class="milestones" aria-label="Capture stages">
						{#each steps as step, index}
							<li data-state={milestoneState(index)}>
								<span class="milestone-dot" aria-hidden="true"></span><span>{step.label}</span><span class="milestone-state"
									>{milestoneState(index) === "done"
										? "Done"
										: milestoneState(index) === "active"
											? "Current"
											: milestoneState(index) === "stopped"
												? "Stopped"
												: "Waiting"}</span
								>
							</li>
						{/each}
					</ol>
					{#if cancelError}<p class="form-alert" role="alert"><span aria-hidden="true">!</span>{cancelError}</p>{/if}
					<div class="form-actions">
						{#if canCancel}<Button
								variant="secondary"
								disabled={cancelling}
								onclick={() => {
									void cancel();
								}}>{cancelling ? "Cancelling…" : "Cancel archiving"}</Button
							>{/if}
						{#if archive.status === JobStatus.COMPLETED && archive.snapshotId}<Button
								variant="primary"
								href={`/snapshots/${archive.snapshotId}`}>Open snapshot <ArrowRight size={18} aria-hidden="true" /></Button
							>{/if}
						{#if terminal}<Button variant="secondary" onclick={restart}>Start another capture</Button>{/if}
						{#if canCancel}<span class="actions-note">You can cancel while the request is queued or running.</span>{/if}
					</div>
				</section>
			{:else}
				<section class="main-column" aria-labelledby="form-title">
					<div class="section-head">
						<div>
							<p class="section-label">Submission</p>
							<h2 id="form-title">Choose your source</h2>
						</div>
						<span class="section-index">01 / 02</span>
					</div>
					<TabsRoot value={draft.source} onValueChange={selectSource}>
						<TabsList aria-label="Archive source" class="source-tabs">
							<TabsTrigger value="url" class="source-tab"
								><Globe2 size={22} strokeWidth={1.7} aria-hidden="true" />Live URL</TabsTrigger
							>
							<TabsTrigger value="file" class="source-tab"
								><FileText size={22} strokeWidth={1.7} aria-hidden="true" />Saved file</TabsTrigger
							>
						</TabsList>
						<form id="archive-form" novalidate onsubmit={submit}>
							<TabsPanel value="url" forceMount>
								<InputRoot id="source-url" required invalid={Boolean(errors.sourceUrl)} hasDescription class="field">
									<InputLabel>Page URL <span class="field-qualifier">Required</span></InputLabel>
									<InputControl
										type="url"
										data-value-kind="record"
										name="sourceUrl"
										bind:value={draft.sourceUrl}
										oninput={() => clearError("sourceUrl")}
										placeholder="https://example.org/article"
										maxlength={2048}
										autocomplete="url"
									/>
									<InputDescription>
										Use a public HTTP or HTTPS address. The complete URL remains part of the record.
									</InputDescription>
									{#if errors.sourceUrl}<InputError>{errors.sourceUrl}</InputError>{/if}
								</InputRoot>
							</TabsPanel>
							<TabsPanel value="file" forceMount>
								<InputRoot id="source-file" required invalid={Boolean(errors.file)} hasDescription class="field">
									<InputLabel>Saved page <span class="field-qualifier">Required</span></InputLabel>
									<InputControl
										type="file"
										name="file"
										accept=".html,.mhtml,.webarchive"
										onFilesChange={(files) => {
											draft.file = files[0] ?? null;
											clearError("file");
										}}
									/>
									<InputDescription>HTML, MHTML or Webarchive · up to 250 MB</InputDescription>
									{#if errors.file}<InputError>{errors.file}</InputError>{/if}
								</InputRoot>
								<InputRoot id="original-link" required invalid={Boolean(errors.originalLink)} hasDescription class="field">
									<InputLabel>Original page URL <span class="field-qualifier">Required</span></InputLabel>
									<InputControl
										type="url"
										data-value-kind="record"
										name="originalLink"
										bind:value={draft.originalLink}
										oninput={() => clearError("originalLink")}
										placeholder="https://example.org/original-page"
										maxlength={2048}
									/>
									<InputDescription>Enter the public URL where this saved page originally appeared.</InputDescription>
									{#if errors.originalLink}<InputError>{errors.originalLink}</InputError>{/if}
								</InputRoot>
							</TabsPanel>
							<hr class="form-rule" />
							<div class="section-head">
								<div>
									<p class="section-label">Description</p>
									<h2>Describe the record</h2>
								</div>
								<span class="section-index">02 / 02</span>
							</div>
							<InputRoot id="record-title" required invalid={Boolean(errors.title)} hasDescription class="field">
								<InputLabel>Title <span class="field-qualifier">Required</span></InputLabel>
								<InputControl
									name="title"
									type="text"
									bind:value={draft.title}
									oninput={() => clearError("title")}
									maxlength={500}
									placeholder="A clear title for this page"
								/>
								<InputDescription>This title appears in search results and the snapshot record.</InputDescription>
								{#if errors.title}<InputError>{errors.title}</InputError>{/if}
							</InputRoot>
							<div class="field-grid">
								<InputRoot id="description" invalid={Boolean(errors.description)} hasDescription class="field">
									<InputLabel>Description <span class="field-qualifier">Optional</span></InputLabel>
									<InputTextarea
										name="description"
										bind:value={draft.description}
										oninput={() => clearError("description")}
										maxlength={10000}
										placeholder="What should future readers know about this page?"
									/>
									<InputDescription>A short note adds context to the source.</InputDescription>
									{#if errors.description}<InputError>{errors.description}</InputError>{/if}
								</InputRoot>
								<TagEditor
									id="archive-tags"
									label="Tags"
									requirement="optional"
									listLabel="Added archive tags"
									description="Press Enter or use commas to add tags. Up to 30 tags, 100 characters each."
									tags={draft.tags}
									error={errors.tags}
									onChange={(tags) => {
										draft.tags = tags;
										clearError("tags");
									}}
								/>
							</div>
							{#if formError}<p class="form-alert" role="alert"><span aria-hidden="true">!</span>{formError}</p>{/if}
							<div class="form-actions">
								<Button variant="primary" type="submit" loading={submitting} loadingLabel="Starting archive…"
									>Start archiving <ArrowRight size={18} aria-hidden="true" /></Button
								><span class="actions-note" role={submitting ? "status" : undefined} aria-live="polite"
									>{submitting
										? draft.source === "file"
											? "Uploading the saved page. Large files can take a few minutes; keep this page open."
											: "Submitting the page to the archive…"
										: "The progress of this capture will appear here after you start."}</span
								>
							</div>
						</form>
					</TabsRoot>
				</section>
			{/if}
		</div>
	</main>
	<ToastViewport />
</ToastProvider>

<style>
	.page-shell {
		width: min(100%, 86rem);
		margin-inline: auto;
		padding: 0 clamp(1rem, 4vw, 4rem) 6rem;
		overflow-wrap: anywhere;
	}
	.page-shell :global(.page-breadcrumb) {
		margin: 2.25rem 0 0;
	}
	.eyebrow,
	.section-label {
		margin: 0;
		font-size: 0.75rem;
		font-weight: 600;
		line-height: 1.33;
		letter-spacing: 0.08em;
		text-transform: uppercase;
	}
	.eyebrow {
		display: flex;
		align-items: center;
		gap: 0.75rem;
		color: var(--time-marker);
	}
	.eyebrow::before {
		content: "";
		width: 1.75rem;
		height: 2px;
		background: currentColor;
	}
	.intro {
		border-bottom: 1px solid var(--border-trace);
		padding: 4.5rem 0 4rem;
	}
	h1,
	h2 {
		font-family: var(--font-editorial);
		font-weight: 500;
	}
	h1 {
		max-width: 14ch;
		margin: 1.25rem 0;
		font-size: clamp(3rem, 6vw, 4.5rem);
		line-height: 1.06;
		letter-spacing: -0.025em;
	}
	.lede {
		max-width: 37rem;
		margin: 0;
		color: var(--marginal-note);
		font-size: 1.125rem;
		line-height: 1.55;
	}
	.workspace {
		padding-top: 3rem;
	}
	.main-column {
		min-width: 0;
		max-width: 60rem;
	}
	.section-head {
		display: flex;
		align-items: flex-end;
		justify-content: space-between;
		gap: 1rem;
		margin-bottom: 1.5rem;
	}
	.section-head h2 {
		margin: 0.5rem 0 0;
		font-size: 2rem;
		line-height: 1.18;
		letter-spacing: -0.015em;
	}
	.section-index {
		flex: none;
		color: var(--marginal-note);
		font: 400 0.8125rem/1.5 var(--font-record);
	}
	.page-shell :global(.source-tabs) {
		display: grid;
		grid-template-columns: 1fr 1fr;
		margin-bottom: 2rem;
	}
	.page-shell :global(.source-tab) {
		position: relative;
		display: flex;
		align-items: center;
		gap: 0.75rem;
		min-height: 56px;
		border: 0;
		background: transparent;
		color: var(--marginal-note);
		padding: 0.75rem 1rem;
		text-align: left;
		font-weight: 600;
		cursor: pointer;
	}
	.page-shell :global(.source-tab:hover) {
		background: var(--archive-layer);
	}
	.page-shell :global(.source-tab[aria-selected="true"]) {
		color: var(--reading-ink);
		background: var(--archive-layer);
	}
	.page-shell :global(.source-tab[aria-selected="true"]::after) {
		content: "";
		position: absolute;
		right: 0;
		bottom: -1px;
		left: 0;
		height: 3px;
		background: var(--register-mark);
	}
	.page-shell :global(.field) {
		margin-bottom: 1.5rem;
	}
	.form-alert span {
		display: inline-grid;
		flex: none;
		place-items: center;
		width: 1.1rem;
		height: 1.1rem;
		border: 1px solid currentColor;
		border-radius: 50%;
		font-size: 0.75rem;
		line-height: 1;
	}
	.form-rule {
		border: 0;
		border-top: 1px solid var(--border-trace);
		margin: 2.5rem 0;
	}
	.field-grid {
		display: grid;
		grid-template-columns: 1fr 1fr;
		align-items: start;
		gap: 1.5rem;
	}
	.form-actions {
		display: flex;
		align-items: center;
		flex-wrap: wrap;
		gap: 1rem;
		border-top: 1px solid var(--border-trace);
		margin-top: 2.5rem;
		padding-top: 1.5rem;
	}
	.actions-note {
		max-width: 29rem;
		color: var(--marginal-note);
		font-size: 0.875rem;
		line-height: 1.43;
	}
	.form-alert {
		display: flex;
		align-items: baseline;
		gap: 0.6rem;
		margin: 1rem 0 0;
		border-left: 2px solid var(--time-marker);
		background: var(--archive-layer);
		padding: 1rem;
		color: var(--reading-ink);
		font-size: 0.875rem;
		font-weight: 600;
	}
	.form-alert span {
		color: var(--time-marker);
	}
	.progress-heading {
		display: flex;
		align-items: center;
		flex-wrap: wrap;
		gap: 0.75rem;
		margin-bottom: 1rem;
	}
	.progress-heading h2 {
		margin: 0;
		font-size: 2rem;
		line-height: 1.18;
	}
	.status-label {
		display: inline-flex;
		align-items: center;
		min-height: 28px;
		border: 1px solid var(--border-trace);
		border-radius: 999px;
		padding: 0.2rem 0.7rem;
		color: var(--marginal-note);
		font-size: 0.75rem;
		font-weight: 600;
		text-transform: capitalize;
	}
	.status-label[data-status="COMPLETED"] {
		border-color: var(--preservation-green);
		color: var(--preservation-green);
	}
	.status-label[data-status="CANCELLED"],
	.status-label[data-status="FAILED"],
	.status-label[data-status="DISCARDED"],
	.status-label[data-status="ABORTED"] {
		border-color: var(--time-marker);
		color: var(--time-marker);
	}
	.status-line {
		display: flex;
		align-items: center;
		gap: 0.75rem;
		margin: 1rem 0 0.5rem;
		font-weight: 600;
	}
	.status-symbol {
		display: inline-grid;
		place-items: center;
		flex: none;
		width: 1.75rem;
		height: 1.75rem;
		border: 1px solid var(--register-mark);
		border-radius: 50%;
		color: var(--register-mark);
		font-size: 0.875rem;
		line-height: 1;
	}
	.status-line[data-status="COMPLETED"] .status-symbol {
		border-color: var(--preservation-green);
		color: var(--preservation-green);
	}
	.status-line[data-status="CANCELLED"] .status-symbol,
	.status-line[data-status="FAILED"] .status-symbol,
	.status-line[data-status="DISCARDED"] .status-symbol,
	.status-line[data-status="ABORTED"] .status-symbol {
		border-color: var(--time-marker);
		color: var(--time-marker);
	}
	.progress-description {
		margin: 0 0 2rem;
		color: var(--marginal-note);
	}
	.status-note {
		margin: 0;
		border-left: 2px solid var(--warning-ochre);
		padding: 0.5rem 0 0.5rem 1rem;
		color: var(--reading-ink);
		font-size: 0.875rem;
	}
	.progress-source {
		margin: 2rem 0;
		border-top: 1px solid var(--border-trace);
		border-bottom: 1px solid var(--border-trace);
	}
	.progress-source div {
		display: grid;
		grid-template-columns: 9rem minmax(0, 1fr);
		gap: 1rem;
		padding: 0.875rem 0;
	}
	.progress-source div + div {
		border-top: 1px solid var(--border-trace);
	}
	.progress-source dt {
		color: var(--marginal-note);
		font-size: 0.75rem;
		font-weight: 600;
		letter-spacing: 0.08em;
		text-transform: uppercase;
	}
	.progress-source dd {
		margin: 0;
		min-width: 0;
		overflow-wrap: anywhere;
	}
	.progress-source :global(time) {
		font-family: var(--font-record);
		font-size: 0.8125rem;
	}
	.progress-source .record-value {
		font: 400 0.8125rem/1.54 var(--font-record);
	}
	.progress-source .source-value {
		display: grid;
		grid-template-columns: minmax(0, 1fr) auto;
		align-items: center;
		gap: 0.5rem;
	}
	.progress-source .source-value .record-value {
		min-width: 0;
		overflow-wrap: anywhere;
	}
	.milestones {
		margin: 2rem 0 0;
		padding: 0;
		list-style: none;
	}
	.milestones li {
		position: relative;
		display: grid;
		grid-template-columns: 1rem minmax(0, 1fr) auto;
		align-items: baseline;
		gap: 1rem;
		min-height: 50px;
		color: var(--marginal-note);
		font-size: 0.9375rem;
	}
	.milestones li::before {
		content: "";
		position: absolute;
		left: 5px;
		top: 16px;
		bottom: -1px;
		width: 1px;
		background: var(--border-trace);
	}
	.milestones li:last-child::before {
		display: none;
	}
	.milestone-dot {
		position: relative;
		z-index: 1;
		width: 11px;
		height: 11px;
		margin-top: 7px;
		border: 1px solid var(--border-trace);
		border-radius: 50%;
		background: var(--reading-room);
	}
	.milestones li[data-state="done"] {
		color: var(--reading-ink);
	}
	.milestones li[data-state="done"] .milestone-dot {
		border-color: var(--preservation-green);
		background: var(--preservation-green);
	}
	.milestones li[data-state="active"] {
		color: var(--reading-ink);
		font-weight: 600;
	}
	.milestones li[data-state="active"] .milestone-dot {
		border: 3px solid var(--time-marker);
		border-radius: 2px;
	}
	.milestones li[data-state="stopped"] {
		color: var(--reading-ink);
		font-weight: 600;
	}
	.milestones li[data-state="stopped"] .milestone-dot {
		border: 2px solid var(--time-marker);
		border-radius: 2px;
	}
	.milestone-state {
		font-size: 0.75rem;
		font-weight: 600;
		text-transform: uppercase;
		letter-spacing: 0.04em;
	}
	@media (max-width: 56rem) {
		.intro {
			padding-top: 3rem;
		}
	}
	@media (max-width: 40rem) {
		.field-grid {
			grid-template-columns: 1fr;
			gap: 0;
		}
		.intro {
			padding-bottom: 3rem;
		}
		.workspace {
			padding-top: 2rem;
		}
	}
	@media (max-width: 25rem) {
		.page-shell :global(.source-tab) {
			font-size: 0.875rem;
			gap: 0.5rem;
			padding-inline: 0.5rem;
		}
		.progress-source div {
			grid-template-columns: 1fr;
			gap: 0.15rem;
		}
		.milestones li {
			grid-template-columns: 1rem minmax(0, 1fr);
		}
		.milestone-state {
			grid-column: 2;
			margin-top: -0.75rem;
		}
	}
</style>
