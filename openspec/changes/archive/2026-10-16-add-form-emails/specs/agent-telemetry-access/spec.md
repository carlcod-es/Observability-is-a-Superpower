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
- **WHEN** the agent tries to run a resource command (for example restarting the site)
- **THEN** an approval prompt is shown before anything runs

## ADDED Requirements

### Requirement: Presenter notes out of the agent's reach
The committed agent settings SHALL deny the agent reading the presenter notes
folder `docs/presenter/`.

#### Scenario: Agent tries to read presenter notes
- **WHEN** the agent tries to read a file under `docs/presenter/` with its file tools
- **THEN** the read is denied
