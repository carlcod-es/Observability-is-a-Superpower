## 1. Decision records and planning (Architect)

Owner: Architect. Depends on: none. ADR-0022; design D5.

- [x] 1.1 Set ADR-0017 Status to note the partial supersession by ADR-0022 (one MCP server, Grafana from start-up); update `CLAUDE.md` Stack (Aspire MCP local stage, Grafana MCP production stage started on demand, Azure dropped; the "No Aspire MCP" sentence removed) and `PLANNING.md` (this change, `add-production-environment` tunnel only); verify `git grep -n "No Aspire" CLAUDE.md` finds nothing and ADR-0017, ADR-0022 and `CLAUDE.md` agree on both servers
- [x] 1.2 Annotate `add-demo-foundation` tasks 8.1 and 8.2 as replaced by `rework-aspire-first` section 6 (left unticked; closed when this change is accepted); verify both lines name this change

## 2. Environment and telemetry fan-out (`src/SuperPower.AppHost/`)

Owner: worker-backend. Depends on: none. Spec `local-environment`; ADR-0022; design D2, D3.

- [x] 2.1 Make `lgtm` explicit start, remove the collector's `WaitFor(lgtm)`, keep `grafana-mcp` `WaitFor(lgtm)` (design D2, with its fallbacks if start-up blocks); verify after `aspire run --apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj` that every resource except `lgtm` and `grafana-mcp` is Running, `lgtm` shows Not started, the site serves Home, and `aspire describe` output is quoted in the report
- [x] 2.2 Split `collector/config.yaml` into `*/aspire` and `*/lgtm` pipelines with separate batch processors and `otlphttp/lgtm` retry and sending queue disabled (design D3); verify with the AppHost running: submit no form, load About us before starting `lgtm`, start `lgtm` from the CLI or dashboard, load Home, then Loki (`http://localhost:3000`, admin/admin) has the Home request log line and no About us line from before the start, while the Aspire dashboard shows both
- [x] 2.3 Measure go-live: time from Start on `lgtm` to `grafana-mcp` Running and Grafana `/api/health` OK, without the site restarting (site process start time unchanged); verify the times are in the report

## 3. Agent configuration (`.mcp.json`, `.claude/settings.json`; shared, assigned here)

Owner: worker-backend. Depends on: none. Spec `agent-telemetry-access`; ADR-0022; design D1.

- [x] 3.1 Add the `aspire` stdio server to `.mcp.json` next to `grafana`, and allow `mcp__aspire__<tool>` for the 13 Aspire tools other than `execute_resource_command` plus `mcp__grafana` in `.claude/settings.json`; verify both files parse as JSON (`node -e "JSON.parse(require('fs').readFileSync(f))"` per file) and `execute_resource_command` appears in neither allow entry

## 4. Smoke check (`tests/smoke/`; shared, assigned here)

Owner: worker-backend. Depends on: section 2. Design D4; ADR-0012.

- [x] 4.1 In the apphost target: pre-request `/smoke-pre/?smoke-pre=<marker>` (404 path; About us warms the content cache, design D4), start `lgtm` via the Aspire CLI if not running (skip and report "not applicable" if it is), wait for Grafana health and port 8091, then run the existing checks; add the check "no pre-start telemetry in Grafana" (no Loki line with the marker); update the header comment; verify on a freshly started AppHost that `dotnet run tests/smoke/smoke.cs` reports ALL CHECKS PASSED including the new check, and `-- standalone` still passes

## 5. Docs (`README.md` shared, assigned here; `docs/project-setup.md` Architect)

Owner: worker-backend (README), Architect (project-setup). Depends on: sections 2-4.

- [x] 5.1 README: Grafana section (start `lgtm` from the dashboard; holds only telemetry from then on), "Connecting Claude Code" (two servers, `grafana` connects after go-live via `/mcp`, `select_apphost` while `spikes/` exists, resource commands prompt); verify every command and port it names against `AppHost.cs`, `.mcp.json` and `.claude/settings.json`
- [x] 5.2 (Architect) `docs/project-setup.md`: resources table (`lgtm` explicit start, `grafana-mcp` waits), telemetry fan-out (two pipeline sets, no replay), agent access (two servers); verify every path, port and resource name against the code

## 6. Agent and go-live checks (PO, with Architect)

Owner: PO runs, Architect records in `verification.md`. Depends on: sections 1-5. Replaces
`add-demo-foundation` tasks 8.1 and 8.2. Fresh AppHost, `lgtm` not started, new Claude Code
session at repo root in default permission mode.

- [ ] 6.1 (PO) Local stage: `/mcp` shows `aspire` connected and `grafana` failed; the agent retrieves the latest Home trace with spans and durations, that trace's logs, the resource list with state, and the site's console output, all without approval prompts; asking it to run the Toxiproxy Slow command shows an approval prompt (decline); verify results recorded in `verification.md`
- [ ] 6.2 (PO) No credentials: the `list_resources` result for `site` and `sql` and the site's console output contain no database password or connection string; verify recorded (if they do: stop, Architect removes `list_resources` from the allow list and escalates to the PO per design Risks)
- [ ] 6.3 (PO) Go live: submit the Contact form, then Start `lgtm` in the dashboard, `/mcp` reconnect `grafana`; the agent retrieves a request-duration metric for Home and a Home trace from Grafana without prompts, and finds no trace or log for the earlier Contact submission (metric counters may include it, PO 2026-10-15); `grafana` tool list shows 22 read-only tools; asking for stored submissions finds no database tool; verify recorded
