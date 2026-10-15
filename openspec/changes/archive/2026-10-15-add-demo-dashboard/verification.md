# Verification: add-demo-dashboard

Branch `add-demo-dashboard`. Aspire CLI/SDK 13.6.1, `grafana/otel-lgtm:0.36.0`, macOS.

## Automated (sections 1-5), 2026-10-15

- Build: `dotnet build SuperPower.slnx` 0 errors (existing NU1902/NU1903/NU1507, CS0618 warnings).
  Tests: `dotnet test --solution SuperPower.slnx` 39/39 passed (adds `PageTypeTagTests`).
- 1.1 Fresh AppHost, `aspire resource lgtm start`: `/api/search?query=Demo` lists folder "Demo" and
  `demo-site` (`folderTitle` Demo). Bind-mount provisioning works; D1 fallback not needed.
- 1.2 `aspnetcore-19924.json`: grafana.com 19924 revision 5, byte-identical to upstream after
  `${DS_PROMETHEUS}` -> `prometheus` (reviewer diff, rc 0). MIT (microsoft/aspire `src/Grafana`,
  `LICENSE.TXT`). After `lgtm` restart listed in Demo (uid `KdDACDp4z`); with traffic, Requests
  Duration p50-p99.9, Errors Rate 4XX/5XX/All (0) and Total Requests return data, `job=site`.
- 1.3 `demo-site.json` via `POST /api/ds/query`, `now-15m`, ~2.5 min traffic after `lgtm` restart:
  request rate 8 series (contact, event, events, home, none, page, service, services); error rate
  1 frame, last value 0; p95 8 series plus exemplar frame (17 rows, `trace_id`/`span_id`);
  Loki error logs 1 frame, 0 rows. Prometheus datasource `exemplarTraceIdDestinations` -> `tempo`.
- 2.1 Tempo `{ kind=server } | select(span.acme.page_type, span.url.path)`: `/` -> `home`,
  `/does-not-exist-xyz` -> `none`. Grafana Tempo used instead of the Aspire dashboard (same spans).
- 2.2 After GET `/assets/rubber-duck.svg` (200): Tempo `{ span.url.path =~ ".*rubber-duck.*" }`
  returns no traces. Prometheus `http_server_request_duration_seconds_count{job="site"}`:
  `home`/200 2, `none`/404 2, `none`/200 2 (the asset GETs; counted in metrics per D4).
- 3.1 `dotnet run tests/traffic/traffic.cs` against the AppHost: `Traffic to https://localhost:44368/,
  12 pages: / /events/ /services/ /about-us/ /contact-us/ /services/umbraco-upgrade/
  /services/seo-audit/ /services/adwords-audit/ /events/how-to-get-the-most-out-of-your-cms/
  /events/upgrading-when-is-the-best-time/ /events/migrating-to-umbraco-from-other-cmss/
  /events/umbraco-builders-day/`, then `requests=60 failures=0` per minute (4 minutes).
  Submission count before/after: owed by the PO (`SELECT COUNT(*) FROM acmeFormSubmission;`
  before and after a traffic run without the smoke check).
- 4.1 Smoke, fresh AppHost, after rework: ALL CHECKS PASSED, including
  ```
  PASS dashboards provisioned - demo-site and 19924 (KdDACDp4z) listed
  PASS page-type label on metric - acme_page_type=home count 2
  PASS page-type label on span - 1 span(s) with acme.page_type=home, e.g. trace a2cca3cb...
  PASS page-type label on form post - POST server spans: service x1
  PASS assets excluded - GET /assets/rubber-duck.svg 200; 0 span(s) with an /assets url.path
  ```
  `-- standalone`: ALL CHECKS PASSED (before the rework; the rework touches only the apphost-only
  `DashboardCheck`). First Home load showed 2 database spans (earlier runs 10); check needs > 0.
- 5.1 README "Start Grafana" and "Demo traffic" followed on a fresh AppHost: both `aspire resource
  lgtm start` forms work; both dashboards listed in Demo; traffic `requests=60 failures=0`;
  request-rate query returns series.
- 5.2 Paths and names in `docs/project-setup.md`, FR-0001, FR-0003, PLANNING.md checked against the
  code (reviewer).

## PO checks (section 6)

Windows, fresh AppHost, default permission mode. PO sign-off 2026-10-15.

### 6.1 Dashboards on go-live
- [x] Both dashboards under Dashboards > Demo without import.
- [x] Demo site: page-type series on rate and p95, error rate 0 %, error logs empty or real errors.
- [x] p95 exemplar click opens a trace of that page type.
- [x] ASP.NET Core dashboard, job `site`: request duration, error rate, total requests show data
  (spec `demo-dashboard` "Generic dashboard").

### 6.2 After AppHost restart
- [x] Both dashboards listed again; data once traffic runs.

### 6.3 Agent metric question
- [x] "how long do requests to the Home page take, from metrics?": answered from
  `http_server_request_duration_seconds` filtered by `acme_page_type="home"`, no approval prompts.

### 3.1 Submission count (PO)
- [x] Count before and after a traffic run unchanged.

### Smoke on Windows (PO run, 2026-10-15)
- Fix: `tests/smoke/smoke.cs:74` SQL server `localhost` -> `127.0.0.1` (PO). The SQL container publishes on
  127.0.0.1 only; `localhost` timed out from SqlClient on Windows.
- After the fix: pages, contact, service enquiry, event booking and invalid submission PASS.
- FAIL, accepted by the PO as pass: telemetry, dashboard checks, mail setup, mail heal. Cause: `Process.Start("aspire")`
  does not resolve the .NET-tool shim `aspire.cmd` on Windows, so the lgtm pre-start, `aspire describe` and resource
  commands fail; telemetry and dashboard checks then reach Grafana before `lgtm` is started. Not a defect in the
  change's runtime behaviour: the same items passed by hand in 6.1 and 6.2. Follow-up in `review-followups.md`.

## Section 7 (README run-through and database login)

- First plan (FR-0005, fixed password; reverted by task 7.1, PO 2026-10-15): 7.2 `src/SuperPower.AppHost/appsettings.json` `Parameters:sql-password`; smoke reads it from there (port from user
  secrets, `SMOKE_SQL_CONNECTION` override kept). `dotnet build SuperPower.slnx` 0 errors; `dotnet build
  tests/smoke/smoke.cs` succeeded. Reviewer APPROVED 2026-10-15.
- 7.3 FR-0005 Status Rejected; ADR-0009 amendment removed (file equals HEAD); proposal and spec `local-environment`
  delta ("Documented database login", "Demo run-through") updated; `openspec validate add-demo-dashboard` valid.
  `grep -rn "FR-0005\|SuperPower!Demo"` over docs, the change, README, src, tests: only the FR, the re-plan and the
  rejection records.
- 7.1 Reverted: `git diff main -- src/SuperPower.AppHost/appsettings.json` empty; smoke password from user secrets
  again (no password lines in `git diff main -- tests/smoke/smoke.cs`); both builds 0 errors. Reviewer APPROVED
  2026-10-15.
- 7.2 README "Run the demo" and "Connect to the database": anchors, ports, resource/database/table names and
  user-secrets keys checked against the code (reviewer APPROVED 2026-10-15; two minors fixed). Followed both
  sections on a running AppHost on Windows: PO 2026-10-15, passed.
