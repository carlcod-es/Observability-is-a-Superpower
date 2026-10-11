# Agent asks: spike-metrics-mcp task 3.2

Same asks for both candidates. One fresh Claude Code session per candidate, started in
`spikes/metrics-mcp/`. Paste each session's transcript into `agent-answers.md` under
`## Candidate A` / `## Candidate B`.

## Per candidate

1. Stop any running AppHost (`aspire stop`, then the leftover-process check in the README).
2. From `spikes/metrics-mcp/`: `aspire start -- --Spike:Candidates=a` (then `b` for the second run).
   Wait until all resources are Healthy in the dashboard.
3. `node load.mjs`. Keep its output (UTC phase times).
4. Wait 1 minute, then start `claude` in `spikes/metrics-mcp/`. Approve tool calls as they come.
5. Send the asks below one at a time. Replace `<SERVER>` with `prometheus` (A) or `grafana` (B),
   and paste the `load.mjs` output where shown.

## Asks

0. Use only the `<SERVER>` MCP server for metrics. Do not read source code or logs. Here is the
   output of a load run against the app: `<paste load.mjs output>`
1. Which metrics are available for the services `probe-api` and `probe-web`? Group them by what
   they measure.
2. What were the request rate and the error rate of `probe-api` during the load run?
3. What was the p95 duration of `probe-api` `GET /visits` during the normal phase, and during the
   slow phase?
4. What is the current total of the custom counter `probe.visits`?
5. What is the p95 of the custom histogram `probe.visit.duration`?
6. What were the average and p95 database call durations of `probe-api`?
7. Which numbers above are you least sure about, and why?

## Cross-check (Architect)

For each answer, compare against the dashboard Metrics page (PO) and the backend UI or API
(Prometheus `http://localhost:9090`, Grafana Explore `http://localhost:3000`). Result table in
`findings.md`.
