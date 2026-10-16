## ADDED Requirements

### Requirement: Optimised stage run
The presenter SHALL start the environment for the talk with one documented command that runs
an optimised build of the site and does not use the CMS's debug mode (ADR-0029). The site SHALL
NOT compile views at runtime on a page's first request. Behaviour SHALL match the development
run: the same pages, forms, emails, telemetry and agent access. Each change in this requirement
SHALL be backed by a telemetry measurement before and after it on the presenter's machine.

#### Scenario: Optimised start
- **WHEN** the presenter runs the documented stage start command
- **THEN** all resources reach the states defined by "One-command start" and the site runs as an
  optimised build with CMS debug mode off

#### Scenario: First page load without view compilation
- **WHEN** a visitor loads a page of a given type for the first time after the site starts on
  the stage run
- **THEN** the request completes without compiling a view, and its trace duration is lower than
  the recorded baseline for that page type

#### Scenario: Behaviour unchanged
- **WHEN** the smoke checks run against the stage run and the presenter loads the Home page for
  the first time after start
- **THEN** the smoke checks pass, and the Home page trace contains its database calls as defined
  by "Telemetry in one place"

#### Scenario: Site run alone keeps development settings
- **WHEN** an attendee runs only the site project on a clean clone
- **THEN** the site behaves as defined by "File-based database mode", with its development
  settings unchanged
