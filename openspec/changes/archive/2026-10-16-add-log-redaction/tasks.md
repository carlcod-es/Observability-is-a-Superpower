Bar: demo/prototype (design.md "Bar and test strategy"). Sections 1 and 2 run in parallel
(disjoint files); 3 after 1 and 2; 4 after 3.
Contract between sections 1 and 2: config key `Telemetry:RedactionKey` (env
`Telemetry__RedactionKey`), UTF-8 string; site falls back to a random per-process key.

## 1. Redaction in ServiceDefaults (worker-backend; `src/SuperPower.ServiceDefaults/`, `tests/SuperPower.Web.Tests/`, `src/SuperPower.Web/appsettings.Development.json` by brief; no dependencies)

- [x] 1.1 `LogRedaction.Redact` pure function per design.md D2 (ADR-0028) with xUnit tests for: flat and dotted names, case-insensitivity, email-valued field gets `[REDACTED:<id>]`, other blocked fields `[REDACTED]`, collected values replaced in the message, email in free text, same email (any case) same id, different email different id, different key different id, entry without matches unchanged; verify `dotnet test --solution SuperPower.slnx` passes
- [x] 1.2 `SerilogForwardingSink` flattens `StructureValue` into dotted attributes (design.md D3); `PersonalDataLogProcessor` applies `LogRedaction` in `OnEnd`; registered in `ConfigureOpenTelemetry` on one marked line before exporters, key from `Telemetry:RedactionKey` or random (design.md D1, D4); `InternalsVisibleTo` for the test project; pipeline xUnit test (Serilog -> sink -> provider with processor and a capturing processor, `{@Model}` with `Canary Person` / `canary@test.invalid`) asserts dotted attributes masked and canaries absent from attributes and formatted message; verify tests pass and that with the marked line commented out the build passes and the pipeline test's canaries appear (then restore the line)
- [x] 1.3 Remove the Serilog `WriteTo` Console entry from `src/SuperPower.Web/appsettings.Development.json` (design.md D5); verify `dotnet run --project src/SuperPower.Web` starts and serves Home with no Serilog lines on the console

## 2. Key in the AppHost (worker-backend; `src/SuperPower.AppHost/`; no dependencies)

- [x] 2.1 Generated, persisted secret parameter `telemetry-redaction-key` passed to the site as `Telemetry__RedactionKey` (design.md D4); stop and report if `GenerateParameterDefault` / `persist` is not available in Aspire 13.6; verify `dotnet build SuperPower.slnx` passes, a fresh AppHost start needs no manual input, and `dotnet user-secrets list --project src/SuperPower.AppHost` shows the parameter (key name only in the report, never the value)

## 3. Runtime check (worker-backend; temporary uncommitted edit in `src/SuperPower.Web/Forms/AcmeFormsController.cs`; depends on sections 1 and 2)

- [x] 3.1 With a temporary `logger.LogInformation("Received {@Model}", model)` in `Handle`, start the AppHost, start `lgtm`, submit Contact twice with `Canary Person` / `canary@test.invalid` and once with `other@test.invalid`; verify in the Aspire structured logs and Grafana Loki that the entries show masked dotted fields, the two canary entries share one `[REDACTED:<id>]` and the third differs, neither canary value appears anywhere (Aspire structured and console logs, Loki `|= "canary"`), and `Form <form> stored as <reference>` is unchanged; then remove the temporary line and verify `git diff` shows no change to the controller; run `dotnet run tests/smoke/smoke.cs` on a fresh AppHost start and verify only the accepted FAIL `no pre-start telemetry in Grafana`

## 4. Docs (Architect for `CLAUDE.md`, `docs/`; README by brief; depends on section 3)

- [x] 4.1 Layer 1 rule and the processor in CLAUDE.md "Telemetry" and `docs/project-setup.md` "Telemetry" (design.md D6), project-setup "Two ways to run the site" (no console logs), README "Log levels" (console output removed) and "Connect to the database" (AppHost secret `telemetry-redaction-key` next to the SQL password); verify every name and path against the code
- [ ] 4.2 (PO) Place the "console logs disabled" talking point appended to `docs/presenter/runthrough.md` and decide which demo branches comment out the processor line; verify during the `add-form-emails` task 6.2 rehearsal
