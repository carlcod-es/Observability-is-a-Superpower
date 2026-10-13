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
- `/opsx:explore` - Analyst: gather and document product requirements only: users, goals,
  scenarios, testable acceptance criteria, non-functional requirements, constraints,
  out of scope, open questions. No technology discussion or suggestions (frameworks,
  platforms, languages, databases). This overrides the explore skill's
  design/architecture prompts. Technology named in the talk promises, or mentioned by
  the PO, is recorded as a constraint, not discussed.
- `/opsx:propose` - Architect: recommend technology for the requirements with the
  alternatives and trade-offs; defer when the PO overrides and record the PO's rationale.
  Record each decision as an ADR in `docs/adrs/` (`NNNN-<slug>.md`, cited as ADR-NNNN;
  sections: Status, Context, Decision, Alternatives, Consequences) and reference it from
  the specs. `tasks.md` sections follow component boundaries (one owner each, see
  "Apply-phase team") and state their dependencies on other sections.
- `/opsx:apply` - see "Apply-phase team".
- Without PO approval: no commits, pushes, branches, new dependencies, or technology
  decisions outside `/opsx:propose`.

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
- .NET 10, Umbraco 17 LTS, .NET Aspire (AppHost + ServiceDefaults), OpenTelemetry.
- Agent: Claude Code CLI. Local: Grafana MCP (read-only) over Grafana LGTM, fed by an
  OpenTelemetry Collector alongside the Aspire dashboard; logs, traces and metrics. No Aspire
  MCP: the agent gets no resource state or commands (PO 2026-10-13).
- Prod hosting (PO 2026-10-13; final choice open, made by the PO before it is needed):
  - Baseline, built first, and the fallback: the local environment exposed to attendees
    through a Cloudflare tunnel.
  - Stretch, only if time allows: Azure Container Apps (1 replica, min replicas 1), Azure SQL,
    Application Insights, Azure Communication Services Email (SMTP), Azure MCP Server
    (read-only).
  - Do not build Azure-only work ahead of the baseline.

## Demo site
- Acme Agency (fictional marketing agency): Home, About us, Contact us, Events list, event
  pages, Services list, service pages. Spec `agency-site`.
- Events: one document type; free; title, description, start date and time; no format,
  capacity or places. Past events show "This event has ended" and no form.
- Forms: three, fixed in the site code (no package, no editor-defined forms, fields or form
  text). The page type decides the form; the event or service comes from the page. Spec
  `form-submission`.
  - Contact: Name, Email, Message, "Reply by phone?".
  - Service enquiry: Name, Email, Company (optional), Website (optional), Message.
  - Event booking: Name, Email, Notes (optional), "Join mailing list". An ordinary
    submission: no booking status, places or limits.
  - Server-side validation only; no format rules beyond Email. Success text "Thank you,
    we'll be in touch."
  - Rendering: ViewComponent; POST via SurfaceController with antiforgery.
- Submissions: one site-owned table. Stores form, page key, UTC timestamp, field values.
  No IP or browser info.
- Must work on SQLite and SQL Server: no raw SQL, NPoco Sql<ISqlContext> builders,
  NVARCHARMAX for JSON columns. Demo uses SQL Server container via Aspire.
- Later (change `add-form-emails`): owner notification on all forms, submitter confirmation on
  Service enquiry and Event booking, via Umbraco IEmailSender. Owner address is an environment
  setting; subjects and bodies are fixed text.

## Local environment (AppHost)
- SQL Server: AddAzureSqlServer(...).RunAsContainer() - container locally, Azure SQL deployed.
  Set umbracoDbDSN and umbracoDbDSN_ProviderName.
- SMTP: Umbraco -> Toxiproxy -> smtp4dev (container). Toxiproxy injects latency /
  failures; exposed as Aspire dashboard resource commands, run by the presenter only.
- ServiceDefaults: OTLP locally; Azure Monitor exporter when
  APPLICATIONINSIGHTS_CONNECTION_STRING is set. Instrumentation code identical in both.

## Telemetry
- The site's forms code emits its own ActivitySource and Meter (spans and instruments in
  change `add-form-emails`).
- Never recorded: Name, Message, Notes, Company, Website, email address. Fine: form, event or
  service, outcome.

## Scope
- Protect PII from the observability platform only. The database is out of scope;
  agents never get database access.

## Repo conventions
- Planning via OpenSpec in `openspec/`. Change order: `add-demo-foundation`,
  `add-form-emails`, `add-production-environment`.
- ADRs, `design.md` and `tasks.md` of `add-demo-foundation` predate the 2026-10-10 and
  2026-10-13 decisions until `/opsx:update add-demo-foundation` runs (see `PLANNING.md`).
  Where they disagree with this file, this file holds the PO decision.
- `PLANNING.md` (temporary): open decisions, unknowns and todos. Check it before
  starting work; update it when items are resolved or discovered.
- Demo recovery points are git branches `demo/act-N-*`; `main` is the final state.

## Apply-phase team
The main session is the Architect: it plans blocks, briefs agents, ticks tasks and reports
to the PO. Only the Architect assigns work; sub-agents cannot delegate. Agent files in
`.claude/agents/` describe roles only; paths, ports and rules live here or in `design.md`.

Agents: `worker-backend`, `worker-frontend` (Sonnet; implement), `reviewer` (Sonnet; block
review), `supervisor` (Opus; section review). Reviewer and supervisor never modify code;
Bash only for build, tests, linters and read-only git.

Ownership:
- worker-backend: `src/SuperPower.AppHost/`, `src/SuperPower.ServiceDefaults/`,
  `src/SuperPower.Web/` C# code (Program.cs, migrations, repositories, controllers, view
  models), `tests/SuperPower.*.Tests/`.
- worker-frontend: `src/SuperPower.Web/Views/`, `src/SuperPower.Web/wwwroot/`, any
  client-side script and its tests.
- PO: `src/SuperPower.Web/uSync/` (backoffice export; tasks marked "(PO)").
- Shared, changed only when the Architect assigns them in a brief: `SuperPower.slnx`,
  `Directory.*.props`, `.gitignore`, `appsettings*.json`, `launchSettings.json`, `.mcp.json`,
  `.claude/settings.json`, `README.md`, `tests/smoke/`.
- Architect only: `docs/`, `openspec/`, `CLAUDE.md`, `PLANNING.md`.

Blocks:
- A block is contiguous tasks from one `tasks.md` section, one worker, small enough for
  one review. Split work that crosses ownership. Contracts (interfaces, schemas) before
  their consumers. Run blocks in parallel only when their files are disjoint. Fresh agent
  per block.
- Brief is self-contained: task IDs and text, spec and ADR paths, in-scope and
  out-of-scope paths, acceptance criteria, contracts to honour, validation commands, the
  bar from `design.md`, and on rework the review findings verbatim.

Workers:
- Do the assigned block only. Stop and report instead of expanding scope, adding
  dependencies, deviating from specs or ADRs, or guessing.
- Never edit `tasks.md`, specs, ADRs, or touch git beyond read-only commands.
- Run the brief's validation commands before reporting; report results verbatim.

Reviews:
- Each block: reviewer. Each completed `tasks.md` section: supervisor.
- Findings: blocker / major / minor / nit, each with file:line and a proposed fix.
  Verdict: APPROVED or CHANGES REQUESTED.
- Blockers and majors are fixed (fresh worker, findings verbatim) and re-reviewed. Minors
  and nits go to `openspec/changes/<change>/review-followups.md`; read it at the start of
  every apply session.
- Only the Architect ticks `tasks.md`, on reviewer evidence; never for partial or stubbed
  work. Acceptance per task: met / deferred to PO / not met.

Escalate to the PO: after three failed reviews of a block; contradictory requirements; new
technology or dependencies; scope growth; anything destructive.

After supervisor approval: summary to the PO (built, deviations, deferred, risks). Commit
only once the PO approves.
