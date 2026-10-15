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

### 6.1 Local stage
- `/mcp`: aspire = ; grafana =
- `select_apphost` needed? (review-followups n2):
- Latest Home trace with spans and durations:
- That trace's logs:
- Resource list with state:
- Site console output:
- Approval prompts on the above (expect none):
- Toxiproxy Slow via agent: prompt shown? (decline):

### 6.2 No credentials
- `list_resources` for `site` and `sql`: password / connection string present?
- Site console output: password / connection string present?
- (Other secrets seen, outside spec: `collector` API key, `grafana-mcp` password?)

### 6.3 Go live
- Contact form submitted at (UTC):
- `lgtm` Start clicked at; `grafana-mcp` Running at:
- `/mcp` reconnect `grafana`:
- Home request-duration metric (no prompt):
- Home trace from Grafana (no prompt):
- Trace or log for the earlier Contact submission (expect none; metric counters may include it):
- `grafana` tool count (expect 22, read-only):
- Stored submissions request: database tool available? (expect none):
