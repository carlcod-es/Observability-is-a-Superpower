# ADR-0001: Platform and versions

## Status
Accepted (2026-10-07)

## Context
The talk fixes the platform: .NET 10, Umbraco 17 LTS, .NET Aspire with AppHost and
ServiceDefaults, OpenTelemetry, Claude Code with MCP. These are prerequisites, not choices.
What remains is pinning versions and closing gaps the constraints imply.

Observed: the empty site uses Umbraco.Cms 17.7.1 and uSync 17.4.3 with central package
management. The installed Aspire CLI is 9.3.1-preview; current stable Aspire is 13.6.0
(2026-09-29). Aspire 13.x moved the MCP server from the dashboard into the CLI.

## Decision
- .NET SDK 10.0.x; target `net10.0` everywhere.
- Umbraco.Cms 17.7.1, uSync 17.4.3 (already pinned).
- Aspire 13.6.0 (`Aspire.AppHost.Sdk`, hosting packages, CLI). Update the CLI before any
  other work (`aspire update --self`, or reinstall from https://aspire.dev/install.sh).
- Pin all versions centrally; no floating versions. Freeze package updates from 13 Oct 2026
  until after the talk.

## Alternatives
- Platform: none; fixed by the talk.
- Floating or per-project versions: less maintenance, but an unplanned update can break a
  rehearsed demo; rejected.
- Stay on Aspire CLI 9.3.1-preview: predates the CLI-hosted MCP server; rejected.

## Consequences
- The Aspire MCP setup follows the 13.6 CLI model (`aspire agent mcp`), not the older dashboard MCP.
- Version freeze avoids surprise breaks during rehearsal; security updates are applied only
  if critical.
- Attendees need the Aspire CLI 13.6+ to use the agent setup.
