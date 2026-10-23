## ADDED Requirements

### Requirement: Agent works from the production-stage platform alone
With the production-stage platform running and only the production-stage agent server
registered, the agent SHALL be able to diagnose each demo act and verify its fix from the
platform's traces, logs and metrics alone (ADR-0033). The agent SHALL NOT restart services
itself; it SHALL ask the presenter to restart the named service. The stage agent instructions
SHALL say so and SHALL name only the production-stage platform as the agent's telemetry source.

#### Scenario: Diagnose without the local server
- **WHEN** only the production-stage agent server is registered and the agent is asked about a
  failing form post or a booking that was not counted
- **THEN** it finds the relevant traces and log entries on the production-stage platform
  without attempting to reach the local-environment server

#### Scenario: Fix needs a restart
- **WHEN** the agent has changed code that needs a service restart to take effect
- **THEN** it asks the presenter to restart that service and waits, rather than running a
  command

## MODIFIED Requirements

### Requirement: Agent reads local telemetry
Once the local-environment agent server has been enabled (opt-in, README; ADR-0033), with the
local environment running, the coding agent SHALL be able to query traces and their
spans, query structured logs, including logs belonging to a given trace, and read resource
state and console output, all from the local environment's own telemetry (ADR-0022). Metrics
at the local stage are shown by the presenter in the dashboard; the agent is not required to
read them. The agent SHALL NOT run resource commands without the presenter approving each
call.

#### Scenario: Agent inspects a request
- **WHEN** the agent is asked about a recent page request
- **THEN** it can retrieve that request's trace, its spans with durations, and its log entries

#### Scenario: Agent checks resource state
- **WHEN** the agent is asked whether the site and its dependencies are running
- **THEN** it can list the resources with their state and read the site's console output

#### Scenario: Agent tries a resource command
- **WHEN** the agent tries to run a resource command (for example restarting the site)
- **THEN** an approval prompt is shown before anything runs

### Requirement: Committed agent configuration
The repo SHALL register only the production-stage telemetry source, so an attendee's agent
connects to it without manual setup beyond starting the environment and the production-stage
platform (ADR-0033). The committed agent settings SHALL pre-approve the local-environment
source's read tools, and the README SHALL give the single step that enables that source.

#### Scenario: Attendee agent connects
- **WHEN** an attendee starts the environment and the production-stage platform and opens the
  agent in the repo
- **THEN** the agent can query the production-stage telemetry without editing configuration

#### Scenario: Production-stage source after start
- **WHEN** the production-stage platform has been started and the agent is (re)connected
- **THEN** the agent can query that platform without editing configuration

#### Scenario: Local source not registered
- **WHEN** an attendee opens the agent in a fresh clone
- **THEN** the agent lists only the production-stage source

#### Scenario: Attendee enables the local source
- **WHEN** an attendee runs the README step to enable the local-environment source with the
  environment running
- **THEN** the agent can query the local telemetry and its read tools run without an approval
  prompt
