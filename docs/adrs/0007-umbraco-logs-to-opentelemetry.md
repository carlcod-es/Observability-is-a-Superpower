# ADR-0007: Route Umbraco's Serilog logs into the OpenTelemetry logging pipeline

## Status
Accepted (2026-10-07). Spike passed 2026-10-08 (`add-demo-foundation` task 1.3): forwarding
sink verified with OTLP; fallback not needed. Azure Monitor path to be verified in
`add-production-environment`.

## Context
Umbraco 17.7.1 owns logging: `CreateUmbracoBuilder()` clears logging providers and registers
`SerilogLoggerFactory` without a provider collection. ServiceDefaults' OpenTelemetry logging
provider is therefore silently ignored; traces and metrics are unaffected. Umbraco's Serilog
configuration reads `ReadFrom.Services`, so any `ILogEventSink` registered in DI joins its
pipeline. CLAUDE.md requires identical instrumentation code locally (OTLP) and in prod
(Azure Monitor).

## Decision
- ServiceDefaults registers a Serilog `ILogEventSink` in DI that forwards events to the
  OpenTelemetry `ILoggerProvider` configured by ServiceDefaults (resolved from DI), so Umbraco and app logs flow through the OpenTelemetry logging provider and
  whichever exporter ServiceDefaults selected.
- `AddServiceDefaults()` is called after `CreateUmbracoBuilder()`.
- The forwarding sink is not wrapped in Serilog's `Async` sink, to keep `Activity.Current`
  and trace correlation.
- Serilog `MinimumLevel` and overrides are the only log-level filter; documented in the README.

## Alternatives
- Serilog.Sinks.OpenTelemetry 4.2.0: proven on Umbraco 17, reads Aspire's OTLP variables, but
  exports OTLP only, so prod would need a different route; kept as fallback if the spike fails.
- Replace Umbraco's logger factory: fights the CMS's own configuration; rejected.

## Consequences
- One logging path; no duplicate entries.
- Implemented as `SerilogForwardingSink` in ServiceDefaults: forwards only to
  `OpenTelemetryLoggerProvider`; drops events whose `RequestPath` is excluded by
  `TelemetryPaths` (the same paths the trace filter excludes, ADR-0008); renders the message template itself so string values are not
  quoted; only `Serilog` 4.4.0 referenced (the version Umbraco resolves; direct reference
  approved by the PO 2026-10-09). The pin moves with Umbraco's Serilog version.
- A page request logs nothing at Information by default (`Microsoft` overridden to Warning).
  The site's Development config sets `Microsoft.AspNetCore.Hosting.Diagnostics` to
  Information, so each page trace has linked Request starting/finished entries.
