## ADDED Requirements

### Requirement: README matches the talk's agent setup
The README SHALL state that the talk runs the production-stage platform from the start and
gives the agent only the production-stage telemetry source, that the repo registers both
sources, and how to disable the local-environment source for the repo folder to match the talk
(ADR-0033).

#### Scenario: Attendee matches the talk
- **WHEN** an attendee follows the README section on the talk's agent setup
- **THEN** the agent lists only the production-stage source as active and can query it once the
  platform is running

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
