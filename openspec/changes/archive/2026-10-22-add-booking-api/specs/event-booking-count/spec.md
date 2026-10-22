## Purpose

Shows on each event page how many people have booked it, using a count kept by a separate
booking service and updated through a queue, so page and booking traces cross more than one
service (ADR-0030, ADR-0031, ADR-0032).

## ADDED Requirements

### Requirement: Booking count on event pages
An event page SHALL show the event's booking count from the booking service: "1 person has
booked this event" for one booking, "N people have booked this event" for N greater than one.
It SHALL show no count text when the count is zero or unavailable. This applies to upcoming
and past events.

#### Scenario: Event with bookings
- **WHEN** the booking service holds a count of 3 for an event and a visitor opens the event page
- **THEN** the page shows "3 people have booked this event"

#### Scenario: One booking
- **WHEN** the count is 1
- **THEN** the page shows "1 person has booked this event"

#### Scenario: No bookings
- **WHEN** the event has no count in the booking service
- **THEN** the page shows no count text

### Requirement: Stored booking updates the count
After an event booking is stored, the site SHALL put a message identifying the event on the
queue; the booking service SHALL take it and add one to that event's count. The count SHALL
reflect the booking within 5 seconds while the queue, service and cache are running; it MAY
not yet reflect it on the page shown straight after the confirmation. Invalid posts, bookings
refused because the event has ended, and posts that fail to store SHALL NOT change the count.
The count is a display figure: stored submissions remain the record, the count is not rebuilt
from them, and a failed update is not retried.

#### Scenario: Book an event
- **WHEN** a visitor submits a valid booking on an event page whose count is 2
- **THEN** the confirmation shows as today, and within 5 seconds the event page shows
  "3 people have booked this event"

#### Scenario: Invalid booking
- **WHEN** a visitor submits the booking form without an email
- **THEN** no message is queued and the count is unchanged

#### Scenario: Counts are per event
- **WHEN** a booking is stored for one event
- **THEN** the counts of all other events are unchanged

#### Scenario: Service back after an outage
- **WHEN** the booking service is stopped, two bookings are stored for an event, and the
  service is started again
- **THEN** within 5 seconds of the service running, the event's count includes both bookings

### Requirement: Site works without the booking service
When the booking service, its cache or the queue is stopped, failing or slow, event pages
SHALL render and bookings SHALL be stored, confirmed and notified as today; the count SHALL be
hidden on the page, and a booking that cannot be queued SHALL not be counted. The site SHALL
wait no longer than 2 seconds per request for the service or the queue. The failure SHALL be
recorded in telemetry (error status on the call, a warning log entry on the site), not shown
to the visitor.

#### Scenario: Service stopped
- **WHEN** the presenter stops the booking service and a visitor opens an event page and books
- **THEN** the page renders without count text, the booking shows the confirmation with its
  reference and the owner email arrives, the trace shows the failed call to the service, and
  the booking message waits in the queue

#### Scenario: Queue stopped
- **WHEN** the queue is stopped and a visitor books an event
- **THEN** the booking shows the confirmation with its reference and the owner email arrives,
  and the trace shows the failed attempt to queue the message

#### Scenario: Cache stopped
- **WHEN** the cache is stopped and a visitor opens an event page
- **THEN** the page renders without count text and the trace shows the service's failed
  cache call

#### Scenario: Site run alone
- **WHEN** an attendee runs the site alone on the file-based database
- **THEN** event pages render without count text and bookings work, without delay from
  calling a service or queue that is not there

### Requirement: No personal data to the booking service
The site SHALL send the booking service and the queue only the event's identity: no form
field values, visitor address, browser details or cookies. The booking service's, the
cache's and the queue's telemetry SHALL contain no personal data. The service's log entries
SHALL pass through the same personal-data masking as the site's (spec `log-privacy`).

#### Scenario: Booking content
- **WHEN** a booking is stored with name `Canary Person` and email `canary@test.invalid`
- **THEN** neither value appears in the booking service's requests, the queued message, spans,
  logs or cache entries

### Requirement: Booking service telemetry
The booking service SHALL emit traces, logs and metrics to the same destinations as the site:
the local dashboard always, and the production-stage platform once started. Each request and
each queued message SHALL carry the trace context from the site, so the site's work, the
queue hand-over, the service's handling and its cache operations appear in one trace. Spans
SHALL identify the event by its identity only.

#### Scenario: Event page trace
- **WHEN** a visitor opens an event page
- **THEN** the local dashboard shows one trace with the site's page request, the site's call to
  the booking service, the service's request handling and its cache read, from two resources

#### Scenario: Booking trace
- **WHEN** a valid event booking is stored
- **THEN** one trace contains the submission span and the site's queue publish on the site, and
  the service's message handling and cache update on the booking service

#### Scenario: Production-stage platform
- **WHEN** the production-stage platform is running and an event page is opened
- **THEN** the platform holds the trace with spans from both the site and the booking service,
  and the agent can query the service's logs and request metrics there
