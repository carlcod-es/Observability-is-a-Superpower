# ADR-0033: Grafana from the start of the talk, Grafana MCP as the agent's only stage source

## Status
Accepted (2026-10-23). Supersedes ADR-0022's stage split, two-server registration and explicit
`lgtm` start.

## Context
ADR-0022 gave the agent the Aspire MCP before go-live and the Grafana MCP after, so the Act 1
personal-data leak never reached "production". The PO (2026-10-22) wants Grafana running for the
whole talk and the agent on one source in every act: the Aspire dashboard stays the presenter's
view for logs, traces and metrics, and the agent no longer spends queries on a second server.
The PO accepts that Act 1's unmasked values reach Grafana and stay there (made-up details only).
Attendees should still be able to use the Aspire MCP. The PO (2026-10-23) wants a fresh clone to
match the talk with no disable step; the Aspire MCP becomes opt-in.

## Decision
- `.mcp.json` registers only `grafana` (PO 2026-10-23). `.claude/settings.json` keeps the
  `mcp__aspire__*` allow entries, so the Aspire read tools are pre-approved once enabled.
- `lgtm` starts with the AppHost (no `WithExplicitStart`, PO 2026-10-23); `collector` waits for `lgtm`,
  and `site` and `api` wait for `collector`, so start-up telemetry reaches Grafana as well as the Aspire
  dashboard (the LGTM exporter has no retry). `grafana-mcp` waits for `lgtm`. The smoke check's
  pre-start check is removed.
- On stage: Grafana runs from the morning's final AppHost start; Claude Code is opened after
  `grafana-mcp` is Running. The agent uses only `grafana` from the first act.
- The stage agent instructions (`CLAUDE.md`) name only the Grafana MCP and tell the agent to
  ask the presenter for any restart; it must not restart services itself.
- README section "Enable the Aspire MCP": `claude mcp add --scope local aspire -- aspire agent mcp`,
  or add the block back into `.mcp.json`.

## Alternatives
- Keep `aspire` in `.mcp.json` and disable it per folder with `/mcp` (this ADR as first accepted,
  PO 2026-10-22): a fresh clone does not match the talk and needs a disable step;
  replaced by the PO 2026-10-23.
- Keep `aspire` registered and turn it off with `disabledMcpjsonServers` in `.claude/settings.json`:
  conflicts with `enableAllProjectMcpServers`, and settings lists merge across scopes, so a local
  setting may not re-enable it; rejected.
- `lgtm` explicit-start (ADR-0022, this ADR as first accepted): a manual start step every morning
  and for attendees, and telemetry from before it is never in Grafana; replaced by the PO
  2026-10-23. Cost accepted: heavier start, `site` starts later by `lgtm`'s time to healthy.
- `lgtm` and `collector` started last, after `site`: start-up telemetry lost in both dashboards
  (`WithAppForwarding` routes all app telemetry through the collector); rejected (PO 2026-10-23).
- Keep ADR-0022's split: two sources on stage, agent queries split by act; rejected by the PO.
- Wipe Grafana after Act 1 (restart `lgtm`): extra stage step, loses Act 1 evidence; rejected
  (PO 2026-10-22).

## Consequences
- Act 1's leaked values stay in Loki for the talk; any later log query over that range can show
  them. Presenter uses made-up details.
- Go-live is the tunnel and demo traffic only; "Grafana holds only telemetry from go-live" no
  longer applies anywhere: Grafana holds telemetry from the AppHost start.
- After a fix the agent asks; the presenter rebuilds the resource in the Aspire dashboard (Restart does
  not recompile).
- Metrics are queried from Grafana in every act (the Aspire MCP had none), so local-stage check
  queries can use counters.
- The talk no longer demonstrates the Aspire MCP; the README states how to enable it.
- Dev clones and attendees who want the Aspire MCP (the agent path without `lgtm`) run one
  `claude mcp add` command.
