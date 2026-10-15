# ADR-0023: Demo dashboards as JSON in the repo, provisioned into Grafana LGTM on start

## Status
Proposed (2026-10-15, change `add-demo-dashboard`). Closes FR-0001.

## Context
Grafana LGTM (`grafana/otel-lgtm:0.36.0`, Grafana 13.2.3) plays production and is started on
demand (ADR-0022). Dashboards live inside the container and are lost whenever it is recreated
(every AppHost restart). `spike-grafana-dashboard` `findings.md`: a custom dashboard authored as
JSON rendered on first load; community dashboard 19924 works unchanged against the site's
built-in ASP.NET Core metrics; the image provisions fixed datasource UIDs (`prometheus`, `loki`,
`tempo`); the LGTM Prometheus datasource sets `timeInterval: 60s`, so `$__rate_interval` is
minutes long; `.NET` exemplars link duration points to Tempo traces. The image reads dashboard
providers from `/otel-lgtm/grafana/conf/provisioning/dashboards/` (unverified for an added file).
PO 2026-10-14: provision 19924 too, with `${DS_PROMETHEUS}` replaced by the literal UID
`prometheus`; if provisioning fails, the AppHost imports the JSON on start, never by hand.

## Decision
- Two dashboard JSON files in `src/SuperPower.AppHost/grafana/dashboards/`: `demo-site.json`
  (uid `demo-site`, authored JSON-first) and `aspnetcore-19924.json` (as published, only
  `${DS_PROMETHEUS}` replaced by `prometheus`).
- Demo dashboard: request rate and p95 duration per page type (label from ADR-0024), 5xx share
  with an `or 0 *` guard for "no data", Loki error-level logs for `service_name="site"`; fixed
  `[1m]` rate windows; exemplars on the p95 query; literal datasource UIDs; window `now-15m`,
  refresh 10 s. Form panels are added by `add-form-emails`.
- Provisioning: a provider file `src/SuperPower.AppHost/grafana/acme-dashboards.yaml`
  (folder "Demo", `allowUiUpdates: false`) and the dashboards directory bind-mounted read-only
  into the `lgtm` container by the AppHost. Load on start only; no live reload.
- Fallback if the mount does not load (verified first in apply): on the `lgtm`
  `ResourceReadyEvent` the AppHost posts each file to `/api/dashboards/db` with
  `overwrite: true`.

## Alternatives
- Import by hand from the files after each start (spike route): a step to forget on stage and
  for attendees; FR-0001 rejects it.
- AppHost import as the primary route: more code than two mounts; kept as the fallback.
- Custom image with dashboards baked in: a build step and an image to maintain; no gain.
- Community dashboards only (19924, 19925): generic HTTP health, no page types, no logs, no
  trace links.
- Building panels in the Grafana UI and exporting: needs the PO's time; JSON-first took one
  iteration in the spike.

## Consequences
- Dashboards exist on every go-live, for presenter and attendees, from repo files only. Edits in
  the UI cannot be saved; change the JSON and restart `lgtm`.
- The dashboard depends on LGTM's fixed datasource UIDs; another Grafana would need edits.
- 19924 is third-party JSON in the repo: its source and licence are recorded next to it.
- The dashboards are not visible to the agent (Grafana MCP has no dashboard tools, ADR-0017).
