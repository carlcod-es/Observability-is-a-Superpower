# Observability is a Superpower

Demo for the talk "Observability is a Superpower - Giving Developers and AI Agents X-Ray
Vision": a local Umbraco site with .NET Aspire, OpenTelemetry, Grafana, and read-only agent
access to the telemetry over MCP.

- Running through the talk's demo: [docs/demo-setup.md](docs/demo-setup.md)
- How it is built (architecture, resources, telemetry, data): [docs/project-setup.md](docs/project-setup.md)
- Decisions and their reasons: [docs/adrs/](docs/adrs/)

## Prerequisites

- .NET 10 SDK, and Aspire CLI 17.0 or later (https://aspire.dev; an older CLI stops the AppHost).
- Docker Desktop or Podman. The SQL Server image is amd64-only (emulated on Apple Silicon).
- A trusted dev certificate: `dotnet dev-certs https --trust`.
- Optional: Claude Code, to let an agent query the telemetry.

## Pre-pull the container images

About 5.2 GB; pull them before you need them (Podman: replace `docker` with `podman`):

```sh
docker pull mcr.microsoft.com/mssql/server:2022-latest
docker pull grafana/otel-lgtm:0.36.0
docker pull ghcr.io/open-telemetry/opentelemetry-collector-releases/opentelemetry-collector-contrib:0.162.0
docker pull rnwood/smtp4dev:3.15.0
docker pull grafana/mcp-grafana:2.0.2
docker pull redis:8.6
docker pull rabbitmq:4.3-management
```

## Run

```sh
aspire run --apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj
```

The first start installs Umbraco and imports the content (a minute or two).

| What | URL |
|---|---|
| Site | https://localhost:44368 (backoffice `/umbraco`, login in `src/SuperPower.Web/appsettings.Development.json`) |
| Aspire dashboard | https://localhost:17210 (logs, traces, metrics, smtp4dev inbox link) |
| Grafana | http://localhost:3000 (admin/admin), dashboard Dashboards > Demo > Demo site |

Then:

1. Submit a form with made-up data. You get a reference `ACME-nnnn`, and the owner email is in smtp4dev.
2. Find the form post trace, logs and metrics in the Aspire dashboard or Grafana. Form field values
   never appear in telemetry.
3. Generate traffic: `dotnet run tests/traffic/traffic.cs` (page views until Ctrl+C) and
   `dotnet run tests/traffic/burst.cs` (50 concurrent form submissions).

Grafana data is lost when the AppHost stops. To run the site alone on SQLite, without telemetry export:
`dotnet run --project src/SuperPower.Web`.

## Connecting Claude Code

1. Start the AppHost, then open Claude Code at the repo root.
2. `/mcp` shows `grafana` connected (read-only Grafana MCP, registered in `.mcp.json`). Reconnect it
   there if Claude Code started first.
3. Ask, for example, "show the latest trace for the Home page and its logs".

The agent cannot restart services or reach the database. After a code change, use Rebuild (not
Restart) on the resource in the Aspire dashboard.

### Enable the Aspire MCP

Optional. It gives the agent the Aspire dashboard's resources, logs and traces (no metrics):

```sh
claude mcp add --scope local aspire -- aspire agent mcp
```

## The acts

Each act is a problem planted in the code and marked `// Demo Code`. `main` runs without them. The
comment says how to switch it on and gives the answer, so try to diagnose it from the telemetry
first, in Grafana or by asking Claude Code. Rebuild the resource after each change.

| Act | Switch on in | Then | Fixed when |
|---|---|---|---|
| 1 - personal data in logs (on stage) | `src/SuperPower.ServiceDefaults/Extensions.cs` | Submit a form; check its logs | No name, email or message in a new post's logs |
| 2 - load breaks submissions (on stage) | `src/SuperPower.Web/Forms/Storage/FormSubmissionRepository.cs` | Run the burst; Demo site dashboard | The burst runs with no failures |
| Homework 3 - bookings never counted | `src/SuperPower.Api/BookingStoredConsumer.cs` | Book an event; panel "Bookings: stored vs counted" | Counted rises with stored |
| Homework 4 - a slow page | `src/SuperPower.Web/Views/Contact.cshtml` | Run the traffic; panel "Request duration p95" | The page's p95 is in line with the others |

Example prompt (Act 2): "Some visitors get an error when they submit a form. Use our observability
in Grafana to find out what is failing and why, fix it, and tell me how we will know it is fixed."

## Reset

Stop the AppHost, then:

```sh
docker ps -aq --filter name=oisp- | xargs docker rm -f -v
docker volume rm oisp-sql-data oisp-cache-data
rm -rf src/SuperPower.Web/umbraco/Data/TEMP
```

The next start imports the content again, with fresh event dates.

## Tests

```sh
dotnet test                       # unit tests
dotnet run tests/smoke/smoke.cs   # end-to-end check; run right after starting the AppHost
```

## Pending cleanup (maintainer)

The talk's planning and agent workflow files are still in the repo. To remove them, run from the
repo root, then delete this section and the demo-setup link at the top:

```sh
git mv -f CLAUDE.learning.md CLAUDE.md
git rm -r -q .claude/agents docs/feature-requests docs/research docs/demo-setup.md docs/adrs/0011-*.md docs/adrs/0018-*.md docs/adrs/0020-*.md docs/adrs/0029-*.md
```
