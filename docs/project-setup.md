# Project setup

How the repo, the local environment and the site fit together. Run instructions are in the
root `README.md`; decisions and their reasons are in `docs/adrs/` (cited as ADR-NNNN).

## Repo layout

```
SuperPower.slnx              solution (root)
Directory.Build.props        shared MSBuild settings
Directory.Packages.props     central package versions, exact pins only (ADR-0001)
global.json                  test runner: Microsoft.Testing.Platform
src/
  SuperPower.AppHost/        Aspire AppHost: every local resource (see "Local environment")
    collector/config.yaml    OpenTelemetry Collector pipelines
    grafana/                 dashboard provider + dashboards/ JSON, mounted into `lgtm` (ADR-0023)
  SuperPower.ServiceDefaults/ OpenTelemetry, health checks, exporter switch, Serilog forwarding
  SuperPower.Api/            booking service `api`: booking count over HTTP, queue consumer (ADR-0030)
  SuperPower.Web/            Umbraco 17 site (Rubber Duck Agency; code names keep `Acme`)
banners/                     banner SVG generator `generate.mjs` (Node) and its output
tests/
  SuperPower.Web.Tests/      xUnit v3 unit tests (ADR-0012)
  smoke/                     end-to-end smoke check, `dotnet run tests/smoke/smoke.cs` (ADR-0012)
  traffic/                   demo traffic source `traffic.cs` (GET only) and submission burst
                             `burst.cs` (ADR-0012)
  load/                      load test `load-test.mjs` (Node), results in `load/results/`
docs/
  adrs/                      architecture decision records
  presenter/                 presenter runthrough for the talk
.mcp.json                    MCP server for Claude Code: grafana; aspire opt-in (ADR-0033)
.claude/settings.json        pre-approved read tools (grafana; aspire once enabled)
```

Layout decisions: ADR-0002. Versions: ADR-0001 and `Directory.Packages.props`.

## Two ways to run the site

| | Aspire (full environment) | Standalone |
|---|---|---|
| Command | `aspire run --apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj` | `dotnet run --project src/SuperPower.Web` |
| Database | SQL Server container | SQLite file |
| Mail | smtp4dev (owner notifications) | not configured: no email sent |
| Telemetry | OTLP -> Collector -> Aspire dashboard + Grafana LGTM | none exported (no OTLP endpoint set) |
| Console | no log entries (structured logs only) | no log entries; read `umbraco/Logs/` |
| Use | demo, telemetry, agent | quick content or view work |

The AppHost overrides the site's settings with environment variables, so the same site code
runs in both modes. The two databases are independent: each is filled from uSync on its first
start, and nothing syncs them afterwards.

## Local environment (AppHost)

```
 browser --> site ------------------+---------------------------+
             |  |  |                | SMTP localhost:2525       | OTLP (site and api)
             |  |  | umbracoDbDSN   v                           v
             |  |  +--> sql      smtp4dev                    collector --+--> Aspire dashboard
             |  | HTTP (count)                                           +--> lgtm (Grafana :3000)
             |  +-----> api --> cache (Redis)                                    ^
             | publish   ^                                               grafana-mcp :8091
             +--> queue -+ consume
                (RabbitMQ)
```

Claude Code reads Grafana through `grafana-mcp` (and the Aspire dashboard through the opt-in
`aspire` MCP). `lgtm` starts with the AppHost; `collector` and `grafana-mcp` wait for it (ADR-0033).

| Resource | Image / type | Ports | Persistence | ADR |
|---|---|---|---|---|
| `sql` / database `umbraco` | Azure SQL run as SQL Server container | dynamic | persistent container + data volume | ADR-0009 |
| `smtp4dev` | `rnwood/smtp4dev:3.15.0` | 2525 (SMTP), dynamic web UI (Inbox link in dashboard) | none (in-memory inbox) | ADR-0010, ADR-0025 |
| `lgtm` | `grafana/otel-lgtm:0.36.0`, starts with the AppHost; read-only mounts `grafana/acme-dashboards.yaml`, `grafana/dashboards/` | 3000 (Grafana, admin/admin) | none: telemetry history lost on restart; dashboards reloaded from the repo on every start | ADR-0017, ADR-0023, ADR-0033 |
| `collector` | OpenTelemetry Collector contrib 0.162.0, waits for `lgtm` | dynamic | none | ADR-0017, ADR-0033 |
| `grafana-mcp` | `grafana/mcp-grafana:2.0.2`, waits for `lgtm` | 8091 (`/mcp`) | none | ADR-0017, ADR-0022 |
| `site` | `SuperPower.Web` project | 44368 (https), 21258 (http) | - | ADR-0002 |
| `api` | `SuperPower.Api` project, booking count service | dynamic, no external endpoint | - | ADR-0030 |
| `cache` | Redis `redis:8.6`, container `oisp-cache` | dynamic | persistent container + volume `oisp-cache-data` | ADR-0031 |
| `queue` | RabbitMQ `rabbitmq:4.3-management`, container `oisp-queue` | dynamic, management UI linked in the dashboard | persistent container, no volume | ADR-0032 |

Start order: `lgtm` -> `collector` and `grafana-mcp` -> `site` (also waits for `sql` and `smtp4dev`)
and `api` (also waits for `cache` and `queue`), so start-up telemetry reaches Grafana (ADR-0033). All
fixed ports are listed in the root `README.md`.

### Dashboards and demo traffic

On every `lgtm` start Grafana provisions the JSON files in
`src/SuperPower.AppHost/grafana/dashboards/` into folder "Demo": `demo-site.json` (Demo site:
request rate and p95 per page type with trace exemplars, 5xx share, error logs, form
submissions per minute by form and outcome) and
`aspnetcore-19924.json` (community ASP.NET Core dashboard, MIT, source in the README there). UI
edits cannot be saved; change the JSON and restart `lgtm` (ADR-0023).
`tests/traffic/traffic.cs` requests the site's pages (GET only, about one per second) until
Ctrl+C; optional argument: site URL (default `https://localhost:44368`).
`tests/traffic/burst.cs` posts valid forms concurrently, round-robin over the three forms, with
made-up names and `@example.com` addresses: `dotnet run tests/traffic/burst.cs [-- <site-url>]
[--count 50] [--parallel 10]`. Prints `ok=<n> failed=<n>` and the references; exit 1 on any
failure. Each post stores a row and sends a notification email.

### Mail path
The site sends SMTP to `localhost:2525`, smtp4dev's SMTP port (no TLS, not proxied by Aspire;
ADR-0025). Each stored submission sends one owner notification to `Acme:Forms:OwnerEmail` (AppHost:
`owner@acme-agency.example`), subject `New <form> submission ACME-nnnn`, plain-text body with the
form, page, reference, UTC time and field values (ADR-0027). Personal data stays in the local
inbox. Without an owner address or SMTP host (standalone) no email is sent.

## Data: what persists and where

| Data | Location | Survives restart | Reset |
|---|---|---|---|
| SQL Server database | Docker volume `oisp-sql-data` (inside Docker's VM; same name for every clone) | yes; the container `oisp-sql` is persistent too | stop AppHost, `docker rm -f oisp-sql`, `docker volume rm oisp-sql-data` |
| SQLite database | `src/SuperPower.Web/umbraco/Data/Umbraco.sqlite.db` (gitignored) | yes | delete `Umbraco.sqlite.db*` |
| Umbraco cache/indexes | `src/SuperPower.Web/umbraco/Data/TEMP/` (shared by both modes) | yes | delete with either database reset |
| Telemetry (LGTM) | inside the `lgtm` container; from the AppHost start | no | restart the AppHost |
| Emails (smtp4dev) | in memory | no | restart the AppHost |
| Content and media definitions | `src/SuperPower.Web/uSync/v17/` (committed) | source of truth | - |
| Media files | `src/SuperPower.Web/wwwroot/media/` (committed; banners generated by `banners/generate.mjs`, 1600x500) | source of truth | - |

The database is never committed. After any reset, uSync imports content, document types and
media on first start (`ImportOnFirstBoot`, ADR-0003).

## Telemetry

Configured once in ServiceDefaults; the site code does not depend on the exporter.

- **Traces**: ASP.NET Core, HttpClient and SqlClient instrumentation, plus the site's
  `Acme.Forms` ActivitySource: `acme.forms.submit` per form post (`acme.form`,
  `acme.form.outcome` stored/invalid/failed, `acme.submission.reference` when stored) and child
  `acme.forms.notify` (ADR-0027). SQL spans are kept only under a recorded parent, which drops
  Umbraco's background database noise (ADR-0008).
- **Metrics**: ASP.NET Core, HttpClient, .NET runtime, plus the site's `Acme.Forms` Meter:
  counter `acme.forms.submissions` (Prometheus `acme_forms_submissions_total`, labels `acme_form`
  `contact`/`service_enquiry`/`event_booking`, `acme_form_outcome`) (ADR-0027).
- **Booking service** (`api`, same ServiceDefaults and log masking): a stored event booking adds
  span `acme.forms.publish` on the site; `api` server and queue-consumer spans carry
  `acme.event.key` (event GUID only), so one trace runs site -> queue -> api -> cache. Meter
  `Acme.Bookings`, counter `acme.bookings.counted` (Prometheus `acme_bookings_counted_total`);
  log `Booking recorded for {EventKey}, count {Count}`; a failed update marks the consumer span
  as error and logs `Booking update failed for {EventKey}` (ADR-0030, ADR-0032, ADR-0034).
- **Page type**: `PageTypeTelemetryMiddleware` tags the request span and
  `http.server.request.duration` with `acme.page_type` (Prometheus `acme_page_type`): the
  rendered content type alias (`home`, `page`, `contact`, `events`, `event`, `services`,
  `service`) or `none` (ADR-0024).
- **Logs**: Umbraco logs through Serilog. A sink in ServiceDefaults forwards Serilog events to
  the OpenTelemetry logger, so logs keep named properties and link to their trace (ADR-0007).
  The forms code logs `Form {Form} for {Page} stored as {Reference}` per stored submission
  (`Page`: the page name, e.g. the event or service title).
  Destructured objects (`{@Model}`) become dotted attributes (`Model.Email`).
  Log levels are set in the site's Serilog configuration only; `Logging:LogLevel` has no effect.
  No Serilog console sink: log entries leave the site only through OpenTelemetry (raw values
  stay in Umbraco's local files under `umbraco/Logs/`).
- **Log redaction** (ADR-0028): `PersonalDataLogProcessor`, the first processor on the
  OpenTelemetry logger, rewrites every entry before any exporter sees it. Attributes whose last
  name segment is Name, Email, Message, Notes, Company or Website become `[REDACTED]` (also
  in the message); every email address becomes `[REDACTED:<id>]`, id = first 12 hex characters
  of HMAC-SHA256 over the lower-cased address. Same address, same id, as long as the key stays.
  Key: `Telemetry:RedactionKey`, from the AppHost's generated secret parameter
  `telemetry-redaction-key`; standalone runs use a random key per process. To show raw values in
  a demo, comment out the line marked `ADR-0028` in `ConfigureOpenTelemetry`
  (`src/SuperPower.ServiceDefaults/Extensions.cs`). Not covered: traces, metrics, exception
  messages, scopes, values inside sequences or dictionaries (email pattern still applies).
- **Filtered out**: `/health`, `/alive`, `/umbraco/backoffice`, `/css`, `/js`, `/media`,
  `/assets`, `/favicon.ico` leave neither spans nor request logs (`TelemetryPaths.cs`, ADR-0008);
  the request metric still counts them under `acme_page_type="none"`.
- **Exporter switch**: `APPLICATIONINSIGHTS_CONNECTION_STRING` set -> Azure Monitor; else
  `OTEL_EXPORTER_OTLP_ENDPOINT` set (by Aspire) -> OTLP; else nothing.
- **Personal data**: form field values (name, email, message, notes, company, website) are
  never recorded; form, page, outcome and reference are. Rule for code: log what happened
  (form, outcome, reference), never the submitted model or field values; the log processor is a
  safety net, not permission.

The Collector (`collector/config.yaml`) has two pipeline sets per signal, each with its own batch
processor: `*/aspire` to the Aspire dashboard (always) and `*/lgtm` to Grafana LGTM. The LGTM
exporter has retry and sending queue off: batches are dropped while `lgtm` is unreachable, never
replayed; the collector waits for `lgtm` at start, so this only matters if `lgtm` is stopped later
(ADR-0033). The Aspire path is unaffected.

## Agent access (Claude Code)

`.mcp.json` registers only `grafana`; `aspire` is opt-in (README "Enable the Aspire MCP")
(ADR-0022, ADR-0033):

- `aspire` (stdio, `aspire agent mcp`; opt-in): traces, structured logs, a trace's logs,
  resource state and console logs from the running AppHost; no metrics. `.claude/settings.json`
  pre-approves its read tools by name; `execute_resource_command` prompts every call.
- `grafana` (`http://localhost:8091/mcp`): the Grafana MCP with writes
  disabled and only the Prometheus (metrics), Loki (logs), Tempo (traces) and datasource tools,
  22 read-only tools, all pre-approved (ADR-0017). Reachable once `grafana-mcp` is Running;
  opened earlier, Claude Code shows it failed and `/mcp` reconnects it.

The agent has no database access.

## The site (`src/SuperPower.Web`)

| Folder | Contents |
|---|---|
| `Program.cs` | Umbraco builder, then `AddServiceDefaults()` |
| `Models/` | ModelsBuilder output, namespace `SuperPower.Web.Models`, committed. Regenerated from the backoffice (Settings > Models Builder) after document type changes; Development mode is `SourceCodeManual` |
| `uSync/v17/` | document types, templates, data types, content, media (exported by uSync on save) |
| `Views/` | layout `master.cshtml`, one view per template, partials for cards, media and forms |
| `Views/Partials/Forms/` | the three form partials (Contact, Service enquiry, Event booking) |
| `wwwroot/css/`, `wwwroot/js/` | `site.css` and `site.js`, edited in place (ADR-0021); self-hosted fonts; no external resources |
| `wwwroot/assets/` | static site images: header logo `rubber-duck.svg` |
| `Forms/` | form models, `AcmeFormsController` (SurfaceController), submission storage, migration plan "Acme.Site", telemetry names (ADR-0016) |
| `Events/` | past/upcoming event rules on `TimeProvider` (ADR-0014) |

Content tree: Home; About us; Events with event pages; Services with service pages; Contact
us. The page type decides the form: Contact us shows Contact, an upcoming event shows Event
booking, a service shows Service enquiry. A past event shows "This event has ended" and no
form.

Form submissions go to one site-owned table, `acmeFormSubmission` (form key, page key, UTC
timestamp, reference number, field values as JSON), created by the site's migration plan on start. Works on
SQLite and SQL Server.

## Tests

- `dotnet test` at the root runs `tests/SuperPower.Web.Tests`: form validation, field-to-JSON
  mapping, past/upcoming event rules with a fake `TimeProvider`, page-type tag values, reference
  format, notification subject and body.
- `dotnet run tests/smoke/smoke.cs` (full environment, run right after starting the AppHost) or
  `... -- standalone` (SQLite): end-to-end check of pages, forms, stored rows, notification email,
  traces, metrics, dashboards and the booking count (ADR-0012). No Umbraco or Aspire integration
  harness.

## Branches

- `main` only. Planted demo problems are marked `// Demo Code : Act N` in the code, with the fix
  commented beside them.
