## MODIFIED Requirements

### Requirement: Demo dashboard contents
The demo dashboard SHALL show, for the site only, with a default time window of 15 minutes
and automatic refresh:
- request rate per page type;
- server error (5xx) rate as a share of requests, showing 0 rather than no data when there are
  no errors;
- 95th-percentile request duration per page type, where a point can be followed to a trace
  of a matching request;
- the site's error-level log entries;
- form submissions per minute by form and outcome.
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

#### Scenario: Submissions on the dashboard
- **WHEN** the submission burst tool has run after go-live
- **THEN** the form submissions panel shows the submissions by form and outcome within a
  minute

## ADDED Requirements

### Requirement: Submission burst tool
The repo SHALL include a tool the presenter runs from a terminal that submits valid forms to
the site concurrently: a given number of submissions (default 50) with a given number in
flight at once (default 10), spread over the three forms, using made-up names and
`@example.com` addresses. It SHALL target the local site by default and accept another site
address, and SHALL print how many submissions succeeded and how many failed, with the
references it received.

#### Scenario: Burst against the local site
- **WHEN** the presenter runs the burst tool with defaults against the running environment
- **THEN** about 50 submissions are posted, ten at a time, and the tool prints the success
  and failure counts

#### Scenario: Made-up data only
- **WHEN** the burst tool has run
- **THEN** every submission it made uses an `@example.com` address and a made-up name
