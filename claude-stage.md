# Rubber Duck Agency site

Stage copy of the demo site. Becomes `CLAUDE.md` on the `demo` branch; not read on `main`.

## Site
- Umbraco 17 on .NET 10, run by a .NET Aspire AppHost (`src/SuperPower.AppHost`), shared telemetry setup in
  `src/SuperPower.ServiceDefaults`, site code in `src/SuperPower.Web` (code names use `Acme`).
- Pages: Home, About us, Contact us, Events, Services. Three forms (Contact, Service enquiry, Event booking),
  code in `src/SuperPower.Web/Forms/`. Each stored submission gets a reference `ACME-nnnn` and sends an owner
  email (smtp4dev locally).
- Aspire resources: `site`, `sql`, `smtp4dev`, `collector`, `lgtm`, `grafana-mcp`.

## How to work
- Diagnose from telemetry before reading or changing code. Name the log entries, traces or metrics that
  show the problem.
- After a fix: ask to restart `site`, then prove the fix from new telemetry, not from the code alone.
- Never start, stop or rebuild the AppHost (`aspire run`, `aspire start`, `aspire stop`, `dotnet run` on the
  AppHost); the presenter runs it. Restart only `site`, via the Aspire resource command.
- Make the smallest change that fixes the cause. No refactoring, new dependencies or commits.
- Personal form data (name, email address, message, notes, company, website) must never reach logs,
  traces or metrics. Recording the form, page, outcome and reference is fine.
- No database access. Answer questions about submissions from telemetry: counts and references only.

## Telemetry access
- Local: Aspire MCP (`aspire`): resources, structured logs, traces, console logs. Resource commands need
  approval on every call.
- Production: Grafana MCP (`grafana`), read-only: Loki logs, Tempo traces, Prometheus metrics. Available
  only after go-live; holds telemetry from go-live onwards.
- Site telemetry: ActivitySource and Meter `Acme.Forms`; spans `acme.forms.submit`, `acme.forms.notify`;
  counter `acme.forms.submissions` by form and outcome. Counters are cumulative: use `increase` or `rate`.
