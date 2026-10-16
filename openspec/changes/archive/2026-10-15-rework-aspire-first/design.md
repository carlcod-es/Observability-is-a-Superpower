## Context

See proposal.md - Why. Current state (branch `sample-site` at `bedd4c0`, ADR-0017):
- `src/SuperPower.AppHost/AppHost.cs`: `lgtm` (Grafana host port 3000, OTLP HTTP dynamic),
  `collector` (`WithAppForwarding()`, `LGTM_OTLP_URL` from `lgtm`, `WaitFor(lgtm)`),
  `grafana-mcp` (host port 8091, `--disable-write`, 22 tools, `WaitFor(lgtm)`), `site` waits
  for `collector`.
- `src/SuperPower.AppHost/collector/config.yaml`: one pipeline per signal, one `batch`
  processor, exporters `otlp/aspire` and `otlphttp/lgtm` (defaults: retry on, in-memory queue
  on, so a down LGTM gets data replayed on recovery).
- `.mcp.json`: `grafana` only. `.claude/settings.json`: `mcp__grafana` allowed.
- `tests/smoke/smoke.cs` reads the Home trace from Grafana (Loki + Tempo) and expects LGTM up.
- Aspire MCP 13.6 tools (`spike-aspire-probe` findings): `list_resources`,
  `list_console_logs`, `execute_resource_command`, `list_structured_logs`, `list_traces`,
  `list_trace_structured_logs`, `select_apphost`, `list_apphosts`, `list_integrations`,
  `doctor`, `refresh_tools`, `list_docs`, `search_docs`, `get_doc`. `spikes/` holds two more
  AppHosts until pre-talk cleanup.

Fixed by the talk: Aspire, OpenTelemetry, Claude Code with MCP. Implied gap: the Aspire MCP has
no metrics tool, so metrics stay a production-stage (Grafana) capability for the agent.

```
 site --OTLP--> collector --pipelines */aspire--> Aspire dashboard <-- aspire MCP (stdio) <--+
                     |                                                                        |
                     +--pipelines */lgtm (no retry, no queue)--> lgtm [explicit start]        Claude Code
                                                                   ^                          |
                                                    grafana-mcp :8091 (waits for lgtm) <------+
```

## Goals / Non-Goals

**Goals:**
- Local stage works with Aspire only; production stage starts with one click and holds only
  post-start telemetry (traces and logs; metric counters cumulative, PO 2026-10-15)
  (specs `local-environment`, `agent-telemetry-access`).

**Non-Goals:**
- No Grafana dashboard provisioning (`add-demo-dashboard`), no tunnel work.
- No change to the site, ServiceDefaults or instrumentation.
- No per-tool narrowing of the Grafana MCP beyond ADR-0017's flags.

## Decisions

### D1: Two MCP servers, Aspire read tools pre-approved by name (ADR-0022)
`.mcp.json` adds `"aspire": {"type": "stdio", "command": "aspire", "args": ["agent", "mcp"]}`
(same as `spikes/aspire-probe/.mcp.json`). `.claude/settings.json` allow list:
`mcp__grafana` plus `mcp__aspire__<tool>` for every Aspire tool except
`execute_resource_command`. Alternatives: allow `mcp__aspire` wholesale (commands without
prompt; PO chose prompt); Aspire CLI skills (bypass MCP permissions, ADR-0011).

### D2: `lgtm` explicit start; `grafana-mcp` follows (ADR-0022)
`lgtm` gets `.WithExplicitStart()`. `grafana-mcp` keeps `.WaitFor(lgtm)` without explicit
start, so it sits in Waiting and starts once `lgtm` is healthy: one click. `collector` drops
`.WaitFor(lgtm)`. If `LGTM_OTLP_URL` cannot resolve before `lgtm` starts (dynamic endpoint not
allocated), fall back to a fixed container-network URL `http://lgtm:4318` in the AppHost.
Alternatives: both explicit start (two clicks; fallback if Waiting misbehaves); config switch
and restart (loses telemetry, ADR-0022).

### D3: Separate LGTM pipelines, no retry, no queue (ADR-0022)
Collector config: pipelines `traces/aspire`, `metrics/aspire`, `logs/aspire` and
`traces/lgtm`, `metrics/lgtm`, `logs/lgtm`, all receiving from `otlp`; `batch/aspire` and
`batch/lgtm` processors; `otlphttp/lgtm` with `retry_on_failure.enabled: false` and
`sending_queue.enabled: false`. A failed LGTM export drops that batch; the Aspire pipelines
and the site never see the failure. Alternatives: keep one pipeline with retry off (a failing
exporter in a shared pipeline still adds latency to the shared batch; separate pipelines are
three extra lines each); filter processor toggled at go-live (needs restart).

### D4: Smoke check starts `lgtm` and proves "no earlier telemetry"
Before its first Home request the smoke check (apphost target): requests `/smoke-pre/?smoke-pre=<marker>`
(an unknown path, 404 but logged; a real page such as About us warms the content cache and
removes the first-Home-load DB spans, found in apply 2026-10-15), then runs
`aspire resource lgtm start --apphost <AppHost csproj> --non-interactive` (same CLI pattern as
its Toxiproxy commands) and waits for Grafana `/api/health` and the `grafana-mcp` port. Then the
existing checks run. New check: Loki has no line with `smoke-pre=<marker>`. If `lgtm` is
already running, the check skips the start and reports the pre-check as "not applicable".
Alternative: read the Home trace from the Aspire dashboard (no stable script API; the Aspire
MCP over stdio from a script is overkill); rejected.

### D5: Documentation
README "Connecting Claude Code": two servers, `grafana` connects after Start on `lgtm`
(`/mcp` reconnect); Grafana section: start it from the dashboard. `docs/project-setup.md`:
resources table (`lgtm` explicit start), agent access. `CLAUDE.md` Stack: Aspire MCP local,
Grafana MCP production stage, Azure dropped. ADR-0017 status notes the partial supersession.
`add-demo-foundation` tasks 8.1 and 8.2 annotated as replaced by this change's section 6.

## Bar and test strategy

Bar: **prototype** (as `add-demo-foundation`). Automated: `dotnet build SuperPower.slnx`,
`dotnet test --solution SuperPower.slnx`, smoke check on a freshly started AppHost (both
targets). Manual (PO, Claude Code session at repo root, default permission mode): the checks
in tasks.md section 6. Results in `verification.md`.

## Risks / Trade-offs

- [`list_resources` returns environment variables incl. the SQL connection string] → task 6.2;
  if so, remove `list_resources` from the allow list (it prompts) and report to the
  PO, since spec `agent-telemetry-access` expects resource state without prompts.
- [`aspire agent mcp` finds three AppHosts while `spikes/` exists] → `select_apphost` is
  pre-approved; README notes it; resolved by pre-talk cleanup of `spikes/`.
- [Waiting `grafana-mcp` or unresolved `LGTM_OTLP_URL` blocks start-up] → D2 fallbacks.
- [Collector logs an export error per batch before go-live] → accepted; collector console is
  not shown on stage.
- [LGTM start on stage is slow] → image pre-pulled (README); measured in task 2.3; if over a
  minute, presenter starts it during the slide before the go-live act.
- [Claude Code shows `grafana` failed at session start] → expected; `/mcp` reconnect after
  go-live (spec scenario "Production-stage source after start").

## Migration Plan

Work on branch `aspire-first` (from `sample-site` at `bedd4c0`). PO evaluates after section 6;
accept = merge into `sample-site`; reject = delete the branch (ADR-0022 and this change go
with it).

## Open Questions

- Exact `aspire resource <name> start` command name in Aspire CLI 13.6 (D4); verified by the
  worker, fallback `aspire resource lgtm resource-start` or the dashboard by hand.
