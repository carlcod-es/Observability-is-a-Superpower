# Demo setup

Steps to bring the environment to a fresh state before a demo. Commands run from the repo root.
Background for each step is in `README.md`; the talk itself is in `docs/presenter/`.

## 1. Before you start
- Docker Desktop is running.
- Container images are pulled (`README.md` "Pre-pull the container images").
- Port 2525 (smtp4dev SMTP) is free.

## 2. Reset the database
Stop the AppHost, then remove the SQL Server container, its volume and the Umbraco temp cache:

```sh
docker rm -f oisp-sql
docker volume rm oisp-sql-data
```

Delete `src/SuperPower.Web/umbraco/Data/TEMP/`.

## 3. Start the environment

```sh
aspire run --apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj
```

First start installs Umbraco, runs the site migrations and imports the content (a minute or
two). Wait until the `site` resource is Running. Do not start `lgtm`; it is started at go-live.

## 4. Smoke check
Run immediately after the start, before loading any page:

```sh
dotnet run tests/smoke/smoke.cs
```

Expected: every check passes except `no pre-start telemetry in Grafana` (known, accepted). Any
other FAIL is a real problem. The smoke check stores a few test submissions, so the first
reference you see on stage will not be ACME-0001.

## 5. Start the Cloudflare tunnel
The tunnel needs `.cloudflared-token` in the repo root (gitignored; see `README.md` "Cloudflare
tunnel"). To try this yourself, set up your own tunnel.

```sh
cloudflared tunnel run --token-file .cloudflared-token
```

Open the public URL and load the Home page to check it.
