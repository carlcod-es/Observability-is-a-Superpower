# Verification: rework-aspire-first

Branch `aspire-first`. Aspire CLI/SDK 13.6.1, macOS.

## Automated (sections 2-4), 2026-10-15

- Build: `dotnet build SuperPower.slnx` 0 errors (existing NU1903/NU1507 warnings).
  Tests: `dotnet test --solution SuperPower.slnx` 36/36 passed.
- 2.1 `aspire describe` after `aspire run` (trimmed):
  ```
  collector   Container                 Running    Healthy
  grafana-mcp Container                 Waiting    -
  lgtm        Container                 NotStarted -
  site        Project                   Running    Healthy   https://localhost:44368
  smtp4dev    Container                 Running    Healthy
  sql         Container                 Running    Healthy
  toxiproxy   Container                 Running    Healthy
  umbraco     SqlServerDatabaseResource Running    Healthy
  ```
  `LGTM_OTLP_URL` resolved before start (`http://lgtm.dev.internal:4318`); no D2 fallback.
- 2.2 About us `?pre=zzmarker1` before start, Home `?post=zzmarker2` after. Loki
  `{service_name="site"} |= "<marker>"`: zzmarker1 0 lines, zzmarker2 2 lines.
  `aspire otel logs site --search`: both markers present in the Aspire dashboard.
- 2.3 Go-live via `aspire resource lgtm start --apphost
  src/SuperPower.AppHost/SuperPower.AppHost.csproj --non-interactive`: command returns at once;
  Grafana `/api/health` 200 after 9 s; `grafana-mcp` Running after 13 s; site pid start time
  unchanged (no restart). Image pre-pulled.
- 4.1 Smoke check, fresh AppHost: ALL CHECKS PASSED incl. "home trace (first load): database
  spans + linked logs" and "no pre-start telemetry in Grafana" (Loki only; traces covered by
  6.3). `lgtm` already running: ALL CHECKS PASSED, new check N/A. `-- standalone`: ALL CHECKS
  PASSED. Pre-request is `/smoke-pre/` (404), see design D4.

## PO checks (section 6)

Fresh AppHost, `lgtm` not started, new Claude Code session at repo root, default permission
mode. Do not run the smoke check first (it starts `lgtm`).

Run 2026-10-15, PO session; outputs pasted to the Architect.

### 6.1 Local stage
- Contact POST trace from Aspire (before go-live): trace d86b6655…, POST /contact-us/ 97 ms
  302, child SQL span INSERT INTO [acmeFormSubmission] 5 ms; the trace's two log lines read.
  Agent also read site console output (Umbraco telemetry POST seen there).
- No form values in telemetry: SQL span parameterised, request log gives content type/size only.
- PO confirmed: /mcp aspire connected / grafana failed, resource list with state, no prompts on
  read tools.
- Toxiproxy Slow: NO prompt; command ran (08:18:20 UTC, "Command 'slow' executed successfully").
  Cause: the PO session ran in permission mode `auto` (session transcript), not default as
  task 6 requires; committed allow list excludes `execute_resource_command`. Re-run in default
  mode pending. Slow toxic left active; Heal needed.
- Re-run 2026-10-15, PO healed `toxiproxy`, new session in default mode: "run the Toxiproxy Slow
  command" showed the approval prompt for `aspire — Execute Resource Command` (toxiproxy, slow).
  After the prompt the proxy had no toxics (`GET /proxies/smtp/toxics` -> `[]`): not executed.
  Met. Note: option 2 "don't ask again" would write a local allow rule; decline it on stage.

### 6.2 No credentials
- PO: no database password or connection string in `list_resources` (site, sql) or console
  output; only the Aspire dashboard login token, accepted by the PO. Tool description states
  "Environment variable values aren't provided because they could contain sensitive
  information." Met.

### 6.3 Go live
- Contact submitted 08:23:10 UTC, before `lgtm` start. Oldest site log in Loki 08:26:44 UTC.
- Home trace from Grafana (Tempo): 26dd5b05…, GET / 27.5 ms 200, one server span (cached).
- Earlier Contact submission in Grafana: Tempo POST search no traces; Loki "contact-us" no
  lines. Prometheus POST 302 count 1 (cumulative, allowed by spec).
- Request-duration metric: `http_server_request_duration_seconds` retrieved, but has no
  `http_route`/path label, so not per page (Home not separable). See findings.
- Stored submissions: agent has no database tool (answered from rule; tool list not shown).
- PO: `grafana` 22 tools, all read-only; no prompts. Met except F1.

### Findings
- F1 (spec gap): spec `agent-telemetry-access` "Agent checks a metric" (THEN: that page's
  request-duration metric) not met: the HTTP server metric has no route/page label under
  Umbraco's catch-all routing. Page durations are available from traces only. Resolved
  (PO 2026-10-15): scenario reworded to site-wide metric + per-page durations from traces;
  per-page label recorded as FR-0003, built with `add-demo-dashboard`.
