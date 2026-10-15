Decisions: ADR-0022 (production-stage platform started on demand, no replay); ADR-0017
(collector, Grafana LGTM, Grafana MCP); in `docs/adrs/`.

## MODIFIED Requirements

### Requirement: One-command start
The presenter SHALL start the site, its server database, the mail catcher, the mail-path
fault hop and the telemetry collector with one command, and the dashboard SHALL show each as
a resource with its state. The production-stage telemetry platform SHALL show as a resource
that is not started, and its agent server as a resource waiting for it (ADR-0022).

#### Scenario: Start the environment
- **WHEN** the presenter runs the start command on a machine with the prerequisites
- **THEN** all resources except the production-stage platform and its agent server reach a
  running state, the site is reachable from the dashboard, the production-stage platform shows
  as not started and its agent server as waiting

#### Scenario: Fresh database on start
- **WHEN** the environment starts without existing database data
- **THEN** the site populates itself as defined by the agency-site capability

### Requirement: Attendee README
The repo SHALL include a README that lists prerequisites and explains how to run the full
environment, how to run the site alone, how to start the production-stage platform, and how to
connect the coding agent to each telemetry source.

#### Scenario: Attendee follows the README
- **WHEN** an attendee follows the README on a clean clone
- **THEN** they reach a running site and dashboard without other instructions, and can start
  the production-stage platform and query it from the agent

## ADDED Requirements

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
