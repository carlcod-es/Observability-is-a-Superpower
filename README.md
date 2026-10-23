# Observability is a Superpower

Demo for the talk "Observability is a Superpower - Giving Developers and AI Agents X-Ray
Vision". Local Umbraco site with .NET Aspire, OpenTelemetry and read-only agent access to
telemetry over MCP.

- How the repo, local environment, data and telemetry are set up: `docs/project-setup.md`.
- Fresh environment before a demo (database reset, smoke check, tunnel): `docs/demo-setup.md`.
- The talk's act-by-act runthrough: `docs/presenter/`.
- Decisions and their reasons: `docs/adrs/`.

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

The first full run pulls about 5.2 GB of images (Grafana LGTM alone is 2.6 GB). Pull them
before you need them:

```sh
docker pull mcr.microsoft.com/mssql/server:2022-latest
docker pull grafana/otel-lgtm:0.36.0
docker pull ghcr.io/open-telemetry/opentelemetry-collector-releases/opentelemetry-collector-contrib:0.162.0
docker pull rnwood/smtp4dev:3.15.0
docker pull grafana/mcp-grafana:2.0.2
docker pull redis:8.6
docker pull rabbitmq:4.3-management
```

(Podman: replace `docker` with `podman`.)

## Run the demo

1. Check the [Prerequisites](#prerequisites) and do the [pre-pull](#pre-pull-the-container-images).
2. [Run the full environment](#run-the-full-environment); open the Aspire dashboard
   (https://localhost:17210) and the site (https://localhost:44368).
3. Browse the site: Home, an event, a service.
4. Submit a form: Contact, a service enquiry or an upcoming event booking. Use made-up data. The
   success text shows the submission reference `ACME-nnnn` (ADR-0026). The owner notification
   email `New <form> submission ACME-nnnn` is in the smtp4dev inbox (Inbox link on the `smtp4dev`
   resource in the Aspire dashboard; ADR-0027).
5. In the Aspire dashboard find the page request and form post traces, their structured logs and
   the request-duration metric (Metrics > `site` > `http.server.request.duration`). The form post
   trace has an `acme.forms.submit` span with a child `acme.forms.notify`. Form field values never
   appear in telemetry.
6. Optional: [enable the Aspire MCP](#enable-the-aspire-mcp) and ask Claude Code, for example,
   "show the latest trace for the Home page and its logs".
7. Go live: the [demo traffic](#demo-traffic) and the [submission burst](#submission-burst)
   (Grafana is already running, see [Grafana](#grafana)). Open Dashboards > Demo > Demo site
   (http://localhost:3000/d/demo-site); the "Form submissions by outcome" panel follows the burst.
8. Ask Claude Code a metrics question through Grafana (see
   [Connecting Claude Code](#connecting-claude-code)), for example "how long do requests to the
   Home page take, from metrics?".
9. Optional: look at the stored submissions ([Connect to the database](#connect-to-the-database));
   run the [tests](#tests).

## Run the full environment

Rebuild if you need to

```sh
dotnet clean SuperPower.slnx
dotnet build SuperPower.slnx -c Release
aspire run --apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj
```

The explicit `--apphost` selects this AppHost. The command prints a login link for the Aspire
dashboard. It starts SQL Server, smtp4dev, Grafana LGTM (`lgtm`), the OpenTelemetry Collector, `grafana-mcp`, `cache` (Redis), `queue` (RabbitMQ), the booking
service `api` and the site. The site waits for SQL Server, smtp4dev and the Collector, not for `api`, `cache` or `queue`; first start installs
Umbraco and imports the content (a minute or two). The Collector waits for `lgtm` and `grafana-mcp`
waits for it too (see "Grafana").

- Site: https://localhost:44368 (backoffice `/umbraco`; login in
  `src/SuperPower.Web/appsettings.Development.json`, Unattended user).
- Aspire dashboard: logs, traces and metrics; the smtp4dev inbox link.
- `api`: booking service (ADR-0030), no public endpoint. It counts event bookings in `cache`; the event
  pages show "N people have booked this event" from it.
- `queue`: the RabbitMQ management UI is the URL on the `queue` resource in the Aspire dashboard. The
  login is user `guest` and the password is the generated `queue-password` parameter (Aspire dashboard,
  Parameters/Resources view).

The Aspire dashboard has all telemetry from the start. The SQL Server database persists between
runs.

## Grafana

Grafana LGTM (the production-stage platform, ADR-0022) starts with the AppHost. The Collector waits
for it, so start-up telemetry reaches Grafana (ADR-0033). It is up in about 15 s with the image
pulled (see "Pre-pull the container images"; a first start without it is slow).

- Grafana: http://localhost:3000 (admin/admin), Explore for Tempo (traces), Loki (logs) and
  Prometheus (metrics).
- Metric counters are cumulative and reset when a service restarts. Load a few pages before
  querying. Grafana data is lost when the AppHost stops.
- Dashboards are provisioned on every `lgtm` start, under Dashboards > Demo (Demo site:
  http://localhost:3000/d/demo-site):
  - "Demo site": request rate and p95 duration per page type, 5xx share, error logs, form
    submissions per minute by form and outcome ("Form submissions by outcome"), event bookings
    stored vs counted per minute ("Bookings: stored vs counted"). Click a p95 exemplar point to
    open its trace.
  - "ASP.NET Core": community dashboard 19924; pick job `site`.
  - UI edits are not saved: change the JSON in `src/SuperPower.AppHost/grafana/dashboards/` and
    restart `lgtm`. Panels fill once the site has traffic.

### Demo traffic

```sh
dotnet run tests/traffic/traffic.cs                       # optional: -- <site-url>
```

Default site URL `https://localhost:44368`. GET requests only, about one page per second; never
posts a form. Prints `requests=<n> failures=<n>` each minute. Stop with Ctrl+C. Start it at
go-live; earlier it only clutters the local traces.

### Submission burst

```sh
dotnet run tests/traffic/burst.cs                          # optional: -- <site-url> --count 50 --parallel 10
```

Default site URL `https://localhost:44368`, 50 submissions, 10 in parallel. Posts valid forms
concurrently, round-robin over Contact, Service enquiry and Event booking, with made-up data
(`Burst Visitor <n>`, `burst<n>@example.com`). These are real submissions: each stores a row and
sends a notification email to smtp4dev. Prints `ok=<n> failed=<n>`, the references, the distinct
reference count and any failures; exit code 1 on any failure. Run it after go-live to load the
production stage.

## Run the site on its own (SQLite)

```sh
dotnet run --project src/SuperPower.Web
```

Uses `src/SuperPower.Web/umbraco/Data/Umbraco.sqlite.db`, created and filled on first start. No
notification email, no telemetry export (no OTLP endpoint is set) and no booking count on event
pages (no `api`, `cache` or `queue`); event bookings still work. Use it for content and view work. The SQLite and SQL Server databases are independent of each other.

## Reset the databases

Content, document types and media live in `src/SuperPower.Web/uSync/` and
`src/SuperPower.Web/wwwroot/media/`; a fresh database imports them on first start.

- SQL Server: stop the AppHost, then remove the container and its volume:
  ```sh
  docker rm -f oisp-sql
  docker volume rm oisp-sql-data
  ```
- Booking counts (`cache`) and the queue: with the AppHost stopped, also remove their containers and the
  cache volume (this zeroes the counts; `-v` also removes the queue's anonymous volume):
  ```sh
  docker rm -f oisp-cache
  docker rm -f -v oisp-queue
  docker volume rm oisp-cache-data
  ```
- SQLite: stop the site and delete `src/SuperPower.Web/umbraco/Data/Umbraco.sqlite.db*`.
- Either way, also delete `src/SuperPower.Web/umbraco/Data/TEMP/` (cache shared by both runs;
  stale copies log "Last synced Id ... not found", harmless).

## Connect to the database

The full environment's SQL Server, with any SQL client:

- Password and port: Aspire generates the password on first run; both are in the AppHost user
  secrets, keys `Parameters:sql-password` and `Resources:sql:tcp:port` (next to
  `Parameters:telemetry-redaction-key`, the generated log-redaction key, ADR-0028):
  ```sh
  dotnet user-secrets list --project src/SuperPower.AppHost
  ```
  The port is also on the `sql` resource in the Aspire dashboard.
- Server: `127.0.0.1,<port>`. Not `localhost`: the container listens on IPv4 only, and where
  `localhost` resolves to `::1` first (Windows) the connection times out.
- Database: `umbraco`. User: `sa`.
- `TrustServerCertificate=True` (client option "Trust server certificate").
- Table `acmeFormSubmission` holds the form submissions.

Local demo data only; the agent has no database access. The standalone run uses a SQLite file
instead (see [Run the site on its own](#run-the-site-on-its-own-sqlite)).

## Log levels

Umbraco logs through Serilog, and only Serilog's levels decide what reaches the dashboard and
Grafana: change `Serilog:MinimumLevel` in `src/SuperPower.Web/appsettings*.json`.
`Logging:LogLevel` has no effect.

The site writes no log entries to its console (Serilog console sink removed, ADR-0028): read
them in the dashboard's structured logs or Grafana, where personal data is masked. Run alone,
the raw entries are in `src/SuperPower.Web/umbraco/Logs/`.

## Connecting Claude Code

`.mcp.json` registers only `grafana`, the talk's setup. `.claude/settings.json` enables it and
pre-approves its tools, plus the Aspire MCP read tools for when that is
[enabled](#enable-the-aspire-mcp).

| Server | Transport | Gives the agent |
|---|---|---|
| `grafana` | http://localhost:8091/mcp (Grafana MCP container, read-only) | Traces, logs and metrics from Grafana |
| `aspire` (opt-in, see [Enable the Aspire MCP](#enable-the-aspire-mcp)) | stdio, `aspire agent mcp` (Aspire CLI on PATH) | Resource state, console logs, structured logs and traces from the Aspire dashboard; resource commands |

1. Start the full environment.
2. Open Claude Code at the repo root (trust the folder if asked).
3. `/mcp` shows `grafana` connected. (If Claude Code was opened before `grafana-mcp` was Running, reconnect it
   in `/mcp`.)
4. Ask for telemetry, for example "show the latest trace for the Home page and its logs".

Notes:

- Pre-approved: all `grafana` tools, and the Aspire read tools once `aspire` is enabled.
- With `aspire` enabled: several AppHosts live under the repo (`spikes/`), so the agent must call
  `select_apphost` first (pre-approved); `execute_resource_command` is not pre-approved, Claude
  Code prompts on each call; there is no metrics tool, metrics come from Grafana.
- The agent has no database tool and no write access to Grafana.

## Enable the Aspire MCP

The talk ran Grafana (`lgtm`) from the start and gave the agent only `grafana`, which is what the
repo registers. The Aspire MCP (resource state, console logs, structured logs and traces from the
Aspire dashboard; no `lgtm` needed; no metrics) is optional. To add it for yourself, at the repo
root with the Aspire CLI on PATH:

```bash
claude mcp add --scope local aspire -- aspire agent mcp
```

Or add this block to `mcpServers` in `.mcp.json` for everyone using the clone:

```json
"aspire": { "type": "stdio", "command": "aspire", "args": ["agent", "mcp"] }
```

`/mcp` (or `claude mcp list`) then shows `aspire` connected. Read tools are pre-approved; resource
commands prompt on each call. Remove with `claude mcp remove --scope local aspire`.

The agent cannot restart resources; after a code change use Rebuild on the resource in the Aspire
dashboard (Restart does not recompile).

## Stable ports and URLs

Fixed ports are pinned in `launchSettings.json` files or the AppHost code; everything else
(smtp4dev inbox, SQL Server) gets a dynamic port per run; reach those via the Aspire dashboard.

| What | URL | Pinned in |
|---|---|---|
| Site (https) | https://localhost:44368 | `src/SuperPower.Web/Properties/launchSettings.json` |
| Site (http) | http://localhost:21258 | `src/SuperPower.Web/Properties/launchSettings.json` |
| Umbraco backoffice | https://localhost:44368/umbraco (https only; login over http fails with OpenIddict ID2083) | - |
| Aspire dashboard | https://localhost:17210 | `src/SuperPower.AppHost/Properties/launchSettings.json` |
| Grafana | http://localhost:3000 (admin/admin) | AppHost code (ADR-0017) |
| Grafana MCP | http://localhost:8091/mcp | AppHost code (ADR-0017) |
| SMTP (site -> smtp4dev) | localhost:2525 | AppHost code (ADR-0025) |
| Public site (Cloudflare tunnel) | https://observability.carlcod.es/ | Cloudflare; origin is the local site |

Ports 3000, 8091 and 2525 must be free (the AppHost binds them at start); 3000 often clashes with Node dev servers.

## Tests

```sh
dotnet test
```

Unit tests for form validation, stored field values, the event date rules, the reference format and
the notification subject and body.

End-to-end smoke check (pages, form submissions, stored rows, the reference on the success page
matching the stored row, the notification email in smtp4dev (full environment), Home trace with
database spans and linked logs, provisioned dashboards, page-type label `acme.page_type`,
`/assets` exclusion, booking count rising after a booking, counted-bookings metric
(`acme_bookings_counted_total`) rising within 60 s, `api` spans and the site-to-api booking
trace in Tempo; standalone: no booking count, booking still returns a reference):

```sh
dotnet run tests/smoke/smoke.cs               # full environment, SQL Server
dotnet run tests/smoke/smoke.cs -- running    # full environment that has already served pages
dotnet run tests/smoke/smoke.cs -- standalone # site on its own, SQLite
```

`-- running` skips the first-load check (Home database spans) and prints it as N/A.

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
the site is running (same command on every OS; full order in `docs/demo-setup.md`):

```sh
cloudflared tunnel run --token-file .cloudflared-token
```

The token file is a credential (it lets anyone serve content on the public hostname). Never
commit it, and keep the token out of shell history and screen captures; rotate it in
Cloudflare Zero Trust if it leaks. Smoke test the public URL the morning of the talk.
