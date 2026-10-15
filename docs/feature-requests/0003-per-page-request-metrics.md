# FR-0003: Request metrics per page type

## Status
Built in change `add-demo-dashboard` (2026-10-15; ADR-0024). Source: `rework-aspire-first` task 6.3 (`verification.md` finding F1).
Built together with the dashboard (PO 2026-10-15).

## Request
The site's HTTP request metrics (`http.server.request.duration`) carry the Umbraco page type, so
request rate and duration can be split by page type in Grafana and queried by the agent.

## Why
Umbraco serves every content page through one catch-all route, so the default ASP.NET Core
metric has no usable route label: Home cannot be separated from any other page, and the three
form POSTs share one series. Per-page durations are available only from sampled traces. The
dashboard needs per-page rate and latency panels (with exemplars to that page's traces), and the
agent should answer "how long does page X take" from metrics.

## Acceptance criteria
- Request-duration metric samples carry the document type alias of the rendered page (e.g.
  `home`, `event`, `service`); requests with no Umbraco page (404, static, unrouted) carry one
  fixed value.
- The label has a fixed, small set of values: never the URL path or query string.
- The request span carries the same value, so metric and trace filters match.
- In Grafana, Home's request duration can be queried on its own; the demo dashboard shows
  per-page-type rate and latency.
- No personal data in the label.

## Notes
- Candidate route: after Umbraco resolves the published request, add the tag through
  `IHttpMetricsTagsFeature` and set it on `Activity.Current`. Small site change, no new
  package; design and ADR in `add-demo-dashboard`.
- Once built, `agent-telemetry-access` scenario "Agent checks a metric" can be tightened back
  to a page's request-duration metric (reworded 2026-10-15 in `rework-aspire-first`).
