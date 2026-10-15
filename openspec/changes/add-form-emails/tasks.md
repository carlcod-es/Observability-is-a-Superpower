Bar: demo/prototype (design.md "Bar and test strategy"). Talk 16 Oct 13:30: sections 1, 2 and 4
run in parallel (disjoint files); 3 after 2.2; 5 after 1-4; 6 last.
Contracts: TempData keys `AcmeFormSuccess` (form key) and `AcmeFormReference` (`ACME-nnnn`)
between sections 2 and 3; metric `acme_forms_submissions_total{acme_form, acme_form_outcome}`
between sections 2 and 1 (ADR-0027).

## 1. Environment and dashboard (worker-backend; `src/SuperPower.AppHost/`; no dependencies)

- [x] 1.1 Remove the Toxiproxy container, its `ResourceReadyEvent` hook, the three commands, `ToxicCommand` and `SlowLatencyMs`; publish smtp4dev SMTP on host 2525 -> 25 (`isProxied: false`); site `.WaitFor(smtp4dev)`; add `Acme__Forms__OwnerEmail=owner@acme-agency.example` to the site (design.md D1, ADR-0025, ADR-0027); verify `dotnet build SuperPower.slnx` passes and a fresh AppHost start shows no `toxiproxy` resource and all other resources as before
- [x] 1.2 Add the "Form submissions by outcome" panel to `grafana/dashboards/demo-site.json` (design.md D5); verify after `lgtm` start and a few submissions that the panel's target returns data through Grafana `/api/ds/query` with series per form and outcome

## 2. Site forms (worker-backend; `src/SuperPower.Web/` C# code, `tests/SuperPower.Web.Tests/`; no dependencies)

- [x] 2.1 Reference: `ReferenceNumber` column + unique index on `FormSubmissionDto`, migration step `AddSubmissionReferenceStep` (column, backfill, index, lock row 800001), repository allocates under `scope.WriteLock` and returns the number, `ACME-{n:D4}` formatter with xUnit test (design.md D2, ADR-0026); verify `dotnet test --solution SuperPower.slnx` passes, the AppHost starts on the existing SQL Server database and the standalone site on its existing SQLite file without migration errors, and a valid post stores a row with a reference (PO runs the count/reference query)
- [x] 2.2 Notification and telemetry in `AcmeFormsController`: success TempData carries form key and reference, `SubmissionNotification.Build` (pure, xUnit test for subject/body), send via `IEmailSender` after the scope with skip rules, `acme.forms.submit` / `acme.forms.notify` spans, `acme.forms.submissions` counter, `Form {Form} stored as {Reference}` log (design.md D3, D4, ADR-0027); verify tests pass, a Contact post on the AppHost shows both spans with form/outcome/reference and the log line in the Aspire dashboard, one email in smtp4dev with the same reference, and a standalone-site post succeeds with no email and no error

## 3. Success text (worker-frontend; `src/SuperPower.Web/Views/Partials/Forms/`; depends on 2.2 contract)

- [x] 3.1 All three form partials render "Thank you, we'll be in touch. Your reference is ACME-nnnn." from the TempData contract; verify by posting each form on the AppHost and seeing its reference, and that a refresh stores no second row

## 4. Burst tool (worker-backend, shared path assigned by the Architect; `tests/traffic/burst.cs`; no dependencies, checked against section 2)

- [x] 4.1 Write `tests/traffic/burst.cs` per design.md D6; verify against the AppHost running section 2's code that `dotnet run tests/traffic/burst.cs` prints `ok=50 failed=0` with 50 distinct references, smtp4dev holds 50 new emails, and `--count 5 --parallel 1` works

## 5. Smoke check, config and docs (worker-backend for `tests/smoke/`, shared config by brief; Architect for `docs/`, `PLANNING.md`, `CLAUDE.md`; depends on sections 1-4)

- [x] 5.1 `tests/smoke/smoke.cs`: remove the Slow/Break/Heal mail check, check the success page shows a reference that matches the stored row, and that smtp4dev holds an email with that reference; update the header comment; verify `dotnet run tests/smoke/smoke.cs` passes on a freshly started AppHost and `-- standalone` passes
- [x] 5.2 `.gitignore` adds `.presenter/`; `.claude/settings.json` adds `permissions.deny` `Read(./.presenter/**)` (design.md D7); verify `git status` does not list `.presenter/` and an agent `Read` of a file under it is denied
- [x] 5.3 No personal data: submit each form with name `Canary Person` and email `canary@test.invalid`, start `lgtm`, run the burst tool; verify neither value appears in the Aspire dashboard's logs/traces or in Grafana (Loki `|= "canary"`, Tempo search), and the demo dashboard's form panel and 5xx panel show the burst
- [x] 5.4 README (prerequisites drop the Toxiproxy image, environment description, burst tool, run-through) and `docs/project-setup.md` (resources, mail path, ports, telemetry names), CLAUDE.md (Stack, Local environment, Demo site, Telemetry), PLANNING.md; verify every path, port and name against the code

## 6. Presenter preparation (PO; depends on sections 1-5)

- [ ] 6.1 Create the demo branches and stage clone as described in `docs/presenter/runthrough.md`; verify each branch starts and the stage clone has no `docs/presenter/` folder
- [ ] 6.2 Rehearse the runthrough end to end on the stage machine; verify each act's expected observation in `docs/presenter/runthrough.md` is seen, timings recorded there
