## Why

Before building more of the demo, the PO wants evidence of what the local Aspire dashboard
and the Aspire MCP server actually give a developer and a coding agent: logs, traces and
metrics in the dashboard, and which of them the agent can read. The Umbraco site is too heavy
to iterate on for this, so a minimal probe app that behaves like it at the telemetry level
answers the question quickly. `add-demo-foundation` is parked meanwhile (no changes to it).

## What Changes

- A throwaway probe in `spikes/aspire-probe/`, outside the demo solution, deleted once the
  findings are recorded. Never published to attendees.
- The probe: a web page that calls a small API, which reads and writes a database, plus
  custom spans, custom metrics, structured logs, console output, a failure switch and a slow
  switch. It uses the demo's own service defaults, so findings carry over to the Umbraco site.
- A findings record answering the questions below, fed back into ADR-0011 if it changes
  anything.

Questions to answer:
1. Dashboard: do structured logs, traces and metrics (built-in and custom) appear for every
   service, are logs linked to their trace, and does one trace span web, API and database?
2. Agent over Aspire MCP: can Claude Code list resources, read console output, find the
   failing and the slow request from traces, and read that trace's logs? Confirm whether any
   metrics are reachable.

Out of scope:
- Any change to `add-demo-foundation`, the Umbraco site, or the repo-root `.mcp.json` /
  `.claude/settings.json`.
- Standalone dashboard, production / Application Insights, Azure MCP.
- Tests, README, cross-platform checks (throwaway bar).

## Capabilities

None. No specs; this change produces findings, not product behaviour.

## Impact

- New folder `spikes/aspire-probe/` (own `.slnx`, not in `SuperPower.slnx`), removed at the
  end of the change.
- One new package version in `Directory.Packages.props`: `Microsoft.Data.SqlClient` 6.1.6
  (PO approved 2026-10-11; same version Umbraco resolves). Removed with the probe if nothing
  else uses it.
- References `src/SuperPower.ServiceDefaults` read-only; no edits to it.
- Possible update to ADR-0011 Consequences.
