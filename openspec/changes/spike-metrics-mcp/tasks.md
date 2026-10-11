Bar: throwaway. No tests; each task is verified by observation and recorded in `findings.md`.
Exception: `spikes/metrics-mcp/README.md` (task 1.6, design D7).
Package and images in `design.md` D1-D3: PO approved 2026-10-11.

## 1. Spike AppHost

Owner: worker-backend. Shared files assigned: `Directory.Packages.props` (add
`CommunityToolkit.Aspire.Hosting.OpenTelemetryCollector` 13.4.0 only). Depends on: none.
Do not edit `spikes/aspire-probe/`, `src/SuperPower.ServiceDefaults/` or `SuperPower.slnx`.

- [x] 1.1 Create `spikes/metrics-mcp/` with `MetricsMcp.slnx`, `aspire.config.json` and `Metrics.AppHost` (Aspire AppHost SDK 13.6.1, `AspireUseCliBundle`), referencing `../aspire-probe/Probe.Api` and `../aspire-probe/Probe.Web` and defining SQL, `probe-api` and `probe-web` exactly as `spikes/aspire-probe/Probe.AppHost/AppHost.cs`, plus the candidate switch `Spike:Candidates` = `none` | `a` | `b` | `both` (default `both`, invalid value fails start-up with the valid values; design D7) read from configuration while the model is built; verify `dotnet build spikes/metrics-mcp/MetricsMcp.slnx` succeeds and `aspire run` from `spikes/metrics-mcp/` shows the three resources running, and an invalid switch value stops start-up with the message
- [ ] 1.2 Add the Collector (`AddOpenTelemetryCollector` + `WithAppForwarding()`, pinned collector-contrib tag, `collector/config.yaml` via `WithConfig`) forwarding all signals to the dashboard (design D1; fallback: hand-wired `AddContainer`, report which); verify the dashboard shows structured logs, traces (web → api → SQL) and metrics incl. `probe.visits` and `probe.visit.duration` for both services, as in `spike-aspire-probe` `findings.md` Q1
- [x] 1.3 Candidate A: `prom/prometheus` container (pinned 3.x tag, `--web.enable-otlp-receiver`, host port 9090) and a Collector metrics exporter to its `/api/v1/otlp` (design D2), both added only when the switch is `a` or `both`; verify in the Prometheus UI at `http://localhost:9090` that the series for `probe.visits` and `http.server.request.duration` exist after a few requests, and record their translated names
- [x] 1.4 Candidate B: `grafana/otel-lgtm` container (pinned tag, Grafana host port 3000) and Collector exporters for all three signals to its OTLP endpoint (design D3), both added only when the switch is `b` or `both`; verify in Grafana Explore at `http://localhost:3000` that the probe metric, one probe trace and one probe log are visible
- [x] 1.5 MCP servers as container resources over streamable HTTP (design D4): `ghcr.io/pab1it0/prometheus-mcp-server` (pinned tag, Prometheus URL by resource reference, host port 8090) and `grafana/mcp-grafana` (pinned tag, `--disable-write`, Grafana URL by resource reference, LGTM default credentials, host port 8091), each added only with its candidate; verify each answers MCP `initialize` and `tools/list` with `curl`, and report the tool lists verbatim. If a server has no HTTP transport, do not improvise: report it
- [x] 1.6 Write `spikes/metrics-mcp/README.md` (design D7): the prerequisites (Docker, Aspire CLI, .NET 10 SDK); the run command for each mode (`aspire run -- --Spike:Candidates=<mode>`, already verified with Aspire 13.6.1), and the `Spike__Candidates` environment variable as an alternative for bash/zsh and PowerShell; a table per mode of the resources, host ports and URLs (dashboard, Prometheus `http://localhost:9090`, Grafana `http://localhost:3000`, MCP 8090/8091); the Grafana default credentials; how to run the load script (task 3.1); and a note that `.mcp.json` servers of a candidate that is not running show as failed in Claude Code. Verify by starting each of the four modes from the README alone and checking that the dashboard resource list matches its table

## 2. Agent configuration

Owner: Architect. Depends on: 1.5, 1.6.

- [x] 2.1 Add `spikes/metrics-mcp/.mcp.json` with `aspire` (stdio, `aspire agent mcp`), `prometheus` and `grafana` (`http`, ports from 1.5; stdio `docker run -i --rm` form for any server reported without HTTP in 1.5); verify a Claude Code session started in `spikes/metrics-mcp/` lists all three servers as connected
- [x] 2.2 Record in `findings.md` both servers' tool lists, which tools read vs write, and which flags restrict them (proposal question 4); verify against the `tools/list` output from 1.5

## 3. Investigation and findings

Owner: Architect, with the PO observing the dashboard and capturing transcripts. Depends on: 1, 2.

- [ ] 3.1 Add `spikes/metrics-mcp/load.mjs` (Node, no dependencies): a fixed sequence of 20 normal, 5 slow (`slow=2000`) and 5 failing requests to `probe-web`, logging the UTC start/end of each phase; verify the run's request counts match the dashboard's `http.server.request.duration` count for `probe-api`
- [ ] 3.2 Agent reads metrics (proposal question 1): fresh session per candidate, AppHost started with that candidate only (switch `a`, then `b`), fixed asks (list metrics; API request rate and error rate; `/visits` p95 before vs during the slow phase; `probe.visits` total; `probe.visit.duration` p95; database call duration). The PO captures `agent-answers.md`. Verify each answer against the dashboard Metrics page and the backend UI in a table in `findings.md`
- [ ] 3.3 Freshness (proposal question 2): time from one request to the change being visible via each MCP server; verify three measurements per candidate recorded in `findings.md`
- [ ] 3.4 Local developer experience (proposal question 3): `aspire run` start-up time and `docker stats` memory for switch modes `none`, `a` and `b`, each run separately; dashboard completeness unchanged (1.2); ServiceDefaults changes needed (expected none); attendee prerequisites; cross-platform risks; verify recorded in `findings.md`
- [ ] 3.5 Talk fit and recommendation (proposal question 5): how metric names and units read to the agent, comparison with Application Insights via Azure MCP, and a recommendation (adopt A, adopt B, or keep ADR-0011); verify the PO signs off and `PLANNING.md` is updated
- [ ] 3.6 After PO sign-off, and together with `spike-aspire-probe` task 3.4 (the probe projects are shared): delete `spikes/metrics-mcp/`, remove the toolkit package from `Directory.Packages.props`, then archive; verify `dotnet build SuperPower.slnx` succeeds from the repo root
