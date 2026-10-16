## Context

See proposal.md (Why) and spec `ai-demo`. Bar: demo (CLAUDE.md "Principle"): runs on the
presenter machine and for attendees; no tests beyond build and the checks in tasks.md.
Talk is 2026-10-16 13:30 BST; this segment is optional and must not touch the main demo.

Current state: the main demo (`src/`, `SuperPower.slnx`) uses Aspire 13.6 AppHost with
`AddAzureSqlServer(...).RunAsContainer()`, ServiceDefaults with Serilog forwarding (ADR-0007),
OTLP export (ADR-0008), Umbraco 17.7.1 via central package management in the root
`Directory.Packages.props`. Its `sql` container has a persistent lifetime, so it keeps running
after the main AppHost stops. The two demos never run at the same time (PO 2026-10-16).

## Goals / Non-Goals

**Goals:**
- `demo-ai/` builds and starts on its own with `aspire run`.
- AI prompt calls visible as spans and metrics in the AI demo's Aspire dashboard.
- One marked line flips prompt/response capture, live on stage.
- Everything except the switch-over, the prompt run and the switch flip is done before the talk.

**Non-Goals:**
- Collector, LGTM, Grafana MCP, smtp4dev, log redaction, Azure Monitor wiring beyond what the
  copied ServiceDefaults already does.
- Aspire MCP configuration for this AppHost (the existing `aspire agent mcp` server can select
  it via `list_apphosts`; not demoed).
- Automated tests, smoke script, act branches.

## Decisions

1. **Standalone solution in `demo-ai/`** (ADR-0030 decision 1). Layout:
   ```
   demo-ai/
     Directory.Build.props        net10.0, nullable, implicit usings (copy of root)
     Directory.Packages.props     own CPM list (Umbraco, Aspire, OpenTelemetry, Clean, Umbraco.AI)
     SuperPower.AiDemo.slnx
     README.md
     presenter-notes.md
     src/SuperPower.AiDemo.AppHost/      AppHost.cs, aspire.config.json, Properties/launchSettings.json
     src/SuperPower.AiDemo.ServiceDefaults/
     src/SuperPower.AiDemo.Web/          template output + Clean + Umbraco.AI
   ```
   Ports: whatever `dotnet new` / the Aspire AppHost template generate; no need to differ from the
   main demo (ADR-0030 decision 5).
   Package versions: the same as the root `Directory.Packages.props` for shared packages
   (Umbraco 17.7.1, Aspire 13.6.1, OpenTelemetry 1.17.0, ...), plus `Clean` 7.0.8,
   `Umbraco.AI` 17.5.2, `Umbraco.AI.Anthropic` 17.1.2, `Umbraco.AI.Prompt` 17.3.0. uSync and
   test packages are not needed.
   Alternative: inside the main solution (rejected, ADR-0030).

2. **Site from template + Clean** (ADR-0030 decision 2). Commands (from `demo-ai/src/`):
   ```
   dotnet new install Umbraco.Templates@17.7.1 --force
   dotnet new umbraco -n SuperPower.AiDemo.Web --friendly-name "Administrator" \
     --email "admin@example.com" --password "1234567890" --development-database-type SQLite
   ```
   Then strip versions from the generated `PackageReference`s (CPM), keep the template's
   app-local ICU block (version property in `demo-ai/Directory.Packages.props` as in the root),
   add `Clean`, `Umbraco.AI`, `Umbraco.AI.Anthropic`, `Umbraco.AI.Prompt` and the ServiceDefaults
   project reference. `Program.cs` gets `builder.AddServiceDefaults()` and
   `app.MapDefaultEndpoints()` in the same places as the main site. Template appsettings stay
   (SQLite for a standalone run; AppHost overrides the connection string).
   ModelsBuilder as the main site (PO 2026-10-16): `ModelsMode` `SourceCodeManual` in
   `appsettings.Development.json`, `Nothing` in `appsettings.json`, `ModelsDirectory` `~/Models`;
   generated models committed in `demo-ai/src/SuperPower.AiDemo.Web/Models/`, `Models/ood.flag`
   git-ignored. Only difference: `ModelsNamespace` `Umbraco.Cms.Web.Common.PublishedModels`
   (Umbraco default), because Clean's views use that namespace and Clean's build targets copy the
   views back on every build.

3. **AppHost** (ADR-0030 decision 3, ADR-0031 decisions 3, 4):
   ```csharp
   var db = builder.AddAzureSqlServer("sql")
       .RunAsContainer(c => c.WithDataVolume().WithLifetime(ContainerLifetime.Persistent))
       .AddDatabase("umbraco");

   var anthropicKey = builder.AddParameter("anthropic-api-key", secret: true);

   builder.AddProject<Projects.SuperPower_AiDemo_Web>("site")
       .WithReference(db, connectionName: "umbracoDbDSN")
       .WithEnvironment("ConnectionStrings__umbracoDbDSN_ProviderName", "Microsoft.Data.SqlClient")
       .WithEnvironment("Umbraco__AI__Secrets__AnthropicApiKey", anthropicKey)
       // Demo Code : AI
       // Prompt and response text on AI spans: "true" to show what would leak, "false" by default
       .WithEnvironment("OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT", "false")
       .WaitFor(db)
       .WithExternalHttpEndpoints();
   ```
   The marked comment follows the main demo's `// Demo Code : Act N` convention.
   Alternative: a boolean AppHost parameter for the switch; one fewer edit on stage, but a
   parameter prompt is less visible than a code line on screen. Kept as a code line.

4. **ServiceDefaults copy** (ADR-0030 decision 4, ADR-0031 decision 2). Copy
   `Extensions.cs`, `SerilogForwardingSink.cs`, `TelemetryPaths.cs`; drop `LogRedaction.cs`,
   `PersonalDataLogProcessor.cs`, the redaction key code, `Acme.Forms` and the
   `InternalsVisibleTo`. Keep the `Microsoft.Extensions.Hosting` namespace (Aspire convention, as the main copy). Add `.AddSource("Umbraco.AI")` to
   tracing and `.AddMeter("Umbraco.AI")` to metrics. The `gen_ai.client.*` metrics are emitted
   on that meter.

5. **Backoffice setup by hand** (ADR-0031 decision 5). Done by the PO once; persisted in the SQL
   data volume. Steps go in `demo-ai/README.md`:
   1. Check the AI section is visible (the Umbraco.AI install migration assigns it to Administrators); if not, Users > User Groups > Administrators: enable it; save; refresh.
   2. AI > Connections > Create: provider Anthropic, alias `anthropic`, API key
      `$Umbraco:AI:Secrets:AnthropicApiKey`.
   3. AI > Profiles > Create: chat profile on that connection, a fast model.
   4. Prompts: one prompt usable on a Clean blog article property (e.g. "Summarise this
      article in one sentence").

6. **Prepared before the talk; switch-over on stage** (ADR-0030 decision 5). Before the talk
   (presenter notes "Preparation"), with the main demo stopped:
   - pull the SQL Server image; `dotnet build demo-ai/SuperPower.AiDemo.slnx`;
   - first `aspire run`: Umbraco install, Clean content and Umbraco.AI migrations complete; key
     entered and saved to user secrets;
   - backoffice setup (decision 5) done; prompt run once and its span checked; capture line at
     `false`;
   - in the AI demo dashboard stop the `sql` resource, then stop the AppHost, so it does not hold
     memory while the main demo runs.
   On stage after act 2 (presenter notes "Switch-over"): in the main dashboard stop the persistent
   `sql` resource, stop the main AppHost (Ctrl+C), `aspire run` the AI demo (starts its stopped
   `sql` container; no install), log in to the backoffice in a browser tab kept open from
   preparation. Nothing to build, install or type except the start command.

7. **Presenter notes** (`demo-ai/presenter-notes.md`, Architect). Same style as
   `docs/presenter/`: numbered steps, exact commands and clicks, what to point at on screen.
   Sections: Preparation (day before, then on the morning of the talk), Switch-over,
   Stage steps (run prompt; trace: `gen_ai.chat` span attributes, child HTTP span to Anthropic,
   no prompt text; Metrics: token usage and duration; flip capture line to `true`, restart AppHost,
   rerun, prompt and response on the span; flip back), Fallback (no network or provider error:
   show the error span and move on, or skip the segment), Reset after the talk.

8. **Verification through the Aspire dashboard and Aspire MCP.** The Architect checks traces
   with the existing Aspire MCP (`select_apphost` on the AI demo AppHost, `list_traces`); the PO
   checks metrics in the dashboard (Aspire MCP has no metrics, ADR-0022).

## Risks / Trade-offs

- [Main demo's persistent SQL container left running during the AI segment] → Switch-over
  step stops it from the main dashboard first; rehearsed (task 6.2).
- [Venue network or Anthropic outage] → Segment is optional; presenter notes "Fallback".
- [Switch-over and AppHost start eat stage time: SQL under Rosetta, Umbraco boot] → All install
  and migration work done in preparation; switch-over rehearsed and timed (task 6.2).
- [Live switch flip needs an AppHost restart (~30-60 s)] → PO accepted 2026-10-16; the SQL
  container stays up across the restart; talk over it.
- [Dashboard telemetry is in memory; the preparation test trace is gone after restart] → The stage
  run produces fresh traces; nothing relies on prepared telemetry.
- [Umbraco.AI sets `EnableSensitiveData` itself, so the environment variable is ignored] →
  Task 4.3 checks both positions; on failure stop and report to the PO (fallback options:
  drop the switch from the demo, or a code hook, which would need a new decision).
- [Clean 7.0.8 targets Umbraco 17.5.1; site runs 17.7.1] → Minor upgrade within 17 LTS; NuGet
  resolves the higher version. Verified by the first-start check (task 2.3).
- [Template demo password `1234567890` in the repo] → Local-only demo admin, same as the main
  demo.

## Migration Plan

New folder only. Rollback: delete `demo-ai/` and the README pointer. The SQL data volume and
persistent container are removed with `docker rm` / `docker volume rm` if wanted.

## Open Questions

- Which Clean property and prompt wording make the best stage moment; PO picks during task 3.2.
- Which Anthropic model in the profile; the provider lists them; prefer the fastest for latency.
