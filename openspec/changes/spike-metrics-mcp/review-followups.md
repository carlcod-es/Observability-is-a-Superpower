# Review follow-ups: spike-metrics-mcp

Minors and nits from block reviews. Read at the start of every apply session.

## Block 1 (tasks 1.1-1.2), reviewer APPROVED 2026-10-11
- Nit `spikes/metrics-mcp/collector/config.yaml:1-2`: header comment long. No change.
- Nit `spikes/metrics-mcp/Metrics.AppHost/AppHost.cs`: Collector image is the toolkit default
  `ghcr.io/open-telemetry/opentelemetry-collector-releases/opentelemetry-collector-contrib:0.162.0`,
  not Docker Hub `otel/opentelemetry-collector-contrib` (design D1-D3 list). Same project. Record in
  `findings.md` (task 3.4) and tell the PO.
- Minor (environment): stale worker shell left after `aspire start | tail`. Killed by the Architect.
  Next briefs: run `aspire start` without piping, and check `aspire ps` / `docker ps` at the end.
- Open for task 1.2: metrics reaching the dashboard seen only via a temporary Collector debug
  exporter. PO confirms `probe.visits` / `probe.visit.duration` on the dashboard Metrics page
  before 1.2 is ticked.
- Note for task 1.2 text: probe metrics come from `probe-api` only ("both services" applies to the
  built-in metrics).

## Block 2 (tasks 1.3-1.5), reviewer APPROVED 2026-10-12
- Nit `spikes/metrics-mcp/Metrics.AppHost/AppHost.cs:50-51`: Grafana `admin`/`admin` in code (LGTM
  defaults, design D3). README says local-only, not a pattern to copy.
- Nit `AppHost.cs:35-39`, `:52-54`: MCP endpoints on host ports without auth (Prometheus MCP fully
  unauthenticated). README note.
- Nit `AppHost.cs:40-43`: LGTM `otlp-http` has a dynamic host port (container-to-container only).
  README lists no host port for it.
- Task 2.2: `grafana_api_request` survives `--disable-write`. Binary string suggests GET-only in
  read-only mode; confirm with a POST at runtime. It still allows any Grafana GET (users,
  datasources). `--disable-api` removes it; record as the hardening step (talk promise:
  controlled, read-only access).
- Task 3.2: Grafana MCP `query_prometheus` needs `endTime: "now"` for instant queries, else
  "parsing end time: syntax error".
- Evidence for 1.3/1.4 came via the Prometheus and Grafana HTTP APIs, not the UIs. Accepted.
- Design D4 open question answered: Prometheus MCP supports streamable HTTP
  (`PROMETHEUS_MCP_SERVER_TRANSPORT=http`); no stdio fallback.

## Block 3 (task 1.6), reviewer APPROVED 2026-10-12
- Minor `spikes/metrics-mcp/README.md:91`, `:59`: "added in task 2.1/3.1" stale; `.mcp.json` and
  `load.mjs` exist. Drop the task references.
- Minor `README.md:61-65`: load section lacks `node load.mjs [baseUrl]`, default
  `http://localhost:5172`, the 45 expected probe-api requests (retries), and "stack must be running".
- Nit `README.md:38`: mention `aspire start` as the detached variant.
- Nit `README.md:25-35`: env-var / PowerShell commands untested; comment the `Remove-Item` line.
