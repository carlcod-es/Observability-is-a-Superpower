# extend-demo review follow-ups

- minor (task 2.1 runtime check, pre-existing): the "Error rate (5xx)" panel expr returns NaN (no data)
  with no site traffic at all, not 0 %. Spec `demo-dashboard` says 0 rather than no data when there are no
  errors; with demo traffic running it reads 0. Not changed by `extend-demo`.
- nit (task 2.1): first-booking gap after a `site` restart applies to the stored line too (ADR-0034
  Consequences); Act 2 burst recreates the series before Act 3.

## Supervisor, sections 1-2 (APPROVED both)
- minor: `src/SuperPower.Api/BookingStoredConsumer.cs` Act 3 explanation and `Fix:` lines give the answer to a
  stage agent reading `src/` (same exposure as Acts 1-2, design.md Risks). Task 4.2: the runthrough's stripping
  decision for `demo` must name this file.
- nit: older markers read "Initail Demo State" (`src/SuperPower.ServiceDefaults/Extensions.cs`,
  `src/SuperPower.Web/Forms/Storage/FormSubmissionRepository.cs`); Act 3 reads "Initial". Fix the typo in a later pass.
- nit: if `BasicAckAsync` throws after `Counted.Add(1)`, the booking is counted and also marked error and nacked.
  Demo bar; no change.
- nit: in the planted state the failing deliver span has no `acme.event.key` (thrown before `SetTag`); the agent
  links the failure to the event through the site's `acme.forms.publish` span in the same trace. Note in the
  runthrough Act 3 expected path (task 4.2).

## Reviewer, section 3 (APPROVED)
- minor: `tests/smoke/smoke.cs` (~492) `countedBefore` falls back to 0 when Prometheus cannot answer before the
  booking; a stale non-zero series could then pass without a real increase. Fix: fail on a null baseline or retry once.
- nit: README "Tests" intro still says run right after start; the `-- running` note covers the alternative.
- Task 3.3: `/mcp` -> `aspire` -> Disable is interactive; PO verifies `claude mcp list` shows `aspire` disabled. Obsolete 2026-10-23: `aspire` removed from `.mcp.json` (tasks 3.3, 3.4).
- Architect: Tempo search lag measured 30.6-32.5 s; smoke Tempo polls raised to 60 s (pre-existing borderline).

## Supervisor, section 3 (APPROVED)
- major (task 4.1, fixed by the Architect): `claude-stage.md` asked for a "restart"; Aspire Restart does not recompile.
  Now "rebuild". Runthrough uses Rebuild in every act; design.md D4/D6 updated.
- minor (README, fixed in rework): "restart them from the Aspire dashboard" -> Rebuild; "Demo site" panel list lacks
  "Bookings: stored vs counted"; Match-the-talk step 3 should say `aspire` disabled (obsolete 2026-10-23: section replaced, task 3.3).
- nit: specs `agent-telemetry-access` / `event-booking-count` say "restart" generically; acceptable.
- nit: README load-test section (~301) "restart the site" after a change; fix in a later pass.
- nit: `tests/smoke/smoke.cs` counted check compares raw values; a rebuild of `api` seconds before smoke #2 with no
  export yet can give a false FAIL. Unlikely on stage.
- risk: `.claude/settings.json` `enableAllProjectMcpServers: true`; PO's `claude mcp list` check (task 3.3) confirms
  the per-folder Disable holds.

## Supervisor, section 4 (CHANGES REQUESTED -> fixed by the Architect)
- Fixed: runthrough Act 3 fallback pointed at Planted problems 3 (now 4); "Morning step 3" (now 7) and the
  "*Important!*" line told the presenter to plant before smoke #1; Act 3 page text (no booking line at 0); panel
  window note; ADR-0033 body said "restarts"/"no tool".
- Resolved: sections 1-2 items "stripping decision must name this file" (runthrough "Decide") and "failing span has
  no `acme.event.key`" (runthrough Planted problems 4 and Act 3 expected path).
- nit: CLAUDE.md "Repo conventions" change order lists only the original four changes (pre-existing); PLANNING.md
  holds the full order.
- nit: runthrough Act 2 step 7 "Optional" sub-bullets malformed (PO text); tidy when copying.
- risk: `aspire resource <name> rebuild` CLI form used in the runthrough Scripts table; ran for `api` on 2026-10-22
  (section 1 worker); dashboard Rebuild is the primary path. Rehearse in task 5.1.

## Reviewer, tasks 3.3-3.4 (APPROVED; 3.3 macOS check deferred to PO)
- PO check (task 3.3): this machine's `~/.claude.json` has `disabledMcpServers: ["aspire"]` for the folder (earlier
  `/mcp` Disable), so `claude mcp add` shows `aspire` "Disabled for this project". Re-enable via `/mcp`, then confirm
  `aspire` connected and `mcp__aspire__list_resources` runs without a prompt.
- nit: `README.md:242-246` second enable path (`.mcp.json` block) is optional under the demo bar. Kept: design.md D4.
- nit: `README.md:210-211` Aspire pre-approval note duplicates line 227; keep one.
- nit: `README.md:62-63` over-long line (pre-existing); reflow.
- PO check done 2026-10-23 in a fresh clone at another path: `/mcp` listed only `grafana` (22 tools) (task 3.4);
  after `claude mcp add --scope local aspire -- aspire agent mcp`, `aspire` connected and the agent listed resources
  with no approval prompt (task 3.3).

## Supervisor, section 3 re-review after Aspire opt-in (APPROVED)
- minor (task 4.5, fixed by the Architect): `docs/project-setup.md:34-35` still listed `aspire` in `.mcp.json`.
- nit: ADR-0022:18 Decision says two servers; Status marks it superseded. Left (ADR convention).
- nit: `extend-demo` task 4.2 text says `aspire` disabled; superseded by task 4.4. Left (history).
- nit: main spec `agent-telemetry-access` "Telemetry tools are read-only and pre-approved" says "both sources";
  still true (allow entries kept). Left.
- Risk: stage clone for task 5.1 must be a fresh clone at a new path (or `aspire` absent from `claude mcp list`).

## Reviewer, task 3.5 (APPROVED; runtime re-verified by the Architect)
- Architect 2026-10-23: fresh `aspire start`, all resources Running incl. `lgtm`, `grafana-mcp`; smoke `ALL CHECKS
  PASSED` (17 PASS, no N/A; "Grafana healthy, grafana-mcp port 8091 open"); `-- running` `ALL CHECKS PASSED`, one N/A
  (home trace). Reviewer could not run it: the worker's AppHost had stopped when its shell exited.
- Beyond brief (accepted): README Grafana bullet on dropped pre-start telemetry replaced by cumulative counters reset
  on service restart.
- minor: `README.md:78` long unwrapped line; wrap.
- nit: `README.md:263` long unwrapped line.
- nit: `README.md:212` kept a one-line `/mcp` reconnect hedge (Claude Code opened before `grafana-mcp` Running). Kept.
- nit: README "Grafana" intro cites ADR-0022; ADR-0033 is the current decision.

## Supervisor, section 3 re-review after lgtm auto-start (CHANGES REQUESTED -> fixed by the Architect)
- major (task 4.6, fixed): `docs/demo-setup.md:28,37` still said "do not start `lgtm`" and expected the pre-start
  FAIL; file added to task 4.6.
- major (task 4.6, fixed): `.temp/runthrough.md` smoke #2 expected two N/A lines; now one.
- minor (fixed): design.md D4 runthrough sentence; D5 stray "and"; local-environment delta "Default unchanged" wording.
- minor (fixed): `event-booking-count` "Booking service telemetry" said "platform once started"; MODIFIED delta added.
- minor (open, worker-owned): `src/SuperPower.AppHost/collector/config.yaml:3-5` comment says LGTM "started on demand
  (ADR-0022)"; behaviour (retry and queue off) unchanged. Comment-only fix in a later block.
- minor (open, at archive): main spec Purpose lines `local-environment` (ADR-0022 "started on demand") and
  `demo-dashboard` ("started on demand"): reword citing ADR-0033 when archiving.
- nit (fixed): FR-0004 item 27 marked obsolete. nit (left): task 3.1 text is history.
- Risk: `site` and `api` start only after `lgtm` is healthy; pre-pull images every morning. Read the bookings panel as
  increases only (counters reset on `api` restart).
