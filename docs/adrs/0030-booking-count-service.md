# ADR-0030: Booking count service

## Status
Accepted (2026-10-22).

## Context
Every span comes from `site`; SQL Server and smtp4dev show only as uninstrumented peers. A
website -> API -> cache path lets traces cross services and hides problems deeper than the site.
Feature: a booking count on event pages. The site must keep working when the service fails. The
site also runs alone on SQLite with no AppHost. ServiceDefaults (ADR-0008, ADR-0028) already gives any .NET
project OTLP export, HttpClient service discovery and resilience, and log redaction.

## Decision
- New ASP.NET Core minimal API project `src/SuperPower.Api` (.NET 10), Aspire resource `api`,
  referencing `SuperPower.ServiceDefaults` (`AddServiceDefaults`, `MapDefaultEndpoints`). HTTP
  only, no auth (local, not exposed through the tunnel). Launch profile on
  http://localhost:5290; no external endpoint.
- Read path, HTTP (event key = Umbraco content key, a GUID):
  `GET /events/{eventKey:guid}/bookings` -> `200 {"count": n}`; `n = 0` when no entry.
- Write path, queue (ADR-0032): a hosted consumer in `api` takes `booking-stored` messages and
  adds one to the event's count. No write endpoint.
- Site read client: typed `HttpClient` behind `IBookingCounts.GetAsync(Guid) -> int?`, base
  address `http://api` via service discovery. Its resilience pipeline replaces the
  ServiceDefaults one: `RemoveAllResilienceHandlers()` then `AddStandardResilienceHandler`
  with total timeout 2 s and attempt timeout 1 s (retries and circuit breaker at defaults).
  Any failure is caught, logged as a Warning without personal data, and returns no count; the
  call never throws. When the site has no `api` endpoint configured (run alone), a no-op
  implementation returns no count without a request.
- Calls: the event view reads the count on every render; the booking post publishes after
  the submission is stored and before the owner notification (ADR-0027 order otherwise
  unchanged). The AppHost does not make `site` wait for `api` or the queue.
- Telemetry: no new ActivitySource or Meter in the site; one span `acme.forms.publish` on the
  existing `Acme.Forms` source around the publish (tag `acme.event.key`, Error on failure), so a
  publish that fails before RabbitMQ.Client starts its own span still shows in the trace. HttpClient spans and W3C propagation
  (ServiceDefaults) link the read; RabbitMQ client spans link the write (ADR-0032). `api`
  emits ASP.NET Core server spans and metrics, tags its server span and its message-handling
  span `acme.event.key`, and logs `Booking recorded for {EventKey}, count {Count}` per message.
  Logs pass through `PersonalDataLogProcessor`; the AppHost passes `Telemetry__RedactionKey`
  to `api` as to `site`.

## Alternatives
- Write over HTTP (`POST`, first version of this ADR): same shape as the read, so the booking
  trace shows nothing new; replaced by the queue (PO 2026-10-21).
- Separate worker resource for the consumer: a third process for no extra story; rejected.
- gRPC: richer RPC spans, but proto tooling and HTTP/2 setup for no extra story; rejected.
- MVC controllers: more code than one endpoint needs; rejected.
- Count in the site's SQL database: no second service, no cross-service trace; rejected.
- Site waits for `api` at start: couples start-up to the service the site must survive
  without; rejected.
- ServiceDefaults resilience (10 s attempt, 30 s total): a stopped service would hold the page
  for many seconds; rejected for this client.

## Consequences
- Every event page render produces a cross-service trace over HTTP; every stored booking one
  over the queue.
- A stopped or slow `api` adds up to 2 s to an event page; visible on the page latency metric
  and as retry/timeout spans. Bookings are not slowed by `api`.
- Counts lag bookings by the queue hand-over (seconds; accepted, PO 2026-10-21) and drift from
  stored bookings when an update fails (display figure).
- One more project in the solution and one more process on the presenter's laptop (~80 MB).
