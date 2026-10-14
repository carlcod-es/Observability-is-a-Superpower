# Observability is a Superpower

Demo for the talk "Observability is a Superpower - Giving Developers and AI Agents X-Ray
Vision". Local Umbraco site with .NET Aspire, OpenTelemetry and read-only agent access to
telemetry over MCP. Full run instructions land with the attendee README (change
`add-demo-foundation` task 9.1).

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

While the spike AppHosts exist, start the environment with an explicit apphost:
`aspire run --apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj`.

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
