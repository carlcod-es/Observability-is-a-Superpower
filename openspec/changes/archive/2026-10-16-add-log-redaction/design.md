## Context

See proposal.md - Why. Current state (main, 2026-10-15):
- Umbraco owns logging; `SerilogForwardingSink` (ServiceDefaults) forwards every Serilog event
  to `OpenTelemetryLoggerProvider` (ADR-0007). Destructured values become one string
  attribute (`LogValues.Convert` -> `ToString()`); the sink renders the message itself.
- `ConfigureOpenTelemetry` calls `builder.Logging.AddOpenTelemetry(...)` before
  `AddOpenTelemetryExporters`; OpenTelemetry .NET 1.17.
- `appsettings.Development.json` adds the Serilog Console sink; the Aspire dashboard shows it
  as the site's console logs.
- Site log calls: `AcmeFormsController` (`Form {Form} stored as {Reference}`) and
  `AcmeSiteMigrationRunner` (plan name and state). None log field values.
- Tests: `tests/SuperPower.Web.Tests` (xUnit v3) references the site, so ServiceDefaults
  transitively.

Fixed constraints: no personal data in telemetry; the database is out of scope; no new
dependencies without PO approval.

## Goals / Non-Goals

**Goals:**
- Layer 1 rule documented; layer 2 processor on `main`, on by default, one line to disable.

**Non-Goals:**
- Traces, metrics, exception messages, log scopes, Umbraco log files.
- Detecting names or free text by content.
- Demo branch content (the PO comments lines out).

## Decisions

### D1. Redaction as an OpenTelemetry log processor (ADR-0028)
`PersonalDataLogProcessor : BaseProcessor<LogRecord>` in ServiceDefaults, added inside
`builder.Logging.AddOpenTelemetry(logging => ...)` via `logging.AddProcessor(...)`, which runs
before the exporter processors added later by `AddOpenTelemetryExporters`. `OnEnd` reads
`record.Attributes` and `record.FormattedMessage`, calls D2's function and writes both back.
The registration is one line with the comment
`// ADR-0028: log redaction - comment out to show raw values in a demo`.
Alternatives (MEL compliance redaction, Serilog packages, sink-side masking, collector) and
why they lost: ADR-0028.

### D2. Pure masking function
`LogRedaction.Redact(IReadOnlyList<KeyValuePair<string, object?>> attributes, string? message,
byte[] key)` returns new attributes and message. Order:
1. Attribute whose last `.`-segment is in {Name, Email, Message, Notes, Company, Website}
   (ordinal ignore case): value -> stand-in if the string is an email, else `[REDACTED]`.
   Original non-empty string values are collected.
2. Message: each collected value replaced by its replacement (longest first).
3. Email pattern `[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}` replaced in all remaining
   string attribute values and the message by `[REDACTED:<id>]`.
4. `{OriginalFormat}` left as is (template, no values).
`id` = first 12 lower-case hex characters of `HMACSHA256(key, UTF8(address.ToLowerInvariant()))`.
Attributes with no match are returned unchanged (same instance when nothing changed).
Alternative: regex over the whole rendered object string (`Name: "..."`) - fragile; lost.

### D3. Destructured objects flattened in the sink (ADR-0028)
`SerilogForwardingSink.LogValues.Build` turns a `StructureValue` into one attribute per
property, `Parent.Child` (recursive). Scalars unchanged; sequences and dictionaries keep
`ToString()`. Message rendering unchanged. Needed so D2 can match nested fields by name.

### D4. Key (ADR-0028)
Config `Telemetry:RedactionKey` (UTF-8 bytes). AppHost:
`builder.AddParameter("telemetry-redaction-key", new GenerateParameterDefault { MinLength = 32,
Special = false }, secret: true, persist: true)` and on the site
`.WithEnvironment("Telemetry__RedactionKey", key)`. Persisted in AppHost user secrets like
`sql-password`. Missing key: `RandomNumberGenerator.GetBytes(32)` at start-up.
Alternatives: committed key, manual secret, unkeyed hash - ADR-0028.

### D5. Console sink removed (ADR-0028, PO 2026-10-15)
Delete the `WriteTo` Console entry from `appsettings.Development.json`. Structured logs in
the dashboard are unchanged; console logs keep only non-Serilog output. Talking point appended to
`docs/presenter/runthrough.md` (Architect, 2026-10-15; PO places it).

### D6. Layer 1 rule
CLAUDE.md "Telemetry" and `docs/project-setup.md` "Telemetry": "Log what happened (form,
outcome, reference), never the submitted model or field values; the log processor is a
safety net, not permission." Existing site log calls already comply; no code change.

### Bar and test strategy
Bar: demo (prototype quality, reliable on stage tomorrow). Tests:
- xUnit for `LogRedaction.Redact`: flat and dotted names, case-insensitivity, email-valued
  field gets stand-in, message replacement, same/different email ids, email in free text,
  untouched entry, different key gives different id.
- One pipeline test: Serilog logger -> `SerilogForwardingSink` -> `OpenTelemetryLoggerProvider`
  with the processor and a capturing processor after it; log `{@Model}` with canary values;
  assert dotted attributes masked and canaries absent from attributes and formatted message.
  `InternalsVisibleTo("SuperPower.Web.Tests")` on ServiceDefaults for the sink.
- Runtime: AppHost with a temporary (uncommitted) `{@Model}` log line, canary submission;
  check Aspire structured logs, console logs and Grafana Loki. Smoke check unchanged.

## Risks / Trade-offs

- [`GenerateParameterDefault` API differs in Aspire 13.6] -> worker stops and reports; fallback
  is `AddParameter(name, secret: true)` with the value set in AppHost user secrets (PO decides).
- [Processor order: exporter added first would export unmasked] -> D1 registration precedes
  exporters; the pipeline test asserts masking through the provider.
- [Names in free text or in sequences slip through] -> layer 1 rule; documented limit.
- [Raw values in Umbraco log files and exception messages] -> local only; out of scope
  (ADR-0028 Consequences). Stage rule stays: never lower Serilog to Debug while submitting.
- [Masking disabled on a demo branch and forgotten] -> PO's demo branches; `main` keeps it on.

## Migration Plan

No data migration. The key parameter is generated on the first AppHost start after the change.
Rollback: comment out the D1 line.
