# ADR-0030: Standalone AI demo solution with Clean starter content

## Status
Accepted (PO 2026-10-16)

## Context
Change `add-ai-demo`, spec `ai-demo`. An optional segment after act 2 shows Umbraco's AI
features in local telemetry. PO 2026-10-16: a separate folder next to the main demo, the same
Aspire setup as the main demo, and the Clean starter kit so the site has content from the start.
The main demo must stay untouched (talk is the same day). The main ServiceDefaults registers
`Acme.Forms` and the log redaction processor (ADR-0028), which this site does not use.

## Decision
1. **Folder and solution.** `demo-ai/` at the repo root with `demo-ai/SuperPower.AiDemo.slnx`
   and projects `demo-ai/src/SuperPower.AiDemo.AppHost`, `.ServiceDefaults`, `.Web`. Own
   `demo-ai/Directory.Build.props` and `demo-ai/Directory.Packages.props`: MSBuild imports only
   the nearest of each, so the main demo's package versions do not apply and are not changed.
2. **Site.** Created with `Umbraco.Templates` 17.7.1 (`dotnet new umbraco`, same CMS version as
   the main demo), plus the `Clean` 7.0.8 package. Clean installs its document types and sample
   content through package migrations on first boot, on SQLite or SQL Server. Unattended install
   with the template's demo administrator in `appsettings.Development.json`, as in the main demo.
3. **AppHost.** Same pattern as the main demo: `AddAzureSqlServer(...).RunAsContainer()` with a
   data volume and persistent lifetime, database `umbraco`, `umbracoDbDSN` and provider name set
   on the site. No smtp4dev, collector, LGTM or Grafana MCP.
4. **ServiceDefaults.** A copy of the main ServiceDefaults without `Acme.Forms`, the redaction
   processor and its key; keeps the Serilog forwarding sink (ADR-0007), static and health path
   exclusions and SQL span filter (ADR-0008).
5. **Runs instead of the main demo** (PO 2026-10-16). The two demos never run at the same time.
   Everything except the switch-over is prepared before the talk (install, content, key, AI
   setup). Switch-over after act 2: stop the main demo's persistent `sql` resource from its
   dashboard, stop the main AppHost, start the AI demo. Ports need not differ from the main demo.
   Steps in `demo-ai/presenter-notes.md`.

## Alternatives
- **Project in the main solution, shared ServiceDefaults**: one build; but changes main demo files
  on talk day and pulls `Acme.Forms` and redaction into a site that has neither.
- **SQLite, no container** (the PO's snippet): faster start, no second SQL Server; rejected
  because the PO asked for the same Aspire setup, and database spans come for free with SQL Server.
- **Hand-built sample content or uSync export**: more work for no demo value; Clean ships content.
- **Root `Directory.Packages.props` entries**: one version list, but edits a shared main-demo file.

## Consequences
- Two independent solutions; versions can drift. Accepted: demo-ai is a one-off segment.
- A second SQL Server container (amd64, emulated on Apple Silicon) and data volume on the machine.
  Only one SQL container runs at a time, because the switch-over stops the main one (its
  persistent lifetime would otherwise keep it running after its AppHost stops).
- Switch-over costs stage time (AppHost start, Umbraco boot); rehearsed and timed.
- Clean brings its own views, styles and document types; nothing in `demo-ai/` is front-end work.
