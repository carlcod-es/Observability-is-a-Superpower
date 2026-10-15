# ADR-0022: Aspire MCP for the local stage, Grafana started on demand as production

## Status
Proposed (2026-10-15, change `rework-aspire-first`, branch `aspire-first`); supersedes the
"one MCP server" and "Grafana runs from start-up" parts of ADR-0017. Abandoned with the branch
if the PO rejects the rework.

## Context
The PO's talk story (2026-10-15): fix bugs locally with Aspire only, then go live through the
Cloudflare tunnel with Grafana playing production. No real production environment (Azure
dropped). ADR-0017 gives the agent only the Grafana MCP, and the Collector sends every signal
to Grafana LGTM from start-up, so the local acts' data, including the planted personal-data
leak, would already be in "production". The Aspire MCP (`aspire agent mcp`, stdio, 14 tools,
no metrics; `spike-aspire-probe` findings) was the original agent server (ADR-0011).

## Decision
- `.mcp.json` registers two servers: `aspire` (stdio, `aspire agent mcp`) and `grafana`
  (unchanged, `http://localhost:8091/mcp`).
- `.claude/settings.json` pre-approves the Aspire MCP read tools by name (traces, structured
  logs, trace logs, resources, console logs, AppHost selection, integrations, docs) and all
  Grafana MCP tools (read-only by flags, ADR-0017). `execute_resource_command` is not
  pre-approved: it prompts each call.
- `lgtm` is an explicit-start resource: defined, not started by `aspire run`. `grafana-mcp`
  waits for `lgtm`, so one Start click on `lgtm` in the dashboard brings both up. The
  Collector no longer waits for `lgtm`.
- The Collector's LGTM export has no retry and no sending queue, in its own pipelines with
  their own batch processor: data produced while LGTM is down is dropped, never replayed, and
  the Aspire dashboard path is unaffected.

## Alternatives
- Grafana MCP only, Grafana from start-up (ADR-0017): local-act data in "production"; rejected.
- Aspire MCP only: no metrics or production stage for the agent; rejected.
- Separate AppHost profile or config switch for Grafana: needs an AppHost restart on stage,
  losing the dashboard's telemetry; rejected.
- Grafana running from start with a Collector filter switched off at go-live: runtime toggle
  the Collector does not offer without a restart or extra component; rejected.
- Aspire CLI skills instead of the Aspire MCP: act through shell commands, bypassing MCP
  permissions (ADR-0011); rejected.

## Consequences
- The agent reads resource state and console logs at the local stage; database credentials
  must not appear in pre-approved tool output (verified in the change's checks).
- Before go-live the Collector logs export errors for LGTM; expected, not shown on stage.
- Traces and logs from before go-live never reach LGTM. Metrics are cumulative: counters
  exported after go-live include totals from before it (accepted by the PO 2026-10-15; delta
  temporality or a reset step would add machinery the demo does not need).
- Claude Code starts with `grafana` disconnected until go-live; `/mcp` reconnects it.
- The smoke check starts `lgtm` itself before reading telemetry from Grafana.
- Attendees need the Aspire CLI for the agent (already required to run the AppHost).
