## Why

The booking flow added by `add-booking-api` is fail-soft: when booking processing breaks, the visitor
still gets a confirmation, so nobody notices and the site dashboard stays green. A third act shows
the failure that only telemetry across services reveals, and that outcomes need watching as well as
errors. Running the agent against Grafana for the whole talk keeps one telemetry source on stage and
saves the agent's queries against a second server. The next delivery is about a month away.

## What Changes

Act 3, silent booking failure (production stage, after Act 2):
- A planted problem on `main`, marked `// Demo Code : Act 3` with an explanation, a fix and the
  initial demo state, like the Act 1 and Act 2 markers. `main` is committed in the fixed state; the
  presenter sets the initial state in the stage clone. What fails and how it is fixed are recorded
  only in that comment and the presenter runthrough, not in OpenSpec or other docs.
- With the problem planted, every stored event booking fails in the booking service: the visitor
  sees the confirmation and the owner email arrives, the event's count never changes. One booking
  reproduces it; no low-traffic fallback.
- The failure is diagnosable from Grafana: the booking's trace shows the failed processing step in
  the booking service as an error, and the booking service logs an error. Both carry only the
  event's identity.
- New demo dashboard panel "Bookings: stored vs counted" (row 3, right half; "Form submissions by
  outcome" moves to the left half). The two lines move together when healthy; any gap is lost
  bookings, whatever the cause. Bookings lost before the fix are not recounted, so the gap remains
  after the fix.

Grafana throughout:
- The production-stage platform runs from the morning for the whole talk. The Aspire dashboard
  remains the presenter's view for showing logs, traces and metrics; the agent uses only the
  Grafana MCP in every act. Go-live becomes the tunnel and demo traffic only.
- The Aspire MCP is disabled in the stage clone only; the repo keeps both servers. The README
  states that the talk ran the agent on the Grafana MCP only and how to disable the Aspire MCP to
  match. The runthrough's morning steps include disabling it and checking Grafana is connected.
- The agent cannot restart services on stage: it asks the presenter, who restarts from the Aspire
  dashboard.
- Accepted (PO 2026-10-22): Act 1's unmasked values reach Grafana and stay there for the talk; the
  presenter uses made-up details.

Smoke check:
- Morning: the smoke check runs against the fixed state, before the initial demo states are set.
- End of talk: optional on-stage fallback after all fixes, showing a good state. Needs a mode for
  an environment that has already served traffic (skips the first-load checks), worded so it does
  not hint at any planted problem.

Presenter runthrough (private): Act 3 section (prompt, expected path, check queries, speaking
notes), Planted problems row for Act 3, reordered morning steps, Act 1 prompt and check queries moved
to Grafana, rule "no event bookings before Act 3". Timings are set by the PO, not planned.

Out of scope: retrying or recovering lost bookings, an error panel for the booking service, removing
the Aspire MCP from the repo, Windows verification.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `demo-dashboard`: "Demo dashboard contents" gains the bookings stored vs counted panel and the
  row 3 layout.
- `event-booking-count`: booking processing failures are recorded in telemetry (error on the
  booking's trace, error log in the booking service, event identity only).
- `agent-telemetry-access`: the agent can diagnose and verify every act from the production-stage
  platform alone, with the Aspire MCP disabled; restarts go through the presenter.
- `local-environment`: README and demo run-through describe running with the production-stage
  platform from the start and disabling the Aspire MCP to match the talk.

## Impact

- Booking service: the planted, marked problem; telemetry on processing failure.
- Demo dashboard definition and its spec.
- Smoke check: new mode for an already-running environment; morning order.
- README, stage agent instructions (`claude-stage.md`), presenter runthrough working copy.
- ADR-0022 (Aspire MCP local, Grafana at go-live): the talk no longer follows its stage split;
  the Architect records a superseding or amending ADR at propose.
- `CLAUDE.md` Stack and `openspec/config.yaml` context describe the two-stage agent setup and the
  16 Oct date; the stack text needs updating (Architect); the date stays (PO 2026-10-22).
