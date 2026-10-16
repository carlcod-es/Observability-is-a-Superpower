Bar: demo (design.md Context). Optional segment after act 2, talk 2026-10-16 13:30 BST. Order:
1 → 2 → 3 → 4 → 5 → 6. The main demo is stopped (including its `sql` resource) whenever the AI demo
runs. No file outside `demo-ai/` changes except README.md (task 5.1); Architect files: tasks 5.2,
5.3.

## 1. Site and solution (worker-backend; `demo-ai/`; no dependencies)

- [x] 1.1 Create `demo-ai/Directory.Build.props` (copy of root), `demo-ai/Directory.Packages.props` (versions per design.md decision 1; ICU version property as in the root) and `demo-ai/SuperPower.AiDemo.slnx` (ADR-0030 decision 1); verify `dotnet build demo-ai/SuperPower.AiDemo.slnx` resolves only `demo-ai/Directory.Packages.props` (e.g. `dotnet msbuild -getProperty:DirectoryPackagesPropsPath` on the site project) and `git status` shows no change outside `demo-ai/`
- [x] 1.2 Generate `demo-ai/src/SuperPower.AiDemo.Web` with `Umbraco.Templates` 17.7.1 using the design.md decision 2 command, convert it to central package management, add `Clean` 7.0.8, add it to the slnx (ADR-0030 decision 2); verify `dotnet build demo-ai/SuperPower.AiDemo.slnx` passes and a standalone `dotnet run --project demo-ai/src/SuperPower.AiDemo.Web` installs on SQLite, serves the Clean home page with sample blog articles, and the backoffice accepts the demo administrator; delete the generated SQLite database and media afterwards and confirm they are git-ignored
- [x] 1.3 Add `Umbraco.AI` 17.5.2, `Umbraco.AI.Anthropic` 17.1.2 and `Umbraco.AI.Prompt` 17.3.0 to the site (ADR-0031 decision 1); verify the build passes and a standalone run logs the Umbraco.AI package migrations as completed with no errors

## 2. AppHost and telemetry (worker-backend; `demo-ai/src/`; depends on 1)

- [x] 2.1 Create `demo-ai/src/SuperPower.AiDemo.ServiceDefaults` as a copy of the main ServiceDefaults per design.md decision 4 (no redaction, no `Acme.Forms`; add source and meter `Umbraco.AI`), reference it from the site and call `AddServiceDefaults()` / `MapDefaultEndpoints()` in `Program.cs` as the main site does (ADR-0030 decision 4, ADR-0031 decision 2); verify the build passes and `grep -r "Acme\|Redact" demo-ai/src` finds nothing
- [x] 2.2 Create `demo-ai/src/SuperPower.AiDemo.AppHost` (Aspire 13.6 SDK, `AspireUseCliBundle`, own `UserSecretsId`, `aspire.config.json`) with `AppHost.cs` per design.md decision 3, including the marked capture line set to `false`, and a `launchSettings.json` with generated ports (ADR-0030 decision 3, ADR-0031 decisions 3, 4); verify the build passes
- [x] 2.3 Start with `aspire run --apphost demo-ai/src/SuperPower.AiDemo.AppHost/SuperPower.AiDemo.AppHost.csproj` on an empty data volume, entering any placeholder value for `anthropic-api-key` when the dashboard asks; verify `sql`, `umbraco` and `site` reach running, the site serves the Clean home page from SQL Server, a page trace in the dashboard contains SQL spans, and a restart keeps the content (spec `ai-demo` "One-command start", "Sample content on first start"); report the dashboard URL and the site URL

## 3. Backoffice AI setup (PO; database state only; depends on 2)

- [ ] 3.1 (PO) Start the AI demo; enter the Anthropic key when the dashboard asks and save it to user secrets; verify `dotnet user-secrets list --project demo-ai/src/SuperPower.AiDemo.AppHost` shows the parameter and no key appears in `git diff`
- [ ] 3.2 (PO) Do design.md decision 5 steps 1-4 (AI section, connection with `$Umbraco:AI:Secrets:AnthropicApiKey`, profile, prompt); pick the property, prompt wording and model; verify the prompt returns a response on a sample blog article and the connection's key field shows the reference (spec `ai-demo` "Editor runs an AI prompt")

## 4. Verification (Architect via Aspire MCP, PO for the dashboard; depends on 3)

- [ ] 4.1 With the PO running the prompt once, find the trace via Aspire MCP (`select_apphost` on the AI demo, `list_traces`); verify a `gen_ai.chat {model}` span with `umbraco.ai.profile.alias`, `umbraco.ai.feature.type`, `umbraco.ai.entity.id` and `umbraco.ai.user.id`, and a child HTTP span to the Anthropic API; record the trace summary in `verification.md` (spec `ai-demo` "AI calls in telemetry")
- [ ] 4.2 (PO) In the AI demo dashboard Metrics, verify `gen_ai.client.token.usage` (input and output) and `gen_ai.client.operation.duration` show values for the model; note in `verification.md`
- [ ] 4.3 Capture switch: with `false`, verify via Aspire MCP that the 4.1 trace and its structured logs contain no prompt or response text; PO sets the marked line to `true`, restarts, runs the prompt; verify the new span carries the prompt and response text; PO sets it back to `false`; record both results in `verification.md`. If `true` has no effect, stop and report to the PO (ADR-0031 Consequences) (spec `ai-demo` "Prompt content kept out by default")

## 5. Docs (worker-backend for README files by brief; Architect for presenter notes, `CLAUDE.md`, `PLANNING.md`; depends on 4)

- [ ] 5.1 Write `demo-ai/README.md` (prerequisites, image pre-pull, start command, provider key prompt, design.md decision 5 setup steps with the PO's choices from 3.2, the capture switch and where it is, stop/reset commands) and add a short "AI demo" pointer to the root `README.md`; verify the README start command and the switch location match the code
- [ ] 5.2 Architect: write `demo-ai/presenter-notes.md` per design.md decisions 6 and 7 (Preparation, Switch-over, Stage steps with the live capture flip, Fallback, Reset), using the PO's choices from 3.2 and the results in `verification.md`; verify every command, click path and span or metric name in the notes matches the code and `verification.md` (spec `ai-demo` "Presenter notes")
- [ ] 5.3 Architect: CLAUDE.md (stack line for the AI demo, `demo-ai/` ownership to worker-backend), PLANNING.md entry, ADR-0030/ADR-0031 status confirmed; verify `openspec validate add-ai-demo --strict` passes

## 6. Preparation and rehearsal (PO; no files; depends on 5)

- [ ] 6.1 (PO) Run the presenter notes "Preparation" section on the presenter machine; verify the AI demo then starts with no install, migration, key prompt or setup step, the prompt works at once, the capture line is `false`, and the AI demo's `sql` resource and AppHost are stopped at the end (spec `ai-demo` "Prepared before the talk")
- [ ] 6.2 (PO) Rehearse from the notes only: main demo running → "Switch-over" → "Stage steps" including the live capture flip and flip back; verify every step works as written, the main demo's `sql` container is stopped (`docker ps`), and note the switch-over and segment times in `verification.md`; report any step that needed information outside the notes (spec `ai-demo` "Runs instead of the main demo", "Presenter notes")
