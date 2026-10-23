## ADDED Requirements

### Requirement: Booking processing outcome in telemetry
The booking service SHALL record each booking it adds to a count in a metric of counted
bookings, available on the production-stage platform (ADR-0034). When it fails to process a
booking message, it SHALL mark the message handling on the booking's trace as an error with the
exception, and SHALL write an error-level log entry. The metric, the error and the log entry
SHALL identify the event by its identity only, or not at all.

#### Scenario: Booking counted
- **WHEN** a valid event booking is stored and the booking service adds it to the count
- **THEN** the counted-bookings metric rises by one within a minute on the production-stage
  platform

#### Scenario: Processing fails
- **WHEN** the booking service fails to process a booking message
- **THEN** the booking's trace shows the booking service's message handling as an error with
  the exception, the booking service logs an error-level entry, the counted-bookings metric
  does not rise, and the visitor still sees the confirmation with the reference

### Requirement: Planted booking processing failure
The code SHALL contain one demo toggle, marked `// Demo Code : Act 3` with an explanation, its
fix and its initial demo state, that makes the booking service fail to process every booking
message. `main` SHALL be committed with the toggle in its fixed state. Applying the fix and
restarting the booking service SHALL restore counting without a database, cache or queue reset.
What the toggle does SHALL be described only in the code comment and the presenter runthrough.

#### Scenario: Toggle planted
- **WHEN** the presenter sets the toggle to its initial demo state, restarts the booking service
  and books an event
- **THEN** the booking shows the confirmation, the owner email arrives, the event's count does
  not change, and the failure is visible as described in "Booking processing outcome in
  telemetry"

#### Scenario: Toggle fixed
- **WHEN** the presenter applies the fix, restarts the booking service and books an event
- **THEN** the event's count rises by one within 5 seconds; bookings made while the toggle was
  planted stay uncounted

## MODIFIED Requirements

### Requirement: Booking service telemetry
The booking service SHALL emit traces, logs and metrics to the same destinations as the site:
the local dashboard and the production-stage platform (ADR-0033). Each request and
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
