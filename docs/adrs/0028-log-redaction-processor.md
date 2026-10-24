# ADR-0028: Log redaction in the OpenTelemetry logging pipeline

## Status
Accepted (2026-10-15).

## Context
Personal form values must not reach any observability platform through logs, even when a log
line includes them; email addresses become a keyed, linkable stand-in (keyed hashing: EDPB
Guidelines 01/2025 on Pseudonymisation; unkeyed hashes of emails are reversible: AEPD/EDPS hash
paper, 2019). Umbraco
owns logging: its `SerilogLoggerFactory` replaces the .NET logger factory, and
`SerilogForwardingSink` forwards every Serilog event to the OpenTelemetry logger provider
(ADR-0007). The Development config also writes Serilog to the console, which the Aspire
dashboard and Aspire MCP show as console logs. PO 2026-10-15: rule plus in-app safety net;
no collector filter; everything on `main`, parts commented out for demos.

## Decision
- A `BaseProcessor<LogRecord>` in ServiceDefaults, registered as the first processor on the
  OpenTelemetry logger provider (one marked line, so it can be commented out for a demo). In
  `OnEnd` it rewrites the record before any exporter sees it:
  - attributes whose last name segment is Name, Email, Message, Notes, Company or Website
    (case-insensitive) become `[REDACTED]` (email values: the stand-in below); their original
    string values are replaced in the formatted message too;
  - every email address in remaining string attributes and the formatted message becomes
    `[REDACTED:<id>]`, id = first 12 hex characters of HMAC-SHA256(key, lower-cased address).
- The masking logic is a pure static function; the processor only applies it to the record.
- `SerilogForwardingSink` flattens destructured objects (`{@Model}`) into dotted attributes
  (`Model.Email`) instead of one string, so nested fields are matched by name.
- Key from configuration `Telemetry:RedactionKey`. The AppHost supplies it from a generated,
  persisted secret parameter (stored in AppHost user secrets, like the SQL password). Without
  a key (site run alone) a random key per process is used.
- The Serilog Console sink is removed from `appsettings.Development.json`; logs leave the site
  only through the OpenTelemetry path.

## Alternatives
- `Microsoft.Extensions.Compliance.Redaction` (data classification + `EnableRedaction`): needs
  the .NET extended logger factory, which Umbraco replaces; covers only source-generated log
  methods; new packages. Rejected.
- Serilog masking packages (Destructurama.Attributed, Serilog.Enrichers.Sensitive): work in
  Umbraco's pipeline but are new dependencies and Serilog-specific, so not reusable by
  attendees outside Serilog. Rejected.
- Masking inside `SerilogForwardingSink`: no new concepts, but Serilog-specific and mixes
  forwarding with policy; the OpenTelemetry processor is the pattern attendees can reuse.
  Rejected.
- Collector `redaction` / `transform` processor on the `*/lgtm` pipelines: protects only the
  production stage; the local dashboard and Aspire MCP still see raw values; built-in hashes
  are unkeyed. Not chosen by the PO (possible later layer).
- Unkeyed SHA-256 of emails: reversible by hashing guessed addresses (AEPD/EDPS). Rejected.
- Fixed key in committed config: anyone with the repo can compute ids. Rejected.
- Keep the console sink and accept the gap, or mask console output with Serilog-side code:
  PO chose removal; the presenter covers the gap on stage.

## Consequences
- Every exporter (OTLP to the collector, Azure Monitor if set) receives cleaned logs; the
  Aspire dashboard, Grafana and both MCP servers see the same masked entries.
- Names and free text are caught only by property name, not by content. Values inside
  sequences or dictionaries are not matched by name (email pattern still applies).
- Exception messages and scopes are not rewritten; traces and metrics are unaffected.
- Umbraco's own log files (`umbraco/Logs/`) keep raw values; local disk, out of scope like the
  database.
- Destructured objects appear as dotted attributes instead of one string.
