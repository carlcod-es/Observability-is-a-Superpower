Decisions: ADR-0024 (page-type label); ADR-0022 (production-stage platform on demand); in
`docs/adrs/`.

## MODIFIED Requirements

### Requirement: Agent reads production-stage telemetry
After the presenter starts the production-stage platform, the agent SHALL be able to query
its traces, logs and metrics, and SHALL find there no traces or logs produced before the start.
Metric counters MAY include totals from before the start (cumulative; PO 2026-10-15).

#### Scenario: Agent checks a metric
- **WHEN** the agent is asked how long requests to a page take, after the platform started
  and the page was requested
- **THEN** it can retrieve that page type's request durations from the site's request-duration
  metric, filtered by page type (FR-0003, ADR-0024)

#### Scenario: Nothing from before go-live
- **WHEN** the agent searches the platform for a request made before the platform started
- **THEN** it finds no trace or log for that request
