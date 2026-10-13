## 1. Solution and service defaults

Owner: worker-backend; 1.1 by the Architect. Shared files assigned: `SuperPower.slnx`,
`Directory.*.props`, the site's `appsettings*.json`. Depends on: none.

- [x] 1.1 (Architect) Update the Aspire CLI to 13.6.0 (ADR-0001); verify `aspire --version` prints 13.6.x
- [x] 1.2 Restructure per ADR-0002: move `SuperPower.slnx` and `Directory.Packages.props` to the repo root, remove `CentralPackageFloatingVersionsEnabled` from it (ADR-0001: no floating versions), add `Directory.Build.props`, create `src/SuperPower.AppHost` (aspire-apphost) and `src/SuperPower.ServiceDefaults` (aspire-servicedefaults), reference ServiceDefaults from the site, call `AddServiceDefaults()` after `CreateUmbracoBuilder()` and `MapDefaultEndpoints()`; verify `dotnet build` at the root succeeds, no package version is a floating range, `aspire run` shows the site resource running, and `/health` returns 200 without breaking Umbraco routes (front end and `/umbraco` load)
- [x] 1.3 Spike ADR-0007: register the Serilog forwarding sink in ServiceDefaults, remove the `Async` wrapper from the site's Development Serilog config; verify Umbraco start-up log entries appear in the dashboard's structured logs with named properties as fields and a request's logs linked to its trace. If it fails, switch to Serilog.Sinks.OpenTelemetry, record the outcome in ADR-0007, and tell the PO

## 2. Local environment and baseline telemetry (AppHost, ServiceDefaults)

Owner: worker-backend. Depends on: 1.2; 2.4 needs the SQL resource from 2.1.

- [x] 2.1 Add the Azure SQL resource as a persistent container with data volume and database `umbraco`, referenced by the site as `umbracoDbDSN` plus provider name (ADR-0009); verify a fresh volume produces an installed site and the SQL resource shows healthy
- [x] 2.2 Add smtp4dev 3.15.0 and Toxiproxy 2.12.0 containers, populate the `smtp` proxy on Toxiproxy ready, expose port 2525 unproxied, and set the site's SMTP host/port/`SecureSocketOptions=None` from the AppHost (ADR-0010); verify an email sent from the site (Umbraco backoffice "send test email" in Settings > Health check, or a temporary test call) arrives in smtp4dev's inbox via the dashboard link
- [x] 2.3 Add Slow, Break and Heal HTTP commands on the Toxiproxy resource; verify Slow measurably delays a test email, Break makes it fail quickly, Heal restores delivery, and the proxy starts healed
- [x] 2.4 Add SqlClient instrumentation, `SuperPower.Forms` source/meter registration, request filters for health and static paths, and the Azure Monitor exporter switch (ADR-0008); verify with the SQL resource from 2.1 that a Home page trace contains database spans, and record how many parentless background database spans appear per minute; if they crowd the trace list, filter them and verify the list shows page traces only
- [ ] 2.5 Add the OpenTelemetry Collector (`CommunityToolkit.Aspire.Hosting.OpenTelemetryCollector` 13.4.0, `WithAppForwarding()`) fanning out to the dashboard and to `grafana/otel-lgtm:0.36.0` (Grafana on fixed host port 3000), and the `grafana/mcp-grafana:2.0.2` container on fixed host port 8091 with `--disable-write --enabled-tools prometheus,loki,tempo,datasource --allowed-hosts localhost:8091,127.0.0.1:8091` (ADR-0017); verify the dashboard still shows the site's logs, traces and metrics as in 2.4, Grafana Explore at `http://localhost:3000` shows a site trace, a site log and `http.server.request.duration`, and the MCP endpoint answers `initialize` and `tools/list` with 22 tools, none writable

## 3. Site forms backend (`src/SuperPower.Web` C# code, `tests/SuperPower.Web.Tests`)

Owner: worker-backend. Depends on: 1.2; 3.2 needs the SQL resource from 2.1. 3.3 is the model
contract for 4.1 and 3.5. Replaces the forms package plan (ADR-0016); the removed package work
is recorded in `verification.md` and git history.

- [ ] 3.1 Delete `src/SuperPower.Forms` and `tests/SuperPower.Forms.Tests` (projects, solution entries, site project reference, composer and migration registrations); add an `ActivitySource` and `Meter` named `Acme.Forms` to the site and register them in ServiceDefaults in place of `SuperPower.Forms` (ADR-0016); verify root `dotnet build` and `dotnet test` succeed and a fresh SQLite start runs no package migration and creates no package types
- [ ] 3.2 Create the site migration plan "Acme.Site" with step `acmeFormSubmission` (columns as the package table: Id, FormKey indexed, PageKey, SubmittedUtc, FieldsJson NVARCHAR(MAX)), run by `Upgrader` on `UmbracoApplicationStartingNotification` with a log line when the plan is skipped; NPoco repository using `Sql<ISqlContext>` builders, reusing the package's storage and JSON code (ADR-0016); verify on a fresh SQLite database and a fresh SQL volume that the table exists after first boot, a second start changes nothing, and a submission round-trips with byte-identical `FieldsJson` on both
- [ ] 3.3 Implement `ContactForm`, `ServiceEnquiryForm` and `EventBookingForm` with DataAnnotations (`[Required]`, `[EmailAddress]`, `[Display]` labels; no other format rules) and the field-values-to-JSON mapping (ADR-0016); verify with the unit tests in 3.6
- [ ] 3.4 Read one stored `startDate` value (Date Time with time zone) to confirm its shape, then implement the past-event rule with `TimeProvider` (ADR-0014): an event is past when `startDate`, as an absolute instant, is at or before UTC now; expose it as a site service used by the views (ended message, no form) and the controller (reject posts); verify with the unit tests in 3.6 and record the stored value shape in this task
- [ ] 3.5 Implement the `AcmeFormsController` SurfaceController: one action per form with `[ValidateAntiForgeryToken]`; an Event booking post for a past event is rejected and stores nothing; validation failure re-displays via `CurrentUmbracoPage()` with entered values and errors; success stores one row and redirects (TempData + post/redirect/get) (ADR-0016); verify a missing token is rejected and stores nothing, an invalid post re-renders with values and errors and stores nothing, a post to a past event stores nothing, and a valid post stores once and a refresh stores nothing more
- [ ] 3.6 Create the xUnit v3 project `tests/SuperPower.Web.Tests` in the solution (ADR-0012) with tests for each form's validation rules, posted-values-to-JSON mapping, and the past/upcoming boundary with a fake `TimeProvider`; verify all pass with `dotnet test` at the root

## 4. Form partials (`src/SuperPower.Web/Views/`)

Owner: worker-frontend. Depends on: 3.3.

- [ ] 4.1 Implement one partial view per form from the 3.3 models: tag helpers (`asp-for`), labels from `[Display]`, required markers, `novalidate`, antiforgery token, field errors and posted values re-displayed on failure, success message "Thank you, we'll be in touch." after redirect (ADR-0016); verify the rendered HTML of each form contains every field with its label, exactly one `__RequestVerificationToken` input, and no `required`/`pattern` attributes or scripts

## 5. Site logic (`src/SuperPower.Web` C# code)

Owner: worker-backend. Depends on: 3.4; a manual check needs content from 6.2 and 6.3.

- [ ] 5.1 Expose an upcoming-events query service built on the 3.4 rule: events with a future `startDate` ordered by start date, used by the Home and Events views; verify ordering and the past/upcoming boundary with tests in `tests/SuperPower.Web.Tests`

## 6. Site content (PO authors in the backoffice; uSync exports)

Owner: PO. Depends on: 3.1 (package removal), 1.2. Steps: `po-backoffice-steps.md`.

- [ ] 6.1 (PO) After 3.1: delete the local SQLite database and `umbraco/Data/TEMP/`, remove the SQL data volume, start the site standalone; verify a fresh install with the existing `sample-site` content imported and no package element types or form document type (design.md Risks: leftover package types)
- [ ] 6.2 (PO) Document types and templates per ADR-0014 and the design's site structure: a Date Time with time zone data type limited to Europe/London; `event` (composition `compPage` + mandatory `startDate`, template Event), `events` (template Events, allowed child `event`), `services` (template Services, allowed child `service`), `service` (template Service), `contact` (template Contact); allow `events`, `services` and `contact` under `home`; remove the `form` picker property from `compPage` and the image block from its Block List; delete the empty media folder (spec `agency-site` "No media library dependency"); verify the uSync files under `src/SuperPower.Web/uSync/v17/` reflect all of it and saving an event without a start date is refused
- [ ] 6.3 (PO) Create and publish the content per spec `agency-site`: Home (introduction), About us, Contact us as `contact`; Events with at least three `event` children, e.g. "How to get the most out of your CMS", every `startDate` after 16 Oct 2026 (Europe/London); Services with Umbraco upgrade, SEO audit and AdWords audit, each with a short description; verify uSync content files exist and are committed and no form or forms-folder nodes exist

## 7. Site views (`src/SuperPower.Web/Views/`, `src/SuperPower.Web/wwwroot/`)

Owner: worker-frontend. Depends on: 6.2 (document types, templates), 4.1, 5.1.

- [ ] 7.1 Build the shared layout, navigation (Home, About us, Events, Services, Contact us), views for Home (introduction, upcoming events), Page, Events (upcoming events by date with title, date and time), Event (details; Event booking partial, or "This event has ended" and no form when past), Services, Service (description; Service enquiry partial), Contact (Contact partial), and one stylesheet with no external resources or scripts (spec `agency-site`); verify each page renders with the navigation, the browser network panel shows requests to the site only, and an event whose start is moved into the past locally (not committed) shows "This event has ended" and no form

## 8. Agent access

Owner: Architect (shared files `.mcp.json`, `.claude/settings.json`). Depends on: 2.5.

- [ ] 8.1 Commit `.mcp.json` with the single `grafana` server (streamable HTTP, `http://localhost:8091/mcp`) and `.claude/settings.json` pre-approving the Grafana MCP tools (ADR-0017); verify in Claude Code with the AppHost running that the agent retrieves a Home page trace with spans, that trace's logs, and a request-duration metric, all without approval prompts
- [ ] 8.2 Verify the agent's tool surface: `tools/list` shows only the 22 read-only telemetry tools (no write tools, no resource state or commands, no database tool; spec `agent-telemetry-access`), and no other MCP server is configured in the repo

## 9. Documentation and integration checks

Owner: Architect; README and `tests/smoke/` assigned to a worker as shared files. Depends on:
sections 1-8.

- [ ] 9.1 Write the attendee README: prerequisites (.NET 10 SDK, container runtime, Aspire CLI 13.6+), image pre-pull step (~4.8 GB first pull, LGTM alone 2.6 GB; ADR-0017), full-environment run, standalone SQLite run (no DB spans on SQLite), resetting the database and persistent containers (including `umbraco/Data/TEMP`), log-level configuration (Serilog levels only, ADR-0007), Grafana at `http://localhost:3000` with its default credentials, connecting Claude Code (Grafana MCP); verify by following it on a fresh clone
- [ ] 9.2 Verify first-boot seeding: remove the SQL data volume and start the AppHost, then delete the SQLite file and start the site standalone; on both, all pages, events, services and the three forms are published and no content is duplicated after a second start
- [ ] 9.3 Cross-platform check (spec `local-environment` "Cross-platform"): on Windows and macOS, start the AppHost and the standalone site from a fresh clone with no OS-specific changes; on Windows also verify the Collector, LGTM and Grafana MCP path (design.md Risks: the spike ran on macOS); on Linux, start the standalone site in a .NET 10 SDK container; verify all start and serve the Home page, and report any OS not checked to the PO
- [ ] 9.4 End-to-end smoke check, scripted in `tests/smoke/`, on the AppHost: one valid submission of each form stored with form, page key, UTC timestamp and field JSON; an Event booking post to a past event rejected, storing nothing; an invalid submission re-rendered, storing nothing; Home page trace with database spans and linked logs; Slow/Break/Heal working; verify the script passes and record results in the change before cutting `demo/act-1-start`
- [ ] 9.5 Update `PLANNING.md` (resolved spikes, Architect proposals now ADRs) and verify no demo answers were added to it
