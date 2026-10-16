## Why

Teams are adding AI features to their CMS, and AI calls are a new black box: which model ran, for
which content and user, how long it took, how many tokens it used, and whether prompt text leaks
into telemetry. An optional segment after act 2 shows that the same local observability setup
answers those questions for Umbraco's own AI features, with no custom code.

## What Changes

- A second, standalone demo next to the main demo: an Umbraco site with ready-made sample content,
  started from the same kind of local orchestration as the main demo, with telemetry in the same
  kind of local dashboard.
- Umbraco's AI features installed and connected to Anthropic; an editor runs a prompt on sample
  content in the backoffice.
- Each AI call shows up in local telemetry: a trace span (model, Umbraco profile, prompt, document,
  user), the outbound call to the provider beneath it, and token usage and duration metrics.
- Prompt and response text is left out of telemetry by default. One marked switch turns capture on
  for the demo, to show what would leak and why it stays off.
- The AI provider key never goes in the repo.
- Runs instead of the main demo, never at the same time: after act 2 the presenter stops the main
  demo and starts the AI demo (PO 2026-10-16).
- Everything that can be done before the talk is done before it: install, sample content, provider
  key, backoffice AI setup, a test run. On stage the presenter only switches over, runs the prompt,
  shows the telemetry and flips the capture switch live.
- Presenter notes in the AI demo folder: preparation checklist, switch-over from the main demo,
  stage steps, fallback, reset.
- Out of scope: Grafana, the collector and the production stage; front-end AI features; Copilot
  and agents; agent (MCP) use of this demo's telemetry; the main demo's forms and log redaction;
  changes to the main demo.

## Capabilities

### New Capabilities
- `ai-demo`: standalone AI demo site, its local environment, AI prompt setup, AI telemetry and
  the prompt-content capture switch.

### Modified Capabilities
None.

## Impact

- New top-level folder with its own solution; the main solution, site and specs are unchanged.
- New dependencies, approved by the PO 2026-10-16: Umbraco AI packages (core, Anthropic
  provider, prompts) and the Clean starter kit.
- External service: Anthropic API, called live from the backoffice. Needs network access and a
  presenter-owned API key on the demo machine.
- Backoffice setup (AI section access, connection, profile, prompt) is done once before the talk
  and kept in the database volume.
- README: how to run the AI demo. Presenter notes in the AI demo folder.
- Talk flow: the main demo is stopped after act 2 when this segment runs.
