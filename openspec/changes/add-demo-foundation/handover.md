# Handover: add-demo-foundation

State at 2026-10-14 (late), branch `sample-site`. Resume with `/opsx:apply add-demo-foundation`;
read this file, `review-followups.md` and `po-backoffice-steps.md` first. `tasks.md` is the
source of truth for progress; evidence is in `verification.md`.

## Progress

17 of 30 tasks ticked. Sections 1, 2, 4 and 5 complete. Section 3 lacks 3.2 and 3.5.

Done and reviewed, awaiting one check each:
- 7.1 site views (block 7A, reviewer APPROVED): PO past-event page check + network panel glance.
- 3.5 controller: runtime checks met except a booking post to a past event.
- 9.4 smoke script `tests/smoke/smoke.cs` (block 9A, reviewer APPROVED after rework): manual
  past-event post check.
- 3.2 migration plan: existing-database checks met on SQL Server and SQLite (byte-identical
  `FieldsJson`); fresh-volume / fresh-SQLite first boot waits for 9.2. Unticked by the
  sections 3-5 supervisor for missing post-re-plan SQL Server evidence.

Reviews done: sections 3-5 supervisor APPROVED (findings in `review-followups.md`). Owed:
section 7 supervisor after 7.1; section 9 supervisor at the end.

## PO decisions this session (recorded in specs, tasks, ADRs)

- Smoke check = .NET 10 file-based app `tests/smoke/smoke.cs` (ADR-0012 amended).
- Past-event booking post not scripted; manual check recorded in `verification.md`.
- Spec `local-environment` "Page request trace": database spans on the first Home load after
  start only (Umbraco caches later loads). The smoke check must run on a freshly started
  AppHost before any page is loaded; on a warm site its Home check fails with a restart hint.
- `docs/project-setup.md` added (task 9.6).

## Open tasks

| Task | Owner | Waiting on |
|---|---|---|
| 3.5, 7.1, 9.4 | PO + Architect | One past-event session: PO moves one event's start into the past (do not commit the uSync export); PO checks "This event has ended", no form, gone from Events list and Home; Architect posts an Event booking to it and confirms nothing stored; PO glances at the network panel; PO reverts the date |
| 6.3 | PO | Content: Home body ("Rubber Duck Agency" / Borderlands placeholder, banner heading "Homepage"), Summary on top-level pages, About us body, event summaries/bodies, service descriptions |
| 6.4 | PO | Optional banner images; the `<img>` branch of `Views/Partials/Media.cshtml` has never run |
| 8.1, 8.2 | PO | Fresh Claude Code session at the repo root with the AppHost running: Home trace with spans, its logs, a request-duration metric, no approval prompts; 22 read-only tools only |
| 9.1 | Architect | README drafted (prerequisites, pre-pull, runs, resets, log levels, Claude Code, smoke usage); verify by following it on a fresh clone (with 9.3) |
| 9.2 | Architect (destructive: PO approval) | Remove SQL volume + SQLite file + TEMP, start both; after content and media are final. Also closes 3.2 |
| 9.3 | PO + Architect | Windows and macOS from a fresh clone; Linux standalone in a container |
| 9.5 | Architect | `PLANNING.md` tidy |
| 9.6 | Architect | `docs/project-setup.md` matches the final state (now also: smoke usage, ADR-0018/0019 after the merge) |

Next backend block (from follow-ups): remove the dead submission read path
(`IFormSubmissionRepository`/`FormSubmissionRepository` read methods, `FormSubmission`), move
`EventAlias` next to `StartDateAlias`.

## Constraints for PO UI work

UI changes must keep these, or 7.1 and the forms checks need re-running:
- One stylesheet (`wwwroot/css/site.css`), no scripts, no external fonts, images or CSS (spec
  `agency-site` "Minimal page dependencies").
- Fonts stay under `/css/fonts/`; a new top-level static path (e.g. `/fonts`, `/img`) produces
  traces unless added to `TelemetryPaths.cs` (ADR-0008).
- No `<img>` without an image: missing `mainBanner` renders the colour block
  (`Views/Partials/Media.cshtml`).
- Form partials (`Views/Partials/Forms/`): keep `Html.BeginUmbracoForm<AcmeFormsController>`,
  `novalidate`, `asp-for` inputs bound to the form class, `asp-validation-summary="ModelOnly"`
  on Event booking, no `required`/`pattern`, and the classes `form-field`, `form-check`,
  `required-marker`, `visually-hidden`, `field-error`, `form-errors`, `form-success` (ADR-0016).
- Views render forms with a fresh model and use `IEventSchedule` / `IUpcomingEvents`; no date
  logic in views.
- Razor compiles at runtime (`RazorCompileOnBuild=false`): load every page type after changes;
  `dotnet run tests/smoke/smoke.cs -- standalone` covers pages and the Contact form.
- After document type changes, regenerate models and commit `src/SuperPower.Web/Models/`.
- Acme Agency branding only.

## Environment notes

- Two independent databases (Aspire: SQL Server container + Docker volume; standalone: SQLite);
  details and resets in `docs/project-setup.md`. Both hold fake "Smoke Test" submissions.
- SQL Server holds a trashed duplicate "Contact Us" node; harmless, gone after 9.2.
- Don't lower Umbraco/Serilog log levels to Debug while submitting forms: failed inserts would
  log form values.
- Merge of `main` (prototype, ADR-0018/0019, `add-site-prototype`) is safe; one `.gitignore`
  conflict, keep both blocks.
- `aspire run` needs `--apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj` while
  `spikes/` exists. LGTM telemetry is lost on every AppHost restart.
