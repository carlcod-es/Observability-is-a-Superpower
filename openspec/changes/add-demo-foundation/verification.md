# Verification log: add-demo-foundation

Recorded observations for spec scenarios not covered by unit tests (design.md "Bar and test
strategy"). Evidence comes from the block reviews; the reviewer ran each check independently.

Tasks were renumbered 2026-10-13 (ADR-0016/ADR-0017 re-plan); entries cite the task numbers
current at the time. The section 3 entries, and the task 6.1 and folder-step entries, record
the forms package that the re-plan removes (new task 3.1).

## Section 1

- `add-demo-foundation` task 1.1, 2026-10-08: `aspire --version` prints 13.6.0.
- `add-demo-foundation` task 1.2, 2026-10-08: root `dotnet build` 0 errors; AppHost started,
  `aspire describe` shows `site` Running/Healthy; `/health`, `/alive`, `/`, `/umbraco` return 200.
- `add-demo-foundation` task 1.3, 2026-10-08, default config, no env overrides (spec
  `local-environment` "Site logs are structured", log half of "Page request trace"):
  - start-up entries in `aspire otel logs site` with named properties as separate attributes
    (`CacheName`, `address`, `ContentRoot`); no message contains stray quotes;
  - `GET /`: "Request starting" and "Request finished" entries carry the trace id and span id of
    the GET / server span in `aspire otel traces`;
  - no duplicate entries; only `Microsoft.Hosting.Lifetime` and
    `Microsoft.AspNetCore.Hosting.Diagnostics` among `Microsoft.*` categories.
  - Database spans in that trace: pending task 2.4 (SQL resource).

## Section 3

- `add-demo-foundation` task 3.2, 2026-10-09, fresh SQLite (reviewer, scratch database): migration
  "SuperPower.Forms" ran once; five element types with `fieldAlias`, `label` (mandatory),
  `required`, `pattern`, `patternMessage`; Select has `options` (Repeatable Textstrings); second
  start: no migration run, counts unchanged (6 content types, 2 data types, 28 property types).
- `add-demo-foundation` task 3.3, migration part, same run: Block List "SuperPower Forms - Fields"
  lists the five element type keys (config shape as the PO's backoffice-created
  `compPageMainContentBlockList`); `superPowerForm` has `fields` and `successMessage`, no
  template, not allowed at root. Editor UI check: pending PO observation.

## Section 2

- `add-demo-foundation` task 2.1, 2026-10-09 (reviewer, fresh volume): `sql`, `umbraco`, `site`
  Running/Healthy; site env targets `Database=umbraco` on the container with provider
  `Microsoft.Data.SqlClient`; unattended install, "SuperPower.Forms" migration and uSync first
  boot in the logs; `/` and `/umbraco` 200; 77 tables in `umbraco`. Restart: no reinstall.
- `add-demo-foundation` task 2.2, 2026-10-09: worker sent mail from the site through a temporary
  endpoint (removed): arrived in smtp4dev, 13 ms. Reviewer: site env has SMTP Host localhost,
  Port 2525, SecureSocketOptions None, From; curl SMTP via 2525 arrives in the Inbox; smtp4dev
  `/api/server`: TLS None, settings locked; in-memory store per container log.
- `add-demo-foundation` task 2.3, 2026-10-09 (reviewer, curl SMTP via 2525): baseline 0.013 s;
  Slow 6.04 s, arrives; Break fails in 0.005 s (connection reset), nothing arrives; Heal 0.007 s,
  arrives; proxy starts with no toxics; Slow and Break repeat-safe. Worker, from the site: Slow
  6052 ms, Break fails in 14 ms (`SmtpProtocolException`), Heal 11 ms.
- `add-demo-foundation` task 3.4, 2026-10-09: new step `...4002` ran on a fresh SQLite database,
  on the existing SQLite database (only the new step) and on the existing SQL Server database.
  Columns Id, FormKey (indexed), PageKey, SubmittedUtc, FieldsJson (NVARCHAR(MAX) / TEXT); no
  client columns. Reviewer: raw `FieldsJson` byte-identical on SQLite and SQL Server for the
  same submission (quotes, non-ASCII, newline).
- `add-demo-foundation` task 2.4, 2026-10-09 (spec `local-environment` "Page request trace"):
  first GET / after start: server span + 2 SqlClient SELECT child spans, `db.query.text`
  parameterised; cached GET /: 1 span. Background database traces unfiltered ~76/min (worker,
  4-minute idle window: distributed jobs, cache instructions, locks, webhooks), filtered 0
  (worker 4 min, reviewer 2.5 min). `/health`, `/alive`, `/css`, `/umbraco/backoffice`: no
  traces, no Request logs. Exporter switch: code only (Azure Monitor path in
  `add-production-environment`).
- `add-demo-foundation` task 6.1, 2026-10-09 (PO): fresh SQLite install standalone; package types
  present; sample-site content imported.
- Folder step `...4003` (PO request), 2026-10-09: fresh scratch SQLite (worker and
  reviewer) and a copy of the PO's database at `...4002`: root document type folder
  "SuperPower Forms" (key `...5001`) created once, six package types moved into it, data types
  untouched; restart: no migration, no duplicate folder.

## Section 7 (and runtime checks for tasks 3.5, 4.1)

- `add-demo-foundation` tasks 7.1/3.5/4.1, 2026-10-14, standalone SQLite site (fresh install, uSync
  first boot), worker run and independent reviewer re-run:
  - 13 pages (Home, About us, Events, Services, Contact us, 3 services, 5 events) return 200 with
    the navigation; 0 `<script>`, 0 external resource URLs, exactly one stylesheet; fonts served
    from `/css/fonts/` (telemetry-excluded path).
  - No page has a `mainBanner` image: every banner and card renders the colour block, 0 `<img>`
    (spec `agency-site` "Page without an image"). Image branch not exercised.
  - Forms (Contact, SEO audit service, Umbraco Builders Day event): one
    `__RequestVerificationToken` and one `ufprt` per form; `method="post"` + `novalidate`; no
    `required`/`pattern`; `asp-*` expanded; valid post 302 then "Thank you, we'll be in touch." on
    that form only, refresh stores nothing (rows +1 per valid post via `sqlite3 -readonly`);
    invalid post 200 with entered values and field errors, nothing stored; post without token
    400, nothing stored.
  - Pending PO: past-event page check (and past-event post rejection), browser network panel.
  - Past-event session 2026-10-14, AppHost (SQL Server), event "Creating great editor experiences"
    moved to 2026-10-06 by the PO (uSync export not committed): PO confirmed the page shows "This
    event has ended" with no form and the event is gone from the Events list; Architect over HTTP:
    Home does not list it, the page loads only `/css/site.css` (no `<script>`, `<img>` or external
    URL). Event booking post (antiforgery token + `ufprt` from an upcoming event's form) to the
    past event: 200, no redirect, "This event has ended", no success text, no form; PO SQL count of
    rows from that post: 0. PO browser network panel: all requests to the site's own domain.

## Section 7 supervisor (2026-10-14, HEAD 244d2e6)

- APPROVED, no blockers or majors. `dotnet build SuperPower.slnx` 0 errors (recorded NU15xx/NU190x
  warnings); `dotnet test --solution SuperPower.slnx` 37/37. Read-only GETs on the AppHost: all page
  types 200, one stylesheet, 0 `<script>`, 0 `<img>`, 0 external URLs; colour blocks on every card;
  past event ended with no form and absent from Home/Events; forms carry one token + one `ufprt`,
  `novalidate`, POST, unprefixed input names; event times London local with correct offset across
  the 25 Oct clock change. ADR-0016 contract honoured; no date logic in views; fonts under
  `/css/fonts/` (telemetry-excluded). Minors fixed in ADR-0008, ADR-0019 and review-followups;
  nits in `review-followups.md`.

## Backend clean-up block (2026-10-14, review follow-ups)

- Dead submission read path removed (`InsertAsync` returns `Task`; `GetAsync`, `GetFieldsJsonAsync`,
  `FormSubmission`, `SubmissionJson.Deserialize` and its round-trip test gone); `EventAlias` moved to
  `EventSchedule`. Reviewer APPROVED, no findings: `dotnet build SuperPower.slnx --no-incremental`
  0 errors, 47 warnings (NU15xx/NU190x + pre-existing CS0618); `dotnet test --solution
  SuperPower.slnx` 36/36. Not run at runtime; next smoke run covers the insert path.

## Sections 3-5 supervisor (2026-10-14, HEAD e78babd)

- APPROVED, no blockers or majors. `dotnet build` clean apart from recorded warnings;
  `dotnet test` 37/37. Controller/partial binding contract, TempData success key, past-event
  guard (controller fail-closed), upcoming-events use in views, DI and `Acme.Forms`
  registration consistent; ADR-0016 and ADR-0014 conform; no form values in logs, traces or
  metrics at configured levels; no package leftovers.
- Coverage gap: spec `form-submission` "Both databases" has no post-re-plan evidence on SQL
  Server; task 3.2 unticked, re-verified via tasks 9.4 and 9.2.

## Section 9 (block 9A, smoke check)

- `add-demo-foundation` task 9.4, 2026-10-14, `dotnet run tests/smoke/smoke.cs` (worker, reworked;
  reviewer re-run, APPROVED): on a freshly started AppHost (no page loaded before the script) all
  checks PASS, exit 0: 13 pages 200; Contact, Service enquiry and Event booking submissions 302 +
  success text, one row each with form key, page key, UTC timestamp, field JSON; invalid
  submission 200 with field errors, 0 rows; Home trace (first load) 1 server span + 2 database
  spans, request log line carries its trace_id (spec `local-environment` "Page request trace",
  amended PO 2026-10-14); mail baseline delivered (<400 ms), Slow ~6.04 s delivered, Break fails
  in ~1 s, Heal delivered (<30 ms), delivery confirmed in smtp4dev. Second run on the warm
  AppHost: Home check FAILs with the restart message, exit 1 (as designed). Standalone target
  (SQLite): pages, Contact submission, invalid submission PASS.
- Contact `FieldsJson` asserted byte-for-byte against one literal on both targets (quote,
  non-ASCII, newline): identical on SQL Server and SQLite (spec `form-submission` "Both
  databases").
- `add-demo-foundation` task 3.2, existing databases (worker, via sqlcmd/sqlite3): SQL Server
  `acmeFormSubmission` Id int PK, FormKey uniqueidentifier (IX_acmeFormSubmission_FormKey),
  PageKey, SubmittedUtc datetime, FieldsJson nvarchar(max); SQLite equivalent; upgrader state
  `Acme.Site` at final state on both; restarts log "Running migration plan Acme.Site ... At
  <final state> ... Done" with no step executed. Fresh-volume / fresh-SQLite first boot: task 9.2.
- Not scripted (PO 2026-10-14): Event booking post to a past event; done manually in the past-event
  session 2026-10-14 (see block 7A entry above): rejected, 0 rows.

## PO change requests (2026-10-14 evening, uncommitted on e9bff8e)
Brand rename to Rubber Duck Agency (views), breadcrumb alignment, Contact "Reply by phone?"
removed, content (summary, `pageTitle`, `mainContent`) in uSync files, imported by the PO into
SQL Server only.
- `dotnet test`: 36 passed.
- Smoke, fresh AppHost start (SQL Server): ALL CHECKS PASSED — 12 pages, three valid
  submissions (rows 1029-1031), invalid submission 0 rows, Home first-load trace with 2 database
  spans and linked log, mail baseline 291 ms / Slow 6054 ms / Break 1008 ms failed / Heal 11 ms.
- Smoke `-- standalone` (SQLite): ALL CHECKS PASSED — 13 pages, Contact submission (row 14),
  invalid submission. Page count differs because SQLite still holds older content (the past event
  dated 29 Oct there, so it is listed); task 9.2 rebuilds both databases from the uSync files.

## Task 9.2 / 3.2 fresh SQL volume, first boot (2026-10-15, PO run, `sample-site` after `rework-aspire-first` merge)
- Wiped: AppHost stopped, container `sql-d0adb38c` and volume `superpower.apphost-d0adb38ccd-sql-data`
  removed, `umbraco/Data/TEMP` deleted (PO, per Architect steps).
- First boot, then `dotnet run tests/smoke/smoke.cs` before any browser request: ALL CHECKS PASSED.
  pages 200 (12 pages); Contact/Service enquiry/Event booking rows 1, 2, 3 (new table, "Acme.Site"
  plan ran on SQL Server); invalid submission 0 rows; Home first-load trace 1 server span, 9
  database spans, linked log; no pre-start telemetry in Grafana; mail baseline/slow/break/heal.
- Open for 9.2: browser check of all page types, events, services, forms, media/images (or colour
  fallback); second start with no duplicated content or media; fresh SQLite standalone run.
- Open for 3.2: second start changes nothing (plan-skipped log line); byte-identical `FieldsJson`
  round-trip on SQL Server and fresh SQLite (PO SQL query); fresh SQLite first boot.
