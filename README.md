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

## Run the full environment

```sh
aspire run --apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj
```

The explicit `--apphost` selects this AppHost. The command prints a
login link for the Aspire dashboard. It starts SQL Server, smtp4dev behind Toxiproxy, the
OpenTelemetry Collector, Grafana LGTM, the Grafana MCP server and the site. The site waits for
its dependencies; first start installs Umbraco and imports the content (a minute or two).

- Site: https://localhost:44368 (backoffice `/umbraco`; login in
  `src/SuperPower.Web/appsettings.Development.json`, Unattended user).
- Aspire dashboard: logs, traces and metrics; the smtp4dev inbox link; the Slow, Break and Heal
  commands on the `toxiproxy` resource.
- Grafana: http://localhost:3000 (admin/admin), Explore for Tempo (traces), Loki (logs) and
  Prometheus (metrics).

Telemetry in Grafana is kept only while the AppHost runs; after a restart, load a few pages
before querying. The SQL Server database persists between runs.

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

## Log levels

Umbraco logs through Serilog, and only Serilog's levels decide what reaches the dashboard and
Grafana: change `Serilog:MinimumLevel` in `src/SuperPower.Web/appsettings*.json`.
`Logging:LogLevel` has no effect.

## Connecting Claude Code

`.mcp.json` registers one MCP server, `grafana`, at http://localhost:8091/mcp (the Grafana MCP
container in the AppHost). `.claude/settings.json` pre-approves its read-only tools.

1. Start the full environment.
2. Open Claude Code at the repo root (trust the folder if asked). The project's `grafana` MCP
   server is enabled by `.claude/settings.json`; `/mcp` shows it connected.
3. Ask for telemetry, for example "show the latest trace for the Home page and its logs".

The agent gets 22 read-only tools over traces, logs and metrics: no writes, no database
access, no control of the environment. The server is reachable only while the AppHost runs.

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
| Grafana | http://localhost:3000 (admin/admin) | AppHost code (ADR-0017) |
| Grafana MCP | http://localhost:8091/mcp | AppHost code (ADR-0017) |
| SMTP (site -> Toxiproxy) | localhost:2525 | AppHost code (ADR-0010) |
| Public site (Cloudflare tunnel) | https://observability.carlcod.es/ | Cloudflare; origin is the local site |

Ports 3000, 8091 and 2525 must be free; 3000 often clashes with Node dev servers.

## Tests

```sh
dotnet test
```

Unit tests for form validation, stored field values and the event date rules.

End-to-end smoke check (pages, form submissions, stored rows, Home trace with database spans
and linked logs, Slow/Break/Heal):

```sh
dotnet run tests/smoke/smoke.cs               # full environment, SQL Server
dotnet run tests/smoke/smoke.cs -- standalone # site on its own, SQLite
```

Run the full-environment check right after starting the AppHost, before loading any page: Home
makes database calls only on its first load after start. The check adds test submissions to the
database.

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
