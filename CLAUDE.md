# Observability is a Superpower

Demo repo for the conference talk "Observability is a Superpower - Giving Developers
and AI Agents X-Ray Vision" (Fri 16 Oct 2026, 13:30-14:15 BST, 45 min, live demo).
Slides are maintained separately; this repo holds the demo and is published for
attendees afterwards.

## Talk promises (the demo must deliver each)
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
- Placeholder name `Superpower.Forms`; must not be named "Umbraco Forms" (HQ product).
  May move to its own repo and be released later - keep it self-contained.
- Form definitions: Block List of field element types (Text, Email, Textarea,
  Checkbox, Select). Per field: alias, label, required, optional regex + error message.
  Email field has a "send confirmation" toggle. No custom backoffice UI.
- Form settings: owner email + subject, confirmation subject + body.
- Submissions: custom table via NPoco + PackageMigrationPlan (the same plan creates
  the element types). Stores form key, page key, UTC timestamp, fields JSON, basic
  client info (IP storage policy undecided).
- Emails: owner notification + submitter confirmation via Umbraco IEmailSender.
- Rendering: ViewComponent; POST via SurfaceController with antiforgery.
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
