# Observability is a Superpower

Demo repo for the talk "Observability is a Superpower - Giving Developers and AI Agents X-Ray
Vision": a local Umbraco site run by .NET Aspire, with logs, traces and metrics in the Aspire
dashboard and Grafana LGTM, and read-only agent access to that telemetry over MCP. Kept for
experimenting with Aspire and OpenTelemetry. Run instructions: `README.md`; architecture:
`docs/project-setup.md`; decisions: `docs/adrs/` (cited as ADR-NNNN).

## Principle
- Keep it simple: the simplest option that shows the observability point. No edge-case
  handling or configurability the demo does not need.

## Site
- .NET 10, Umbraco 17, .NET Aspire AppHost (`src/SuperPower.AppHost`), shared telemetry setup in
  `src/SuperPower.ServiceDefaults`, site in `src/SuperPower.Web` (Rubber Duck Agency; code names
  use `Acme`).
- Pages: Home, About us, Contact us, Events, Services. Three forms fixed in code (Contact, Service
  enquiry, Event booking) in `src/SuperPower.Web/Forms/`. Each stored submission gets a reference
  `ACME-nnnn` and sends an owner email (smtp4dev).
- Booking service `api` (`src/SuperPower.Api`): event pages read the booking count over HTTP; the
  site queues each stored event booking on `queue` (RabbitMQ) and `api` adds it to the count in
  `cache` (Redis). The site works without them (2 s cap, fail-soft).
- Aspire resources: `site`, `api`, `cache`, `queue`, `sql`, `smtp4dev`, `collector`, `lgtm`,
  `grafana-mcp`.
- Must work on SQLite (standalone run) and SQL Server (AppHost): no raw SQL, NPoco
  `Sql<ISqlContext>` builders.
- Planted problems are marked `// Demo Code : Act N` with the fix commented beside them.

## Telemetry
- Site: ActivitySource and Meter `Acme.Forms`; spans `acme.forms.submit`, `acme.forms.notify`,
  `acme.forms.publish`; counter `acme.forms.submissions` by form and outcome; log
  `Form {Form} for {Page} stored as {Reference}`.
- `api`: server and consumer spans tagged `acme.event.key`; Meter `Acme.Bookings`, counter
  `acme.bookings.counted`; log `Booking recorded for {EventKey}, count {Count}`.
- Counters are cumulative and reset when a service restarts: query with `increase` or `rate`.
- Personal form data (name, email address, message, notes, company, website) never reaches
  logs, traces or metrics. Form, page, outcome and reference are fine. Log what happened, never
  the submitted model or field values.
- `PersonalDataLogProcessor` (ServiceDefaults) masks log entries before export (ADR-0028); it is
  a safety net, not permission.

## How to work
- Diagnose from telemetry before reading or changing code. Name the log entries, traces or
  metrics that show the problem, and prove a fix from new telemetry.
- Telemetry access: Grafana MCP (`grafana`, read-only Loki, Tempo, Prometheus). The Aspire MCP is
  opt-in (README).
- A code change needs Rebuild on the resource in the Aspire dashboard (Restart does not
  recompile). Ask the user to do it rather than starting or stopping resources yourself.
- No database access; answer questions about submissions from telemetry.
- No commits, pushes or new dependencies without the user's approval.
