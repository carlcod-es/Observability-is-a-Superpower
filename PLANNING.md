# Planning (temporary)

Working list of unknowns, open decisions and todos. Delete this file, and its
reference in CLAUDE.md, before the talk.

Rule: no demo answers here (planted problems, their causes or fixes). CLAUDE.md
references this file, so the on-stage agent may read it.

## Next step
- [x] `add-demo-foundation` (30/30, section 9 supervisor APPROVED) and `rework-aspire-first` (12/12,
  ADR-0022) archived 2026-10-15 to `openspec/changes/archive/2026-10-15-*`; specs synced to
  `openspec/specs/` (agency-site, agent-telemetry-access, form-submission, local-environment).
  Open items carried to Todos below and FR-0004.
- [x] `add-mobile-responsive` (2026-10-14): applied and merged to `main` (PR #3), 9/9; task 4.2
  PO browser check done 2026-10-15; archived 2026-10-15, spec `responsive-layout` synced.
- [x] `add-site-prototype`: complete and merged to `main` 2026-10-14; `prototype/` deleted by the PO
  2026-10-14 (ADR-0021); archived 2026-10-15 without spec sync (prototype removed).
- [x] `spike-grafana-dashboard` run 2026-10-14 (PO reviewed in the UI, hosts stopped). Result in its
  `findings.md` section 6: one custom JSON-first demo dashboard (request rate, 5xx rate, p95 with
  trace exemplars, form meters, Loki error logs), drill-down through the bundled Logs/Traces
  Drilldown apps, JSON in the repo. Open: its task 4.4 (provisioning, not attempted), now FR-0001.
- [x] `add-demo-dashboard` (branch `add-demo-dashboard`; ADR-0023, ADR-0024 accepted; FR-0001, FR-0003): PO
  sign-off 2026-10-15 on Windows; archived 2026-10-15 to
  `openspec/changes/archive/2026-10-15-add-demo-dashboard`, specs synced (new `demo-dashboard`). Commit pending.
  Built: dashboards provisioned from the repo into `lgtm` (folder Demo: Demo site, ASP.NET Core 19924), page-type label `acme.page_type`, `/assets` excluded, `tests/traffic/traffic.cs`.
  Smoke checks and README done. Smoke `aspire` launch fails on Windows (.NET-tool shim), accepted by the PO;
  follow-up in its `review-followups.md`. Form panel added in `add-form-emails`.
- [x] `add-form-emails` (ADR-0025/0026/0027) and `add-log-redaction` (ADR-0028) archived 2026-10-16
  to `openspec/changes/archive/2026-10-16-*`; specs synced (new `log-privacy`; `form-submission`
  gains "Form hidden after submit"). Open PO tasks carried to Todos below.
- [x] Spike closure (PO 2026-10-13): keep both spikes open for reference until pre-talk cleanup
  (Todos).

## Product requirements confirmed in explore (2026-10-06, revised 2026-10-08, 2026-10-13)
- Keep it simple (PO 2026-10-13, CLAUDE.md "Principle"): basic demo, no edge cases.
- Site: fictional marketing agency, Rubber Duck Agency (replaces the pottery studio). Pages: Home,
  About us, Contact us, Events list, event pages, Services list, service pages.
- Services (e.g. Umbraco upgrade, SEO audit, AdWords audit): Service enquiry form.
- Events (e.g. "How to get the most out of your CMS"): free; title, description, start date and
  time; no format, capacity or places. Past events show "This event has ended", no form. No payment.
- Forms (three; the event or service comes from the page, not a field). After a valid
  submission: "Thank you, we'll be in touch. Your reference is ACME-nnnn."
  - Contact: Name, Email, Message ("Reply by phone?" dropped, PO 2026-10-14). Owner notification.
  - Service enquiry: Name, Email, Company (optional), Website (optional), Message. Owner
    notification (submitter confirmations dropped, PO 2026-10-15).
  - Event booking: Name, Email, Notes (optional), "Join mailing list" checkbox. Owner
    notification. An ordinary submission: no booking status,
    confirmation link, waiting list or limit per email (PO 2026-10-13).
- Stored submission: field values, form, page, UTC timestamp, reference. No IP or browser info.
- Agent never accesses the database; demo personal data is in telemetry only.
- Agent telemetry by stage (PO 2026-10-15, ADR-0022, replaces PO 2026-10-13 "Grafana MCP only"):
  local stage Aspire MCP (traces, logs, resource state, console logs; commands prompt);
  production stage Grafana MCP (read-only logs, traces, metrics), Grafana started at go-live.
- Never recorded in telemetry: Name, Message, Notes, Company, Website, email address. Fine:
  form, event or service, outcome.
- Owner dashboard of bookings: stretch goal.
- Prod data: scripted submissions over the days before the talk (repeat submitters, validation
  failures, all forms, bookings across events), plus attendees submitting live via QR code.
- Prod attendee handling: notice on all forms (live demo, deleted within 7 days); any form may use
  a made-up email; delete prod submissions within 7 days of 16 Oct (or by taking prod down); one
  presenter switch closes every form; real emails sent (superseded PO 2026-10-15: no submitter
  emails).
- Prod owner mailbox: presenter-owned demo alias (prod settings only, never in the repo).
- Forms simplified (2026-10-10): no separate forms package; the talk no longer promises a
  custom package. Three fixed forms in the site; the page type decides the form. No editor-
  defined forms, fields or form text. No format rules beyond Email.
- Email settings (`add-form-emails`): owner address is an environment setting; subjects and
  bodies are fixed text. No backoffice settings. Each submission gets a reference `ACME-nnnn`
  shown on the success message (PO 2026-10-15).
- Act 4 split: resolved (presenter-only notes).

## Change order (2026-10-08, revised 2026-10-13)
1. `add-demo-foundation`: agency site, events and services, three forms.
2. `add-demo-dashboard`: demo Grafana dashboard, provisioning, demo traffic (PO 2026-10-14).
3. `add-form-emails`: all emails.
3a. `add-log-redaction` (PO 2026-10-15, ADR-0028): layer 1 logging rule + in-app log
   redaction processor; Serilog console sink removed. Archived 2026-10-16.
3b. `improve-local-performance` (PO 2026-10-15, ADR-0029 proposed): stage run in Release,
   views compiled at build, CMS debug off, M1 machine checklist; before/after telemetry.
   Proposed, not applied.
3c. `add-booking-api` (PO 2026-10-21, ADR-0030/0031/0032 accepted 2026-10-22): booking count on event pages via a
   separate API, Redis cache and RabbitMQ queue for bookings; cross-service traces; no planted problems yet.
   Archived 2026-10-22 (`openspec/changes/archive/2026-10-22-add-booking-api`); specs synced (new
   `event-booking-count`). Task 6.2 (Windows run) skipped by the PO: demo on the Mac only.
3d. `extend-demo` (PO 2026-10-22, ADR-0033/0034 accepted 2026-10-23): Act 3 (booking processing failure,
   marked toggle), counted-bookings metric and dashboard panel, Grafana MCP throughout the talk, smoke
   `-- running` mode. Applied 2026-10-23 except PO task 5.1 (rehearsal).
4. `add-production-environment`: Cloudflare tunnel and database reset only (Azure dropped, PO
   2026-10-15).
`add-event-bookings` dropped 2026-10-13 (change directory removed). `add-talk-slides` dropped
2026-10-15 (PO; change directory removed, not archived).

## Open decisions
- [x] Prod hosting (PO 2026-10-15): local environment behind the Cloudflare tunnel; Grafana
  plays production; Azure dropped. Earlier notes (PO 2026-10-13): working assumption is the local environment behind a
  Cloudflare tunnel as baseline and fallback, built first; Azure built only if time allows. PO
  makes the final choice before it is needed. Implications of the tunnel baseline, to resolve
  in `add-production-environment`:
  - "Read-only AI access to production observability": prod telemetry is the same local stack
    the agent already reads; decide what is shown as production and how access is controlled.
  - Real emails to attendees: moot; submitter confirmations dropped (PO 2026-10-15), owner
    notifications stay in smtp4dev.
  - Scripted prod submissions over the days before the talk: needs the laptop running for days,
    or generate the history shortly before.
  - Load on stage (burst tool) shares the site with attendees submitting via QR code (fault
    injection removed, ADR-0025).
  - Venue network: adds a public inbound dependency ("everything local except the LLM" no
    longer holds).
  - Architect: ADR-0008 (Azure Monitor exporter) and ADR-0009 (Azure SQL for deploy parity)
    lose their reason if Azure is never built.
- [x] Prod site after the talk (PO 2026-10-13): taken down after the talk. Azure: deleting the
  resources deletes attendee submissions. Tunnel: close the tunnel, reset the local database
  and telemetry.
- [x] Telemetry MCP (PO 2026-10-13): logs, traces and metrics queries go to the Grafana MCP
  (covers all three, `spike-metrics-mcp` findings). Architect: revise ADR-0011.
- [x] Aspire MCP (PO 2026-10-13): dropped; reinstated 2026-10-15 for the local stage
  (`rework-aspire-first`, ADR-0022). Resource commands prompt each call; the presenter runs
  them in the dashboard.
- [x] Event bookings (PO 2026-10-13): Event booking is an ordinary submission; `add-event-bookings`
  dropped.

## Unknowns to spike
Decided in ADRs; verified by `add-demo-foundation` tasks:
- [x] Umbraco 17 Serilog -> OTLP via forwarding sink (ADR-0007): task 1.3, passed 2026-10-08.
- [x] SqlClient spans for Umbraco's NPoco queries; background span volume (ADR-0008): task 2.4,
  passed 2026-10-09 (`verification.md`).
- [x] smtp4dev and Toxiproxy in the AppHost, Slow/Break/Heal commands (ADR-0010): tasks 2.2, 2.3,
  passed 2026-10-09 (`verification.md`). Toxiproxy removed 2026-10-15 (ADR-0025).
- [x] Date Time with time zone stored value shape (ADR-0014): task 3.4, confirmed.
- [ ] LGTM stack, Collector and Grafana MCP on Windows (ADR-0017; spike ran on macOS): task 9.3.
  `spike-grafana-dashboard` ran the spike stack on Windows 2026-10-14 without Windows-specific
  issues; Aspire CLI must match the AppHost SDK (13.4.6 CLI vs 13.6.1 SDK kills the AppHost).
- [x] Grafana dashboard provisioning in `grafana/otel-lgtm` via bind mount (`/otel-lgtm/grafana/conf/provisioning/dashboards/`): works on macOS and Windows
  2026-10-15 (`add-demo-dashboard` tasks 1.1, 6.1). FR-0001
  (`docs/feature-requests/0001-grafana-dashboard-provisioning.md`).
For `add-production-environment`, tunnel baseline:
- [ ] Cloudflare tunnel to the local site: public URL for the QR code, only the site exposed
  (no dashboard, Grafana, smtp4dev or backoffice).
Azure stretch: dropped (PO 2026-10-15); items kept for reference only.
- [ ] Azure MCP Server: read-only mode, Monitor/App Insights query tools, running under a limited identity (Monitoring Reader + Log Analytics Reader).
- [ ] Azure Communication Services Email via SMTP from Umbraco IEmailSender.
- [ ] Umbraco 17 on Azure Container Apps: deploy via azd / `aspire deploy`, cold start, Examine/temp on local disk.
- [ ] Azure Monitor exporter switch (ADR-0008; code in task 2.4) verified against Application Insights.

## Todos
- [x] Static site prototype (`add-site-prototype`, ADR-0018/0019) used as the basis for the
  Umbraco views: `add-demo-foundation` task 7.1 done 2026-10-14; `main` merged into `sample-site`.
- [ ] Pre-talk cleanup of both spikes (PO 2026-10-11, reconfirmed 2026-10-13: keep open for
  reference until then): `spike-aspire-probe` task 3.4 and `spike-metrics-mcp` task 3.6 —
  delete `spikes/` (includes `spikes/metrics-mcp/dashboards/` from `spike-grafana-dashboard`;
  copy any dashboard JSON the demo adopts out first), remove `Microsoft.Data.SqlClient` from `Directory.Packages.props` (keep the
  toolkit package, used by `add-demo-foundation` task 2.5), root build. All three spike changes
  archived 2026-10-15 to `openspec/changes/archive/2026-10-15-spike-*` with these tasks open.
  Keep `spikes/` out of the stage clone and the published repo.
- [ ] Carried from `add-form-emails` (archived 2026-10-16), PO:
  - Task 6.1: stage clone of `main` per `docs/presenter/runthrough.md` (no demo branches, PO 2026-10-16);
    stage clone has no `docs/presenter/`. Decide whether ADR-0025, ADR-0026/0027 Consequences and the
    `local-environment` spec stay readable in the stage clone.
  - Task 6.2: end-to-end rehearsal on the stage machine; timings in `docs/presenter/runthrough.md`.
  - Fresh SQL Server migration at the pre-talk DB reset, then AppHost smoke (expect exactly one
    FAIL, "no pre-start telemetry in Grafana").
- [ ] Carried from `add-log-redaction` (archived 2026-10-16) task 4.2, PO: place the "console logs
  disabled" talking point in `docs/presenter/runthrough.md`; the ADR-0028 line in `Extensions.cs` is
  commented out on `main` (`// Demo Code : Act 1`); check during the rehearsal.
- [ ] After the talk: smoke pre-start FAIL fix (`timeout: 2s` on collector exporter
  `otlphttp/lgtm`, or an unproxied lgtm OTLP endpoint); remaining minors and nits in the archived
  `review-followups.md` of both changes.
- [x] Demo fallback branches: dropped (PO 2026-10-16). Planted problems live on `main` as
  `// Demo Code : Act N` comments; fallback is applying the fix by hand. Stage uses one `demo`
  branch with docs, OpenSpec, planning and agent workflow files deleted (presenter runthrough "Demo branch").
  Built in the stage clone only (runthrough "Building the demo branch"); never pushed or merged.
  Stage CLAUDE.md drafted as `claude-stage.md` (renamed to `CLAUDE.md` on `demo`); PO to review.
- [x] Demo-specific CLAUDE.md for stage: `claude-stage.md`, renamed to `CLAUDE.md` on `demo`, which also
  drops `.claude/agents/`, `.claude/commands/` and `.claude/skills/` (2026-10-16). Open, PO: whether the
  published repo keeps them and the OpenSpec planning.
- [ ] Script and rehearse agent prompts; rehearse with `/fast`. Present in default permission
  mode: auto mode approves Aspire resource commands without a prompt (`rework-aspire-first`
  task 6.1, 2026-10-15).
- [ ] Record fallback videos of agent segments.
- [ ] Tunnel baseline: start the environment and tunnel before the talk; smoke test the public
  URL the morning of.
- [ ] Stage layout: VSCodium + Claude CLI terminal, browser tabs (dashboard, smtp4dev, site).
- [ ] Venue network plan: everything local except the LLM.
- [ ] Carried from `add-demo-foundation` review-followups.md before archive (2026-10-15):
  - PO decisions: trace backoffice page loads `/umbraco`, `/umbraco/login` or not; replace the
    Umbraco default `favicon.ico`; replace Umbraco's default 404 page (umbra.co links), visible
    to attendees through the tunnel.
  - Stage PII rules: never lower Umbraco/Serilog levels to Debug while forms are submitted
    (failed inserts log `FieldsJson`); keep personal data out of query strings (Development
    "Request starting" logs log the full query); do not show `aspire describe` output (SQL sa
    password).
  - `add-production-environment`: `/health` and `/alive` mapped only in Development;
    possible duplicate logs/spans and filter bypass with `UseAzureMonitor` (moot if Azure stays
    dropped); `.WithExternalHttpEndpoints()` on smtp4dev.
- [ ] FR-0004 (2026-10-15): review nits from `add-demo-foundation` (31 items, code, views, tests,
  README, content); after the talk unless the PO pulls items forward.
- [x] FR-0006 (2026-10-16): delivered by `add-booking-api` (2026-10-22).
- [x] `add-booking-api` PO tasks 6.1 (runthrough reset) done, 6.2 skipped (Mac only); archived 2026-10-22.
- [ ] Windows unverified for `api` / `cache` / `queue` (spec `local-environment` "Cross-platform"); attendees on
  Windows may hit it. PO 2026-10-22: not before the talk.
- [x] New act for queue errors: delivered as Act 3 by `extend-demo` (2026-10-23).
- [ ] `extend-demo` task 5.1 (PO): copy `.temp/runthrough.md` to `docs/presenter/`; rehearse Morning and Act 3
  with the agent on the Grafana MCP only; confirm `/mcp` -> `aspire` -> Disable shows `aspire` disabled in
  `claude mcp list` (`extend-demo` task 3.3).
- [ ] Before the talk (PO): decide whether the `demo` branch strips the explanatory lines under each
  `// Demo Code : Act N` (runthrough "Demo branch"; Act 3's names the fix). Review
  `openspec/changes/extend-demo/review-followups.md`.
- [ ] Runthrough uses Rebuild (not Restart) to apply fixes in every act (Restart does not recompile;
  found 2026-10-23). Rehearse it for Acts 1 and 2 too.
- [ ] After the talk (PO): add the slides to the repo for attendees.
- [ ] Before the talk: remove demo-plan entries from Claude memory, or run the demo
  from a different clone path (memory is keyed to the folder path and loads on stage).

## Decision rationale (confirmed decisions in CLAUDE.md)
- Fixed forms in the site (2026-10-10, replaces Block List field definitions): editor-defined
  forms added build and backoffice work without adding to the observability story.
- NPoco + PackageMigrationPlan over EF Core: provider-agnostic from one migration;
  EF Core needs per-provider migration sets. SqlClient instrumentation gives DB spans on
  SQL Server (including Umbraco's own queries); SQLite gets none.
- smtp4dev over MailPit: presenter familiarity. Toxiproxy fault injection removed 2026-10-15
  (ADR-0025).
- Claude Code CLI on stage: presenter's normal workflow; Copilot is not available in VSCodium.
- Prod (either hosting): runs the finished build; Act 4 prod segment is about PII not
  reaching the observability platform. Tunnel first because it is the shorter route to a
  public site before 16 Oct (PO 2026-10-13).
