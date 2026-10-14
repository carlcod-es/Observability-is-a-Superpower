## Why

The demo will show the site with a Grafana dashboard displaying common information, but the
PO has no experience with Grafana as a platform. Before planning a demo dashboard, learn
what a basic dashboard can show over the telemetry we already collect, how one is built,
and what it costs to keep. The grafana spike (`spike-metrics-mcp`) proved the LGTM stack
and the data; this spike builds on it.

## What Changes

- A third throwaway spike, reusing the `spikes/metrics-mcp` environment unchanged (probe
  app + Collector + LGTM, mode `b`). Only Grafana-side dashboard work; no app, Collector
  or MCP changes.
- A basic dashboard is built over the probe's existing telemetry, by the Architect/agent,
  not the PO (PO 2026-10-14: the PO presents a complete dashboard, they do not build one).
- The community ASP.NET Core dashboards (grafana.com IDs 19924/19925) are imported as a
  baseline comparison.
- A findings record answering the questions below, plus a recommendation to the PO on what
  the demo's dashboard should contain. The demo dashboard itself is planned later
  ("probably more" than this basic one, PO 2026-10-14).

Questions to answer:
1. Community import: do the ASP.NET Core dashboards (IDs 19924/19925) light up against
   our LGTM data as-is?
2. Authoring: can a basic custom dashboard - request rate, error rate, p95 duration, the
   custom counter and histogram, a Loki error-logs panel - be authored as dashboard JSON
   and loaded into Grafana so every panel renders correctly, and how much effort and
   iteration does that take?
3. Faults: what does the dashboard show when the probe's slow and fail switches are on;
   do the faults become visible without the agent?
4. Persistence: can the dashboard be exported and stored so it survives a restart of the
   LGTM container and could ship in the repo?
5. ADR-0017 safety: does any of this touch or risk the adopted set-up (Collector config,
   narrowed read-only Grafana MCP)?

Out of scope:
- The Umbraco site, `add-demo-foundation`, demo-act planning.
- The repo-root `.mcp.json`, `.claude/settings.json`, shared telemetry set-up.
- Production and Application Insights.
- Anything beyond one basic dashboard (alerting, template variables, multiple dashboards
  beyond the community import comparison).
- Tests and cross-platform runs (throwaway bar). Cross-platform risks are recorded, not
  tested.

## Capabilities

### New Capabilities
None. This change produces findings, not product behaviour (`skip_specs: true`).

### Modified Capabilities
None.

## Impact

- Dashboard artefacts inside the existing `spikes/metrics-mcp` LGTM environment (and, if
  question 4 says yes, exported dashboard files in the spike folder). The probe app,
  Collector config and MCP set-up are not modified.
- Extends the life of `spikes/metrics-mcp`: the pre-talk cleanup (its task 3.6, tracked in
  `PLANNING.md` Todos) must also cover whatever this spike leaves behind.
- If a demo dashboard is adopted: an ADR and follow-up work in `add-demo-foundation`,
  both planned separately.
