# Apply state: add-ai-demo

Snapshot 2026-10-16, branch `feature/add-ai-demo`, nothing committed. Read with `tasks.md` and
`review-followups.md` when resuming `/opsx:apply add-ai-demo`. Delete when the change is archived.

## Progress

6/16 tasks ticked: `add-ai-demo` sections 1 (tasks 1.1-1.3) and 2 (tasks 2.1-2.3), each block
reviewed and each section supervisor-approved.

Done outside the task list (PO 2026-10-16, reviewed APPROVED): ModelsBuilder `SourceCodeManual`
like the main site; 45 models committed-ready in `demo-ai/src/SuperPower.AiDemo.Web/Models/`
(namespace `Umbraco.Cms.Web.Common.PublishedModels`, required by Clean's views). design.md
decision 2 updated.

Started early, not ticked: task 5.1 draft. `demo-ai/README.md` written; root `README.md` has an
`## AI demo` pointer before "Cloudflare tunnel". TBD markers: model and prompt wording (task 3.2),
"Telemetry" section (section 4), "Capture switch" behaviour (task 4.3). Not yet reviewed.

## Next

1. PO: task 3.1 (key) and task 3.2 (backoffice setup), following `demo-ai/README.md` "Provider
   key" and "Backoffice setup". Extra checks from the section 2 supervisor:
   - task 3.1: start with no `Parameters__anthropic-api-key` env var and no user secret; confirm
     the dashboard asks before the site starts (never exercised; 2.3 used an env var placeholder).
   - task 3.2: confirm the `admin@example.com` backoffice login on SQL Server first (only proven
     on SQLite).
   - PO reports: chosen model, target property, prompt name and instructions; any UI label that
     differs from the README.
2. Architect, with the PO's AppHost running and the prompt run once: task 4.1 via Aspire MCP
   (`list_apphosts`, `select_apphost` on
   `demo-ai/src/SuperPower.AiDemo.AppHost/SuperPower.AiDemo.AppHost.csproj`, `list_traces` search
   `gen_ai`); write `verification.md`. PO: task 4.2 (metrics in dashboard). Task 4.3: check spans,
   structured logs and console logs in both switch positions (`review-followups.md` section 2
   minor); PO flips `AppHost.cs:15`, restarts, reruns, flips back.
3. Finish task 5.1 (fill TBDs, reviewer), task 5.2 presenter notes (include the cookie/login and
   warm-up risks from `review-followups.md` section 1), task 5.3 (CLAUDE.md stack line, `demo-ai/`
   ownership to worker-backend, PLANNING.md entry 3c status, ADR status, `openspec validate
   add-ai-demo --strict`).
4. PO: tasks 6.1, 6.2.

## Environment facts

- Start: `aspire run --apphost demo-ai/src/SuperPower.AiDemo.AppHost/SuperPower.AiDemo.AppHost.csproj`.
  Dashboard https://localhost:16347, site https://localhost:44383 (launch profile
  `Umbraco.Web.UI`), backoffice login `admin@example.com` / `1234567890`.
- AI demo SQL container `sql-abbe1079`, volume `superpower.aidemo.apphost-abbe107915-sql-data`
  (already installed with Clean content and Umbraco.AI migrations; works with the new committed
  models, no reset needed). Main demo SQL container `sql-d0adb38c`. Both ran side by side with no
  clash (PO asked to test it; on stage only one runs).
- Passing a parameter from a shell needs `env 'Parameters__anthropic-api-key=...' aspire run ...`
  (hyphen in the name). Stop with SIGINT to the `aspire run` process; persistent `sql` keeps
  running.
- Generating models without the UI: Management API `POST /umbraco/management/api/v1/models-builder/build`
  after the backoffice auth-code + PKCE flow; the real token is in cookie `__Host-umbAccessToken`,
  sent with the cookie jar plus `Authorization: Bearer`. In the UI: Settings > Models Builder.
- Umbraco.AI facts (section 2 supervisor, from package inspection): `Umbraco.AI` 17.5.2 uses
  Microsoft.Extensions.AI 10.7.0, which reads `OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT`;
  Umbraco.AI does not set `EnableSensitiveData`. AI management routes are
  `/umbraco/ai/management/api/v1/...`, not filtered by `TelemetryPaths`. `$Umbraco:AI:Secrets:...`
  references resolve by default (`AllowedConfigurationKeyPrefixes`).
- Prompt model (Umbraco.AI.Prompt 17.3.0 docs): instructions with `{{propertyAlias}}`,
  `{{entity.name}}`; `IncludeEntityContext`; `OptionCount` 0/1/2+; display as property action or
  TipTap tool; scope allow/deny rules by content type, property alias, property editor UI.
- Clean `article` text properties: `title`, `subtitle`, `metaDescription`; tags `metaKeywords`;
  body block list `contentRows`.

## Process notes

- Block 1 worker ran `dotnet new uninstall Umbraco.Templates` (outside its brief); reinstall with
  `dotnet new install Umbraco.Templates@17.7.1` if needed.
- Uncommitted planning files from before apply: `PLANNING.md` (entry 3c), ADR-0030, ADR-0031,
  `openspec/changes/add-ai-demo/`.
