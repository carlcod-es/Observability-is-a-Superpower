# Review follow-ups: add-ai-demo

Minors and nits from block and section reviews. Read at the start of every apply session.

## Section 1, block 1.1-1.3 (reviewer, APPROVED)
- nit: `demo-ai/src/SuperPower.AiDemo.Web/Properties/launchSettings.json` keeps the template "IIS Express" and "Umbraco.Web.UI" profiles. Harmless; optionally drop IIS Express.
- nit: generated schema files (`appsettings-schema*.json`, `umbraco-package-schema.json`) are git-ignored by the template `.gitignore`; the main site commits its copies. No action.
- nit: build shows 16 warnings: 14 NU1902/NU1903 from transitive `SixLabors.ImageSharp` 3.1.12 and `System.Security.Cryptography.Pkcs` 10.0.0, inherited from Umbraco 17.7.1, plus 2 NU1507 (machine NuGet config has two sources; repo has no nuget.config). No action.
- note: the block 1 worker ran `dotnet new uninstall Umbraco.Templates` at the end (outside the brief); reinstall if needed.

## Section 1 (supervisor, APPROVED)
- nit: Clean's `buildTransitive/Clean.targets` copies views and assets into `demo-ai/src/SuperPower.AiDemo.Web/Views/**` and `wwwroot/{assets,css,favicon.ico}` before every build, overwriting edits. Decide commit vs ignore before commit; note it in the task 5.1 README.
- nit: template output has CRLF line endings; `core.autocrlf=input` normalises on commit. No action.
- nit: `demo-ai/src/SuperPower.AiDemo.Web/appsettings.Development.json:7-18` keeps the template Serilog Console sink. Acceptable (no redaction here); task 4.3 must also check console logs for prompt text.
- risk (presenter notes, task 5.2 / rehearsal 6.2): both demos use `localhost` and default backoffice cookie names; a main-demo backoffice login can invalidate the AI demo tab kept open from preparation. Use a separate browser profile or plan a re-login.
- risk (presenter notes): `InMemoryAuto` ModelsBuilder and Razor compile on first request after each start; warm the home page and backoffice before talking over them (switch-over and capture-flip restart).
- closed: task 2.3 showed Clean and Umbraco.AI migrations run on SQL Server.

## Section 2, block 2.1-2.2 (reviewer, APPROVED)
- deviation accepted (Architect): ServiceDefaults namespace kept as `Microsoft.Extensions.Hosting` (Aspire convention, as the main copy) instead of renamed per design.md decision 4.
- nit: `demo-ai/src/SuperPower.AiDemo.Web/Program.cs` ~line 17 has a double blank line. Cosmetic.

## Section 2 (supervisor, APPROVED)
- minor: task 2.3 passed the key via `Parameters__anthropic-api-key`, so the spec `ai-demo` "Missing provider key" dashboard prompt is unproven; covered by task 3.1 (start with no env var and no user secret, confirm the dashboard asks before the site starts).
- minor: spec `ai-demo` "Fresh database" backoffice login verified on SQLite only; confirm the `admin@example.com` login on SQL Server at the start of task 3.2.
- minor (done): the Umbraco.AI migration `AddAISectionToAdminGroup` already grants the AI section to Administrators; design.md decision 5 step 1 changed to a check. Carry into README (task 5.1) and presenter notes (task 5.2).
- minor: `AIOpenTelemetryChatMiddleware` passes an `ILoggerFactory` to `OpenTelemetryChatClient`; with capture on, message content may also appear in logs (no redaction here). Task 4.3: check structured and console logs as well as spans in both positions; record what `true` shows.
- nit: build warnings 24 (more NU1507, Pkcs 10.0.12 via Aspire). No action.
- nit (done): design.md decision 4 said "Rename namespace"; changed to match the accepted deviation.
- note: Umbraco.AI does not set `EnableSensitiveData` (no member reference in its DLLs); M.E.AI 10.7.0 contains the env var literal. Task 4.3 remains the proof.
- note: backoffice and AI management API requests are traced; on stage filter traces by `gen_ai.chat`.

## ModelsBuilder SourceCodeManual (PO 2026-10-16; reviewer, APPROVED)
- done: `SourceCodeManual` (Development) / `Nothing` (base), `~/Models`, namespace `Umbraco.Cms.Web.Common.PublishedModels` (Clean views); 45 models in `demo-ai/src/SuperPower.AiDemo.Web/Models/`; `/Models/ood.flag` ignored. design.md decision 2 updated.
- note: `<title>` looked empty to a single-line grep; `Views/Partials/metaData.cshtml:18-20` renders it on its own line. Not a regression.
- note (README, task 5.1): without committed models, Clean pages fail with a Razor `CompilationFailedException`; after any document type change, regenerate via Settings > Models Builder and rebuild.
