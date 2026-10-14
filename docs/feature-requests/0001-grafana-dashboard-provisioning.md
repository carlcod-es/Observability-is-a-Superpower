# FR-0001: Grafana dashboards present after start, no manual import

## Status
Open (2026-10-14). Source: `spike-grafana-dashboard` task 4.4 (stretch, not attempted).

## Request
The demo's Grafana dashboards exist as soon as the local environment is up. No import step
before or during the talk, and none for attendees running the repo.

## Why
Dashboards live in the `lgtm` container and are lost on every AppHost restart
(`spike-grafana-dashboard` `findings.md`). Re-importing by hand on stage costs time and is a
step to forget; attendees would need the same manual step.

## Acceptance criteria
- After a plain start of the AppHost, Grafana lists the demo dashboard(s) without any import.
- After restarting the AppHost, the dashboards are still present and render with data.
- Works on Windows (presenting machine) and macOS/Linux.
- The dashboard JSON in the repo is the single source; nothing is edited in the Grafana UI to
  make it appear.

## Notes
- Candidate route from the spike: mount the dashboard JSON into the `grafana/otel-lgtm`
  provisioning directory (`/otel-lgtm/grafana/conf/provisioning/dashboards/`). Unverified:
  whether the image ships a dashboard provider config or one must be mounted too.
- Startup load only; live reload of edited JSON is not required (file-change events may not
  reach the container on Windows).
- Planned home: the demo dashboard ADR and `/opsx:update add-demo-foundation` (`PLANNING.md`);
  Windows confirmation alongside `add-demo-foundation` task 9.3.
