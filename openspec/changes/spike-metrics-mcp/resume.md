# Resume notes: spike-metrics-mcp

Parked 2026-10-12. Resume with `/opsx:apply spike-metrics-mcp`; read this file,
`review-followups.md` and `findings.md` first. Delete this file when the change is archived.

## State

- Progress: 7/14 tasks ticked (1.1, 1.3-1.6, 2.1, 2.2). Section 1 and 2 code reviewed and APPROVED
  (three block reviews). Section-1 supervisor review not run yet (waits for 1.2).
- Package and images approved by the PO 2026-10-11 (`design.md` D1-D3).
- Nothing is running. Last run (macOS) was stopped and cleaned up.
- Uncommitted: `spikes/metrics-mcp/`, this change folder, one line in `Directory.Packages.props`
  (`CommunityToolkit.Aspire.Hosting.OpenTelemetryCollector` 13.4.0), `PLANNING.md`. To resume on
  another machine the PO commits and pushes first (Claude does not commit).

## Open tasks, in order

| Task | Owner | What is left |
|---|---|---|
| 1.2 | PO check | Dashboard Metrics page, `probe-api`: `probe.visits` and `probe.visit.duration` have data. Code done and reviewed; only this eye check is missing. |
| 3.1 | PO check | Dashboard `http.server.request.duration` count for `probe-api` after one `node load.mjs` run on a fresh start = 45 (25 × 200, 20 × 500). Prometheus already showed exactly this (`findings.md`). |
| supervisor | Architect | Section 1 supervisor review once 1.2 is ticked. |
| 3.2 | PO + Architect | PO runs the two agent sessions per `agent-asks.md`, pastes transcripts into `agent-answers.md`; Architect cross-checks into `findings.md`. |
| 3.3 | Architect | Freshness: time from one request to the change being visible via each MCP server, 3 measurements per candidate. Do while the PO runs 3.2 or after. |
| 3.4 | Architect | Start-up time and `docker stats` memory for modes `none`, `a`, `b`, each a separate run; attendee prerequisites; cross-platform risks. |
| 3.5 | Architect → PO | Talk fit and recommendation (adopt A / adopt B / keep ADR-0011); PO sign-off; update `PLANNING.md`. |
| 3.6 | Architect | After sign-off, with `spike-aspire-probe` task 3.4: delete `spikes/metrics-mcp/`, remove the toolkit package, archive. |

Fresh-start check for 1.2 + 3.1 (one run covers both):
1. `cd spikes/metrics-mcp` → `aspire start -- --Spike:Candidates=both` → wait for all Healthy.
2. `node load.mjs` once, nothing else hitting the app.
3. Dashboard (`https://localhost:17410`, login link printed by `aspire start`) → Metrics →
   `probe-api`: check both custom metrics and the request count.

## Resuming on another machine

- Needs: Docker, Aspire CLI 13.6.1, .NET 10 SDK, Node 20+ (load script). Images (~2 GB incl. SQL
  Server and LGTM) are pulled on first start.
- `spikes/metrics-mcp/` depends on `spikes/aspire-probe/` (Probe.Api, Probe.Web) and
  `src/SuperPower.ServiceDefaults` (both committed already).
- Fixed host ports must be free: 17410 (dashboard), 5171/7171, 5172/7172, 9090, 3000, 8090, 8091.
- Windows (untested so far; record results under task 3.4 cross-platform):
  - Run commands from PowerShell; leftover processes: `Get-Process Probe.Api,Probe.Web -ErrorAction SilentlyContinue | Stop-Process`.
  - Docker Desktop must use Linux containers.
  - The HTTPS dev certificate must be trusted (`dotnet dev-certs https --trust`) for the dashboard.
- `claude mcp list` in `spikes/metrics-mcp/` with mode `both` running should show `aspire`,
  `prometheus`, `grafana` Connected (verified on macOS).

## Known issues and gotchas

- `aspire stop` leaves `Probe.Api` / `Probe.Web` processes; kill them (README) and poll `docker ps`
  until empty before the next start, or ports clash.
- Don't pipe `aspire start` through `tail`/`head` from an agent shell; it left a hung shell once.
- Grafana MCP `query_prometheus` instant queries need `endTime: "now"`.
- Grafana MCP `--allowed-hosts` is tied to host port 8091.
- Each failing web request becomes 4 `probe-api` calls (resilience retries); the expected counts in
  `load.mjs` output account for this.
