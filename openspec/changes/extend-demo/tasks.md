Bar: demo (design.md Context). Order: 1 and 2 in parallel (disjoint files; metric name fixed in
design.md D2) -> 3 -> 4 -> 5 (PO). The Act 3 planted problem is given in the section 1 brief only.

## 1. Booking service telemetry and Act 3 toggle (worker-backend; `src/SuperPower.Api/`, `src/SuperPower.ServiceDefaults/Extensions.cs` metrics registration only; no dependencies)

- [x] 1.1 Consumer catch marks the deliver span as an error with the exception before the existing log and nack (design.md D1, ADR-0034); verify `dotnet build SuperPower.slnx` passes
- [x] 1.2 Add meter `Acme.Bookings` and counter `acme.bookings.counted` in `api`, `Add(1)` after the cache increment succeeds, `Add(0)` once at consumer start; `.AddMeter("Acme.Bookings")` in ServiceDefaults (design.md D2, ADR-0034); verify `dotnet build SuperPower.slnx` passes
- [x] 1.3 Add the `// Demo Code : Act 3` toggle as given in the brief, committed in the fixed state (design.md D6); with the AppHost and `lgtm` running: (a) fixed: book an event, count rises within 5 s, Grafana Prometheus has `acme_bookings_counted_total{job="api"}` (report the exact series name and labels); (b) initial state, restart `api`, book: confirmation and owner email as normal, count unchanged, Tempo booking trace shows `api` `deliver booking-stored` with error status and the exception, Loki has the `api` error entry, counter unchanged; (c) fixed again, restart `api`, book: count rises by one. Verify by reporting each observation and that the file is back in the fixed state (`git diff` shows only 1.1-1.3 changes)

## 2. Demo dashboard panel (worker-backend; `src/SuperPower.AppHost/grafana/dashboards/demo-site.json`; contract design.md D2, D3; no file dependency on 1)

- [x] 2.1 Shrink "Form submissions by outcome" to the left half and add "Bookings: stored vs counted" as design.md D3; verify the JSON parses (`node -e "require('./src/SuperPower.AppHost/grafana/dashboards/demo-site.json')"`) and, after section 1 is merged, with `lgtm` running and bookings made in the fixed and the planted state, the panel shows matching lines when fixed and counted at 0 when planted (spec `demo-dashboard`, "Bookings counted", "Bookings lost"); report observations

## 3. Smoke check and README (worker-backend by brief; shared: `tests/smoke/smoke.cs`, `README.md`; depends on 1)

- [x] 3.1 Smoke `-- running` mode as design.md D5: skips the first-load and pre-start checks (printed `N/A`), no `/smoke-pre/` request; header documents it without naming acts or problems; verify on an AppHost that has served pages: `dotnet run tests/smoke/smoke.cs -- running` reports `ALL CHECKS PASSED` with two N/A lines
- [x] 3.2 Smoke check: counted-bookings metric increases within 60 s of the smoke booking (apphost, both modes; design.md D5); verify `dotnet run tests/smoke/smoke.cs` on a freshly started AppHost passes with the new check (existing expected results unchanged) and `-- standalone` is unchanged
- [ ] 3.3 README: section "Match the talk's agent setup" (design.md D4, ADR-0033); "Tests" lists `-- running`; no planted problems described; verify the `/mcp` steps on macOS: after Disable, `claude mcp list` shows `aspire` disabled for the folder and `grafana` connected with `lgtm` running

## 4. Docs, stage instructions and runthrough (Architect; `docs/`, `openspec/`, `CLAUDE.md`, `PLANNING.md`, `claude-stage.md`, `.temp/runthrough.md`; depends on 1-3)

- [x] 4.1 `claude-stage.md` per design.md D4 (Grafana MCP only, ask the presenter to restart, resources incl. `api`, `cache`, `queue`); no planted problem or fix details; verify by reading it against spec `agent-telemetry-access` "Agent works from the production-stage platform alone"
- [x] 4.2 `.temp/runthrough.md` per design.md D7 (Act 3 row and entry, Morning reorder with smoke #1 before initial states, `lgtm` from the morning, `aspire` disabled, Go-live tunnel and traffic only, Act 1 and check queries on Grafana, Act 3 section, rule no bookings before Act 3, Close smoke #2); verify every command in changed steps runs as written on the Mac
- [x] 4.3 CLAUDE.md "Stack" and "Telemetry" (Grafana MCP throughout, counted metric, Act 3), `openspec/config.yaml` context, ADR-0033 and ADR-0034 Status -> Accepted with deviations, PLANNING.md change order and todos (replace the queue-errors todo); talk date unchanged (PO 2026-10-22); verify `openspec validate extend-demo --strict` passes and no doc outside the runthrough and the code comment describes the planted problem

## 5. Rehearsal (PO; depends on 4)

- [ ] 5.1 (PO) Copy `.temp/runthrough.md` to `docs/presenter/`; run Morning in a stage clone, then Act 3 with the agent on the Grafana MCP only; verify the agent finds the failure from Grafana, the fix restores counting, and `dotnet run tests/smoke/smoke.cs -- running` passes at the end
