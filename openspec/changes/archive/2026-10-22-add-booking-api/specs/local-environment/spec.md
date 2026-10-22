## MODIFIED Requirements

### Requirement: One-command start
The presenter SHALL start the site, its server database, the mail catcher, the booking service,
its cache, the queue and the telemetry collector with one command, and the dashboard SHALL show
each as a resource with its state. The production-stage telemetry platform SHALL show as a
resource that is not started, and its agent server as a resource waiting for it (ADR-0022,
ADR-0030, ADR-0031, ADR-0032).

#### Scenario: Start the environment
- **WHEN** the presenter runs the start command on a machine with the prerequisites
- **THEN** all resources except the production-stage platform and its agent server reach a
  running state, the site is reachable from the dashboard, the production-stage platform shows
  as not started and its agent server as waiting

#### Scenario: Fresh database on start
- **WHEN** the environment starts without existing database data
- **THEN** the site populates itself as defined by the agency-site capability

#### Scenario: Cache kept across restarts
- **WHEN** the environment is stopped and started again
- **THEN** booking counts are the same as before the restart, and they reset only when the
  presenter resets the cache along with the database

## ADDED Requirements

### Requirement: Traces across services
A request or message that the site passes to another instrumented service SHALL appear as one
trace with spans from each service, and the dashboard SHALL list each service as a separate
resource (spec `event-booking-count`, "Booking service telemetry").

#### Scenario: Two resources in one trace
- **WHEN** a visitor opens an event page with the environment running
- **THEN** the dashboard's trace view lists the site and the booking service for that trace

#### Scenario: Across the queue
- **WHEN** a visitor books an event with the environment running
- **THEN** the dashboard's trace view lists the site and the booking service for the booking's
  trace
