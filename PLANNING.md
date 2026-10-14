# Planning (temporary)

Working list of unknowns, open decisions and todos. Delete this file, and its
reference in CLAUDE.md, before the talk.

Rule: no demo answers here (planted problems, their causes or fixes). CLAUDE.md
references this file, so the on-stage agent may read it.

## Next step
- [ ] `add-site-prototype` (2026-10-14): standalone static prototype (Eleventy, ADR-0018
  Accepted) as the basis for the Umbraco views. PO decisions 2026-10-14: Blog and newsletter
  signup are prototype-only (not demo scope); detail pages carry their forms as static markup;
  forms are markup-only, submitting to a static thank-you page. Apply done through section 5,
  all reviews closed (sections 1-3 supervisor-approved after fixes; section 5 theme
  supervisor-approved 2026-10-14). PO sign-off task 4.1 (Acme-branded version) given 2026-10-14;
  PO then commissioned the Rubber Duck Consulting theme (prototype-only, PO 2026-10-14; demo
  scope stays Acme Agency): Baloo 2/Nunito self-hosted (ADR-0019), duck palette, 10 bath-toy SVG
  illustrations, rebranded copy and slugs. PO task 6.1 sign-off given 2026-10-14; merged to
  main on PO instruction 2026-10-14. Remaining: archive the change. Known
  gap carried to `add-demo-foundation`: form error / past-event notice styles; Umbraco
  integration re-brands back to Acme and must use POST forms (see review-followups.md).
- [x] Architect (2026-10-13): ADR-0017 (Collector, LGTM, Grafana MCP, allow-list) written, ADR-0011
  superseded; ADR-0016 written, ADR-0004/0005/0006/0013/0015 removed; ADR-0002/0012/0014 revised;
  `/opsx:update add-demo-foundation` done — design.md, specs, tasks.md re-planned (sections 3-9),
  `po-backoffice-steps.md` and `review-followups.md` reworked.
- [ ] `/opsx:apply add-demo-foundation`: resume with task 2.5 and section 3 (first: 3.1 package
  removal, then PO task 6.1 database reset).
- [x] Spike closure (PO 2026-10-13): keep both spikes open for reference until pre-talk cleanup.
  Then: `spike-aspire-probe` task 3.4 and `spike-metrics-mcp` task 3.6 (delete `spikes/`, drop
  `Microsoft.Data.SqlClient` from `Directory.Packages.props`, keep the toolkit package — used by
  `add-demo-foundation` task 2.5), archive both. Tracked in Todos.

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
- [x] `/opsx:update add-demo-foundation` done 2026-10-13 (see "Next step").
- [ ] PO: reset local SQLite and SQL databases once the package code is removed — now
  `add-demo-foundation` task 6.1 (after task 3.1).

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
- [x] SqlClient spans for Umbraco's NPoco queries; background span volume (ADR-0008): task 2.4,
  passed 2026-10-09 (`verification.md`).
- [x] smtp4dev and Toxiproxy in the AppHost, Slow/Break/Heal commands (ADR-0010): tasks 2.2, 2.3,
  passed 2026-10-09 (`verification.md`).
- [ ] Date Time with time zone stored value shape (ADR-0014): task 3.4.
- [ ] LGTM stack, Collector and Grafana MCP on Windows (ADR-0017; spike ran on macOS): task 9.3.
For `add-production-environment`, tunnel baseline:
- [ ] Cloudflare tunnel to the local site: public URL for the QR code, only the site exposed
  (no dashboard, Grafana, smtp4dev or backoffice).
For `add-production-environment`, Azure stretch (only if time allows):
- [ ] Azure MCP Server: read-only mode, Monitor/App Insights query tools, running under a limited identity (Monitoring Reader + Log Analytics Reader).
- [ ] Azure Communication Services Email via SMTP from Umbraco IEmailSender.
- [ ] Umbraco 17 on Azure Container Apps: deploy via azd / `aspire deploy`, cold start, Examine/temp on local disk.
- [ ] Azure Monitor exporter switch (ADR-0008; code in task 2.4) verified against Application Insights.

## Todos
- [ ] Static site prototype (change `add-site-prototype`, commit 3c6e726 on `main`,
  `prototype/`, Eleventy, ADR-0018/0019): branded Rubber Duck Consulting, prototype-only -
  the Umbraco site stays Acme Agency (spec `site-prototype` Purpose, PO 2026-10-14). Task 7.1
  uses its layouts, form markup and `site.css` as the template basis and copies the built
  self-hosted fonts (ADR-0019); its Blog section and newsletter box are prototype-only.
  Imagery changed 2026-10-14 (explore): media-library images via `mainBanner`, optional with a
  single-colour fallback (design.md Decision 12, spec `agency-site` "Imagery from the media
  library", tasks 2.6/6.4). The 7.1 worker materialises `prototype/` from
  `main` via `git archive` into the scratchpad (no merge needed). Merging `main` into
  `sample-site` is the PO's call; expect trivial conflicts in PLANNING.md and .gitignore.
- [ ] Pre-talk cleanup of both spikes (PO 2026-10-11, reconfirmed 2026-10-13: keep open for
  reference until then): `spike-aspire-probe` task 3.4 and `spike-metrics-mcp` task 3.6 —
  delete `spikes/`, remove `Microsoft.Data.SqlClient` from `Directory.Packages.props` (keep the
  toolkit package, used by `add-demo-foundation` task 2.5), root build; archive both changes.
  Keep `spikes/` off `demo/act-N-*` branches and out of the published repo.
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
