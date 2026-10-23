# Rubber Duck Agency site

Stage copy of the demo site. Becomes `CLAUDE.md` on the `demo` branch; not read on `main`.

## Site
- Umbraco 17 on .NET 10, run by a .NET Aspire AppHost (`src/SuperPower.AppHost`), shared telemetry setup in
  `src/SuperPower.ServiceDefaults`, site code in `src/SuperPower.Web` (code names use `Acme`).
- Pages: Home, About us, Contact us, Events, Services. Three forms (Contact, Service enquiry, Event booking),
  code in `src/SuperPower.Web/Forms/`. Each stored submission gets a reference `ACME-nnnn` and sends an owner
  email (smtp4dev locally).
- Booking service `api` (`src/SuperPower.Api`): event pages read the booking count from it over HTTP; the site
  queues each stored event booking on `queue` (RabbitMQ) and `api` adds it to the count in `cache` (Redis).
- Aspire resources: `site`, `api`, `cache`, `queue`, `sql`, `smtp4dev`, `collector`, `lgtm`, `grafana-mcp`.

## How to work
- Diagnose from telemetry before reading or changing code. Name the log entries, traces or metrics that
  show the problem.
- After a fix: ask the presenter to rebuild the changed resource (`site` or `api`) and wait for confirmation,
  then prove the fix from new telemetry, not from the code alone.
- Never start, stop, restart or rebuild anything yourself (no `aspire`, `dotnet run` or `docker` commands); the
  presenter runs the environment.
- Make the smallest change that fixes the cause. No refactoring, new dependencies or commits.
- Personal form data (name, email address, message, notes, company, website) must never reach logs,
  traces or metrics. Recording the form, page, outcome and reference is fine.
- No database access. Answer questions about submissions from telemetry: counts and references only.

## Telemetry access
- Grafana MCP (`grafana`) only, read-only: Loki logs, Tempo traces, Prometheus metrics, for every service.
  Use no other telemetry source.
- Site telemetry: ActivitySource and Meter `Acme.Forms`; spans `acme.forms.submit`, `acme.forms.notify`,
  `acme.forms.publish`; counter `acme.forms.submissions` by form and outcome.
- Booking service telemetry: server and message consumer spans tagged `acme.event.key`; Meter `Acme.Bookings`,
  counter `acme.bookings.counted`.
- Counters are cumulative and reset when a service restarts: use `increase` or `rate`.
