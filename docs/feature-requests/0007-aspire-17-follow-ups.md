# FR-0007: Aspire 17 follow-ups

## Status
In progress (2026-11-25; ADR-0036, ADR-0037). Event dates: relative to the import day (PO 2026-11-25). Opened 2026-11-25. Source: PO 2026-11-24, after the first-round Aspire 17 upgrade (ADR-0035). That round
was Aspire 17.0.0 packages, the AppHost SDK and the collector toolkit; these items were deferred.

## Request
Finish the upgrade on the Umbraco 17 LTS and .NET 10:
1. Migrate the AppHost off the deprecated Aspire APIs.
2. Update the other libraries and the container images.
3. Get the smoke check passing again.
4. Bring the presenter material in line with the Aspire 17 dashboard.

## Why
- **Warnings:** the build warns about deprecated Aspire APIs (ASPIRE012, CS0618). It also has NuGet audit
  warnings, some high severity, from packages Umbraco pulls in.
- **Smoke check:** it no longer builds. Its booking checks cannot pass while every event is in the past.
- **Stage:** the redesigned dashboard may change the presenter steps used on stage.

## Acceptance criteria
- **AppHost migration** (no ASPIRE012 or CS0618 warnings):
  - `Sdk="Microsoft.NET.Sdk"`.
  - Add `Aspire.Hosting.AppHost` and `Aspire.Hosting.Dotnet` 17.0.0.
  - Drop the `ProjectReference`s to `SuperPower.Api` and `SuperPower.Web`.
  - Replace `AddProject<Projects.SuperPower_Api>("api")` with
    `AddDotnetProject("api", "../SuperPower.Api/SuperPower.Api.csproj")`, and the same for `site`.
  - Resource names and their chained configuration stay unchanged.
- **Libraries** (stay on `net10.0`):

  | Package | From | To |
  |---|---|---|
  | Umbraco.Cms.* | 17.7.1 | 17.8.2 |
  | uSync | 17.4.3 | 17.6.3 |
  | OpenTelemetry.* | 1.17.0 | 1.21.0 |
  | Microsoft.Extensions.Http.Resilience | 10.10.0 | 10.11.1 |
  | Microsoft.Extensions.ServiceDiscovery | 10.10.0 | 10.11.1 |
  | xunit.v3 | 4.0.1 | 4.0.3 |

  - Not Microsoft.Extensions 11.x: it pulls .NET 11 libraries into the net10 app.
  - The build has no high-severity NuGet audit warnings. Current sources: SixLabors.ImageSharp 3.1.12 and
    System.Security.Cryptography.Pkcs/Xml (high), and MessagePack 3.1.8 (moderate). All come in through Umbraco.
- **Container images:** update to the latest stable tags, then re-check all three acts:

  | Image | From | To | Check |
  |---|---|---|---|
  | `grafana/otel-lgtm` | 0.36.0 | 0.41.1 | dashboard provisioning paths |
  | `grafana/mcp-grafana` | 2.0.2 | 2.4.0 | `--disable-write`, `--enabled-tools`, `--allowed-hosts` still valid; read-only |
  | collector | 0.162.0 | latest stable | `collector/config.yaml` still valid |
  | smtp4dev | 3.15.0 | latest stable | |

- **Smoke check:**
  - `dotnet run tests/smoke/smoke.cs` builds. It now fails with NU1010 because `Microsoft.Data.SqlClient` has
    no `PackageVersion` in `Directory.Packages.props`; 6.1.6 is the version Umbraco resolves.
  - It prints `ALL CHECKS PASSED` on a freshly started AppHost.
  - (PO) At least one event is upcoming: move the event dates forward in the backoffice and re-export uSync.
- **Presenter material:** `docs/presenter/` steps and screenshots match the Aspire 17 dashboard, especially
  resource Rebuild and the logs, traces and metrics pages.
- **Permissions:** `.claude/settings.json` pre-approves the new Aspire MCP read tool
  `mcp__aspire__get_integration_docs`.
- **Code warnings:**
  - `src/SuperPower.Web/Events/UpcomingEvents.cs` no longer uses the obsolete `IPublishedContent.Children`.
  - `tests/SuperPower.Web.Tests/BookingCountsTests.cs` has no xUnit1051 warnings.

## Tasks
Bar: demo. All worker groups run `dotnet build SuperPower.slnx` and `dotnet test SuperPower.slnx`
before reporting.

Order:
- **Now:** 1 and 2 (done), 6 (done), 7.1, then 8. The database reset in 8 dates the events (ADR-0037).
- **Next, if needed:** 3 then 4, before the demo only if the smoke check and a rehearsal pass.
- **After the demo:** 5, plus 3-4 if they did not ship before it.

### 1. Event dates relative to the import day (worker-backend; done)
- [x] 1.1-1.3 Dates shifted on uSync import (ADR-0037).

### 2. Smoke check builds (worker-backend; done)
- [x] 2.1 `PackageVersion` `Microsoft.Data.SqlClient` 6.1.6.

### 3. Library updates and warnings (worker-backend; `Directory.Packages.props`, `src/SuperPower.Web/Events/UpcomingEvents.cs`, `tests/SuperPower.Web.Tests/BookingCountsTests.cs`; depends on 1 and 2)
- [ ] 3.1 Versions per the ADR-0036 table, in `Directory.Packages.props` only; Microsoft.Extensions stays
  on 10.11.1. Verify the build has no high-severity NU1901-NU1904 warning and tests pass. If one remains,
  stop and report: pinning a transitive package is an Architect decision.
- [ ] 3.2 `UpcomingEvents.GetUpcoming` uses the `Children()` extension method (Umbraco.Extensions)
  instead of the obsolete property; `BookingCountsTests` passes `TestContext.Current.CancellationToken`
  (xUnit1051). Verify the build shows neither warning and tests pass.
- [ ] 3.3 On a reset AppHost environment, `dotnet run tests/smoke/smoke.cs` reports `ALL CHECKS PASSED`;
  report uSync and Umbraco start-up warnings, if any, from `aspire otel logs site --severity Warning`.

### 4. AppHost project model (worker-backend; `src/SuperPower.AppHost/SuperPower.AppHost.csproj`, `src/SuperPower.AppHost/AppHost.cs`, `Directory.Packages.props`; depends on 3)
- [ ] 4.1 Migrate per ADR-0036: `Microsoft.NET.Sdk` with `Aspire.Hosting.AppHost` and
  `Aspire.Hosting.Dotnet` 17.0.0; remove the two `ProjectReference`s; replace each
  `AddProject<Projects.SuperPower_X>("name")` with
  `AddDotnetProject("name", "../SuperPower.X/SuperPower.X.csproj")`. Resource names, chained calls,
  `UserSecretsId` and `AspireUseCliBundle` stay. Verify:
  - The build shows no ASPIRE012, ASPIRE013 or CS0618.
  - `aspire start` brings every resource up healthy (`aspire describe`).
  - `site` still reaches `api`: an event page shows the booking count after a booking.
- [ ] 4.2 Verify `aspire resource site rebuild` and `aspire resource api rebuild` work (report output),
  and on a reset environment the smoke check reports `ALL CHECKS PASSED`.

### 5. Container images (worker-backend; `src/SuperPower.AppHost/AppHost.cs` image tags only; depends on 4; after the demo)
If an image fails, keep its old tag and report.
- [ ] 5.1 lgtm 0.41.1 and grafana-mcp 2.4.0. Must still work: the provisioning mount paths
  (`/otel-lgtm/grafana/conf/provisioning/dashboards/`, `/otel-lgtm/acme-dashboards`) and the flags
  `-t streamable-http --address --disable-write --enabled-tools --allowed-hosts`. Verify:
  - Both demo dashboards are provisioned.
  - `claude mcp list` shows `grafana` connected.
  - The tool list is read-only (no write tools).
  - The smoke check passes.
- [ ] 5.2 Collector 0.165.0 and smtp4dev 3.16.1. Must still work: `collector/config.yaml` keys and the
  smtp4dev `ServerOptions__*` environment names. Verify:
  - The collector starts with no config errors in its console log.
  - Telemetry reaches both the Aspire dashboard and Grafana.
  - The smtp4dev inbox receives owner emails.
  - The smoke check passes.

### 6. Agent settings and README (worker-backend by brief; done)
- [x] 6.1 `mcp__aspire__get_integration_docs` pre-approved in `.claude/settings.json`.
- [x] 6.2 README: Aspire agent skills note and fresh event dates after a reset.

### 7. Docs and runthrough (Architect)
- [ ] 7.1 Now: `docs/presenter/runthrough.md` has a generic date, the Morning includes the database
  reset, and every command (`aspire start`, `aspire resource ... rebuild`, `aspire stop`) and dashboard
  step (Rebuild, logs, traces, metrics) is re-checked against Aspire 17 on the Mac.
- [ ] 7.2 After the demo:
  - ADR-0036 and ADR-0037 Status: Accepted (with any deviations).
  - ADR-0003, ADR-0014 (amended by ADR-0037) and ADR-0035 Status notes.
  - CLAUDE.md "Demo site" events line and "Stack".
  - `docs/project-setup.md` versions.
  - This FR's Status: Done.

### 8. Rehearsal (PO; depends on 1, 2, 7.1, and on 3-4 if they ship before the demo)
- [ ] 8.1 (PO) Morning run on the stage machine: reset, `aspire start`, smoke #1, then Acts 1-2 with the
  agent on the Grafana MCP; each act's problem shows and its fix restores normal behaviour; smoke #2
  (`-- running`) passes.

### Follow-ups
None recorded.

## Notes
- **Tried on a scratch copy (2026-11-24):** the AppHost migration plus the library updates built clean and
  passed 75/75 tests. The audit warnings were not reported with Umbraco 17.8.2.
- **Aspire skills** (`aspire agent init --skills aspire`): proposed to keep them out of the repo; PO to confirm.
  - `aspire-orchestration` restarts resources through the CLI, which breaks "the agent asks, the presenter
    restarts" (ADR-0033).
  - Skills also bypass MCP permissions, which is why ADR-0022 rejected them.
  - At most, add a README note for attendees' own projects.
- **Umbraco 19:** targets .NET 11 and is at 19.0.0-rc2 (2026-11-24). Re-evaluate once it is stable.
  `IPublishedContent.Children` is removed in v18.
