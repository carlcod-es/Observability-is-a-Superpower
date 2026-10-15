## Why

`spike-aspire-probe` showed the Aspire dashboard has metrics, but the Aspire MCP server has no
metrics tool (ADR-0011). Agent-driven acts are therefore limited to traces and logs, so
questions like "is p95 latency back to normal after the fix?" or "how many visits failed
in the last 5 minutes?" can only be answered by the presenter in the dashboard. The PO wants
to know if other tools give the agent read access to metrics over MCP, and what each costs
the local Aspire developer experience, before the demo build continues.

## What Changes

- A second throwaway spike, using the same probe app as `spike-aspire-probe` (same web, API,
  database, custom and built-in metrics, slow and fail switches). The probe app itself is not
  changed; only how its telemetry is routed and stored.
- Scope decision 2026-10-12 (PO): only candidate B (Grafana LGTM + Grafana MCP) is tested.
  The PO prefers a single MCP server covering logs, traces and metrics; candidate A
  (Prometheus + Prometheus MCP, metrics only) is dropped. A was built and its set-up and
  tool findings stay in `findings.md` as a record; its code is removed from the spike.
- The local observability backend with its MCP server runs next to the Aspire dashboard
  in the same local start-up. Not instead of it.
- A start-up switch selects `none` (Collector only, baseline) or `b` (default). A short README
  in the spike folder explains how to run each option (PO request 2026-10-11).
- A findings record answering the questions below, plus a recommendation to the PO: adopt
  B alongside the Aspire MCP, adopt B instead of the Aspire MCP for telemetry, or keep
  ADR-0011 as it is.

Questions to answer for B:
1. Agent reads telemetry: can Claude Code, over the Grafana MCP only, list the available
   metrics and answer with numbers that match the dashboard for the following?
   - The request rate and error rate of the API.
   - The p95 duration of the API endpoint, before and after the slow switch.
   - The custom counter and custom histogram.
   - The database call duration.
   - Logs: the error logs of the failing requests, and how many.
   - Traces: the dominant span of one slow request.
   - One failing request correlated across its log, its trace and the error-rate metric.
2. Freshness: how long after a request until the agent can see it in metrics?
3. Local developer experience: is it still one start command? Do the dashboard's logs,
   traces and metrics stay complete and unchanged? Does the shared telemetry set-up need any
   changes? What is the extra start-up time and memory? What must an attendee install?
4. Access control: can the MCP server be limited to read-only tools, and allow-listed the
   same way as the Aspire MCP tools?
5. Fit for the talk: what does a metric look like to the agent (names, units), and does the
   local tool resemble what the production story offers (Application Insights via Azure MCP)?
6. Replacing the Aspire MCP: can the Grafana MCP replace the Aspire MCP for telemetry, and
   what is lost (resource listing and commands, console logs)? Is its tool surface (62 tools
   by default) narrowable to the telemetry tools?

Out of scope:
- Any change to `add-demo-foundation`, the Umbraco site, the repo-root `.mcp.json` /
  `.claude/settings.json`, or the shared telemetry set-up.
- Production, Application Insights, Azure MCP (named only as the comparison point for
  question 5).
- Hosted or paid observability services.
- Tests and cross-platform runs (throwaway bar). Cross-platform risks are recorded,
  not tested.

## Capabilities

### New Capabilities
None. This change produces findings, not product behaviour (`skip_specs: true`).

### Modified Capabilities
None.

## Impact

- New spike folder next to the existing probe. It reuses the probe app and does not modify
  it. `spike-aspire-probe` task 3.4 (delete the probe) must wait until this spike ends.
- New local container images, and possibly one AppHost hosting package: PO approval
  needed (approved 2026-10-11). The images are listed in `design.md`.
- If B is adopted: a new ADR (or an ADR-0011 revision) and follow-up work in
  `add-demo-foundation`, both planned separately.
