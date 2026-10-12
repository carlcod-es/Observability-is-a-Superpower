# Findings: spike-metrics-mcp

Aspire CLI/AppHost 13.6.1, macOS, run 2026-10-12. Spike: `spikes/metrics-mcp/` (README there).
Agent transcripts: `agent-answers.md` (PO-captured, task 3.2).

Scope decision 2026-10-12 (PO): only candidate B (Grafana LGTM + Grafana MCP) is tested, for a
single MCP server covering logs, traces and metrics. Candidate A (Prometheus + Prometheus MCP)
was built and verified in tasks 1.3, 1.5 and 2.2; those sections below stay as a record. Later
sections (3.2-3.5) cover B only.

## Set-up after task 1.7 (modes `none`, `b`; default `b`)

Candidate A removed (Prometheus, Prometheus MCP, `config-a.yaml`, port 8090, `.mcp.json` entry).
Block reviewer APPROVED 2026-10-12; section-1 supervisor review 2026-10-12.
Verification (worker, from `spikes/metrics-mcp/`; build re-checked by the Architect):
- `dotnet build MetricsMcp.slnx`: 0 errors, 8 NU1507 warnings.
- `aspire start -- --Spike:Candidates=a`: `InvalidOperationException: Invalid Spike:Candidates value
  'a'. Valid values: none, b.`
- Mode `none`: `collector`, `probe`, `probe-api` (5171/7171), `probe-web` (5172/7172), `sql`.
- Mode `b` (default): the above plus `lgtm` (Grafana 3000, OTLP dynamic) and `grafana-mcp` (8091).
- `claude mcp list` in mode `b`: `aspire` and `grafana` Connected.
- Nothing left running (`aspire ps`, `docker ps` empty).
Task 1.2 (PO, 2026-10-12): dashboard Metrics page, `probe-api`, shows data for `probe.visits` and
`probe.visit.duration`; `http.server.request.duration` is under meter `Microsoft.AspNetCore.Hosting`.
Accepted deviation: SQL Server uses Aspire's default image tag `mcr.microsoft.com/mssql/server:2022-latest`,
as in the probe AppHost (design D5: "defined as in the probe AppHost" wins over "no `latest`").

## Set-up as built, pre-1.7 record (tasks 1.1-1.6; modes `a`, `b`, `both`)

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
matching the expected 20 + 5 + 5 × 4 (resilience handler retries).
Dashboard (PO, 2026-10-12, mode `b`): `http.server.request.duration` (meter
`Microsoft.AspNetCore.Hosting`, resource `probe-api`) = 90 after two runs, i.e. 45 per run. Matches.

## Agent reads metrics (task 3.2)

Agent run 2026-10-12 (Grafana MCP only; no `aspire` MCP, no source reads). Load run window
~21:28:36-21:29:28 UTC, inferred from trace timing (the pasted `load.mjs` lines were phase
definitions without timestamps). Agent answers below; the Dashboard and Explore columns are for the
PO to fill. The agent did not open the dashboard or Explore (no browser automation requested; the
`aspire` MCP was excluded).

Comparison caveats: the dashboard counters accumulate over all runs since AppHost start (findings
above: 90 after two runs), while LGTM held one run (25 visits, 45 DB inserts); a higher dashboard
total is not a mismatch if it equals a multiple of one run. Use the same time range in Explore as
the run window.

| # | Question | Agent answer | Agent confidence | Dashboard page (PO) | Explore query | Dashboard value | Explore value | Match |
|---|---|---|---|---|---|---|---|---|
| 1 | Metrics per service, grouped | 8 groups (inbound HTTP, outbound HTTP, DB, custom, Kestrel, .NET runtime, process/memory, trace-derived). `probe-api` only: `db_client_*`, `probe_*`, `kestrel_tls_*`, `aspnetcore_diagnostics_exceptions_total` | medium: spanmetrics / service-graph labelling not checked | Metrics, `probe-api` and `probe-web` | `count by (service_name, __name__) ({service_name=~"probe-api\|probe-web"})` | | | |
| 2 | Request / error rate, `probe-api` | 45 calls in ~51 s = ~0.9 req/s; 20 × 500 = ~0.4 req/s, 44 %. Prometheus `increase`: 200 ≈ 14, 500 ≈ 19 | low for 200 (14 vs expected 25, cause unverified) | Metrics, `http.server.request.duration` count by status code | `sum by (http_response_status_code) (http_server_request_duration_seconds_count{service_name="probe-api"})` | | | |
| 3 | p95 `GET /visits`, normal and slow | normal 208.5 ms (14 of 15 spans 3.5-9.0 ms; first request is cold start, p95 without it 9.0 ms); slow 2013.75 ms (5 spans, 2010-2014 ms) | low: only 15 of 20 normal spans found, phase windows inferred | Traces, filter `probe-api` `GET /visits`, durations | `{ resource.service.name="probe-api" && kind=server && name="GET /visits" && status!=error }` | | | |
| 4 | `probe.visits` total | 25 | high | Metrics, `probe.visits` | `probe_visits_total` | | | |
| 5 | p95 `probe.visit.duration` | ~2125 ms (cumulative buckets, 25 obs: 20 ≤ 1000 ms, 25 ≤ 2500 ms); Grafana helper (`rate` 30 m) gave 2290 ms; real ~2.01 s | medium: interpolation, two methods disagree | Metrics, `probe.visit.duration` histogram | `histogram_quantile(0.95, sum by (le) (probe_visit_duration_milliseconds_bucket))` | | | |
| 6 | DB call average / p95 | INSERT 45 calls avg 8.1 ms p95 ~42 ms; SELECT 25 calls avg 2.6 ms p95 ~9 ms; combined avg 6.1 ms p95 ~36 ms | medium: bucket-interpolated p95; sum/count averages exact | Metrics, `db.client.operation.duration` | `sum(db_client_operation_duration_seconds_sum) / sum(db_client_operation_duration_seconds_count)` | | | |
| 7 | Error logs, failing requests, message | 5 failing client requests; message `Probe failure requested` (`InvalidOperationException`); 25 error lines: 20 `probe-api` (4 per request), 5 `probe-web` (Polly "Attempt: 3, Result 500") | high | Structured logs, level Error | `sum by (service_name) (count_over_time({service_name=~"probe-.*"} \| detected_level="error" [2h]))` | | | |
| 8 | Slow request, dominant span | trace `666050830a0680e760f13db92bb2569`: `probe.slow-work` 2000.9 ms of 2016.7 ms (99 %); its parent is the 4 ms `INSERT` span, not `GET /visits` | high; parenting anomaly noted | Traces, that trace ID | trace ID `666050830a0680e760f13db92bb2569` | | | |
| 9 | One failing request across signals | trace `5918af322845b1be6e70535d25cfed74`: 4 error spans in each service, root 200, 5 error log lines, `probe-api` 500 ≈ 14.2 in 21:29-21:30 vs 14 failing spans; agrees | medium: attempt count in the minute reconstructed by hand | Traces + Structured logs (that trace ID) + Metrics (500, 1 min) | trace ID; `{service_name=~"probe-.*"} \| trace_id="5918af322845b1be6e70535d25cfed74"` | | | |
| 10 | Least sure | Q3, Q2, Q6 (insert-on-failure inference), Q5, Q1, Q9 | n/a | n/a | n/a | n/a | n/a | n/a |

Architect cross-check against LGTM (Grafana datasource proxy API, same stack, 2026-10-12; the
Dashboard columns remain with the PO):

| # | Explore / API value | Verdict vs agent |
|---|---|---|
| 2 | `http_server_request_duration_seconds_count{service_name="probe-api"}` by status: 200 = 25, 500 = 20 (total 45) | Totals match expectation. Agent's `increase` 200 ≈ 14 was a short-window artefact, not missing data |
| 3 | Tempo search `{resource.service.name="probe-api" && kind=server && name="GET /visits" && status!=error}`: 25 traces; durations ms 4-11 (19 values), 392 (cold start), 2012 ×3, 2016 ×2 | Slow 2013.75 ms agrees. Agent found only 15 of 20 normal spans (search not paged / limited); true normal p95 is 11 ms nearest-rank, 392 ms is the cold start. Agent's 208.5 ms is an interpolation over a partial set: wrong in kind |
| 4 | `probe_visits_total` = 25 | Match |
| 5 | `histogram_quantile(0.95, sum by (le)(probe_visit_duration_milliseconds_bucket))` = 2125 ms; count 25 | Match (agent's first method); Grafana helper's 2290 ms was the odd one |
| 6 | `sum(db_client_operation_duration_seconds_sum)/sum(..._count)` = 6.106 ms; 70 operations (45 INSERT + 25 SELECT) | Match (combined avg 6.1 ms) |
| 7 | Loki `count_over_time({service_name=~"probe-.*"} \| detected_level="error" [2h])` by service: `probe-api` 20, `probe-web` 5 | Match (25 lines) |
| 8, 9 | Trace `666050830a0680e760f13db92bb2569` resolves in Tempo (the ID has 31 hex digits as the agent printed it; Tempo accepts it) | Spans and parenting not re-read by the Architect; PO to confirm on the dashboard Traces page |
| 1 | not re-run (LGTM label sets) | PO to confirm on the dashboard |

Dashboard cross-check (Architect, Firefox via `playwright-cli` at PO request 2026-10-12; the Firefox
MCP was not available in the session; same stack, run window 22:28-22:30 local = 21:28-21:30 UTC):

| # | Dashboard value | Match |
|---|---|---|
| 1 | Metrics, `probe-api`: lists the agent's groups, incl. `db.client.operation.duration`, `probe.visits`, `probe.visit.duration`, `kestrel.tls_handshake.duration`, `aspnetcore.diagnostics.exceptions`, runtime and HTTP client meters | Match (`probe-web` list not read) |
| 2 | `http.server.request.duration` count, filter 200 = 25, 500 = 20, total 45 | Match to expected; agent's 14 / 19 were wrong |
| 3 | Traces, `probe-web: GET /`: 20 normal traces 4.3-11.3 ms plus first 0.39 s (cold start); 5 slow 2.01-2.02 s | Match to Tempo. All 20 normal traces exist; agent found 15 |
| 4 | `probe.visits` = 25 | Match |
| 5 | `probe.visit.duration`: P50 5 ms, P90 and P99 2500 ms (histogram bucket bounds) | Consistent with agent's ~2125 ms; dashboard reports bucket bounds, not interpolated values |
| 6 | `db.client.operation.duration`: P50 0.005 s, P90 0.05 s, P99 0.5 s (bucket bounds) | Consistent with avg 6.1 ms / p95 ~36 ms |
| 7 | Structured logs, level Error: 20 `probe-api`, 5 `probe-web`; message "An unhandled exception has occurred while executing the request." (`probe-api`), Polly "Attempt: 3, Result 500" (`probe-web`) | Match counts. "Probe failure requested" is in the exception detail, not the list message |
| 8 | Trace `0666050830a0680e760f13db92bb2569` (agent printed it without the leading `0`): `probe.slow-work` 2 s of 2.02 s; `INSERT dbo.visit` 4.1 ms, `SELECT dbo.visit` 6.05 ms | Match on dominant span. Span parenting not read (agent's context-leak point unverified) |
| 9 | Trace `5918af322845b1be6e70535d25cfed74`: root 5.93 s, 4 attempts, each `HTTP GET 500` + `GET /visits` + `INSERT dbo.visit`; logs by trace: `probe-api` Error "An unhandled exception..." and `probe-web` Polly warnings/error | Match to agent's 4 attempts |

Caveat: `agent-answers.md` is the agent's own write-up, not a raw tool-call transcript. "No `aspire` MCP, no source reads" is self-reported and not verifiable from it (no `mcp__*` tool names appear). Accepted by the Architect for a throwaway spike; PO may veto.

Reading: the metric and log answers are exact. The weak spots are statistical, not access: the
agent chose trace sampling and `increase` windows that undercount, and said so in its confidence
column (Q2, Q3). Q5 shows the agent comparing two methods itself.

Open points raised by the agent run (to resolve with the dashboard data):
- Q3: why only 15 of 20 normal requests have a `probe-api` `GET /visits` span; Q5 shows 20 normal
  visits, so the metric and the traces disagree on the normal count.
- Q2: why `probe-api` 200 `increase` (~14) is below the 25 expected.
- Q6: 45 `INSERT` vs 25 `probe.visits`: failed requests appear to insert before failing (a failing
  `GET /visits` trace has an `INSERT dbo.visit` child); whether the row is kept is unchecked.
- Q8: `probe.slow-work` and `SELECT dbo.visit` have the `INSERT` span as parent (context leak).

## Freshness (task 3.3)

Measured 2026-10-12, mode `b`, stack running, by the Architect. Time from sending one `GET http://localhost:5172/` to the change
being queryable (polled every 250 ms; accuracy about +-0.3 s).

Method 1, backend APIs (Grafana datasource proxy: Prometheus, Loki, Tempo). Each request carried a known W3C `traceparent`, so
the trace is fetched by ID and the log is found by `trace_id`; the metric is `sum(probe_visits_total)` rising above its baseline.

| Run | Request took | Trace visible | Log visible | Metric visible |
|---|---|---|---|---|
| 1 | 135 ms (cold) | 747 ms | 747 ms | 1019 ms |
| 2 | 25 ms | 623 ms | 623 ms | 623 ms |
| 3 | 35 ms | 600 ms | 600 ms | 862 ms |

Method 2, through the Grafana MCP (`http://localhost:8091/mcp`, streamable HTTP, `tools/call query_prometheus` instant query with
`endTime: "now"`), metric only: visible after 331 ms, 910 ms, 1151 ms (3 runs).

Reading: all three signals become queryable in 0.3-1.2 s; the metric is the slowest by up to ~0.4 s. Dashboard-side, Aspire sets
short OTLP export intervals in development and the Collector batches briefly, so the agent can verify a fix within about 2 s of
the request. The MCP adds negligible latency over the backend API. Logs and traces were not re-measured through the MCP (same
backends; the MCP is a thin wrapper). Script: scratchpad only, not kept.

## Local developer experience (task 3.4)

Measured 2026-10-12 on macOS (arm64, Docker with 7.65 GiB), Aspire 13.6.1, one run per mode, each from a stopped state
(`aspire stop --all`, leftover `Probe.*` killed, `docker ps` empty). Warm start: images already pulled, so first-time pull time is
not included. Used `aspire start -- --Spike:Candidates=<mode>` (detached; same AppHost start-up as `aspire run`). Ready = every
resource Running and not Unhealthy in `aspire describe`.

| | `none` (baseline) | `b` (default) | Delta |
|---|---|---|---|
| `aspire start` returns | 5.1 s | 5.3 s | |
| All resources healthy | 38.3 s | 40.4 s | +2.1 s |
| Containers | collector, sql (+ tunnel-proxy) | + lgtm, grafana-mcp | +2 |
| collector | 77 MiB idle, 90 MiB after load | 68 MiB idle, 92 MiB after load | ~0 |
| sql | 1.07 GiB | 1.07 GiB | 0 |
| lgtm | n/a | 657 MiB idle, 739 MiB after load | +0.66-0.74 GiB |
| grafana-mcp | n/a | 20 MiB | +20 MiB |
| Probe.Api / Probe.Web (RSS) | 48 / 50 MiB | not measured | |
| Container memory total (`docker stats`) | ~1.17 GiB | ~1.85 GiB idle, ~1.93 GiB after load | ~+0.7-0.8 GiB (+60 %) |

- One start command: yes (`aspire start` / `aspire run`, optional `-- --Spike:Candidates=none`). No separate `docker compose`.
- Dashboard completeness unchanged (task 1.2; PO confirmed logs, traces, metrics in mode `b`; probe, ASP.NET Core, HttpClient,
  SqlClient and runtime metrics all present). Unchanged by design: the Collector forwards every signal to the dashboard in both modes.
- ServiceDefaults changes needed: none (`git status src` clean; `WithAppForwarding()` re-points the OTLP endpoint).
- Attendee prerequisites for B: Docker (Linux containers) with about 2 GiB free RAM for LGTM on top of SQL Server and the apps (the
  Umbraco site in the demo adds more); .NET 10 SDK; Aspire CLI 13.6.1; Node 20+ for `load.mjs`. Disk for images (`docker images`):
  `grafana/otel-lgtm:0.36.0` 2.58 GB, `mssql/server:2022-latest` 1.59 GB, collector-contrib 0.162.0 380 MB, `grafana/mcp-grafana:2.0.2`
  205 MB. First start pulls about 4.8 GB (LGTM alone 2.6 GB): do this before the talk and tell attendees.
- Fixed host ports required: 17410, 5171/7171, 5172/7172, 3000 (Grafana), 8091 (Grafana MCP). Port 3000 often clashes with Node dev servers.
- Cross-platform risks (not tested): Windows needs Docker Desktop in Linux-container mode and a trusted dev certificate
  (`dotnet dev-certs https --trust`); `aspire stop` left `Probe.Api` / `Probe.Web` running on macOS, so leftovers can hold ports on any OS;
  Collector config uses `insecure_skip_verify` for the dashboard dev certificate; LGTM and the Collector are amd64/arm64 multi-arch but Rosetta
  emulation on Apple silicon was not checked; the Grafana MCP `--allowed-hosts` is tied to port 8091.
- Image note: the Collector comes from `ghcr.io/open-telemetry/opentelemetry-collector-releases/...` (toolkit default), not Docker Hub
  `otel/opentelemetry-collector-contrib` listed in design D1-D3.
- Earlier observation (block 2, candidate A era): prometheus 47 MiB, prometheus-mcp 74 MiB; irrelevant now.

## Talk fit and recommendation (task 3.5)

Draft by the Architect 2026-10-12. PO signed off 2026-10-13: adopt B alongside the Aspire MCP (PO: acceptable if the cost of keeping both is small; Architect assessed it as small: no runtime cost for the Aspire MCP, some agent-context and tool-overlap cost, mitigated by a prompt line).

### Tool surface (proposal questions 4 and 6), verified 2026-10-12 against `grafana/mcp-grafana:2.0.2` over HTTP, `tools/list`
| Flags | Tools |
|---|---|
| none | 79 |
| `--disable-write` (spike setting) | 62 (incl. `grafana_api_request`: GET-only, reaches any Grafana GET, e.g. `/api/users`) |
| `--disable-write --disable-api` | 61 (drops `grafana_api_request`) |
| `--disable-write --enabled-tools prometheus,loki,tempo,datasource` | **22**: `query_prometheus`, `query_prometheus_histogram`, `list_prometheus_*`, `query_loki_logs`, `query_loki_stats`, `query_loki_patterns`, `analyze_loki_labels`, `list_loki_label_*`, `search_tempo_traces`, `get_tempo_trace`, `query_tempo_metrics`, `diff_tempo_traces`, `list_tempo_attribute_*`, `get_tempo_traceql_docs`, `list_datasources`, `get_datasource`, `check_datasources_health` |
| same plus `--disable-query` | 11 (metadata only: not useful) |

`--enabled-tools` is an allow-list of categories and works as intended. The 22-tool set is read-only telemetry only: no dashboards,
users, OnCall, incidents or generic API. It can also be allow-listed per tool in `.claude/settings.json` like the Aspire tools
(ADR-0011). Answers proposal questions 4 and 6 (narrowable: yes).

### Agent quality (proposal questions 1 and 5)
- Access works: the agent listed metrics, queried PromQL, LogQL and TraceQL, and correlated a failing request across log, trace and
  metric by trace ID (asks 7-9). Its log and trace answers and `probe.visits`, DB average and counts matched the dashboard.
- Wrong or weak answers were statistical, not access problems, and mostly self-flagged as low confidence: error and request counts via
  `increase()` over a short window (14 vs 25; ask 2), p95 from an incomplete trace sample (15 of 20 normal spans, 208 ms vs 11 ms; ask 3),
  histogram p95 by two methods (2125 vs 2290 ms vs ~2.0 s real; ask 5). Plan prompt hints for the demo: use counters over a window that
  covers the run, prefer `query_prometheus_histogram`, state that the first request is a cold start.
- Names: OTel names are translated: `probe.visits` is `probe_visits_total`, `probe.visit.duration` (ms) is
  `probe_visit_duration_milliseconds_*`, `http.server.request.duration` is `http_server_request_duration_seconds_*`, `service.name` is
  `service_name`. The agent listed and used the translated names correctly (`agent-answers.md` ask 1; the tool it used is not recorded). Units appear in the name, which helps the agent.
- Quirks: `query_prometheus` instant queries need `endTime: "now"`; the agent printed a trace ID without its leading zero.
- Production comparison (not tested here): Application Insights via the Azure MCP is KQL over Log Analytics, minutes of ingestion lag
  (design D3), so a local PromQL/LogQL/TraceQL session does not look like the production one. The shape of the questions and the
  read-only story are the same; the query languages differ. Present local B as "the same workflow, different backend".

### Can the Grafana MCP replace the Aspire MCP for telemetry? (question 6)
- For logs, traces and metrics: yes. Logs and traces are covered by Loki and Tempo with trace-ID correlation, and metrics are added.
  Freshness 0.3-1.2 s (task 3.3).
- Lost without the Aspire MCP: resource listing and health state, per-resource console logs (stdout, not OTLP logs), AppHost selection,
  `execute_resource_command` (restart, Toxiproxy fault commands in the demo), and Aspire docs tools. Console logs and resource state matter
  for start-up failures, before telemetry exists.

### Cost (question 3, task 3.4)
+2 s start-up, +0.7-0.8 GiB RAM (+60 %), +2.8 GB images (about 4.8 GB total first pull), a new hosting package and two images, fixed
ports 3000 and 8091, and more moving parts for attendees (Windows untested). No app or ServiceDefaults change.

### Recommendation
**Adopt B alongside the Aspire MCP**, with the narrowed Grafana MCP (`--disable-write --enabled-tools prometheus,loki,tempo,datasource`,
22 tools), and keep ADR-0011 for the Aspire MCP (resources, console logs, commands). Use B for the metric-based verification the talk
promises ("p95 back to normal", "how many failed"); use the Aspire MCP to diagnose and to act.
- Why not instead of the Aspire MCP: it would lose resource state, console logs and the fault-injection commands, for a saving of one
  MCP server and no memory (the backend runs either way).
- Why not keep ADR-0011 only: the agent cannot check metrics, which the talk promises ("validate the result" with telemetry).
- If the PO prefers one server for simplicity: B instead of the Aspire MCP is viable for telemetry-only acts; the commands and console
  logs would then be run by the presenter.
- Follow-ups if adopted: a new ADR (or ADR-0011 revision), a plan in `add-demo-foundation` (package `CommunityToolkit.Aspire.Hosting.OpenTelemetryCollector`,
  images, `.mcp.json`, `.claude/settings.json` allow-list), prompt hints, Windows run, and a pre-pull step for attendees.

### Metric names as seen in Prometheus/LGTM (record)
`probe.visits` → `probe_visits_total`; `probe.visit.duration` (ms) → `probe_visit_duration_milliseconds_{bucket,count,sum}`;
`http.server.request.duration` → `http_server_request_duration_seconds_{bucket,count,sum}`; `service.name` → `service_name` (also `job`);
`service_instance_id` present. The names the agent used in task 3.2 match.
