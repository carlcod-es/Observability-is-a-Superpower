Decisions: ADR-0024 (page-type label on request telemetry); ADR-0008 (excluded static
paths); in `docs/adrs/`.

## ADDED Requirements

### Requirement: Requests labelled by page type
The site's request-duration metric and its request traces SHALL carry the page type of the
rendered page: one value per content type (Home, content page, Contact, Events list, event,
Services list, service). Requests that render no content page (not found, static files,
back office, health) SHALL carry one fixed value. The label SHALL come from a small fixed set
and SHALL NOT contain the URL path, query string or any personal data. Metric and trace SHALL
carry the same value for the same request (ADR-0024).

#### Scenario: Home on its own
- **WHEN** the Home page and an event page are requested
- **THEN** the request-duration metric can be filtered to Home alone, and the Home request's
  trace carries the same page-type value

#### Scenario: Form post
- **WHEN** a form on a service page is submitted
- **THEN** the post's request telemetry carries the service page type

#### Scenario: Not found
- **WHEN** a URL with no content is requested
- **THEN** its request telemetry carries the fixed no-page value

### Requirement: Static assets leave no request telemetry
Requests for the site's static files (styles, scripts, fonts, media library files and the
site's own image assets) SHALL leave no traces or logs (ADR-0008).

#### Scenario: Logo request
- **WHEN** a page is loaded and the browser fetches the header logo
- **THEN** the local dashboard shows a trace for the page request and none for the logo
