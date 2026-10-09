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

## 3. Forms package (`src/SuperPower.Forms` except `Views/`, `tests/SuperPower.Forms.Tests`)

Owner: worker-backend. Depends on: 1.2. 3.7 is the view-model contract for 4.1; verifying
3.8's re-render needs 4.1. 3.9 may run before 3.5.

- [x] 3.1 Create the Razor class library, reference it from the site, add the `ActivitySource`/`Meter` named `SuperPower.Forms` with no OpenTelemetry package references (ADR-0006); verify the solution builds and the package has no OpenTelemetry dependency (`dotnet list package --include-transitive` on the project)
- [x] 3.2 Implement the package migration plan steps for the five element types with fixed keys (ADR-0004); verify on a fresh database that the five element types exist with the shared properties and Select has options, and a second start does not duplicate them
- [ ] 3.3 Create the Block List data type and the `superPowerForm` document type (`fields`, `successMessage`) in the migration; first create an equivalent Block List once in the backoffice and mirror its stored configuration; verify editors can add, remove and reorder all five field types on a form node and set its success message
- [x] 3.4 Create the `superPowerFormSubmission` table step and an NPoco repository using `Sql<ISqlContext>` builders (ADR-0004); verify a stored submission round-trips on SQLite and on SQL Server with identical field JSON and no client info
- [x] 3.5 Implement the validator (required, email, pattern with match timeout and configured message, select membership, fixed values) and duplicate-alias detection; verify with the unit tests in 3.10
- [x] 3.6 Define `IFormSubmissionHandler` (ADR-0013): `Prepare` returns closed-with-message or fixed field values for a form and page; `OnStored` receives the stored submission inside the same scope; handlers registered through DI; verify with the unit tests in 3.10
- [x] 3.7 Implement the `SuperPowerForm` ViewComponent class and its view model (ADR-0005, ADR-0013): runs handlers' `Prepare`; exposes the non-fixed fields (label, type, required, options, posted value, error), the closed message, the success message after redirect, and the post target; refuses to render with a log entry on duplicate aliases; verify with the unit tests in 3.10
- [ ] 3.8 Implement the SurfaceController post action with `[ValidateAntiForgeryToken]`: run handlers' `Prepare` before validation (closed rejects with its message, fixed values enforced), re-display on failure with values and errors, store and run `OnStored` in one scope, redirect on success (ADR-0005, ADR-0013); verify a missing token is rejected, an invalid post re-renders with errors and stores nothing, a throwing `OnStored` stores nothing, and a valid post stores once and a refresh stores nothing more
- [x] 3.9 Create the xUnit v3 project `tests/SuperPower.Forms.Tests` in the solution (ADR-0012); verify `dotnet test` runs at the root
- [ ] 3.10 Add tests for each validation rule, the regex timeout, select tampering, fixed-value mismatch, closed-form rejection, duplicate aliases, posted-value to field-JSON mapping, and the view model (fixed fields omitted, closed form exposes only its message); verify all pass

## 4. Forms package view (`src/SuperPower.Forms/Views/`)

Owner: worker-frontend. Depends on: 3.7.

- [ ] 4.1 Implement the ViewComponent's default view from the 3.7 view model (ADR-0005): labels, required markers, `novalidate`, antiforgery token, field errors and posted values, the form's success message, closed form shows its message only; verify the rendered HTML of a test form contains every non-fixed field and no `required`/`pattern` attributes or scripts

## 5. Site logic (`src/SuperPower.Web` C# code, `tests/SuperPower.Web.Tests`)

Owner: worker-backend. Depends on: 3.6 (handler interface), 2.1 (SQL volume for 5.1);
5.2 reads a stored event from 6.2 and 6.3.

- [ ] 5.1 Create the `acmeEventBooking` table with a site `MigrationPlan` run by `Upgrader` on `UmbracoApplicationStartingNotification`, and an NPoco repository using `Sql<ISqlContext>` builders (ADR-0015); verify on a fresh SQLite database and a fresh SQL volume that the table exists after first boot, a second start changes nothing, and a booking round-trips
- [ ] 5.2 Read one stored `startDate` value (Date Time with time zone) to confirm its shape, then implement `EventBookingHandler` with `TimeProvider` (ADR-0013, ADR-0014): applies when the page's document type is `eventInPerson` or `eventOnline`; `Prepare` closes the form with "This event has ended" when `startDate` <= UTC now and fixes Places to 1 on online events; `OnStored` inserts a `Pending` booking with the event key, submission id and places; verify with the tests in 5.3
- [ ] 5.3 Create the xUnit v3 project `tests/SuperPower.Web.Tests` in the solution (ADR-0002, ADR-0012) and add tests, using a fake `TimeProvider`, for the past/upcoming boundary, the online fixed place, in-person places passed through, and the booking row values; verify all pass
- [ ] 5.4 Expose the past/upcoming rule from 5.2 as a site service the views use for upcoming events (Home and Events lists, ordered by start date); verify with tests in `tests/SuperPower.Web.Tests` for ordering and the past/upcoming boundary

## 6. Site content (PO authors in the backoffice; uSync exports)

Owner: PO. Depends on: 3.3 (form document type), 1.2.

- [ ] 6.1 (PO) `uSync:Settings:ImportOnFirstBoot` is already `true` in the site's `appsettings.json`; delete the local SQLite database and start the site standalone; verify a fresh install with the package's types present and the existing `sample-site` content imported
- [ ] 6.2 (PO) Extend document types and templates per the design's site structure (ADR-0014): optional `form` picker on `compPage` (allowed type `superPowerForm`); `events`, `eventBase` composition (`compPage` + `startDate`, Date Time with time zone, Europe/London only), `eventInPerson` (+ `capacity`, Numeric, required, min 1), `eventOnline`, `services`, `service`, `formsFolder` (allowed at root, no template, allows `superPowerForm` children); templates Events, Event, Services, Service; remove the image block from `compPage`'s Block List and delete the empty media folder (spec `agency-site` "No media library dependency"); verify uSync files appear under `src/SuperPower.Web/uSync/v17/`, saving an in-person event without a capacity or with 0 is refused, and an online event shows no capacity property
- [ ] 6.3 (PO) Create and publish the content per spec `agency-site`: Home, About us, Contact us (form: Contact); Events (form: Event booking) with at least two in-person and one online event, all starting after 16 Oct 2026; Services (form: Service enquiry) with at least Umbraco upgrade, SEO audit, AdWords audit; Forms folder with Contact, Service enquiry and Event booking forms with exactly the fields in spec `form-definitions` "Site form definitions", Event booking success message "Booking received"; verify uSync content files exist and are committed, and each form's fields, types, required flags and Places options in the uSync files match spec `form-definitions` "Site form definitions" exactly

## 7. Site views (`src/SuperPower.Web/Views/`, `src/SuperPower.Web/wwwroot/`)

Owner: worker-frontend. Depends on: 6.2 (document types, templates), 4.1, 5.4.

- [ ] 7.1 Build the shared layout, navigation (Home, About us, Events, Services, Contact us), views for Home (introduction, upcoming events), Page, Events (upcoming events by date with title, date, time, format), Event (details, parent's form), Services, Service (description, parent's form), and one stylesheet with no external resources or scripts (spec `agency-site`); verify each page renders with the navigation, the browser network panel shows requests to the site only, an online event's form has no Places field, and an event with its start moved into the past locally (not committed) shows "This event has ended" and no form

## 8. Agent access

Owner: Architect (shared files `.mcp.json`, `.claude/settings.json`). Depends on: 2.4.

- [ ] 8.1 Run `aspire agent init --mcp`, keep only `.mcp.json` (no skills), and commit `.claude/settings.json` allowing the read-only Aspire MCP tools only (ADR-0011); verify in Claude Code with the AppHost running that the agent lists resources, a Home page trace with spans, and that trace's logs without approval prompts
- [ ] 8.2 Verify the agent is prompted before `execute_resource_command` and has no database tool configured

## 9. Documentation and integration checks

Owner: Architect; README and `tests/smoke/` assigned to a worker as shared files. Depends on:
sections 1-8.

- [ ] 9.1 Write the attendee README: prerequisites (.NET 10 SDK, container runtime, Aspire CLI 13.6+), full-environment run, standalone SQLite run (no DB spans on SQLite), resetting the database and persistent containers, log-level configuration, connecting Claude Code; verify by following it on a fresh clone
- [ ] 9.2 Verify first-boot seeding: remove the SQL data volume and start the AppHost, then delete the SQLite file and start the site standalone; on both, all pages, events, services and the three forms are published and no content is duplicated after a second start
- [ ] 9.3 Cross-platform check (spec `local-environment` "Cross-platform"): on Windows and macOS, start the AppHost and the standalone site from a fresh clone with no OS-specific changes; on Linux, start the standalone site in a .NET 10 SDK container; verify all start and serve the Home page, and report any OS not checked to the PO
- [ ] 9.4 End-to-end smoke check, scripted in `tests/smoke/`, on the AppHost: valid Contact and Service enquiry submissions stored; Event booking for 3 places on an in-person event stores a submission and a pending booking for 3; Event booking on an online event stores a pending booking for 1; a tampered Places value on an online event and a post to a past event are rejected and store nothing; an invalid submission re-rendered; Home page trace with database spans and linked logs; Slow/Break/Heal working; verify the script passes and record results in the change before cutting `demo/act-1-start`
- [ ] 9.5 Update `PLANNING.md` (resolved spikes, Architect proposals now ADRs) and verify no demo answers were added to it
