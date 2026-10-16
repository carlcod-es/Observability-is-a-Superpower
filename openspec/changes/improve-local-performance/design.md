## Context

See proposal.md (Why) and spec delta `local-environment` ("Optimised stage run"). The current
state and the constraints are in ADR-0029, Context.

Bar: **demo** (CLAUDE.md "Principle"). Settings and build changes only, with no new code
paths. Proof comes from measurement plus the existing smoke checks. No new automated tests,
because nothing new is testable at unit level.

## Goals / Non-Goals

**Goals:**
- Measured gains on the M1, ranked as the PO set them (2026-10-15): warm page and form
  durations (M3, M4) and memory (M5) first; first page loads (M2) next; start time (M1) last.
- No change in behaviour.
- One stage start command that keeps the Aspire MCP working.

**Non-Goals:**
- Warming the site up before the talk (PO 2026-10-15: the first Home trace with database calls
  is needed live).
- Changes to the site's code or queries. Warm page requests come from Umbraco's cache.
- Changes for attendees' machines beyond the views compiled at build.

## Decisions

All in ADR-0029:
1. Views compiled at build (remove `RazorCompileOnBuild`/`RazorCompileOnPublish=false`).
2. Release build for the stage run via `aspire run`. Task 2.1 picks the mechanism.
3. `Umbraco__CMS__Hosting__Debug=false` set by the AppHost on the site.
4. Umbraco runtime mode unchanged. `Production` mode was rejected (ADR-0029, Alternatives).
5. A presenter machine checklist in the runthrough.

### Measurement
The Architect takes the measurements through the Aspire MCP: traces, structured logs and
console logs. The PO reads memory in the dashboard (the Aspire MCP has no metrics, ADR-0022).
The runs are on the M1, on mains power, with the same apps open each time. Order: baseline,
then after each decision (1 → 3 → 2), so each gain can be attributed.

| # | Measure | Source | How |
|---|---------|--------|-----|
| M1 | Start to site ready | site console log | From AppHost start to the site's "Now listening" / Umbraco boot-complete line. The SQL container is persistent and already running. |
| M2 | First load per page type | traces | First request after start: Home, About us, Contact us, Events list, event page, Services list, service page. Root span duration. |
| M3 | Warm page duration | traces | `tests/traffic/traffic.cs` run once. Median root span duration per page type. |
| M4 | Form submit duration | traces | `tests/traffic/burst.cs --count 50 --parallel 10`. Median `acme.forms.submit` duration. |
| M5 | Site memory | dashboard metrics (PO) | `process.runtime.dotnet.gc.heap.size` / working set after M3. Also `docker stats` totals. |

Each run uses a fresh AppHost start (stop, then start) so that M1 and M2 are cold. Results go
in `verification.md` as a table: baseline, after each step, and the delta. Keep rule: an item
stays if it improves M3, M4 or M5 beyond noise, or improves M1 or M2 without making M3-M5
worse. M5 also records macOS memory pressure and swap (Activity Monitor). Swapping on the 16 GB
machine would slow every request.

## Risks / Trade-offs

- [`aspire run` cannot select Release while keeping the MCP backchannel] → task 2.1 tests the
  environment-property and `--no-build` routes. If neither works, the stage run stays Debug and
  only decisions 1 and 3 ship.
- [Precompiled views hide a Razor error until build time instead of first hit] → It turns up
  earlier, which is better. Smoke checks cover every page.
- [The Release build skips `appsettings.Local.json` (`#if DEBUG` in `Program.cs`)] → Nothing
  uses it today. Task 4.2 confirms the stage run needs no local overrides.
- [Measurement noise on a laptop (thermals, background apps)] → Run each measure three times
  and take the median. A gain inside the noise counts as no gain and the item is dropped
  (ADR-0029).
- [Late change the day before the talk] → Settings only, each reversible in one line. Demo
  branches `demo/act-N-*` are rebased or merged only after PO approval.

## Migration Plan

Rollback: revert the csproj line, the AppHost environment line, and go back to the Debug start
command. Merge into the act branches only after section 4 passes.
