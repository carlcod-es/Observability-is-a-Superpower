# Resume notes: spike-metrics-mcp

Updated 2026-10-12. Resume with `/opsx:apply spike-metrics-mcp`; read this file,
`review-followups.md` and `findings.md` first. Delete this file when the change is archived.

## State

- Scope decision 2026-10-12 (PO): test only candidate B (Grafana LGTM + Grafana MCP); candidate A
  dropped (`proposal.md`, `design.md` D2). Task 1.7 removed it from the code.
- Progress: 14/15 ticked (3.6 held). PO signed off 2026-10-13: adopt B alongside the Aspire MCP.
  Supervisor majors and README minors fixed 2026-10-12.
- Package and images approved by the PO 2026-10-11 (`design.md` D1-D3).
- A stack may still be running (mode `both`): check `aspire ps` / `docker ps` and stop it.
- Uncommitted: this change folder (and possibly `spikes/metrics-mcp/` edits). Claude does not commit.

## Open tasks, in order

| Task | Owner | What is left |
|---|---|---|
| 1.2 | done | PO confirmed `probe.visits` and `probe.visit.duration` on the dashboard, 2026-10-12. |
| 3.1 | done | Dashboard `http.server.request.duration` count for `probe-api` = 90 after two runs (45 per run), PO confirmed 2026-10-12. |
| 3.2 | done | Agent run, cross-check against Grafana API and dashboard (Firefox via playwright-cli) recorded in `findings.md`, 2026-10-12. |
| 3.3 | done | Freshness measured 2026-10-12 (`findings.md`). |
| 3.4 | done | Start-up and memory measured 2026-10-12 (`findings.md`). |
| 3.5 | done | PO signed off 2026-10-13 (adopt B alongside the Aspire MCP). `PLANNING.md` updated. |
| 3.6 | HELD (PO 2026-10-13) | Delete `spikes/metrics-mcp/`, remove the toolkit package, archive: only after the new ADR and the `add-demo-foundation` plan exist, together with `spike-aspire-probe` task 3.4. |

Dashboard reading tips (found 2026-10-12):
- The Metrics page defaults to "Last 5 minutes" and lists only instruments with data in that
  window. Set Duration to 15 minutes or check right after a load run.
- Playwright: `playwright-cli` is installed globally; direct URL
  `https://localhost:17410/metrics/resource/probe-api?meter=<meter>&instrument=<name>&duration=15`.
  Clicking the Table tab by text did not work; use a JS eval click or read it by eye.

## Resuming on another machine

- Needs: Docker, Aspire CLI 13.6.1, .NET 10 SDK, Node 20+ (load script). Images (~2 GB incl. SQL
  Server and LGTM) are pulled on first start.
- `spikes/metrics-mcp/` depends on `spikes/aspire-probe/` (Probe.Api, Probe.Web) and
  `src/SuperPower.ServiceDefaults` (both committed already).
- Fixed host ports must be free: 17410 (dashboard), 5171/7171, 5172/7172, 3000, 8091.
- Windows (untested so far; record results under task 3.4 cross-platform):
  - Run commands from PowerShell; leftover processes: `Get-Process Probe.Api,Probe.Web -ErrorAction SilentlyContinue | Stop-Process`.
  - Docker Desktop must use Linux containers.
  - The HTTPS dev certificate must be trusted (`dotnet dev-certs https --trust`) for the dashboard.
- `claude mcp list` in `spikes/metrics-mcp/` with mode `b` running should show `aspire` and
  `grafana` Connected.

## Known issues and gotchas

- `aspire stop` leaves `Probe.Api` / `Probe.Web` processes; kill them (README) and poll `docker ps`
  until empty before the next start, or ports clash. A second AppHost (the old `spike-aspire-probe`
  one) holds ports 5171/5172: `aspire stop --all` first, and `aspire ps` to check.
- Don't pipe `aspire start` through `tail`/`head` from an agent shell; it left a hung shell once.
- Grafana MCP `query_prometheus` instant queries need `endTime: "now"`.
- Grafana MCP `--allowed-hosts` is tied to host port 8091.
- Each failing web request becomes 4 `probe-api` calls (resilience retries); the expected counts in
  `load.mjs` output account for this.
