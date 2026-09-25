# Planning (temporary)

Working list of unknowns, open decisions and todos. Delete this file, and its
reference in CLAUDE.md, before the talk.

Rule: no demo answers here (planted problems, their causes or fixes). CLAUDE.md
references this file, so the on-stage agent may read it.

## Next question (discovery paused here, 2026-09-25)
- [ ] Demo site look: (a) blank Umbraco + minimal template, (b) community starter
  kit (e.g. Clean; v17 support unknown, adds trace noise), (c) small custom site
  (home + contact page, simple CSS). Recommended: (c) - readable traces, full control.

## Proposed, awaiting confirmation
- [ ] Repo layout:
  `src/Superpower.AppHost`, `src/Superpower.ServiceDefaults`, `src/Superpower.Web`
  (thin Umbraco site), `src/Superpower.Forms` (package, single RCL project),
  `tests/Superpower.Forms.Tests` (xUnit), `infra/` (only if `aspire deploy` is not enough),
  `Superpower.slnx`.
- [ ] Package telemetry depends only on BCL (`ActivitySource`, `Meter`, `ILogger`), no
  OpenTelemetry package references; host opts in via `AddSource`/`AddMeter("Superpower.Forms")`.
  Rationale: zero cost for consumers, releasable later, Act 1 talking point.
- [ ] Server-side validation only (no client JS): every submission reaches the server,
  validation failures per form/field become a metric - "useful telemetry" example.

## Open decisions
- [ ] IP / client info storage policy: store, truncate, hash or omit.
- [ ] Keep or drop the Select field type (only type needing extra config).
- [ ] Final package name (placeholder `Superpower.Forms`).
- [ ] Repo layout: AppHost, ServiceDefaults, demo site, package, tests.
- [ ] Act 4 split between agent-driven work and presenter/slide content (parked; question held in Claude memory).
- [ ] Demo content in prod: create manually after first deploy, or uSync.
- [ ] Media on Azure Container Apps: media-free site, Azure Files mount or Blob provider.

## Unknowns to spike
- [ ] Current Aspire version at talk date; dashboard MCP server tool set (traces, logs, metrics, resource commands?).
- [ ] Aspire CLI command for agent/MCP config (`aspire mcp init` vs `aspire agent init`).
- [ ] Umbraco 17 logging (Serilog) -> OTLP: bridge vs ILogger provider; how logs appear in the dashboard.
- [ ] SqlClient instrumentation spans for Umbraco's NPoco queries.
- [ ] smtp4dev container in AppHost: ports, endpoints, UI.
- [ ] Toxiproxy container in AppHost: proxy setup at startup, dashboard resource commands (`WithCommand`).
- [ ] Azure MCP Server: read-only mode, Monitor/App Insights query tools, running under a limited identity (Monitoring Reader + Log Analytics Reader).
- [ ] Azure Communication Services Email via SMTP from Umbraco IEmailSender.
- [ ] Umbraco 17 on Azure Container Apps: deploy via azd / `aspire deploy`, cold start, Examine/temp on local disk.
- [ ] Azure Monitor exporter switch in ServiceDefaults when APPLICATIONINSIGHTS_CONNECTION_STRING is set.

## Todos
- [ ] Capture confirmed decisions as an OpenSpec change (proposal, design, specs, tasks).
- [ ] Build the forms package and demo site.
- [ ] Create `demo/act-N-*` fallback branches.
- [ ] Commit Claude Code config for attendees: `.mcp.json`, `.claude/settings.json` (read-only MCP tools allowlisted).
- [ ] Demo-specific CLAUDE.md for stage: agent instructions to diagnose and verify via telemetry.
- [ ] Script and rehearse agent prompts; rehearse with `/fast`.
- [ ] Record fallback videos of agent segments.
- [ ] Deploy to Azure the day before; smoke test the morning of; min replicas 1.
- [ ] Stage layout: VSCodium + Claude CLI terminal, browser tabs (dashboard, smtp4dev, site).
- [ ] Venue network plan: everything local except the LLM.
- [ ] Attendee README: how to run, MCP setup, SQLite note (no DB spans on SQLite).
- [ ] Before the talk: remove demo-plan entries from Claude memory, or run the demo
  from a different clone path (memory is keyed to the folder path and loads on stage).

## Decision rationale (confirmed decisions in CLAUDE.md)
- Block List field definitions: Umbraco 17 backoffice is Lit/TS; custom UI would be
  most of the build and adds nothing to the talk.
- NPoco + PackageMigrationPlan over EF Core: provider-agnostic from one migration;
  EF Core needs per-provider migration sets. SqlClient instrumentation gives DB spans on
  SQL Server (including Umbraco's own queries); SQLite gets none.
- smtp4dev over MailPit: presenter familiarity; MailPit's fault injection is covered by Toxiproxy.
- Toxiproxy: localhost SMTP is too fast to be realistic; latency toxic multiplies per
  SMTP round trip (~7 per email).
- Claude Code CLI on stage: presenter's normal workflow; Copilot is not available in VSCodium.
- Real Azure deploy: prod runs the finished build; Act 4 prod segment is about PII not
  reaching the observability platform.
- PII: source-level redaction is primary (knows semantics, GDPR minimisation); pipeline
  (OTel Collector) and ingestion (DCR) are defence in depth for known-shape data.
