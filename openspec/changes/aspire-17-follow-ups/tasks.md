# Tasks

Bar: demo (design.md Context).

**Order** (design.md Risks, Migration Plan):

| Phase | Sections |
|---|---|
| **Now** | 1 and 2 in parallel (disjoint files); 6; 7.1; then 8 (reset, `aspire start`, smoke #1, rehearsal; the reset dates the events, design.md D1) |
| **Next, if needed** | 3 → 4, before the demo only if the smoke check and a rehearsal pass; otherwise after the demo |
| **After the demo** | 5; 3-4 if they did not ship before the demo |

All worker sections: run `dotnet build SuperPower.slnx` and `dotnet test SuperPower.slnx` before reporting.

## 1. Event dates relative to the import day (Now; worker-backend; `src/SuperPower.Web/Events/`, a new composer there, `tests/SuperPower.Web.Tests/`; no dependencies)

- [x] 1.1 Confirm uSync 17.4.3's importing-item notification fires during `ImportOnFirstBoot` and lets a handler change the item before it is saved; if not, confirm the ADR-0037 fallback (shift after import with export on save disabled for that save). If neither works, stop and report; verify by reporting the notification type, the property used, and evidence from a first-boot import on a fresh SQLite database (`dotnet run --project src/SuperPower.Web` after deleting `umbraco/Data/Umbraco.sqlite.db*` and `umbraco/Data/TEMP/`)
- [x] 1.2 Pure calculation per design.md D1:
  - Shift by the whole days from the reference day 2026-10-16 to the import day (Europe/London dates).
  - Keep the local time and recompute the offset; `timeZone` unchanged.
  - Clock from `TimeProvider`.

  Verify with new xUnit tests:
  - 20 Oct 12:50 BST imported on 2026-11-26 gives 30 Nov 12:50 GMT (+00:00).
  - Import on 2026-10-16 changes nothing.
  - A date crossing the summer/winter time change keeps its local time.
  - The JSON round-trips with `timeZone` kept.

  `dotnet test SuperPower.slnx` passes.
- [x] 1.3 Handler and composer that apply 1.2 to `event` items during import (design.md D1, ADR-0037). Verify on a fresh SQLite database:
  - The Events page lists 4 upcoming events dated D+4, D+12, D+20 and D+28 at their original times (D = import day;
    design.md D1 "Planned dates").
  - The D−10 event page shows "This event has ended".
  - `git status` shows no change under `src/SuperPower.Web/uSync/`.
  - A second start on the same database leaves the dates unchanged.

## 2. Smoke check builds (Now; worker-backend; `Directory.Packages.props` one line only; no dependencies)

- [x] 2.1 Add `PackageVersion` `Microsoft.Data.SqlClient` 6.1.6 (design.md D5, ADR-0036). Verify:
  - `dotnet run tests/smoke/smoke.cs` builds.
  - After section 1 is merged: stop the AppHost, reset SQL Server, cache and queue (README "Reset the
    databases"), `aspire start`, and the smoke check reports `ALL CHECKS PASSED`, booking checks included.

## 3. Library updates and warnings (Next, if needed; worker-backend; `Directory.Packages.props`, `src/SuperPower.Web/Events/UpcomingEvents.cs`, `tests/SuperPower.Web.Tests/BookingCountsTests.cs`; depends on 1 and 2)

- [ ] 3.1 Versions per ADR-0036 table (Umbraco 17.8.2, uSync 17.6.3, OpenTelemetry 1.21.0, Microsoft.Extensions 10.11.1, xunit.v3 4.0.3). Verify:
  - `dotnet build SuperPower.slnx` has no high-severity NU190x warning. If one remains, stop and report
    (design.md D4).
  - `dotnet test SuperPower.slnx` passes.
- [ ] 3.2 `UpcomingEvents.GetUpcoming` uses the `Children()` extension method (design.md D2); `BookingCountsTests` passes `TestContext.Current.CancellationToken` (xUnit1051). Verify the build shows neither warning and tests pass.
- [ ] 3.3 Verify on a reset AppHost environment: `dotnet run tests/smoke/smoke.cs` reports `ALL CHECKS PASSED`; report uSync and Umbraco start-up warnings, if any, from `aspire otel logs site --severity Warning`.

## 4. AppHost project model (Next, if needed; worker-backend; `src/SuperPower.AppHost/SuperPower.AppHost.csproj`, `src/SuperPower.AppHost/AppHost.cs`, `Directory.Packages.props`; depends on 3)

- [ ] 4.1 Migrate per design.md D3 (ADR-0036). Verify:
  - `dotnet build SuperPower.slnx` shows no ASPIRE012, ASPIRE013 or CS0618.
  - `aspire start` brings every resource up healthy (`aspire describe`).
  - `site` still reaches `api`: an event page shows the booking count after a booking.
- [ ] 4.2 Verify:
  - `aspire resource site rebuild` and `aspire resource api rebuild` work (report output).
  - On a reset environment the smoke check reports `ALL CHECKS PASSED`.

## 5. Container images (After the demo; worker-backend; `src/SuperPower.AppHost/AppHost.cs` image tags only; depends on 4)

- [ ] 5.1 Update lgtm to 0.41.1 and grafana-mcp to 2.4.0 (design.md D6). Verify:
  - Both demo dashboards are provisioned.
  - `claude mcp list` shows `grafana` connected.
  - The tool list is read-only (no write tools).
  - The smoke check passes.

  If either image fails, keep its old tag and report.
- [ ] 5.2 Update the collector to 0.165.0 and smtp4dev to 3.16.1. Verify:
  - The collector starts with no config errors in its console log.
  - Telemetry reaches both the Aspire dashboard and Grafana.
  - The smtp4dev inbox receives owner emails.
  - The smoke check passes.

  Keep the old tag on failure.

## 6. Agent settings and README (Now; worker-backend by brief; shared: `.claude/settings.json`, `README.md`; depends on 1)

- [x] 6.1 Add `mcp__aspire__get_integration_docs` to `.claude/settings.json` `allow` (design.md D7). Verify the JSON parses (`python3 -m json.tool .claude/settings.json`).
- [x] 6.2 README per design.md D7: the Aspire agent skills note (exists, left out and why, `aspire agent init` for own projects) and one line under "Reset the databases" that events get fresh dates. Verify the README has no planted problems or fixes and that spec `local-environment` "Attendee README" scenario "Attendee asks about agent skills" is met (reviewer reads it).

## 7. Docs and runthrough (Architect; `docs/`, `openspec/`, `CLAUDE.md`, `PLANNING.md`)

- [ ] 7.1 Now — `docs/presenter/runthrough.md` per design.md D8:
  - Generic date.
  - Morning includes the database reset.
  - Aspire 17 commands and dashboard steps re-checked.

  Verify every command in Scripts and Morning runs as written on the Mac, and the dashboard steps match
  the Aspire 17 UI.
- [ ] 7.2 After the demo — statuses and docs:
  - ADR-0036 and ADR-0037 Status → Accepted (with any deviations).
  - ADR-0003, ADR-0014 and ADR-0035 Status notes.
  - CLAUDE.md "Demo site" events line and "Stack".
  - `docs/project-setup.md` versions.
  - FR-0007 Status → this change.
  - PLANNING.md.

  Verify `openspec validate aspire-17-follow-ups --strict` passes.

## 8. Rehearsal (Now; PO; depends on 1, 2, 7.1 — and on 3-4 if they ship before the demo)

- [ ] 8.1 (PO) Morning run on the stage machine: reset, `aspire start`, smoke #1, then Acts 1-3 with the agent on the Grafana MCP; verify each act's problem shows and its fix restores normal behaviour, and smoke #2 (`-- running`) passes.

## Workflow follow-up

- Archive with `/opsx:archive` after section 5 and the after-the-demo check; sync specs `agency-site` and `local-environment`.
