# ADR-0024: Page-type label on request metrics and spans

## Status
Accepted (2026-10-15).

## Context
Umbraco serves every content page through one catch-all route, so the ASP.NET Core
`http.server.request.duration` metric has no useful `http.route`: Home cannot be separated
from other pages, and form posts share one series.
The demo dashboard (ADR-0023) and the agent (Grafana MCP, ADR-0033) need per-page rate and
latency from metrics. The label must stay low-cardinality and free of personal data.

## Decision
- Tag name `acme.page_type` (Prometheus: `acme_page_type`), namespaced like the `Acme.Forms`
  telemetry (ADR-0016).
- Value: the content type alias of the page Umbraco rendered (`home`, `page`, `contact`,
  `events`, `event`, `services`, `service`); `none` when no content page was rendered (404,
  static files, back office, health).
- Set by one site middleware registered in `Program.cs` before `app.UseUmbraco()`: after the
  rest of the pipeline has run, it reads the published content from Umbraco's route values and
  adds the tag to `IHttpMetricsTagsFeature.Tags` and to the request activity
  (`IHttpActivityFeature`). Same value on metric and span.
- No new package.

## Alternatives
- Overwrite `http.route` with the page type: breaks the semantic convention and the community
  dashboard's route panels.
- Tag the URL path: unbounded cardinality, can carry query strings; rejected.
- Page name or key instead of type: grows with content and splits single pages; the PO chose
  page type (2026-10-15).
- Umbraco routing notification: runs mid-routing and needs the HTTP context accessor; the
  middleware sees the final result in one place.
- Per-page durations from traces only: sampled, no dashboard rate panel; rejected.

## Consequences
- Request rate and duration can be split by page type in Grafana and queried by the agent.
- Static file requests are counted in the metric under `none` (metrics are not filtered by
  ADR-0008's path list; traces are).
- A new document type adds one label value; acceptable at demo scale.
