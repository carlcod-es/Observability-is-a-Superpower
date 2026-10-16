# ADR-0017: Grafana LGTM and Grafana MCP for agent telemetry

## Status
Accepted (2026-10-13). Supersedes ADR-0011. Partly superseded by ADR-0022 (2026-10-15, change
`rework-aspire-first`): two MCP servers (Aspire MCP for the local stage, Grafana MCP for the
production stage) instead of one, and LGTM plus Grafana MCP started on demand instead of from
start-up. Collector, LGTM and Grafana MCP versions and flags stand.

## Context
The talk promises the agent reads logs, traces and metrics via MCP. The Aspire MCP has no
metrics tool (ADR-0011); `spike-aspire-probe` confirmed none exists. `spike-metrics-mcp`
(findings, task 3.5) verified: Grafana LGTM fed by an OpenTelemetry Collector, queried through
the Grafana MCP, answers metric, log and trace questions correctly with 0.3-1.2 s freshness,
and its tool surface can be narrowed to read-only telemetry. The Architect recommended running
it alongside the Aspire MCP; the PO chose replacement (2026-10-13): one MCP server, keep it
simple; the presenter runs Toxiproxy fault commands in the dashboard.

## Decision
- The AppHost adds an OpenTelemetry Collector via
  `CommunityToolkit.Aspire.Hosting.OpenTelemetryCollector` (13.4.0, collector image
  `opentelemetry-collector-contrib:0.162.0`) with `WithAppForwarding()`:
  apps and ServiceDefaults unchanged; the Collector fans out to the Aspire dashboard and to
  `grafana/otel-lgtm:0.36.0` (Grafana on fixed host port 3000).
- `grafana/mcp-grafana:2.0.2` container on fixed host port 8091 with
  `--disable-write --enabled-tools prometheus,loki,tempo,datasource
  --allowed-hosts localhost:8091,127.0.0.1:8091`: 22 read-only telemetry tools (PromQL, LogQL,
  TraceQL, datasource health); no dashboards, users, OnCall, incidents or generic API tool.
- Committed `.mcp.json` has one server: `grafana` (streamable HTTP,
  `http://localhost:8091/mcp`). No Aspire MCP: the agent gets no resource state, console logs
  or commands.
- Committed `.claude/settings.json` pre-approves the Grafana MCP tools (all read-only by
  annotation), satisfying spec `agent-telemetry-access` "Read query".
- No database MCP server or connection string for the agent (carried over from ADR-0011).

## Alternatives
- Prometheus + Prometheus MCP (spike candidate A): metrics only; logs and traces would still
  need another server; rejected.
- Aspire MCP alone (ADR-0011): no metrics tool, and the talk promises metric-based
  verification; rejected.
- Grafana MCP alongside the Aspire MCP (Architect recommendation, `spike-metrics-mcp` findings
  task 3.5): keeps resource state, console logs and commands for the agent; PO overrode
  2026-10-13 — one server is a simpler story, and the presenter runs commands in the dashboard.

## Consequences
- The agent diagnoses and verifies from telemetry only; start-up failures that produce no
  telemetry are read by the presenter in the dashboard.
- Cost (spike task 3.4): about +2 s start-up, +0.7-0.8 GiB RAM, ~2.8 GB extra images (~4.8 GB
  first pull in total) — the README gets an attendee pre-pull step; fixed host ports 3000
  (often clashes with Node dev servers) and 8091.
- OTel names are translated in Prometheus/Loki (`probe.visits` → `probe_visits_total`,
  `service.name` → `service_name`); demo prompts should hint: counters over a window covering
  the run, prefer `query_prometheus_histogram`, instant `query_prometheus` needs
  `endTime: "now"`, the first request is a cold start.
- Windows run unverified (the spike ran on macOS); verified during apply.
