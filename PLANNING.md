# Planning (temporary)

Working list of unknowns, open decisions and todos. Delete this file, and its
reference in CLAUDE.md, before the talk.

Rule: no demo answers here (planted problems, their causes or fixes). CLAUDE.md
references this file, so the on-stage agent may read it.

## Next step
- [ ] `/opsx:propose`: product requirements below become the proposal and specs; Architect
  decisions (proposals, open technical decisions, spikes) become design and ADRs.

## Product requirements confirmed in explore (2026-10-06)
- Site: fictional pottery studio (working name Slipcast Studio). Pages: Home, Classes, Contact.
- Forms (two):
  - Contact: Name, Email, Message, "Reply by phone?" checkbox. Owner notification only.
  - Book a class: Name, Email (sends confirmation), Class (select: Wheel throwing, Hand
    building, Glazing), Experience notes (optional), "Join mailing list" checkbox.
    Owner notification + submitter confirmation. A booking is a plain submission: no
    places, availability or payment.
- Select field type stays.
- Stored submission: field values, form, page, UTC timestamp. No IP or browser info.
- Agent never accesses the database; demo personal data is in telemetry only.
- Name, Message and Experience notes are never recorded in telemetry.
- Prod data: scripted submissions over the days before the talk (repeat submitters,
  validation failures, both forms, all classes), plus attendees submitting live via QR code.
- Prod attendee handling: notice on both forms (live demo, deleted within 7 days, made-up
  email allowed); delete prod submissions within 7 days of 16 Oct; per-visitor submission
  limit; presenter switch to close forms; real confirmation emails sent.
- Prod owner mailbox: presenter-owned demo alias (prod settings only, never in the repo).
- Package name `Superpower.Forms` is final; not planned for release.
- Act 4 split: resolved (presenter-only notes).

## Architect proposals (deferred to /opsx:propose; not for explore)
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
- [ ] Repo layout: AppHost, ServiceDefaults, demo site, package, tests.
- [ ] Site content (pages, forms) in prod: create manually after first deploy, or uSync.
- [ ] Media on Azure Container Apps: media-free site, Azure Files mount or Blob provider.
- [ ] Prod site after the talk (PO decides later): take it down once attendee submissions
  are deleted (7 days after 16 Oct; closes the public form, stops cost), or keep it running
  as a live reference (abuse surface stays open).

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
- [ ] First /opsx:propose: re-run the existing technology decisions (CLAUDE.md, Decision rationale below)
  through the Architect phase as ADRs. Talk technology is a prerequisite; check it for gaps.
- [ ] Capture confirmed decisions as an OpenSpec change (proposal, design, specs, tasks).
- [ ] Build the forms package and demo site.
- [ ] Plan and create `demo/act-N-*` fallback branches. Keep `docs/research/` and any OpenSpec
  artifacts that describe later-act fixes off earlier demo branches.
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
