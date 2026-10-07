## 1. Setup and telemetry spikes

- [ ] 1.1 Update the Aspire CLI to 13.6.0 (ADR-0001); verify `aspire --version` prints 13.6.x
- [ ] 1.2 Restructure per ADR-0002: move `SuperPower.slnx` and `Directory.Packages.props` to the repo root, add `Directory.Build.props`, create `src/SuperPower.AppHost` (aspire-apphost) and `src/SuperPower.ServiceDefaults` (aspire-servicedefaults), reference ServiceDefaults from the site, call `AddServiceDefaults()` after `CreateUmbracoBuilder()` and `MapDefaultEndpoints()`; verify `dotnet build` at the root succeeds, `aspire run` shows the site resource running, and `/health` returns 200 without breaking Umbraco routes (front end and `/umbraco` load)
- [ ] 1.3 Spike ADR-0007: register the Serilog forwarding sink in ServiceDefaults, remove the `Async` wrapper from the site's Development Serilog config; verify Umbraco start-up log entries appear in the dashboard's structured logs with named properties as fields and a request's logs linked to its trace. If it fails, switch to Serilog.Sinks.OpenTelemetry, record the outcome in ADR-0007, and tell the PO
- [ ] 1.4 Add SqlClient instrumentation, `SuperPower.Forms` source/meter registration, request filters for health and static paths, and the Azure Monitor exporter switch (ADR-0008); verify with the SQL resource from 2.1 that a Home page trace contains database spans, and record how many parentless background database spans appear per minute; if they crowd the trace list, filter them and verify the list shows page traces only

## 2. Local environment (AppHost)

- [ ] 2.1 Add the Azure SQL resource as a persistent container with data volume and database `umbraco`, referenced by the site as `umbracoDbDSN` plus provider name (ADR-0009); verify a fresh volume produces an installed site and the SQL resource shows healthy
- [ ] 2.2 Add smtp4dev 3.15.0 and Toxiproxy 2.12.0 containers, populate the `smtp` proxy on Toxiproxy ready, expose port 2525 unproxied, and set the site's SMTP host/port/`SecureSocketOptions=None` from the AppHost (ADR-0010); verify an email sent from the site (Umbraco backoffice "send test email" in Settings > Health check, or a temporary test call) arrives in smtp4dev's inbox via the dashboard link
- [ ] 2.3 Add Slow, Break and Heal HTTP commands on the Toxiproxy resource; verify Slow measurably delays a test email, Break makes it fail quickly, Heal restores delivery, and the proxy starts healed

## 3. Forms package (`src/SuperPower.Forms`)

- [ ] 3.1 Create the Razor class library, reference it from the site, add the `ActivitySource`/`Meter` named `SuperPower.Forms` with no OpenTelemetry package references (ADR-0006); verify the solution builds and the package has no OpenTelemetry dependency (`dotnet list package --include-transitive` on the project)
- [ ] 3.2 Implement the package migration plan steps for the five element types with fixed keys (ADR-0004); verify on a fresh database that the five element types exist with the shared properties and Select has options, and a second start does not duplicate them
- [ ] 3.3 Create the Block List data type and the `superPowerForm` document type in the migration; first create an equivalent Block List once in the backoffice and mirror its stored configuration; verify editors can add, remove and reorder all five field types on a form node
- [ ] 3.4 Create the `superPowerFormSubmission` table step and an NPoco repository using `Sql<ISqlContext>` builders (ADR-0004); verify a stored submission round-trips on SQLite and on SQL Server with identical field JSON and no client info
- [ ] 3.5 Implement the validator (required, email, pattern with match timeout and configured message, select membership) and duplicate-alias detection; verify with the unit tests in 4.2
- [ ] 3.6 Implement the `SuperPowerForm` ViewComponent and default view (labels, required markers, `novalidate`, antiforgery token, confirmation message, refusal plus log entry on duplicate aliases) (ADR-0005); verify the rendered HTML of a test form contains every field and no `required`/`pattern` attributes or scripts
- [ ] 3.7 Implement the SurfaceController post action with `[ValidateAntiForgeryToken]`, re-display on failure with values and errors, store and redirect on success (ADR-0005); verify a missing token is rejected, an invalid post re-renders with errors and stores nothing, and a valid post stores once and a refresh stores nothing more

## 4. Tests (`tests/SuperPower.Forms.Tests`)

- [ ] 4.1 Create the xUnit v3 project in the solution (ADR-0012); verify `dotnet test` runs at the root
- [ ] 4.2 Add tests for each validation rule, the regex timeout, select tampering, duplicate aliases and posted-value to field-JSON mapping; verify all pass

## 5. Site content (PO authors in the backoffice; uSync exports)

- [ ] 5.1 Set `uSync:Settings:ImportOnFirstBoot: true` in the site's `appsettings.json`, delete the local SQLite database, start the site standalone; verify a fresh install with the package's types present
- [ ] 5.2 (PO) Create document types and templates per the design's site structure: `home`, `classes`, `contact` (each with a form picker where needed, intro text fields), `formsFolder` (allowed at root, no template, allows `superPowerForm` children); verify uSync files appear under `src/SuperPower.Web/uSync/v17/`
- [ ] 5.3 (PO) Create and publish the content: Home, Classes, Contact pages; Forms folder with Contact and Book a class forms with exactly the fields in spec `form-definitions` "Site form definitions"; pick the forms on Classes and Contact; verify uSync content files exist and are committed
- [ ] 5.4 Build the shared layout, navigation, the three page views and one stylesheet with no external resources or scripts (spec `studio-site`); verify each page renders, links to the other two, and the browser network panel shows requests to the site only
- [ ] 5.5 Verify first-boot seeding: remove the SQL data volume and start the AppHost, then delete the SQLite file and start the site standalone; on both, all three pages and both forms are published and no content is duplicated after a second start

## 6. Agent access

- [ ] 6.1 Run `aspire agent init --mcp`, keep only `.mcp.json` (no skills), and commit `.claude/settings.json` allowing the read-only Aspire MCP tools only (ADR-0011); verify in Claude Code with the AppHost running that the agent lists resources, a Home page trace with spans, and that trace's logs without approval prompts
- [ ] 6.2 Verify the agent is prompted before `execute_resource_command` and has no database tool configured

## 7. Documentation and integration checks

- [ ] 7.1 Write the attendee README: prerequisites (.NET 10 SDK, container runtime, Aspire CLI 13.6+), full-environment run, standalone SQLite run (no DB spans on SQLite), resetting the database and persistent containers, log-level configuration, connecting Claude Code; verify by following it on a fresh clone
- [ ] 7.2 End-to-end smoke check on the AppHost: valid Contact and Book a class submissions stored, an invalid submission re-rendered, Home page trace with database spans and linked logs, Slow/Break/Heal working; record results in the change before cutting `demo/act-1-start`
- [ ] 7.3 Update `PLANNING.md` (resolved spikes, Architect proposals now ADRs) and verify no demo answers were added to it
