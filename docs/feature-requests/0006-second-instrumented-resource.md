# FR-0006: Second instrumented resource in the trace

## Status
Open (2026-10-16), after the talk (PO). Source: PO 2026-10-16, on the Home trace in the Aspire
dashboard listing only `site`.

## Request
Add a second service that emits its own telemetry, so a trace visibly spans more than one
resource. Candidate (PO): an analytics service that records each page request, fed by the site
through a message queue.

## Why
Today every span comes from `site`. SQL Server and smtp4dev emit no telemetry; the dashboard shows
them only as uninstrumented peers on the site's client spans, so traces never show propagation
across services. A cache (e.g. Redis) would not change this: it is another uninstrumented peer.

## Acceptance criteria
- A Home page request produces one trace with spans from `site` and the new service; the
  dashboard lists both resources.
- With a queue between them, the trace shows publish (site) and consume (service) linked by
  propagated trace context.
- Page response time does not depend on the service: the site does not wait for the analytics
  write, and a stopped service or queue does not fail the page.
- Analytics records hold page and page type (`acme.page_type`) only: no IP, user agent, query
  string, cookies or other personal data. The new service's logs go through
  `PersonalDataLogProcessor` (ADR-0028).
- Telemetry reaches both the Aspire dashboard and Grafana LGTM, like the site's.
- Small memory and CPU cost on the presenter's laptop alongside SQL Server, smtp4dev, collector
  and LGTM.

## Notes
- Options for `/opsx:propose`: queue broker (e.g. RabbitMQ container) with a .NET worker
  consumer; or direct HTTP from site to a small .NET API (simpler, synchronous unless
  fire-and-forget). The broker itself stays an uninstrumented peer; the second resource is the
  consumer.
- Where the service stores analytics is open; agents never get database access (CLAUDE.md
  "Scope").
- Alternative considered (2026-10-16): move owner notification into its own service; touches
  ADR-0027 and the form spans, so larger.
