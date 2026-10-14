## Context

See proposal.md for motivation. Built so far (sections 1-2 and parts of 3 in tasks.md, before
the 2026-10-10/13 PO decisions): AppHost with SQL Server (Azure SQL as container), smtp4dev
behind Toxiproxy with Slow/Break/Heal commands, ServiceDefaults with the Serilog forwarding
sink, SqlClient instrumentation and exporter switch, and a partial forms package
`src/SuperPower.Forms` that is now removed (ADR-0016). Site content on branch `sample-site`
(uSync): document types `home` and `page` (composition `compPage`), pages Home, About us,
Contact us.

Fixed by the talk (ADR-0001): .NET 10, Umbraco 17 LTS, Aspire, OpenTelemetry, Claude Code with
MCP. Gaps these constraints imply, resolved here: Umbraco's logging bypasses the OpenTelemetry
logging provider (ADR-0007); Umbraco emits no spans itself (ADR-0008); the Aspire MCP has no
metrics tool, so the agent queries Grafana LGTM through the Grafana MCP instead (ADR-0017).

```
                       +---------------- Aspire AppHost ----------------+
 browser --> site ---->| SuperPower.Web (+ ServiceDefaults)             |
                       |   |  umbracoDbDSN         | SMTP :2525         |
                       |   v                       v                    |
                       | sql (Azure SQL as     toxiproxy --> smtp4dev   |
                       |      container)       (Slow/Break/Heal cmds)   |
                       +-----------------------------------------------+
                              | OTLP (logs, traces, metrics)
                              v
                        collector --+--> Aspire dashboard   (presenter)
                                    +--> lgtm (Grafana :3000)
                                             ^
                                   grafana-mcp :8091 <-- Claude Code
                                   (22 read-only tools)
```

## Goals / Non-Goals

**Goals:**
- Act 1 state: one command starts everything; a page request shows one trace with database
  spans and linked structured logs; mail-path commands work; the agent reads traces, logs and
  metrics through the Grafana MCP.
- Same instrumentation code for local and prod exporters.
- Fresh database populates itself on SQL Server and SQLite.

**Non-Goals:**
- The forms' own spans and metrics, MailKit instrumentation, emails (change `add-form-emails`).
- Azure deployment, App Insights verification, prod agent access (change
  `add-production-environment`).
- Any booking workflow: an Event booking is an ordinary submission (PO 2026-10-13); no places,
  capacity, status or owner dashboard.
- Demo branch creation (separate planning topic).
- The demo Grafana dashboard, its provisioning and demo traffic (change `add-demo-dashboard`).

## Bar and test strategy

Bar: **prototype** (PO, 2026-10-08). Demo code shown on stage and published for attendees;
correct on the specified scenarios, readable, not hardened for production load or long-term
maintenance.

Test strategy that follows (ADR-0012):
- Unit tests (xUnit v3, `tests/SuperPower.Web.Tests`) for logic that needs no Umbraco runtime:
  form model validation, field-values-to-JSON mapping, the past-event rule and upcoming-events
  ordering with a fake `TimeProvider`.
- Scripted smoke checks in `tests/smoke/` for startup, seeding and end-to-end behaviour on the
  AppHost and the standalone SQLite site; run before cutting each demo branch.
- Human observation for dashboard, agent and backoffice behaviour, recorded in the task.
- No coverage target; every spec scenario maps to a unit test, a smoke check or a recorded
  observation in `tasks.md`.
- No Umbraco integration-test harness or Aspire testing in this change.

## Decisions

1. **Versions and freeze** - Aspire 13.6.0, Umbraco 17.7.1, uSync 17.4.3, central pinning,
   freeze from 13 Oct. ADR-0001.
2. **Layout** - solution and central package props at repo root; AppHost, ServiceDefaults and
   Web under `src/`; `SuperPower.Web.Tests` under `tests/`; no forms package. ADR-0002.
3. **Content seeding** - uSync files committed, `ImportOnFirstBoot` on; views as source files.
   Rejected: embedded package.zip migration, content-in-code, committed DB. ADR-0003.
4. **Fixed site forms** - three typed models with DataAnnotations in `SuperPower.Web`, one
   partial per form, one SurfaceController (`AcmeFormsController`, antiforgery, PRG), success
   message "Thank you, we'll be in touch.", past-event rule via `TimeProvider`, one
   `acmeFormSubmission` table from site migration plan "Acme.Site" (NPoco `Sql<ISqlContext>`,
   `NVARCHARMAX` JSON, no client info), telemetry names `Acme.Forms`. The page type decides
   the form. Rejected: generic form engine, separate forms project. ADR-0016.
5. **Logs** - forward Umbraco's Serilog events to ServiceDefaults' logger providers via a DI
   sink; call `AddServiceDefaults()` after `CreateUmbracoBuilder()`; no Async wrapper.
   Fallback: Serilog.Sinks.OpenTelemetry. ADR-0007.
6. **Traces, metrics, exporters** - ASP.NET Core, HttpClient, SqlClient 1.17.0
   instrumentation; health/static requests filtered; OTLP or Azure Monitor chosen by
   environment variables. ADR-0008.
7. **Local database** - Azure SQL resource run as a persistent container with data volume,
   referenced as `umbracoDbDSN`; SQLite for standalone runs. Rejected: plain `AddSqlServer`,
   session lifetime. ADR-0009.
8. **Mail path** - smtp4dev 3.15.0 behind Toxiproxy 2.12.0 (non-proxied fixed port 2525);
   proxy populated on ready; `WithHttpCommand` Slow (latency) / Break (reset_peer) / Heal
   (reset). Rejected: MailPit, .NET Toxiproxy client, timeout toxic. ADR-0010.
9. **Agent access** - OpenTelemetry Collector
   (`CommunityToolkit.Aspire.Hosting.OpenTelemetryCollector` 13.4.0, collector image
   `opentelemetry-collector-contrib:0.162.0`, `WithAppForwarding()`)
   fans out to the dashboard and `grafana/otel-lgtm:0.36.0` (port 3000); `grafana/mcp-grafana`
   2.0.2 (port 8091, `--disable-write --enabled-tools prometheus,loki,tempo,datasource`,
   22 read-only tools) is the agent's only MCP server in `.mcp.json`, pre-approved in
   `.claude/settings.json`; no Aspire MCP, no DB access. Rejected: Prometheus MCP, Aspire MCP
   alone or alongside. ADR-0017 (supersedes ADR-0011).
10. **Testing** - xUnit v3 in `tests/SuperPower.Web.Tests`; scripted smoke checks in
    `tests/smoke/`. See "Bar and test strategy". ADR-0012.
11. **Event content model** - one document type `event` (`compPage` + `startDate`, Date Time
    with time zone, Europe/London only, mandatory); past when `startDate` <= UTC now; no form
    picker, the template decides the form. Rejected: in-person/online types with capacity,
    plain Date Time, form picker. ADR-0014.
12. **Media imagery** (PO 2026-10-14, explore; replaces "no media items") - a page's image is
    its existing `compPage.mainBanner` (MediaPicker3), rendered in the page banner and in the
    page's card on Home and list pages. Media is seeded like content: uSync media items plus
    the stored files under `src/SuperPower.Web/wwwroot/media/`, both committed;
    `ImportOnFirstBoot` covers media items. Imagery is optional everywhere: no image (or
    missing media) renders a plain single-colour block, never a broken image (spec
    `agency-site` "Imagery from the media library"). `/media` and `/favicon.ico` join the
    `TelemetryPaths` exclusions (ADR-0008) so image requests leave no traces or logs.
    Rejected: static-asset images by URL-segment convention (media library is the natural
    Umbraco story and matches the PO's later media plans); mandatory imagery (fallback caps
    the risk of a failed media import on stage).

Site structure (content, via uSync):
```
Home (home)                  template Home      introduction (banner; body when present),
                                                Services and upcoming Events card sections
+-- About us (page)          template Page
+-- Events (events)          template Events    upcoming events by date
|   +-- <event> (event)      template Event     details; Event booking form ("This event has
|                                               ended" + no form when past)
+-- Services (services)      template Services  service list
|   +-- <service> (service)  template Service   description; Service enquiry form
+-- Contact us (contact)     template Contact   Contact form
```
Pages share a layout with navigation, a breadcrumb trail from the content tree, and one small
stylesheet; imagery via `mainBanner` from the media library, optional with a single-colour
fallback (Decision 12); no form content nodes. Template basis: the `prototype/` Eleventy site
on `main` (ADR-0018/0019) — its layouts, form markup, `site.css` and self-hosted fonts; its
Blog section, newsletter box and thank-you page are prototype-only. Banner heading maps to
`pageTitle` (fallback: node name), banner text and card summaries to `summary`, page bodies to
`mainContent`. Home mirrors the prototype home — banner introduction, `mainContent` body when
present, then Services and upcoming Events card sections — with the Blog section dropped.
(Mapping, breadcrumbs and the Home shape: PO 2026-10-14, explore.)

## Risks / Trade-offs

- [uSync first boot skipped because a database booted once with the flag off] → flag set
  before seeding work; README reset steps; smoke check on a fresh volume.
- [Persistent SQL container survives between runs, hiding first-boot problems] → smoke check
  on a removed volume before cutting any demo branch.
- [Port 2525, 3000, 8091 or the Aspire dashboard port in use on the stage machine] → check
  during rehearsal; 3000 commonly clashes with Node dev servers.
- [Date Time with time zone stored value shape unverified for 17.7.1] → read one stored value
  before writing the past-event check (task 3.4).
- [LGTM stack unverified on Windows for this AppHost; `spike-metrics-mcp` ran on macOS,
  `spike-grafana-dashboard` ran the spike stack on Windows 2026-10-14 without issues] → verify
  during apply (task 9.3); Aspire CLI must match the AppHost SDK (a mismatched CLI kills the
  AppHost); Docker Desktop in Linux-container mode, trusted dev certificate.
- [~4.8 GB first image pull (LGTM alone 2.6 GB) stalls attendees and rehearsals] → pre-pull
  step in the README; presenter pulls before the talk.
- [Leftover package element types and form document type in existing local databases] → reset
  SQLite and the SQL volume after the package removal (task 6.1).
- [uSync first-boot media import unverified] → the colour fallback keeps every page complete
  without media; task 9.2 verifies media items import on both databases.
- [ServiceDefaults' HttpClient resilience handler alters Umbraco's own HTTP calls] → keep
  defaults; revisit only if a failure appears.

## Migration Plan

Greenfield for what remains. Rollback is resetting the branch. Removing the forms package
(task 3.1) plus the database resets (task 6.1) discard the package's schema; the submissions
table is recreated by the site's "Acme.Site" migration plan.

## Open Questions

- Default latency for Slow (value tuned during rehearsal; set in the AppHost).
