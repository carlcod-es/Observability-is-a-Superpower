# Spec Delta

## MODIFIED Requirements

### Requirement: Attendee README
The repo SHALL include a README that lists prerequisites and explains how to run the full
environment, how to run the site alone, how to start the production-stage platform, and how to
connect the coding agent to each telemetry source. It SHALL note that the orchestration
platform's agent skills are not installed in the repo and why (ADR-0036).

#### Scenario: Attendee follows the README
- **WHEN** an attendee follows the README on a clean clone
- **THEN** they reach a running site and dashboard without other instructions, and can start
  the production-stage platform and query it from the agent

#### Scenario: Attendee asks about agent skills
- **WHEN** an attendee reads the README's agent section
- **THEN** it says the agent skills exist, that the repo leaves them out because they let the agent
  restart services without asking, and that attendees may install them in their own projects

## ADDED Requirements

### Requirement: Clean build
Building the solution SHALL report no deprecation warnings from the orchestration platform and no
known high-severity vulnerability warnings for any package (ADR-0036).

#### Scenario: Build on a clean clone
- **WHEN** a developer builds the solution on a clean clone with the prerequisites
- **THEN** the build succeeds with no orchestration deprecation warnings and no high-severity
  vulnerability warnings
