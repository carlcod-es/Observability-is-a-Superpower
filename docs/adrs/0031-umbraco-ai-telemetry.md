# ADR-0031: Umbraco.AI with Anthropic, prompt telemetry and content capture switch

## Status
Accepted (PO 2026-10-16)

## Context
Change `add-ai-demo`, spec `ai-demo`. PO 2026-10-16: Anthropic as provider, AI calls triggered by
backoffice prompts, Aspire dashboard only, prompt/response text capture available behind a demo
toggle. Umbraco.AI (docs `ai-in-umbraco/17.latest`, "Observability") wraps provider clients in
Microsoft.Extensions.AI's OpenTelemetry chat client under source and meter `Umbraco.AI`
(`AITelemetry.SourceName`): spans `gen_ai.chat {model}` with `umbraco.ai.profile.*`,
`umbraco.ai.entity.*`, `umbraco.ai.feature.*`, `umbraco.ai.user.id`, `umbraco.ai.audit.id`;
metrics `gen_ai.client.token.usage`, `gen_ai.client.operation.duration`. Microsoft.Extensions.AI
10.7 leaves message content out of spans unless `EnableSensitiveData` is set; its default reads
the environment variable `OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT`.

Umbraco 17 compatible versions (NuGet, 2026-10-16; 18.x targets Umbraco 18):
`Umbraco.AI` 17.5.2, `Umbraco.AI.Anthropic` 17.1.2, `Umbraco.AI.Prompt` 17.3.0, all requiring
Umbraco 17.5 or later.

## Decision
1. **Packages.** `Umbraco.AI` 17.5.2, `Umbraco.AI.Anthropic` 17.1.2, `Umbraco.AI.Prompt` 17.3.0
   in the AI demo site only.
2. **Telemetry registration.** ServiceDefaults adds `AddSource("Umbraco.AI")` and
   `AddMeter("Umbraco.AI")` as literals (ServiceDefaults does not reference Umbraco.AI, same
   pattern as `Acme.Forms` in the main demo). The existing HttpClient instrumentation provides
   the child span to the provider.
3. **Provider key.** AppHost secret parameter `anthropic-api-key` (no default; Aspire asks for it
   in the dashboard and can save it to AppHost user secrets) passed to the site as
   `Umbraco__AI__Secrets__AnthropicApiKey`. The backoffice connection's key field holds the
   reference `$Umbraco:AI:Secrets:AnthropicApiKey`.
4. **Content capture switch.** AppHost sets `OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT`
   to `false` on the site on one line marked as demo code; the presenter changes it to `true`
   and restarts to show prompt and response text on the span.
5. **Backoffice setup by hand, once.** AI section access for Administrators, the Anthropic
   connection, one chat profile and one prompt, done before the talk and kept in the SQL data
   volume. Steps in `demo-ai/README.md`.

## Alternatives
- **OpenAI provider**: the docs' main example; PO chose Anthropic.
- **Copilot agent or a front-end AI feature**: more visual or closer to public-site use, but more
  packages or custom code; PO chose backoffice prompts.
- **Content capture always on, or redacted**: always-on contradicts the talk's privacy message;
  span redaction would be new code. A default-off switch makes the point with no code.
- **Switch in code (`EnableSensitiveData`) instead of the environment variable**: needs a hook
  into Umbraco.AI's client pipeline; the environment variable is honoured by
  Microsoft.Extensions.AI directly.
- **Seeding AI setup in code or uSync**: no supported export path found; one-off manual setup is
  simpler for a single segment.

## Consequences
- Live dependency on the Anthropic API and venue network; a presenter-owned key with spend on it.
- When capture is on, prompt text includes whatever content the editor sends; that is the point
  of the demo and why it stays off by default. The log redaction processor (ADR-0028) does not
  cover spans.
- If Umbraco.AI sets `EnableSensitiveData` explicitly, the environment variable has no effect;
  checked during apply (`add-ai-demo` task 4.3) before the switch is documented.
- AI setup is lost if the SQL data volume is deleted; redo from the README.
