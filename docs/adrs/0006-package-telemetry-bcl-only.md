# ADR-0006: Package telemetry uses BCL APIs only

## Status
Accepted (2026-10-07)

## Context
CLAUDE.md requires the package to emit its own ActivitySource and Meter. The original
rationale included a future release; release is no longer planned. The question is whether
the package references OpenTelemetry packages.

## Decision
- The package uses only `System.Diagnostics.ActivitySource`, `System.Diagnostics.Metrics.Meter`
  and `Microsoft.Extensions.Logging.ILogger`, all named `SuperPower.Forms`.
- No OpenTelemetry package references in `SuperPower.Forms`. The host opts in through
  ServiceDefaults with `AddSource("SuperPower.Forms")` and `AddMeter("SuperPower.Forms")`.
- This change creates the names and registration only; spans and instruments are added in
  change `add-form-emails`.

## Alternatives
- Reference OpenTelemetry API packages in the package: no capability gained over the BCL
  types; adds dependencies for consumers; rejected.

## Consequences
- Zero cost when nobody listens; the package is exporter-agnostic (OTLP locally, Azure Monitor
  in prod).
- Talking point: library authors instrument with BCL types, apps choose the pipeline.
