## Purpose

Gives the coding agent read access to the local environment's logs, traces and metrics, so it
can identify failures and verify changes from evidence rather than guesswork.

Decisions: ADR-0011 (agent access to local telemetry; to be revised: one telemetry MCP server
covering logs, traces and metrics, no resource state or commands, PO 2026-10-13); in
`docs/adrs/`.

## ADDED Requirements

### Requirement: Agent reads local telemetry
With the local environment running, the coding agent SHALL be able to query traces and their
spans, query structured logs, including logs belonging to a given trace, and query metrics.
The agent SHALL NOT be able to read resource state or run resource commands; the presenter runs
those from the dashboard.

#### Scenario: Agent inspects a request
- **WHEN** the agent is asked about a recent page request
- **THEN** it can retrieve that request's trace, its spans with durations, and its log entries

#### Scenario: Agent checks a metric
- **WHEN** the agent is asked how long requests to a page take
- **THEN** it can retrieve that page's request-duration metric

### Requirement: Committed agent configuration
The repo SHALL contain the agent configuration needed to connect to the local telemetry, so an
attendee's agent connects without manual setup beyond starting the environment.

#### Scenario: Attendee agent connects
- **WHEN** an attendee starts the environment and opens the agent in the repo
- **THEN** the agent can query telemetry without editing configuration

### Requirement: Telemetry tools are read-only and pre-approved
The agent's telemetry tools SHALL be read-only, and the committed agent settings SHALL
pre-approve them.

#### Scenario: Read query
- **WHEN** the agent queries traces, logs or metrics
- **THEN** no approval prompt is shown

### Requirement: No database access for the agent
The committed agent configuration SHALL NOT give the agent any database connection or
database query tool.

#### Scenario: Agent asks for submissions
- **WHEN** the agent is asked to read stored submissions
- **THEN** it has no tool that can read the database
