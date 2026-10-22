# local-environment Specification

## Purpose

Starts the whole demo locally with one command and brings logs, traces and metrics from the
site and its dependencies into one observability dashboard.

Decisions: ADR-0001 (platform and versions), ADR-0007 (logs), ADR-0008 (traces, metrics,
exporters), ADR-0009 (local database), ADR-0010 (mail path; fault injection removed by
ADR-0025), ADR-0017 (collector, Grafana LGTM and Grafana MCP); in `docs/adrs/`.

Decisions: ADR-0022 (production-stage platform started on demand, no replay); ADR-0017
(collector, Grafana LGTM, Grafana MCP); in `docs/adrs/`.

Decisions: ADR-0024 (page-type label on request telemetry); ADR-0008 (excluded static paths); in
`docs/adrs/`.

Decisions: ADR-0025 (mail fault injection removed); in `docs/adrs/`.

Decisions: ADR-0030 (booking service), ADR-0031 (cache), ADR-0032 (queue); in `docs/adrs/`.

## Requirements

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

### Requirement: Telemetry in one place
Structured logs, traces and metrics from the site SHALL appear in the dashboard, correlated by
trace. A page request trace SHALL include the database calls the request makes when running on
the server database.

#### Scenario: Page request trace
- **WHEN** a visitor loads the Home page for the first time after the site starts
- **THEN** the dashboard shows one trace for the request containing its database calls, and
  the request's log entries are linked to that trace (later loads are served from Umbraco's
  cache and make no database calls; PO 2026-10-14)

#### Scenario: Site logs are structured
- **WHEN** the site writes a log entry with named values
- **THEN** the dashboard shows the entry with those values as separate, filterable fields

### Requirement: Mail catcher
All outgoing mail SHALL be delivered to a local mail catcher with a web inbox reachable from
the dashboard; no mail SHALL leave the machine.

#### Scenario: Mail stays local
- **WHEN** the site sends an email in the local environment
- **THEN** it appears in the mail catcher's inbox and is not delivered externally

### Requirement: File-based database mode
The site SHALL run on its own, without the environment host or a container runtime, using a
file-based database, so attendees can try it with only the SDK installed.

#### Scenario: Run the site alone
- **WHEN** an attendee runs only the site project on a clean clone
- **THEN** the site installs itself on a file-based database and serves all pages and forms

### Requirement: Cross-platform
The environment and site SHALL start on Windows, macOS and Linux.

#### Scenario: Any OS
- **WHEN** the start command runs on any of the three operating systems with prerequisites
- **THEN** the environment starts without OS-specific changes

### Requirement: Attendee README
The repo SHALL include a README that lists prerequisites and explains how to run the full
environment, how to run the site alone, how to start the production-stage platform, and how to
connect the coding agent to each telemetry source.

#### Scenario: Attendee follows the README
- **WHEN** an attendee follows the README on a clean clone
- **THEN** they reach a running site and dashboard without other instructions, and can start
  the production-stage platform and query it from the agent

### Requirement: Production-stage platform on demand
The presenter SHALL start the production-stage telemetry platform and its agent server with
one action in the dashboard, without restarting the environment or the site. From then on the
platform SHALL receive the site's traces, logs and metrics; it SHALL NOT receive traces or
logs produced before it started. Metric counters are cumulative and MAY include totals from
before the start (PO 2026-10-15). The local dashboard SHALL keep receiving all telemetry throughout.

#### Scenario: Go live
- **WHEN** the presenter starts the production-stage platform from the dashboard while the
  site is running
- **THEN** within a minute the platform and its agent server are running, the site was not
  restarted, and new page requests appear in the platform

#### Scenario: No earlier telemetry
- **WHEN** a form is submitted before the platform starts and the platform is started
  afterwards
- **THEN** the platform contains no trace or log from that submission, while the local
  dashboard shows it

#### Scenario: Site unaffected while the platform is stopped
- **WHEN** the site serves pages and forms before the platform has been started
- **THEN** pages and forms work, and the local dashboard shows their telemetry

### Requirement: Requests labelled by page type
The site's request-duration metric and its request traces SHALL carry the page type of the
rendered page: one value per content type (Home, content page, Contact, Events list, event,
Services list, service). Requests that render no content page (not found, static files,
back office, health) SHALL carry one fixed value. The label SHALL come from a small fixed set
and SHALL NOT contain the URL path, query string or any personal data. Metric and trace SHALL
carry the same value for the same request (ADR-0024).

#### Scenario: Home on its own
- **WHEN** the Home page and an event page are requested
- **THEN** the request-duration metric can be filtered to Home alone, and the Home request's
  trace carries the same page-type value

#### Scenario: Form post
- **WHEN** a form on a service page is submitted
- **THEN** the post's request telemetry carries the service page type

#### Scenario: Not found
- **WHEN** a URL with no content is requested
- **THEN** its request telemetry carries the fixed no-page value

### Requirement: Static assets leave no request telemetry
Requests for the site's static files (styles, scripts, fonts, media library files and the
site's own image assets) SHALL leave no traces or logs (ADR-0008).

#### Scenario: Logo request
- **WHEN** a page is loaded and the browser fetches the header logo
- **THEN** the local dashboard shows a trace for the page request and none for the logo

### Requirement: Documented database login
The README SHALL say how to connect a SQL client to the local SQL Server: user `sa`, where to
find the generated password and the database port.

#### Scenario: Connect with a SQL client
- **WHEN** someone has started the environment from a fresh clone
- **THEN** they can connect a SQL client by following the README, with no other source

### Requirement: Demo run-through
The README SHALL give the demo as a short ordered run-through that links the detailed
sections, and SHALL NOT describe planted problems or their fixes.

#### Scenario: Attendee follows the run-through
- **WHEN** an attendee follows the run-through on a fresh clone
- **THEN** they reach each talk step (site, form post, local telemetry, Grafana go-live, demo
  traffic, agent questions) without other instructions

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
