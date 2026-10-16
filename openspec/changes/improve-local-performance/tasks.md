Bar: demo (design.md Context). Talk 16 Oct 13:30. Order: 1 → 2 → 3 → 4; 5 (PO) in parallel
with 2-4. Each step is measured as in design.md "Measurement" (M1-M5), recorded in
`verification.md`; an item without a gain beyond noise is reverted (ADR-0029).

## 1. Baseline (Architect via Aspire MCP, PO for M5; no code; no dependencies)

- [ ] 1.1 With the current Debug `aspire run`, record M1-M5 three times each on the M1 (fresh AppHost start per run) and write the median table to `verification.md`; verify the table has a value for every measure and page type, and that the first Home trace contains database spans

## 2. Site build and stage settings (worker-backend; `src/SuperPower.Web/SuperPower.Web.csproj`, `src/SuperPower.AppHost/AppHost.cs`; depends on 1)

- [ ] 2.1 Find a stage start command that builds AppHost and site in Release through `aspire run` and keeps the Aspire MCP working (try `Configuration=Release` as an MSBuild environment property, then a Release build plus `aspire run --no-build`) (ADR-0029 decision 2); verify the site assembly loaded is `bin/Release` (console log or process path), `mcp__aspire__list_apphosts` lists the AppHost, and report the exact working command (no file changes)
- [ ] 2.2 Remove `RazorCompileOnBuild`/`RazorCompileOnPublish=false` and their comment from `SuperPower.Web.csproj` (ADR-0029 decision 1); verify `dotnet build SuperPower.slnx` passes, `bin/*/net10.0/SuperPower.Web.dll` contains compiled views (e.g. a `Views_Home` type), and `dotnet run tests/smoke/smoke.cs` and `-- standalone` pass
- [ ] 2.3 In `AppHost.cs`, set `Umbraco__CMS__Hosting__Debug=false` on the site (ADR-0029 decision 3); verify `dotnet build SuperPower.slnx` passes, the site's environment in the Aspire dashboard shows the value, and the smoke check passes

## 3. Measure after each step (Architect via Aspire MCP, PO for M5; depends on 2)

- [ ] 3.1 Record M1-M5 after 2.2, after 2.3, and on the 2.1 Release command, three runs each, as columns next to the baseline in `verification.md`; verify each item shows a gain beyond the run-to-run spread, or note it for revert (PO decides)

## 4. Behaviour unchanged and docs (worker-backend for README by brief; Architect for `docs/`, `PLANNING.md`, `CLAUDE.md`; depends on 3)

- [ ] 4.1 On the stage command: smoke check passes; first Home load trace has database spans; a Contact post shows `acme.forms.submit`/`acme.forms.notify`, the counter, the redacted log and an email in smtp4dev; `lgtm` starts and Grafana MCP answers a Tempo/Loki query; verify each item and record it in `verification.md` (spec `local-environment` "Optimised stage run")
- [ ] 4.2 Standalone site (`dotnet run --project src/SuperPower.Web`) still installs on SQLite with development settings and serves all pages and forms; confirm the stage run needs no `appsettings.Local.json`; verify with `dotnet run tests/smoke/smoke.cs -- standalone`
- [ ] 4.3 README: add the stage start command next to the development command, with one line on when to use each; ADR-0029 Status → Accepted with the measured gains; PLANNING.md and CLAUDE.md "Local environment" updated; verify the README command matches the one reported by 2.1

## 5. Presenter machine and runthrough (PO; `docs/presenter/`; no dependencies)

- [ ] 5.1 (PO) Docker Desktop: Rosetta emulation and VirtioFS on; memory sized from `docker stats` with `lgtm` running, leaving macOS without swap; quit heavy apps; mains power; restart Docker and the AppHost before the talk (ADR-0029 decision 5); verify `docker info` shows the memory, the SQL container starts, and Activity Monitor shows no swap growth during a `burst.cs` run
- [ ] 5.2 (PO) Runthrough: switch to the stage start command; no warm-up step (PO 2026-10-15); verify a full rehearsal on the stage command, with the first Home load on stage showing database spans
- [ ] 5.3 (PO) Approve the merge into `main` (no demo branches, PO 2026-10-16); verify `main` builds and starts with the stage command
