# Review follow-ups: add-demo-dashboard

Minors and nits from block and section reviews. Read at the start of every apply session.

## Block 2A (tasks 2.1, 2.2), reviewer APPROVED 2026-10-15
- [ ] minor `src/SuperPower.ServiceDefaults/TelemetryPaths.cs:12`: missing space in `"/assets","/favicon.ico"`.
- [x] nit `tests/SuperPower.Web.Tests/PageTypeTagTests.cs:1-2`: using order vs other test files. Not applicable:
  existing test files are mixed (section 1+2 supervisor).
- [x] nit `src/SuperPower.Web/Program.cs`: doubled blank line after the middleware registration. Stale: single blank
  lines now (section 1+2 supervisor).
- [x] runtime check owed: form post carries the page type (smoke D6 "page-type label on form post", task 4.1).
- [x] runtime check owed: static-file requests (if `UseStaticFiles` runs before the middleware) may carry no
  `acme.page_type` instead of `none` (ADR-0024 consequence); check the metric label values.
  Resolved 2026-10-15 (block 1A run): metric shows `acme_page_type="none"`, status 200, count 2 after two
  `/assets/rubber-duck.svg` GETs; static requests carry `none` in metrics, no span (D4).

## Block 3A (task 3.1), reviewer APPROVED 2026-10-15
- [x] minor `tests/traffic/traffic.cs:36,56`: `when (e is not OperationCanceledException)` also skips HttpClient
  timeouts (`TaskCanceledException`): a timed-out loop request stops the program silently, a timed-out start page
  crashes it. Fixed 2026-10-15 (`when (!cts.IsCancellationRequested)`, build clean).
- [ ] minor `tests/traffic/traffic.cs:68`: final "Stopped." line prints counts since the last minute line, not totals.
- [ ] nit `tests/traffic/traffic.cs:32`: `/events` and `/events/` dedupe as different pages.
- [ ] nit `tests/traffic/traffic.cs:26`: regex matches double-quoted `href` only (fine for Razor output).

## Block 1B (tasks 1.2, 1.3), reviewer APPROVED 2026-10-15
- [ ] minor `src/SuperPower.AppHost/grafana/dashboards/demo-site.json` panel 4: Loki panel shows raw log lines;
  PII-free only because the site never logs form values. Re-check in `add-form-emails`.
- [ ] nit `demo-site.json` panel 2: no thresholds / no-data handling (covered by the `or 0 *` guard).
- [ ] nit `src/SuperPower.AppHost/grafana/dashboards/README.md`: no pointer that the AppHost provisions them (D1).

## Sections 1 and 2, supervisor APPROVED 2026-10-15 (section 2 conditional on the form-post check, task 4.1)
- [ ] nit `src/SuperPower.Web/Telemetry/PageTypeTelemetryMiddleware.cs:18`: a pipeline re-execute (exception handler,
  status-code pages) would `Tags.Add` a duplicate tag. None configured today; if `add-form-emails` adds one, replace
  instead of add.
- [ ] nit ADR-0023, ADR-0024 Status "Proposed": set Accepted after PO sign-off (Architect).
- Stage note (PO): browsing the site while traffic runs adds a `none` series (logo, back office) to rate/p95 panels
  (D4, expected). Dashboard edits in Grafana cannot be saved (`allowUiUpdates: false`).

## Block 4A (task 4.1), reviewer CHANGES REQUESTED 2026-10-15, rework APPROVED 2026-10-15
Major (assets check vacuous) and minor (form-post check not tied to own post) fixed: sentinel span wait + strict
Tempo query; form post narrowed to the smoke's service page path. An older post to that page in the window can
still satisfy it (accepted, prototype bar).
- [ ] minor (description only): worker said helpers retry on non-success; only the new `DashboardCheck()` code does.
  Existing `TelemetryCheck` unchanged, still fails loudly.
- [ ] nit `tests/smoke/smoke.cs:363,394`: a Grafana error in `/api/search` reports one "dashboard checks" FAIL and
  skips the other four checks (acceptable at prototype bar).
- Observation (smoke run 2026-10-15): first Home load showed 2 database spans (earlier runs 10); check needs > 0.

## Tasks 5.1, 5.2 (docs), reviewer APPROVED 2026-10-15
- [x] nit `docs/project-setup.md` "Filtered out": "metric still counts them under `none`" unverified by smoke.
  Verified by the block 1A run (Prometheus `none`/200 count 2 after two `/assets` GETs).
- [ ] nit `README.md` "Demo traffic": does not say run from the repo root with the site up.
- [ ] nit `README.md` "Demo traffic": `requests=<n> failures=<n>` are per-minute counts; could say so.

## Sections 4 and 5, supervisor APPROVED 2026-10-15
- [ ] minor `tests/smoke/smoke.cs:389-390`: spec `local-environment` "Not found" has no repeatable check; the
  sentinel 404 span is fetched but `acme.page_type=none` not asserted. Verified once (task 2.1; supervisor live
  query: sentinel span `none`/404). Optional: add a "page-type label on not found" Report.
- [x] minor tasks.md 6.1 / verification.md: "Generic dashboard" had no UI acceptance. Added to 6.1 (Architect).
- [x] minor PLANNING.md "Next step": stale open items. Fixed (Architect).
- [ ] nit `tests/smoke/smoke.cs:17`: "the telemetry section starts it" -> section 0 starts `lgtm`.
- [ ] nit `README.md:178`: Tests summary omits page-type tag unit tests (project-setup has them).
- [x] nit block 4A follow-up line reference corrected to `smoke.cs:363,394`.

## Block 7A (first-plan task 7.2), reviewer APPROVED 2026-10-15; reverted (PO dropped FR-0005)
- [x] nit `tests/smoke/smoke.cs:73-74`: AppHost `appsettings.json` without `Parameters:sql-password` throws a bare
  `KeyNotFoundException` (acceptable at prototype bar).

## Blocks 7B, 7C (tasks 7.1, 7.2), reviewer APPROVED 2026-10-15
- [x] minor README "Run the demo" step 8: no `/mcp` reconnect for attendees who skip step 6. Fixed.
- [x] minor README "Run the demo" step 5: where the metric is. Fixed (Metrics > `site` > `http.server.request.duration`).
- [ ] nit README "Run the demo" steps 5 and 8: lines exceed the file's wrap width.

## PO acceptance on Windows (section 6), 2026-10-15
- [ ] major `tests/smoke/smoke.cs:486` `Run()`: `Process.Start("aspire")` cannot start the .NET-tool shim
  `aspire.cmd` on Windows ("The system cannot find the file specified"). lgtm pre-start, telemetry, dashboard checks,
  mail setup and mail heal fail. PO accepted as pass for this change. Fix: on Windows start via `cmd.exe /c aspire ...`.
- [ ] minor README "Connect to the database": says server `localhost,<port>`; the SQL container publishes on
  127.0.0.1 only and the smoke check now uses `127.0.0.1` (PO fix, `smoke.cs:74`). Align the README.
