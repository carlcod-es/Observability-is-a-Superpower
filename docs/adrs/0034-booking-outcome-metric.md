# ADR-0034: Counted-bookings metric in the booking service and a stored vs counted panel

## Status
Accepted (2026-10-23, change `extend-demo`). Deviation (PO 2026-10-22): `Add(0)` at consumer start, found
in `extend-demo` task 2.1 (see Decision and Consequences). Series confirmed as `acme_bookings_counted_total{job="api"}`.

## Context
The booking flow is fail-soft (ADR-0030, ADR-0032): a booking the booking service fails to
process leaves no trace on the site's panels. The PO (2026-10-22) wants a demo dashboard panel
comparing bookings stored with bookings counted, so any loss shows, whatever its cause. The
stored side exists: `acme.forms.submissions` with `acme.form=event_booking`,
`acme.form.outcome=stored` (ADR-0027). The counted side has no signal on the platform. Services
restart during the talk (fixes), which resets cumulative counters.

## Decision
- `api` emits counter `acme.bookings.counted` (meter `Acme.Bookings`, no attributes) after each
  successful cache increment, and `Add(0)` once at consumer start (see Consequences). ServiceDefaults registers the meter by literal name, as for
  `Acme.Forms`.
- Demo dashboard panel "Bookings: stored vs counted", row 3 right half (form submissions panel
  left half), per-minute series:
  `sum(increase(acme_forms_submissions_total{job="site",acme_form="event_booking",acme_form_outcome="stored"}[1m])) or vector(0)`
  and `sum(increase(acme_bookings_counted_total{job="api"}[1m])) or vector(0)`.
- On processing failure the consumer marks the deliver span as an error with the exception
  (log entry already exists).

## Alternatives
- Count "Booking recorded" log lines in Loki: mixed data sources in one panel, log-derived
  metric depends on log wording and level; rejected.
- Running totals (cumulative lines with a growing gap): raw counters reset on each service
  restart; reset-safe totals need a Grafana transformation tied to the 15-minute window, so the
  gap would scroll out mid-act; rejected for simplicity. Per-minute series match the form
  submissions panel.
- Counter with an `acme.event.key` attribute: one series per event, not needed for the panel;
  rejected.
- Error-log panel for `api`: shows only failures that log errors; rejected (PO 2026-10-22).

## Consequences
- Lost bookings show as minutes where stored is above 0 and counted is 0; after the fix the lines
  meet again. The total lost is a query (agent or Explore), not a number on the panel.
- `or vector(0)` keeps both lines at 0 rather than "no data" before any booking.
- `api` calls `Add(0)` on the counter at consumer start (PO 2026-10-22): a series first exported
  at 1 has no earlier sample, so `increase()` (panel and agent queries) would miss the first booking
  after each `api` start, including the proof booking after the Act 3 fix. The site's stored counter
  has the same gap after a `site` restart; the Act 2 burst recreates it before Act 3, so unchanged.
- Prometheus series names depend on the collector's naming (`_total` suffix, `job` = service
  name); verified in the change's tasks.
