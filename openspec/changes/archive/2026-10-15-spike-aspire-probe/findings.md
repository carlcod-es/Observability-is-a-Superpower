# Findings: spike-aspire-probe

Aspire CLI/AppHost 13.6.0, run 2026-10-11 on macOS. Agent transcript: `agent-answers.md` (PO-captured).
Agent answers cross-checked with `aspire otel spans|logs|traces` and `aspire describe` (same dashboard telemetry API).

## Q1 Dashboard

| Check | Result | Notes |
|---|---|---|
| Structured logs per service, named properties | Pass | probe-api log `Visit recorded for {Path}, total {VisitCount}` carries `Path`, `VisitCount` plus scope fields `RequestPath`, `RequestId`, `ConnectionId`. Values arrive as strings (`"VisitCount":"5"`). `Path` excludes the query string. |
| Logs linked to their trace | Pass | Every request log carries `traceId`/`spanId`; `list_trace_structured_logs` returned 42 logs for the failing trace across both services. |
| One trace web → API → SQL | Pass | Slow trace `85e6254…`: web `GET /` → web client `GET` → api `GET /visits` → `INSERT dbo.visit`, `probe.slow-work` (2 s), `SELECT dbo.visit`. Startup DDL has no span (recorded-parent filter, as designed). |
| Failing request marked as error with exception | Partial | API server span: status Error, `error.type=System.InvalidOperationException`, `url.query=?fail=true`. No exception message or stack on the span (`RecordException` off); message and stack are in the linked `DeveloperExceptionPageMiddleware` error log. Web root span is OK/200 (web handles the error); trace list still marks the trace ERR. |
| Built-in metrics (ASP.NET Core, HttpClient, SqlClient, runtime) | Pass | Seen by the PO on the Metrics page for probe-api: `http.server.request.duration`, `http.client.request.duration`, `dotnet.exceptions` (System.Runtime), `db.client.operation.duration` (meter `OpenTelemetry.Instrumentation.SqlClient`). SqlClient metrics are registered in Probe.Api only (PO, 2026-10-11); ServiceDefaults does not register them. |
| Custom metrics `probe.visits`, `probe.visit.duration` | Pass | Seen by the PO on the Metrics page for probe-api, updating live. Recorded on successful requests only (throw precedes recording). |
| Console output | Pass | `Probe.Api starting` (Console.WriteLine), console logger lines, DCP wait/start lines, and the unhandled-exception text all present. |

## Q2 Agent over Aspire MCP

Tool names for asks 1, 3-5 are inferred from the answers' content; ask 2 named `list_console_logs`.

| Ask | Tool(s) called | Result | Matches dashboard |
|---|---|---|---|
| List resources | `list_resources` | All resources with state, health, endpoints, dependencies, commands; also hidden ones (`aspire-dashboard`, `*-rebuilder`, `azure-environment`). | Yes (`aspire describe`; hidden resources not listed there) |
| Console logs | `list_console_logs` | 176 lines incl. startup `Console.WriteLine`, per-request lines, full exception text. | Yes (6 `Visit recorded` lines) |
| Slowest successful request | `list_traces`, trace spans | Trace `85e6254…`, 2037 ms; 98% in `probe.slow-work` with `probe.delay_ms=2000`; DB 28 ms. Excluded the failing trace by asking for "successful". | Yes (span durations identical) |
| Failing request | `list_traces`, `list_trace_structured_logs` | Trace `e1820ff…`; exception type, message, file:line from the error log; found the 4 retries, the web 200 masking the failure, and the 4 inserted rows. | Yes (13 spans, 4 api 500s, 4 inserts) |
| That trace's logs | `list_trace_structured_logs` | 42 logs: Polly Warning per retry, Error on exhaustion; api exception log x4 (dashboard deduplicates). | Yes |
| Any metric value | none available | Not reachable. Agent estimated `probe.visits` = 6 from source + logs and named the gap. | n/a |

MCP `tools/list` (queried directly, 2026-10-11) returns 14 tools: `list_resources`, `list_console_logs`, `execute_resource_command`, `list_structured_logs`, `list_traces`, `list_trace_structured_logs`, `select_apphost`, `list_apphosts`, `list_integrations`, `doctor`, `refresh_tools`, `list_docs`, `search_docs`, `get_doc`. None reads metrics.

Aspire CLI has the same limit: `aspire otel` offers logs, spans, traces; `aspire export` contains no metrics.

## Impact on the demo

- ADR-0011 confirmed, no change: the agent reads resources, console logs, structured logs and traces; no metrics. Agent-driven acts must be diagnosable from traces and logs.
- Exception detail reaches the agent via logs, not spans. Diagnosis needs `list_trace_structured_logs` after `list_traces`. Turning on `RecordException` is an open ADR-0008 option (`add-demo-foundation`); not needed for diagnosis.
- Handled failures are invisible on the root span: a page that catches a downstream error returns 200 while child spans are Error. The trace list flags the trace, and the agent found it.
- The standard resilience handler (ServiceDefaults) retries a 500 three times with backoff: one failure becomes 4 calls, ~7 s, and repeats non-idempotent side effects. Applies to any `HttpClient` call made by the site.
- SqlClient metrics are absent from ServiceDefaults; decision (add to ServiceDefaults or not) belongs to ADR-0008 in `add-demo-foundation`. When registered, they appear on the Metrics page.
- Log attribute values are strings in the dashboard API; numeric filtering by the agent is textual.
- Tooling: `aspire start` / `aspire stop` work non-interactively; `aspire stop` left project processes running (needed `pkill -f aspire-probe/Probe`, sometimes twice).
