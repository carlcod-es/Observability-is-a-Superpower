## Why

After go-live the talk shows "production" in Grafana, but Grafana starts empty: no dashboard
until someone imports one by hand, and dashboards vanish on every environment restart
(FR-0001). Its panels also need steady traffic, or they empty within minutes
(`spike-grafana-dashboard` `findings.md` section 6). And the site's request metrics cannot
separate one page from another, so neither a panel nor the agent can answer "how slow is the
Home page" from metrics (FR-0003). The talk is on 16 Oct 2026, so this has to be ready now.

## What Changes

- A demo dashboard for the site in the production-stage platform: request rate, server error
  rate and request duration (with links from a duration point to its trace) per page type, and
  the site's error logs. Its layout follows `spike-grafana-dashboard` `findings.md` section 6.
  Form panels come later with `add-form-emails`.
- A generic ASP.NET Core dashboard (community dashboard 19924) as a second view, unchanged
  apart from its data-source reference (PO 2026-10-14).
- Both dashboards are present as soon as the production-stage platform starts, after every
  restart, with no import step for the presenter or attendees (FR-0001).
- The site's request metrics and request traces carry the page type of the rendered page
  (Home, Events list, event page and so on), from a small fixed set of values; requests with
  no page carry one fixed value. Never the URL or personal data (FR-0003).
- A demo traffic source that browses the site's pages at a steady rate and never submits a
  form, started and stopped by the presenter.
- The agent's metric check is tightened back to a single page's request duration (FR-0003
  notes).
- Images under the site's static assets folder leave no traces or logs, like the other static
  files (found 2026-10-15: the header logo added that folder).

Out of scope: form panels (`add-form-emails`), the tunnel (`add-production-environment`),
dashboards in the local Aspire dashboard, agent access to dashboards (the agent keeps its
read-only telemetry tools, ADR-0022).

## Capabilities

### New Capabilities
- `demo-dashboard`: the production-stage dashboards (contents, present on start with no
  import) and the demo traffic source.

### Modified Capabilities
`local-environment` and `agent-telemetry-access` in `openspec/specs/` (synced 2026-10-15).
- `local-environment`: page requests are labelled with their page type in metrics and traces;
  static asset requests leave no traces or logs.
- `agent-telemetry-access`: "Agent reads production-stage telemetry" scenario "Agent checks a
  metric" reads a single page's request-duration metric.

## Impact

- Environment definition (production-stage platform gains the dashboards), site request
  telemetry (page-type label), excluded static paths, a new traffic tool under `tests/`, the
  smoke check (dashboard present, label present), README and project-setup doc.
- New ADRs for the dashboard (contents, JSON in the repo, provisioning) and the page-type
  label.
- Closes FR-0001 and FR-0003.
