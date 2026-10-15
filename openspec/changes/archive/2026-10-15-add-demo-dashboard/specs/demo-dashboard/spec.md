## Purpose

Gives the production-stage platform ready-made dashboards for the site, present from the
moment the platform starts, and a traffic source that keeps their panels filled during the talk.

Decisions: ADR-0023 (dashboards: contents, kept in the repo, loaded on platform start);
ADR-0024 (page-type label); ADR-0012 (file-based tools under `tests/`); ADR-0022 (platform
started on demand); in `docs/adrs/`.

## ADDED Requirements

### Requirement: Dashboards present on platform start
When the production-stage platform starts, it SHALL list the site's demo dashboard and the
generic ASP.NET Core dashboard (community dashboard 19924) with no import or other manual step,
on every start, including after an environment restart. The dashboard definitions in the repo
SHALL be their only source; nothing is edited in the platform's UI to make them appear.

#### Scenario: First go-live
- **WHEN** the presenter starts the production-stage platform on a freshly started environment
- **THEN** both dashboards are listed in the platform without any import

#### Scenario: After a restart
- **WHEN** the environment is stopped, started again and the production-stage platform is
  started
- **THEN** both dashboards are listed again and render with data once the site has traffic

### Requirement: Demo dashboard contents
The demo dashboard SHALL show, for the site only, with a default time window of 15 minutes
and automatic refresh:
- request rate per page type;
- server error (5xx) rate as a share of requests, showing 0 rather than no data when there are
  no errors;
- 95th-percentile request duration per page type, where a point can be followed to a trace
  of a matching request;
- the site's error-level log entries.
Panels SHALL react to a change in traffic within about a minute. They SHALL NOT show personal
data from forms.

#### Scenario: Normal traffic
- **WHEN** the traffic source has run for two minutes after go-live
- **THEN** the request-rate and duration panels show one series per visited page type, the
  error-rate panel shows 0 %, and the error-log panel shows no entries or only real errors

#### Scenario: From a slow point to its trace
- **WHEN** the presenter selects a point on the duration panel that offers a trace link
- **THEN** the platform opens a trace of a request of that page type

#### Scenario: Generic dashboard
- **WHEN** the presenter opens the ASP.NET Core dashboard and picks the site
- **THEN** its request-duration, error-rate and request-count panels show the site's data

### Requirement: Demo traffic source
The repo SHALL include a traffic source that the presenter starts and stops from a terminal.
While running it SHALL request the site's published pages (Home, top-level pages, event and
service pages) at a steady rate of about one request per second, SHALL NOT submit any form,
and SHALL print one line per minute summarising requests and failures. It SHALL target the
local site by default and accept another site address.

#### Scenario: Steady traffic
- **WHEN** the presenter starts the traffic source with the environment running
- **THEN** requests reach every page type within two minutes and continue until it is stopped

#### Scenario: No form data
- **WHEN** the traffic source has run for five minutes
- **THEN** no new submission is stored and no form post appears in the telemetry

### Requirement: Dashboards documented
The README SHALL say where the dashboards are, that they appear when the production-stage
platform starts, and how to start and stop the traffic source.

#### Scenario: Attendee finds the dashboard
- **WHEN** an attendee follows the README after starting the production-stage platform
- **THEN** they open the demo dashboard and, with the traffic source running, see data
