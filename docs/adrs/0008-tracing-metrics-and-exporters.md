# ADR-0008: Baseline tracing, metrics and exporters

## Status
Accepted (2026-10-07)

## Context
Umbraco 17.7.1 emits no spans or metrics of its own; spans come from ASP.NET Core, HttpClient
and SqlClient. Umbraco opens its own SQL connections from `umbracoDbDSN`. Page traces
must show database calls. CLAUDE.md requires OTLP locally
and Azure Monitor when `APPLICATIONINSIGHTS_CONNECTION_STRING` is set, with identical
instrumentation code.

## Decision
- ServiceDefaults (Aspire template) plus:
  - `OpenTelemetry.Instrumentation.SqlClient` 1.17.0 (`AddSqlClientInstrumentation`): each
    SqlClient version requires the OpenTelemetry core of its own version, and the Aspire 13.6.0
    ServiceDefaults template pins 1.17.0. Query parameter capture stays off (default).
  - `AddSource("Acme.Forms")`, `AddMeter("Acme.Forms")` (ADR-0027).
- Exporters: OTLP when `OTEL_EXPORTER_OTLP_ENDPOINT` is set; Azure Monitor
  (`Azure.Monitor.OpenTelemetry.AspNetCore` 1.6.0, compatible with the 1.17.0 set) when `APPLICATIONINSIGHTS_CONNECTION_STRING`
  is set.
- Exclude health and static asset requests (`/health`, `/alive`, `/umbraco/backoffice` assets,
  `/css`, `/js`) from request tracing to keep page traces readable.
- Drop database spans that have no recorded parent when background-job noise (distributed locks,
  cache instructions) hides page traces (measured: see Outcome).
- MailKit's built-in ActivitySource and meters are not enabled (ADR-0027).
- No `Aspire.Microsoft.Data.SqlClient` client integration: Umbraco manages connections.

## Alternatives
- Rely on SqlClient's own diagnostics without the instrumentation package: no OTel spans;
  rejected.
- OTLP everywhere including prod via a collector: extra component to run; rejected for a
  one-replica demo. Rejected for the prod path only: locally a collector fans the same OTLP
  out to the dashboard and Grafana LGTM (ADR-0017); the app's exporter switch is unchanged.

## Outcome (2026-10-09)
- Exporters are exclusive: Azure Monitor when `APPLICATIONINSIGHTS_CONNECTION_STRING` is set,
  else OTLP when `OTEL_EXPORTER_OTLP_ENDPOINT` is set. Instrumentation code is identical.
- Excluded request paths live in `TelemetryPaths` (`/health`, `/alive`, `/umbraco/backoffice`,
  `/css`, `/js`), shared by the ASP.NET Core trace filter and the log forwarding sink
  (ADR-0007), plus `/media`, `/favicon.ico` and `/assets` (site image assets, header logo).
  Backoffice page loads (`/umbraco`, `/umbraco/login`) and
  `/umbraco/management/api` calls are still traced.
- Background database spans measured at ~76 per minute with the site idle (distributed jobs,
  cache instructions, locks, webhooks), so they are filtered: a SqlClient span is kept only when
  its parent activity is recorded. Root SQL spans and SQL under filtered requests are dropped;
  SQL under page requests, form posts and recorded package spans is kept.
- Unverified: whether `UseAzureMonitor()` adds its own
  ASP.NET Core / SqlClient instrumentation or a second logger provider that bypasses these
  filters or duplicates telemetry.

## Consequences
- SQLite produces no database spans; attendees on SQLite see request spans only (README).
- `db.query.text` contains parameterised SQL only, never form values.
