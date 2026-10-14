## Why

The talk on 16 Oct 2026 needs slides around the live demo. Slides are maintained outside this
repo, and no outline exists yet that maps each talk promise to a slide or demo segment within
45 minutes.

## What Changes

- New slide outline for the talk: opening, concept slides, a slide lead-in to each live demo
  act, closing. About 15 minutes of slides, about 30 minutes of live demo.
- Each slide entry: title, on-slide text, speaker notes, suggested visual, time budget.
- Demo acts drafted from the talk promises. Planted problems, their causes and fixes are left
  as PO placeholders; the outline holds no demo answers.
- Audience: Umbraco developers. OpenTelemetry, Aspire and MCP get a short introduction; Umbraco
  basics do not.
- Non-code change: no site, environment or telemetry behaviour changes.

## Capabilities

### New Capabilities
None. Documentation only; the change sets `skip_specs: true`.

### Modified Capabilities
None.

## Impact

- New file in this change directory: the slide outline. Moved outside the repo once the PO
  takes it into the slide tool.
- No code, configuration or dependency changes.
- Reads existing docs only: `README.md`, `docs/project-setup.md`, ADRs, `PLANNING.md`.
