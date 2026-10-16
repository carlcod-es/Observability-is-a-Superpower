# metrics-mcp spike

Throwaway spike: can a coding agent read logs, traces and metrics through one MCP server? Aspire MCP has no metrics tool. The AppHost runs the `../aspire-probe` projects (`Probe.Api`, `Probe.Web`, SQL Server container, unchanged) and fans telemetry out through an OpenTelemetry Collector to the Aspire dashboard plus one candidate backend (Grafana LGTM) queried through one MCP server (Grafana MCP) for logs, traces and metrics. Scope and questions: [`proposal.md`](../../openspec/changes/archive/2026-10-15-spike-metrics-mcp/proposal.md); design: [`design.md`](../../openspec/changes/archive/2026-10-15-spike-metrics-mcp/design.md).

## Prerequisites

- Docker (running)
- Aspire CLI 13.6.1
- .NET 10 SDK
- Node.js 20+ (load script only)
- Windows (untested): Docker Desktop must use Linux containers; trust the HTTPS dev certificate (`dotnet dev-certs https --trust`).

## Run

From `spikes/metrics-mcp/`. The `Spike:Candidates` key selects the backend; `b` is the default. The value is case-insensitive.

| Mode | Command | Starts |
|---|---|---|
| `none` | `aspire run -- --Spike:Candidates=none` | probe + Collector (baseline) |
| `b` | `aspire run` (or `-- --Spike:Candidates=b`) | + Grafana LGTM, Grafana MCP |

`aspire start` takes the same arguments and runs the stack detached (stop with `aspire stop`).

Any other value (including the removed `a` and `both`) stops start-up with `Invalid Spike:Candidates value '<x>'. Valid values: none, b.`

Environment variable instead of the argument (untested on Windows):

```sh
# bash / zsh
Spike__Candidates=none aspire run
```

```powershell
# PowerShell
$env:Spike__Candidates = 'none'; aspire run
# Clear the variable afterwards, or it stays set in this shell session
Remove-Item Env:Spike__Candidates
```

`aspire run` prints the dashboard login link with token. Dashboard: `https://localhost:17410`.

## Resources per mode

| Resource | Image:tag | Host port | URL | none | b |
|---|---|---|---|---|---|
| `sql` (+ database `probe`) | `mcr.microsoft.com/mssql/server:2022-latest` | dynamic | - | x | x |
| `probe-api` | project | 5171 (http), 7171 (https) | `https://localhost:7171` | x | x |
| `probe-web` | project | 5172 (http), 7172 (https) | `https://localhost:7172` | x | x |
| `collector` | `ghcr.io/open-telemetry/opentelemetry-collector-releases/opentelemetry-collector-contrib:0.162.0` | dynamic | - (OTLP receiver; apps are re-pointed to it) | x | x |
| `lgtm` | `grafana/otel-lgtm:0.36.0` | 3000 (Grafana); OTLP/HTTP dynamic (used container-to-container only) | `http://localhost:3000` | | x |
| `grafana-mcp` | `grafana/mcp-grafana:2.0.2` | 8091 | `http://localhost:8091/mcp` | | x |

Dashboard (`https://localhost:17410`) is always on and receives all signals in both modes.

Grafana login: `admin` / `admin` (LGTM default).

## Load script

From `spikes/metrics-mcp/`, with the stack running:

```sh
node load.mjs [baseUrl]
```

`baseUrl` defaults to `http://localhost:5172` (`probe-web`, http). Sends 20 normal, 5 slow and 5 failing requests to `probe-web`. Resilience retries turn each failing web request into 4 `probe-api` calls, so a fresh start shows 45 `probe-api` requests (25 x 200, 20 x 500). Node 20+, no dependencies.

## Dashboards

Mode `b` only. Grafana: `http://localhost:3000`, login in [Resources per mode](#resources-per-mode). Dashboards live in the `lgtm` container and are lost on every AppHost restart; import them again after each start.

| File in `dashboards/` | Dashboard |
|---|---|
| `probe-basic.json` | Probe - basic: request rate, 5xx error rate, request p95 (with trace exemplars), `probe_visits_total` rate, `probe_visit_duration_milliseconds` p95, Loki error logs |
| `aspnetcore-19924.json` | ASP.NET Core (grafana.com 19924, unchanged) |
| `aspnetcore-endpoint-19925.json` | ASP.NET Core Endpoint (grafana.com 19925, unchanged) |

Import each file:

1. Grafana > Dashboards > New > Import.
2. Upload the JSON file.
3. For the two ASP.NET Core files, select `Prometheus` as the datasource.
4. Import (choose overwrite if the dashboard already exists).

Run `node load.mjs` or open `probe-web` a few times; the panels cover the last 15 minutes (probe-basic) or 5 minutes (ASP.NET Core).

## Stop

`aspire stop` (or Ctrl+C for `aspire run`; `aspire stop --all` if another AppHost holds ports 5171/5172). `Probe.Api` / `Probe.Web` processes can survive; check, then confirm containers are gone:

```sh
# macOS / Linux
pgrep -fl "Probe\.(Api|Web)"
pkill -f "Probe\.(Api|Web)"
docker ps
```

```powershell
# Windows (untested)
Get-Process Probe.Api,Probe.Web -ErrorAction SilentlyContinue | Stop-Process
docker ps
```

## Notes

- Local only. Grafana `admin`/`admin` is in `AppHost.cs`; do not copy the pattern.
- The Grafana MCP endpoint sits on a host port with no auth.
- Grafana MCP runs with `--allowed-hosts localhost:8091,127.0.0.1:8091`; change it if host port 8091 changes.
- Grafana MCP `--disable-write` leaves `grafana_api_request`: rejects non-GET at runtime (POST and DELETE rejected); still reaches any Grafana GET, e.g. `/api/users`. `--disable-api` removes it (hardening step).
- `.mcp.json` lists `aspire` and `grafana`; in mode `none` the `grafana` server shows as failed in Claude Code.
- Port 3000 (Grafana) often clashes with Node dev servers; change it in `AppHost.cs` if needed.
