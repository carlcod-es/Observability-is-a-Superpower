## Context

See proposal.md for motivation. Current state: `src/SuperPower.Web` is an unmodified Umbraco
17.7.1 site (SQLite, unattended install, uSync 17.4.3 referenced, database git-ignored) with
`src/SuperPower.slnx`. No AppHost, package or tests exist. Installed Aspire CLI is
9.3.1-preview; current is 13.6.0.

Fixed by the talk (ADR-0001): .NET 10, Umbraco 17 LTS, Aspire, OpenTelemetry, Claude Code
with MCP, Azure for prod. Gaps these constraints imply, resolved here: Umbraco's logging
bypasses the OpenTelemetry logging provider (ADR-0007); Umbraco emits no spans itself
(ADR-0008); Aspire's MCP moved to the CLI and has no metrics tool (ADR-0011).

```
                       +---------------- Aspire AppHost ----------------+
 browser --> site ---->| SuperPower.Web (+ ServiceDefaults, + Forms pkg) |
                       |   |  umbracoDbDSN         | SMTP :2525          |
                       |   v                       v                     |
                       | sql (Azure SQL as     toxiproxy --> smtp4dev    |
                       |      container)       (Slow/Break/Heal cmds)    |
                       +------------------------------------------------+
                              | OTLP (logs, traces, metrics)
                              v
                       Aspire dashboard  <-- aspire agent mcp (stdio) <-- Claude Code
```

## Goals / Non-Goals

**Goals:**
- Act 1 state: one command starts everything; a page request shows one trace with database
  spans and linked structured logs; mail-path commands work; the agent reads traces and logs.
- Same instrumentation code for local and prod exporters.
- Fresh database populates itself on SQL Server and SQLite.

**Non-Goals:**
- Package spans and metrics, MailKit instrumentation, emails (change `add-form-emails`).
- Azure deployment, App Insights verification, prod agent access (change
  `add-production-environment`).
- Demo branch creation (separate planning topic).

## Decisions

1. **Versions and freeze** - Aspire 13.6.0, Umbraco 17.7.1, uSync 17.4.3, central pinning,
   freeze from 13 Oct. ADR-0001.
2. **Layout** - solution and central package props at repo root; AppHost, ServiceDefaults,
   Web, Forms under `src/`; tests under `tests/`. ADR-0002.
3. **Content seeding** - uSync files committed, `ImportOnFirstBoot` on; views as source files.
   Rejected: embedded package.zip migration, content-in-code, committed DB. ADR-0003.
4. **Package schema and storage** - one idempotent async package migration plan creating
   element types, a Block List data type and a form document type with fixed keys, plus the
   submissions table (NPoco, `NVARCHARMAX` JSON, no client info). Site content owns form nodes;
   pages pick a form. Rejected: EF Core, custom editor UI, per-page block forms. ADR-0004.
5. **Rendering and validation** - ViewComponent + SurfaceController, antiforgery,
   server-side-only validation with `novalidate`, regex timeout, post/redirect/get. Rejected:
   client validation, JS fetch endpoint. ADR-0005.
6. **Package telemetry dependency** - BCL `ActivitySource`/`Meter`/`ILogger` named
   `SuperPower.Forms`, host opts in. Rejected: OTel API package reference. ADR-0006.
7. **Logs** - forward Umbraco's Serilog events to ServiceDefaults' logger providers via a DI
   sink; call `AddServiceDefaults()` after `CreateUmbracoBuilder()`; no Async wrapper.
   Fallback: Serilog.Sinks.OpenTelemetry. ADR-0007.
8. **Traces, metrics, exporters** - ASP.NET Core, HttpClient, SqlClient 1.19.0
   instrumentation; health/static requests filtered; OTLP or Azure Monitor chosen by
   environment variables. ADR-0008.
9. **Local database** - Azure SQL resource run as a persistent container with data volume,
   referenced as `umbracoDbDSN`; SQLite for standalone runs. Rejected: plain `AddSqlServer`,
   session lifetime. ADR-0009.
10. **Mail path** - smtp4dev 3.15.0 behind Toxiproxy 2.12.0 (non-proxied fixed port 2525);
    proxy populated on ready; `WithHttpCommand` Slow (latency) / Break (reset_peer) / Heal
    (reset). Rejected: MailPit, .NET Toxiproxy client, timeout toxic. ADR-0010.
11. **Agent access** - committed `.mcp.json` (`aspire agent mcp`), read-only tools allowed,
    `execute_resource_command` prompts, no DB access, no CLI skills. ADR-0011.
12. **Testing** - xUnit v3 for package logic; scripted smoke checks for startup and
    end-to-end. ADR-0012.

Site structure (content, via uSync):
```
Home (home)            template Home      introduction, upcoming classes
+-- Classes (classes)  template Classes   static class list + form picker -> Book a class
+-- Contact (contact)  template Contact   form picker -> Contact
Forms (formsFolder, root, no template)
+-- Contact            (superPowerForm)
+-- Book a class       (superPowerForm)
```
Pages share a layout with navigation and one small stylesheet; no media items.

## Risks / Trade-offs

- [Log forwarding sink does not work as inferred] → spike first (task 1.3); fallback to
  Serilog.Sinks.OpenTelemetry for local, revisit prod route in `add-production-environment`.
- [Umbraco background jobs flood traces with database spans] → measure (task 1.4); drop
  parentless database spans.
- [uSync first boot skipped because a database booted once with the flag off] → flag set
  before seeding work; README reset steps; smoke check on a fresh volume.
- [`MapDefaultEndpoints` clashes with Umbraco routing] → verify `/health` in task 1.2; rename
  paths if needed.
- [ServiceDefaults' HttpClient resilience handler alters Umbraco's own HTTP calls] → keep
  defaults; revisit only if a failure appears.
- [Aspire MCP has no metrics] → agent-driven acts rely on traces and logs; metrics shown by the
  presenter.
- [Persistent SQL container survives between runs, hiding first-boot problems] → smoke check
  on a removed volume before cutting any demo branch.
- [Port 2525 or the Aspire dashboard port in use on the stage machine] → check during
  rehearsal.
- [Block List data type configuration shape unverified for 17.7.1] → create it once in the
  backoffice, read the stored configuration, mirror it in the migration (task 3.3).

## Migration Plan

Greenfield. Rollback is resetting the branch. The site's existing local SQLite database is
discarded once `ImportOnFirstBoot` is enabled and content is authored on a fresh database.

## Open Questions

- Default latency for Slow (value tuned during rehearsal; set in the AppHost).
