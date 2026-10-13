# Observability is a Superpower

Demo repo for the conference talk "Observability is a Superpower - Giving Developers
and AI Agents X-Ray Vision" (Fri 16 Oct 2026, 13:30-14:15 BST, 45 min, live demo).
Slides are maintained separately; this repo holds the demo and is published for
attendees afterwards.

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
- Local Umbraco site + custom package; .NET Aspire + OpenTelemetry bring logs,
  traces and metrics into one local observability environment.
- Build a new feature, diagnose real problems, verify fixes using telemetry.
- Coding agents use observability data via MCP to identify failures, suggest
  changes and validate the result - not just generate code.
- What telemetry is genuinely useful.
- Avoid collecting personal form data in telemetry.
- Controlled, read-only AI access to production observability.

## Stack
- .NET 10, Umbraco 17 LTS, .NET Aspire (AppHost + ServiceDefaults), OpenTelemetry.
- Agent: Claude Code CLI, Aspire MCP (local), Azure MCP Server (prod, read-only).
- Prod: Azure Container Apps (1 replica, min replicas 1), Azure SQL,
  Application Insights, Azure Communication Services Email (SMTP).

## Forms package (demo package)
- Name `SuperPower.Forms` (final); must not be named "Umbraco Forms" (HQ product).
  Not planned for release.
- Form definitions: Block List of field element types (Text, Email, Textarea,
  Checkbox, Select). Per field: alias, label, required, optional regex + error message.
  No custom backoffice UI. Form setting: success message.
- Submissions: custom table via NPoco + PackageMigrationPlan (the same plan creates
  the element types). Stores form key, page key, UTC timestamp, fields JSON. No IP or
  browser info.
- Rendering: ViewComponent; POST via SurfaceController with antiforgery.
- Site hooks: `IFormSubmissionHandler` (close a form, fix field values, act on a stored
  submission in the same scope). ADR-0013.
- Later (change `add-form-emails`): Email field "send confirmation" toggle; form settings
  owner email + subject, confirmation subject + body; owner notification + submitter
  confirmation via Umbraco IEmailSender.

## Demo site
- Acme Agency (fictional marketing agency): pages, events (`eventInPerson` with capacity,
  `eventOnline`), services; Contact, Service enquiry and Event booking forms. Spec
  `agency-site`, ADR-0014.
- Event bookings: site-owned table `acmeEventBooking` via a site migration plan. ADR-0015.
- Must work on SQLite and SQL Server: no raw SQL, NPoco Sql<ISqlContext> builders,
  NVARCHARMAX for JSON columns. Demo uses SQL Server container via Aspire.

## Local environment (AppHost)
- SQL Server: AddAzureSqlServer(...).RunAsContainer() - container locally, Azure SQL deployed.
  Set umbracoDbDSN and umbracoDbDSN_ProviderName.
- SMTP: Umbraco -> Toxiproxy -> smtp4dev (container). Toxiproxy injects latency /
  failures; exposed as Aspire dashboard resource commands.
- ServiceDefaults: OTLP to Aspire locally; Azure Monitor exporter when
  APPLICATIONINSIGHTS_CONNECTION_STRING is set. Instrumentation code identical in both.

## Telemetry
- Package emits its own ActivitySource and Meter.

## Scope
- Protect PII from the observability platform only. The database is out of scope;
  agents never get database access.

## Repo conventions
- Planning via OpenSpec in `openspec/`.
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
  `src/SuperPower.Forms/` (except `Views/`), `src/SuperPower.Web/` C# code (Program.cs,
  migrations, handlers, repositories, controllers, view models), `tests/SuperPower.*.Tests/`.
- worker-frontend: `src/SuperPower.Web/Views/`, `src/SuperPower.Web/wwwroot/`,
  `src/SuperPower.Forms/Views/`, any client-side script and its tests.
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
