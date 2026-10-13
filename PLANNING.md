# Planning (temporary)

Working list of unknowns, open decisions and todos. Delete this file, and its
reference in CLAUDE.md, before the talk.

Rule: no demo answers here (planted problems, their causes or fixes). CLAUDE.md
references this file, so the on-stage agent may read it.

## Next step
- [x] `spike-aspire-probe` (2026-10-11): signed off by the PO 2026-10-11. Findings in
  `openspec/changes/spike-aspire-probe/findings.md`: ADR-0011 confirmed (no metrics over MCP).
  Open for `add-demo-foundation` (ADR-0008): SqlClient metrics in ServiceDefaults; RecordException.
  `add-demo-foundation` can resume; ADR-0016 proposal still awaits PO approval.
- [x] `spike-metrics-mcp` (2026-10-11..13): PO signed off 2026-10-13 on recommendation "adopt B alongside
  the Aspire MCP": Grafana LGTM + Grafana MCP (`--disable-write --enabled-tools
  prometheus,loki,tempo,datasource`, 22 tools) next to the Aspire MCP (ADR-0011 kept). Findings in
  `openspec/changes/spike-metrics-mcp/findings.md`. Open: task 3.6 (delete `spikes/metrics-mcp/`, remove toolkit
  package, archive) waits for `spike-aspire-probe` task 3.4 (shared probe projects).
- [ ] Architect: new ADR for B (Collector, LGTM, Grafana MCP, allow-list) and fold it into
  `add-demo-foundation` (package `CommunityToolkit.Aspire.Hosting.OpenTelemetryCollector`, images,
  `.mcp.json`, `.claude/settings.json`, attendee pre-pull, Windows run). PO 2026-10-13: the Grafana MCP
  replaces the Aspire MCP (not alongside); revise ADR-0011. Via `/opsx:update`.
- [ ] Architect: `/opsx:update add-demo-foundation` for the 2026-10-13 simplification (Event booking is
  an ordinary submission): supersede ADR-0015; revise ADR-0014 (one `event` document type, no format or
  capacity) and ADR-0011; design.md; re-plan tasks (remove the booking table code from task 5.1, task 8.1
  Aspire MCP -> Grafana MCP, PO content tasks 6.1-6.3 and uSync for the merged event type).
- [ ] `/opsx:apply add-demo-foundation`: resumes after the updates above.

## Product requirements confirmed in explore (2026-10-06, revised 2026-10-08, 2026-10-13)
- Keep it simple (PO 2026-10-13, CLAUDE.md "Principle"): basic demo, no edge cases.
- Site: fictional marketing agency, Acme Agency (replaces the pottery studio). Pages: Home,
  About us, Contact us, Events list, event pages, Services list, service pages.
- Services (e.g. Umbraco upgrade, SEO audit, AdWords audit): Service enquiry form.
- Events (e.g. "How to get the most out of your CMS"): free; title, description, start date and
  time; no format, capacity or places. Past events show "This event has ended", no form. No payment.
- Forms (three; the event or service comes from the page, not a field). After a valid
  submission: "Thank you, we'll be in touch."
  - Contact: Name, Email, Message, "Reply by phone?" checkbox. Owner notification only.
  - Service enquiry: Name, Email, Company (optional), Website (optional), Message. Owner
    notification + submitter confirmation.
  - Event booking: Name, Email, Notes (optional), "Join mailing list" checkbox. Owner
    notification + submitter confirmation. An ordinary submission: no booking status,
    confirmation link, waiting list or limit per email (PO 2026-10-13).
- Stored submission: field values, form, page, UTC timestamp. No IP or browser info.
- Agent never accesses the database; demo personal data is in telemetry only.
- Agent reads logs, traces and metrics via the Grafana MCP only; no resource state or commands
  (presenter runs fault commands in the dashboard). PO 2026-10-13.
- Never recorded in telemetry: Name, Message, Notes, Company, Website, email address. Fine:
  form, event or service, outcome.
- Owner dashboard of bookings: stretch goal.
- Prod data: scripted submissions over the days before the talk (repeat submitters, validation
  failures, all forms, bookings across events), plus attendees submitting live via QR code.
- Prod attendee handling: notice on all forms (live demo, deleted within 7 days); any form may use
  a made-up email; delete prod submissions within 7 days of 16 Oct (or by taking prod down); one
  presenter switch closes every form; real emails sent.
- Prod owner mailbox: presenter-owned demo alias (prod settings only, never in the repo).
- Forms simplified (2026-10-10): no separate forms package; the talk no longer promises a
  custom package. Three fixed forms in the site; the page type decides the form. No editor-
  defined forms, fields or form text. No format rules beyond Email.
- Email settings (`add-form-emails`): owner address is an environment setting; subjects and
  bodies are fixed text. No backoffice settings.
- Act 4 split: resolved (presenter-only notes).

## Change order (2026-10-08, revised 2026-10-13)
1. `add-demo-foundation`: agency site, events and services, three forms.
2. `add-form-emails`: all emails.
3. `add-production-environment`: tunnel baseline first; Azure only if time allows (see Open
   decisions, prod hosting).
`add-event-bookings` dropped 2026-10-13 (change directory removed).

## Architect follow-ups from the 2026-10-10 forms simplification
- [ ] `/opsx:update add-demo-foundation`: design.md; revise ADR-0002, ADR-0004, ADR-0005,
  ADR-0006, ADR-0013 (no package, no field element types or form document type);
  re-plan `tasks.md` sections 3, 4 and 6 (ticked 3.2, 3.5, 3.7 and code for 3.8, 4.1 rework);
  `review-followups.md` entries that no longer apply.
- [ ] PO: reset local SQLite and SQL databases once the package code is removed (field element
  types are still in both).

## Open decisions
- [ ] Prod hosting (PO 2026-10-13): working assumption is the local environment behind a
  Cloudflare tunnel as baseline and fallback, built first; Azure built only if time allows. PO
  makes the final choice before it is needed. Implications of the tunnel baseline, to resolve
  in `add-production-environment`:
  - "Read-only AI access to production observability": prod telemetry is the same local stack
    the agent already reads; decide what is shown as production and how access is controlled.
  - Real emails to attendees: smtp4dev catches everything; needs a real mail route or the
    confirmations are not sent.
  - Scripted prod submissions over the days before the talk: needs the laptop running for days,
    or generate the history shortly before.
  - Faults injected on stage also reach attendees submitting via QR code.
  - Venue network: adds a public inbound dependency ("everything local except the LLM" no
    longer holds).
  - Architect: ADR-0008 (Azure Monitor exporter) and ADR-0009 (Azure SQL for deploy parity)
    lose their reason if Azure is never built.
- [x] Prod site after the talk (PO 2026-10-13): taken down after the talk. Azure: deleting the
  resources deletes attendee submissions. Tunnel: close the tunnel, reset the local database
  and telemetry.
- [x] Telemetry MCP (PO 2026-10-13): logs, traces and metrics queries go to the Grafana MCP
  (covers all three, `spike-metrics-mcp` findings). Architect: revise ADR-0011.
- [x] Aspire MCP (PO 2026-10-13): dropped. Presenter runs Toxiproxy commands in the dashboard;
  no console logs or resource state for the agent.
- [x] Event bookings (PO 2026-10-13): Event booking is an ordinary submission; `add-event-bookings`
  dropped.

## Unknowns to spike
Decided in ADRs; verified by `add-demo-foundation` tasks:
- [x] Umbraco 17 Serilog -> OTLP via forwarding sink (ADR-0007): task 1.3, passed 2026-10-08.
- [ ] SqlClient spans for Umbraco's NPoco queries; background span volume (ADR-0008): task 2.4.
- [ ] smtp4dev and Toxiproxy in the AppHost, Slow/Break/Heal commands (ADR-0010): tasks 2.2, 2.3.
- [ ] Date Time with time zone stored value shape (ADR-0014): task 5.2.
For `add-production-environment`, tunnel baseline:
- [ ] Cloudflare tunnel to the local site: public URL for the QR code, only the site exposed
  (no dashboard, Grafana, smtp4dev or backoffice).
For `add-production-environment`, Azure stretch (only if time allows):
- [ ] Azure MCP Server: read-only mode, Monitor/App Insights query tools, running under a limited identity (Monitoring Reader + Log Analytics Reader).
- [ ] Azure Communication Services Email via SMTP from Umbraco IEmailSender.
- [ ] Umbraco 17 on Azure Container Apps: deploy via azd / `aspire deploy`, cold start, Examine/temp on local disk.
- [ ] Azure Monitor exporter switch (ADR-0008; code in task 2.4) verified against Application Insights.

## Todos
- [ ] `spike-aspire-probe` task 3.4 (PO deferred 2026-10-11, keep the probe until closer to the
  talk): delete `spikes/aspire-probe/`, remove `Microsoft.Data.SqlClient` from
  `Directory.Packages.props`, root build; then archive the change. Keep `spikes/` off
  `demo/act-N-*` branches and out of the published repo.
- [ ] Plan and create `demo/act-N-*` fallback branches. Keep `docs/research/` and any OpenSpec
  artifacts that describe later-act fixes off earlier demo branches.
- [ ] Demo-specific CLAUDE.md for stage: agent instructions to diagnose and verify via telemetry.
  Decide whether the Roles and "Apply-phase team" sections and `.claude/agents/` stay on
  `demo/act-N-*` branches and in the published repo. Analyst recommendation (PO to confirm):
  leave them, and `.claude/commands/opsx/`, off demo branches and `main`; on stage they make
  the agent role-play and route fixes through sub-agents. PO decides separately whether
  attendees see the OpenSpec planning.
- [ ] Script and rehearse agent prompts; rehearse with `/fast`.
- [ ] Record fallback videos of agent segments.
- [ ] Tunnel baseline: start the environment and tunnel before the talk; smoke test the public
  URL the morning of. Azure (if built): deploy the day before; min replicas 1.
- [ ] Stage layout: VSCodium + Claude CLI terminal, browser tabs (dashboard, smtp4dev, site).
- [ ] Venue network plan: everything local except the LLM.
- [ ] Before the talk: remove demo-plan entries from Claude memory, or run the demo
  from a different clone path (memory is keyed to the folder path and loads on stage).

Resolved 2026-10-08 and removed: repo layout (ADR-0002), media (spec `agency-site`, no media
library), Architect proposals (ADR-0002, ADR-0005, ADR-0006), uSync seeding (ADR-0003), Aspire
version and MCP command (ADR-0001, ADR-0011), first `/opsx:propose` and OpenSpec change, attendee
agent config (task 8.1), attendee README (task 9.1), build (`add-demo-foundation`), apply-phase
team (CLAUDE.md "Apply-phase team", `.claude/agents/`), ADR file naming (`NNNN-<slug>.md`),
`add-demo-foundation` bar (prototype).

## Decision rationale (confirmed decisions in CLAUDE.md)
- Fixed forms in the site (2026-10-10, replaces Block List field definitions): editor-defined
  forms added build and backoffice work without adding to the observability story.
- NPoco + PackageMigrationPlan over EF Core: provider-agnostic from one migration;
  EF Core needs per-provider migration sets. SqlClient instrumentation gives DB spans on
  SQL Server (including Umbraco's own queries); SQLite gets none.
- smtp4dev over MailPit: presenter familiarity; MailPit's fault injection is covered by Toxiproxy.
- Toxiproxy: localhost SMTP is too fast to be realistic; latency toxic multiplies per
  SMTP round trip (~7 per email).
- Claude Code CLI on stage: presenter's normal workflow; Copilot is not available in VSCodium.
- Prod (either hosting): runs the finished build; Act 4 prod segment is about PII not
  reaching the observability platform. Tunnel first because it is the shorter route to a
  public site before 16 Oct (PO 2026-10-13).
- PII: source-level redaction is primary (knows semantics, GDPR minimisation); pipeline
  (OTel Collector) and ingestion (DCR) are defence in depth for known-shape data.
