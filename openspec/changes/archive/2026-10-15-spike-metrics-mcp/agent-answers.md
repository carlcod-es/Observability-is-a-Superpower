# Agent answers: spike-metrics-mcp (task 3.2)

Agent: Claude Code (Sonnet 5.5), 2026-10-12, from `spikes/metrics-mcp/`. Tools: `grafana` MCP only (Loki,
Tempo, Prometheus); no `aspire` MCP, no source reads. Questions in `agent-asks.md`. Compared with the
dashboard and Explore in `findings.md` ("Agent reads metrics").

Load input: the pasted `load.mjs` lines were phase definitions without timestamps:
`normal` 20 × `/`, `slow` 5 × `/?slow=2000`, `fail` 5 × `/?fail=true`. Run window ~21:28:36-21:29:28
UTC (~51 s), inferred from trace timing. Phase split by query string and timing.

## Phase overview (from traces)

- `slow`: 5 `probe-web` `GET /` traces, 2012-2016 ms, no errors; ~2000 ms is one `probe-api` span.
- `fail`: 5 traces, 5.9-10.1 s; each has 4 failing `probe-api` `GET /visits` calls (500, 5-40 ms) from
  `probe-web` retries; `probe-web` returns HTTP 200 to the client.
- Failure hidden at the web tier; retries add latency (up to ~10 s).

## 1. Metrics available for `probe-api` and `probe-web`

Labels: `service_name`. Histograms as `_bucket` / `_count` / `_sum`. Both services unless marked.

- Inbound HTTP: `http_server_request_duration_seconds`, `http_server_active_requests`,
  `aspnetcore_routing_match_attempts_total`; `aspnetcore_diagnostics_exceptions_total` (`probe-api` only).
- Outbound HTTP: `http_client_request_duration_seconds`, `http_client_request_time_in_queue_seconds`,
  `http_client_active_requests`, `http_client_open_connections`, `http_client_connection_duration_seconds`,
  `dns_lookup_duration_seconds`.
- Database: `db_client_operation_duration_seconds` (`probe-api` only).
- Custom (`probe-api`): `probe_visits_total`, `probe_visit_duration_milliseconds`.
- Kestrel: `kestrel_active_connections`, `kestrel_queued_connections`, `kestrel_connection_duration_seconds`;
  `kestrel_active_tls_handshakes`, `kestrel_tls_handshake_duration_seconds` (`probe-api` only, HTTPS).
- .NET runtime: GC (`dotnet_gc_*`), thread pool (`dotnet_thread_pool_*`), JIT (`dotnet_jit_*`),
  `dotnet_monitor_lock_contentions_total`, `dotnet_timer_count`, `dotnet_exceptions_total`.
- Process / memory: `dotnet_process_cpu_time_seconds_total`, `dotnet_process_cpu_count`,
  `dotnet_process_memory_working_set_bytes`, `dotnet_assembly_count`, `aspnetcore_memory_pool_*`.
- Trace-derived (not emitted by the apps): `traces_spanmetrics_*`, `traces_service_graph_*`; labelling
  not checked.
- Gaps: no retry or circuit-breaker metrics in `probe-web`.

## 2. Request rate and error rate, `probe-api`

- Prometheus (`http_server_request_duration_seconds_count`, route `/visits`), `increase` over the run:
  200 ≈ 14, 500 ≈ 19. Peak 30 s rates: 200 0.49 req/s (21:28:45-21:29:00), 500 0.48 req/s (21:29:30).
- Expected from the phases: 25 × 200 (20 normal + 5 slow), 20 × 500 (5 fail × 4 attempts) = 45.
- Average over ~51 s: ~0.9 req/s; errors ~0.4 req/s; error ratio ~44 % (per client request 5 of 30, 17 %).
- Caveat: the 200 count (~14) is below 25; cause unverified (first sample of a new counter series is
  one candidate). `increase` values are extrapolated.

## 3. p95 `probe-api` `GET /visits` (Tempo server spans, status != error)

| Phase | Spans | p95 | Range |
|---|---|---|---|
| normal | 15 | 208.5 ms (nearest rank = max) | 3.5-208.5 ms |
| normal, first request excluded | 14 | 9.0 ms | 3.5-9.0 ms |
| slow | 5 | 2013.75 ms (max) | 2010.1-2013.8 ms |

- First normal request (208.5 ms, epoch 1791840516.09) looks like a cold start.
- Only 15 of 20 normal spans found in Tempo; missing five unexplained. The slow delay is 2000 ms plus
  ~10-14 ms handling.
- Prometheus histogram not used: the normal phase lasted under 1 s.

## 4. `probe.visits` total

25 (`probe_visits_total`, one series, `service_name="probe-api"`). Equals the 25 successful calls
(20 normal + 5 slow); the 20 failed attempts are not counted. Resets on `probe-api` restart.

## 5. p95 `probe.visit.duration`

~2125 ms, interpolated from cumulative buckets (25 observations): 16 ≤ 5 ms, 19 ≤ 10 ms, 20 ≤ 250 ms
(and ≤ 1000 ms), 25 ≤ 2500 ms. p95 = rank 23.75 → 1000 + (3.75 / 5) × 1500 = 2125 ms.
The Grafana helper (`rate` over 30 m) returned 2290 ms; preferred the cumulative figure (`rate` drops the
counter's first increase). Spans show the five slow visits at ~2.01 s, so the true p95 is ~2.01 s; a bucket
boundary near 2000 ms would tighten it.

## 6. Database call duration, `probe-api`

SQL Server, database `probe`, table `dbo.visit`. Cumulative since start; p95 from buckets.

| Operation | Calls | Average | p95 |
|---|---|---|---|
| `INSERT dbo.visit` | 45 | 8.1 ms (0.3623 s) | ~42 ms |
| `SELECT dbo.visit` | 25 | 2.6 ms (0.0651 s) | ~9 ms |
| Combined | 70 | 6.1 ms | ~36 ms |

- The database plays no part in the slow phase (2 s) or the failures.
- 45 inserts = 25 successful visits + 20 failed attempts, while `probe.visits` is 25 and SELECTs are 25:
  failed requests appear to insert before returning 500. A failing `GET /visits` trace (question 9) has an
  `INSERT dbo.visit` child. Whether the row is committed is unchecked.

## 7. Error logs of the failing requests

- 5 failing requests; message `Probe failure requested` (`System.InvalidOperationException`).
- 25 error-level lines over 2 h, all from the run: 20 `probe-api` ("An unhandled exception has occurred
  while executing the request.", 4 per request) and 5 `probe-web` (Polly `Standard-Retry`: "Result: '500',
  Handled: 'True', Attempt: '3'", one per request).
- Failing traces: `3db8853b7b8ef2f495525118900c94c7`, `422b4ac5c27ebda3f8659d09e5a3675f`,
  `1107fa0f3ab5b0aeeb1c87eb7d20073f`, `5918af322845b1be6e70535d25cfed74`,
  `a1ad5e00e424355d345008c44522e4f0` (4 `probe-api` exceptions each).
- Each client request: original attempt + 3 retries. Stack trace points to the request handler
  (`Program.cs` lines 74 and 84); exceptions pass through `DeveloperExceptionPageMiddleware` (Development).
- Gaps: `probe-web` returns 200 and its own request log shows no error; the Polly line has no exception
  message, so linking to the cause needs the trace ID.

## 8. Slow request, dominant span

Trace `666050830a0680e760f13db92bb2569` (slow phase), 2016.7 ms, HTTP 200.

| Span | Service | Duration |
|---|---|---|
| `GET /` | `probe-web` | 2016.7 ms |
| `GET` (HTTP client) | `probe-web` | 2014.5 ms |
| `GET /visits` | `probe-api` | 2013.8 ms |
| **`probe.slow-work`** (`probe.delay_ms = 2000`) | `probe-api` | **2000.9 ms (99.2 %)** |
| `INSERT dbo.visit` | `probe-api` | 4.1 ms |
| `SELECT dbo.visit` (`SELECT COUNT(*) FROM dbo.visit`) | `probe-api` | 6.1 ms |

- Anomaly: `probe.slow-work` and `SELECT dbo.visit` have the 4 ms `INSERT` span as parent (the `SELECT`
  starts ~2 s after it ended); they should be children of `GET /visits`. Cause not confirmed.
- No attribute or log records why the delay happened, only the requested value.

## 9. One failing request across signals

Trace `5918af322845b1be6e70535d25cfed74`, minute 21:29:00-21:30:00 UTC.

- Log: `probe-api` error "An unhandled exception has occurred…" `InvalidOperationException: Probe failure
  requested`, `trace_id` / `span_id` (`ed4b2853bb084772`) match the trace. 5 error lines in total: 4
  `probe-api` + 1 Polly (`probe-web`).
- Trace: root `GET /` (`probe-web`) 5927.8 ms, HTTP 200, status unset; 4 client `GET` spans (500) and 4
  `GET /visits` server spans (`status=error`, 4.6 / 8.2 / 13.5 / 13.0 ms, starts 21:29:11.6, :14.3, :16.9,
  :17.5), each with an `INSERT dbo.visit` child (4-12 ms). Most of the 5.9 s is waiting between retries.
- Metric (minute): `probe-api` 500 ≈ 14.2, 200 = 0; `probe-web` 200 ≈ 4.1, no 500.
- Agreement: yes. 14 failing `probe-api` spans in the minute (2 from `422b…`, 4 each from `1107…`, `5918…`,
  `a1ad…`) match the metric; log elapsed times match span durations.
  `probe-web` shows only 200s in the metric and the root span.
- Unit difference: the metric counts attempts (14); trace and Polly log count one failed client request
  (4 attempts). A dashboard on `probe-api` 500s reports ~4× the user-visible failures.

## 10. Self-assessment (least sure first)

1. Q3 normal-phase p95: 15 of 20 spans found, p95 ≈ max, phase windows inferred. Conflicts with Q5's 20
   normal visits.
2. Q2 rates: 200 count ~14 vs 25 expected, cause unverified; averages derived from the inferred window;
   `increase` extrapolated.
3. Q6 insert-on-failure inference: from 45 vs 25 counts and a failing trace's `INSERT` child; commit
   unchecked.
4. Q5 p95 2125 ms: bucket interpolation; Grafana helper gave 2290 ms; true value ~2.01 s.
5. Q1: spanmetrics / service-graph labels not checked.
6. Q9 "14 matches 14": hand reconstruction of attempts in the minute (one borderline at ~21:29:00.6);
   clock mapping derived from one query timestamp.

Most reliable: Q4, Q7, Q8 (direct query results, little inference).
