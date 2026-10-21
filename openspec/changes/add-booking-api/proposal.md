## Why

Every span in a trace today comes from the site; the database and mail catcher show only as
uninstrumented peers. Traces never show a request crossing services, and every planted problem
lives in the site. A second service with its own data store and a queue in front of it give
traces more depth to show on stage, one synchronous hop and one asynchronous hop, and give the
presenter more layers to hide problems in. Delivers FR-0006 (second instrumented resource).

## What Changes

- Event pages show how many people have booked the event: "1 person has booked this event" or
  "N people have booked this event". Hidden when nobody has booked or the count is unavailable.
- A separate booking service, started with the rest of the environment, holds the count per
  event in its own cache. The site asks it directly for the count when an event page is
  rendered.
- After each stored event booking, the site puts a "booking stored" message on a queue. The
  booking service takes messages from the queue and adds one to the event's count. The count
  catches up within a few seconds of the booking, not immediately (PO 2026-10-21).
- The site works without the booking service or the queue: event pages render and bookings
  succeed when either is down or slow; the count is then hidden or not updated. While only the
  booking service is down, messages wait in the queue and the counts catch up when it returns.
  Failures show only in telemetry.
- The booking service, its cache and the queue appear as resources in the dashboard. An event
  page trace spans the site, the booking service and its cache read. An event booking trace
  spans the site's submission, the message being put on the queue, the booking service taking
  it, and the cache update. The service's logs, traces and metrics reach the local dashboard
  and the production-stage platform like the site's.
- The booking service receives only the event's identity, never form fields or other personal
  data, by call or by message. Its logs pass through the same personal-data masking as the
  site's.
- Counts are a display figure, not a record: stored submissions stay the source of truth. A
  count drifts from the stored bookings when an update fails, and resets when the cache is
  reset. Queued messages need not survive a queue reset (PO 2026-10-21).
- No planted problems in this change (PO 2026-10-21). The PO decides later how many acts and
  problems to show; they go in the presenter runthrough, not in planning documents.

Out of scope: counts on the Events list or any page other than event pages; booking limits or
places (CLAUDE.md "Events"); rebuilding counts from stored submissions; retrying failed count
updates; agent access to the cache or queue (agents never get data store access, CLAUDE.md
"Scope"); new dashboard panels.

PO decisions (2026-10-21): the service does the event booking count; the site degrades
quietly when the service fails; problems are planted later; FR-0006 folded in with the queue
on the booking path; a few seconds' lag on counts after a booking is accepted; queue contents
need not persist, and the queue is set up for the shortest restart loop.

## Capabilities

### New Capabilities
- `event-booking-count`: booking count on event pages, read from a separate booking service and
  updated through a queue; degrades quietly; no personal data reaches the service; telemetry
  across site, queue, service and cache.

### Modified Capabilities
- `local-environment`: one-command start includes the booking service, its cache and the queue;
  a trace can span more than one service.

## Impact

- New service, cache and queue in the local environment; one more application in the solution.
- Site: event page rendering calls the service; the event booking post puts a message on the
  queue.
- New dependencies (PO approval with this proposal): cache and queue hosting and client
  integrations (design.md, ADR-0031, ADR-0032).
- New ADRs: ADR-0030 (booking service), ADR-0031 (cache), ADR-0032 (queue).
- README (run instructions, resources), smoke check, presenter runthrough (database reset also
  resets the cache and queue; PO).
- Site run alone (file-based database) gets no count; pages and bookings work as today.
- Load comparison for an audience of 150-200 (PO 2026-10-21): the site is load-tested before
  the change and again after it, under the same conditions, and the results compared.
- FR-0006 delivered; `PLANNING.md` change order and todos.
