## Why

The talk on 16 Oct 2026 needs a working demo base: a believable site with forms, a local
environment that brings logs, traces and metrics into one place, and an agent that can read
that telemetry. Every demo branch starts from this state (Act 1), so it must exist and be
stable before any feature, fault or fix is built on top of it. Nothing is built yet.

## What Changes

- A small fictional pottery studio site (working name Slipcast Studio) with Home, Classes and
  Contact pages, populated automatically when the site starts against an empty database.
- A forms package (`SuperPower.Forms`) that lets editors define forms from field blocks and
  renders, validates and stores submissions. Two forms on the site: Contact and Book a class.
- A local environment the presenter starts with one command: site, database, mail catcher and
  a fault-injection hop on the mail path, all visible in one observability dashboard.
- Baseline telemetry from the site and its database calls, without any package-specific
  instrumentation.
- Agent access to local telemetry over MCP, with the configuration committed for attendees.
- A file-based database mode so attendees can run the site without a container runtime.
- A short attendee README.

Out of scope (later changes):
- Owner notification and submitter confirmation emails, and the package's own spans and metrics.
- Production environment, production agent access, attendee safeguards, history generation.
- Protection of personal data in telemetry beyond what the baseline records.

## Capabilities

### New Capabilities
- `studio-site`: the pottery studio site, its pages and content, and how it is populated on
  first start.
- `form-definitions`: how editors define a form and its fields (types, alias, label, required,
  pattern with error message).
- `form-submission`: rendering a form, server-side validation, error display, forgery
  protection and storing a submission.
- `local-environment`: one-command start of all components, the observability dashboard, the
  mail catcher, mail-path fault controls, and the file-based database mode.
- `agent-telemetry-access`: the coding agent reading local telemetry and resource state over MCP.

### Modified Capabilities
<!-- none: no existing specs -->

## Impact

- New projects under `src/` (site exists as an empty Umbraco site; package, environment host and
  shared defaults are new) and a test project under `tests/`.
- New repo files: agent MCP configuration, attendee README, ADRs under `docs/adrs/`.
- Committed site content files; the database stays out of source control.
- Developer prerequisites: .NET 10 SDK, a container runtime for the full environment, current
  Aspire CLI (installed version 9.3.1-preview is outdated).
