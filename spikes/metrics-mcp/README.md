# metrics-mcp spike

Throwaway spike: can a coding agent read metrics from a local, queryable store? Aspire MCP has no metrics tool. The AppHost runs the `../aspire-probe` projects (`Probe.Api`, `Probe.Web`, SQL Server container, unchanged) and fans telemetry out through an OpenTelemetry Collector to the Aspire dashboard plus two candidate backends, each with an MCP server. Scope and questions: [`proposal.md`](../../openspec/changes/spike-metrics-mcp/proposal.md); design: [`design.md`](../../openspec/changes/spike-metrics-mcp/design.md).

## Prerequisites

- Docker (running)
- Aspire CLI 13.6.1
- .NET 10 SDK
- Node.js 20+ (load script only)

## Run

From `spikes/metrics-mcp/`. The `Spike:Candidates` key selects the backends; `both` is the default.

| Mode | Command | Starts |
|---|---|---|
| `none` | `aspire run -- --Spike:Candidates=none` | probe + Collector (baseline) |
| `a` | `aspire run -- --Spike:Candidates=a` | + Prometheus, Prometheus MCP |
| `b` | `aspire run -- --Spike:Candidates=b` | + Grafana LGTM, Grafana MCP |
| `both` | `aspire run` (or `-- --Spike:Candidates=both`) | all of the above |

Any other value stops start-up with `Invalid Spike:Candidates value '<x>'. Valid values: none, a, b, both.`

Environment variable instead of the argument:

```sh
# bash / zsh
Spike__Candidates=a aspire run
```

```powershell
# PowerShell
$env:Spike__Candidates = 'a'; aspire run
Remove-Item Env:Spike__Candidates
```

`aspire run` prints the dashboard login link with token. Dashboard: `https://localhost:17410`.

## Resources per mode

| Resource | Image:tag | Host port | URL | none | a | b | both |
|---|---|---|---|---|---|---|---|
| `sql` (+ database `probe`) | `mcr.microsoft.com/mssql/server:2022-latest` | dynamic | - | x | x | x | x |
| `probe-api` | project | 5171 (http), 7171 (https) | `https://localhost:7171` | x | x | x | x |
| `probe-web` | project | 5172 (http), 7172 (https) | `https://localhost:7172` | x | x | x | x |
| `collector` | `ghcr.io/open-telemetry/opentelemetry-collector-releases/opentelemetry-collector-contrib:0.162.0` | dynamic | - (OTLP receiver; apps are re-pointed to it) | x | x | x | x |
| `prometheus` | `prom/prometheus:v3.15.0` | 9090 | `http://localhost:9090` | | x | | x |
| `prometheus-mcp` | `ghcr.io/pab1it0/prometheus-mcp-server:1.6.2` | 8090 | `http://localhost:8090/mcp` | | x | | x |
| `lgtm` | `grafana/otel-lgtm:0.36.0` | 3000 (Grafana); OTLP/HTTP dynamic (used container-to-container only) | `http://localhost:3000` | | | x | x |
| `grafana-mcp` | `grafana/mcp-grafana:2.0.2` | 8091 | `http://localhost:8091/mcp` | | | x | x |

Dashboard (`https://localhost:17410`) is always on and receives all signals in every mode.

Grafana login: `admin` / `admin` (LGTM default).

## Load script

Added in task 3.1. From `spikes/metrics-mcp/`:

```sh
node load.mjs
```

Sends 20 normal, 5 slow and 5 failing requests to `probe-web`. Node 20+, no dependencies.

## Stop

`aspire stop` (or Ctrl+C for `aspire run`). `Probe.Api` / `Probe.Web` processes can survive; check, then confirm containers are gone:

```sh
# macOS / Linux
pgrep -fl "Probe\.(Api|Web)"
pkill -f "Probe\.(Api|Web)"
docker ps
```

```powershell
# Windows
Get-Process Probe.Api,Probe.Web -ErrorAction SilentlyContinue | Stop-Process
docker ps
```

## Notes

- Local only. Grafana `admin`/`admin` is in `AppHost.cs`; do not copy the pattern.
- MCP endpoints sit on host ports with no auth (the Prometheus MCP server is fully unauthenticated).
- Grafana MCP runs with `--allowed-hosts localhost:8091,127.0.0.1:8091`; change it if host port 8091 changes.
- Grafana MCP `--disable-write` leaves `grafana_api_request` (GET-only in read-only mode per the binary; runtime confirmation pending in task 2.2). `--disable-api` removes it.
- Prometheus names OTLP metrics with `_` and unit/`_total` suffixes: `probe.visits` becomes `probe_visits_total`, `http.server.request.duration` becomes `http_server_request_duration_seconds_*`.
- `.mcp.json` (added in task 2.1) lists `prometheus` and `grafana`; servers of a candidate that is not running show as failed in Claude Code.
