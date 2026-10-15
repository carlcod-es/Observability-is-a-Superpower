# local-environment Specification

## Purpose

Starts the whole demo locally with one command and brings logs, traces and metrics from the
site and its dependencies into one observability dashboard, with controls to degrade the mail
path on demand.

Decisions: ADR-0001 (platform and versions), ADR-0007 (logs), ADR-0008 (traces, metrics,
exporters), ADR-0009 (local database), ADR-0010 (mail path and fault injection), ADR-0017
(collector, Grafana LGTM and Grafana MCP); in `docs/adrs/`.

Decisions: ADR-0022 (production-stage platform started on demand, no replay); ADR-0017
(collector, Grafana LGTM, Grafana MCP); in `docs/adrs/`.

## Requirements

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

### Requirement: Mail-path fault controls
Mail from the site SHALL pass through a fault hop. The dashboard SHALL offer three commands
on that hop: Slow (adds latency to every mail exchange), Break (mail connections fail) and
Heal (removes all faults). The hop SHALL start healed.

#### Scenario: Slow mail
- **WHEN** the presenter runs Slow and the site sends an email
- **THEN** sending takes measurably longer and the email still arrives

#### Scenario: Break and heal
- **WHEN** the presenter runs Break, then Heal
- **THEN** mail sent while broken fails, and mail sent after Heal arrives

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
