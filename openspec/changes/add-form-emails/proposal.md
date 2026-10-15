## Why

The talk (16 Oct 2026) needs a new feature to build, diagnose and verify with telemetry, and
enough form activity on the production stage for the dashboard and the agent to work with.
Acts are simplified for the day before the talk (PO 2026-10-15): local stage on the Aspire
dashboard and Aspire MCP; production stage behind the tunnel with Grafana, its dashboard and
the Grafana MCP, under attendee and scripted load. Mail-path fault injection is no longer
demonstrated.

## What Changes

- Every stored submission gets a unique, human-readable reference (e.g. `ACME-0042`). The
  success message shows it: "Thank you, we'll be in touch. Your reference is ACME-0042."
- Owner notification email for every stored submission of all three forms, to an owner address
  set per environment. Fixed subject and body; the body carries the form, page, reference and
  submitted field values. Delivered to the local mail catcher only (nothing leaves the machine;
  local data is deleted after the talk).
- Submitter confirmation emails are dropped (were planned; PO 2026-10-15).
- Form telemetry: each submission attempt is traced and counted by form and outcome, with the
  reference when stored. No personal data in telemetry (unchanged rule).
- Demo dashboard: a form submissions panel (by form and outcome).
- Submission burst tool: submits many valid forms concurrently with made-up data, so the
  production stage sees form load beyond what attendees produce.
- **BREAKING (environment)**: the mail-path fault hop and its Slow / Break / Heal commands are
  removed. Mail goes straight from the site to the mail catcher.
- Presenter notes live in a git-ignored folder the agent's committed settings deny reading.

Out of scope: real mail delivery, submitter confirmations, retries or queueing of mail, the
tunnel itself and database reset (`add-production-environment`, `docs/demo-setup.md`), slide
changes (`add-talk-slides`, dropped 2026-10-15).

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `form-submission`: submission reference, success message with reference, owner
  notification, submission telemetry.
- `local-environment`: mail-path fault controls removed; one-command start no longer includes
  the fault hop.
- `demo-dashboard`: form submissions panel; submission burst tool.
- `agent-telemetry-access`: resource-command scenario no longer cites a mail-path fault.

## Impact

- Site forms code, its stored submission (new reference), form partials (success text), the
  environment host, the demo dashboard, demo tools under `tests/`, smoke check, README and
  `docs/project-setup.md`, committed agent settings, `.gitignore`.
- ADR-0010 partly superseded (fault hop removed); ADR-0016 amended (reference, notification,
  telemetry).
- `add-talk-slides` dropped 2026-10-15 (PO); slides added to the repo after the talk (PLANNING.md Todos).
