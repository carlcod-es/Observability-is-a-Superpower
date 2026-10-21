## Context

Current state: AppHost runs `site` (Umbraco), `sql`, `smtp4dev`, `collector`, `lgtm` (explicit
start) and `grafana-mcp`. `collector` uses `.WithAppForwarding()` to re-point project OTLP at
itself. ServiceDefaults gives each project OTLP export, HttpClient service discovery and
standard resilience, and `PersonalDataLogProcessor` (ADR-0028). Event pages render
`Event.cshtml`; the booking post runs `AcmeFormsController.SubmitEventBooking` -> `Handle`
(store, log, notify; ADR-0027). FR-0006 asked for a second instrumented resource and a queue.

Fixed constraints applied: .NET 10, Aspire 13.6.1 line, OpenTelemetry via ServiceDefaults,
Aspire MCP / Grafana MCP unchanged (ADR-0022). Gaps they imply, decided here: how the new
project exports (ServiceDefaults), whether the collector forwards it (task 1.5 verifies), how
agents see it (automatically, as resources), and that agents get no cache or queue access.

Bar: **demo** (CLAUDE.md "Principle"). One endpoint, one consumer, two Redis commands, one
typed client, one publisher. Tests: xUnit for the site-side logic that decides what the
visitor sees and that must never fail a request (count text, fail-soft client, fail-soft
publisher); the API, consumer and cross-service traces are verified by the smoke check and by
observing traces. No API test project.

Planted problems: none in this change (PO 2026-10-21). They are chosen later and recorded only
in `docs/presenter/runthrough.md`.

## Goals / Non-Goals

**Goals:**
- Event page traces `site` -> HTTP -> `api` -> Redis; booking traces `site` -> queue -> `api`
  -> Redis, each one trace.
- Site unaffected beyond a 2 s cap when `api`, `cache` or `queue` fails.
- Short restart loop: containers persistent, no rebuild of broker or cache per run.
- Same telemetry destinations and log masking for `api` as for `site`.

**Non-Goals:**
- Counts on the Events list, count rebuild, retries of failed updates, booking limits.
- Dashboard panels for `api` or the queue; Grafana dashboards unchanged.
- Agent access to Redis or RabbitMQ.

## Decisions

### D1. Booking service (ADR-0030)
Minimal API project `src/SuperPower.Api`, resource `api`, references ServiceDefaults.
`GET /events/{eventKey:guid}/bookings` -> `{"count": n}`; server span tagged `acme.event.key`.
No write endpoint. Alternatives (HTTP write, separate worker, gRPC, controllers, SQL table):
ADR-0030.

### D2. Cache (ADR-0031)
Redis `cache` (`oisp-cache`, volume `oisp-cache-data`, persistent), client
`AddRedisClient("cache")` in `api` with tracing on; keys `event:{eventKey}:bookings`, `INCR`
per message / `GET` per read. Alternatives: ADR-0031.

### D3. Queue (ADR-0032)
RabbitMQ `queue` (`oisp-queue`, persistent lifetime, no volume, management plugin). Queue
`booking-stored` (durable, not exclusive, not auto-delete; declared identically by publisher
and consumer), default exchange, body `{"eventKey":"<guid>"}`. Clients `AddRabbitMQClient("queue")`
in `api` and, when `ConnectionStrings:queue` is set, in `site` with `DisableHealthChecks =
true`. Alternatives (Redis Streams, MassTransit, Service Bus emulator, Kafka, session
lifetime, volume): ADR-0032.

### D4. AppHost wiring
```csharp
var cache = builder.AddRedis("cache")
    .WithContainerName("oisp-cache")
    .WithDataVolume("oisp-cache-data")
    .WithLifetime(ContainerLifetime.Persistent);

var queue = builder.AddRabbitMQ("queue")
    .WithContainerName("oisp-queue")
    .WithManagementPlugin()
    .WithLifetime(ContainerLifetime.Persistent);

var api = builder.AddProject<Projects.SuperPower_Api>("api")
    .WithReference(cache)
    .WithReference(queue)
    .WithEnvironment("Telemetry__RedactionKey", redactionKey)
    .WaitFor(cache)
    .WaitFor(queue)
    .WaitFor(collector);

// site: add .WithReference(api).WithReference(queue); no WaitFor on either (site must run without them)
```
`api` gets no external endpoint (not reachable through the tunnel).

### D5. Consumer in `api` (ADR-0030, ADR-0032)
`BackgroundService`: on start, create a channel from the `IConnection`, declare
`booking-stored`, consume with `AsyncEventingBasicConsumer`, manual ack. Per message: parse
`eventKey`; `INCR`; tag `acme.event.key` on the current activity; log
`Booking recorded for {EventKey}, count {Count}`; ack. On failure: log Error with
`{EventKey}` and exception, nack without requeue. If task 1.2 finds delivery spans only
linked to the publish, start an activity `booking-stored process` with the propagated context
as parent (ADR-0032).

### D6. Site read client (ADR-0030)
`src/SuperPower.Web/Events/BookingCounts.cs`:
- `IBookingCounts { Task<int?> GetAsync(Guid eventKey, CancellationToken ct = default); }`
- `BookingCountsClient(HttpClient, ILogger<…>)`: any exception or non-2xx -> Warning
  `Booking service call failed for {EventKey}` (with exception), returns `null`. Never throws.
- `NoBookingCounts`: returns `null`, no request.
- Registered in `AcmeFormsComposer`: when configuration has `services:api` children -> typed
  client, `BaseAddress = http://api`, `.RemoveAllResilienceHandlers()
  .AddStandardResilienceHandler(o => { o.TotalRequestTimeout.Timeout = 2 s;
  o.AttemptTimeout.Timeout = 1 s; })`; otherwise `NoBookingCounts`. If the options are
  rejected at start, the worker stops and reports.
- `BookingCountText.Format(int? count) -> string?`: `null` for `null` or `<= 0`; `"1 person has
  booked this event"`; `"{n} people have booked this event"`.

### D7. Site publisher and booking post (ADR-0032)
`src/SuperPower.Web/Events/BookingEvents.cs`:
- `IBookingEvents { Task PublishStoredAsync(Guid eventKey, CancellationToken ct = default); }`
- `RabbitBookingEvents(IServiceProvider, ILogger<…>)`: inside the call, resolve `IConnection`,
  create a channel, declare `booking-stored`, publish; the whole call wrapped in a 2 s timeout
  (`WaitAsync`). Any exception or timeout -> Warning `Booking event not queued for {EventKey}`,
  swallowed. Channel per publish is acceptable at demo volume.
- `NoBookingEvents`: no-op.
- `Program.cs`: `AddRabbitMQClient("queue", s => s.DisableHealthChecks = true)` only when
  `ConnectionStrings:queue` is set; composer registers `RabbitBookingEvents` under the same
  condition, otherwise `NoBookingEvents`.
- `AcmeFormsController`: inject `IBookingEvents`. In the event-booking path, after
  `repository.InsertAsync` succeeds and the scope completes, before `Notify`, call
  `PublishStoredAsync(CurrentPage.Key)`. Contact and service enquiry unchanged. Hook shape is
  the worker's choice; constraint: runs only for a stored event booking, never throws.

### D8. Event page
`Event.cshtml` injects `IBookingCounts`, awaits `GetAsync(Model.Key)`, renders
`<p class="event-bookings">` with `BookingCountText.Format(...)` when non-null, under the event
date, for upcoming and past events. One small rule in `wwwroot/css/site.css` if needed to match
`.event-when` (ADR-0021).

### D9. Telemetry
- No new ActivitySource or Meter in the site. Page trace: site request span -> HttpClient span
  (error on failure or timeout; retries as sibling spans) -> `api` server span -> Redis `GET`.
  Booking trace: site request -> `acme.forms.submit` -> RabbitMQ publish span -> `api` deliver
  / process span -> Redis `INCR`.
- ServiceDefaults gains `AddSource("RabbitMQ.Client.*")` only if the Aspire client does not
  register the sources itself (task 1.2 checks).
- `api` request metrics (`http.server.request.duration`, `http.route`) and site
  `http.client.request.duration` come from ServiceDefaults; Grafana `job="api"` / `job="site"`.
- Logs: `api` uses ServiceDefaults logging -> `PersonalDataLogProcessor`. The Serilog
  forwarding sink registered by ServiceDefaults is inert in `api` (no Serilog pipeline).
- Aspire MCP lists `api`, `cache` and `queue` without configuration changes; Grafana MCP sees
  `service_name="api"` once `lgtm` runs.

### D10. Tests and checks
- `tests/SuperPower.Web.Tests`: `BookingCountText` (null, 0, 1, 3); `BookingCountsClient` with
  a fake `HttpMessageHandler` (200 parses, 500 -> null, throwing handler -> null, event key in
  the path); `RabbitBookingEvents` with a throwing and a never-completing connection (returns,
  does not throw, finishes within ~2 s); registration switches if testable without an Umbraco
  runtime (otherwise covered by the standalone smoke).
- `tests/smoke/smoke.cs` (apphost mode): open the first upcoming event page, book once, poll the
  page up to 5 s until the count text rises by one; Tempo has a trace with `api` spans carrying
  `acme.event.key` and one booking trace with spans from `site` and `api`. Standalone mode:
  event page has no `event-bookings` element and a booking still returns a reference.

### D11. Load comparison (PO 2026-10-21)
Question: does the change slow the site for a room of 150-200 people scanning the QR code?
Measured before any change code and again after section 4, same conditions, so the delta is
the change's cost. Results in `verification.md`; raw files in `tests/load/results/`.

Conditions (both runs identical): presenter Mac on mains power, macOS-performance prep applied
(`docs/presenter/macos-performance.md`); same build configuration as the stage run at the time;
fresh AppHost start; `lgtm` started; one discarded warm-up run (`--users 5 --duration 20`) so
first-hit view compilation is excluded; traffic generator (`tests/traffic/traffic.cs`) not
running.

Runs:
- Local: `node tests/load/load-test.mjs --base http://localhost:21258 --users 150 --duration 120
  --ramp 30 --label <baseline|after>-local` (HTTP endpoint, so Node needs no dev certificate).
- Tunnel (PO): same with the default base (public URL) and `--label <baseline|after>-tunnel`.
- During each run, from 30 s in: `dotnet run tests/load/burst.cs -- 10 8` (80 concurrent posts
  across the three forms, a third of them event bookings).
- After runs: `--compare <baseline file>`.

Recorded: per-page avg/p50/p95/failures and TOTAL throughput; burst outcome (302 / 500
counts); `docker stats` peak per container; site (and after: `api`) process memory; swap
(Activity Monitor); after only: time for `booking-stored` to drain to 0 after the burst.

Criteria for "after" (reported met / not met; PO decides on misses):
- No failed page requests; burst all 302 (as baseline).
- Event pages p95 at most 50 ms above baseline; other pages p95 within 10 % of baseline.
- `booking-stored` drains within 5 s of the last post.
- No swap growth during the run.

`load-test.mjs` is GET-only; bookings come from the burst. The two together approximate the
room; the venue network is not modelled (tunnel run shows the uplink of the test location
only).

### Dependencies (new; PO approves with this change)
`Aspire.Hosting.Redis`, `Aspire.StackExchange.Redis`, `Aspire.Hosting.RabbitMQ`,
`Aspire.RabbitMQ.Client`, all 13.6.1, in `Directory.Packages.props`. The Redis pair is in the
local NuGet cache; the RabbitMQ pair at 13.6.1 is not (downloaded on restore).

## Risks / Trade-offs

- [Delivery span linked, not parented] -> task 1.2 checks; consumer parents its own span from
  the propagated context (D5).
- [Site connects to RabbitMQ lazily and the Aspire client retries connection creation] -> the
  2 s `WaitAsync` cap bounds the post; the abandoned attempt continues in the background and
  may log; accepted.
- [Redis spans flushed on an interval] -> may appear seconds after the request spans; smoke
  check polls Tempo as it does for site spans.
- [Collector forwarding misses `api`] -> task 1.5 checks `api` in the dashboard and in Grafana;
  if missing, add the equivalent per-resource wiring and report.
- [Persistent Redis or RabbitMQ password changes between runs] -> task 1.5 restarts the AppHost
  and checks counts still read and messages still flow.
- [`api` stopped -> up to 2 s per event page] -> intended; shown in latency and retry spans.
- [Count lag after a booking] -> accepted (PO 2026-10-21); the confirmation page may show the
  old count.
- [Count drift vs stored bookings] -> accepted display figure (spec `event-booking-count`).
- [Laptop memory] -> Redis ~10 MB, RabbitMQ ~100-150 MB, `api` ~80 MB beside the existing stack.
- [Talk material] -> README and runthrough must not describe planted problems; this change adds
  none.

## Open Questions

None.
