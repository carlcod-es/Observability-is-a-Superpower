## Why

The whole talk runs on the presenter's laptop: an Apple M1 Pro with 16 GB of RAM and 8 GB given
to the container runtime. The same laptop serves the site through the tunnel. Slow starts and
slow first page loads cost stage time and look bad on screen. The PO wants every gain available
before the talk (16 Oct 2026), as long as the demo behaves the same. Each gain must be shown with
before/after telemetry.

## What Changes

- Record a baseline on the presenter's laptop with the demo's own telemetry, then re-measure
  after the changes: environment start time, the first load of each page type, warm page and
  form request durations, and site memory.
- Run the stage demo with an optimised build of the site and AppHost instead of the
  development build.
- Compile the site's views ahead of time, so no page compiles a view on its first request.
- Turn off the CMS's debug mode for the stage run. Attendees running the site on its own keep
  their current development settings.
- Add a presenter machine checklist (container runtime resources, CPU emulation for the
  database image, background apps, power) and the optimised start command.
- Unchanged (guard rails): all pages, forms, emails and telemetry stay the same, including the
  database calls in the first Home page trace (spec `local-environment`, "Telemetry in one
  place") and full trace capture. The agent's Aspire MCP and Grafana MCP access stays the same.
  The site still runs on its own with the file-based database.

Out of scope: changing the database engine or container images, sampling or reducing telemetry,
pre-loading the CMS cache at start (it would remove the database calls from the first Home
trace), the CMS's production runtime mode (see design), and any code changes for speed.

PO decisions (2026-10-15): no warm-up before the talk, because the presenter needs the first
Home trace with its database calls. Continued (warm) speed matters more than start-up speed:
items are judged first on warm page and form durations and on memory.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `local-environment`: adds a stage-run performance requirement (optimised start, no view
  compilation on first page load, behaviour unchanged) with a telemetry-measured acceptance
  check.

## Impact

- Site project build settings (view compilation), the AppHost's settings for the site (CMS
  debug off), the README start instructions, and the presenter runthrough (PO).
- No new dependencies. No change to specs other than `local-environment`.
- New ADR for the optimised stage run.
- Presenter machine settings (container runtime), outside the repo.
