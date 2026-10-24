# ADR-0017: Grafana LGTM and Grafana MCP for agent telemetry

## Status
Accepted (2026-10-13). Partly superseded by ADR-0022 and ADR-0033 (Aspire MCP as an opt-in second server); Collector, LGTM and Grafana MCP versions and flags stand.

## Context
The talk promises the agent reads logs, traces and metrics via MCP. The Aspire MCP has no
metrics tool. Tested: Grafana LGTM fed by an OpenTelemetry Collector, queried through the
Grafana MCP, answers metric, log and trace questions correctly with 0.3-1.2 s freshness, and its
tool surface can be narrowed to read-only telemetry. One MCP server keeps it simple.

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
  annotation).
- No database MCP server or connection string for the agent.

## Alternatives
- Prometheus + Prometheus MCP: metrics only; logs and traces would still
  need another server; rejected.
- Aspire MCP alone: no metrics tool, and the talk promises metric-based
  verification; rejected.
- Grafana MCP alongside the Aspire MCP: keeps resource state, console logs and commands for the
  agent; one server is a simpler story, and the presenter runs commands in the dashboard;
  rejected (later made opt-in, ADR-0033).

## Consequences
- The agent diagnoses and verifies from telemetry only; start-up failures that produce no
  telemetry are read by the presenter in the dashboard.
- Cost (measured): about +2 s start-up, +0.7-0.8 GiB RAM, ~2.8 GB extra images (~4.8 GB
  first pull in total) — the README gets an attendee pre-pull step; fixed host ports 3000
  (often clashes with Node dev servers) and 8091.
- OTel names are translated in Prometheus/Loki (`acme.forms.submissions` → `acme_forms_submissions_total`,
  `service.name` → `service_name`); demo prompts should hint: counters over a window covering
  the run, prefer `query_prometheus_histogram`, instant `query_prometheus` needs
  `endTime: "now"`, the first request is a cold start.
- Measured on macOS; Windows verified later.
