Bar: throwaway. No tests; each task is verified by observation and recorded in `findings.md`.
Exception: the `spikes/metrics-mcp/README.md` "Dashboards" section (task 4.3, design D1).
Almost all work happens in the Grafana UI, not the repo; the only repo artefacts are
exported dashboard JSON under `spikes/metrics-mcp/dashboards/` and the README section.

## 1. Preparation

Owner: Architect. Depends on: none (reuses `spikes/metrics-mcp` mode `b` unchanged).

- [ ] 1.1 Start the spike environment (`aspire run` in `spikes/metrics-mcp/`, default mode `b`), run `node load.mjs` for baseline traffic, and record in `findings.md` the Grafana version of the pinned LGTM image (design, Risks); verify Grafana at `http://localhost:3000` shows the probe metrics, logs and traces in Explore

## 2. Community import (proposal question 1)

Owner: Architect, PO observing. Depends on: 1.

- [ ] 2.1 Import grafana.com dashboards 19924 (ASP.NET Core) and 19925 (ASP.NET Core Endpoint) via the Grafana UI, by ID or downloaded JSON (design D2.1), choosing the LGTM Prometheus datasource when prompted; verify both open without error and record in `findings.md` which panels show data and which stay empty, with the apparent cause for each empty panel - no query edits
- [ ] 2.2 Record in `findings.md` the answer to proposal question 1: usable as-is / partially / not, with a screenshot or panel list as evidence; verify the PO has seen the result in the UI

## 3. Custom dashboard authoring (proposal question 2)

Owner: Architect. Depends on: 1. Independent of section 2.

- [ ] 3.1 Author the custom dashboard as JSON in `spikes/metrics-mcp/dashboards/` with the design D2.2 panels - request rate, error rate, p95 request duration, `probe_visits_total`, `probe_visit_duration_milliseconds` and a Loki panel filtered to error logs - and load it via the Grafana UI import or HTTP API (LGTM admin credentials, spike-local only; design D2.2); verify each panel shows data matching Explore, iterating the JSON until it does (fallback: build in the UI and export, recording the switch; design D2)
- [ ] 3.2 Record in `findings.md` the answer to proposal question 2: effort and iteration count to a fully rendering dashboard, which route won (JSON-first or UI-then-export), and what authoring needed to know (name translation, units, histogram quantiles, datasource UIDs); verify the PO has reviewed the finished dashboard in the UI

## 4. Faults, persistence and README (proposal questions 3 and 4)

Owner: Architect, PO observing the dashboard. Depends on: 3 (uses the custom dashboard;
2's imports are included if usable).

- [ ] 4.1 With the custom dashboard open, turn on the probe's `slow` then `fail` switches under `load.mjs` traffic (design D4); verify the dashboard visibly changes (p95 rise, error-rate rise, error logs appearing) and record in `findings.md` what was visible without the agent, with screenshots or notes per fault
- [ ] 4.2 Ensure every kept dashboard exists as JSON in `spikes/metrics-mcp/dashboards/` (the custom one is already there from 3.1; export the community imports if kept), restart the LGTM container, re-import from the files (design D3 step 1); verify the re-imported dashboards render with data and record in `findings.md` which JSON form handled datasource UIDs cleanly
- [ ] 4.3 Add a "Dashboards" section to `spikes/metrics-mcp/README.md`: the files in `dashboards/`, how to import them, the Grafana URL and credentials pointer; verify a re-import following only the README succeeds
- [ ] 4.4 Stretch, only if 4.2 was quick (design D3 step 2): provision the exported JSON via the LGTM image's Grafana provisioning directory so the dashboard exists after a plain start; verify it appears without manual import, or record the obstacle in `findings.md` and stop

## 5. Findings and sign-off (proposal question 5 and recommendation)

Owner: Architect. Depends on: 2, 3, 4.

- [ ] 5.1 Record in `findings.md` the answer to proposal question 5: confirm the Collector config, `.mcp.json` and MCP flags are untouched (read-only git status over `spikes/metrics-mcp/` except `dashboards/` and the README) and note that the narrowed MCP surface has no dashboard tools, so dashboards stay a presenter/audience surface (design, Context); verify with the git output in the findings
- [ ] 5.2 Write the recommendation to the PO in `findings.md`: what the demo's dashboard should contain, built how (UI vs JSON, import vs custom), persisted how, and what the follow-up plan would touch (ADR + `add-demo-foundation`, proposal Impact); verify the PO signs off and `PLANNING.md` is updated (spike result, and the pre-talk cleanup todo now covers `dashboards/`)
