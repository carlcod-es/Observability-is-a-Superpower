# AI demo

Standalone Umbraco site (Clean starter kit) with Umbraco.AI prompts calling Anthropic, run under its own Aspire AppHost so each AI call shows up in the Aspire dashboard. Optional segment after act 2 of the talk. Decisions: ADR-0030, ADR-0031. Plan: `openspec/changes/add-ai-demo/`.

Draft: sections marked "to be confirmed" are not yet verified (`add-ai-demo` sections 3 and 4). Presenter notes: `demo-ai/presenter-notes.md` (to come, `add-ai-demo` task 5.2).

## Prerequisites

- .NET 10 SDK.
- Docker (SQL Server image is amd64, emulated on Apple Silicon). Podman: replace `docker` with `podman`.
- Aspire CLI 13.6.1, same as the main demo (root `README.md`, Prerequisites).
- An Anthropic API key. Presenter-owned; calls cost money.
- Network access to `api.anthropic.com`.

Runs instead of the main demo, never at the same time (ADR-0030 decision 5). Stop the main demo's `sql` from its Aspire dashboard and stop the main AppHost first. (Ports differ, so both can run side by side for testing.)

## Pre-pull

Same image as the main demo:

```sh
docker pull mcr.microsoft.com/mssql/server:2022-latest
```

## Start

From the repo root:

```sh
aspire run --apphost demo-ai/src/SuperPower.AiDemo.AppHost/SuperPower.AiDemo.AppHost.csproj
```

The command prints a login link (with token) for the Aspire dashboard.

- Aspire dashboard: https://localhost:16347
- Site: https://localhost:44383
- Backoffice: https://localhost:44383/umbraco, login `admin@example.com` / `1234567890` (template demo admin, local only)
- Resources: `sql` (persistent container, data volume), `umbraco` (database), `site`; parameter `anthropic-api-key`.

First start on an empty volume: unattended Umbraco install, Clean starter kit content (home, about, features, blog with sample articles), Umbraco.AI and Umbraco.AI.Prompt migrations. A restart keeps the data.

## Provider key

AppHost secret parameter `anthropic-api-key`, no default (ADR-0031 decision 3). If it is not set, the dashboard shows a banner for the unresolved parameter: Enter values, paste the key, tick "Save to user secrets", save. The site then starts.

```sh
dotnet user-secrets list --project demo-ai/src/SuperPower.AiDemo.AppHost
dotnet user-secrets remove "Parameters:anthropic-api-key" --project demo-ai/src/SuperPower.AiDemo.AppHost
```

`list` shows `Parameters:anthropic-api-key`; `remove` deletes it.

The AppHost passes the key to the site as `Umbraco__AI__Secrets__AnthropicApiKey`. The backoffice connection holds only the reference `$Umbraco:AI:Secrets:AnthropicApiKey`, so the key is in neither the repo nor the database.

## Backoffice setup

Once; kept in the SQL data volume (lost on [reset](#reset)). Labels follow the Umbraco.AI 17.x package docs and may differ slightly on screen.

1. Check the AI section is visible (the Umbraco.AI install migration assigns it to Administrators). If not: Users > User Groups > Administrators, add the AI section, save, refresh.
2. AI > Connections > Create: provider Anthropic, alias `anthropic`, API key `$Umbraco:AI:Secrets:AnthropicApiKey` (literally, not the key).
3. AI > Profiles > Create: chat profile on connection `anthropic`. Model: the fastest in the list (e.g. the newest Claude Haiku). TBD (`add-ai-demo` task 3.2).
4. AI > Prompts > Create (suggested; final wording TBD, `add-ai-demo` task 3.2):
   - Name: `Write meta description`
   - Profile: from step 3
   - Instructions: `Write an SEO meta description of at most 155 characters for this article. Return only the text.`
   - Include entity context: on (whole article sent as context)
   - Option count: 1
   - Display as: property action
   - Scope: allow rule, content type `article`, property `metaDescription`
5. Test: Content > Blog > an article (e.g. "Join the Umbraco Community on Mastodon") > Meta Description property (SEO tab) > property action > run, insert, save.

Other prompt ideas for Clean `article` (text properties `title`, `subtitle`, `metaDescription`; tags `metaKeywords`; body in block list `contentRows`). Instructions support `{{propertyAlias}}` and `{{entity.name}}`. Option count: 0 = informational only, 1 = insert, 2+ = choices.

| Name | Property | Instructions | Options |
|---|---|---|---|
| Suggest subtitle | `subtitle` | `Suggest a one-line subtitle for this article.` | 3 |
| Punchier title | `title` | `Rewrite the title {{title}} to be punchier, max 8 words.` | 1 |
| Summarise | any | `Summarise this article in one sentence.` | 0 |

## Telemetry

To be confirmed (`add-ai-demo` section 4). ActivitySource and Meter `Umbraco.AI` are registered in the demo-ai ServiceDefaults (ADR-0031).

- Span `gen_ai.chat {model}` with `umbraco.ai.profile.*`, `umbraco.ai.feature.*`, `umbraco.ai.entity.*`, `umbraco.ai.user.id`; child HTTP span to `api.anthropic.com`.
- Metrics `gen_ai.client.token.usage`, `gen_ai.client.operation.duration`.
- Backoffice requests are traced too; filter traces by `gen_ai.chat`.

## Capture switch

`demo-ai/src/SuperPower.AiDemo.AppHost/AppHost.cs` line 15, marked `// Demo Code : AI` (ADR-0031 decision 4):

```csharp
.WithEnvironment("OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT", "false")
```

- `"false"` (default): no prompt or response text in telemetry.
- `"true"`: prompt and response text on the AI span (possibly logs too). Restart the AppHost after changing.
- Behaviour to be confirmed (`add-ai-demo` task 4.3).
- Always set back to `"false"`. This demo has no log redaction (unlike the main demo, ADR-0028).

## Models

ModelsBuilder mode is `SourceCodeManual` (Development) / `Nothing` (base). Models are committed in `demo-ai/src/SuperPower.AiDemo.Web/Models/`, namespace `Umbraco.Cms.Web.Common.PublishedModels` (Clean's views use it). Without them the Clean pages fail with a Razor compilation error.

After a document type change: Settings > Models Builder > Generate models, then rebuild.

Clean's build targets copy its views and assets into the site on every build, overwriting local edits.

## Stop

Ctrl+C stops the AppHost; the `sql` container is persistent and keeps running. Stop it first from the dashboard (`sql` > Stop), or:

```sh
docker ps                  # container name starts with sql-
docker stop <name>
```

The main demo's container name also starts with `sql-`; check the volume with `docker inspect <name>`.

## Reset

Loses content and AI setup. Stop as above, then:

```sh
docker ps -a
docker rm <sql container>
docker volume ls           # superpower.aidemo.apphost-<hash>-sql-data
docker volume rm <volume>
```

Then redo the [key](#provider-key) (if removed) and the [backoffice setup](#backoffice-setup).

## Without Aspire

```sh
dotnet run --project demo-ai/src/SuperPower.AiDemo.Web
```

SQLite, no telemetry. AI calls need `Umbraco:AI:Secrets:AnthropicApiKey` set on the Web project (e.g. `dotnet user-secrets set "Umbraco:AI:Secrets:AnthropicApiKey" "<key>" --project demo-ai/src/SuperPower.AiDemo.Web`).
