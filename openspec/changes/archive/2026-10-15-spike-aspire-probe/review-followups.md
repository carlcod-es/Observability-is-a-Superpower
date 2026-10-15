# Review follow-ups: spike-aspire-probe

Minors and nits from block and section reviews. Throwaway bar; act only if a finding affects section 3.

## Section 1 block review (2026-10-11, APPROVED)
- Minor, `spikes/aspire-probe/Probe.Api/Program.cs:71-81`: `fail=true` throws before the `Visit recorded` log and metric records, so failing requests have no named-property log and no `probe.visit.duration` datapoint. Fix if wanted: log/record in `try/finally`. Effect on `spike-aspire-probe` task 3.2: the failing trace's logs are the framework's unhandled-exception logs only.
- Nit, `spikes/aspire-probe/Probe.Web/Program.cs:22`: page shows raw JSON `{"count":4}` instead of the number.
- Nit, `spikes/aspire-probe/Probe.Api/Program.cs:27-48`: startup DDL retry covers only `SqlException`.
- Nit, `spikes/aspire-probe/Probe.Api/Program.cs:53`: `Path` includes the query string. Useful; no change.

## Observations for findings.md
- The standard resilience handler retries a 500 three times: one failing web click produces 4 API calls (~9 s), 4 failing API spans, and 4 inserted rows.
- `aspire stop` left `Probe.Api` / `Probe.Web` processes running; killed with `pkill -f aspire-probe/Probe`.

## Section 1 supervisor review (2026-10-11, APPROVED)
- Minor, `src/SuperPower.ServiceDefaults/Extensions.cs:62-68`: SqlClient metrics (`db.client.operation.duration`) not registered; `spike-aspire-probe` task 3.1 SqlClient-metrics check will fail. Same gap on the site; ADR-0008 never decided it. Record in findings; decide on an ADR-0008 amendment in `add-demo-foundation`. On the site the metric would also count Umbraco background-job queries (not trace-filtered).
- Minor, `spikes/aspire-probe/Probe.Web/Program.cs:20` + resilience handler: the failing web request (~9 s, 4 attempts) is the slowest trace, so `spike-aspire-probe` task 3.2 prompts must ask for the slowest *successful* request.
- Nit, `src/SuperPower.ServiceDefaults/Extensions.cs:73`: `RecordException` off; exception is on the linked error log, not the span. Record which MCP tool returned it in Q2.
- Nit, `spikes/aspire-probe/Probe.Api/Program.cs:11`: `AddSource("Probe.Api")` duplicates ServiceDefaults' ApplicationName source. Harmless.
- Risk: the probe logs via `ILogger` → OTel directly, not via the site's Serilog forwarding sink (verified separately in `add-demo-foundation` task 1.3).

## PO decisions (2026-10-11)
- SqlClient metrics: registered in `Probe.Api` only (probe-only; ServiceDefaults unchanged). Mark as probe-only in `findings.md` Q1; ADR-0008 decision for the site stays open.
- Failing-path log/metrics (block review minor): no change; the failing trace shows the framework's unhandled-exception log, as on the site.

## Observations from the SqlClient-metrics rework (2026-10-11)
- Aspire CLI 13.6 has no metrics path: `aspire otel` offers `logs`, `spans`, `traces`; `aspire export` zip holds traces, structured logs, console logs, resources only. Relevant to Q2 and ADR-0011.
- Expected SqlClient metric: `db.client.operation.duration` (from the 1.17.0 binary; export not yet observed).
- Cleanup: a single `pkill -f aspire-probe/Probe` right after `aspire stop` sometimes misses; repeat it.
