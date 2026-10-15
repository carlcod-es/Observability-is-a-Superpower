Decisions: ADR-0022 (Aspire MCP for the local stage, Grafana MCP for the production stage;
supersedes part of ADR-0017); in `docs/adrs/`.

## MODIFIED Requirements

### Requirement: Agent reads local telemetry
With the local environment running, the coding agent SHALL be able to query traces and their
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
- **WHEN** the agent tries to run a resource command (for example a mail-path fault)
- **THEN** an approval prompt is shown before anything runs

### Requirement: Committed agent configuration
The repo SHALL contain the agent configuration needed to connect to both telemetry sources,
the local environment and the production-stage platform, so an attendee's agent connects
without manual setup beyond starting the environment (and the production-stage platform for
the second source).

#### Scenario: Attendee agent connects
- **WHEN** an attendee starts the environment and opens the agent in the repo
- **THEN** the agent can query the local telemetry without editing configuration

#### Scenario: Production-stage source after start
- **WHEN** the production-stage platform has been started and the agent is (re)connected
- **THEN** the agent can query that platform without editing configuration

### Requirement: Telemetry tools are read-only and pre-approved
The agent's telemetry read tools on both sources SHALL be pre-approved by the committed agent
settings. Tools that change state (resource commands) SHALL NOT be pre-approved. The
production-stage tools SHALL be read-only.

#### Scenario: Read query
- **WHEN** the agent queries traces, logs, resource state or metrics
- **THEN** no approval prompt is shown

#### Scenario: Production-stage tool surface
- **WHEN** the production-stage tool list is inspected
- **THEN** it contains only read tools for traces, logs and metrics

### Requirement: No database access for the agent
The committed agent configuration SHALL NOT give the agent any database connection or
database query tool, and the pre-approved local tools SHALL NOT reveal database credentials.

#### Scenario: Agent asks for submissions
- **WHEN** the agent is asked to read stored submissions
- **THEN** it has no tool that can read the database

#### Scenario: Resource state without credentials
- **WHEN** the agent lists resources and their details through a pre-approved tool
- **THEN** no database password or connection string appears in the result

## ADDED Requirements

### Requirement: Agent reads production-stage telemetry
After the presenter starts the production-stage platform, the agent SHALL be able to query
its traces, logs and metrics, and SHALL find there no traces or logs produced before the start.
Metric counters MAY include totals from before the start (cumulative; PO 2026-10-15).

#### Scenario: Agent checks a metric
- **WHEN** the agent is asked how long requests to a page take, after the platform started
  and the page was requested
- **THEN** it can retrieve that page's request-duration metric from the platform

#### Scenario: Nothing from before go-live
- **WHEN** the agent searches the platform for a request made before the platform started
- **THEN** it finds no trace or log for that request
