## MODIFIED Requirements

### Requirement: One-command start
The presenter SHALL start the site, its server database, the mail catcher and the telemetry
collector with one command, and the dashboard SHALL show each as a resource with its state.
The production-stage telemetry platform SHALL show as a resource that is not started, and its
agent server as a resource waiting for it (ADR-0022).

#### Scenario: Start the environment
- **WHEN** the presenter runs the start command on a machine with the prerequisites
- **THEN** all resources except the production-stage platform and its agent server reach a
  running state, the site is reachable from the dashboard, the production-stage platform shows
  as not started and its agent server as waiting

#### Scenario: Fresh database on start
- **WHEN** the environment starts without existing database data
- **THEN** the site populates itself as defined by the agency-site capability

## REMOVED Requirements

### Requirement: Mail-path fault controls
**Reason**: The acts no longer demonstrate injected mail faults (PO 2026-10-15); the
production-stage problem comes from load. ADR-0025.
**Migration**: Mail goes from the site straight to the mail catcher. No Slow / Break / Heal
commands; the presenter's notes no longer use them.
