/* Isolated design prototype: all mutations below affect sample DOM only. No API is called. */
const root = document.documentElement;
const params = new URLSearchParams(location.search);
root.dataset.theme = params.get("theme") === "dark" ? "dark" : "light";
if (params.get("scale") === "2") root.style.zoom = "2";

function announce(message) {
  const feedback = document.querySelector("[data-feedback]");
  if (feedback) {
    feedback.textContent = message;
    feedback.hidden = false;
  }
}

document
  .querySelector("[data-theme-toggle]")
  ?.addEventListener("click", (event) => {
    root.dataset.theme = root.dataset.theme === "dark" ? "light" : "dark";
    event.currentTarget.setAttribute(
      "aria-pressed",
      String(root.dataset.theme === "dark"),
    );
  });
document
  .querySelector("[data-theme-toggle]")
  ?.setAttribute("aria-pressed", String(root.dataset.theme === "dark"));
document.querySelector("[data-menu]")?.addEventListener("click", (event) => {
  const navigation = document.querySelector("#primary-navigation");
  const open = navigation.classList.toggle("is-open");
  event.currentTarget.setAttribute("aria-expanded", String(open));
});
document.querySelectorAll("[data-copy]").forEach((button) => {
  button.addEventListener("click", async () => {
    try {
      await navigator.clipboard.writeText(button.dataset.copy);
      announce("Source URL copied.");
    } catch {
      announce("Select the full source URL to copy it.");
    }
  });
});

/* Filters operate on existing sample records, preserving ownership in this mockup. */
const search = document.querySelector("[data-search]");
const statusFilter = document.querySelector("[data-status-filter]");
function filterRecords() {
  const query = search?.value.trim().toLowerCase() ?? "";
  const state = statusFilter?.value ?? "all";
  let visible = 0;
  document.querySelectorAll("[data-record]").forEach((record) => {
    const matchesState =
      state === "all" ||
      record.dataset.status === state ||
      (state === "awaiting" &&
        ["pending", "resubmitted"].includes(record.dataset.status));
    record.hidden =
      !record.textContent.toLowerCase().includes(query) || !matchesState;
    if (!record.hidden) visible += 1;
  });
  const count = document.querySelector("[data-count]");
  if (count)
    count.textContent = `${visible} ${visible === 1 ? "submission" : "submissions"}`;
  const empty = document.querySelector("[data-no-results]");
  if (empty) empty.hidden = visible !== 0;
}
search?.addEventListener("input", filterRecords);
statusFilter?.addEventListener("change", filterRecords);
document
  .querySelector("[data-apply-filter]")
  ?.addEventListener("click", filterRecords);

document
  .querySelector("[data-resubmit]")
  ?.addEventListener("click", (event) => {
    const record = event.currentTarget.closest("[data-record]");
    record.dataset.status = "resubmitted";
    const badge = record.querySelector("[data-moderation]");
    badge.className = "status resubmitted";
    badge.textContent = "↻ Resubmitted";
    const submittedTime = record.querySelector(".record-subline time");
    submittedTime.dateTime = new Date().toISOString();
    submittedTime.textContent = submittedTime.dateTime
      .replace("T", " · ")
      .replace(/\.\d+Z$/, " UTC");
    record.querySelector("[data-latest-reason]").hidden = true;
    const entry = document.createElement("li");
    const title = document.createElement("strong");
    title.textContent = "Resubmitted";
    const time = document.createElement("time");
    time.dateTime = new Date().toISOString();
    time.textContent = time.dateTime
      .replace("T", " · ")
      .replace(/\.\d+Z$/, " UTC");
    entry.append(title, time);
    record.querySelector(".history").prepend(entry);
    event.currentTarget.disabled = true;
    event.currentTarget.textContent = "Submitted for review";
    record.querySelector("[data-show-resubmit]").disabled = true;
    record.querySelector("[data-show-resubmit]").textContent =
      "Submitted for review";
    record.querySelector("[data-resubmit-confirmation]").hidden = true;
    announce("Snapshot resubmitted. The previous decision remains in History.");
    filterRecords();
  });
document
  .querySelector("[data-show-resubmit]")
  ?.addEventListener("click", () => {
    const confirmation = document.querySelector("[data-resubmit-confirmation]");
    confirmation.hidden = false;
    confirmation.querySelector("button").focus();
  });
document
  .querySelector("[data-cancel-resubmit]")
  ?.addEventListener("click", () => {
    document.querySelector("[data-resubmit-confirmation]").hidden = true;
    document.querySelector("[data-show-resubmit]").focus();
  });

/* Review samples keep rejected content read-only until its creator resubmits it. */
document.querySelector("[data-show-reject]")?.addEventListener("click", () => {
  const form = document.querySelector("[data-reject-form]");
  form.hidden = false;
  form.querySelector("textarea").focus();
});
document
  .querySelector("[data-cancel-reject]")
  ?.addEventListener("click", () => {
    document.querySelector("[data-reject-form]").hidden = true;
    document.querySelector("[data-show-reject]").focus();
  });
function decide(status) {
  const badge = document.querySelector("[data-review-status]");
  if (!badge) return;
  badge.className = `status ${status}`;
  badge.textContent = status === "approved" ? "✓ Approved" : "× Rejected";
  document.querySelector("[data-decision-controls]").hidden = true;
  document.querySelector("[data-reject-form]").hidden = true;
  const note = document.querySelector("[data-decision-result]");
  note.hidden = false;
  note.textContent =
    status === "approved"
      ? "Snapshot approved. It is now available in the public archive."
      : "Snapshot rejected. Only the submitter can resubmit this snapshot.";
  const reason = document.querySelector("#rejection-reason")?.value.trim();
  const entry = document.createElement("li");
  const title = document.createElement("strong");
  title.textContent = status === "approved" ? "Approved" : "Rejected";
  entry.append(title);
  if (status === "rejected" && reason) {
    const paragraph = document.createElement("p");
    paragraph.textContent = reason;
    entry.append(paragraph);
  }
  document.querySelector("[data-review-history]").prepend(entry);
  const selected = document.querySelector(
    "[data-review-sample][aria-pressed=true]",
  );
  if (selected) {
    selected.dataset.status = status;
    selected.querySelector(".status").className = `status ${status}`;
    selected.querySelector(".status").textContent =
      status === "approved" ? "✓ Approved" : "× Rejected";
    filterRecords();
  }
  document.querySelector(".preview-bar span").textContent =
    status === "approved" ? "Public" : "Not public";
  announce(status === "approved" ? "Snapshot approved." : "Snapshot rejected.");
  note.focus();
}
document
  .querySelector("[data-approve]")
  ?.addEventListener("click", () => decide("approved"));
document
  .querySelector("[data-reject-form]")
  ?.addEventListener("submit", (event) => {
    event.preventDefault();
    decide("rejected");
  });

/* Access previews show role defaults, the selected override, and the effective result. */
function refreshAccess() {
  const admin = document.querySelector("#admin-role")?.checked ?? false;
  document.querySelectorAll("[data-permission]").forEach((row) => {
    const setting = row.querySelector("select").value;
    const allowed = setting === "allow" || (setting === "inherit" && admin);
    row.querySelector("[data-role-default]").textContent =
      `Role default: ${admin ? "Allowed by Admin" : "Not granted by User"}`;
    const effective = row.querySelector("[data-effective]");
    const managementBlocked = row.dataset.requiresAdmin === "true" && !admin;
    effective.className = `effective ${allowed && !managementBlocked ? "allowed" : "denied"}`;
    effective.textContent =
      managementBlocked && allowed
        ? "− Admin role also required"
        : `${allowed ? "✓ Allowed" : "− Denied"} · ${setting === "inherit" ? "Role default" : "Individual setting"}`;
  });
  const roleLabel = document.querySelector("[data-selected-role]");
  if (roleLabel) roleLabel.textContent = admin ? "User · Admin" : "User";
}
document
  .querySelector("#admin-role")
  ?.addEventListener("change", refreshAccess);
document
  .querySelectorAll("[data-permission] select")
  .forEach((select) => select.addEventListener("change", refreshAccess));
document.querySelectorAll("[data-save-access]").forEach((button) => {
  button.addEventListener("click", () => {
    refreshAccess();
    announce(
      button.dataset.saveAccess === "role"
        ? "Role assignment saved in this preview."
        : "Individual permission saved in this preview.",
    );
  });
});
if (document.querySelector("[data-permission]")) refreshAccess();

const reviewExamples = {
  resubmitted: {
    title: "Open access to local history",
    url: "https://example.org/history/open-access",
    creator: "Jamie Park · @jamie.park",
    history: ["Resubmitted", "Rejected", "Submitted"],
    status: "resubmitted",
  },
  pending: {
    title: "A field guide to urban trees",
    url: "https://example.org/journal/urban-trees",
    creator: "Alex Morgan · @alex.morgan",
    history: ["Submitted"],
    status: "pending",
  },
  anonymous: {
    title: "Public garden catalogue",
    url: "https://example.org/gardens/catalogue",
    creator: "Anonymous",
    history: ["Submitted"],
    status: "pending",
  },
  rejected: {
    title: "The changing coastline",
    url: "https://example.org/research/the-changing-coastline",
    creator: "Alex Morgan · @alex.morgan",
    history: ["Rejected", "Submitted"],
    status: "rejected",
  },
  approved: {
    title: "Notes from the public library",
    url: "https://example.org/library/notes",
    creator: "Alex Morgan · @alex.morgan",
    history: ["Approved", "Resubmitted", "Rejected", "Submitted"],
    status: "approved",
  },
};
document.querySelectorAll("[data-review-sample]").forEach((button) => {
  button.addEventListener("click", () => {
    const sample = reviewExamples[button.dataset.reviewSample];
    const state = button.dataset.status;
    document
      .querySelectorAll("[data-review-sample]")
      .forEach((item) =>
        item.setAttribute("aria-pressed", String(item === button)),
      );
    document.querySelector("[data-review-title]").textContent = sample.title;
    document.querySelector("[data-preview-title]").textContent = sample.title;
    document.querySelector("[data-review-source]").textContent = sample.url;
    document.querySelector("[data-review-copy]").dataset.copy = sample.url;
    document.querySelector("[data-review-creator]").textContent =
      sample.creator;
    const status = document.querySelector("[data-review-status]");
    const labels = {
      pending: "◷ Pending",
      resubmitted: "↻ Resubmitted",
      rejected: "× Rejected",
      approved: "✓ Approved",
    };
    status.className = `status ${state}`;
    status.textContent = labels[state];
    const terminal = state === "rejected" || state === "approved";
    document.querySelector("[data-decision-controls]").hidden = terminal;
    document.querySelector("[data-reject-form]").hidden = true;
    document.querySelector("#rejection-reason").value = "";
    const result = document.querySelector("[data-decision-result]");
    result.hidden = !terminal;
    result.textContent =
      state === "rejected"
        ? "Only the submitter can resubmit this snapshot."
        : "This snapshot is available in the public archive.";
    document.querySelector(".preview-bar span").textContent =
      state === "approved" ? "Public" : "Not public";
    const timeline = document.querySelector("[data-review-history]");
    timeline.replaceChildren();
    const entries = [...sample.history];
    if (
      (state === "approved" || state === "rejected") &&
      state !== sample.status
    ) {
      entries.unshift(state === "approved" ? "Approved" : "Rejected");
    }
    entries.forEach((text) => {
      const entry = document.createElement("li");
      const title = document.createElement("strong");
      title.textContent = text;
      entry.append(title);
      timeline.append(entry);
    });
  });
});

const userExamples = {
  sam: {
    name: "Sam Lee",
    handle: "sam.lee",
    admin: true,
    review: "inherit",
    publish: "deny",
  },
  jamie: {
    name: "Jamie Park",
    handle: "jamie.park",
    admin: false,
    review: "allow",
    publish: "inherit",
  },
  alex: {
    name: "Alex Morgan",
    handle: "alex.morgan",
    admin: true,
    review: "inherit",
    publish: "inherit",
  },
};
document.querySelectorAll("[data-user-sample]").forEach((button) => {
  button.addEventListener("click", () => {
    const sample = userExamples[button.dataset.userSample];
    document
      .querySelectorAll("[data-user-sample]")
      .forEach((item) =>
        item.setAttribute("aria-pressed", String(item === button)),
      );
    document.querySelector("[data-user-name]").textContent = sample.name;
    document.querySelector("[data-user-handle]").textContent =
      `@${sample.handle}`;
    document.querySelector("#admin-role").checked = sample.admin;
    document.querySelector("#permission-review").value = sample.review;
    document.querySelector("#permission-publish").value = sample.publish;
    document.querySelector("#permission-roles").value = "inherit";
    document.querySelector("#permission-management").value = "inherit";
    document.querySelector("[data-override-explanation]").textContent =
      sample.name === "Jamie Park"
        ? "Jamie can review snapshots through an individual Allow, without the Admin role."
        : sample.publish === "deny"
          ? "Sam can review snapshots, but cannot publish without approval."
          : "Alex inherits all four permissions from the Admin role.";
    refreshAccess();
  });
});
document
  .querySelector("[data-user-search]")
  ?.addEventListener("input", (event) => {
    const query = event.currentTarget.value.trim().toLowerCase();
    document.querySelectorAll("[data-user-sample]").forEach((button) => {
      button.hidden = !button.textContent.toLowerCase().includes(query);
    });
  });
if (document.querySelector("[data-record]")) filterRecords();

/* The preview harness changes iframe size, never the product page's own CSS. */
const previewFrame = document.querySelector("#preview-frame");
if (previewFrame) {
  const route = document.querySelector("#preview-route");
  const width = document.querySelector("#preview-width");
  const theme = document.querySelector("#preview-theme");
  const scale = document.querySelector("#preview-scale");
  function updatePreview() {
    previewFrame.style.width = `${width.value}px`;
    previewFrame.src = `${route.value}?theme=${theme.value}&scale=${scale.value}`;
    previewFrame.title = `${route.selectedOptions[0].textContent} at ${width.value} pixels`;
  }
  [route, width, theme, scale].forEach((control) =>
    control.addEventListener("change", updatePreview),
  );
  updatePreview();
}
