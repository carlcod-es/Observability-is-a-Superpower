# Verification: add-booking-api

## 7.1 Baseline (design.md D11)

Conditions, both runs: `main` at `495cb67`, clean tree; presenter Mac (M1 Pro, 16 GB; Docker 8 GB,
10 CPUs); macOS-performance prep applied by the PO; Low Power Mode off. **On battery** (deviation
from D11 "mains power", PO 2026-10-22: the live demo runs on battery, so measure as on stage).
Debug build (ADR-0029 not applied). Fresh AppHost start, `lgtm` started and healthy, one discarded
warm-up (`--users 5 --duration 20`), traffic generator not running. Burst
`dotnet run tests/load/burst.cs -- 10 8` started 30 s into each run. Docker and process memory
sampled every 5 s (`docker stats`, `ps` RSS), swap from `sysctl vm.swapusage`.

### baseline-local (2026-10-22 09:09 UTC)
`node tests/load/load-test.mjs --base http://localhost:21258 --users 150 --duration 120 --ramp 30 --label baseline-local`
Result: `tests/load/results/2026-10-22T09-09-00-404Z-baseline-local.json`

| page | requests | failed | avg ms | p50 ms | p95 ms |
|---|---:|---:|---:|---:|---:|
| / | 57799 | 0 | 28 | 24 | 66 |
| /about-us/ | 57252 | 0 | 27 | 23 | 63 |
| /services/ | 57554 | 0 | 27 | 23 | 66 |
| /services/seo-audit/ | 57588 | 0 | 27 | 23 | 65 |
| /services/adwords-audit/ | 57628 | 0 | 27 | 23 | 65 |
| /events/ | 57977 | 0 | 27 | 23 | 65 |
| /events/how-to-get-the-most-out-of-your-cms/ | 57670 | 0 | 27 | 23 | 64 |
| /events/upgrading-when-is-the-best-time/ | 57654 | 0 | 27 | 23 | 65 |
| /events/migrating-to-umbraco-from-other-cmss/ | 57577 | 0 | 27 | 23 | 65 |
| /contact-us/ | 57949 | 0 | 27 | 23 | 64 |
| TOTAL | 576648 | 0 | 27 | 23 | 65 |

Throughput 4801 req/s. Burst: 80 posts, 80 × 302 with reference, 0 × 500, no duplicate references.

Peak memory / CPU: `oisp-collector` 3139 MB / 213 %; `oisp-lgtm` 2864 MB / 436 %; `oisp-sql`
1490 MB / 53 %; `oisp-smtp4dev` 151 MB / 126 %; `oisp-grafana-mcp` 61 MB; site process 420 MB RSS.
Swap used: 3022 MB at start, peak 3178 MB, 2404 MB at end.

Swap already moves during baseline runs (+156 MB local peak, +491 MB tunnel peak), so the D11
criterion "no swap growth" is read as "swap peak not materially above the baseline peak" for 7.2;
the PO decides on that reading.

Note: the collector peaks above `lgtm` at this request rate (local run issues ~4800 req/s, far
above a room of 150-200 people); compare against the after run, not as an absolute.

### baseline-tunnel (2026-10-22 09:13 UTC)
`node tests/load/load-test.mjs --users 150 --duration 120 --ramp 30 --label baseline-tunnel`
(default base `https://observability.carlcod.es`, Cloudflare tunnel started by the PO; run by the
Architect from the presenter Mac, so the uplink is this location's). The burst posts to
`https://localhost:44368` (fixed in `burst.cs`), not through the tunnel.
Result: `tests/load/results/2026-10-22T09-13-50-913Z-baseline-tunnel.json`

| page | requests | failed | avg ms | p50 ms | p95 ms |
|---|---:|---:|---:|---:|---:|
| / | 9914 | 0 | 166 | 155 | 305 |
| /about-us/ | 9823 | 0 | 152 | 134 | 287 |
| /services/ | 9895 | 0 | 157 | 142 | 296 |
| /services/seo-audit/ | 9796 | 0 | 164 | 150 | 304 |
| /services/adwords-audit/ | 9744 | 0 | 162 | 150 | 296 |
| /events/ | 10053 | 0 | 158 | 143 | 294 |
| /events/how-to-get-the-most-out-of-your-cms/ | 9788 | 0 | 154 | 136 | 297 |
| /events/upgrading-when-is-the-best-time/ | 9961 | 0 | 162 | 147 | 300 |
| /events/migrating-to-umbraco-from-other-cmss/ | 9898 | 0 | 166 | 147 | 298 |
| /contact-us/ | 9919 | 0 | 156 | 141 | 294 |
| TOTAL | 98791 | 0 | 160 | 145 | 297 |

Throughput 823 req/s. Max outliers 4-27 s on four pages (tunnel; p99 ≤ 402 ms everywhere).
Burst: 80 posts, 80 × 302 with reference, 0 × 500, no duplicate references.

Peak memory / CPU: `oisp-collector` 3390 MB / 248 %; `oisp-lgtm` 2367 MB / 125 %; `oisp-sql`
985 MB / 24 %; `oisp-smtp4dev` 173 MB / 37 %; `oisp-grafana-mcp` 55 MB; site process 227 MB RSS.
Swap used: 2393 MB at start, peak 2884 MB, 2631 MB at end.

## 1.2 / 1.4 / 1.5 Booking service, cache, queue (block 1b, 2026-10-22)
- Deliver span `deliver booking-stored` (api, Consumer) is a child of the publish (also linked); D5 fallback not applied.
- RabbitMQ and Redis spans appear without ServiceDefaults changes; D9 fallback not applied.
- Message to `booking-stored` -> `GET /events/{guid}/bookings` = 1; after AppHost restart still 1, second message -> 2.
- `oisp-cache` / `oisp-queue` StartedAt and Created unchanged across the AppHost restart (persistent, passwords stable).
- AppHost restart to `api` Running + Healthy: 14 s. Tempo has `{resource.service.name="api"}` traces once `lgtm` runs.

## 3.1 Event page (Architect, 2026-10-22)
- Before bookings: no `event-bookings` element on any event page.
- One booking on /events/upgrading-when-is-the-best-time/ (ACME-0201): first poll (< 1 s) showed "1 person has booked this event"; other event pages unchanged.

## 4.1 Smoke (Architect, 2026-10-22)
- `dotnet run tests/smoke/smoke.cs` on a fresh AppHost: ALL CHECKS PASSED (all pre-existing checks plus "booking count rises" 1 -> 2, "api spans with acme.event.key", "booking trace spans site and api").
- `dotnet run tests/smoke/smoke.cs -- standalone`: ALL CHECKS PASSED (plus "standalone: no booking count", "standalone: event booking returns reference").
- Production-stage platform (spec "Booking service telemetry", section 1 supervisor follow-up): Loki `{service_name="api"} |= "Booking recorded for"` returns the consumer log; Prometheus `http_server_request_duration_seconds_count{job="api"}` = 8 on route `/events/{eventKey:guid}/bookings`.

## 4.2 Fail-soft (block 4b, 2026-10-22)
(a) `api` stopped: event page 2.04 s (vs 0.03-0.11 s running), no count text; two bookings 302 with ACME-0205/0206, owner emails arrived; site Client spans to `/events/{key}/bookings` Error (TaskCanceledException), attempts ~1 s, no 10 s attempt (2 s / 1 s pipeline confirmed, section 2 supervisor follow-up); site Warnings `Booking service call failed for {EventKey}`; queue 2 ready, 0 consumers. `api` started: first poll at +1 s showed the count incl. both bookings.
(b) `queue` stopped: 5 bookings in a row 302 (ACME-0207..0211), 23-102 ms, emails arrived; event page GETs 0.03 s and `/health` Healthy during the outage (section 2 supervisor follow-up); site Warnings `Booking event not queued for {EventKey}` (AlreadyClosedException) correlated to the booking trace. **Gap:** no span for the failed publish (connection fails before RabbitMQ.Client's publish span). PO 2026-10-22: add site span `acme.forms.publish` (Acme.Forms source) with Error status; rework in progress. After `queue` start (api not restarted): consumer reattached, next booking counted (section 1 supervisor follow-up). Bookings made while `queue` was down are not counted (spec: not retried).
(c) `cache` stopped: event page 2.03 s, no count text; `api` server span Error (RedisConnectionException, ~5-6 s); the Redis client span itself carries no Error status (instrumentation behaviour). After `cache` start (api not restarted) counts read again. Count dropped by one across the cache restart (last INCR before stop not persisted, Redis `--save 60 1`); accepted drift (spec: count is a display figure).

## 4.3 PII (block 4b, 2026-10-22)
Booking Name `Canary Person`, Email `canary@test.invalid` (ACME-0213) with `api` stopped. Queued message body `{"eventKey":"703ae352-f9a3-4b10-a063-f2923015a4b4"}`, headers `traceparent` only. 155 `api` spans and 552 `api` logs: 0 matches for `Canary`, `canary`, `test.invalid` (also server-side search). Site telemetry: 0 matches. PASS.

## 4.4 README commands (Architect, macOS, 2026-10-22)
`docker pull redis:8.6`, `docker pull rabbitmq:4.3-management` (up to date); `docker rm -f oisp-cache`, `docker rm -f -v oisp-queue`, `docker volume rm oisp-cache-data`: all succeeded, containers and volume gone.
(b) recheck after the `acme.forms.publish` rework (Architect, 2026-10-22, fresh cache/queue after the README reset; all event pages without count text on start): `queue` running, booking ACME-0218: trace `0c2bb126…` has site `acme.forms.submit` > `acme.forms.publish` OK > `rabbitmq connect` + `publish booking-stored`, api `deliver booking-stored` > `INCR`. `queue` stopped, booking ACME-0219 302: trace `bb4ac1b5…` has `acme.forms.publish` **ERR** (3 ms) under `acme.forms.submit`, `acme.forms.notify` OK. Gap closed.

## 7.2 After change (D11)

Conditions as 7.1 except: working tree with sections 1-4 applied (uncommitted on `495cb67`);
on battery (100 %, PO unplugged the charger before the run); swap already 5538 MB at start
(baseline 3022 / 2393 MB). Fresh AppHost start, `lgtm` started and healthy, one discarded warm-up,
same burst at +30 s. `booking-stored` polled every 0.5 s through the management API.

### after-local (2026-10-22 12:56 UTC)
`node tests/load/load-test.mjs --base http://localhost:21258 --users 150 --duration 120 --ramp 30 --label after-local`
Result: `tests/load/results/2026-10-22T12-56-49-335Z-after-local.json` (compared to `baseline-local` by script;
the run was started without `--compare`).

| page | baseline p95 | after p95 | Δ p95 | baseline avg | after avg | failed |
|---|---:|---:|---:|---:|---:|---:|
| / | 66 | 110 | +44 ms (+67 %) | 28 | 41 | 0 |
| /about-us/ | 63 | 104 | +41 ms (+65 %) | 27 | 39 | 0 |
| /services/ | 66 | 106 | +40 ms (+61 %) | 27 | 40 | 0 |
| /services/seo-audit/ | 65 | 107 | +42 ms (+65 %) | 27 | 40 | 0 |
| /services/adwords-audit/ | 65 | 108 | +43 ms (+66 %) | 27 | 40 | 0 |
| /events/ | 65 | 109 | +44 ms (+68 %) | 27 | 40 | 0 |
| /events/how-to-get-the-most-out-of-your-cms/ | 64 | 188 | +124 ms | 27 | 76 | 0 |
| /events/upgrading-when-is-the-best-time/ | 65 | 190 | +125 ms | 27 | 76 | 0 |
| /events/migrating-to-umbraco-from-other-cmss/ | 65 | 190 | +125 ms | 27 | 76 | 0 |
| /contact-us/ | 64 | 107 | +43 ms (+67 %) | 27 | 40 | 0 |
| TOTAL | 65 | 140 | +75 ms | 27 | 51 | 0 |

Throughput 2577 req/s (baseline 4801). Burst: 80 posts, 80 × 302, 0 × 500; 20 of them event
bookings, all published and acked. Queue: backlog never seen above 0 at 0.5 s sampling (drains
< 0.5 s), 1 consumer throughout.

Peak memory / CPU: `oisp-lgtm` 3229 MB / 710 % (baseline 2864 / 436); `oisp-collector` 2612 MB /
558 % (3139 / 213); `oisp-sql` 960 MB; `oisp-smtp4dev` 165 MB; `oisp-queue` 145 MB; `oisp-cache`
14 MB; site 458 MB RSS (420); `api` 188 MB RSS. Swap used: 5538 MB at start, peak 5945 MB (+407 MB),
5553 MB at end.

Reading: event pages add one HTTP call to `api` plus a Redis GET per view, and each view now
produces three more spans (site client, api server, Redis). At ~2600 req/s the Mac is CPU bound
(`lgtm` + collector together ~1270 % CPU vs ~650 % at baseline), so all pages slow, not only event
pages.

### after-tunnel (2026-10-22 13:10 UTC)
`node tests/load/load-test.mjs --users 150 --duration 120 --ramp 30 --label after-tunnel --compare tests/load/results/2026-10-22T09-13-50-913Z-baseline-tunnel.json`
(tunnel restarted by the PO; started ~10 min after `after-local`, same AppHost.)
Result: `tests/load/results/2026-10-22T13-10-39-059Z-after-tunnel.json`

| page | baseline p95 | after p95 | Δ p95 | baseline avg | after avg | failed |
|---|---:|---:|---:|---:|---:|---:|
| / | 305 | 505 | +200 ms (+66 %) | 166 | 227 | 0 |
| /about-us/ | 287 | 412 | +125 ms (+44 %) | 152 | 199 | 0 |
| /services/ | 296 | 433 | +137 ms (+46 %) | 157 | 206 | 0 |
| /services/seo-audit/ | 304 | 467 | +163 ms (+54 %) | 164 | 216 | 0 |
| /services/adwords-audit/ | 296 | 476 | +180 ms (+61 %) | 162 | 219 | 0 |
| /events/ | 294 | 449 | +155 ms (+53 %) | 158 | 210 | 0 |
| /events/how-to-get-the-most-out-of-your-cms/ | 297 | 537 | +240 ms | 154 | 249 | 0 |
| /events/upgrading-when-is-the-best-time/ | 300 | 588 | +288 ms | 162 | 266 | 0 |
| /events/migrating-to-umbraco-from-other-cmss/ | 298 | 578 | +280 ms | 166 | 263 | 0 |
| /contact-us/ | 294 | 441 | +147 ms (+50 %) | 156 | 209 | 0 |
| TOTAL | 297 | 491 | +194 ms | 160 | 226 | 0 |

Throughput 581 req/s (baseline 823). Burst: 80 posts, 79 × 302, **1 × 500** (`/contact-us/`, round 1;
contact form, a path this change does not touch), no duplicate references. Cause not captured: the
Aspire dashboard buffer had rolled past it and Grafana stopped answering queries (see below).
Queue: backlog never above 0 at 0.5 s sampling, 1 consumer; 19 event bookings published and acked.

Peak memory / CPU: `oisp-lgtm` 3496 MB / 831 % (baseline 2367 / 125); `oisp-collector` 2847 MB /
464 % (3390 / 248); `oisp-sql` 751 MB / 209 %; site 257 MB RSS (227); `api` 632 MB RSS. Swap used:
5744 MB at start, peak 6973 MB of 7168 MB (+1229 MB; baseline +491), 5760 MB at end.

After the run, with no load: `oisp-lgtm` stayed at 680-970 % CPU and 3.6 GB; Grafana `/api/health`
timed out (20 s), Loki and Tempo queries did not return. Chrome (dashboards open) used ~200 % CPU,
not measured at baseline.

Reading: the baseline tunnel run also followed its local run within minutes, and `lgtm` kept up
then (125 % CPU). After the change it did not, although `after-local` sent fewer requests than
`baseline-local` (309k vs 577k): each event page view now adds a site client span, an `api` server
span, a Redis span and two `api` hosting logs (`Request starting` / `Request finished`, Information).
`lgtm` was still ingesting that backlog when the tunnel traffic started, so the host was CPU and
memory bound for the whole run.

### 7.2 against D11

| criterion | after-local | after-tunnel |
|---|---|---|
| no failed page requests | met | met |
| burst all 302 | met | **not met** (1 × 500, contact form) |
| event pages p95 ≤ baseline + 50 ms | **not met** (+125 ms) | **not met** (+240 to +288 ms) |
| other pages p95 within 10 % | **not met** (+61 to +68 %) | **not met** (+44 to +66 %) |
| queue drains within 5 s | met (< 0.5 s) | met (< 0.5 s) |
| swap peak not materially above baseline | +407 MB vs +156 MB | +1229 MB vs +491 MB, 97 % of swap |

**PO decision (2026-10-22):** the misses above are accepted as load-test-only. The test runs closed
loop with no think time (580-4800 req/s); 150-200 people in the room produce an estimated 50-100 req/s,
and the worst run still served 581 req/s with 0 failed page requests and p95 ≈ 0.5 s. Do not run a
load test shortly before the talk (the `lgtm` backlog lasts minutes). The burst 500 is on the contact
form, outside this change.

## 6.1 Runthrough reset (PO, 2026-10-22)
Runthrough updated (Morning steps 1, 4, 8; telemetry list; After the talk). After reset and start: PO saw
`booking-stored` Ready 0 / Unacked 0 / Total 0 with 1 consumer in the management UI; Architect: every
event page linked from `/events/` shows no `event-bookings` text.
