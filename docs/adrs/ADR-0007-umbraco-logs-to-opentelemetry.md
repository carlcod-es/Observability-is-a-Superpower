# ADR-0007: Route Umbraco's Serilog logs into the OpenTelemetry logging pipeline

## Status
Accepted, pending spike (2026-10-07). Revisit if the spike in `add-demo-foundation` task 1.3
fails.

## Context
Umbraco 17.7.1 owns logging: `CreateUmbracoBuilder()` clears logging providers and registers
`SerilogLoggerFactory` without a provider collection. ServiceDefaults' OpenTelemetry logging
provider is therefore silently ignored; traces and metrics are unaffected. Umbraco's Serilog
configuration reads `ReadFrom.Services`, so any `ILogEventSink` registered in DI joins its
pipeline. CLAUDE.md requires identical instrumentation code locally (OTLP) and in prod
(Azure Monitor).

## Decision
- ServiceDefaults registers a Serilog `ILogEventSink` in DI that forwards events to the
  `ILoggerProvider`s configured by ServiceDefaults (Serilog.Extensions.Logging provider
  collection), so Umbraco and app logs flow through the OpenTelemetry logging provider and
  whichever exporter ServiceDefaults selected.
- `AddServiceDefaults()` is called after `CreateUmbracoBuilder()`.
- The forwarding sink is not wrapped in Serilog's `Async` sink, to keep `Activity.Current`
  and trace correlation.
- Serilog `MinimumLevel` and overrides are the only log-level filter; documented in the README.

Alternatives considered:
- Serilog.Sinks.OpenTelemetry 4.2.0: proven on Umbraco 17, reads Aspire's OTLP variables, but
  exports OTLP only, so prod would need a different route; kept as fallback if the spike fails.
- Replace Umbraco's logger factory: fights the CMS's own configuration; rejected.

## Consequences
- One logging path; no duplicate entries.
- Unverified end to end until the spike runs; it is the first build task because Act 1 depends
  on logs in the dashboard.
