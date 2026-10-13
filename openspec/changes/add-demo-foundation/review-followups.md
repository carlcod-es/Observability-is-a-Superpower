# Review follow-ups: add-demo-foundation

Minor and nit findings from block and section reviews. Read at the start of every apply session.

Tasks were renumbered 2026-10-13 (ADR-0016/ADR-0017 re-plan). Block tags and task numbers in
each entry are those current at the time of review; entries kept below are re-pointed to new
task numbers where it matters.

## Open

- [ ] minor (block 1A, task 1.2) `src/SuperPower.ServiceDefaults/Extensions.cs:118`: `/health` and `/alive` are mapped only in Development (template behaviour); production returns 404. Decide in `add-production-environment` whether to map them in all environments (Container Apps probes).
- [ ] nit (block 1A, task 1.2) Build warning NU1507: two package sources (nuget.org, Umbraco Prereleases) under central package management. Fix: root `nuget.config` with package source mapping (shared file; PO approval).
- [ ] nit (block 1A, task 1.2) Build warnings NU1902/NU1903: SixLabors.ImageSharp 3.1.12 via Umbraco.Cms 17.7.1. No action unless Umbraco ships a patch before the freeze (ADR-0001).
- [ ] minor (block 1B, task 1.3) `src/SuperPower.ServiceDefaults/SerilogForwardingSink.cs:60`: forwarded log record timestamp is the forwarding time, not `logEvent.Timestamp`; harmless while synchronous. Fix: comment, or carry the Serilog timestamp if the OTel logger API allows.
- [ ] minor (block 1B, task 1.3) `src/SuperPower.ServiceDefaults/SerilogForwardingSink.cs:85-89`: non-scalar values (EventId struct, sequences, dictionaries) are forwarded as Serilog-formatted strings. Acceptable at prototype bar; convert sequences to arrays later if useful.
- [ ] nit (block 1B re-review, task 1.3) `src/SuperPower.ServiceDefaults/SerilogForwardingSink.cs:92-108`: `PropertyToken.Alignment` ignored; fine at prototype bar.
- [ ] nit (block 1B re-review, task 1.3) `src/SuperPower.ServiceDefaults/SerilogForwardingSink.cs:38`: missing space in `ScalarValue { Value: string s}`.
- [x] (done in task 2.4) minor (section 1 supervisor) `src/SuperPower.ServiceDefaults/SerilogForwardingSink.cs:27-29`: forwards to every non-Serilog `ILoggerProvider`, broader than ADR-0007 ("the OpenTelemetry provider"). Planned in the task 2.4 brief: filter to `OpenTelemetryLoggerProvider`.
- [x] (done in task 2.4) minor (section 1 supervisor) `src/SuperPower.Web/appsettings.Development.json:6-8` with `Extensions.cs:71-73`: filtered requests (health, static, backoffice assets) still write Request starting/finished logs with trace ids of unexported traces. Planned in the task 2.4 brief: drop those log events in the sink by `RequestPath`, matching the trace filter.
- [x] (done in task 2.4) risk (section 1 supervisor) Task 2.4: OTLP and Azure Monitor exporters must be mutually exclusive when both variables are set. Planned in the task 2.4 brief.
- [ ] risk (section 1 supervisor) `Logging:LogLevel` has no effect on what reaches the dashboard (Serilog levels only, ADR-0007). README (task 9.1) must say so.
- [ ] Architect (section 1 supervisor, re-pointed) After the site forms backend (new section 3) lands: re-run root build and tests and re-check start-up and GET / logs in the dashboard (no duplicates). Covered by the section 3 supervisor and task 9.4.
- [ ] nit (block 2A, task 2.1) Restart logs "Last synced Id found 0 but was not found in the database": stale `src/SuperPower.Web/umbraco/Data/TEMP` shared between SQLite and SQL Server runs; harmless. README (task 9.1) reset steps: delete `umbraco/Data/TEMP` with the volume.
- [ ] nit (block 2A, task 2.1) `aspire describe` prints the SQL sa password in the site environment; do not show that output on stage.
- [ ] nit (block 2B, task 2.2) `src/SuperPower.AppHost/AppHost.cs`: smtp4dev SMTP (25) is also published on a dynamic host port by the Aspire proxy, bypassing Toxiproxy; unneeded and misleading in the dashboard. Optional: drop or hide the endpoint if the container alias still resolves.
- [ ] note (block 2B, task 2.3) Slow and Break stack; Heal is the only reset (spec: Heal removes all faults). `SlowLatencyMs` = 1000 gives ~6 s per email; tune in rehearsal (design.md open question).
- [ ] minor (block 3F, re-pointed to new task 3.2 — storage/JSON code is reused) `SubmissionJson.cs:14`: default encoder escapes non-ASCII and quotes in stored JSON; valid and identical on both databases but less readable. Fix: `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` (stored, never embedded in HTML) plus a test pinning the output.
- [ ] nit (block 3F, reused code) `SubmissionJson.cs:56-61`: O(n) `FindIndex` per property; harmless at form scale.
- [ ] nit (block 3F, reused code) `FormSubmissionDto.cs:10,17`: both NPoco `[PrimaryKey]` and Umbraco `[PrimaryKeyColumn]`; intentional (inserts vs DDL).
- [ ] nit (block 5A, reused pattern) submission repository: `cancellationToken` unused; harmless.
- [ ] PO (block 3F, re-pointed) Old round-trip test rows (2 in local SQLite, 2 in SQL Server `umbraco`) are removed by the task 6.1 reset.
- [ ] minor (block 2C, task 2.4) `src/SuperPower.ServiceDefaults/Extensions.cs:95`, `SerilogForwardingSink.cs:34`: with `UseAzureMonitor` a second `OpenTelemetryLoggerProvider` may be registered (sink would forward twice), and Azure Monitor's own instrumentation may bypass the request filter. Distro 1.6.0 also registers its own ASP.NET Core and SqlClient instrumentation: possible duplicate SQL spans, SQL spans skipping the parentless filter, requests skipping the path filter. Verify in `add-production-environment`; dedupe in the sink if needed.
- [ ] nit (block 2C) `Extensions.cs:78`: comment quotes a measured rate (~75/min) that will go stale.
- [ ] PO (block 2C) `/umbraco` and `/umbraco/login` page loads are traced (backoffice assets are not). Decide whether backoffice page loads should be traced.
- [ ] minor (section 2 supervisor) `src/SuperPower.AppHost/AppHost.cs:27`: `.WithExternalHttpEndpoints()` on smtp4dev is not needed locally; if the AppHost model is deployed it would publish a mail inbox (and Toxiproxy, SMTP env vars). `add-production-environment`: remove it and add mail-path resources and SMTP env only when `builder.ExecutionContext.IsRunMode`; record in that change's design.
- [ ] nit (section 2 supervisor) `src/SuperPower.AppHost/AppHost.cs:42`: `HttpClient` created per ready event and not disposed; `using var`.
- [ ] nit (section 2 supervisor) `src/SuperPower.ServiceDefaults/TelemetryPaths.cs:12`: `/favicon.ico` is traced. Task 7.1 brief: stylesheet under `wwwroot/css/`; optionally add `/favicon.ico`.
- [ ] risk (section 2 supervisor) Development config logs "Request starting" with the full query string (traces redact `url.query`). Keep personal data out of query strings, including redirect targets.
- [ ] risk (section 2 supervisor) SQL Server image is amd64-only (emulated on Apple Silicon; not on Linux arm64). Task 9.3 starts only the standalone site on Linux; Windows AppHost run still needed in 9.3.
- [ ] Architect (section 2 supervisor, re-pointed) Task 9.4 smoke re-check: Home trace still has database spans after task 3.5 (new controller); Slow/Break/Heal; no duplicate start-up logs.
- [ ] Architect (block 4A, re-pointed) Task 7.1 brief: the site stylesheet styles the form partials' classes, including a visually-hidden class if the markup uses one.
- [ ] nit (block 4A, re-pointed to new task 4.1) Consider `autocomplete` hints on the form partials later.

## Superseded by the 2026-10-13 re-plan (ADR-0016, ADR-0017)

The forms package (`src/SuperPower.Forms`, `tests/SuperPower.Forms.Tests`), the booking table
and the Aspire MCP were removed from the plan; these findings' code or subject is deleted in
task 3.1 or replaced by the new task texts. Kept for the record.

- minor (block 3D, task 3.5) `FormValidator.cs:32-45`: fixed value on a Checkbox fails when nothing is posted — code deleted.
- minor (block 3D, task 3.5) `FormValidator.cs:46-47`: fixed values not checked against field rules — code deleted.
- minor (block 3D, task 3.10) `FormValidatorTests.cs`: missing Checkbox/Select pattern tests — tests deleted.
- nit (block 3D) `FormValidator.cs:70`: static `Regex.IsMatch` cache — code deleted.
- nit (block 3D review) `CreateSchemaStep.cs:28`: CS9107 — code deleted.
- nit (block 3C, tasks 3.2-3.3) `CreateSchemaStep.cs:46`: no explicit scope — code deleted.
- nit (block 3C) `CreateSchemaStep.cs:65-69`, `:93-97`: retry skips half-built objects — code deleted.
- nit (block 3C) `CreateSchemaStep.cs:136`: `result.Result` vs `result.Status` — code deleted.
- minor (block 3E, task 3.7) `SuperPowerFormViewModelBuilder.cs:16-17`: success message dropped when a post closes the form — no capacity closing exists any more.
- minor (block 3E, task 3.7) `FormDefinitionReader.cs:20`: blank `fieldAlias` / unknown element types — code deleted.
- nit (block 3E) `SuperPowerFormViewComponent.cs:50-57`: one form per page documentation — code deleted; fixed partials are one per page by construction.
- nit (block 3E) `SuperPowerFormViewComponent.cs:59`: TempData flag before key comparison — code deleted.
- Architect (block 3E review) Task 3.8 brief closed-form re-render — folded into new task 3.5 (past-event rejection).
- Architect (block 4A, task 4.1) Rendered-HTML check before ticking 4.1 — folded into new task 4.1's verify text.
- minor (block 4A, task 4.1) `Default.cshtml:25`: `id="sp-{alias}"` collisions — view deleted.
- [x] (fixed in rework, verified on disk) minor (block 3G, task 3.8) `SuperPowerFormsComposer.cs`: `TimeProvider.System` registration — code deleted.
- minor (block 3G, task 3.8) Four controller behaviours had no automated tests — behaviours re-specified in new task 3.5's verify; smoke in task 9.4.
- nit (block 3G) `PostedValues.cs`: posted values read for fixed aliases — code deleted.
- Architect (block 3G) Runtime checks before ticking 3.8 — folded into new task 3.5's verify text.
- minor (block 5A, task 5.1) `EventBookingMigrationRunner.cs:17`: silent skip when runtime level is not Run — folded into new task 3.2 ("log line when the plan is skipped").
- nit (block 5A) `EventBookingRepository.cs:21`: `DateTimeKind.Unspecified` — booking repository deleted.
- Architect (block 5A) Pending checks before ticking 5.1 (booking table) — booking table dropped.
- nit (block 3H, folder step) `MoveTypesToFolderStep.cs`: missing type logs a warning — code deleted.
- Architect (section 2 supervisor) Task 8.2: inspect Aspire MCP `list_resources` for the `umbracoDbDSN` connection string — the Aspire MCP is dropped (ADR-0017); the agent has no resource tool.
