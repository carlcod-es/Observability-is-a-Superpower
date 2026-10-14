# Handover: add-demo-foundation

State at 2026-10-14 (late), branch `sample-site` at 92bbbe1 (`main` merged). Resume with
`/opsx:apply add-demo-foundation`; read this file, `review-followups.md` and
`po-backoffice-steps.md` first. `tasks.md` is the source of truth for progress; evidence is in
`verification.md`.

## Resume here

1. `git status`: only this file and `docs/project-setup.md` may be uncommitted; ask the PO
   whether anything changed since (UI work, content). If views changed: reviewer pass against
   "Constraints for PO UI work" below, plus `dotnet run tests/smoke/smoke.cs -- standalone`.
2. Nothing runs between sessions; agents start and stop the AppHost or standalone site
   themselves. Serialize anything that runs the site with PO backoffice sessions.
3. Next actions, in order (backend clean-up block and 9.5 done 2026-10-14): PO content (6.3, 6.4) and
   8.1/8.2 -> 9.2 (destructive, PO approval) -> 9.3 -> 9.1, 9.6 -> section 9 supervisor.

## Progress

21 of 30 tasks ticked. Sections 1, 2, 4 and 5 complete. Section 3 lacks 3.2. Past-event session done 2026-10-14 (3.5, 7.1, 9.4 ticked).

Done and reviewed, awaiting one check each:
- 7.1 site views (block 7A, reviewer APPROVED): PO past-event page check + network panel glance.
- 3.5 controller: runtime checks met except a booking post to a past event.
- 9.4 smoke script `tests/smoke/smoke.cs` (block 9A, reviewer APPROVED after rework): manual
  past-event post check.
- 3.2 migration plan: existing-database checks met on SQL Server and SQLite (byte-identical
  `FieldsJson`); fresh-volume / fresh-SQLite first boot waits for 9.2. Unticked by the
  sections 3-5 supervisor for missing post-re-plan SQL Server evidence.

Reviews done: sections 3-5 supervisor APPROVED (findings in `review-followups.md`). Section 7 supervisor APPROVED 2026-10-14. Owed: section 9 supervisor at the end.

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
| - | PO | Revert the past event date (do not commit the uSync export) |
| 6.3 | PO | Content: Home body ("Rubber Duck Agency" / Borderlands placeholder, banner heading "Homepage"), Summary on top-level pages, About us body, event summaries/bodies, service descriptions |
| 6.4 | PO | Optional banner images; the `<img>` branch of `Views/Partials/Media.cshtml` has never run |
| 8.1, 8.2 | PO | Fresh Claude Code session at the repo root with the AppHost running: Home trace with spans, its logs, a request-duration metric, no approval prompts; 22 read-only tools only |
| 9.1 | Architect | README drafted (prerequisites, pre-pull, runs, resets, log levels, Claude Code, smoke usage); verify by following it on a fresh clone (with 9.3) |
| 9.2 | Architect (destructive: PO approval) | Remove SQL volume + SQLite file + TEMP, start both; after content and media are final. Also closes 3.2 |
| 9.3 | PO + Architect | Windows and macOS from a fresh clone; Linux standalone in a container |
| 9.6 | Architect | `docs/project-setup.md` matches the final state (smoke usage and `prototype/` added 2026-10-14) |

Backend clean-up block done 2026-10-14 (reviewer APPROVED; see verification.md). Remaining backend follow-ups are optional nits in review-followups.md.

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
- `main` merged 2026-10-14 (92bbbe1): `prototype/`, ADR-0018/0019 and change `add-site-prototype`
  (complete; archiving is the PO's call) are now on this branch.
- `aspire run` needs `--apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj` while
  `spikes/` exists. LGTM telemetry is lost on every AppHost restart.
