# Agent asks: spike-metrics-mcp task 3.2

Candidate B only (Grafana LGTM + Grafana MCP; scope decision 2026-10-12). One fresh Claude Code
session started in `spikes/metrics-mcp/`. Paste the transcript into `agent-answers.md`.

## Run

1. Stop any running AppHost (`aspire stop`, then the leftover-process check in the README).
2. From `spikes/metrics-mcp/`: `aspire start` (default mode `b`). Wait until all resources are
   Healthy in the dashboard.
3. `node load.mjs`. Keep its output (UTC phase times).
4. Wait 1 minute, then start `claude` in `spikes/metrics-mcp/`. Approve tool calls as they come.
5. Send the asks below one at a time. Paste only the three JSON phase lines of the `load.mjs`
   output, not the final "Expected probe-api requests" line (it gives away the answers to asks 2
   and 7).

Expected values (Architect, written before the run): 45 `probe-api` requests = 25 x 200 + 20 x 500
(each failing web request becomes 4 `probe-api` calls through the retry handler); `probe.visits` =
25 per run (not incremented on failure); dominant span of a slow request `probe.slow-work`; ask 7
accepts 5 failing web requests or 20 failing API calls if the agent says which it counts.
Cross-check also confirms the transcript contains no `mcp__aspire__*` calls (`.mcp.json` still
loads the `aspire` server, so the restriction rests on the prompt).

## Asks

0. Use only the `grafana` MCP server for telemetry. Do not read source code, and do not use the
   `aspire` MCP server. Here is the output of a load run against the app: `<paste the three JSON phase lines of load.mjs>`

Metrics
1. Which metrics are available for the services `probe-api` and `probe-web`? Group them by what
   they measure.
2. What were the request rate and the error rate of `probe-api` during the load run?
3. What was the p95 duration of `probe-api` `GET /visits` during the normal phase, and during the
   slow phase?
4. What is the current total of the custom counter `probe.visits`?
5. What is the p95 of the custom histogram `probe.visit.duration`?
6. What were the average and p95 database call durations of `probe-api`?

Logs
7. Show the error logs from the failing requests in the load run. How many failing requests were
   there, and what is the error message?

Traces
8. Find one slow request (the slow phase) and say which span takes most of its time.

Across signals
9. Take one failing request: show its log entry, its trace, and the error-rate metric for the same
   minute, and say whether they agree.

Self-assessment
10. Which answers above are you least sure about, and why?

## Cross-check (Architect)

Compare each answer against the dashboard (PO: Metrics, Structured logs, Traces pages) and Grafana
Explore (`http://localhost:3000`). Result table in `findings.md`.
