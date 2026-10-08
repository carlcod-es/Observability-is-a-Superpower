## Purpose

Starts the whole demo locally with one command and brings logs, traces and metrics from the
site and its dependencies into one observability dashboard, with controls to degrade the mail
path on demand.

## ADDED Requirements

### Requirement: One-command start
The presenter SHALL start the site, its server database, the mail catcher and the mail-path
fault hop with one command, and the dashboard SHALL show each as a resource with its state.

#### Scenario: Start the environment
- **WHEN** the presenter runs the start command on a machine with the prerequisites
- **THEN** all resources reach a running state and the site is reachable from the dashboard

#### Scenario: Fresh database on start
- **WHEN** the environment starts without existing database data
- **THEN** the site populates itself as defined by the agency-site capability

### Requirement: Telemetry in one place
Structured logs, traces and metrics from the site SHALL appear in the dashboard, correlated by
trace. A page request trace SHALL include the site's database calls when running on the server
database.

#### Scenario: Page request trace
- **WHEN** a visitor loads the Home page
- **THEN** the dashboard shows one trace for the request containing its database calls, and
  the request's log entries are linked to that trace

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
environment, how to run the site alone, and how to connect the coding agent.

#### Scenario: Attendee follows the README
- **WHEN** an attendee follows the README on a clean clone
- **THEN** they reach a running site and dashboard without other instructions
