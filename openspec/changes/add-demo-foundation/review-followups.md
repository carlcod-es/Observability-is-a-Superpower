# Review follow-ups: add-demo-foundation

Minor and nit findings from block and section reviews. Read at the start of every apply session.

Tasks were renumbered 2026-10-13 (ADR-0016/ADR-0017 re-plan). Block tags and task numbers in
each entry are those current at the time of review; entries kept below are re-pointed to new
task numbers where it matters.

## Open

- [ ] PO decision (task 6.2, 2026-10-14) Kept against the task text, by PO choice: the image
  block in `compPage`'s `mainContent` Block List and the "products" media folder (PO plans
  media pickers and content later; spec `agency-site` "No media library dependency" only
  requires pages not to depend on media), and the unused duplicate data type "Date Time
  Picker (with time zone)" (PO could not delete it in the backoffice). If media is added
  later: demo content must still render with no missing images on a fresh database, and the
  site stays script-free (spec `agency-site`).
- [ ] note (task 6.2) `src/SuperPower.Web/Models/ood.flag` (ModelsBuilder out-of-date marker)
  appeared untracked after the backoffice session; gitignored by the Architect 2026-10-14.
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
- [ ] nit (block 2D, task 2.5) `src/SuperPower.AppHost/AppHost.cs`: Grafana admin/admin credentials hard-coded; acceptable for the local demo (LGTM default), no action.
- [ ] nit (block 2D, task 2.5) `aspire start`/`aspire run` need `--apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj` while the spike AppHosts exist; README (task 9.1) must say so, or the note dies with the pre-talk spike cleanup (PLANNING.md Todos).
- [ ] minor (section 2 supervisor, task 2.5) `src/SuperPower.AppHost/AppHost.cs`: the site does not `WaitFor` the collector; with the persistent SQL container already healthy, start-up logs and first spans race the collector's OTLP listener and survive only via exporter retry. Start-up logs in the dashboard are an Act 1 point. Fix: `.WaitFor(collector)` on the site (the toolkit collector has a health check by default). Assign to the next worker block that touches `AppHost.cs`; fix before cutting `demo/act-1-start` (task 9.4 re-checks start-up logs).
- [ ] nit (section 2 supervisor, task 2.5) `src/SuperPower.AppHost/AppHost.cs`: lgtm has no health check, so `WaitFor(lgtm)` waits only for Running; first collector exports rely on the otlphttp retry queue, and grafana-mcp may answer before Grafana is up. Acceptable at prototype bar. Optional: `.WithHttpHealthCheck("/api/health", endpointName: "grafana")` on lgtm (same block as the minor above if picked up).
- [ ] note (section 2 supervisor, task 2.5) LGTM data is session-only (no volume): Grafana and agent history are lost on every AppHost restart while SQL persists. Fits the demo; rehearsals must generate traffic after a restart before the agent queries anything. README (task 9.1) should mention it.
- [x] (Architect, done 2026-10-13) minor (section 2 supervisor, task 2.5) spec `local-environment` drift: ADR-0017 missing from Purpose; "One-command start" did not name the collector, LGTM and Grafana MCP. Fixed in the spec.
- [x] (Architect, done 2026-10-13) nit (section 2 supervisor, task 2.5) collector image tag 0.162.0 pinned only in code. Recorded in ADR-0017 Decision and design.md Decision 9.
- [x] (Architect, done 2026-10-13) nit (section 2 supervisor, task 2.5) ADR-0008 Alternatives "via a collector: rejected" read as contradicting the local collector. Clarified (prod-path rejection; local collector per ADR-0017).
- [ ] Architect (block 3A, task 3.1) Root `dotnet test` exits 1 on the zero-test-project solution ("Get projects properties with MSBuild didn't execute properly with exit code: 1", MTP runner from global.json); build is clean, so nothing is broken. Task 3.6 must re-verify `dotnet test` exits 0; if it still fails, look at the empty `/tests/` folder element in `SuperPower.slnx` and the MTP runner config. Task 3.1's "dotnet test succeed" is satisfied in substance, literal pass deferred to 3.6.
- [ ] nit (block 3A, task 3.1) `SuperPower.slnx`: empty `<Folder Name="/tests/">` element; harmless, filled by task 3.6.
- [ ] nit (block 3A, task 3.1) `src/SuperPower.Web/Forms/AcmeFormsTelemetry.cs:8`: doc comment could say the literal in ServiceDefaults must match `Name`; optional.
- [ ] nit (block 3B, task 3.2) `src/SuperPower.Web/Forms/Storage/SubmissionJson.cs:19`: `OrderBy(p => p.MetadataToken)` relies on compiler declaration order (holds for these flat classes). Guard: task 3.6 tests assert each form's field order; also note the reliance in the line-13 doc comment.
- [ ] nit (block 3B, task 3.2) `SubmissionJson.cs:13`: doc comment documents "true"/"false" bools but not the null-to-empty-string rule; add it.
- [ ] nit (block 3B, task 3.2) `FormSubmissionRepository.cs:40` `GetFieldsJsonAsync` exists for the byte-identity check; keep if the 3.6 tests use it, else remove at pre-talk cleanup.
- [ ] note (block 3B, task 3.2) Task text "a log line when the plan is skipped" means the not-Run runtime level skip (origin: block 5A finding); a normal second start logs "Running ... finished" as a no-op. Accepted reading, recorded here.
- [ ] note (block 3B, task 3.2) Field values are stored as strings ("true"/"false" for bools, "" for null), matching the package format; 3.6 tests should pin this.
- [ ] PO (section 8, tasks 8.1/8.2) Files `.mcp.json` and `.claude/settings.json` are written (Architect, 2026-10-14); the live verification needs a FRESH Claude Code session at the repo root (so the grafana MCP server loads) with the AppHost running: ask for a Home page trace with spans, that trace's logs, and a request-duration metric — all must work without approval prompts. 8.1/8.2 stay unticked until then.
- [ ] note (section 8, task 8.2) `spikes/aspire-probe/.mcp.json` and `spikes/metrics-mcp/.mcp.json` also configure MCP servers (incl. the Aspire MCP). They load only for sessions opened inside those folders, never at the repo root, and die with the pre-talk spike cleanup (PLANNING.md Todos). The 8.2 check "no other MCP server in the repo" is met for the repo root; record this caveat when ticking.
- [ ] nit (block 3C, task 3.6) `tests/SuperPower.Web.Tests/FormValidationTests.cs`: the three per-form required-field tests are copy-pasted; optional `[Theory]` fold. No action at prototype bar.
- [ ] nit (block 3C, task 3.6) Optional coverage: whitespace-only `[Required]` value (default behaviour, low value); `Deserialize` on non-object/non-string input.
- [x] (Architect, done 2026-10-14) nit (block 3C, task 3.6) Generated `appsettings-schema*.json`/`umbraco-package-schema.json` in the test project were not gitignored; added scoped `tests/**` rules to `.gitignore` (the site's committed copies are unaffected).
- [x] (Architect decision 2026-10-14, folded into the task 3.5 brief) minor (block 3D, task 3.4) `EventSchedule.cs` null policy: missing/unreadable `startDate` is "not past" (fail open) — right for views (form beats silent hiding; property is mandatory so unreachable), wrong direction for booking rejection. Decision: the `AcmeFormsController` Event booking action treats an event page with no readable `startDate` as non-bookable (fail closed), keeping the overload as is.
- [ ] nit (block 3D, task 3.4) `AcmeFormsComposer.cs:16`: fully qualified `SuperPower.Web.Events.*` names; a `using` directive would be tidier.
- [ ] note (block 3D, tasks 3.5/4.1) Runtime verification of 3.5's controller behaviours (and 4.1's rendered HTML) needs the partials and templates (4.1, 7.1) and a running site; 3.5/4.1 stay unticked after review until that combined runtime check (before/with 7.1's verification; scripted again in 9.4).
- [ ] Architect (block 3E, task 3.5 → 4.1 brief) Partial-binding contract: the partial's model must be the form class itself (inputs named `Name`, `Email`, ... — no nested wrapper); after `CurrentUmbracoPage()` entered values must be repopulated from `ModelState` (`AttemptedValue`), not `Model`; forms must post via `Html.BeginUmbracoForm<AcmeFormsController>` (ufprt) or `CurrentPage` is null; the past-event guard error uses the empty key, so the Event partial must render model-level errors (`asp-validation-summary="ModelOnly"` or equivalent); success = `TempData["AcmeFormSuccess"]` equal to that form's `AcmeFormKeys` Guid string.
- [ ] nit (block 3E, task 3.5) `EventBookingGuardTests.cs`: no case for start exactly == now (guard treats it as past via IsPast); one-line pin, optional.
- [ ] nit (block 3E, task 3.5) Views/4.1 should reuse `EventBookingGuard.EventAlias` and `EventSchedule.StartDateAlias` instead of new literals.
- [ ] nit (block 5A, task 5.1) `UpcomingEvents.cs:30`: deliberate use of obsolete `IPublishedContent.Children` (CS0618; removal in Umbraco 18 never bites the 17.7.1 freeze, ADR-0001). Optional: comment or pragma to mark it deliberate.
- [ ] nit (block 5A, task 5.1) `UpcomingEventsTests.cs`: third private `FakeTime` copy; shared helper optional.
- [ ] nit (blocks 3D/5A) `AcmeFormsComposer.cs:16-17`: fully qualified `SuperPower.Web.Events.*` registrations; a `using` would read better (dup of the 3D nit).
- [x] (Architect, done 2026-10-14) CLAUDE.md said "Rendering: ViewComponent" (package-era leftover); ADR-0016/design/task 4.1 say one partial view per form. CLAUDE.md aligned to ADR-0016.
- [ ] nit (block 4A, task 4.1) Required labels render "Name * (required)": `*` aria-hidden + visually-hidden "(required)"; `DisplayNameFor` call redundant next to `label asp-for` but cannot differ. Fine.
- [ ] note (block 4A review, task 4.1 → 7.1 runtime check) Antiforgery unsettled from the compiled package (~85% confident `BeginUmbracoForm` auto-emits the token; no manual `@Html.AntiForgeryToken()` added — right default, duplicate risk). The 7.1 runtime check MUST confirm per form: exactly one `__RequestVerificationToken` + the `ufprt` field; `novalidate` + method=post on the form tag; no `required`/`pattern`/`<script>`; valid post → PRG + success text on that form only; invalid post → values + field errors redisplayed; past-event booking → ended message via ModelOnly summary; `asp-*` attributes actually expanded (i.e. _ViewImports fix effective site-wide).
- [ ] PO (block 2D, task 2.5) Human-observation residuals, low risk (API-level evidence covers the data path): glance at the Aspire dashboard Metrics tab for `http.server.request.duration`, and open Grafana Explore at `http://localhost:3000` once to confirm a site trace, log and metric.
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
