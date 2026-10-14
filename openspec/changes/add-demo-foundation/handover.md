# Handover: add-demo-foundation

State at 2026-10-14 (end of session), branch `sample-site`. Paused for PO UI work on the site.
Resume with `/opsx:apply add-demo-foundation`; read this file, `review-followups.md` and
`po-backoffice-steps.md` first. `tasks.md` is the source of truth for progress.

## Progress

18 of 30 tasks ticked. Sections 1, 2, 4 and 5 are complete; section 3 lacks only 3.5.

Done this session:
- Block 7A (task 7.1): typed views against `SuperPower.Web.Models`, layout with navigation and
  breadcrumbs, card/media/main-content partials, block list component views (textBlock,
  titleBlock, imageBlock), one stylesheet `wwwroot/css/site.css`, self-hosted fonts with
  licences in `wwwroot/css/fonts/`. Reviewer APPROVED. Evidence in `verification.md`
  "Section 7"; minors and nits in `review-followups.md` (block 7A).
- Task 4.1 ticked on the block 7A runtime check.
- `docs/project-setup.md` written (PO request), linked from `README.md`; task 9.6 added to keep
  it current.

Uncommitted (PO commits): the block 7A views, stylesheet and fonts; `docs/project-setup.md`;
the `README.md` link; edits to `tasks.md`, `review-followups.md`, `verification.md`,
`po-backoffice-steps.md` and this file.

## Open tasks

| Task | Owner | Waiting on |
|---|---|---|
| 3.5 | Architect | PO past-event check; then an Event booking POST to that past event must store nothing (row count via `sqlite3 -readonly`, or `sqlcmd` in the `sql` container on the Aspire run) |
| 7.1 | PO check | Move one event's start into the past locally (do not commit the uSync export), confirm "This event has ended" and no form, and that it leaves the Events list and Home; glance at the browser network panel (site requests only) |
| 6.3 | PO | Content: Home body (Borderlands placeholder and "Rubber Duck Agency" text, banner heading "Homepage"), Summary on top-level pages, About us body, event summaries/bodies, service descriptions |
| 6.4 | PO | Optional banner images for events and services; the `<img>` branch of `Views/Partials/Media.cshtml` has never run |
| 8.1, 8.2 | PO | Fresh Claude Code session at the repo root with the AppHost running: Home trace with spans, its logs, a request-duration metric, no approval prompts; 22 read-only tools only |
| 9.1 | Architect | Attendee README (can start now) |
| 9.2 | Architect | Fresh-database seeding on SQL Server and SQLite; after content and media are final |
| 9.3 | PO + Architect | Windows and macOS runs from a fresh clone; Linux standalone in a container |
| 9.4 | worker-backend | Smoke script in `tests/smoke/` (can start now); must pass before cutting `demo/act-1-start` |
| 9.5 | Architect | `PLANNING.md` tidy |
| 9.6 | Architect | `docs/project-setup.md` matches the final state |

Reviews owed: supervisor over sections 3, 4 and 5 once 3.5 is ticked; supervisor over
section 7 once 7.1 is ticked.

Planned next on resume: PO past-event check -> tick 3.5 and 7.1 -> supervisor passes ->
9.4 (worker) and 9.1 (Architect) in parallel -> 9.2, 9.3 -> 9.5, 9.6.

## Constraints for the PO's UI work

The block 7A review approved the views as they are now. UI changes must keep these, or task 7.1
and the forms checks need re-running:
- One stylesheet (`wwwroot/css/site.css`), no scripts, no external fonts, images or CSS (spec
  `agency-site` "Minimal page dependencies").
- Fonts stay under `/css/fonts/`: `/css` is excluded from telemetry; a new top-level static
  path (e.g. `/fonts`, `/img`) would produce traces unless added to `TelemetryPaths.cs`
  (ADR-0008).
- No `<img>` without an image: missing `mainBanner` renders the colour block
  (`Views/Partials/Media.cshtml`; spec `agency-site` "Imagery from the media library").
- Form partials (`Views/Partials/Forms/`): keep `Html.BeginUmbracoForm<AcmeFormsController>`,
  `novalidate`, `asp-for` inputs bound to the form class, `asp-validation-summary="ModelOnly"`
  on Event booking, no `required`/`pattern` attributes, and the stylesheet classes
  `form-field`, `form-check`, `required-marker`, `visually-hidden`, `field-error`,
  `form-errors`, `form-success` (ADR-0016; contract in `review-followups.md` block 3E).
- Views render forms with a fresh model (`new ContactForm()` etc.) and use `IEventSchedule` /
  `IUpcomingEvents` for event state; do not add date logic in views.
- Razor is compiled at runtime (`RazorCompileOnBuild=false`): `dotnet build` does not catch view
  errors; load every page type after changes.
- After any document type change, regenerate models (Settings > Models Builder > Generate) and
  commit `src/SuperPower.Web/Models/`.
- Acme Agency branding only; no Rubber Duck, Blog, newsletter or thank-you remnants.

Tell the Architect when the UI work lands; it gets a reviewer pass against the list above
before 7.1 is ticked.

## Environment notes

- Two independent databases: Aspire uses the SQL Server container (Docker volume
  `superpower.apphost-<hash>-sql-data`); `dotnet run` uses
  `src/SuperPower.Web/umbraco/Data/Umbraco.sqlite.db`. Both persist; details and resets in
  `docs/project-setup.md`.
- The SQLite database was created fresh by block 7A (none existed) and holds 6 fake test
  submissions.
- `aspire run` needs `--apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj` while
  `spikes/` exists.
- LGTM telemetry is lost on every AppHost restart; generate traffic before agent queries.
- ADR-0018 and ADR-0019 (prototype tooling, fonts) exist on `main` only.
