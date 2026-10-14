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
    collector/config.yaml    OpenTelemetry Collector pipeline
  SuperPower.ServiceDefaults/ OpenTelemetry, health checks, exporter switch, Serilog forwarding
  SuperPower.Web/            Umbraco 17 site (Acme Agency)
tests/
  SuperPower.Web.Tests/      xUnit v3 unit tests (ADR-0012)
  smoke/                     end-to-end smoke check, `dotnet run tests/smoke/smoke.cs` (ADR-0012)
docs/adrs/                   architecture decision records
prototype/                   static Eleventy prototype the site views were built from (ADR-0018,
                             ADR-0019); source of the site's site.css and site.js (ADR-0020);
                             not part of the demo environment
openspec/                    planning: specs (openspec/specs) and changes (openspec/changes)
.mcp.json                    MCP servers for Claude Code (Grafana MCP only, ADR-0017)
.claude/settings.json        pre-approved read-only Grafana MCP tools
```

Layout decisions: ADR-0002. Versions: .NET 10, Umbraco 17.7.1, Aspire 13.6 (SDK 13.6.0, hosting
packages 13.6.x; ADR-0001).

## Two ways to run the site

| | Aspire (full environment) | Standalone |
|---|---|---|
| Command | `aspire run --apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj` | `dotnet run --project src/SuperPower.Web` |
| Database | SQL Server container | SQLite file |
| Mail | Toxiproxy -> smtp4dev | not configured |
| Telemetry | OTLP -> Collector -> Aspire dashboard + Grafana LGTM | none exported (no OTLP endpoint set) |
| Use | demo, telemetry, agent | quick content or view work |

The AppHost overrides the site's settings with environment variables, so the same site code
runs in both modes. The two databases are independent: each is filled from uSync on its first
start, and nothing syncs them afterwards.

## Local environment (AppHost)

```
 browser --> site ------------------+---------------------------+
             |  umbracoDbDSN        | SMTP localhost:2525       | OTLP
             v                      v                           v
           sql (SQL Server)      toxiproxy --> smtp4dev      collector --+--> Aspire dashboard
                                 (Slow/Break/Heal)                       +--> lgtm (Grafana :3000)
                                                                                 ^
                                                        Claude Code --> grafana-mcp :8091
```

| Resource | Image / type | Ports | Persistence | ADR |
|---|---|---|---|---|
| `sql` / database `umbraco` | Azure SQL run as SQL Server container | dynamic | persistent container + data volume | ADR-0009 |
| `smtp4dev` | `rnwood/smtp4dev:3.15.0` | dynamic (Inbox link in dashboard) | none (in-memory inbox) | ADR-0010 |
| `toxiproxy` | `ghcr.io/shopify/toxiproxy:2.12.0` | 2525 (SMTP proxy), dynamic API | none; proxy created healed on start | ADR-0010 |
| `lgtm` | `grafana/otel-lgtm:0.36.0` | 3000 (Grafana, admin/admin) | none: telemetry history lost on restart | ADR-0017 |
| `collector` | OpenTelemetry Collector contrib 0.162.0 | dynamic | none | ADR-0017 |
| `grafana-mcp` | `grafana/mcp-grafana:2.0.2` | 8091 (`/mcp`) | none | ADR-0017 |
| `site` | `SuperPower.Web` project | 44368 (https), 21258 (http) | - | ADR-0002 |

Start order: the site waits for `sql`, `toxiproxy` and `collector`; the collector and
`grafana-mcp` wait for `lgtm`. All fixed ports are listed in the root `README.md`.

### Mail path and fault injection
The site sends SMTP to `localhost:2525`, Toxiproxy's `smtp` proxy, which forwards to smtp4dev.
Three dashboard commands on the `toxiproxy` resource change the path: **Slow** (latency on
every SMTP reply), **Break** (reset every connection), **Heal** (remove all faults).
Presenter-only; the agent has no access to them.

## Data: what persists and where

| Data | Location | Survives restart | Reset |
|---|---|---|---|
| SQL Server database | Docker volume `superpower.apphost-<hash>-sql-data` (inside Docker's VM; hash from the AppHost path) | yes; the container itself is persistent too | stop AppHost, remove the `sql` container, `docker volume rm <volume>` |
| SQLite database | `src/SuperPower.Web/umbraco/Data/Umbraco.sqlite.db` (gitignored) | yes | delete `Umbraco.sqlite.db*` |
| Umbraco cache/indexes | `src/SuperPower.Web/umbraco/Data/TEMP/` (shared by both modes) | yes | delete with either database reset |
| Telemetry (LGTM) | inside the `lgtm` container | no | restart the AppHost |
| Emails (smtp4dev) | in memory | no | restart the AppHost |
| Content and media definitions | `src/SuperPower.Web/uSync/v17/` (committed) | source of truth | - |
| Media files | `src/SuperPower.Web/wwwroot/media/` (committed) | source of truth | - |

The database is never committed. After any reset, uSync imports content, document types and
media on first start (`ImportOnFirstBoot`, ADR-0003).

## Telemetry

Configured once in ServiceDefaults; identical locally and in production.

- **Traces**: ASP.NET Core, HttpClient and SqlClient instrumentation, plus the site's
  `Acme.Forms` ActivitySource. SQL spans are kept only under a recorded parent, which drops
  Umbraco's background database noise (ADR-0008).
- **Metrics**: ASP.NET Core, HttpClient, .NET runtime, plus the site's `Acme.Forms` Meter.
- **Logs**: Umbraco logs through Serilog. A sink in ServiceDefaults forwards Serilog events to
  the OpenTelemetry logger, so logs keep named properties and link to their trace (ADR-0007).
  Log levels are set in the site's Serilog configuration only; `Logging:LogLevel` has no effect.
- **Filtered out**: `/health`, `/alive`, `/umbraco/backoffice`, `/css`, `/js`, `/media`,
  `/favicon.ico` leave neither spans nor request logs (`TelemetryPaths.cs`).
- **Exporter switch**: `APPLICATIONINSIGHTS_CONNECTION_STRING` set -> Azure Monitor; else
  `OTEL_EXPORTER_OTLP_ENDPOINT` set (by Aspire) -> OTLP; else nothing.
- **Personal data**: form field values (name, email, message, notes, company, website) are
  never recorded; form, page and outcome are.

The Collector sends every signal to both the Aspire dashboard and Grafana LGTM
(`collector/config.yaml`).

## Agent access (Claude Code)

`.mcp.json` registers one MCP server, `grafana` at `http://localhost:8091/mcp`. The Grafana MCP
runs with writes disabled and only the Prometheus (metrics), Loki (logs), Tempo (traces) and
datasource tools: 22 read-only tools. `.claude/settings.json` pre-approves them. The agent has
no database access, no Aspire resource state and no resource commands (ADR-0017). The MCP
server is reachable only while the AppHost runs.

## The site (`src/SuperPower.Web`)

| Folder | Contents |
|---|---|
| `Program.cs` | Umbraco builder, then `AddServiceDefaults()` |
| `Models/` | ModelsBuilder output, namespace `SuperPower.Web.Models`, committed. Regenerated from the backoffice (Settings > Models Builder) after document type changes; Development mode is `SourceCodeManual` |
| `uSync/v17/` | document types, templates, data types, content, media (exported by uSync on save) |
| `Views/` | layout `master.cshtml`, one view per template, partials for cards, media and forms |
| `Views/Partials/Forms/` | the three form partials (Contact, Service enquiry, Event booking) |
| `wwwroot/css/`, `wwwroot/js/` | `site.css` and `site.js`: synced copies of `prototype/src/` (edit there, then `npm run sync` in `prototype/`; a test fails on drift, ADR-0020); self-hosted fonts; no external resources |
| `Forms/` | form models, `AcmeFormsController` (SurfaceController), submission storage, migration plan "Acme.Site", telemetry names (ADR-0016) |
| `Events/` | past/upcoming event rules on `TimeProvider` (ADR-0014) |

Content tree: Home; About us; Events with event pages; Services with service pages; Contact
us. The page type decides the form: Contact us shows Contact, an upcoming event shows Event
booking, a service shows Service enquiry. A past event shows "This event has ended" and no
form.

Form submissions go to one site-owned table, `acmeFormSubmission` (form key, page key, UTC
timestamp, field values as JSON), created by the site's migration plan on start. Works on
SQLite and SQL Server.

## Tests

- `dotnet test` at the root runs `tests/SuperPower.Web.Tests`: form validation, field-to-JSON
  mapping, past/upcoming event rules with a fake `TimeProvider`.
- `dotnet run tests/smoke/smoke.cs` (full environment, run right after starting the AppHost) or
  `... -- standalone` (SQLite): pages, form submissions and stored rows, Home trace with database
  spans and linked logs, Slow/Break/Heal (ADR-0012). No Umbraco or Aspire integration harness.

## Planning and branches

- OpenSpec: in-progress changes in `openspec/changes/` (proposal, design, specs, tasks);
  their specs move to `openspec/specs/` when a change is archived.
  Change order: `add-demo-foundation`, `add-demo-dashboard` (planned), `add-form-emails`,
  `add-production-environment`.
- ADRs in `docs/adrs/`; `PLANNING.md` holds open decisions and todos (temporary).
- Branches: `main` is the final state; `demo/act-N-*` are demo recovery points.
- `spikes/` holds throwaway investigations; removed before the talk.
