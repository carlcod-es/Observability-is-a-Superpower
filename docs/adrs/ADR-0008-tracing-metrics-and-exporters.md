# ADR-0008: Baseline tracing, metrics and exporters

## Status
Accepted (2026-10-07)

## Context
Umbraco 17.7.1 emits no spans or metrics of its own; spans come from ASP.NET Core, HttpClient
and SqlClient. Umbraco opens its own SQL connections from `umbracoDbDSN`. Spec
`local-environment` requires page traces with database calls. CLAUDE.md requires OTLP locally
and Azure Monitor when `APPLICATIONINSIGHTS_CONNECTION_STRING` is set, with identical
instrumentation code.

## Decision
- ServiceDefaults (Aspire template) plus:
  - `OpenTelemetry.Instrumentation.SqlClient` 1.19.0 (`AddSqlClientInstrumentation`). Query
    parameter capture stays off (default).
  - `AddSource("SuperPower.Forms")`, `AddMeter("SuperPower.Forms")` (ADR-0006).
- Exporters: OTLP when `OTEL_EXPORTER_OTLP_ENDPOINT` is set; Azure Monitor
  (`Azure.Monitor.OpenTelemetry.AspNetCore`) when `APPLICATIONINSIGHTS_CONNECTION_STRING`
  is set. Verified in prod by change `add-production-environment`.
- Exclude health and static asset requests (`/health`, `/alive`, `/umbraco/backoffice` assets,
  `/css`, `/js`) from request tracing to keep page traces readable.
- Measure background-job database span noise (distributed locks, cache instructions) in task
  1.4; if it hides page traces, drop database spans that have no parent activity.
- MailKit's built-in ActivitySource and meters are not enabled here; decided in change
  `add-form-emails`.
- No `Aspire.Microsoft.Data.SqlClient` client integration: Umbraco manages connections.

Alternatives considered:
- Rely on SqlClient's own diagnostics without the instrumentation package: no OTel spans;
  rejected.
- OTLP everywhere including prod via a collector: extra component to run; rejected for a
  one-replica demo.

## Consequences
- SQLite produces no database spans; attendees on SQLite see request spans only (README).
- `db.query.text` contains parameterised SQL only, never form values.
