## Context

- Probe from `spike-aspire-probe`: `spikes/aspire-probe/` (`Probe.Web` → `Probe.Api` → SQL Server
  container), Aspire CLI/AppHost 13.6.1, reuses `src/SuperPower.ServiceDefaults`. `Probe.Api`
  emits `probe.visits` (counter), `probe.visit.duration` (histogram, ms) and SqlClient
  metrics; ASP.NET Core, HttpClient and runtime metrics come from ServiceDefaults.
- ServiceDefaults exports via `UseOtlpExporter()` when `OTEL_EXPORTER_OTLP_ENDPOINT` is set
  (ADR-0008). Aspire injects that endpoint and the dashboard's OTLP API key header into each
  project, so every signal goes only to the dashboard.
- Aspire MCP has no metrics tool, and neither do `aspire otel` or `aspire export`.
  `spike-aspire-probe` `findings.md` Q2, ADR-0011.
- Docker 29 is available locally. Python is not assumed, so MCP servers run as containers.

## Goals / Non-Goals

**Goals:**
- Get telemetry to a second, local, queryable metrics store without changing app code or
  ServiceDefaults, and keep the Aspire dashboard complete.
- Compare two candidate stacks: a minimal one (metrics only) and a full one (metrics, logs and
  traces behind one MCP server).
- Everything starts with `aspire run`. No separate `docker compose`.

**Non-Goals:**
- Choosing the demo's design. Adoption gets its own ADR after PO sign-off.
- Dashboards, alerting, retention, persistent volumes.
- Hardening, auth beyond defaults, Linux/Windows runs.

## Decisions

### D1. Fan out through an OpenTelemetry Collector in the AppHost
Apps → Collector (container) → Aspire dashboard (all signals) + candidate backends.
Use `CommunityToolkit.Aspire.Hosting.OpenTelemetryCollector` 13.4.0: `AddOpenTelemetryCollector`
+ `WithAppForwarding()` re-points every project's OTLP endpoint at the Collector and wires
the Collector to the dashboard, including its API key. The spike adds the extra exporters
through `WithConfig(...)`. This is a new package: PO approved 2026-10-11.

Alternatives:
- Hand-wire a Collector with `AddContainer` and set each project's OTLP environment variables:
  no new package, but the dashboard endpoint, API key and TLS wiring is fiddly. This is the
  fallback if the toolkit fails with Aspire 13.6.1.
- A second exporter in ServiceDefaults: changes the shared set-up and ADR-0008's exporter
  selection. Rejected for the spike (proposal: out of scope).
- Prometheus scrapes each app's `/metrics`: needs the prerelease
  `OpenTelemetry.Exporter.Prometheus.AspNetCore` and app code changes. Rejected.

Images in D1-D3 (`otel/opentelemetry-collector-contrib`, `prom/prometheus`,
`ghcr.io/pab1it0/prometheus-mcp-server`, `grafana/otel-lgtm`, `grafana/mcp-grafana`): PO approved
2026-10-11. The worker pins exact tags and records them in the findings.

### D2. Candidate A: Prometheus + Prometheus MCP server
- `prom/prometheus` (pinned 3.x tag) with `--web.enable-otlp-receiver`. The Collector sends
  metrics only to `/api/v1/otlp` (`otlphttp` exporter). Host port 9090.
- `ghcr.io/pab1it0/prometheus-mcp-server` (pinned tag): PromQL instant/range queries, metric
  list and metadata, all read-only by nature. Community project: record its licence and
  maintainer in the findings.
- Why: smallest footprint, plain PromQL, widely documented. Metrics only.

### D3. Candidate B: Grafana LGTM + Grafana MCP server
- `grafana/otel-lgtm` (pinned tag): one container with an OTel Collector, Prometheus, Loki,
  Tempo and Grafana. The Collector sends all three signals to it over OTLP. Grafana on host
  port 3000.
- `grafana/mcp-grafana` (pinned tag) with `--disable-write`. Default tool sets include
  Prometheus (`query_prometheus`, `query_prometheus_histogram`, `list_prometheus_metric_names`,
  ...), Loki and Tempo. Auth: `GRAFANA_USERNAME`/`GRAFANA_PASSWORD` (LGTM defaults) or a
  service-account token.
- Why: official vendor MCP server, a read-only switch, and one server for all signals. It
  also shows whether the agent could work from it alone. Heaviest option.

Candidates considered and rejected:
- Application Insights from local dev + Azure MCP Server: cloud dependency, minutes of
  ingestion lag, and dev telemetry leaves the machine. It stays the production story and is
  the comparison point for proposal question 5.
- Self-hosted SigNoz (has an MCP server): ClickHouse-based, too heavy for a laptop demo next to
  Umbraco and SQL Server.
- Hosted services (Grafana Cloud, Honeycomb, Datadog, SigNoz Cloud): out of scope (accounts,
  data leaves the machine).
- Custom MCP server over a metrics store: build cost, and the question is about existing tools.
- An Aspire dashboard metrics API: none exists in 13.6 (`spike-aspire-probe` `findings.md`).

### D4. MCP servers run as Aspire container resources over HTTP
Each MCP server is an AppHost container resource with streamable-HTTP transport on a fixed
host port (Prometheus MCP 8090, Grafana MCP 8091). It reaches its backend by Aspire
resource name on the container network. `.mcp.json` lists them as `http` servers next to
`aspire` (stdio).

- Why: the AppHost owns the MCP servers' lifecycle. Endpoints resolve inside the
  container network, so there are no `host.docker.internal` differences across OSes. The
  servers show as resources in the dashboard.
- Alternative: `docker run -i --rm` stdio from `.mcp.json`, reaching backends via host ports.
  Common in READMEs, but the container outlives or precedes the AppHost and Linux needs
  `--add-host`. Fallback if a server has no HTTP transport. The findings record which form
  was used.

### D5. Layout and reuse
- `spikes/metrics-mcp/` with its own `MetricsMcp.slnx`, `Metrics.AppHost`, `collector/config.yaml`,
  `aspire.config.json`, `.mcp.json` and a load script.
- `Metrics.AppHost` references `spikes/aspire-probe/Probe.Api` and `Probe.Web`, but does not
  edit them, so `spike-aspire-probe` stays reproducible. The SQL resource is defined as in
  the probe AppHost.
- All images pinned to exact tags (resolved by the worker at build time and recorded in the
  findings). No `latest`.

### D7. Candidate switch in the AppHost
One AppHost, one folder. The configuration key `Spike:Candidates` takes `none`, `a`, `b` or
`both` (default `both`; any other value fails start-up with a message listing the valid
values). The value is read from `builder.Configuration` while the model is built, and the
AppHost adds only the selected backends, their MCP servers and their Collector exporters.
`none` still runs the Collector, so the baseline differs from a candidate run only by that
candidate.

- Set on the command line: `aspire run -- --Spike:Candidates=a` (or `aspire start -- ...`).
  Arguments after `--` go to the AppHost (aspire.dev `aspire run` reference), and
  `DistributedApplication.CreateBuilder(args)` adds them to `builder.Configuration`. Verified
  2026-10-11 with Aspire CLI 13.6.1 and a throwaway AppHost: `aspire run -- --Spike:Candidates=a`
  gave `a`, `aspire start -- --Spike:Candidates=both` gave `both`, and the environment variable
  `Spike__Candidates=b` gave `b`. The command-line form is primary (same on every OS); the
  README mentions the environment variable as an alternative.
- `spikes/metrics-mcp/README.md` explains how to run each mode: the commands (bash/zsh and
  PowerShell for the environment-variable form), which resources and ports start in each
  mode, the URLs (dashboard, Prometheus, Grafana), the default credentials, and the fact
  that `.mcp.json` servers of a candidate that is not running show as failed in Claude Code.
  PO request 2026-10-11; this replaces the "no README" part of the throwaway bar for this
  spike.
- Alternatives: two folders, each with its own AppHost, which duplicates the AppHost and
  Collector config. An Aspire parameter (`AddParameter`), whose value is resolved only at
  run time, too late to decide which resources exist. Both rejected.

### D6. Investigation method
Same as `spike-aspire-probe`. A load script sends a fixed mix of normal, slow (`slow=2000`)
and failing requests. A fresh Claude Code session per candidate gets fixed asks, with only
that candidate's MCP server allowed. The PO captures transcripts. The Architect cross-checks
the numbers against the dashboard Metrics page and the backend UI (Prometheus / Grafana
Explore).

### Bar and ADRs
- Bar: throwaway. No tests; every task is verified by observation and recorded in
  `findings.md`. Exception: the spike README (D7).
- No ADR in this change. The image and package choices are scoped to the spike. If the PO
  adopts a candidate, the Architect writes a new ADR (or revises ADR-0011) and plans the
  demo work in `add-demo-foundation`.

## Risks / Trade-offs

- [Toolkit 13.4.0 built against an older Aspire than 13.6.1] → Fallback to a hand-wired
  Collector (D1).
- [Collector → dashboard OTLP over HTTPS with the dev certificate fails] → The toolkit is
  expected to handle it. Otherwise use the dashboard's HTTP OTLP endpoint, or
  `insecure_skip_verify` in the spike config only.
- [OTel → Prometheus name translation: `probe.visits` becomes `probe_visits_total`, units are
  suffixed, `service.name` becomes `job`] → The agent has to discover the names, so record
  how well it does (proposal question 5).
- [Metric freshness: export interval + Prometheus ingestion] → Measured (proposal question 2).
  Aspire sets a short export interval in development; the Collector adds its batch delay.
- [Memory: LGTM is heavy next to SQL Server; Umbraco adds more in the demo] → Measured with
  `docker stats` (proposal question 3).
- [Community MCP server (candidate A) trust and maintenance] → Licence, maintainer and last
  release recorded. The PO weighs it in the recommendation.
- [Fixed host ports clash with local services] → Spike only. Change the ports if needed and
  record it.
- [Grafana MCP auth with LGTM defaults] → Basic auth from defaults. A service-account token
  is the fallback.

## Open Questions

- Does `pab1it0/prometheus-mcp-server` support streamable HTTP in its current release? If
  not, use the stdio fallback (D4). Answered in task 1.5; the task breakdown stays the same.
