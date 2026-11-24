# Proposal

## Why

The first-round Aspire 17 upgrade (ADR-0035) left work behind (FR-0007):
- The build warns about deprecated Aspire APIs and about known security problems in libraries the site
  pulls in.
- The smoke check no longer builds.
- Every event is in the past, so the site shows no booking form, booking count or booking telemetry.

The PO gives the talk again soon and keeps giving it, and the repo stays published for
attendees. So the demo must work on any date and must match the Aspire 17 dashboard.

## What Changes

- **Event dates follow the import day.** A fresh site imports its events with dates relative to the day of
  the import. The original order and spacing are kept: one past event for the "This event has ended" page,
  the rest upcoming within the next four weeks. The demo works on any date after a database reset. The
  committed content files keep their original dates.
- **Clean build.** No deprecation warnings from the orchestration platform, and no known high-severity
  vulnerability warnings from dependencies. The platform stays on the Umbraco 17 LTS and .NET 10
  (PO 2026-11-24).
- **Container images.** Update the observability containers to their current stable versions. All three
  acts must behave as before.
- **Smoke check.** It builds again and passes on a fresh environment, including the booking checks.
- **README.** A note that the Aspire agent skills exist but are deliberately not installed in the repo,
  and why. PO 2026-11-25: keep them out. They let the agent restart services itself, which removes the
  "agent asks, presenter restarts" moment and the approval prompts.
- **Presenter runthrough.** Re-checked against the redesigned Aspire 17 dashboard for the next demo and
  later runs. The talk date in it becomes generic.
- **Agent settings.** Pre-approve the Aspire agent server's new read tool, so the opt-in setup stays
  prompt-free for reads.
- **Unchanged:** pages, forms, emails, telemetry, the three planted problems, the agent's read-only
  Grafana access, and standalone mode.

**Order (PO).**
- **Now:** event dates, smoke check, README and runthrough. The next demo needs them.
- **Next, if needed:** the library updates and the AppHost migration, which close the security warnings.
  They ship before the demo only if the full smoke check and an act rehearsal pass.
- **After the demo:** container images, because they carry the most risk to the acts.

Out of scope:
- Umbraco 19 and .NET 11 (Umbraco 19 is still a release candidate).
- Installing the Aspire skills.
- Dates that move on an existing database without a reset.

## Capabilities

### New Capabilities
None.

### Modified Capabilities
- `agency-site`: "Events" — event dates are relative to the day the content is imported, instead of fixed
  dates after 16 Oct 2026.
- `local-environment`: "Attendee README" gains the note on agent skills. New requirement "Clean build":
  no deprecation or high-severity vulnerability warnings.

## Impact

- Site: content import (event start dates), one obsolete content API call.
- AppHost project and package versions; container image tags.
- Smoke check script; agent permission settings; README.
- Docs:
  - New ADR-0036 (upgrade completion: AppHost project model, library and image versions, Aspire skills
    kept out).
  - New ADR-0037 (event dates relative to the import day; amends ADR-0003 and ADR-0014).
  - ADR-0035 Status.
  - Presenter runthrough, CLAUDE.md, PLANNING.md, FR-0007 Status.
- Presenter: reset the database the morning of each demo (already in the runthrough) so the events get
  fresh dates.
