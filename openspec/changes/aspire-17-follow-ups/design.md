# Design

## Context

Motivation: see proposal.md "Why". Current state after ADR-0035 (Aspire 17.0.0 on `feature/upgrade-to-17`):
- **Build:** the AppHost still uses `Aspire.AppHost.Sdk/17.0.0` and `AddProject<Projects.*>` (ASPIRE012,
  CS0618). NuGet audit warnings come through Umbraco 17.7.1.
- **Smoke check:** `dotnet run tests/smoke/smoke.cs` fails with NU1010. `Microsoft.Data.SqlClient` has no
  `PackageVersion`; it was removed in the spike cleanup.
- **Event content:** five events in `src/SuperPower.Web/uSync/v17/Content/*.config`, all now past (latest
  2026-11-13). `startDate` is JSON with date, offset and `timeZone: Europe/London` (ADR-0014). Views read
  `Model.StartDate`. `EventSchedule` and `UpcomingEvents` apply the past rule.
  `UpcomingEvents.GetUpcoming` uses the obsolete `IPublishedContent.Children`.
- **Content import:** `ImportOnFirstBoot: true` (ADR-0003). uSync exports on save (default).
- **Runthrough:** `docs/presenter/runthrough.md` is written for 16 Oct and Aspire 13.6. It uses
  `aspire resource <name> rebuild` and the dashboard's Rebuild.
- **Tried on a scratch copy (2026-11-24):** AppHost migration plus library updates build clean, pass 75/75
  tests and show no audit warnings.
- **Next demo:** the PO gives the talk again soon after this design, and keeps giving it.

Bar: **demo** (CLAUDE.md "Principle"). Tests:
- New xUnit tests for the date-shift calculation only. It is pure logic and the one place a bug would
  silently remove the booking form.
- Everything else is verified by the build output, the smoke check (`ALL CHECKS PASSED` on a fresh
  database) and an act rehearsal.

## Goals / Non-Goals

**Goals:**
- The next demo runs with bookable events and a correct runthrough, even if the riskier updates miss it.
- A clean build and a smoke check that passes on any date.

**Non-Goals:**
- Dates on an existing database.
- Making the reference day configurable.
- Any behaviour change in pages, forms, telemetry or the planted problems.
- Umbraco 18 or 19.

## Decisions

### D1. Event dates shifted on import (ADR-0037)
- **Where:** a site notification handler for uSync's importing-item notification, registered in a
  composer next to the events code.
- **What it does:** for `event` items it rewrites the `startDate` JSON before uSync saves:
  - Date: original local date plus N days, with N = (Europe/London date of the import) − 2026-10-16.
  - Time of day: unchanged.
  - Offset: recomputed with `TimeZoneInfo` "Europe/London" (works on all three OSes).
  - `timeZone`: unchanged.
- **Pure calculation:** `ShiftStart(original, importDay)` in `EventSchedule.cs` or a sibling file. It takes
  the clock from `TimeProvider`, so it can be unit-tested.
- **First check:** the worker confirms the notification can change the item and fires during
  `ImportOnFirstBoot`. If not, the ADR-0037 fallback applies: shift after import with export on save
  disabled for that save. Either way the committed `.config` files must not change. The worker stops and
  reports if neither works.
- **Alternatives:** see ADR-0037 (manual re-dating, render-time shift, shift every start, seed in code).
- **Planned dates:** relative to the import day D (the database reset before a demo):

  | Event | Committed date | After import |
  |---|---|---|
  | Creating great editor experiences | 2026-10-06 13:57 | D−10, 13:57 (past, "This event has ended") |
  | How to get the most out of your CMS | 2026-10-20 12:50 | D+4, 12:50 (first upcoming; the smoke check books it) |
  | Upgrading: when is the best time? | 2026-10-28 12:50 | D+12, 12:50 |
  | Migrating to Umbraco from other CMSs | 2026-11-05 13:57 | D+20, 13:57 |
  | Umbraco Builders Day | 2026-11-13 09:00 | D+28, 09:00 |

  Committed times are Europe/London local time.

### D2. Obsolete `Children`
- **Change:** `UpcomingEvents.GetUpcoming` uses the `Children()` extension method (Umbraco.Extensions)
  instead of the obsolete property.
- **Why:** same behaviour; it is needed before any v18 move and is required by "Clean build".
- **Alternative:** `IDocumentNavigationQueryService`. It returns keys only, so we would need a second lookup;
  rejected.

### D3. AppHost project model (ADR-0036)
- **Change:**
  - Use `Microsoft.NET.Sdk` with `Aspire.Hosting.AppHost` and `Aspire.Hosting.Dotnet` 17.0.0.
  - Remove the two `ProjectReference`s.
  - Replace each `AddProject<Projects.SuperPower_X>("name")` with `AddDotnetProject("name", "../SuperPower.X/SuperPower.X.csproj")`.
  - Resource names, chained calls, `UserSecretsId` and `AspireUseCliBundle` stay.
- **Checks:**
  - `WithReference(api)` still gives `site` the `services__api__*` discovery variables.
  - `aspire resource site rebuild` still works (runthrough).
- **Alternatives:** keep the obsolete SDK, or suppress the warnings (ADR-0036); rejected.

### D4. Library versions (ADR-0036)
- **Change:** the versions table in ADR-0036, in `Directory.Packages.props` only. Microsoft.Extensions stays
  on 10.11.1.
- **Done when:** `dotnet build SuperPower.slnx` has no NU1901-NU1904 at high severity and no ASPIRE012,
  ASPIRE013 or CS0618.
- **If a high-severity warning remains:** the worker stops and reports. The fallback, pinning that one
  transitive package, is an Architect decision.
- **xUnit1051 in `BookingCountsTests.cs`:** pass `TestContext.Current.CancellationToken`. Not required by
  the spec; done because the file is open.

### D5. Smoke check builds
- **Change:** add `<PackageVersion Include="Microsoft.Data.SqlClient" Version="6.1.6" />` (the version
  Umbraco resolves).
- **Unchanged:** no change to `smoke.cs` checks.
- **Alternative:** pin versions in the script with `#:package X@v`. That bypasses central management, the
  repo's one-place rule (ADR-0001); rejected.

### D6. Container images (ADR-0036), after the demo
- **New tags:** lgtm 0.41.1, mcp-grafana 2.4.0, collector 0.165.0, smtp4dev 3.16.1.
- **Must still work:**
  - lgtm: the provisioning mount paths (`/otel-lgtm/grafana/conf/provisioning/dashboards/`, `/otel-lgtm/acme-dashboards`).
  - mcp-grafana: the flags `-t streamable-http --address --disable-write --enabled-tools --allowed-hosts`.
  - collector: `collector/config.yaml` keys.
  - smtp4dev: the `ServerOptions__*` environment names.
- **Per image:** if something fails, keep that image's old tag and report.

### D7. Agent setup and README (ADR-0036)
- **`.claude/settings.json`:** add `mcp__aspire__get_integration_docs` to `allow`.
- **README:** in the agent section, near "Enable the Aspire MCP", add a short note:
  - The Aspire agent skills exist.
  - The repo leaves them out: the agent could start, stop and restart services without the approval prompt,
    and the demo has the agent ask instead.
  - Attendees can install them in their own projects with `aspire agent init`.
- **README "Reset the databases":** add one line saying events get fresh dates after a reset.

### D8. Runthrough and docs (Architect)
- **Runthrough** (`docs/presenter/runthrough.md`):
  - Generic header date.
  - Check each command against Aspire 17 (`aspire start`, `aspire resource ... rebuild`, `aspire stop`) and
    each dashboard step (Rebuild, logs, traces, metrics views).
  - Morning step: reset the database so events are dated from the demo day.
- **Other docs:**
  - CLAUDE.md "Demo site" events line.
  - ADR-0003 and ADR-0014 Status (amended by ADR-0037).
  - ADR-0035 Status.
  - `docs/project-setup.md` where it names versions.
  - FR-0007 Status.
  - PLANNING.md.

## Risks / Trade-offs

- [The demo is soon; the library or AppHost updates could break an act] → Phases in tasks.md:
  - **Now:** sections 1 and 2 (dates, smoke), 6 and the runthrough. The demo needs them.
  - **Next, if needed:** sections 3 and 4 ship before the demo only after a fresh-database smoke pass and
    an act rehearsal; otherwise after the demo.
  - **After the demo:** section 5 (images).
- [The uSync notification cannot change the imported item] → D1 fallback. The worker stops if the committed
  files would change.
- [Wrong offset across a summer/winter time change] → Shift the local date and recompute the offset (D1).
  Unit tests cover a date that crosses the change.
- [The presenter's existing database keeps old dates] → Reset the database in the morning (runthrough).
  Code changes never need it otherwise.
- [`AddDotnetProject` changes resource behaviour (discovery variables, rebuild command)] → D3 checks, smoke
  check, `aspire resource site rebuild`.

## Migration Plan

1. **Now:** merge sections 1, 2 and 6 and the runthrough.
2. **Morning of the demo:** stop the AppHost and reset SQL Server, cache and queue (README). Then
   `aspire start` and smoke #1.
3. **Next, if needed:** sections 3-4, only if they pass the smoke check and a rehearsal before the demo.
4. **After the demo:** section 5, plus sections 3-4 if they waited.

Rollback: revert the section's commit. Content dates fall back on the next reset.
