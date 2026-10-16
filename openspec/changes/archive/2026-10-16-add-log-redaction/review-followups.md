# Review follow-ups: add-log-redaction

Minors and nits from block and section reviews. Read at the start of every apply session.

## Block A (task 1.1), reviewer 2026-10-15
- nit, `tests/SuperPower.Web.Tests/LogRedactionTests.cs:89-91`: unused `c`; `Same_email_any_case_gives_same_id` compares `a` with `m`, not `a` with `b` by id (covered by full-string compare). Drop `c` or rename.
- nit, `src/SuperPower.ServiceDefaults/LogRedaction.cs:74`: `!ReferenceEquals(masked, text) && masked != text` redundant; use `masked != text`.
- nit, `src/SuperPower.ServiceDefaults/LogRedaction.cs:54-58`: sequential replacement over-masks short collected values inside words; inherent to design.md D2, over-masks rather than leaks. No action.
- nit, `src/SuperPower.ServiceDefaults/LogRedaction.cs:47`: same original value in two blocked fields (one email) - last replacement wins in the message map; both masked. No action.

## Block B (tasks 1.2, 1.3), reviewer 2026-10-15
- minor, `src/SuperPower.ServiceDefaults/SerilogForwardingSink.cs:84-97`: an empty `StructureValue` yields no attribute; parent attribute and type tag dropped once flattened. Matches design.md D3. No action.
- nit, `src/SuperPower.ServiceDefaults/PersonalDataLogProcessor.cs:16`: always assigns `attributes.ToList()` even when `Redact` returned the input unchanged; skip when `ReferenceEquals`.
- nit, `tests/SuperPower.Web.Tests/LogRedactionPipelineTests.cs:33-34`: exporter keys nulled in in-memory config. Supervisor: the in-memory source is added last and overrides a shell `OTEL_EXPORTER_OTLP_ENDPOINT`; assertions hold even with export on. No action.

## Section review (sections 1-4), supervisor 2026-10-15
- major (fixed by rework, see tasks.md 1.1): quote-escaped personal values (`"` rendered as `\"` by Serilog) were not replaced in the message text.
- minor, `src/SuperPower.ServiceDefaults/PersonalDataLogProcessor.cs:13-16`: null `Attributes` with only a message is written back as an empty list; assign only when `!ReferenceEquals(attributes, record.Attributes)` (fold into the Block B ToList nit).
- nit, `src/SuperPower.ServiceDefaults/LogRedaction.cs:54`: `OrderByDescending` allocates when nothing was collected; guard with `collected.Count > 0`.
- risk (PO, rehearsal): site console view now holds only start-up lines; agent acts that read console output must use structured logs. Umbraco/library entries with a `Name`/`Message` property or any email (e.g. `owner@acme-agency.example`) are masked too; check rehearsed diagnoses in `add-form-emails` task 6.2.
