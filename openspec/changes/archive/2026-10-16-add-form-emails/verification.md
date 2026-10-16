# Verification: add-form-emails

## Architect runtime checks, 2026-10-15 (macOS, AppHost, existing SQL Server volume)

- 1.1: `aspire describe` after start: collector, sql, umbraco, smtp4dev, site Running; lgtm
  NotStarted; grafana-mcp Waiting; no `toxiproxy` resource. smtp4dev SMTP `tcp://localhost:2525`.
  `dotnet build SuperPower.slnx`: 0 errors.
- 2.1 (SQL Server): site console `Execute AddSubmissionReferenceStep`, `Migration plan Acme.Site
  finished, final state 7d2f9c14-6e03-4b5a-8f1d-c3a5e90b2d68`, no errors. Trace of a Contact post
  shows `UPDATE umbracoLock ... WHERE id=@?`, `SELECT MAX ([acmeFormSubmission].[ReferenceNumber])`,
  `INSERT ... [ReferenceNumber]` inside `acme.forms.submit`. Burst 50/10: `ok=50 failed=0`,
  `Distinct references: 50` (ACME-0004..ACME-0053). First reference on this database was
  ACME-0001: confirmed by the PO query (see below).
- 2.1 (SQLite, worker block 2A): standalone on the existing file: `Execute AddSubmissionReferenceStep`,
  plan finished, no errors; posts stored as ACME-0002, ACME-0003 (one existing row backfilled to 1).
- 2.2: Contact post trace fb03619b...: server span POST 302 -> `acme.forms.submit` (acme.form
  contact, acme.form.outcome stored, acme.submission.reference ACME-0001) -> child
  `acme.forms.notify` (contact, ACME-0001, 137 ms). Log `Form contact stored as ACME-0001` on the
  submit span. smtp4dev: one email to owner@acme-agency.example, subject `New Contact submission
  ACME-0001`, body with form, page "Contact Us", reference, UTC time, labelled fields. Standalone
  post: success, no email, no error (worker block 2A, skip branch).
- 3.1: burst `--count 3 --parallel 1` posted Contact, Service enquiry, Event booking: ACME-0001..0003
  read from each success page. curl Contact post: "Thank you, we'll be in touch. Your reference is
  ACME-0054."; refresh of the redirect page shows no success text; smtp4dev count 53 -> 54 (one
  row per post).
- 4.1: burst defaults: `ok=50 failed=0`, 50 distinct references; smtp4dev 3 -> 53 messages, 53
  distinct subjects. `--count 3 --parallel 1` and `--count 6 --parallel 2` work.
- 1.2: lgtm started, burst 6, one invalid Contact post. Grafana `/api/ds/query` with the panel
  expr returned series contact/stored, service_enquiry/stored, event_booking/stored (max 2.09
  per minute) and contact/invalid (0.00: a series' first sample after go-live has no increase;
  cumulative counters, ADR-0022).

## PO checks
- 2.1: count/reference query on SQL Server `umbraco` (PO, 2026-10-15): 159 rows, 159 with reference,
  159 distinct, min 1 / max 159, `ReferenceNumber <> Id` 0 (table empty before the migration).
  `IX_acmeFormSubmission_ReferenceNumber` is_unique. `umbracoLock` 800001 `AcmeSite` value -1
  (Umbraco flips 1/-1 on each write lock). `umbracoKeyValue` `Umbraco.Core.Upgrader.State+Acme.Site` =
  7d2f9c14-6e03-4b5a-8f1d-c3a5e90b2d68. Latest rows 155-159: reference = Id, all three forms.
- 5.2: `.gitignore` has `.presenter/`; `git status --short --ignored .presenter` -> `!! .presenter/`,
  not in `git status --short`. `.claude/settings.json` `permissions.deny: ["Read(./.presenter/**)"]`;
  a worker's Read of `.presenter/runthrough.md` was denied in-session (block 5B, reviewer APPROVED).
- 5.1 (block 5A, reviewer APPROVED): smoke `-- standalone` ALL CHECKS PASSED (reference ACME-0004
  matches row 4). AppHost target, two fresh starts (worker 17:48Z, Architect 17:51Z): all PASS incl.
  reference = stored row (ACME-0061..63, ACME-0064..66) and "notification email ... smtp4dev has
  'New <form> submission ACME-nnnn' to owner@acme-agency.example" for all three forms, EXCEPT
  `no pre-start telemetry in Grafana` FAIL both times: the two request log lines of
  `/smoke-pre/` (17:51:31.96Z) reach Loki after `lgtm` starts. Collector and lgtm config not
  changed by this change. PO 2026-10-15: accepted, 5.1 ticked; follow-up logged.
- 5.3: after smoke (lgtm running), Contact, Service enquiry, Event booking posted with Name
  `Canary Person`, Email `canary@test.invalid` (+ `canary` in Message/Company/Website/Notes), one
  invalid Contact post with the same values, then burst defaults (`ok=50 failed=0`, 50 distinct).
  Aspire MCP: structured logs "canary" 0, "Canary Person" 0, traces "canary" 0 (control: "stored as
  ACME-0067" 1). Loki `{service_name=~".+"} |~ "(?i)canary"` 0 (control `|= "stored as"` 56).
  Tempo: all 189 server traces of the last 30 min fetched, 58 with `acme.forms.submit`, 0 contain
  "canary". Prometheus series `{job="site"}`: 0 contain "canary". Demo dashboard queries: form
  panel contact/stored max 18.3/min, service_enquiry 18.3, event_booking 17.3, contact/invalid
  1.0; 5xx panel returns 0 (data, no errors).
- 5.4: README (block 5C), `docs/project-setup.md`, CLAUDE.md, PLANNING.md updated; reviewer checked
  every port, path, command, config key, metric/span/tag/log name and email subject against the
  code: APPROVED; minors applied by the Architect.

## Section 1-4 supervisor follow-up evidence, 2026-10-15
- "Many visitors at once" (spec form-submission): `burst.cs -- --count 40 --parallel 20` on the
  AppHost (SQL Server): `ok=40 failed=0`, `Distinct references: 40`.
- Fresh-database path (design.md risk "then on a fresh database"): standalone site on a new empty
  SQLite file in the Architect's scratchpad (env `ConnectionStrings__umbracoDbDSN`, TEMP moved to
  EnvironmentTemp; repo databases untouched). Unattended install + uSync import, Contact 200
  after 10 s. Log: `Execute CreateSubmissionTableStep`, `Execute AddSubmissionReferenceStep`,
  `Migration plan Acme.Site finished, final state 7d2f9c14-...`, no ERR/FTL (5 WRN "database not
  available" probes before the file is created, Umbraco's own pre-install check). Burst 6/3:
  `ok=6 failed=0`, ACME-0001..ACME-0006, log `Form <form> stored as ACME-000n` for all three forms.
  Fresh SQL Server volume not run (needs a volume reset: PO, `docs/demo-setup.md`).
- Spec "Invalid submission sends nothing": accepted on code inspection - `AcmeFormsController`
  returns before the notification for invalid model state and for the past-event rejection
  (section 5 supervisor).
- 5.3 addendum: metrics checked in Prometheus only; the Aspire MCP has no metrics tools. Metric
  tags are fixed slugs by construction (`acme.form`, `acme.form.outcome`).
- Sections 1, 3, 4 supervisor APPROVED; section 2 APPROVED after the fresh-database and 40/20
  evidence above and the 2.1 PO query; section 5 supervisor APPROVED (2026-10-15).
