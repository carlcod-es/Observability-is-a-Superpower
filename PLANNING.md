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
- [ ] `/opsx:apply add-demo-foundation`: planning complete 2026-10-08 (ADR-0001..0015).
- [ ] Architect: design and tasks for `add-event-bookings`; may run alongside the build
  (ADR-0015 keeps its columns out of `add-demo-foundation`).

## Product requirements confirmed in explore (2026-10-06, revised 2026-10-08)
- Site: fictional marketing agency, Acme Agency (replaces the pottery studio). Pages: Home,
  About us, Contact us, Events list, event pages, Services list, service pages.
- Services (e.g. Umbraco upgrade, SEO audit, AdWords audit): no capacity; Service enquiry form.
- Events (e.g. "How to get the most out of your CMS"): free; in person (capacity set by the
  editor) or online (no capacity). Past events show "This event has ended", no form. No payment.
- Forms (three; the event or service comes from the page, not a field):
  - Contact: Name, Email, Message, "Reply by phone?" checkbox. Owner notification only.
  - Service enquiry: Name, Email, Company (optional), Website (optional), Message. Owner
    notification + submitter confirmation.
  - Event booking: Name, Email, Places 1-4 (in person only; online is always 1), Notes
    (optional), "Join mailing list" checkbox. No per-guest names.
- Booking workflow: submitting stores a pending booking. A personal link (emailed) confirms it;
  places are allocated when the link is opened, all or nothing, else waiting list. Online events
  always confirm. Links never expire; opening again shows current status. Waiting list is final:
  no promotion, no cancellation. One booking per email per event. Places left counted from
  confirmed bookings, shown on in-person event pages.
- Emails: booking received (click to confirm), confirmed (online: joining link), waitlisted;
  enquiry owner + confirmation; contact owner. Owner notified of bookings on confirm/waitlist.
- Stored submission: field values, form, page, UTC timestamp. No IP or browser info.
- Agent never accesses the database; demo personal data is in telemetry only.
- Never recorded in telemetry: Name, Message, Notes, Company, Website, email address, the
  confirmation link. Fine: form, event or service, places, outcome.
- Owner dashboard of bookings: stretch goal.
- Prod data: scripted submissions over the days before the talk (repeat submitters, validation
  failures, all forms, bookings across events incl. some never confirmed, one in-person event
  nearly full at talk time), plus attendees submitting live via QR code.
- Prod attendee handling: notice on all forms (live demo, deleted within 7 days); bookings need a
  real email, contact and enquiry may use a made-up one; delete prod submissions and bookings
  within 7 days of 16 Oct; one booking per email per event; one presenter switch closes every
  form; real emails sent.
- Prod owner mailbox: presenter-owned demo alias (prod settings only, never in the repo).
- Forms simplified (2026-10-10): no separate forms package; the talk no longer promises a
  custom package. Three fixed forms in the site; the page type decides the form. No editor-
  defined forms, fields or form text. No format rules beyond Email and Places 1-4.
- Email settings (`add-form-emails`): owner address is an environment setting; subjects and
  bodies are fixed text. No backoffice settings.
- Act 4 split: resolved (presenter-only notes).

## Change order (2026-10-08)
1. `add-demo-foundation`: agency site, events and services, three forms, booking storage
   (bookings stay pending).
2. `add-event-bookings`: confirmation link, allocation, waiting list, places left.
3. `add-form-emails`: all emails, including the one carrying the link.
4. `add-production-environment`.

## Architect follow-ups from the 2026-10-08 pivot
- [ ] `add-event-bookings`: design.md, ADR(s) and tasks (allocation under concurrent link opens;
  new booking columns via new migration steps, ADR-0015).

## Architect follow-ups from the 2026-10-10 forms simplification
- [ ] `/opsx:update add-demo-foundation`: design.md; revise ADR-0002, ADR-0004, ADR-0005,
  ADR-0006, ADR-0013 (no package, no field element types or form document type);
  re-plan `tasks.md` sections 3, 4 and 6 (ticked 3.2, 3.5, 3.7 and code for 3.8, 4.1 rework);
  `review-followups.md` entries that no longer apply.
- [ ] CLAUDE.md "Talk promises", "Forms package", "Telemetry", "Apply-phase team" ownership;
  `openspec/config.yaml` context (names the demo package).
- [ ] PO: reset local SQLite and SQL databases once the package code is removed (field element
  types are still in both).

## Open decisions
- [ ] Prod site after the talk (PO decides later): take it down once attendee submissions
  are deleted (7 days after 16 Oct; closes the public form, stops cost), or keep it running
  as a live reference (abuse surface stays open).

## Unknowns to spike
Decided in ADRs; verified by `add-demo-foundation` tasks:
- [x] Umbraco 17 Serilog -> OTLP via forwarding sink (ADR-0007): task 1.3, passed 2026-10-08.
- [ ] SqlClient spans for Umbraco's NPoco queries; background span volume (ADR-0008): task 2.4.
- [ ] smtp4dev and Toxiproxy in the AppHost, Slow/Break/Heal commands (ADR-0010): tasks 2.2, 2.3.
- [ ] Booking table migration timing on a fresh unattended install (ADR-0015): task 5.1.
- [ ] Date Time with time zone stored value shape (ADR-0014): task 5.2.
- [ ] Aspire MCP tool set and `aspire agent init --mcp` (ADR-0011): task 8.1.
For `add-production-environment`:
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
  `demo/act-N-*` branches and in the published repo.
- [ ] Script and rehearse agent prompts; rehearse with `/fast`.
- [ ] Record fallback videos of agent segments.
- [ ] Deploy to Azure the day before; smoke test the morning of; min replicas 1.
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
- Real Azure deploy: prod runs the finished build; Act 4 prod segment is about PII not
  reaching the observability platform.
- PII: source-level redaction is primary (knows semantics, GDPR minimisation); pipeline
  (OTel Collector) and ingestion (DCR) are defence in depth for known-shape data.
