## Context

Current state (branch `add-demo-dashboard` from `main` at `d422997`):
- `src/SuperPower.AppHost/AppHost.cs`: `lgtm` (`grafana/otel-lgtm:0.36.0`, Grafana host port
  3000, `WithExplicitStart()`), `collector` with separate `*/lgtm` pipelines (no replay),
  `grafana-mcp` (read-only, `prometheus,loki,tempo,datasource` tools) waits for `lgtm`
  (ADR-0022). No dashboards; nothing mounted into `lgtm`.
- Site metrics: `AddAspNetCoreInstrumentation()` metrics, meter `Acme.Forms` (no instruments
  yet). `http.server.request.duration` has no usable route for Umbraco pages (FR-0003).
  Service name `site` (Prometheus `job="site"`, Loki/Tempo `service_name="site"`).
- `TelemetryPaths` excludes `/health`, `/alive`, `/umbraco/backoffice`, `/css`, `/js`, `/media`,
  `/favicon.ico` from traces and logs. `/assets/rubber-duck.svg` (header logo, added
  2026-10-15) is not excluded: every page load adds a logo trace.
- Content types rendered as pages: `home`, `page`, `contact`, `events`, `event`, `services`,
  `service`.
- `tests/smoke/smoke.cs` (ADR-0012) starts `lgtm` and reads Grafana's Loki and Tempo APIs.
- Spike inputs: `spike-grafana-dashboard` `findings.md` sections 3, 4 and 6 (metric names after
  OTLP translation, `[1m]` windows, `or 0 *` guard, exemplars, literal datasource UIDs).

Fixed by the talk: Grafana plays production, started at go-live (ADR-0022). Implied gaps
closed here: panels need traffic (D5) and per-page metrics (D3).

```
 traffic.cs --GET pages--> site --OTLP (acme.page_type on metric + span)--> collector --> lgtm
                                                                                         ^
       src/SuperPower.AppHost/grafana/{acme-dashboards.yaml, dashboards/*.json} --mount--+
```

## Goals / Non-Goals

**Goals:**
- Demo and ASP.NET Core dashboards present on every `lgtm` start (spec `demo-dashboard`).
- Page-type label on request metric and span (spec `local-environment`, `agent-telemetry-access`).
- Steady demo traffic on demand; `/assets` excluded from traces and logs.

**Non-Goals:**
- Form panels (`add-form-emails`), tunnel (`add-production-environment`).
- Dashboards in the Aspire dashboard; agent access to dashboards.
- Live reload of dashboard JSON; dashboard variables or folders beyond one "Demo" folder.

## Decisions

### D1: Dashboard files and provisioning mount (ADR-0023)
Files: `src/SuperPower.AppHost/grafana/acme-dashboards.yaml`,
`src/SuperPower.AppHost/grafana/dashboards/demo-site.json`,
`src/SuperPower.AppHost/grafana/dashboards/aspnetcore-19924.json` (+ a one-line source and
licence note in `src/SuperPower.AppHost/grafana/dashboards/README.md`). Provider file:
```yaml
apiVersion: 1
providers:
  - name: acme
    folder: Demo
    type: file
    disableDeletion: true
    allowUiUpdates: false
    options:
      path: /otel-lgtm/acme-dashboards
```
`AppHost.cs` on `lgtm`: `.WithBindMount("./grafana/acme-dashboards.yaml",
"/otel-lgtm/grafana/conf/provisioning/dashboards/acme-dashboards.yaml", isReadOnly: true)` and
`.WithBindMount("./grafana/dashboards", "/otel-lgtm/acme-dashboards", isReadOnly: true)`
(relative sources resolve against the AppHost directory; portable). First apply step verifies
the image's Grafana reads that provisioning directory. If not: fallback (PO 2026-10-14)
`ResourceReadyEvent` on `lgtm` posting each JSON as `{"dashboard": <json>, "overwrite": true}`
to `/api/dashboards/db` with admin/admin; mounts removed. Alternatives in ADR-0023.

### D2: Demo dashboard JSON (ADR-0023)
uid `demo-site`, title "Demo site", `now-15m`, refresh `10s`, datasource UIDs literal.
Panels (all `job="site"`, metric `http_server_request_duration_seconds_*`):
| Panel | Query |
|---|---|
| Request rate by page type | `sum by (acme_page_type) (rate(..._count{job="site"}[1m]))`, req/s |
| Error rate (5xx) | `(sum(rate(..._count{job="site",http_response_status_code=~"5.."}[1m])) or 0 * sum(rate(..._count{job="site"}[1m]))) / sum(rate(..._count{job="site"}[1m]))`, percent |
| Request duration p95 by page type | `histogram_quantile(0.95, sum by (acme_page_type, le) (rate(..._bucket{job="site"}[1m])))`, seconds, `exemplar: true` |
| Error logs | Loki `{service_name="site"} \| detected_level="error"` |
Checked through `/api/ds/query` against live data (spike method), then by the PO in the UI.
Alternative: `$__rate_interval` (minutes long with LGTM's `timeInterval: 60s`, faults show
late; spike).

### D3: Page-type tag middleware (ADR-0024)
`src/SuperPower.Web/Telemetry/PageTypeTelemetryMiddleware.cs`, registered in `Program.cs` with
`app.UseMiddleware<PageTypeTelemetryMiddleware>()` before `app.UseUmbraco()`. In `finally`
after `await next(context)`: alias =
`context.Features.Get<UmbracoRouteValues>()?.PublishedRequest.PublishedContent?.ContentType.Alias`;
value = alias or `"none"`; add `new("acme.page_type", value)` to
`context.Features.Get<IHttpMetricsTagsFeature>()?.Tags` and
`context.Features.Get<IHttpActivityFeature>()?.Activity.SetTag(...)`. Tag name and `none` as
constants in one static class (`PageTypeTag`) with a pure `From(string? alias)` covered by an
xUnit test. Form posts run on the page's route values, so they carry the page's type.
Alternatives in ADR-0024.

### D4: `/assets` excluded (ADR-0008 amended)
Add `"/assets"` to `TelemetryPaths.ExcludedPrefixes`. Metrics still count those requests
(under `none`).

### D5: Traffic source `tests/traffic/traffic.cs` (ADR-0012 pattern)
.NET 10 file-based app, no packages. `dotnet run tests/traffic/traffic.cs [-- <site-url>]`,
default `https://localhost:44368`, dev certificate accepted as in `smoke.cs`. Start: GET Home,
Events list and Services list; collect same-origin `href`s from them (no extension, not
`/umbraco`, no `#`, no query); dedupe. Loop round-robin over the list, one GET per second,
until Ctrl+C; per-minute line `requests=<n> failures=<n>` (non-2xx/404 or exception). GET only,
so no form is ever posted. Presenter starts it after go-live (before go-live it adds noise to the
local traces). Alternatives: k6 or another load tool (new toolchain for attendees); a Node
script (the repo's tools are .NET file-based apps, ADR-0012); AppHost-hosted generator
(always on, pollutes the local acts).

### D6: Smoke check additions (`tests/smoke/smoke.cs`, apphost target)
After `lgtm` start and the Home request: Grafana `/api/search` lists uids `demo-site` and the
19924 uid (PASS/FAIL "dashboards provisioned"); Prometheus query via the Grafana datasource
proxy `http_server_request_duration_seconds_count{job="site",acme_page_type="home"}` returns a
series ("page-type label on metric"; wait up to 30 s for export); Tempo search
`{resource.service.name="site" && span.acme.page_type="home"}` finds the Home trace ("page-type
label on span"); the service-enquiry post's span carries `acme.page_type=service` ("page-type
label on form post"); smoke GETs `/assets/rubber-duck.svg` explicitly, then Tempo has no span
with that `url.path` ("assets excluded").

### D7: Documentation
README "Start Grafana": dashboards under Dashboards > Demo (Demo site, ASP.NET Core),
traffic source start/stop. `docs/project-setup.md`: AppHost `lgtm` mounts, `tests/traffic`,
`acme.page_type`. FR-0001 and FR-0003 Status -> this change. `agent-telemetry-access` README
note "Aspire MCP has no metrics tool" unchanged.

### D8: README run-through and database login (PO 2026-10-15)
Generated `sa` password unchanged: Aspire creates it on first run and keeps it in AppHost user
secrets (`Parameters:sql-password`); port dynamic (`Resources:sql:tcp:port`, Aspire dashboard).
README "Connect to the database": server `127.0.0.1,<port>` (not `localhost`: IPv4-only container, `::1` first on Windows), user `sa`, password via
`dotnet user-secrets list --project src/SuperPower.AppHost`, `TrustServerCertificate=True`.
README "Run the demo": numbered run-through linking the existing sections (start, browse and
submit a form, local telemetry, Grafana go-live, traffic, Claude Code); no
planted problems or fixes (PLANNING.md rule). Smoke check reads the password from user secrets
as before. Alternative dropped by the PO: fixed password in AppHost `appsettings.json` (FR-0005;
needed a volume reset, no gain for the talk).

## Bar and test strategy

Bar: **prototype** (as `add-demo-foundation`). Automated: `dotnet build SuperPower.slnx`,
`dotnet test --solution SuperPower.slnx` (adds `PageTypeTag` test), smoke check on a freshly
started AppHost (apphost target with D6 checks; standalone target unchanged). Dashboard panels
via `/api/ds/query` with the traffic source running. Manual (PO): dashboards in the UI, an
exemplar click to a trace, agent per-page metric question; results in `verification.md`.

## Risks / Trade-offs

- [Image's Grafana does not read an added provider file] → D1 fallback import on
  `ResourceReadyEvent`.
- [19924 licence or source unclear] → recorded in the dashboards README before commit; if not
  redistributable, drop the file and report to the PO (demo dashboard is the lead view).
- [Dashboards empty right after go-live: LGTM holds only post-start data] → expected
  (ADR-0022); start the traffic source at go-live; panels fill within a minute or two.
- [`UmbracoRouteValues` not set for surface-controller posts] → smoke Form-post check reads the
  span; if `none`, report to the PO (spec scenario "Form post").
- [Bind mounts on Windows (presenting machine)] → paths relative to AppHost, read-only; checked
  in section 6 on the PO's Windows machine.
- [Traffic source run before go-live clutters the local trace list] → README and act plan say
  start it after go-live.

## Migration Plan

Branch `add-demo-dashboard` from `main`. Merge after section 6 and supervisor approval; PO
commits. Archive after merge; deltas target `openspec/specs/`.

## Open Questions

- None blocking; D1 provisioning path verified at the start of apply.
