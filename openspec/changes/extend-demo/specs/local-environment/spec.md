## ADDED Requirements

### Requirement: README explains enabling the local-environment source
The README SHALL state that the talk runs the production-stage platform from the start and
gives the agent only the production-stage telemetry source, that the repo registers only that
source, and how to enable the local-environment source (ADR-0033).

#### Scenario: Attendee enables the local source
- **WHEN** an attendee follows the README section on enabling the local-environment source
- **THEN** the agent lists both sources as active and can query each once its environment is
  running

### Requirement: Smoke check on a running environment
The smoke check SHALL offer a mode for an environment that has already served traffic. In that
mode it SHALL skip only the checks that need a freshly started environment (first-load database
spans, telemetry from before the platform started) and run all others. Its options and output
SHALL NOT mention demo acts or planted problems.

#### Scenario: End of talk
- **WHEN** the presenter runs the smoke check in that mode after all fixes are applied, with the
  production-stage platform running and traffic already served
- **THEN** it reports the first-load checks as not applicable and every other check as passed

#### Scenario: Default unchanged
- **WHEN** the smoke check runs without the mode on a freshly started environment
- **THEN** it runs and reports every check as before
