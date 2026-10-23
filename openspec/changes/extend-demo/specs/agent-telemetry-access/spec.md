## ADDED Requirements

### Requirement: Agent works from the production-stage platform alone
With the production-stage platform running and the local-environment agent server disabled for
the folder, the agent SHALL be able to diagnose each demo act and verify its fix from the
platform's traces, logs and metrics alone (ADR-0033). The agent SHALL NOT restart services
itself; it SHALL ask the presenter to restart the named service. The stage agent instructions
SHALL say so and SHALL name only the production-stage platform as the agent's telemetry source.

#### Scenario: Diagnose without the local server
- **WHEN** the local-environment agent server is disabled and the agent is asked about a
  failing form post or a booking that was not counted
- **THEN** it finds the relevant traces and log entries on the production-stage platform
  without attempting to reach the local-environment server

#### Scenario: Fix needs a restart
- **WHEN** the agent has changed code that needs a service restart to take effect
- **THEN** it asks the presenter to restart that service and waits, rather than running a
  command
