# FR-0007: Aspire 17 follow-ups

## Status
Open (2026-11-25). Source: PO 2026-11-24, after the first-round Aspire 17 upgrade (ADR-0035). That round
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
- **Suggested split for `/opsx:propose`:**
  - worker-backend: AppHost, libraries and images; smoke check and settings as shared files assigned in the brief.
  - worker-frontend: none.
  - PO: uSync event dates.
  - Architect: presenter docs.
