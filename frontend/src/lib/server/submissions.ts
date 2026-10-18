import { error, redirect, type RequestEvent } from "@sveltejs/kit";
import type { SnapshotHistoryResponse, SnapshotSubmissionDetailResponse, SnapshotSubmissionListResponse } from "$api";
import { requireAdmin } from "./admin";
import { backendUrl } from "./api";
import { loginHref } from "$lib/auth/return-url";
import { pageHref } from "$lib/admin/navigation";

type SubmissionLoadEvent = RequestEvent & { depends: (...dependencies: string[]) => void };

/** Every private page guards its own load; layout loads may run concurrently. */
function requireAccess(event: SubmissionLoadEvent, review: boolean) {
	event.depends("app:auth", "app:submissions");
	if (review) return requireAdmin(event, "snapshots");
	if (!event.locals.session) error(503, "Your account could not be loaded. Please try again.");
	if (!event.locals.session.isAuthenticated || !event.locals.session.user)
		redirect(303, loginHref(event.url.pathname + event.url.search));
}

/** Forwards the session only to the configured backend, never to a supplied URL. */
async function readPrivate<T>(event: RequestEvent, path: string): Promise<{ value: T; etag: string | null }> {
	let response: Response;
	try {
		response = await fetch(backendUrl(path), {
			headers: { cookie: event.request.headers.get("cookie") ?? "", accept: "application/json" },
			cache: "no-store",
			redirect: "error",
			signal: AbortSignal.timeout(10000)
		});
	} catch {
		error(503, "Submissions are temporarily unavailable. Please try again.");
	}
	if (response.status === 401) redirect(303, loginHref(event.url.pathname + event.url.search));
	if (response.status === 403) error(403, "Your access to these submissions has changed.");
	if (response.status === 404) error(404, "This submission could not be found.");
	if (!response.ok) error(503, "Submissions are temporarily unavailable. Please try again.");
	return { value: (await response.json()) as T, etag: response.headers.get("etag") };
}

const statuses = ["PENDING", "APPROVED", "REJECTED", "RESUBMITTED"];

/** Validates URL-owned filters before requesting a bounded backend page. */
export async function loadSubmissions(event: SubmissionLoadEvent, review: boolean) {
	requireAccess(event, review);
	const q = (event.url.searchParams.get("q") ?? "").trim();
	const status = event.url.searchParams.get("moderationStatus") ?? "";
	const page = event.url.searchParams.get("page") ?? "1";
	if (q.length > 500 || (status && !statuses.includes(status)) || !/^[1-9]\d*$/.test(page) || Number(page) > 1000000)
		error(400, "Use a valid search, status and page number.");
	const query = new URLSearchParams({ page, pageSize: "25" });
	if (q) query.set("q", q);
	if (status) query.set("moderationStatus", status);
	const scope = review ? "admin" : "users";
	const result = await readPrivate<SnapshotSubmissionListResponse>(event, `/api/${scope}/snapshots?${query}`);
	const lastPage = Math.max(1, Math.ceil(Number(result.value.total) / Number(result.value.pageSize)));
	if (Number(page) > lastPage) redirect(303, pageHref(event.url, "page", lastPage));
	return { submissions: result.value, filters: { q, status }, review };
}

/** Loads metadata and a separate history page while keeping the decision ETag stable. */
export async function loadSubmission(event: SubmissionLoadEvent, review: boolean) {
	requireAccess(event, review);
	const id = event.params.snapshotId;
	if (!id || !/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(id)) error(404, "Submission not found");
	const historyPage = event.url.searchParams.get("historyPage") ?? "1";
	if (!/^[1-9]\d*$/.test(historyPage) || Number(historyPage) > 1000000) error(400, "Use a valid history page.");
	const base = `/api/${review ? "admin" : "users"}/snapshots/${id}`;
	const [detail, history] = await Promise.all([
		readPrivate<SnapshotSubmissionDetailResponse>(event, base),
		readPrivate<SnapshotHistoryResponse>(event, `${base}/history?page=${historyPage}&pageSize=25`)
	]);
	if (!detail.etag) error(503, "The submission could not be validated. Please try again.");
	const lastPage = Math.max(1, Math.ceil(Number(history.value.total) / Number(history.value.pageSize)));
	if (Number(historyPage) > lastPage) redirect(303, pageHref(event.url, "historyPage", lastPage));
	return { detail: detail.value, etag: detail.etag, history: history.value, review };
}
