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
  SuperPower.Web/            Umbraco 17 site (Rubber Duck Agency; code names keep `Acme`)
banners/                     banner SVG generator `generate.mjs` (Node) and its output
tests/
  SuperPower.Web.Tests/      xUnit v3 unit tests (ADR-0012)
  smoke/                     end-to-end smoke check, `dotnet run tests/smoke/smoke.cs` (ADR-0012)
  traffic/                   demo traffic source `traffic.cs` (GET only) and submission burst
                             `burst.cs` (ADR-0012)
  load/                      load test `load-test.mjs` (Node), results in `load/results/`
spikes/                      throwaway investigations, removed before the talk
docs/
  adrs/                      architecture decision records
  feature-requests/          feature requests (FR-NNNN)
  research/                  background research with sources
  presenter/                 presenter runthrough for the talk
  demo-setup.md              fresh environment before a demo
openspec/                    planning: specs (openspec/specs) and changes (openspec/changes)
.mcp.json                    MCP servers for Claude Code: aspire, grafana (ADR-0022)
.claude/settings.json        pre-approved read tools on both servers; denies reading `docs/presenter/`
```

Layout decisions: ADR-0002. Versions: .NET 10, Umbraco 17.7.1, Aspire 13.6 (SDK 13.6.0, hosting
packages 13.6.x, Community Toolkit collector hosting 13.4.0; ADR-0001).

## Two ways to run the site

| | Aspire (full environment) | Standalone |
|---|---|---|
| Command | `aspire run --apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj` | `dotnet run --project src/SuperPower.Web` |
| Database | SQL Server container | SQLite file |
| Mail | smtp4dev (owner notifications) | not configured: no email sent |
| Telemetry | OTLP -> Collector -> Aspire dashboard + Grafana LGTM (once started) | none exported (no OTLP endpoint set) |
| Console | no log entries (structured logs only) | no log entries; read `umbraco/Logs/` |
| Use | demo, telemetry, agent | quick content or view work |

The AppHost overrides the site's settings with environment variables, so the same site code
runs in both modes. The two databases are independent: each is filled from uSync on its first
start, and nothing syncs them afterwards.

## Local environment (AppHost)

```
 browser --> site ------------------+---------------------------+
             |  umbracoDbDSN        | SMTP localhost:2525       | OTLP
             v                      v                           v
           sql (SQL Server)      smtp4dev                    collector --+--> Aspire dashboard
                                                                         +--> lgtm (Grafana :3000)
                                                                                 ^
                                                                         grafana-mcp :8091
```

Claude Code reads the Aspire dashboard through the `aspire` MCP (stdio) and Grafana through
`grafana-mcp`. `lgtm` is explicit start; `grafana-mcp` waits for it.

| Resource | Image / type | Ports | Persistence | ADR |
|---|---|---|---|---|
| `sql` / database `umbraco` | Azure SQL run as SQL Server container | dynamic | persistent container + data volume | ADR-0009 |
| `smtp4dev` | `rnwood/smtp4dev:3.15.0` | 2525 (SMTP), dynamic web UI (Inbox link in dashboard) | none (in-memory inbox) | ADR-0010, ADR-0025 |
| `lgtm` | `grafana/otel-lgtm:0.36.0`, explicit start; read-only mounts `grafana/acme-dashboards.yaml`, `grafana/dashboards/` | 3000 (Grafana, admin/admin) | none: telemetry history lost on restart; dashboards reloaded from the repo on every start | ADR-0017, ADR-0022, ADR-0023 |
| `collector` | OpenTelemetry Collector contrib 0.162.0 | dynamic | none | ADR-0017 |
| `grafana-mcp` | `grafana/mcp-grafana:2.0.2`, waits for `lgtm` | 8091 (`/mcp`) | none | ADR-0017, ADR-0022 |
| `site` | `SuperPower.Web` project | 44368 (https), 21258 (http) | - | ADR-0002 |

Start order: the site waits for `sql`, `smtp4dev` and `collector`. `lgtm` does not start with
the AppHost: Start on `lgtm` in the dashboard (or `aspire resource lgtm start --apphost
src/SuperPower.AppHost/SuperPower.AppHost.csproj`) starts it without restarting anything else,
and `grafana-mcp`, waiting for `lgtm`, follows (go-live; ADR-0022). All fixed ports are listed
in the root `README.md`.

### Dashboards and demo traffic

On every `lgtm` start Grafana provisions the JSON files in
`src/SuperPower.AppHost/grafana/dashboards/` into folder "Demo": `demo-site.json` (Demo site:
request rate and p95 per page type with trace exemplars, 5xx share, error logs, form
submissions per minute by form and outcome) and
`aspnetcore-19924.json` (community ASP.NET Core dashboard, MIT, source in the README there). UI
edits cannot be saved; change the JSON and restart `lgtm` (ADR-0023).
`tests/traffic/traffic.cs` requests the site's pages (GET only, about one per second) until
Ctrl+C; optional argument: site URL (default `https://localhost:44368`). Start it after go-live.
`tests/traffic/burst.cs` posts valid forms concurrently, round-robin over the three forms, with
made-up names and `@example.com` addresses: `dotnet run tests/traffic/burst.cs [-- <site-url>]
[--count 50] [--parallel 10]`. Prints `ok=<n> failed=<n>` and the references; exit 1 on any
failure. Each post stores a row and sends a notification email (`add-form-emails` design.md D6).

### Mail path
The site sends SMTP to `localhost:2525`, smtp4dev's SMTP port (no TLS, not proxied by Aspire;
ADR-0025). Each stored submission sends one owner notification to `Acme:Forms:OwnerEmail` (AppHost:
`owner@acme-agency.example`), subject `New <form> submission ACME-nnnn`, plain-text body with the
form, page, reference, UTC time and field values (ADR-0027). Personal data stays in the local
inbox. Without an owner address or SMTP host (standalone) no email is sent.

## Data: what persists and where

| Data | Location | Survives restart | Reset |
|---|---|---|---|
| SQL Server database | Docker volume `superpower.apphost-<hash>-sql-data` (inside Docker's VM; hash from the AppHost path) | yes; the container itself is persistent too | stop AppHost, remove the `sql` container, `docker volume rm <volume>` |
| SQLite database | `src/SuperPower.Web/umbraco/Data/Umbraco.sqlite.db` (gitignored) | yes | delete `Umbraco.sqlite.db*` |
| Umbraco cache/indexes | `src/SuperPower.Web/umbraco/Data/TEMP/` (shared by both modes) | yes | delete with either database reset |
| Telemetry (LGTM) | inside the `lgtm` container; only from its start on | no | restart the AppHost |
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
exporter has retry and sending queue off: while `lgtm` is not started its batches are dropped,
never replayed, so LGTM holds no traces or logs from before its start (metric counters are
cumulative and include earlier totals); the Aspire path is unaffected (ADR-0022).
Until then the collector logs an export error per batch.

## Agent access (Claude Code)

`.mcp.json` registers two MCP servers (ADR-0022):

- `aspire` (stdio, `aspire agent mcp`; local stage): traces, structured logs, a trace's logs,
  resource state and console logs from the running AppHost; no metrics. `.claude/settings.json`
  pre-approves its 13 read tools by name; `execute_resource_command` prompts every call. While
  `spikes/` holds other AppHosts the agent calls `select_apphost` first.
- `grafana` (`http://localhost:8091/mcp`; production stage): the Grafana MCP with writes
  disabled and only the Prometheus (metrics), Loki (logs), Tempo (traces) and datasource tools,
  22 read-only tools, all pre-approved (ADR-0017). Reachable only after `lgtm` is started;
  Claude Code shows it failed until then, `/mcp` reconnects it.

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
  `... -- standalone` (SQLite): pages, form submissions and stored rows, Home trace with database
  spans and linked logs, reference on the success page matching the stored row, and (full
  environment) a notification email with that reference in smtp4dev (ADR-0012). The full-environment
  run starts `lgtm` itself and checks that Grafana has no telemetry from a request made before the start, both dashboards
  are provisioned, `acme.page_type` is on the Home metric, span and the service-enquiry post, and
  `/assets` leaves no span. No Umbraco or Aspire integration harness.

## Planning and branches

- OpenSpec: in-progress changes in `openspec/changes/` (proposal, design, specs, tasks);
  their specs move to `openspec/specs/` when a change is archived.
  Change order: `add-demo-foundation`, `add-demo-dashboard`, `add-form-emails`,
  `add-production-environment`.
- ADRs in `docs/adrs/`; `PLANNING.md` holds open decisions and todos (temporary).
- Branches: `main` only. Planted demo problems are marked `// Demo Code : Act N` in the code, with the fix beside them.
- `spikes/` holds throwaway investigations; removed before the talk.
