# Findings: spike-metrics-mcp

Aspire CLI/AppHost 13.6.1, macOS, run 2026-10-12. Spike: `spikes/metrics-mcp/` (README there).
Agent transcripts: `agent-answers.md` (PO-captured, task 3.2).

## Set-up as built (tasks 1.1-1.6)

| Resource | Image | Host port | Mode |
|---|---|---|---|
| collector | `ghcr.io/open-telemetry/opentelemetry-collector-releases/opentelemetry-collector-contrib:0.162.0` (toolkit default registry, not Docker Hub `otel/...`) | dynamic | all |
| prometheus | `prom/prometheus:v3.15.0`, `--web.enable-otlp-receiver` | 9090 | a, both |
| prometheus-mcp | `ghcr.io/pab1it0/prometheus-mcp-server:1.6.2` | 8090 (`/mcp`) | a, both |
| lgtm | `grafana/otel-lgtm:0.36.0` | 3000 (Grafana) | b, both |
| grafana-mcp | `grafana/mcp-grafana:2.0.2`, `--disable-write` | 8091 (`/mcp`) | b, both |

- Fan-out: `CommunityToolkit.Aspire.Hosting.OpenTelemetryCollector` 13.4.0 works with Aspire 13.6.1.
  `WithAppForwarding()` re-points the apps at the Collector; the toolkit passes the dashboard
  endpoint and API key to the Collector. No app or ServiceDefaults change.
- Per-mode Collector config: base `config.yaml` (dashboard pipelines) plus `config-a.yaml` /
  `config-b.yaml` via extra `WithConfig` calls; each adds uniquely named pipelines.
- Backends reached by Aspire resource reference over the container network (no
  `host.docker.internal`). Collector → dashboard needs `insecure_skip_verify` (dev certificate).
- Prometheus MCP supports streamable HTTP (`PROMETHEUS_MCP_SERVER_TRANSPORT=http`): no stdio fallback
  needed (design D4 open question closed).
- Grafana MCP over HTTP rejects host requests (`403 host not allowed`) unless
  `--allowed-hosts localhost:8091,127.0.0.1:8091` is set.
- `WithArgs` replaces the Prometheus image CMD, so `--config.file` must be passed explicitly.
- Prometheus MCP project: MIT, maintainer `pab1it0`, ~520 stars, releases every 2-3 months
  (v1.6.2 2026-08-03), active 2026-10-11.

## Agent configuration (tasks 2.1, 2.2)

`spikes/metrics-mcp/.mcp.json`: `aspire` (stdio), `prometheus` (`http://localhost:8090/mcp`),
`grafana` (`http://localhost:8091/mcp`). `claude mcp list` from `spikes/metrics-mcp/` with mode
`both` running: all three Connected.

### Tool lists (`tools/list`, 2026-10-12)

Prometheus MCP (6), all `readOnlyHint: true`, `destructiveHint: false`:
`health_check`, `execute_query`, `execute_range_query`, `list_metrics`, `get_metric_metadata`,
`get_targets`. No write tools exist; no restricting flag needed (tool subset selectable by env).

Grafana MCP (62 with `--disable-write`), all `readOnlyHint: true`:
`alerting_manage_routing`, `alerting_rules_read`, `alerting_silences_read`, `analyze_loki_labels`,
`check_datasources_health`, `diff_tempo_traces`, `generate_deeplink`, `get_alert_group`,
`get_annotation_tags`, `get_annotations`, `get_assertions`, `get_current_oncall_users`,
`get_dashboard_by_uid`, `get_dashboard_panel_queries`, `get_dashboard_property`,
`get_dashboard_summary`, `get_datasource`, `get_doc`, `get_incident`, `get_oncall_shift`,
`get_panel_image`, `get_plugin`, `get_snapshot`, `get_tempo_trace`, `get_tempo_traceql_docs`,
`grafana_api_request`, `list_alert_groups`, `list_dashboard_versions`, `list_datasources`,
`list_incident_custom_fields`, `list_incidents`, `list_loki_label_names`, `list_loki_label_values`,
`list_oncall_schedules`, `list_oncall_teams`, `list_oncall_users`, `list_prometheus_label_names`,
`list_prometheus_label_values`, `list_prometheus_metric_metadata`, `list_prometheus_metric_names`,
`list_provisioning_repositories`, `list_pyroscope_label_names`, `list_pyroscope_label_values`,
`list_pyroscope_profile_types`, `list_snapshots`, `list_tempo_attribute_names`,
`list_tempo_attribute_values`, `query_loki_logs`, `query_loki_patterns`, `query_loki_stats`,
`query_prometheus`, `query_prometheus_histogram`, `query_pyroscope`, `query_tempo_metrics`,
`search_dashboards`, `search_docs`, `search_folders`, `search_plugin_information`,
`search_tempo_traces`, `suggest_loki_alloy_label_config`, `user_info`, `validate_provisioning_file`.

### Read vs write, restricting flags (proposal question 4)

| Server | Write capability | Restriction | Tested |
|---|---|---|---|
| Prometheus MCP | none (PromQL API is read-only) | n/a | annotations only |
| Grafana MCP | dashboards, annotations, alerts, folders etc. without flags | `--disable-write` removes write tools and SQL/Influx query tools | yes |
| Grafana MCP `grafana_api_request` | generic Grafana HTTP API | under `--disable-write` schema allows `GET` only; `--disable-api` removes the tool | yes |

`grafana_api_request` runtime test (mode `both`):
- `POST /api/folders` → `method POST is not allowed in read-only mode; only GET requests are permitted`.
- `DELETE /api/datasources/uid/loki` → same rejection; datasources unchanged afterwards.
- `GET /api/users` → returned the admin user (login, email, isAdmin). `GET /api/datasources` →
  datasource URLs. Read-only, but reaches any Grafana read endpoint, not only telemetry.

Consequences for access control:
- Both servers can be allow-listed per tool in `.claude/settings.json` like the Aspire MCP tools
  (ADR-0011); all tools are read-only by annotation.
- Grafana MCP's default surface (62 tools incl. OnCall, incidents, dashboards, users via the API
  tool) is much wider than "read telemetry". Tightening: `--disable-api`, and limit tool categories
  (to verify: `--disable-<category>` / enabled-tools flags in 2.0.2) to Prometheus, Loki, Tempo.
- MCP host ports have no auth (local only).

## Load script (task 3.1)

`node load.mjs` (base `http://localhost:5172`), mode `both`, 2026-10-11T23:22:06Z-23:22:52Z UTC:
normal 20 (0.5 s), slow 5 (10.1 s), fail 5 (36.1 s; all 5 show the web error message, web status 200).
probe-api `http_server_request_duration_seconds_count` delta in Prometheus: 25 × 200, 20 × 500 = 45,
matching the expected 20 + 5 + 5 × 4 (resilience handler retries). Dashboard count: PO to confirm.

## Agent reads metrics (task 3.2)

Pending.

## Freshness (task 3.3)

Pending.

## Local developer experience (task 3.4)

Pending. Early observation (block 2, `docker stats --no-stream` after start-up): collector 62-109 MiB,
prometheus 47 MiB, prometheus-mcp 74 MiB, lgtm 607-619 MiB, grafana-mcp 19-22 MiB, sql ~1.07 GiB.
`aspire stop` leaves `Probe.Api` / `Probe.Web` processes running (also seen in `spike-aspire-probe`).

## Talk fit and recommendation (task 3.5)

Pending. Metric names as seen in Prometheus: `probe.visits` → `probe_visits_total`;
`probe.visit.duration` (ms) → `probe_visit_duration_milliseconds_{bucket,count,sum}`;
`http.server.request.duration` → `http_server_request_duration_seconds_{bucket,count,sum}`;
`service.name` → `job`. Via LGTM, `service_name` / `service_instance_id` labels are also present.
Grafana MCP `query_prometheus` instant query needs `endTime: "now"`.
