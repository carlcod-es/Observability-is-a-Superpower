## Context

See proposal.md - Why. Current state (main, 2026-10-15):
- `AcmeFormsController.Handle` validates, inserts one `acmeFormSubmission` row in an Umbraco
  scope, sets TempData to the form key and redirects (ADR-0016). No mail, no custom spans; the
  `Acme.Forms` ActivitySource and Meter exist and ServiceDefaults registers them.
- AppHost: site SMTP `localhost:2525` -> Toxiproxy -> smtp4dev (ADR-0010).
- Demo dashboard `demo-site.json`: four panels, rows y=0 and y=8 (ADR-0023).
- Tools: `tests/smoke/smoke.cs` (incl. Slow/Break/Heal mail check), `tests/traffic/traffic.cs`
  (GET only) (ADR-0012).
- Talk is 16 Oct 2026, 13:30. Work must land, be smoke-checked and rehearsed today.

Fixed constraints taken as given: NPoco builders only, SQLite and SQL Server, no personal data
in telemetry, Aspire MCP local / Grafana MCP production stage (ADR-0022).

## Goals / Non-Goals

**Goals:**
- Spec-conformant feature on `main`: reference, owner notification, form telemetry, form
  panel, burst tool, Toxiproxy removed.
- Same database schema on every demo branch, so switching branches never needs a migration
  the other branch does not know.

**Non-Goals:**
- Demo branch content. Presenter changes for `demo/act-*` branches are described only in the
  git-ignored presenter notes and applied by the PO (tasks.md section 6).
- Mail queueing, retries, HTML mail, submitter emails.

## Decisions

### D1. Mail path: Toxiproxy removed, smtp4dev on fixed port 2525 (ADR-0025)
Site SMTP settings unchanged (`localhost:2525`); smtp4dev's SMTP endpoint published on host
2525 -> container 25, `isProxied: false`. Site `.WaitFor(smtp4dev)`. Alternatives in ADR-0025
(keep Toxiproxy idle; dynamic port).

### D2. Reference allocation under an Umbraco write lock (ADR-0026)
`ReferenceNumber` int column, unique index, lock row 800001 in `umbracoLock`. In the scope:
`scope.WriteLock(800001)`, read max, +1, insert. Repository returns the number; controller
formats `ACME-{n:D4}`. New migration step `AddSubmissionReferenceStep` (idempotent parts:
column, backfill `= Id`, index, lock row). Alternatives in ADR-0026 (id after insert, random,
retry, sequence).

### D3. Notification after commit, in the request (ADR-0027)
After `scope.Complete()` and scope disposal: build subject/body with a pure static
`SubmissionNotification.Build(formName, pageTitle, reference, submittedUtc, fields)`, send via
`IEmailSender` unless the owner address is empty or `CanSendRequiredEmail()` is false. Field
labels from `[Display]` names already used by `SubmissionJson.ToFields` keys. Personal data in
email is acceptable: mail stays in the local catcher, local data is deleted after the talk
(PO 2026-10-15). Alternatives in ADR-0027 (background queue, MailKit tracing).

### D4. Form telemetry (ADR-0027)
`Handle` starts `acme.forms.submit`, sets `acme.form`; on invalid -> outcome `invalid`
(counter, span tag) and re-render; stored -> outcome `stored`, reference tag, log line;
any exception after validation -> outcome `failed`, span status Error + exception, rethrow.
The past-event rejection in `SubmitEventBooking` counts as `invalid`. Notification wrapped in
`acme.forms.notify`. Tag values are the fixed form slugs, never field values. Success TempData
now carries the form key and reference (two TempData keys) so the partial renders "Thank you,
we'll be in touch. Your reference is ACME-0042.".

### D5. Dashboard form panel (ADR-0023 amended)
New timeseries panel "Form submissions by outcome", full width at y=16:
`sum by (acme_form, acme_form_outcome) (increase(acme_forms_submissions_total{job="site"}[1m]))`.
Same datasource UID and conventions as the existing panels.

### D6. Burst tool `tests/traffic/burst.cs` (ADR-0012: file-based tool under `tests/`)
`dotnet run tests/traffic/burst.cs [-- <site-url>] [--count 50] [--parallel 10]`. Discovers the
Contact page, one service page and one upcoming event page (the smoke check's discovery
approach). Each submission: own cookie container; GET page, read `__RequestVerificationToken`
and `ufprt` from the HTML; POST made-up values (`Burst Visitor <n>`, `burst<n>@example.com`);
302 = success, then GET the redirect and read `ACME-\d+`; anything else = failure. Prints
`ok=<n> failed=<n>` and the references, and the HTTP status of each failure. Dev certificate
accepted. Alternatives: extend `traffic.cs` (lost: its spec forbids form posts; mixing modes
makes the steady source riskier); k6/bombardier (lost: new tool for attendees).

### D7. Presenter notes out of agent reach
`docs/presenter/` (moved from `.presenter/`, PO 2026-10-15) in `.gitignore`; `.claude/settings.json`
`permissions.deny`: `Read(./docs/presenter/**)` and `Read(/docs/presenter/**)` (start directory and
project root). File-tool deny does not stop a shell `cat`; the primary safeguard is
that the stage runs from a separate clone (presenter notes). No ADR: no technology choice.

### Bar and test strategy
Bar: demo (prototype quality, must work reliably on stage tomorrow). Tests: xUnit for
reference formatting and notification text (pure functions); smoke check extended (success
text shows a reference, row has it, smtp4dev has a matching email, Toxiproxy checks removed);
burst tool run against `main` must report 0 failures and distinct references; PO rehearsal
on the stage machine.

## Risks / Trade-offs

- [Talk tomorrow; migration on a database with rows] → idempotent step with backfill; smoke
  on the existing Mac database first, then on a fresh database.
- [Umbraco write lock on a custom lock id behaves differently than expected] → task 2.1
  verifies with a burst of 50/10 on SQL Server before anything else builds on it.
- [Error log entries or exception messages could carry form values] → spec scenario "No
  personal data" checked with a distinctive name/email in both telemetry backends (task 5.3).
- [Attendee data visible in the smtp4dev inbox on screen] → accepted by the PO (local only,
  deleted after the talk).
- [Cloudflare may challenge or rate-limit scripted posts through the tunnel] → the burst tool
  targets the local site by default; same database and telemetry.
- [Planning docs on a demo branch could hint at answers] → presenter notes cover what to strip
  from demo branches.

## Migration Plan

New migration step runs on start; no manual SQL. Rollback: revert the branch; the extra
column, index and lock row are harmless to the old code. Database reset before the talk per
`docs/demo-setup.md` (PO).
