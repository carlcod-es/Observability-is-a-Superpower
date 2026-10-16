# ADR-0029: Optimised stage run

## Status
Proposed (2026-10-15)

## Context
Change `improve-local-performance`, spec `local-environment` ("Optimised stage run"). The talk
runs on an M1 Pro (16 GB RAM, 8 GB for Docker Desktop). The PO wants every gain available
without changing demo behaviour, proven with before/after telemetry, landing before 16 Oct.
There is no backoffice use on stage. Current state:
- The demo starts with `aspire run --apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj`,
  which builds Debug. `aspire run` (13.6.1) has no configuration option.
- `SuperPower.Web.csproj` sets `RazorCompileOnBuild=false` / `RazorCompileOnPublish=false`, a
  leftover from the template's `InMemoryAuto` mode (its own comment says to remove them). With
  `ModelsMode` `SourceCodeManual` (Development) and `Nothing` (base), views compile at runtime
  on first hit.
- `appsettings.Development.json` sets `Umbraco:CMS:Hosting:Debug=true`. The Umbraco runtime mode
  is the default, `BackofficeDevelopment`.
- `mcr.microsoft.com/mssql/server:2022-latest` is amd64 only and runs under Rosetta. smtp4dev,
  the collector, LGTM and grafana-mcp are arm64.
- The first Home load after start must show database calls (spec `local-environment`,
  "Telemetry in one place"). Later loads come from Umbraco's cache. PO 2026-10-15: no warm-up
  before the talk; warm speed matters more than start-up speed.

## Decision
1. **Views compiled at build.** Remove `RazorCompileOnBuild` / `RazorCompileOnPublish` (both
   default to true) and the stale comment. This applies to every run, including attendees' runs
   of the site on its own.
2. **Release build for the stage run.** The documented stage command builds AppHost and site in
   Release and starts them through `aspire run`, so the Aspire MCP backchannel keeps working.
   The mechanism (the MSBuild `Configuration` property passed via the environment, or a
   Release build followed by `aspire run --no-build`) is chosen by `improve-local-performance`
   task 2.1, on evidence. The development default stays Debug.
3. **CMS debug off on the stage run only.** The AppHost sets `Umbraco__CMS__Hosting__Debug=false`
   on the site. `appsettings.Development.json` is unchanged, so the site run alone keeps debug.
4. **Runtime mode unchanged** (`BackofficeDevelopment`).
5. **Presenter machine checklist** (presenter runthrough, not code): Docker Desktop with
   Rosetta emulation and VirtioFS on, Docker memory sized from measured `docker stats` with
   LGTM running (so macOS does not swap: raising it starves the site), other heavy apps
   closed, on mains power, Docker and the AppHost restarted fresh before the talk. No warm-up.

Every item is measured before and after with the demo's telemetry (`improve-local-performance`
design, "Measurement") and is dropped if it shows no gain or changes behaviour.

## Alternatives
- **Umbraco `Production` runtime mode.** Its startup validators require a Release build,
  `ModelsMode` `Nothing`, `UseHttps=true` and a fixed `UmbracoApplicationUrl`. HTTPS redirection
  and a single application URL conflict with the localhost-plus-tunnel setup, and once the
  views are compiled at build the extra speed is small. It is a risky change the day before the
  talk. Rejected.
- **Running the AppHost with `dotnet run -c Release`.** It is simple, but the Aspire CLI is what
  registers the AppHost with the Aspire MCP, and the agent depends on that. Kept only as a
  fallback if task 2.1 shows `dotnet run` works with the MCP.
- **SQLite instead of the SQL Server container** (no Rosetta emulation). It removes the SqlClient
  spans the demo shows (ADR-0008, ADR-0009). It changes demo behaviour. Rejected.
- **Seeding Umbraco's cache at start.** It removes the database calls from the first Home trace.
  Rejected.
- **Trace sampling or longer export intervals.** They lose or delay telemetry the demo shows.
  Rejected.
- **ReadyToRun / publish-based run.** Needs a separate publish flow and does not fit
  `aspire run`. Its gain over a Release JIT run is startup only. Rejected for the talk.

## Consequences
- First page loads skip Razor compilation. Startup is somewhat faster in Release. The size of
  each gain is measured, not assumed.
- Precompiled views mean a view edit on stage needs a rebuild. That is acceptable, because the
  feature-building acts change C# and need a restart anyway.
- In Release, `Program.cs` skips `appsettings.Local.json` (`#if DEBUG`). No such file is used
  today. The stage run must not depend on one.
- Breakpoints are unreliable on the Release stage run. The talk uses telemetry, not the
  debugger.
- A second start command exists (stage versus development). README and the presenter runthrough
  name both.
