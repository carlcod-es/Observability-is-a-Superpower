# Observability is a Superpower

Demo for the talk "Observability is a Superpower - Giving Developers and AI Agents X-Ray
Vision". Local Umbraco site with .NET Aspire, OpenTelemetry and read-only agent access to
telemetry over MCP.

How the repo, local environment, data and telemetry are set up: `docs/project-setup.md`.
Decisions and their reasons: `docs/adrs/`.

## Prerequisites

- .NET 10 SDK.
- A container runtime: Docker Desktop (Windows: Linux containers mode) or Podman.
- Aspire CLI 13.6 or later: install from https://aspire.dev, then check `aspire --version`.
  An older CLI cannot connect to this AppHost and stops it during start-up.
- A trusted HTTPS development certificate: `dotnet dev-certs https --trust`.
- Optional: Claude Code, to let an agent query the telemetry (see "Connecting Claude Code").
- Optional: Node.js 18 or later, for the load test (see "Load test").

The SQL Server image is amd64-only: it runs emulated on Apple Silicon and does not run on
Linux arm64. The standalone run (SQLite) works everywhere.

## Pre-pull the container images

The first full run pulls about 4.8 GB of images (Grafana LGTM alone is 2.6 GB). Pull them
before you need them:

```sh
docker pull mcr.microsoft.com/mssql/server:2022-latest
docker pull grafana/otel-lgtm:0.36.0
docker pull ghcr.io/open-telemetry/opentelemetry-collector-releases/opentelemetry-collector-contrib:0.162.0
docker pull rnwood/smtp4dev:3.15.0
docker pull ghcr.io/shopify/toxiproxy:2.12.0
docker pull grafana/mcp-grafana:2.0.2
```

(Podman: replace `docker` with `podman`.)

## Run the demo

1. Check the [Prerequisites](#prerequisites) and do the [pre-pull](#pre-pull-the-container-images).
2. [Run the full environment](#run-the-full-environment); open the Aspire dashboard
   (https://localhost:17210) and the site (https://localhost:44368).
3. Browse the site: Home, an event, a service.
4. Submit a form: Contact, a service enquiry or an upcoming event booking. Use made-up data.
5. In the Aspire dashboard find the page request and form post traces, their structured logs and
   the request-duration metric (Metrics > `site` > `http.server.request.duration`). Form field values never appear in telemetry.
6. Optional: [connect Claude Code](#connecting-claude-code) and ask, for example, "show the latest
   trace for the Home page and its logs".
7. Go live: [start Grafana](#start-grafana), then the [demo traffic](#demo-traffic). Open
   Dashboards > Demo > Demo site (http://localhost:3000/d/demo-site).
8. Ask Claude Code a metrics question through Grafana (reconnect `grafana` via `/mcp` once `grafana-mcp` is running; see
   [Connecting Claude Code](#connecting-claude-code)), for example "how long do requests to the
   Home page take, from metrics?".
9. Optional: look at the stored submissions ([Connect to the database](#connect-to-the-database));
   run the [tests](#tests).

## Run the full environment

```sh
aspire run --apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj
```

The explicit `--apphost` selects this AppHost. The command prints a
login link for the Aspire dashboard. It starts SQL Server, smtp4dev behind Toxiproxy, the
OpenTelemetry Collector and the site. The site waits for its dependencies; first start installs
Umbraco and imports the content (a minute or two). `lgtm` shows in the dashboard as not started
and `grafana-mcp` as waiting for it (see "Start Grafana").

- Site: https://localhost:44368 (backoffice `/umbraco`; login in
  `src/SuperPower.Web/appsettings.Development.json`, Unattended user).
- Aspire dashboard: logs, traces and metrics; the smtp4dev inbox link; the Slow, Break and Heal
  commands on the `toxiproxy` resource.

The Aspire dashboard has all telemetry from the start. The SQL Server database persists between
runs.

## Start Grafana

Grafana LGTM (the production-stage platform, ADR-0022) is not started with the AppHost. Start
it from the Aspire dashboard (Start action on the `lgtm` resource) or the CLI:

```sh
aspire resource lgtm start --apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj
```

`grafana-mcp` waits for `lgtm` and starts by itself; Grafana is up in about 15 s with the image
pulled (see "Pre-pull the container images"; a first start without it is slow). The site is not
restarted.

- Grafana: http://localhost:3000 (admin/admin), Explore for Tempo (traces), Loki (logs) and
  Prometheus (metrics).
- Grafana holds only telemetry produced after `lgtm` started; earlier traces and logs are dropped,
  not replayed, and metric counters are cumulative, so their first values include earlier totals.
  Load a few pages before querying. Grafana data is lost when the AppHost stops.
- Dashboards are provisioned on every `lgtm` start, under Dashboards > Demo (Demo site:
  http://localhost:3000/d/demo-site):
  - "Demo site": request rate and p95 duration per page type, 5xx share, error logs. Click a p95
    exemplar point to open its trace.
  - "ASP.NET Core": community dashboard 19924; pick job `site`.
  - UI edits are not saved: change the JSON in `src/SuperPower.AppHost/grafana/dashboards/` and
    restart `lgtm`. Panels fill once the site has traffic.

### Demo traffic

```sh
dotnet run tests/traffic/traffic.cs                       # optional: -- <site-url>
```

Default site URL `https://localhost:44368`. GET requests only, about one page per second; never
posts a form. Prints `requests=<n> failures=<n>` each minute. Stop with Ctrl+C. Start it after
go-live (after `lgtm` is started); earlier it only clutters the local traces.

## Run the site on its own (SQLite)

```sh
dotnet run --project src/SuperPower.Web
```

Uses `src/SuperPower.Web/umbraco/Data/Umbraco.sqlite.db`, created and filled on first start. No
mail and no telemetry export (no OTLP endpoint is set); use it for content and view work. The
SQLite and SQL Server databases are independent of each other.

## Reset the databases

Content, document types and media live in `src/SuperPower.Web/uSync/` and
`src/SuperPower.Web/wwwroot/media/`; a fresh database imports them on first start.

- SQL Server: stop the AppHost, then remove the container and its volume:
  ```sh
  docker ps -a --filter name=sql-          # find the sql container
  docker rm -f <container>
  docker volume ls --filter name=sql-data  # superpower.apphost-<hash>-sql-data
  docker volume rm <volume>
  ```
- SQLite: stop the site and delete `src/SuperPower.Web/umbraco/Data/Umbraco.sqlite.db*`.
- Either way, also delete `src/SuperPower.Web/umbraco/Data/TEMP/` (cache shared by both runs;
  stale copies log "Last synced Id ... not found", harmless).

## Connect to the database

The full environment's SQL Server, with any SQL client:

- Server: `localhost,<port>`; the port is dynamic: the `sql` resource in the Aspire dashboard
  (also `Resources:sql:tcp:port` in the AppHost user secrets).
- Database: `umbraco`. User: `sa`.
- Password: Aspire generates it on first run and keeps it in the AppHost user secrets, key
  `Parameters:sql-password`:
  ```sh
  dotnet user-secrets list --project src/SuperPower.AppHost
  ```
- `TrustServerCertificate=True` (client option "Trust server certificate").
- Table `acmeFormSubmission` holds the form submissions.

Local demo data only; the agent has no database access. The standalone run uses a SQLite file
instead (see [Run the site on its own](#run-the-site-on-its-own-sqlite)).

## Log levels

Umbraco logs through Serilog, and only Serilog's levels decide what reaches the dashboard and
Grafana: change `Serilog:MinimumLevel` in `src/SuperPower.Web/appsettings*.json`.
`Logging:LogLevel` has no effect.

## Connecting Claude Code

`.mcp.json` registers two MCP servers; `.claude/settings.json` enables them and pre-approves
their read tools.

| Server | Transport | Gives the agent |
|---|---|---|
| `aspire` | stdio, `aspire agent mcp` (Aspire CLI on PATH) | Resource state, console logs, structured logs and traces from the Aspire dashboard; resource commands |
| `grafana` | http://localhost:8091/mcp (Grafana MCP container, read-only) | Traces, logs and metrics from Grafana |

1. Start the full environment. `aspire` works from then on.
2. Open Claude Code at the repo root (trust the folder if asked). `/mcp` shows `aspire`
   connected and `grafana` failed: nothing answers on its port until Grafana is started.
3. Start Grafana (see "Start Grafana"), wait for `grafana-mcp` to be running, then run `/mcp`
   and reconnect `grafana`.
4. Ask for telemetry, for example "show the latest trace for the Home page and its logs".

Notes:

- Several AppHosts live under the repo (`spikes/`), so the agent must call `select_apphost` first;
  it is pre-approved.
- Pre-approved: the Aspire read tools and all `grafana` tools. Aspire `execute_resource_command`
  is not pre-approved: Claude Code prompts on each call.
- The Aspire MCP has no metrics tool; metrics come from Grafana.
- The agent has no database tool and no write access to Grafana.

## Stable ports and URLs

Fixed ports are pinned in `launchSettings.json` files or the AppHost code; everything else
(smtp4dev inbox, Toxiproxy API, SQL Server) gets a dynamic port per run - reach those via the
Aspire dashboard.

| What | URL | Pinned in |
|---|---|---|
| Site (https) | https://localhost:44368 | `src/SuperPower.Web/Properties/launchSettings.json` |
| Site (http) | http://localhost:21258 | `src/SuperPower.Web/Properties/launchSettings.json` |
| Umbraco backoffice | https://localhost:44368/umbraco (https only; login over http fails with OpenIddict ID2083) | - |
| Aspire dashboard | https://localhost:17210 | `src/SuperPower.AppHost/Properties/launchSettings.json` |
| Grafana (reachable after start) | http://localhost:3000 (admin/admin) | AppHost code (ADR-0017) |
| Grafana MCP (reachable after start) | http://localhost:8091/mcp | AppHost code (ADR-0017) |
| SMTP (site -> Toxiproxy) | localhost:2525 | AppHost code (ADR-0010) |
| Public site (Cloudflare tunnel) | https://observability.carlcod.es/ | Cloudflare; origin is the local site |

Ports 3000, 8091 and 2525 must be free (the AppHost binds them at start, before Grafana is
started); 3000 often clashes with Node dev servers.

## Tests

```sh
dotnet test
```

Unit tests for form validation, stored field values and the event date rules.

End-to-end smoke check (pages, form submissions, stored rows, Home trace with database spans
and linked logs, Slow/Break/Heal, provisioned dashboards, page-type label `acme.page_type`,
`/assets` exclusion):

```sh
dotnet run tests/smoke/smoke.cs               # full environment, SQL Server
dotnet run tests/smoke/smoke.cs -- standalone # site on its own, SQLite
```

Run the full-environment check right after starting the AppHost, before loading any page: Home
makes database calls only on its first load after start. The check adds test submissions to the
database.

### Load test

Simulated users request the ten public pages (GET only, no form submissions) for a fixed time,
then report requests, failures and response times (min, avg, p50, p95, p99, max) per page and in
total. Targets the public URL by default, so the full environment and the Cloudflare tunnel must
be running.

```sh
node tests/load/load-test.mjs --label before
# make the change, restart the site
node tests/load/load-test.mjs --label after --compare tests/load/results/<before-run-id>.json
```

| Option | Default | Meaning |
|---|---|---|
| `--label` | `run` | Suffix of the run ID and result file names |
| `--users` | 10 | Concurrent simulated users |
| `--duration` | 60 | Test length in seconds |
| `--ramp` | 10 | Seconds over which users start |
| `--seed` | 1 | Page order per user; same seed and options give the same request sequence |
| `--timeout` | 30000 | Per-request timeout in ms; a timeout counts as a failure |
| `--base` | `https://observability.carlcod.es` | Site root; `http://localhost:21258` tests the local site without the tunnel |
| `--compare` | - | Earlier result `.json`; adds before → after per page (avg, p50, p95, failed) |

Each run writes `tests/load/results/<run-id>.json` (input for `--compare`) and `<run-id>.md`
(table for display). Both record the UTC start and end of the run: use them as the Grafana time
range. Requests carry `User-Agent: SuperPower-LoadTest/1.0 (run <run-id>)`.

- Failed = timeouts, network errors and non-2xx responses. 403 or 429 means Cloudflare rate
  limiting or a bot challenge, not the site.
- Timings vary by about ±30% per page between identical short runs. Compare the TOTAL row and
  p95 from runs of 60 s or more.

## Cloudflare tunnel (presenter, before the demo)

The public URL above is served by a `cloudflared` tunnel from the presenter's machine to the
local site. Put the tunnel token in `.cloudflared-token` at the repo root (gitignored; token
from the Cloudflare Zero Trust tunnel page). Start the tunnel manually before the talk, after
the site is running - same command on every OS:

```sh
cloudflared tunnel run --token-file .cloudflared-token
```

The token file is a credential (it lets anyone serve content on the public hostname). Never
commit it, and keep the token out of shell history and screen captures; rotate it in
Cloudflare Zero Trust if it leaks. Smoke test the public URL the morning of the talk.
