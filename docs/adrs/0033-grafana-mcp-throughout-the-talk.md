# ADR-0033: Grafana from the start of the talk, Grafana MCP as the agent's only stage source

## Status
Accepted (2026-10-23, change `extend-demo`). Deviation: the presenter applies a fix with Rebuild, not
Restart (Aspire's Restart does not recompile). Supersedes the stage split of ADR-0022 for the talk.
ADR-0022's repo setup (both MCP servers registered, `lgtm` explicit-start) stays.

## Context
ADR-0022 gave the agent the Aspire MCP before go-live and the Grafana MCP after, so the Act 1
personal-data leak never reached "production". The PO (2026-10-22) wants Grafana running for the
whole talk and the agent on one source in every act: the Aspire dashboard stays the presenter's
view for logs, traces and metrics, and the agent no longer spends queries on a second server.
The PO accepts that Act 1's unmasked values reach Grafana and stay there (made-up details only).
Attendees should still be able to use the Aspire MCP.

## Decision
- Repo unchanged: `.mcp.json` keeps `aspire` and `grafana`; `.claude/settings.json` unchanged;
  `lgtm` stays explicit-start in the AppHost.
- Stage clone: the presenter starts `lgtm` in the morning after the final AppHost restart, before
  opening Claude Code, and disables `aspire` with `/mcp` (saved per folder). The agent uses only
  `grafana` from the first act.
- The stage agent instructions (`claude-stage.md`) name only the Grafana MCP and tell the agent to
  ask the presenter for any restart; it must not restart services itself.
- README gains a section on matching the talk's setup (disable `aspire` for the folder).

## Alternatives
- Remove `aspire` from `.mcp.json`: attendees lose the lighter agent path (no `lgtm` needed) and
  the dev clone loses it too; rejected (PO 2026-10-22).
- `lgtm` started by `aspire start` (not explicit-start): heavier start for attendees and the
  smoke pre-start check loses its meaning; the presenter starts it anyway; rejected.
- Keep ADR-0022's split: two sources on stage, agent queries split by act; rejected by the PO.
- Wipe Grafana after Act 1 (restart `lgtm`): extra stage step, loses Act 1 evidence; rejected
  (PO 2026-10-22).

## Consequences
- Act 1's leaked values stay in Loki for the talk; any later log query over that range can show
  them. Presenter uses made-up details.
- Go-live is the tunnel and demo traffic only; "Grafana holds only telemetry from go-live" no
  longer applies on stage. The smoke pre-start check is not applicable on stage.
- After a fix the agent asks; the presenter rebuilds the resource in the Aspire dashboard (Restart does
  not recompile).
- Metrics are queried from Grafana in every act (the Aspire MCP had none), so local-stage check
  queries can use counters.
- The talk no longer demonstrates the Aspire MCP; the README states it is available.
