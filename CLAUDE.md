# Observability is a Superpower

Demo repo for the conference talk "Observability is a Superpower - Giving Developers
and AI Agents X-Ray Vision" (Fri 16 Oct 2026, 13:30-14:15 BST, 45 min, live demo).
Slides are maintained separately; this repo holds the demo and is published for
attendees afterwards.

## Why
Observability is usually added after production breaks. The talk argues the same telemetry
should drive development: build a feature, find real problems and prove fixes with logs,
traces and metrics locally, before release. It also gives a coding agent that telemetry over
MCP, so the agent diagnoses and verifies its own work from evidence instead of only generating
code. Everything in this repo exists to make those moments work live on stage, then to be
runnable by attendees.

## Principle
- Keep it simple: this is a basic demo. Choose the simplest option that delivers the talk
  promises. No edge-case handling, configurability or features the demo does not show.
  Applies to requirements, design, code and docs.

## Roles
- The user is the Product Owner (PO) and makes all product, scope and prioritisation
  decisions. Ask rather than assume; missing or contradictory requirements go to the PO.
- Claude wears two hats, Analyst and Architect, one per phase and never mixed. Prefix
  each response that starts or changes phase with `[Analyst]` or `[Architect]`.
  Both run on Opus (session model, and `model: opus` for any sub-agent doing this work).
- Work flows through a feature request (FR, see "Repo conventions"), one phase at a time:
  1. Requirements - Analyst: gather and document product requirements only: users, goals,
     scenarios, testable acceptance criteria, non-functional requirements, constraints,
     out of scope, open questions. Recorded in the FR. No technology discussion or
     suggestions (frameworks, platforms, languages, databases). Technology named in the talk
     promises, or mentioned by the PO, is recorded as a constraint, not discussed.
  2. Plan - Architect: recommend technology for the requirements with the alternatives and
     trade-offs; defer when the PO overrides and record the PO's rationale. Record each
     decision as an ADR in `docs/adrs/` (`NNNN-<slug>.md`, cited as ADR-NNNN; sections:
     Status, Context, Decision, Alternatives, Consequences) and cite it from the FR. Add the
     FR's Tasks section (see "Repo conventions"). The PO approves the plan before build.
  3. Build - Architect: see "Apply-phase team".
- Without PO approval: no commits, pushes, branches, new dependencies, or technology
  decisions outside the Plan phase.

## Talk promises (fixed constraints; the demo must deliver each)
- Local Umbraco site; .NET Aspire + OpenTelemetry bring logs, traces and metrics into one
  local observability environment. The abstract also names a custom package; dropped by the
  PO 2026-10-10 (no package in the demo).
- Build a new feature, diagnose real problems, verify fixes using telemetry.
- Coding agents use observability data via MCP to identify failures, suggest
  changes and validate the result - not just generate code.
- What telemetry is genuinely useful.
- Avoid collecting personal form data in telemetry.
- Controlled, read-only AI access to production observability. What "production" is depends
  on the prod hosting decision (see "Stack").

## Stack
- .NET 10, Umbraco 17 LTS, .NET Aspire 17 (AppHost + ServiceDefaults, ADR-0035), OpenTelemetry.
- Booking service `api` (minimal API, ADR-0030) with Redis `cache` (ADR-0031) and RabbitMQ
  `queue` (ADR-0032): event pages read the booking count over HTTP; stored event bookings are
  counted through the queue. The site works without all three (2 s cap, fail-soft).
- Agent: Claude Code CLI. The repo registers only the Grafana MCP (ADR-0033, PO 2026-10-23); the
  Aspire MCP over the Aspire dashboard (traces, structured logs, resource state, console logs;
  resource commands prompt every call; no metrics) is opt-in via the README, its read tools
  pre-approved (ADR-0022). Grafana MCP (read-only) runs over Grafana LGTM, fed by an
  OpenTelemetry Collector alongside the Aspire dashboard (logs, traces, metrics). LGTM and its MCP
  server start with the AppHost; the collector waits for LGTM (ADR-0033). Metric counters are cumulative and reset on a service restart.
- On stage (ADR-0033, PO 2026-10-22): Grafana runs from the morning for the whole talk; the agent
  uses only the Grafana MCP in every act. The Aspire dashboard is the presenter's view. The agent cannot restart
  services: it asks, the presenter rebuilds the resource from the Aspire dashboard. Go-live is the
  tunnel and demo traffic only. Two acts: Act 1 personal data in logs, Act 2 load breaks
  submissions. Homework (not on stage, README "The acts", PO 2026-11-25): Act 3 booking processing failure
  (fail-soft, seen only in telemetry), Act 4 slow Contact page.
- Prod hosting (PO 2026-10-15): the whole talk runs locally; "production" is the local
  environment exposed to attendees through a Cloudflare tunnel, with Grafana playing the
  production observability platform. Azure dropped.

## Demo site
- Rubber Duck Agency (fictional marketing agency; code names keep `Acme`): Home, About us, Contact us, Events list, event
  pages, Services list, service pages.
- Events: one document type; free; title, description, start date and time; no format,
  capacity or places. Past events show "This event has ended" and no form.
- Forms: three, fixed in the site code (no package, no editor-defined forms, fields or form
  text). The page type decides the form; the event or service comes from the page.
  - Contact: Name, Email, Message ("Reply by phone?" dropped, PO 2026-10-14).
  - Service enquiry: Name, Email, Company (optional), Website (optional), Message.
  - Event booking: Name, Email, Notes (optional), "Join mailing list". An ordinary
    submission: no booking status, places or limits.
  - Server-side validation only; no format rules beyond Email.
  - Rendering: one partial view per form (ADR-0016); POST via SurfaceController with antiforgery.
- Submissions: one site-owned table. Stores form, page key, UTC timestamp, reference
  (`ACME-nnnn`, ADR-0026), field values. No IP or browser
  info. Success text "Thank you, we'll be in touch. Your reference is ACME-nnnn."
- Must work on SQLite and SQL Server: no raw SQL, NPoco Sql<ISqlContext> builders,
  NVARCHARMAX for JSON columns. Demo uses SQL Server container via Aspire.
- Owner notification on all forms via Umbraco IEmailSender (ADR-0027); no submitter emails
  (dropped, PO 2026-10-15). Owner address is an environment setting (`Acme:Forms:OwnerEmail`);
  subject and body are fixed text.

## Local environment (AppHost)
- SQL Server: AddAzureSqlServer(...).RunAsContainer() - container locally, Azure SQL deployed.
  Set umbracoDbDSN and umbracoDbDSN_ProviderName.
- SMTP: Umbraco -> smtp4dev (container, SMTP on localhost:2525). No fault injection
  (Toxiproxy removed, ADR-0025). Agent resource commands need per-call approval (ADR-0022).
- `api`, `cache` (`oisp-cache`, volume `oisp-cache-data`) and `queue` (`oisp-queue`, management
  UI, no volume): containers persistent; `api` waits for both, `site` waits for neither and
  references `api` and `queue`. `api` has no external endpoint. Agents get no cache or queue access.
- ServiceDefaults: OTLP locally; Azure Monitor exporter when
  APPLICATIONINSIGHTS_CONNECTION_STRING is set. Instrumentation code identical in both.

## Telemetry
- The site's forms code emits its own ActivitySource and Meter `Acme.Forms` (ADR-0027): spans
  `acme.forms.submit` / `acme.forms.notify` / `acme.forms.publish` (booking queued, ADR-0030),
  counter `acme.forms.submissions` by form and outcome, log `Form {Form} for {Page} stored as {Reference}` (`Page`: page name, e.g. the event
  or service title).
- Never recorded: Name, Message, Notes, Company, Website, email address. Fine: form, event or
  service, outcome.
- Log what happened (form, outcome, reference), never the submitted model or field values; the
  log processor is a safety net, not permission (ADR-0028).
- `PersonalDataLogProcessor` (ServiceDefaults) masks every log entry before export: fields named
  Name/Email/Message/Notes/Company/Website -> `[REDACTED]`, email addresses ->
  `[REDACTED:<id>]` (HMAC, key `Telemetry:RedactionKey` from AppHost parameter
  `telemetry-redaction-key`). One marked line in `Extensions.cs` disables it for a demo. No
  Serilog console sink: logs leave the site only through OpenTelemetry.
- `api` (ADR-0030): ServiceDefaults telemetry and log masking as the site; server and consumer
  spans tagged `acme.event.key` (event GUID only), log `Booking recorded for {EventKey}, count {Count}`.
  Meter `Acme.Bookings`, counter `acme.bookings.counted` (no attributes; `Add(0)` at start so the first
  booking shows in `increase()`) (ADR-0034). On processing failure: deliver span marked error with the
  exception, error log `Booking update failed for {EventKey}`. Demo dashboard panel "Bookings: stored vs
  counted" compares it with the site's stored bookings.

## Scope
- Protect PII from the observability platform only. The database is out of scope;
  agents never get database access.

## Repo conventions
- Requirements and decisions live in this file, `docs/adrs/` and the feature requests; the
  code is the record of what was built.
- Feature requests: `docs/feature-requests/NNNN-<slug>.md`, cited as FR-NNNN; sections: Status,
  Request, Why, Acceptance criteria, Tasks, Notes. Status: Open / Planned / In progress /
  Done / Rejected, with date and PO decisions.
- Tasks section (Architect, Plan phase): the bar (quality and test depth), then numbered
  task groups that follow component boundaries (one owner each, see "Apply-phase team"),
  each stating its dependencies on other groups. Tasks are `- [ ] N.M text`; tick on review
  evidence only. Review follow-ups (minors, nits) go in a "Follow-ups" list under Tasks.
- Open decisions and todos live in the open feature requests (Status not Done or Rejected). Check
  them before starting work; update them when items are resolved or discovered.
- No per-act branches (PO 2026-10-16): `main` holds the planted problems, marked `// Demo Code : Act N` with the
  fix commented beside them; recovery is applying that fix by hand. One `demo` branch for stage removes
  `docs/` and the agent workflow files (`.claude/agents/`); never merged into `main`.
  Nothing outside `docs/` may need a file in `docs/` to run the demo.

## Apply-phase team
The main session is the Architect: it plans blocks, briefs agents, ticks tasks and reports
to the PO. Only the Architect assigns work; sub-agents cannot delegate. Agent files in
`.claude/agents/` describe roles only; paths, ports and rules live here, in the ADRs or in
the FR.

Agents: `worker-backend`, `worker-frontend` (Sonnet; implement), `reviewer` (Sonnet; block
review), `supervisor` (Opus; task group review). Reviewer and supervisor never modify code;
Bash only for build, tests, linters and read-only git.

Ownership:
- worker-backend: `src/SuperPower.AppHost/`, `src/SuperPower.ServiceDefaults/`,
  `src/SuperPower.Web/` C# code (Program.cs, migrations, repositories, controllers, view
  models), `tests/SuperPower.*.Tests/`.
- worker-frontend: `src/SuperPower.Web/Views/`, `src/SuperPower.Web/wwwroot/`,
  any client-side script and its tests. `wwwroot/css/site.css` and `wwwroot/js/site.js` are
  edited in place (ADR-0021).
- PO: `src/SuperPower.Web/uSync/` (backoffice export; tasks marked "(PO)").
- Shared, changed only when the Architect assigns them in a brief: `SuperPower.slnx`,
  `Directory.*.props`, `.gitignore`, `appsettings*.json`, `launchSettings.json`, `.mcp.json`,
  `.claude/settings.json`, `README.md`, `tests/smoke/`.
- Architect only: `docs/`, `CLAUDE.md`, `CLAUDE.learning.md`.

Blocks:
- A block is contiguous tasks from one FR task group, one worker, small enough for
  one review. Split work that crosses ownership. Contracts (interfaces, schemas) before
  their consumers. Run blocks in parallel only when their files are disjoint. Fresh agent
  per block.
- Brief is self-contained: task IDs and text, FR and ADR paths, in-scope and
  out-of-scope paths, acceptance criteria, contracts to honour, validation commands, the
  bar from the FR, and on rework the review findings verbatim.

Workers:
- Do the assigned block only. Stop and report instead of expanding scope, adding
  dependencies, deviating from the FR or ADRs, or guessing.
- Never edit FRs, ADRs or other `docs/`, or touch git beyond read-only commands.
- Run the brief's validation commands before reporting; report results verbatim.

Reviews:
- Each block: reviewer. Each completed FR task group: supervisor.
- Findings: blocker / major / minor / nit, each with file:line and a proposed fix.
  Verdict: APPROVED or CHANGES REQUESTED.
- Blockers and majors are fixed (fresh worker, findings verbatim) and re-reviewed. Minors
  and nits go to the FR's Follow-ups list; read it at the start of every build session.
- Only the Architect ticks FR tasks, on reviewer evidence; never for partial or stubbed
  work. Acceptance per task: met / deferred to PO / not met.

Escalate to the PO: after three failed reviews of a block; contradictory requirements; new
technology or dependencies; scope growth; anything destructive.

After supervisor approval: summary to the PO (built, deviations, deferred, risks). Commit
only once the PO approves.
