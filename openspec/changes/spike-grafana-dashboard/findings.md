# Findings: spike-grafana-dashboard

Observed on Windows 11, Docker Desktop 29.7.2, Aspire CLI 13.4.6 (AppHost SDK 13.6.1),
2026-10-14. Environment: `spikes/metrics-mcp` mode `b`, unchanged.

## 1. Preparation (`spike-grafana-dashboard` task 1.1)

- Grafana version in `grafana/otel-lgtm:0.36.0`: **13.2.3** (`GET /api/health`).
- Datasource UIDs provisioned by the image are fixed: `prometheus` (default), `loki`, `tempo`,
  `pyroscope`. Dashboard JSON can reference them directly.
- First start on Windows: `aspire start` timed out after the CLI's default 120 s, killed the
  AppHost and left the `lgtm` and `sql` containers orphaned (removed with `docker rm -f`).
  Second start with `ASPIRE_CLI_START_TIMEOUT=600` came up in under a minute but was killed at
  that timeout later. Cause: CLI/AppHost version mismatch (see section 4, Persistence,
  environment note), not image pulls. First Windows run of this spike environment (previous
  spike ran on macOS); otherwise no Windows-specific issues.
- `node load.mjs`: 20 normal, 5 slow, 5 failing web requests, as expected.
- Signals in LGTM, checked through the Grafana datasource proxy API (same queries as Explore):
  - Metrics: `http_server_request_duration_seconds_*` for `probe-web` and `probe-api`
    (probe-api 28 x 200, 20 x 500), `probe_visits_total`, `probe_visit_duration_milliseconds_*`,
    plus ASP.NET Core / .NET runtime / Kestrel / HttpClient metrics. Labels: `job` and
    `service_name` = service name, `instance` = `service_instance_id`.
  - Logs: Loki `{service_name="probe-api"} | detected_level="error"` returns the unhandled
    exception logs with `trace_id`/`span_id` and `exception_*` attributes.
  - Traces: Tempo `{resource.service.name="probe-web"}` returns `GET /` traces spanning
    probe-web and probe-api with error counts.
- Note: probe-web answers 200 with an error page when probe-api fails, so HTTP errors only
  show on `probe-api` (`http_response_status_code="500"`, `error_type` label).

## 2. Community import (`spike-grafana-dashboard` tasks 2.1, 2.2; proposal question 1)

Route: downloaded JSON from `grafana.com/api/dashboards/<id>/revisions/latest/download`, then
`POST /api/dashboards/import` with input `DS_PROMETHEUS` = `prometheus`. This is the call the
UI import screen makes; used instead of clicking through the UI (no browser automation). Both
imported first time, no errors. Stored JSON: no `DS_PROMETHEUS` left, all panels on datasource
UID `prometheus`, schemaVersion 39 as published.

| Dashboard | UID | URL |
|---|---|---|
| ASP.NET Core (19924) | `KdDACDp4z` | `http://localhost:3000/d/KdDACDp4z/asp-net-core` |
| ASP.NET Core Endpoint (19925) | `NagEsjE4z` | `http://localhost:3000/d/NagEsjE4z/asp-net-core-endpoint` |

Panel check: every panel's PromQL run against Prometheus over the last 30 minutes with the
dashboard variables substituted (`job`/`instance` from `http_server_active_requests`, both
services listed; route `/visits`, method `GET`).

19924, `job=probe-api` - all 10 panels show data: Requests Duration (p50-p99.9), Errors Rate
(4xx/5xx share), Current Connections, Current Requests, Total Requests, Total Unhandled
Exceptions, Requests Secured, Requests HTTP Protocol, Top 10 Requested Endpoints, Top 10
Unhandled Exception Endpoints.
19924, `job=probe-web` - 8 of 10 show data; Total Unhandled Exceptions and Top 10 Unhandled
Exception Endpoints are empty because probe-web had no unhandled exceptions (correct, not a
mapping problem).
19925, `job=probe-api`, `GET /visits` - all 6 panels show data: Requests Duration, Errors
Rate, Unhandled Exceptions (by `error_type`), Requests HTTP Status Code (200 and 500),
Requests Secured, Requests HTTP Protocol.

Why it works: the dashboards are written for the .NET 8+ built-in meters, and LGTM's
OTLP-to-Prometheus translation produces the names and labels they expect
(`http_server_request_duration_seconds_bucket`, `http_route`, `http_response_status_code`,
`error_type`, `job`, `instance`). No query edits needed.

Limits seen:
- Default time range is `now-5m`, refresh 10 s: after a single `load.mjs` run the panels go
  empty within five minutes. A demo needs steady background traffic or a wider range.
- They show HTTP server metrics only: nothing about the app's own meters
  (`probe_visits_total`), logs or traces, and no link from a spike to the failing request.
- probe-web's failures do not appear as errors there (it returns 200 with an error page); the
  dashboard shows the real behaviour of the app, which is a point in its favour.

**Answer to proposal question 1: usable as-is.** Both dashboards light up fully against the
LGTM data with no edits. They cover generic ASP.NET Core health only.

## 3. Custom dashboard (`spike-grafana-dashboard` tasks 3.1, 3.2; proposal question 2)

File: `spikes/metrics-mcp/dashboards/probe-basic.json` (uid `probe-basic`,
`http://localhost:3000/d/probe-basic/probe-basic`). Loaded with
`POST /api/dashboards/db` (`{"dashboard": <file>, "overwrite": true}`).

| Panel | Query |
|---|---|
| Request rate | `sum by (job) (rate(http_server_request_duration_seconds_count[1m]))`, req/s |
| Error rate (5xx) | 5xx rate / all rate per `job`, percent |
| Request duration p95 | `histogram_quantile(0.95, sum by (job, le) (rate(..._bucket[1m])))`, seconds, exemplars on |
| Visits per second | `sum(rate(probe_visits_total[1m]))` |
| Visit duration p95 | `histogram_quantile(0.95, sum by (le) (rate(probe_visit_duration_milliseconds_bucket[1m])))`, ms |
| Error logs (Loki) | `{service_name=~"probe-.*"} \| detected_level="error"` |

Verification: every panel's targets sent through `/api/ds/query` (the call a panel makes to
render) over `now-15m` with steady traffic running: all six return data, no query errors.
Cross-check against raw queries: request rate for probe-web and probe-api and visit rate all
~96/min, as expected (one API call and one visit per page view). Error logs panel returns the
probe-api unhandled-exception records. p95 panel also returns exemplar points (trace IDs).

Effort: one JSON file, written once; **iterations: 1** (rendered on first load). Route:
**JSON-first**, no UI fallback needed. About 10 minutes of authoring, most of it spent finding
facts, not writing JSON.

What authoring needed to know (all checked against the live data first):
- Metric names after OTLP-to-Prometheus translation: dots to underscores, unit suffix added
  (`_seconds`, `_milliseconds`), counters get `_total`, histograms split into
  `_bucket`/`_count`/`_sum`. `service.name` becomes `job` and `service_name`.
- Units: HTTP duration is seconds, the custom histogram milliseconds; set per panel.
- Percentiles need `histogram_quantile` over `sum by (le)` of `rate(..._bucket)`.
- Datasource UIDs: the LGTM image provisions fixed UIDs `prometheus`, `loki`, `tempo`, so the
  JSON references them directly; no `${DS_...}` input variables needed in this environment.
- Rate window: the provisioned Prometheus datasource sets `timeInterval: 60s`, so
  `$__rate_interval` resolves to minutes and faults would show late. The apps export metrics
  every 1 s, so the dashboard uses fixed `[1m]` windows.
- Loki: OTLP attributes are queryable; `detected_level="error"` filters error logs.
- Exemplars: the .NET SDK sends them and the datasource links them to Tempo
  (`exemplarTraceIdDestinations`), so `"exemplar": true` on the p95 query gives
  click-through from a duration point to its trace. One flag, no extra panel.

**Answer to proposal question 2: yes.** A basic dashboard authored as JSON rendered every panel
on the first load. The work is knowing the translated metric names, units and quantile
pattern, which an agent can read from the live data (the Grafana MCP or the Prometheus API).

## 4. Faults, persistence, README (`spike-grafana-dashboard` tasks 4.1-4.4; proposal questions 3, 4)

### Faults (task 4.1, proposal question 3)

Method: scratchpad traffic loop to probe-web (one request, then 0.5 s pause), switched from
normal to half normal / half `?slow=2000` at 12:42:39 UTC, to half `?fail=true` at 12:44:10,
back to normal at 12:45:40. Each panel sampled through `/api/ds/query` over `now-5m` at the
end of each 90 s phase.

| Panel | Normal | Slow (+90 s) | Fail (+90 s) |
|---|---|---|---|
| Request rate (req/s) | web 1.93, api 1.93 | both 0.70 (sequential load slows down) | web 0.27, api 0.59 (retries) |
| Error rate (5xx) | api 0 % | api 0 % | **api 74 %** |
| Request duration p95 | web 19 ms, api 16 ms | **both 2.34 s** | api 10 ms, **web 10 s** |
| Visits per second | 1.93 | 0.70 | 0.15 |
| Visit duration p95 | 15.5 ms | **2338 ms** | 9.75 ms |
| Error logs | none in window | none | **47 records** (api unhandled exceptions, web resilience errors) |

Visible without the agent: yes, both faults. Slow shows as a p95 jump on both services and the
custom histogram; fail shows as a probe-api error-rate jump and error logs appearing. Both
were fully visible at the 90 s sample; with `[1m]` windows the lines move within the first
minute.

Readings to be aware of:
- probe-web's error rate stays 0 during fail (it returns 200 with an error page). The failure
  shows on probe-web only as the p95 rise from retries with backoff.
- web p95 during fail reads exactly 10 s: the top finite bucket of the default .NET HTTP
  histogram is 10 s, so `histogram_quantile` caps there. Exemplars show the real duration
  (8.6 s).
- The default dashboard window matters: error logs older than the window disappear, so a
  quiet period shows an empty logs panel.

### Persistence (task 4.2, proposal question 4)

Files in `spikes/metrics-mcp/dashboards/`:
- `probe-basic.json` - custom, authored form: datasource UIDs written literally (`prometheus`,
  `loki`).
- `aspnetcore-19924.json`, `aspnetcore-endpoint-19925.json` - community, as published on
  grafana.com, which is the "export for sharing externally" form: `__inputs` with
  `DS_PROMETHEUS`, panels reference `${DS_PROMETHEUS}`. Kept because they work as-is.

Restart: AppHost stopped, all spike containers removed, AppHost started again; new LGTM
container (`lgtm-nwrzwpty` replaced by `lgtm-wahsahnz`). After start Grafana held only the
image's bundled dashboards (JVM Overview, RED Metrics classic and native); the three imported
dashboards were gone, as expected.

Re-import from the files, all through `POST /api/dashboards/import` (the endpoint the UI's
Import > Upload uses): all three imported first time and render with data. A 60 s fail burst
on the fresh stack filled the error panels on all three (custom error rate 45 % on probe-api,
27 error logs; community exception stats and tables populated).

Datasource UID handling:
- Authored form with literal UIDs: re-imports cleanly with no inputs, by UI upload,
  `/api/dashboards/import` or `/api/dashboards/db`. Works because the LGTM image always
  provisions the same datasource UIDs; it would break on a Grafana with other UIDs.
- Share form (`${DS_PROMETHEUS}`): re-imports cleanly through import (UI asks for the
  Prometheus datasource; API takes `inputs`). Saved directly with `/api/dashboards/db` the
  input is not resolved and every panel fails with "Data source not found" (tested, then
  deleted).

Iteration 2 on the custom dashboard, found here: on fresh data with no 5xx yet the error-rate
panel showed "No data" instead of 0 % (the 5xx series did not exist yet). Fixed with
`(<5xx rate> or 0 * <all rate>) / <all rate>`; file updated, re-imported, shows 0 % for both
services and 45 % during the burst.

**Answer to proposal question 4: yes, by re-import.** Dashboards live in the LGTM container and
are lost when it is recreated; the JSON files in the repo bring them back in seconds. Both file
forms work through import; literal UIDs are simpler for this stack.

Environment note: Aspire CLI 13.4.6 (README requires 13.6.1) cannot connect to the 13.6.1
AppHost. `aspire start` printed "AppHost started successfully", then killed the AppHost at the
start timeout and left every container orphaned; `aspire stop` and `aspire ps` saw no AppHost.
Workaround used from then on: `dotnet run --project Metrics.AppHost`. Upgrading the CLI to the
README version is the expected fix (not tested; global tool left unchanged).

### README (task 4.3)

`spikes/metrics-mcp/README.md` has a "Dashboards" section: files, UI import steps (select
`Prometheus` for the two community files), Grafana URL and login pointer. The steps were
checked through the same API endpoint as the UI upload (`/api/dashboards/import`, all three
files render). PO 2026-10-14: imported a dashboard in the Grafana UI following the README and saw
results.

### Provisioning stretch (task 4.4)

Not attempted. It needs a bind mount in `AppHost.cs`, which design D1 (AppHost unchanged) rules
out and design D3 step 2 allows; the PO stopped the spike before deciding. Recorded for the demo
plan: the image reads dashboard providers from `/otel-lgtm/grafana/conf/provisioning/dashboards/`
(its own `grafana-dashboards.yaml` there points at JSON files under `/otel-lgtm/`), so a
provider file plus the JSON mounted there is the expected route. Unverified.

### Drilldown apps (PO observation 2026-10-14)

The LGTM image ships Grafana's Drilldown apps, enabled, under Drilldown in the menu: Logs
(`/a/grafana-lokiexplore-app/explore`), Traces (`/a/grafana-exploretraces-app/explore`), Metrics,
Profiles. With no set-up they give per-service log browsing with a `detected_level` filter and
exception fields, and trace error rates with breakdowns and the failing trace (probe-web through
its retries to probe-api). The PO reviewed them with manual traffic and found them good. They
cover logs and error drill-down without any dashboard work; the custom dashboard is the
overview that leads into them (exemplars, error-logs panel).

## 5. ADR-0017 safety (`spike-grafana-dashboard` task 5.1, proposal question 5)

Read-only git status after tasks 1.1-4.3 (before task 4.4):

```
$ git status --short
 M openspec/changes/spike-grafana-dashboard/tasks.md
 M spikes/metrics-mcp/README.md
?? openspec/changes/spike-grafana-dashboard/findings.md
?? spikes/metrics-mcp/dashboards/

$ git status --short -- spikes/metrics-mcp ':!spikes/metrics-mcp/dashboards' ':!spikes/metrics-mcp/README.md'
(no output)
```

Collector config (`collector/`), `AppHost.cs`, `spikes/metrics-mcp/.mcp.json`, the repo-root
`.mcp.json` and the Grafana MCP flags (`--disable-write`) are unchanged. All dashboard writes
went straight to the Grafana HTTP API with the spike-local admin login, never through the
MCP. The narrowed MCP surface adopted in ADR-0017 has no dashboard tools, so the on-stage
agent cannot read or change dashboards: they stay a presenter/audience surface.

**Answer to proposal question 5: no impact.** Nothing in the adopted set-up was touched, and
the dashboards need nothing from it beyond the telemetry it already stores.

## 6. Recommendation to the PO (`spike-grafana-dashboard` task 5.2)

PO 2026-10-14: dashboards and Drilldown reviewed in the UI ("looks good"); observations handed
to the main application plan.

**Contents.** One custom demo dashboard, the probe-basic layout applied to the Umbraco site:
- Request rate, 5xx error rate, request p95 with trace exemplars, per service.
- The site's own form meters once `add-form-emails` adds them (submissions by form and
  outcome, email send duration and failures), same pattern as the probe's counter and histogram.
- A Loki error-logs panel.
For drill-down use the bundled Logs and Traces Drilldown apps rather than building more panels.
Optionally also import ASP.NET Core 19924 unchanged as a second, generic view: zero effort, works
as-is. Do not lead with it: it shows nothing app-specific and no logs.

**Built how.** JSON-first, written by the Architect/agent, checked through `/api/ds/query` against
live data. Took one iteration for probe-basic plus one fix found on fresh data. Literal datasource
UIDs (`prometheus`, `loki`, `tempo`), since the LGTM image always provisions the same ones. No
UI building by the PO.

**Persisted how.** JSON files in the repo. To have the dashboard there on stage with no manual
step, provision it through the LGTM image's Grafana provisioning directory (a bind mount in the
AppHost); unverified (task 4.4 not attempted), to verify in the demo plan. Fallback: import from the file after each start (seconds,
UI or API).

**Demo considerations found here.**
- Panels need steady traffic and a window that covers it; after a single burst they empty within
  the window (5 min for 19924, 15 min for probe-basic).
- Use fixed `[1m]` rate windows; the LGTM datasource's `timeInterval: 60s` makes
  `$__rate_interval` minutes long and faults show late.
- An app that turns a downstream failure into a 200 page hides it from error-rate panels (probe-web
  did). Check how the site's form failures surface before choosing the error panel.
- The default .NET HTTP histogram caps p95 at 10 s; exemplars show the real duration.

**Follow-up plan would touch.** An ADR (demo dashboard: contents, JSON-first, provisioning) and
`add-demo-foundation` tasks for the dashboard JSON, the AppHost provisioning mount and a demo
traffic source, with the form panels in `add-form-emails` (proposal, Impact).
