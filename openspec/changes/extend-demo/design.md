## Context

Current state: `add-booking-api` (archived 2026-10-22) added `api`, `cache` and `queue`. The site
publishes `{"eventKey":"<guid>"}` to `booking-stored` after a stored event booking
(`src/SuperPower.Web/Events/BookingEvents.cs`); `BookingStoredConsumer` in `src/SuperPower.Api`
increments `event:{eventKey}:bookings`, logs `Booking recorded for {EventKey}, count {Count}`, and on
any exception logs `Booking update failed for {EventKey}` and nacks without requeue (message
dropped). The catch does not mark the deliver span as an error. `api` emits no custom metric. The
demo dashboard (`src/SuperPower.AppHost/grafana/dashboards/demo-site.json`) has five site-only
panels; row 3 is "Form submissions by outcome" at full width. The smoke check assumes a freshly
started AppHost (first Home load has database spans). Agent setup follows ADR-0022 (Aspire MCP
before go-live, Grafana MCP after).

Fixed constraints applied: Grafana MCP read-only (ADR-0017), `lgtm` explicit-start (ADR-0022),
planted problems marked `// Demo Code : Act N` on `main` in the fixed state (CLAUDE.md "Repo
conventions"). Gaps they imply, decided here: how the agent restarts services without the Aspire
MCP (it asks; D4), and where the counted side of the panel comes from (D2).

Bar: **demo** (CLAUDE.md "Principle"). Tests: no new xUnit tests (the changed code is the `api`
consumer, which has no test project, ADR-0030 design); verification by the smoke check (new
counted-metric check, running mode) and observed telemetry with the toggle planted and fixed.

Planted problem: chosen by the Architect, given to the worker in the block brief, recorded only in
the `// Demo Code : Act 3` comment and the presenter runthrough. Not described here.

## Goals / Non-Goals

**Goals:**
- One marked toggle that makes every booking fail in `api`, diagnosable from Grafana alone: error
  on the deliver span, error log, counted line at 0.
- Dashboard shows stored vs counted bookings per minute.
- Agent uses only the Grafana MCP on stage; Grafana runs from the morning.
- Smoke check usable at the end of the talk.

**Non-Goals:**
- Retrying, recovering or recounting lost bookings; dead-letter queue.
- An `api` error panel; counts per event on the dashboard.
- Removing the Aspire MCP from the repo; AppHost changes.
- Windows verification.

## Decisions

### D1. Processing failure telemetry (spec `event-booking-count`)
In the consumer's catch: `Activity.Current?.AddException(ex)` and
`SetStatus(ActivityStatusCode.Error)` before the existing error log and nack. `Activity.Current`
there is the RabbitMQ `deliver booking-stored` span (the tag `acme.event.key` is already set on it;
smoke check verifies). Log message and template unchanged; `EventKey` may be null when the message
cannot be read. Alternative: rethrow and let the client library record it: changes ack behaviour;
rejected.

### D2. Counted-bookings metric (ADR-0034)
`api` gets `static readonly Meter("Acme.Bookings")` and counter `acme.bookings.counted` (no
attributes), `Add(1)` after `StringIncrementAsync` succeeds, before the log. ServiceDefaults adds
`.AddMeter("Acme.Bookings")` next to `Acme.Forms` (literal, same reason). Prometheus series
`acme_bookings_counted_total{job="api"}` (task 1.3 confirms the name and label). The consumer calls
`Add(0)` once when it starts (PO 2026-10-22): a series first exported at 1 has no earlier sample, so
`increase()` reads the first booking after an `api` start as 0, and the Act 3 fix needs that restart
(found in `extend-demo` task 2.1). The site's stored counter has the same gap after a `site` restart; not changed.

### D3. Dashboard panel (ADR-0034, spec `demo-dashboard`)
"Form submissions by outcome" gridPos `w` 24 -> 12. New timeseries panel "Bookings: stored vs
counted" at `x` 12, `y` 16, `w` 12, `h` 8, Prometheus data source, unit `short`, two targets:
- A, legend `stored`:
  `sum(increase(acme_forms_submissions_total{job="site",acme_form="event_booking",acme_form_outcome="stored"}[1m])) or vector(0)`
- B, legend `counted`:
  `sum(increase(acme_bookings_counted_total{job="api"}[1m])) or vector(0)`
Next free panel `id`. No other panel changes. Alternatives: ADR-0034.

### D4. Grafana MCP throughout (ADR-0033, spec `agent-telemetry-access`)
No repo config change. `claude-stage.md` "Telemetry access": Grafana MCP only, from the start;
"How to work": after a fix, ask the presenter to rebuild the named resource (Aspire Rebuild; Restart does not recompile) (`site` or `api`); no
mention of Aspire resource commands; resource list gains `api`, `cache`, `queue`. README gains
"Match the talk's agent setup": the talk ran Grafana from the start and the agent on `grafana`
only; `/mcp` -> `aspire` -> Disable (per folder); start `lgtm` before opening Claude Code.
Runthrough: Morning starts `lgtm` after the final restart, disables `aspire`, checks `claude mcp
list`; Go-live is the tunnel and traffic; Act 1 prompt and all check queries use Grafana.

### D5. Smoke running mode (spec `local-environment`)
`dotnet run tests/smoke/smoke.cs -- running`: target apphost; skips "home trace (first load)" and
"no pre-start telemetry in Grafana" (both printed as `N/A ... not applicable: running mode`); the
pre-start request to `/smoke-pre/` is not made. Everything else unchanged. Header comment documents
the mode without naming acts. New apphost check (both modes, needs `lgtm`): after the smoke
booking, `acme_bookings_counted_total{job="api"}` increases within 60 s (Prometheus query via
Grafana, as the page-type metric check). Alternatives: detect the planted state (leaks the answer
to the stage agent, which can read `tests/`); expected FAIL on stage; rejected (PO 2026-10-22).

### D6. Planted toggle shape (spec `event-booking-count`)
Same shape as Act 1 and Act 2: marker line, one explanation line, `Fix:` line, `Initial Demo State`
line, then the toggled line(s) in the consumer, committed fixed. The specifics are in the block
brief and the runthrough only. Rebuilding `api` (Aspire Rebuild) applies either state; no reset needed.

### D7. Presenter runthrough (Architect, `.temp/runthrough.md`; PO copies to `docs/presenter/`)
Planted problems row and entry for Act 3; Morning: start, smoke #1 (fixed state), then set initial
states, amend commit, reset booking counts, restart, start `lgtm`, Claude Code with `aspire`
disabled; Act 3 section after Act 2 (prompt, expected path, check queries, speaking notes on the
panel, fallback); rule "no event bookings before Act 3"; Act 2 speaking note to park the bookings
gap; Close: optional smoke #2 (`-- running`). No timings.

## Risks / Trade-offs

- [Burst 500s in Act 2 also hit bookings] -> failed posts are not stored, so they don't affect the
  panel; after the Act 2 fix burst bookings widen the gap before Act 3 (wanted).
- [Panel shows the gap during Act 2] -> speaking note: park it.
- [`or vector(0)` hides a missing metric pipeline] -> smoke check D5 verifies the counted series
  exists on the fixed state each morning.
- [Act 1 values in Loki show up in later agent output] -> made-up details only (PO accepted).
- [Agent reads the consumer comment and skips the telemetry] -> same exposure as Acts 1-2; the
  runthrough's open decision on stripping explanatory lines on `demo` covers Act 3 too.
- [Grafana MCP not connected when Claude Code opens] -> runthrough check `claude mcp list` after
  `grafana-mcp` is Running; `/mcp` reconnect.

## Migration Plan

No data migration. Existing booking counts unaffected. Rollback: revert the commit.

## Open Questions

None. Talk date: not updated (PO 2026-10-22, question dropped).
