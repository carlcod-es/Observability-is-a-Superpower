## ADDED Requirements

### Requirement: README explains enabling the local-environment source
The README SHALL state that the talk runs the production-stage platform from the start and
gives the agent only the production-stage telemetry source, that the repo registers only that
source, and how to enable the local-environment source (ADR-0033).

#### Scenario: Attendee enables the local source
- **WHEN** an attendee follows the README section on enabling the local-environment source
- **THEN** the agent lists both sources as active and can query each once its environment is
  running

### Requirement: Smoke check on a running environment
The smoke check SHALL offer a mode for an environment that has already served traffic. In that
mode it SHALL skip only the checks that need a freshly started environment (first-load database
spans) and run all others. Its options and output SHALL NOT mention demo acts or planted
problems.

#### Scenario: End of talk
- **WHEN** the presenter runs the smoke check in that mode after all fixes are applied, with the
  production-stage platform running and traffic already served
- **THEN** it reports the first-load checks as not applicable and every other check as passed

#### Scenario: Default unchanged
- **WHEN** the smoke check runs without the mode on a freshly started environment
- **THEN** it runs and reports every check

## MODIFIED Requirements

### Requirement: One-command start
The presenter SHALL start the site, its server database, the mail catcher, the booking service,
its cache, the queue, the telemetry collector, the production-stage telemetry platform and its
agent server with one command, and the dashboard SHALL show each as a resource with its state.
The collector SHALL start only once the production-stage platform is healthy, and the site and
the booking service only once the collector runs, so their telemetry from start-up reaches both
the local dashboard and the platform (ADR-0033, ADR-0030, ADR-0031, ADR-0032).

#### Scenario: Start the environment
- **WHEN** the presenter runs the start command on a machine with the prerequisites
- **THEN** all resources reach a running state, including the production-stage platform and its
  agent server, and the site is reachable from the dashboard

#### Scenario: Fresh database on start
- **WHEN** the environment starts without existing database data
- **THEN** the site populates itself as defined by the agency-site capability

#### Scenario: Cache kept across restarts
- **WHEN** the environment is stopped and started again
- **THEN** booking counts are the same as before the restart, and they reset only when the
  presenter resets the cache along with the database

#### Scenario: Start-up telemetry on the platform
- **WHEN** the environment has started and the site has served its first page
- **THEN** the production-stage platform holds the site's start-up logs and the first page's
  trace

### Requirement: Attendee README
The repo SHALL include a README that lists prerequisites and explains how to run the full
environment, how to run the site alone, how to open the production-stage platform, and how to
connect the coding agent to each telemetry source.

#### Scenario: Attendee follows the README
- **WHEN** an attendee follows the README on a clean clone
- **THEN** they reach a running site, dashboard and production-stage platform without other
  instructions, and can query the platform from the agent

## REMOVED Requirements

### Requirement: Production-stage platform on demand
**Reason**: The production-stage platform starts with the environment (PO 2026-10-23, ADR-0033);
there is no separate start and no "before the platform started" period.
**Migration**: "One-command start" covers starting the platform; nothing to start by hand.
