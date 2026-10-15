## Context

- `spikes/metrics-mcp/` mode `b` (`aspire run`, default) runs the probe apps, the Collector
  and `grafana/otel-lgtm`; Grafana is on host port 3000 with the LGTM default credentials
  (`spike-metrics-mcp` D3, D7). All three signals are present and verified
  (`spike-metrics-mcp` `findings.md`).
- Metric names as stored: `http_server_request_duration_seconds_*`, `probe_visits_total`,
  `probe_visit_duration_milliseconds_*`; `service.name` → `service_name`/`job`
  (`spike-metrics-mcp` `findings.md`, "Metric names" record).
- `load.mjs` generates normal, slow and failing traffic; the probe's `slow`/`fail` switches
  are still available.
- The adopted Grafana MCP surface (ADR-0017) is telemetry-only: no dashboard tools. The
  on-stage agent cannot read or edit dashboards; dashboards are a presenter/audience surface.
- The PO has no Grafana experience and does not want to build dashboards - they present a
  complete one (PO 2026-10-14). All building is Architect/agent work.

## Goals / Non-Goals

**Goals:**
- Answer the proposal's five questions with evidence, inside the existing mode `b`
  environment, changing nothing about how telemetry is produced or stored.
- Leave behind an exported basic dashboard and findings the demo dashboard can be
  planned from.

**Non-Goals:**
- Choosing or building the demo's dashboard (PO 2026-10-14: "probably more" than this,
  planned later, ADR then).
- Alerting, template variables, folders/permissions, Grafana configuration changes.
- Any change to the probe apps, Collector config, `.mcp.json` or MCP flags.

## Decisions

### D1. Work inside `spikes/metrics-mcp`, no new spike folder
Dashboard work is entirely Grafana-side; the existing AppHost, Collector and LGTM container
are reused unchanged. Exported dashboard JSON goes in `spikes/metrics-mcp/dashboards/`, and
the spike README gets a short "Dashboards" section. The pre-talk cleanup
(`spike-metrics-mcp` task 3.6) already deletes the whole folder, so nothing new to track.

Alternative: a new `spikes/grafana-dashboard/` folder with its own AppHost referencing the
same probe. Rejected: it would duplicate the AppHost and Collector config to run an
identical environment ("keep it simple").

### D2. Two dashboards, built two ways
1. Community import: ASP.NET Core dashboards grafana.com IDs 19924 + 19925, imported via
   the Grafana UI (import by ID needs internet at import time; downloading the JSON first is
   the offline fallback). Answers proposal question 1 as-is - no query edits; if panels stay
   empty, record which and the apparent cause (name translation, missing labels) rather
   than fixing them.
2. Custom basic dashboard, authored by the Architect/agent as dashboard JSON in the repo
   (`spikes/metrics-mcp/dashboards/`), then loaded into Grafana and iterated until every
   panel renders (proposal question 2): request rate, error rate, p95 duration (from
   `http_server_request_duration_seconds_*`), `probe_visits_total`,
   `probe_visit_duration_milliseconds_*`, and a Loki panel filtered to error logs. Loading
   uses the Grafana UI import or the Grafana HTTP API with the LGTM admin credentials -
   spike-local only; the narrowed MCP (ADR-0017) has no dashboard tools and is not touched.
   Effort and iteration count are the findings.

Alternatives:
- The PO builds it in the Grafana UI (a novice test). Rejected: PO 2026-10-14 - they
  present a complete dashboard, they do not build one.
- The Architect builds it click-by-click in the UI and exports. Viable fallback if
  JSON-first produces broken panels; the findings record which route won. JSON-first is
  preferred because the artefact then lives in the repo from the start and matches how the
  demo would ship a dashboard.

### D3. Persistence by export, provisioning as the stretch
`grafana/otel-lgtm` stores dashboards inside the container; a restart loses them. The spike
answers proposal question 4 in two steps:
1. The custom dashboard's JSON already lives in `spikes/metrics-mcp/dashboards/` (D2.2);
   the community dashboards are exported there too ("Export for sharing externally") if
   kept. Restart the LGTM container, re-import from the files, confirm they render. This
   is the baseline answer and is enough for the demo decision.
2. Stretch, only if 1 is quick: mount the JSON via Grafana's dashboard provisioning
   directory so the dashboard exists on start-up with no manual step, and record the LGTM
   image's provisioning path. If this is fiddly, record the obstacle and stop - the demo
   plan can resolve it.

Known catch to record either way: exported JSON carries datasource UIDs; "export for
sharing" replaces them with an input variable. The findings note which export form
re-imports cleanly.

### D4. Investigation method
As the previous spikes: `load.mjs` for steady traffic, then the `slow`/`fail` switches for
proposal question 3 (read the dashboard during the fault, screenshot or note what changes).
Every answer lands in `findings.md` with how it was observed; the Architect records, the
PO reviews the finished dashboards in the UI.

### Bar and ADRs
- Bar: throwaway. No tests, no cross-platform runs; observations recorded in `findings.md`.
  The README "Dashboards" section is the same documentation exception as
  `spike-metrics-mcp` D7.
- No ADR in this change. If the PO adopts a demo dashboard, that gets its own ADR and
  `add-demo-foundation` follow-up, planned separately (proposal, Impact).

## Risks / Trade-offs

- [Community dashboards expect scrape-style names or labels our OTLP-ingested data lacks]
  → Expected to partially apply; empty panels are themselves a finding (question 1), not a
  defect to fix.
- [Agent-authored dashboard JSON has schema errors or blank panels] → Iterate against the
  UI and record the iteration count (that effort is the question 2 finding); fall back to
  UI-build-then-export (D2) if JSON-first stalls.
- [Import/re-import breaks on datasource UIDs in authored or exported JSON] → Use the
  "export for sharing externally" shape (datasource as input variable); record which form
  works (D3).
- [Grafana version in the pinned LGTM image differs from grafana.com docs/screenshots] →
  Record the Grafana version in the findings alongside the answers.
- [Dashboard work tempts scope growth (variables, alerts, polish)] → Non-goals; the demo
  dashboard is a later, separate plan.

## Open Questions

None. Question 4's provisioning stretch is deliberately allowed to end unresolved
(D3 step 2 records the obstacle instead).
