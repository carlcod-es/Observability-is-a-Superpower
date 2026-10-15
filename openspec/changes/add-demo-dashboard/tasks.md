Bar: prototype (design.md "Bar and test strategy"). Sections 1 and 2 have disjoint files and
run in parallel; the contract between them is the tag `acme.page_type` / label
`acme_page_type` with values `home`, `page`, `contact`, `events`, `event`, `services`,
`service`, `none` (design.md D3, ADR-0024).

## 1. Dashboards and provisioning (worker-backend; `src/SuperPower.AppHost/`; no dependencies)

- [ ] 1.1 Add `src/SuperPower.AppHost/grafana/acme-dashboards.yaml` and the two read-only bind mounts on `lgtm` in `AppHost.cs` (design.md D1, ADR-0023), with a placeholder `demo-site.json` (one text panel); verify with `dotnet build SuperPower.slnx`, then a fresh AppHost start and `lgtm` started: Grafana `GET /api/search?query=Demo` lists `demo-site` in folder "Demo". If it does not, switch to the D1 fallback (import on `ResourceReadyEvent`), remove the mounts, and verify the same search
- [ ] 1.2 Add `src/SuperPower.AppHost/grafana/dashboards/aspnetcore-19924.json` from grafana.com dashboard 19924 (latest revision), with `${DS_PROMETHEUS}` replaced by `prometheus` and nothing else changed; add `src/SuperPower.AppHost/grafana/dashboards/README.md` with its source URL, revision and licence (stop and report if redistribution is not allowed); verify after an `lgtm` restart that it is listed and, with site traffic, its request-duration, error-rate and total-requests panels return data through `/api/ds/query` with `job=site`
- [ ] 1.3 Write `demo-site.json` per design.md D2 (four panels, literal UIDs, `[1m]` windows, `or 0 *` guard, exemplars on p95, `now-15m`, refresh 10 s); verify with traffic running (needs section 2 and task 3.1; until then the Home page requested by hand) that every panel's targets return data through `/api/ds/query` over `now-15m`, the error-rate panel returns 0 rather than no data, and the p95 query returns exemplars

## 2. Page-type label and excluded paths (worker-backend; `src/SuperPower.Web/`, `src/SuperPower.ServiceDefaults/`, `tests/SuperPower.Web.Tests/`; no dependencies)

- [ ] 2.1 Add `PageTypeTag` (tag name, `none`, `From(string? alias)`) and `PageTypeTelemetryMiddleware` in `src/SuperPower.Web/Telemetry/`, registered in `Program.cs` before `app.UseUmbraco()` (design.md D3, ADR-0024), plus an xUnit test for `From` (alias passes through, null or empty gives `none`); verify `dotnet build SuperPower.slnx` and `dotnet test --solution SuperPower.slnx` pass, and on a running AppHost the Aspire dashboard shows `acme.page_type=home` on the Home request span and `none` on a 404
- [ ] 2.2 Add `"/assets"` to `TelemetryPaths.ExcludedPrefixes` (design.md D4, ADR-0008); verify `dotnet build SuperPower.slnx` passes and on a running AppHost a Home load shows no trace for `/assets/rubber-duck.svg` in the Aspire dashboard

## 3. Traffic source (worker-backend; `tests/traffic/`; depends on nothing, checked against section 2)

- [ ] 3.1 Write `tests/traffic/traffic.cs` per design.md D5 (GET only, ~1 req/s round-robin over pages found on Home, Events list and Services list, per-minute summary, optional site URL argument); verify `dotnet run tests/traffic/traffic.cs` against a running AppHost prints its page list including at least one event and one service page and a summary line with failures=0 after a minute, and that the stored submission count is unchanged (PO runs the count query, CLAUDE.md "Scope")

## 4. Smoke check (worker-backend, shared file assigned by the Architect; `tests/smoke/smoke.cs`; depends on sections 1 and 2)

- [ ] 4.1 Add the design.md D6 checks to the apphost target (dashboards provisioned, page-type label on metric and span, `/assets` excluded) and update the header comment; verify `dotnet run tests/smoke/smoke.cs` on a freshly started AppHost reports PASS for every check, and `dotnet run tests/smoke/smoke.cs -- standalone` still passes

## 5. Documentation (Architect for `docs/`, `PLANNING.md`; README via a brief; depends on sections 1-4)

- [ ] 5.1 README "Start Grafana": dashboards under Dashboards > Demo, traffic source start/stop and "start it after go-live" (design.md D7); verify by following the README section on the running environment
- [ ] 5.2 `docs/project-setup.md` (lgtm mounts, `tests/traffic`, `acme.page_type`), FR-0001 and FR-0003 Status naming this change, PLANNING.md "Next step"; verify every path and name against the code

## 6. Acceptance (PO, recorded by the Architect in `verification.md`; depends on sections 1-5)

- [ ] 6.1 (PO) On Windows, fresh AppHost, `lgtm` started, traffic source running: both dashboards listed under Demo without import; demo dashboard panels show page-type series, 0 % errors, empty or real error logs; clicking a p95 exemplar opens a trace of that page type; verify by observation
- [ ] 6.2 (PO) Stop and restart the AppHost, start `lgtm` again: both dashboards still listed and fill with data once traffic runs; verify by observation
- [ ] 6.3 (PO) Claude Code session at repo root after go-live: ask "how long do requests to the Home page take, from metrics?"; the agent answers from the request-duration metric filtered by `acme_page_type="home"` without approval prompts (spec `agent-telemetry-access` "Agent checks a metric"); verify by observation
