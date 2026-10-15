## Why

The talk is to tell a two-stage story: first fix bugs on the presenter's machine with only what
.NET Aspire provides (dashboard plus the Aspire MCP for the agent), then "go live": open the
site to the audience and introduce a separate, production-style observability platform
(Grafana) that the agent reads read-only. There is no time to build a real production
environment, so the whole talk runs locally and Grafana plays production (Azure dropped, PO
2026-10-15). Today the agent only has the Grafana MCP and Grafana receives every signal from
start-up, so the local acts' test data, including the planted personal-data leak, would sit in
"production" before go-live. Built on branch `aspire-first`; the PO evaluates the result and
may abandon the branch.

## What Changes

- Local stage: the agent reads traces, structured logs (including a trace's logs), resource
  state and console logs from the Aspire environment through the Aspire MCP. Resource commands
  (mail-path faults) need approval every time; the presenter runs them from the dashboard.
  Metrics at this stage are shown by the presenter in the dashboard, not read by the agent.
- Production stage: the Grafana platform and its MCP server exist in the environment but do
  not run at start. The presenter starts them with one action from the dashboard during the
  talk, without restarting the environment or the site.
- Grafana holds only telemetry produced after it was started: nothing from the local acts is
  buffered and replayed into it. Exception: metric counters are cumulative, so their first
  values after the start include earlier totals (PO 2026-10-15).
- Once started, the agent has both servers: Aspire for "my machine", Grafana (read-only,
  traces, logs and metrics) for "production".
- **BREAKING** (spec `agent-telemetry-access`): the rule that the agent cannot read resource
  state is relaxed to read-only resource state and console logs; resource commands stay
  behind an approval prompt. No database access, unchanged.
- The automated smoke check keeps checking the Home trace, adapted to the on-demand platform.
- Attendee README and project-setup doc describe the two stages.

Out of scope (later changes on this branch or after): the provisioned Grafana dashboard
(`add-demo-dashboard`), the Cloudflare tunnel and database reset (`add-production-environment`,
now tunnel only), the act plan itself, deleting `spikes/`.

## Capabilities

### New Capabilities
<!-- none -->

### Modified Capabilities
Both capabilities are defined in the unarchived change `add-demo-foundation`; this change
modifies them and is archived after it.
- `agent-telemetry-access`: two telemetry sources by stage; local resource state and console
  logs readable; commands prompt; production-stage telemetry read-only.
- `local-environment`: one-command start excludes the production-stage platform; new
  requirement for starting it on demand with no earlier telemetry.

## Impact

- Environment definition (which resources start, telemetry fan-out), agent configuration
  committed in the repo, smoke check, README, project-setup doc, `CLAUDE.md` Stack line.
- Supersedes part of ADR-0017 (single Grafana MCP server); new ADR.
- `add-demo-foundation` tasks 8.1 and 8.2 (agent checks) are replaced by this change's checks.
- Attendees need the Aspire CLI to connect the agent (already a prerequisite to run the
  environment).
