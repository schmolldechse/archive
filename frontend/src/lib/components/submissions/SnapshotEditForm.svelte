<script lang="ts">
	import { onDestroy, tick, untrack } from "svelte";
	import CircleAlert from "@lucide/svelte/icons/circle-alert";
	import Pencil from "@lucide/svelte/icons/pencil";
	import Search from "@lucide/svelte/icons/search";
	import Trash2 from "@lucide/svelte/icons/trash-2";
	import X from "@lucide/svelte/icons/x";
	import type {
		PublicUserResponse,
		SnapshotOwnerListResponse,
		SnapshotSubmissionDetailResponse,
		UpdateOwnSnapshotRequest,
		UpdateSnapshotForAdminRequest
	} from "$api";
	import { apiUrl } from "$lib/api-url";
	import { normalizeTags } from "$lib/components/tags/tags";
	import TagEditor from "$lib/components/tags/TagEditor.svelte";
	import { snapshotLimits, validSourceUrl } from "$lib/snapshot-validation";
	import Button from "$lib/components/ui/Button.svelte";
	import { InputControl, InputDescription, InputError, InputLabel, InputRoot, InputTextarea } from "$lib/components/ui/input";

	type EditableField = "sourceUrl" | "title" | "description" | "tags" | "createdByHandle" | "form";
	type FieldErrors = Partial<Record<EditableField, string>>;

	let {
		detail,
		etag,
		admin,
		disabled = false,
		onSaved,
		onConflict
	}: {
		detail: SnapshotSubmissionDetailResponse;
		etag: string;
		admin: boolean;
		disabled?: boolean;
		onSaved: (detail: SnapshotSubmissionDetailResponse, etag: string) => void;
		onConflict: () => void;
	} = $props();

	let editing = $state(false);
	let saving = $state(false);
	let title = $state(untrack(() => detail.snapshot.title));
	let description = $state(untrack(() => detail.snapshot.description ?? ""));
	let sourceUrl = $state(untrack(() => detail.snapshot.sourceUrl ?? ""));
	let tags = $state<string[]>(untrack(() => [...detail.snapshot.tags]));
	let createdByHandle = $state(untrack(() => detail.snapshot.createdBy?.handle ?? ""));
	let ownerResults = $state<PublicUserResponse[]>([]);
	let ownerLoading = $state(false);
	let ownerSearchError = $state("");
	let ownerSearchOpen = $state(false);
	let activeOwnerIndex = $state(-1);
	let errors = $state<FieldErrors>({});
	let notice = $state<HTMLDivElement | null>(null);
	let searchTimer: ReturnType<typeof setTimeout> | undefined;
	let ownerRequest = 0;

	const sourceChanged = $derived(sourceUrl.trim() !== (detail.snapshot.sourceUrl ?? ""));
	const sourceEditable = $derived(detail.snapshot.quality !== null);
	const controlsDisabled = $derived(disabled || saving);
	const ownerResultsId = "snapshot-edit-owner-results";

	function reset(): void {
		title = detail.snapshot.title;
		description = detail.snapshot.description ?? "";
		sourceUrl = detail.snapshot.sourceUrl ?? "";
		tags = [...detail.snapshot.tags];
		createdByHandle = detail.snapshot.createdBy?.handle ?? "";
		errors = {};
		ownerResults = [];
		ownerSearchError = "";
		ownerSearchOpen = false;
		activeOwnerIndex = -1;
	}

	function close(): void {
		reset();
		editing = false;
	}

	function validate(): FieldErrors {
		const next: FieldErrors = {};
		if (!validSourceUrl(sourceUrl)) next.sourceUrl = "Enter a valid HTTP or HTTPS URL up to 4,096 characters.";
		if (!title.trim()) next.title = "Add a title for this snapshot.";
		else if (title.length > snapshotLimits.title) next.title = "Use no more than 512 characters for the title.";
		if (description.length > snapshotLimits.description)
			next.description = "Use no more than 2,048 characters for the description.";
		const normalizedTags = normalizeTags(tags);
		if (normalizedTags.length > snapshotLimits.tags || normalizedTags.some((tag) => tag.length > snapshotLimits.tag))
			next.tags = "Use no more than 30 tags, each up to 128 characters.";
		if (admin && createdByHandle) {
			const handle = createdByHandle.trim();
			if (handle.length < 4 || handle.length > 32 || !/^[a-z0-9][a-z0-9_-]*[a-z0-9]$/i.test(handle))
				next.createdByHandle = "Choose a registered account handle between 4 and 32 characters.";
		}
		return next;
	}

	/** Converts API validation keys into the small set of fields rendered by this form. */
	function validationErrors(problem: unknown): FieldErrors {
		if (problem === null || typeof problem !== "object") return {};
		const source = problem as Record<string, unknown>;
		if (source.errors === null || typeof source.errors !== "object" || Array.isArray(source.errors)) return {};
		const result: FieldErrors = {};
		for (const [key, messages] of Object.entries(source.errors as Record<string, unknown>)) {
			const field = key.split(".").pop()?.toLowerCase();
			const target: EditableField | undefined =
				field === "sourceurl"
					? "sourceUrl"
					: field === "title" || field === "description" || field === "tags"
						? field
						: field === "createdbyhandle"
							? "createdByHandle"
							: undefined;
			if (target && Array.isArray(messages))
				result[target] = messages.filter((message): message is string => typeof message === "string").join(" ");
		}
		return result;
	}

	async function save(): Promise<void> {
		if (saving || disabled) return;
		errors = validate();
		if (Object.keys(errors).length) {
			await tick();
			notice?.focus();
			return;
		}

		saving = true;
		const shared: UpdateOwnSnapshotRequest = {
			sourceUrl: sourceUrl.trim(),
			title: title.trim(),
			description: description.trim() || null,
			tags: normalizeTags(tags)
		};
		const body: UpdateOwnSnapshotRequest | UpdateSnapshotForAdminRequest = admin
			? { ...shared, createdByHandle: createdByHandle.trim().toLowerCase() || null }
			: shared;
		try {
			const response = await fetch(apiUrl(`/api/${admin ? "admin" : "users"}/snapshots/${detail.snapshot.id}`), {
				method: "PUT",
				credentials: "include",
				cache: "no-store",
				redirect: "error",
				headers: { "Content-Type": "application/json", "If-Match": etag },
				body: JSON.stringify(body)
			});
			const problem = response.ok ? null : await response.json().catch(() => null);
			if (!response.ok || !response.headers.get("etag")) {
				if (response.status === 412) {
					onConflict();
					close();
					return;
				}
				errors = {
					...validationErrors(problem),
					form:
						response.status === 401 || response.status === 403
							? "Your access changed. Sign in again or return to the snapshot list."
							: typeof problem?.detail === "string"
								? problem.detail
								: "The snapshot could not be saved. Please try again."
				};
				await tick();
				notice?.focus();
				return;
			}
			const updated = (await response.json()) as SnapshotSubmissionDetailResponse;
			onSaved(updated, response.headers.get("etag")!);
			editing = false;
		} catch {
			errors = { form: "The snapshot service is temporarily unavailable. Please try again." };
			await tick();
			notice?.focus();
		} finally {
			saving = false;
		}
	}

	/** Searches only the public account projection returned by the administrator endpoint. */
	function searchOwners(): void {
		if (!admin || controlsDisabled) return;
		if (searchTimer) clearTimeout(searchTimer);
		ownerSearchOpen = true;
		ownerSearchError = "";
		activeOwnerIndex = -1;
		searchTimer = setTimeout(async () => {
			const request = ++ownerRequest;
			ownerLoading = true;
			try {
				const query = new URLSearchParams({ q: createdByHandle.trim(), page: "1", pageSize: "8" });
				const response = await fetch(apiUrl(`/api/admin/snapshots/owners?${query}`), {
					credentials: "include",
					cache: "no-store",
					redirect: "error"
				});
				if (request !== ownerRequest) return;
				if (!response.ok) throw new Error("Owner search failed");
				ownerResults = ((await response.json()) as SnapshotOwnerListResponse).items;
			} catch {
				if (request === ownerRequest) {
					ownerResults = [];
					ownerSearchError = "Account search is temporarily unavailable.";
				}
			} finally {
				if (request === ownerRequest) ownerLoading = false;
			}
		}, 250);
	}

	function chooseOwner(owner: PublicUserResponse): void {
		createdByHandle = owner.handle;
		ownerSearchOpen = false;
		activeOwnerIndex = -1;
		errors.createdByHandle = undefined;
	}

	/** Keeps the keyboard-selected option visible inside the bounded suggestion popup. */
	function revealActiveOwner(): void {
		const index = activeOwnerIndex;
		if (index < 0) return;
		void tick().then(() =>
			document.getElementById(`snapshot-edit-owner-option-${index}`)?.scrollIntoView({ block: "nearest" })
		);
	}

	/** Implements the expected combobox keyboard behavior while keeping result buttons clickable. */
	function handleOwnerKeydown(event: KeyboardEvent): void {
		if (event.key === "Escape") {
			ownerSearchOpen = false;
			activeOwnerIndex = -1;
			return;
		}
		if (!ownerSearchOpen || !ownerResults.length) return;
		if (event.key === "ArrowDown" || event.key === "ArrowUp") {
			event.preventDefault();
			const direction = event.key === "ArrowDown" ? 1 : -1;
			activeOwnerIndex = (activeOwnerIndex + direction + ownerResults.length) % ownerResults.length;
			revealActiveOwner();
		} else if (event.key === "Enter" && activeOwnerIndex >= 0) {
			event.preventDefault();
			chooseOwner(ownerResults[activeOwnerIndex]);
		}
	}

	onDestroy(() => {
		if (searchTimer) clearTimeout(searchTimer);
	});
</script>

<section class="snapshot-edit" class:open={editing} aria-labelledby="snapshot-edit-title">
	<div class="snapshot-edit__heading">
		<div>
			<p class="section-label">Editable record</p>
			<h2 id="snapshot-edit-title">Snapshot details</h2>
			<p>
				{admin ? "Edit metadata, provenance and account attribution." : "Correct the metadata or source for your submission."}
			</p>
		</div>
		{#if editing}
			<Button variant="text" onclick={close} disabled={saving}><X aria-hidden="true" />Close editor</Button>
		{:else}
			<Button
				variant="secondary"
				onclick={() => {
					reset();
					editing = true;
				}}
				{disabled}><Pencil aria-hidden="true" />Edit snapshot</Button
			>
		{/if}
	</div>

	{#if editing}
		<form
			onsubmit={(event) => {
				event.preventDefault();
				void save();
			}}
			novalidate
		>
			{#if errors.form || Object.keys(errors).some((key) => key !== "form")}
				<div bind:this={notice} class="form-notice" tabindex="-1" role="alert">
					<CircleAlert aria-hidden="true" />
					<p>{errors.form ?? "Review the highlighted fields before saving."}</p>
				</div>
			{/if}
			<div class="edit-fields">
				<InputRoot id="snapshot-edit-title-field" required invalid={Boolean(errors.title)} disabled={controlsDisabled}>
					<InputLabel>Title</InputLabel>
					<InputControl bind:value={title} maxlength={snapshotLimits.title} autocomplete="off" />
					{#if errors.title}<InputError>{errors.title}</InputError>{/if}
				</InputRoot>
				<InputRoot
					id="snapshot-edit-description"
					hasDescription
					invalid={Boolean(errors.description)}
					disabled={controlsDisabled}
				>
					<InputLabel>Description <span class="field-qualifier">Optional</span></InputLabel>
					<InputTextarea bind:value={description} maxlength={snapshotLimits.description} rows={5} />
					<InputDescription>Plain-text context for readers. Maximum 2,048 characters.</InputDescription>
					{#if errors.description}<InputError>{errors.description}</InputError>{/if}
				</InputRoot>
				<TagEditor
					id="snapshot-edit-tags"
					label="Tags"
					listLabel="Current snapshot tags"
					description="Press Enter or use a comma to add a tag. Maximum 30 tags."
					requirement="optional"
					{tags}
					error={errors.tags}
					disabled={controlsDisabled}
					onChange={(value) => (tags = value)}
				/>
				<InputRoot
					id="snapshot-edit-source"
					required
					hasDescription
					invalid={Boolean(errors.sourceUrl)}
					disabled={controlsDisabled || !sourceEditable}
				>
					<InputLabel>Source URL</InputLabel>
					<InputControl bind:value={sourceUrl} type="url" maxlength={snapshotLimits.sourceUrl} data-value-kind="record" />
					<InputDescription>
						{sourceEditable
							? admin
								? "Administrative source changes keep the current review status."
								: "Changing this URL sends the snapshot back for review. The stored capture is not replaced."
							: "The source URL can be changed after capture storage has finished."}
					</InputDescription>
					{#if errors.sourceUrl}<InputError>{errors.sourceUrl}</InputError>{/if}
				</InputRoot>
				{#if admin}
					<div class="owner-field">
						<InputRoot
							id="snapshot-edit-owner"
							hasDescription
							invalid={Boolean(errors.createdByHandle)}
							disabled={controlsDisabled}
						>
							<InputLabel>Attributed account <span class="field-qualifier">Optional</span></InputLabel>
							<div class="owner-control">
								<Search aria-hidden="true" />
								<InputControl
									bind:value={createdByHandle}
									autocomplete="off"
									maxlength={32}
									placeholder="Search by handle or name"
									role="combobox"
									aria-autocomplete="list"
									aria-expanded={ownerSearchOpen}
									aria-controls={ownerResultsId}
									aria-activedescendant={activeOwnerIndex >= 0 ? `snapshot-edit-owner-option-${activeOwnerIndex}` : undefined}
									onfocus={searchOwners}
									oninput={searchOwners}
									onkeydown={handleOwnerKeydown}
								/>
							</div>
							<InputDescription
								>Stored history keeps stable internal references; the API returns current public profiles.</InputDescription
							>
							{#if errors.createdByHandle}<InputError>{errors.createdByHandle}</InputError>{/if}
						</InputRoot>
						{#if ownerSearchOpen}
							<div class="owner-results" aria-live="polite">
								<ul id={ownerResultsId} role="listbox" aria-label="Registered account suggestions" aria-busy={ownerLoading}>
									{#if ownerLoading}
										<li role="none"><p>Searching accounts…</p></li>
									{:else if ownerSearchError}
										<li role="none"><p>{ownerSearchError}</p></li>
									{:else if ownerResults.length}
										{#each ownerResults as owner, index (owner.handle)}
											<li role="none">
												<button
													id={`snapshot-edit-owner-option-${index}`}
													type="button"
													role="option"
													aria-selected={index === activeOwnerIndex}
													disabled={controlsDisabled}
													onmouseenter={() => (activeOwnerIndex = index)}
													onclick={() => chooseOwner(owner)}
													><strong>{owner.displayName}</strong><span>@{owner.handle}</span></button
												>
											</li>
										{/each}
									{:else}
										<li role="none"><p>No registered accounts found.</p></li>
									{/if}
								</ul>
							</div>
						{/if}
						<Button
							variant="text"
							onclick={() => {
								createdByHandle = "";
								ownerSearchOpen = false;
							}}
							disabled={controlsDisabled || !createdByHandle}
						>
							<Trash2 aria-hidden="true" />Remove attribution
						</Button>
					</div>
				{/if}
			</div>
			{#if sourceChanged}
				<p class="source-warning">
					<CircleAlert aria-hidden="true" />
					{admin
						? "This changes the provenance URL without changing the review status."
						: "This provenance change will set the snapshot to Resubmitted and require approval."}
				</p>
			{/if}
			<div class="edit-actions">
				<Button variant="secondary" onclick={close} disabled={saving}>Cancel</Button>
				<Button type="submit" variant="primary" {disabled} loading={saving} loadingLabel="Saving snapshot">Save changes</Button>
			</div>
		</form>
	{/if}
</section>

<style>
	.snapshot-edit {
		border-block: 1px solid var(--border-trace);
		padding-block: 1.5rem;
	}
	.snapshot-edit.open {
		padding-block: 2rem;
	}
	.snapshot-edit__heading {
		display: flex;
		align-items: center;
		justify-content: space-between;
		gap: 1.5rem;
	}
	.section-label {
		margin: 0 0 0.375rem;
		color: var(--marginal-note);
		font-size: 0.6875rem;
		font-weight: 700;
		letter-spacing: 0.08em;
		text-transform: uppercase;
	}
	h2 {
		margin: 0;
		font: 500 1.75rem/1.2 var(--font-editorial);
	}
	.snapshot-edit__heading p:last-child {
		max-width: 48rem;
		margin: 0.375rem 0 0;
		color: var(--marginal-note);
		font-size: 0.9375rem;
	}
	form {
		max-width: 52rem;
		margin-top: 2rem;
	}
	.edit-fields {
		display: grid;
		gap: 1.5rem;
	}
	.form-notice,
	.source-warning {
		display: flex;
		align-items: flex-start;
		gap: 0.75rem;
		padding: 0.875rem 1rem;
	}
	.form-notice {
		border-inline-start: 3px solid var(--time-marker);
		background: color-mix(in srgb, var(--time-marker) 7%, transparent);
		margin-bottom: 1.5rem;
	}
	.form-notice p,
	.source-warning {
		margin-block: 0;
		font-size: 0.875rem;
	}
	.form-notice :global(svg),
	.source-warning :global(svg) {
		width: 1.125rem;
		height: 1.125rem;
		flex: none;
	}
	.form-notice :global(svg) {
		color: var(--time-marker);
	}
	.source-warning {
		border-inline-start: 3px solid var(--warning-ochre);
		background: color-mix(in srgb, var(--warning-ochre) 9%, transparent);
		margin-top: 1.5rem;
	}
	.source-warning :global(svg) {
		color: var(--warning-ochre);
	}
	.edit-actions {
		display: flex;
		flex-wrap: wrap;
		justify-content: flex-end;
		gap: 0.75rem;
		margin-top: 1.5rem;
	}
	.owner-field {
		position: relative;
	}
	.owner-control {
		position: relative;
	}
	.owner-control > :global(svg) {
		position: absolute;
		z-index: 1;
		top: 50%;
		left: 1rem;
		width: 1.125rem;
		height: 1.125rem;
		color: var(--marginal-note);
		transform: translateY(-50%);
		pointer-events: none;
	}
	.owner-control :global(input) {
		padding-inline-start: 2.75rem;
	}
	.owner-results {
		position: relative;
		z-index: 2;
		max-height: 16rem;
		margin-top: 0.5rem;
		border: 1px solid var(--border-trace);
		border-radius: var(--radius-control);
		background: var(--reading-room);
		box-shadow: var(--overlay-shadow);
		overflow-y: auto;
	}
	.owner-results ul {
		margin: 0;
		padding: 0.25rem;
		list-style: none;
	}
	.owner-results button {
		display: grid;
		width: 100%;
		min-height: 44px;
		appearance: none;
		border: 0;
		border-radius: var(--radius-control);
		background: transparent;
		padding: 0.625rem 0.75rem;
		color: var(--reading-ink);
		font: inherit;
		text-align: left;
		cursor: pointer;
	}
	.owner-results button:hover,
	.owner-results button:focus-visible,
	.owner-results button[aria-selected="true"] {
		background: var(--archive-layer);
	}
	.owner-results button span {
		color: var(--marginal-note);
		font: 400 0.75rem/1.4 var(--font-record);
	}
	.owner-results p {
		margin: 0;
		padding: 0.875rem 1rem;
		color: var(--marginal-note);
		font-size: 0.875rem;
	}
	.owner-field > :global(.button) {
		margin-top: 0.5rem;
	}
	@media (max-width: 40rem) {
		.snapshot-edit__heading {
			align-items: flex-start;
			flex-direction: column;
		}
		.snapshot-edit__heading :global(.button),
		.edit-actions :global(.button) {
			width: 100%;
		}
	}
</style>
