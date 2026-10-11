Bar: throwaway. No tests; each task is verified by observation and recorded in `findings.md`.

## 1. Probe app

Owner: worker-backend. Shared files assigned: `Directory.Packages.props` (add
`Microsoft.Data.SqlClient` 6.1.6 only). Depends on: none. Do not edit
`src/SuperPower.ServiceDefaults/` or `SuperPower.slnx`.

- [x] 1.1 Create `spikes/aspire-probe/` with `AspireProbe.slnx` and three projects: `Probe.AppHost` (Aspire AppHost SDK 13.6.0), `Probe.Web` and `Probe.Api` (ASP.NET Core minimal APIs, .NET 10), both referencing `src/SuperPower.ServiceDefaults` and calling `AddServiceDefaults()` / `MapDefaultEndpoints()`; the AppHost runs a SQL Server container via `AddAzureSqlServer("sql").RunAsContainer()` with database `probe` (no persistent lifetime, no data volume), and `Probe.Api` (references the database, waits for it) and `Probe.Web` (references the API, external HTTP endpoint); verify `dotnet build spikes/aspire-probe/AspireProbe.slnx` succeeds and `aspire run` from `spikes/aspire-probe/` shows all three resources running
- [x] 1.2 `Probe.Api`: on startup create table `visit` (id, path, created UTC) if missing using `Microsoft.Data.SqlClient`; endpoint `GET /visits` inserts a row and returns the count; query `?slow=<ms>` adds a delay inside a custom span, `?fail=true` throws after the insert; an `ActivitySource` and `Meter` named `Probe.Api` (registered with the OpenTelemetry tracer and meter providers in `Program.cs`) with a counter `probe.visits` and a histogram `probe.visit.duration` (ms); one structured log per request with named properties (`Path`, `VisitCount`); one `Console.WriteLine` on startup; verify with `curl` that `/visits`, `?slow=2000` and `?fail=true` return 200, ~2 s 200, and 500 respectively
- [x] 1.3 `Probe.Web`: `GET /` returns a small HTML page with the visit count fetched from `Probe.Api` over service discovery (`https+http://probe-api`), passing `slow` and `fail` query values through; links on the page for normal, slow and failing calls; verify each link renders (the failing one shows an error message, not a crash page)

## 2. Agent configuration

Owner: Architect. Depends on: 1.1.

- [x] 2.1 Add `spikes/aspire-probe/.mcp.json` with server `aspire` → `aspire agent mcp` (ADR-0011 shape), no `.claude/settings.json` allow-list (prompts are fine in the spike); verify a Claude Code session started in `spikes/aspire-probe/` lists the `aspire` MCP server as connected

## 3. Investigation and findings

Owner: Architect, with the PO observing the dashboard. Depends on: 1, 2.

- [x] 3.1 Dashboard: with the AppHost running, click each link several times; verify and record in `findings.md` (Q1): structured logs per service with named properties; logs linked to traces; one trace spanning web → API → SQL; the failing trace marked as error with the exception; built-in metrics (ASP.NET Core, HttpClient, SqlClient, runtime) and `probe.visits` / `probe.visit.duration` visible on the Metrics page; console output visible
- [x] 3.2 Agent: in a Claude Code session in `spikes/aspire-probe/`, ask it to list resources, show console logs, find the slowest and the failing request, and show that trace's logs; record in `findings.md` (Q2) the tools called, what each returned, and whether any metric value was reachable; verify every answer against the dashboard
- [x] 3.3 Summarise in `findings.md` what changes for the demo (if anything); update ADR-0011 Consequences if a finding contradicts it; report to the PO
- [ ] 3.4 After PO sign-off: delete `spikes/aspire-probe/`, remove `Microsoft.Data.SqlClient` from `Directory.Packages.props` unless another project uses it; verify `dotnet build` at the root still succeeds
  - Signed off 2026-10-11; deletion deferred by the PO until closer to the talk (`PLANNING.md` Todos).
