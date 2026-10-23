## MODIFIED Requirements

### Requirement: Dashboards present on platform start
When the production-stage platform starts, which it does with the environment (ADR-0033), it
SHALL list the site's demo dashboard and the generic ASP.NET Core dashboard (community dashboard
19924) with no import or other manual step, on every start, including after an environment
restart. The dashboard definitions in the repo SHALL be their only source; nothing is edited in
the platform's UI to make them appear.

#### Scenario: First go-live
- **WHEN** the environment has been freshly started
- **THEN** both dashboards are listed in the platform without any import

#### Scenario: After a restart
- **WHEN** the environment is stopped and started again
- **THEN** both dashboards are listed again and render with data once the site has traffic

### Requirement: Demo dashboard contents
The demo dashboard SHALL show, for the site only unless stated, with a default time window of
15 minutes and automatic refresh:
- request rate per page type;
- server error (5xx) rate as a share of requests, showing 0 rather than no data when there are
  no errors;
- 95th-percentile request duration per page type, where a point can be followed to a trace
  of a matching request;
- the site's error-level log entries;
- form submissions per minute by form and outcome;
- event bookings per minute, stored by the site against counted by the booking service, both
  showing 0 rather than no data when there are none (ADR-0034).
Form submissions and bookings share the last row, form submissions on the left, bookings on the
right. Panels SHALL react to a change in traffic within about a minute. They SHALL NOT show
personal data from forms.

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

#### Scenario: Submissions on the dashboard
- **WHEN** the submission burst tool has run after go-live
- **THEN** the form submissions panel shows the submissions by form and outcome within a
  minute

#### Scenario: Bookings counted
- **WHEN** event bookings are stored while the booking service processes them normally
- **THEN** within about a minute the bookings panel shows the stored and counted lines at the
  same values

#### Scenario: Bookings lost
- **WHEN** event bookings are stored while the booking service fails to process them
- **THEN** the bookings panel shows the stored line above zero and the counted line at 0 for
  those minutes, while the error-rate panel stays at 0 % and the form submissions panel shows
  the bookings as stored
