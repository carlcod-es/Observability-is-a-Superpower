## Purpose

Gives the coding agent read access to the local environment's telemetry and resource state, so
it can identify failures and verify changes from evidence rather than guesswork.

## ADDED Requirements

### Requirement: Agent reads local telemetry
With the local environment running, the coding agent SHALL be able to list resources and their
state, query traces and their spans, and query structured logs, including logs belonging to a
given trace.

#### Scenario: Agent inspects a request
- **WHEN** the agent is asked about a recent page request
- **THEN** it can retrieve that request's trace, its spans with durations, and its log entries

#### Scenario: Agent lists resources
- **WHEN** the agent asks for the environment's resources
- **THEN** it receives each resource with its state

### Requirement: Committed agent configuration
The repo SHALL contain the agent configuration needed to connect to the local telemetry, so an
attendee's agent connects without manual setup beyond starting the environment.

#### Scenario: Attendee agent connects
- **WHEN** an attendee starts the environment and opens the agent in the repo
- **THEN** the agent can query telemetry without editing configuration

### Requirement: Telemetry tools are pre-approved, others are not
Read-only telemetry queries SHALL be pre-approved in the committed agent settings. Any action
that changes resource state SHALL require the user's confirmation each time.

#### Scenario: Read query
- **WHEN** the agent queries traces or logs
- **THEN** no approval prompt is shown

#### Scenario: State-changing action
- **WHEN** the agent attempts to run a resource command
- **THEN** the user is asked to approve it

### Requirement: No database access for the agent
The committed agent configuration SHALL NOT give the agent any database connection or
database query tool.

#### Scenario: Agent asks for submissions
- **WHEN** the agent is asked to read stored submissions
- **THEN** it has no tool that can read the database
