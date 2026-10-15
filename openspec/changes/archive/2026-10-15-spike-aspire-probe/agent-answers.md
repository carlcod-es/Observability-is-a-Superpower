# Aspire MCP probe: answers

Captured 2026-10-11 against the running `aspire-probe` AppHost via the Aspire MCP server (`mcp__aspire__*`).
Dashboard: https://localhost:17310

## 1. List the Aspire resources and their state

| Resource | Type | State | Health | Endpoints |
|---|---|---|---|---|
| aspire-dashboard | Executable | Running | Healthy | https://localhost:17310, OTLP gRPC https://localhost:21299 |
| sql | Container (`mssql/server:2022-latest`) | Running | Healthy | tcp://localhost:51238 |
| probe | SqlServerDatabase (child of sql) | Running | Healthy | none |
| probe-api | Project | Running | Healthy | http://localhost:5171, https://localhost:7171 |
| probe-web | Project | Running | Healthy | http://localhost:5172, https://localhost:7172 |
| azure-environment | AzureEnvironment | NotStarted | n/a | none |
| probe-api-rebuilder | Executable | NotStarted | n/a | none |
| probe-web-rebuilder | Executable | NotStarted | n/a | none |

- Dependencies: probe-web waits for probe-api; probe-api waits for probe.
- Rebuilders start only when the `rebuild` command runs. `azure-environment` provisions nothing in a local run.
- Commands on probe-api and probe-web: `rebuild`, `restart`, `stop`. On sql: `restart`, `stop`.

## 2. Show the console logs for probe-api

Source: `list_console_logs` for `probe-api`, 176 lines. Condensed; the per-request block repeats and is shown once.

Startup:

```
Waiting for resource 'sql' to enter the 'Running' state.
Waiting for resource 'probe' to enter the 'Running' state.
Waiting for resource 'sql' to become healthy.
Waiting for resource 'probe' to become healthy.
Finished waiting for resource 'sql'.
Finished waiting for resource 'probe'.
[sys] Starting process...: Cmd = dotnet, Args = ["run", "--project", ".../Probe.Api/Probe.Api.csproj", "--no-build", "--configuration", "Debug", "--no-launch-profile"]
Probe.Api starting
info: Microsoft.Hosting.Lifetime[14] Now listening on: https://localhost:18781
info: Microsoft.Hosting.Lifetime[14] Now listening on: http://localhost:18780
info: Microsoft.Hosting.Lifetime[0] Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0] Hosting environment: Development
```

Successful request (6 occurrences, `Visit recorded ... total 1` to `total 6`):

```
info: Microsoft.AspNetCore.Hosting.Diagnostics[1]   Request starting HTTP/1.1 GET https://localhost:7171/visits - - -
info: Microsoft.AspNetCore.Routing.EndpointMiddleware[0]   Executing endpoint 'HTTP: GET /visits'
info: Program[0]   Visit recorded for /visits, total N
info: Microsoft.AspNetCore.Http.Result.OkObjectResult[1]   Setting HTTP status code 200.
info: Microsoft.AspNetCore.Http.Result.OkObjectResult[3]   Writing value of type '<>f__AnonymousType0`1' as Json.
info: Microsoft.AspNetCore.Routing.EndpointMiddleware[1]   Executed endpoint 'HTTP: GET /visits'
info: Microsoft.AspNetCore.Hosting.Diagnostics[2]   Request finished HTTP/1.1 GET https://localhost:7171/visits - 200 - application/json; charset=utf-8 <ms>
```

Request durations from `Request finished`: 146.6, 22.5, 14.2, 2032.7 (`?slow=2000`), 14.7, 10.5 ms.

Failed request (`?fail=true`, 4 occurrences, 500 in 19.2, 7.6, 17.6, 17.8 ms). There is no `Visit recorded` line:

```
fail: Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware[1]
      An unhandled exception has occurred while executing the request.
      System.InvalidOperationException: Probe failure requested
         at Program.<>c__DisplayClass0_0.<<<Main>$>b__2>d.MoveNext() in .../Probe.Api/Program.cs:line 74
      --- End of stack trace from previous location ---
         at Program.<>c__DisplayClass0_0.<<<Main>$>b__2>d.MoveNext() in .../Probe.Api/Program.cs:line 84
      --- End of stack trace from previous location ---
         at Microsoft.AspNetCore.Http.RequestDelegateFactory.ExecuteTaskResult[T](Task`1 task, HttpContext httpContext)
         at Microsoft.AspNetCore.Routing.EndpointMiddleware.<Invoke>g__AwaitRequestTask|7_0(Endpoint endpoint, Task requestTask, ILogger logger)
         at Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddlewareImpl.Invoke(HttpContext context)
```

## 3. Find the slowest successful request and explain where the time went

Request: `GET /?slow=2000` on probe-web, 2037 ms, HTTP 200.
Trace `85e625474d25d4211f692eb320255087`: https://localhost:17310/traces/detail/85e625474d25d4211f692eb320255087

The slowest successful request overall was the cold-start `GET /` at 291 ms. It is not the slow case, so it was excluded. Only the request with `?slow=2000` is a real latency outlier.

Span breakdown (all times from 21:52:53Z):

| Span | Service | Start offset | Duration |
|---|---|---|---|
| `GET /` (server) | probe-web | 0 ms | 2037 ms |
| `GET` (client) to `/visits?slow=2000` | probe-web | +2 ms | 2034 ms |
| `GET /visits` (server) | probe-api | +3 ms | 2033 ms |
| `INSERT dbo.visit` | probe-api | +5 ms | 17 ms |
| `probe.slow-work` (internal, attr `probe.delay_ms=2000`) | probe-api | +22 ms | 2002 ms |
| `SELECT dbo.visit` (COUNT) | probe-api | +2024 ms | 11 ms |

Where the time went:
- About 2002 ms (98%) is the `probe.slow-work` span. It is an artificial delay driven by the `slow=2000` query parameter, inside probe-api.
- The database accounts for about 28 ms in total (INSERT 17 ms + SELECT 11 ms).
- probe-web and the HTTP hop between the services add about 3 ms.
- The `SELECT` starts only after the delay ends (+2024 ms). The delay therefore sits between the INSERT and the SELECT.
- Nothing is wrong with the network, the database or probe-web. The latency is entirely in the API handler's deliberate delay.

## 4. Find the failing request and tell me the exception

Request: `GET /?fail=true` on probe-web, which calls `GET /visits?fail=true` on probe-api.
Trace `e1820ff706b19b038aafd99eb5c98872`, started 21:53:00Z: https://localhost:17310/traces/detail/e1820ff706b19b038aafd99eb5c98872

Exception: `System.InvalidOperationException: Probe failure requested`
- Thrown at `Probe.Api/Program.cs:74` in the `/visits` handler and rethrown at line 84.
- probe-api returns 500. Log entry 127: https://localhost:17310/structuredlogs?logEntryId=127

Retries:
- probe-web calls probe-api 4 times (attempts 0 to 3) at 21:53:00.16, :01.60, :05.43 and :07.07.
- The gaps between attempts grow, so this is retry with backoff.
- All four fail with the same exception.
- probe-web then returns 200 to the browser after 6.9 s, so the root span looks successful even though all four child calls failed.

Side effect: each failed attempt ran `INSERT dbo.visit` before throwing, so the four failures wrote four rows to the table.

The failure is deliberate. `fail=true` triggers it, so it is not a real fault.

## 5. Show the structured logs for that failing trace

Trace `e1820ff706b19b038aafd99eb5c98872`, 42 log entries, grouped by service and shown in order of occurrence. Full text is in the dashboard: https://localhost:17310/structuredlogs

probe-web (root request and outbound calls):

| logId | Severity | Message |
|---|---|---|
| 116 | Information | Request starting HTTP/2 GET https://localhost:7172/?fail=true |
| 117 | Information | Executing endpoint 'HTTP: GET /' |
| 118 | Information | Start processing HTTP request GET https+http://probe-api/visits?* |
| 119 | Information | Sending HTTP request GET https://localhost:7171/visits?* |
| 120 | Information | Received HTTP response headers after 19.78 ms - 500 |
| 121 | Warning | Polly Execution attempt. Standard-Retry, Result 500, Handled true, Attempt 0, 20.54 ms |
| 122 | Warning | Polly Resilience event: OnRetry, Result 500 |
| 128 / 129 | Information | Sending / Received after 15.57 ms - 500 |
| 130 / 131 | Warning | Polly Attempt 1, 16.32 ms / OnRetry |
| 137 / 138 | Information | Sending / Received after 21.07 ms - 500 |
| 139 / 140 | Warning | Polly Attempt 2, 25.50 ms / OnRetry |
| 146 / 147 | Information | Sending / Received after 20.65 ms - 500 |
| 148 | **Error** | Polly Execution attempt. Standard-Retry, Result 500, Handled true, Attempt 3, 21.43 ms (retries exhausted) |
| 149 | Information | End processing HTTP request after 6925.78 ms - 500 |
| 150 | Information | Write content with HTTP Response ContentType of text/html |
| 151 | Information | Executed endpoint 'HTTP: GET /' |
| 152 | Information | Request finished HTTP/2 GET https://localhost:7172/?fail=true - **200** 272 text/html 6927.28 ms |

probe-api (four requests, one per attempt):

| Attempt | Request logs | Executing / Executed | Exception log | Duration |
|---|---|---|---|---|
| 0 | 123, 124 | 125, 126 | 127 | 19.17 ms, 500 |
| 1 | 132, 133 | 134, 135 | 136 (duplicate of 127) | 7.63 ms, 500 |
| 2 | 141, 142 | 143, 144 | 145 (duplicate of 127) | 17.55 ms, 500 |
| 3 | 153, 154 | 155, 156 | 157 (duplicate of 127) | 17.84 ms, 500 |

Log 127 (**Error**, source `DeveloperExceptionPageMiddleware`):

```
An unhandled exception has occurred while executing the request.
System.InvalidOperationException: Probe failure requested
   at Program.<>c__DisplayClass0_0.<<<Main>$>b__2>d.MoveNext() in .../Probe.Api/Program.cs:line 74
   --- End of stack trace from previous location ---
   at Program.<>c__DisplayClass0_0.<<<Main>$>b__2>d.MoveNext() in .../Probe.Api/Program.cs:line 84
   at Microsoft.AspNetCore.Http.RequestDelegateFactory.ExecuteTaskResult[T](Task`1 task, HttpContext httpContext)
   at Microsoft.AspNetCore.Routing.EndpointMiddleware.<Invoke>g__AwaitRequestTask|7_0(Endpoint endpoint, Task requestTask, ILogger logger)
   at Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddlewareImpl.Invoke(HttpContext context)
```

Observations:
- Polly logs attempts 0 to 2 at Warning and the final attempt 3 at Error.
- Every probe-api 500 has the same exception. The dashboard stores logs 136, 145 and 157 as duplicates of log 127.
- No probe-web log carries the exception. The page still returns 200 even though the API failed, which hides the failure from anything watching only the root span.

## 6. What is the current value of the probe.visits metric?

Not measured. The Aspire MCP server and the Aspire CLI (`aspire otel`, `aspire export`) expose logs, spans and traces only. No metrics tool exists, and `aspire export` produced no metrics files.

Estimate from source: **6**.
- `probe.visits` is a `Counter<long>` on meter `Probe.Api`, defined at `Probe.Api/Program.cs:16`.
- It is incremented by `visitsCounter.Add(1)` at line 80, after the line-74 throw. Failed requests therefore do not count.
- 6 successful `/visits` requests are logged (`Visit recorded ... total 6`).
- The estimate assumes probe-api has not restarted and no requests arrived after the logs were read.

The `total N` in those log lines is `SELECT COUNT(*)` on `dbo.visit`, not the metric. That table count is now about 10, because the 4 failed requests inserted rows before throwing.

To read the real value: dashboard, Metrics, probe-api, meter `Probe.Api`, `probe.visits`.

## Gap found

The MCP server cannot answer metric questions. If the talk demo needs the agent to read metrics, it needs another route, such as a custom MCP tool or Azure MCP queries against Application Insights in production.
