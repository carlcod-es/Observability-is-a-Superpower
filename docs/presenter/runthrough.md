# Presenter runthrough (private; contains demo answers)

45 min, live demo, Acts 1 and 2. Acts 3 and 4 are homework (README "The acts"), not on stage.
Never copy into CLAUDE.md or other docs/. Commands run from the repo root.

## Commands

| Name | Command | Notes |
|---|---|---|
| start | `aspire start --apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj` | First boot imports content (minutes) and dates the events from today |
| wait | `aspire wait site --apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj --timeout 600` | Returns when `site` is healthy |
| stop | `aspire stop --apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj` | Always pass `--apphost` (another AppHost could be picked) |
| rebuild | `aspire resource site rebuild --apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj` | Applies a code fix. Dashboard: Rebuild, not Restart (no recompile) |
| smoke | `dotnet run tests/smoke/smoke.cs` | Smoke #2: add `-- running` |
| tunnel | `cloudflared tunnel run --token-file .cloudflared-token` | Ctrl+C to stop |
| traffic | `dotnet run tests/traffic/traffic.cs` | GET traffic, ~1 req/s |
| burst | `dotnet run tests/load/burst.cs -- 5 8` | Rounds, posts per round. Not `tests/traffic/burst.cs` |
| load | `node tests/load/load-test.mjs --users 10 --duration 60` | Optional; results in `tests/load/results/` |

Burst prints `302 ACME-nnnn` or `500` per post, then a summary. Bug present: about half 500. Fixed: all 302.
No failures: `-- 5 16`. Only successful posts are stored and email the owner.

Reset (AppHost stopped; all clones share these containers and volumes):
```sh
docker rm -f oisp-sql && docker volume rm oisp-sql-data
docker rm -f oisp-cache && docker volume rm oisp-cache-data
docker rm -f -v oisp-queue
rm -rf src/SuperPower.Web/umbraco/Data/TEMP
```

## Planted problems (marked `// Demo Code : Act N`)

`main` is committed fixed. Set the initial state by hand (Morning step 7).

| Act | File | Line | Initial state | Fix |
|---|---|---|---|---|
| 1 | `ServiceDefaults/Extensions.cs` | `logging.AddProcessor(new PersonalDataLogProcessor(redactionKey));` | Comment out | Uncomment |
| 1 | `Web/Forms/AcmeFormsController.cs` | `logger.LogInformation("Form Submitted {@Model}", model);` | Leave in | Leave in |
| 2 | `Web/Forms/Storage/FormSubmissionRepository.cs` | `Scope.WriteLock(AcmeLocks.Submissions);` | Comment out | Uncomment |
| 2 | same | `if (DateTime.UtcNow.Second % 2 == 0) throw ...` | Leave commented | Low-traffic fallback only |
| 3 | `Api/BookingStoredConsumer.cs` | `GetProperty("eventKey")` / `("eventId")` | Leave fixed | Homework |
| 4 | `Web/Views/Contact.cshtml` | `await Task.Delay(TimeSpan.FromSeconds(3));` | Leave commented | Homework |

1. **PII in logs.** `{@Model}` logs every field; with the processor off, Name and Email reach telemetry
   unmasked. After the fix the same entry shows `[REDACTED]` / `[REDACTED:<id>]`, which is the proof.
2. **Duplicate reference.** Without the lock, concurrent posts read the same max reference; the second insert
   hits `IX_acmeFormSubmission_ReferenceNumber` (SqlException 2601) -> 500, no email. Any fix that clears the
   burst is fine. Fallback line: ~half of posts fail on even seconds with `InvalidOperationException
   "Duplicate submission reference N"`; re-comment to fix.
3. **Homework: booking never counted.** Consumer reads `eventId`, site sends `eventKey`: every booking logs
   `Booking update failed for (null)`, deliver span errors, message dropped. Visitor and owner see success;
   "Bookings: stored vs counted" shows counted at 0. Fix: read `eventKey`, rebuild `api`; lost bookings stay lost.
4. **Homework: slow Contact page.** Contact renders 3 s late: ~3 s above other pages on p95.

Code comments still say "Act 2 - slow page" and "Initial Demo State" until reworked.
Fallback for any act: apply the fix by hand, rebuild. Code changes never need a database reset.

## Cloudflare tunnel

https://observability.carlcod.es/ -> local site. Hostname and origin are set in Cloudflare Zero Trust
(Networks > Tunnels), not the repo.

- Needs `cloudflared` (`brew install cloudflared`) and `.cloudflared-token` in the repo root (gitignored,
  credential: never on screen; rotate in Zero Trust if leaked).
- Start only once Home loads locally. Up at "Registered tunnel connection" (normally 4). Check on the phone
  over mobile data.
- Errors in the `cloudflared` log -> network or token. 502 -> site down, or Zero Trust origin is wrong
  (`http://localhost:21258` or `https://localhost:44368`).

## Agent and telemetry

The agent has only the read-only Grafana MCP (Loki, Tempo, Prometheus). Counters are cumulative and reset on
restart: ask for `increase`/`rate`. The agent cannot restart anything: it asks, you Rebuild in the Aspire
dashboard and say when it is Running. Check queries are read-only; none should prompt for approval.

What exists to query:
- Form post: request span (`acme.page_type` = `contact` / `service` / `event`) > `acme.forms.submit`
  (`acme.form`, `acme.form.outcome` = `stored` / `invalid` / `failed`, `acme.submission.reference`), SQL spans,
  `acme.forms.notify`.
- Booking also: `acme.forms.publish` > RabbitMQ publish > `api` `deliver booking-stored` (`acme.event.key`) >
  Redis `INCR`. Event page GET: `api` `GET /events/{eventKey}/bookings` > Redis.
- Logs: `Form {Form} for {Page} stored as {Reference}`; `api`: `Booking recorded for {EventKey}, count {Count}`,
  error `Booking update failed for {EventKey}`.
- Metrics: `acme.forms.submissions` by `acme_form`, `acme_form_outcome`; `http.server.request.duration` by
  `acme_page_type`; `acme_bookings_counted_total{job="api"}`.

## Steps

### Morning
1. **stop** (frees ports 2525, 3000, 5290, 8091, 17210, 44368). Leave the code fixed.
2. **Reset.** This dates the events from today (ADR-0037): one past, four upcoming, the first in 4 days
   (smoke books it). Skip it and events keep old dates.
3. Build and start:
   ```sh
   dotnet clean SuperPower.slnx
   dotnet build SuperPower.slnx -c Release
   ```
   then **start**. Wait for all resources Running, including `lgtm` and `grafana-mcp`. Don't load a page.
4. **smoke** before any page load. Expect `ALL CHECKS PASSED`, no N/A. Any FAIL is real.
5. Tunnel: start, check on phone, stop.
6. Set the Act 1 and Act 2 initial state (Planted problems).
7. **stop**; clear booking count and queue (`docker rm -f oisp-cache && docker volume rm oisp-cache-data`,
   `docker rm -f -v oisp-queue`; keep `oisp-sql`); build and **start** again. This clears Grafana and smoke
   traffic. Wait for all Running. Grafana now runs to the end. No page loads until Local X-ray.
8. Claude Code at the repo root, default permissions, `/fast` on. `claude mcp list`: `grafana` connected, no
   `aspire`. Failed: `/mcp` -> `grafana` -> Reconnect. Deny any `aspire` prompt.
9. Tabs: site, Aspire (Resources, Structured logs, Traces), Grafana (Demo site dashboard), smtp4dev,
   VSCodium. Fallback videos ready.

### 1. Local X-ray (5 min)
Submit a Service enquiry -> reference shown. smtp4dev: same reference. Aspire Traces: `acme.forms.submit`,
SQL, `acme.forms.notify`. Metrics: `acme.forms.submissions`.

Say:
- One command started the whole system. `lgtm` plays production; the agent looks there in every act.
- One reference joins the visitor, the trace and the inbox.
- Logs hang off the trace, as filterable fields.
- The counter is by form and outcome only: how many, which form, did it work. No who.
- Developer loop before release: nothing deployed, already observable.

Check queries:
- "How many form submissions since the start, by form and outcome? Use `increase`."
- "Show the trace for ACME-nnnn: spans, durations, how long the email took."
- "Which pages were forms submitted from? Use the `stored as` logs."
- "Show the latest Home page trace and its database calls." (first Home load only)

### 2. Act 1: personal data in logs (10 min)
> We are about to put this site on the public internet. Using our telemetry in Grafana, check
> whether form submissions leave any personal data in our logs or traces. If they do, fix it at
> the source and prove the fix from the telemetry.

Expected: finds Name / Email on `Form Submitted` in Loki, uncomments the processor, asks for a restart
(Rebuild `site`). Re-submit Contact with made-up details; entry now masked. If it wants to delete the
`{@Model}` line, steer it: keep the line, fix the pipeline. Pre-fix values stay in Loki (accepted, ADR-0033).

Check queries:
- Before: "Do any logs or spans from form posts contain a name, email address or message text?"
- After: "Show the latest `Form Submitted` entry. Are Name, Email and Message masked?"
- After: "Do two posts with the same email show the same `[REDACTED:<id>]`?"
- After: "Does the `stored as` log still show form, page and reference?"

### 3. Go-live (4 min)
**tunnel**, QR slide ("made-up details welcome, deleted after the talk"), **traffic**, Grafana Dashboards >
Demo > Demo site.

Say:
- Same instrumentation, second destination: the collector copies everything to Grafana, no code change.
- The dashboard is a file in the repo, reviewed like code.
- Request rate by page type, not URL: cheap, and no personal data in URLs.
- Error rate shows 0 %, not "no data". Zero is an answer.
- p95 exemplars: click a dot, land on a real trace.
- Bookings stored vs counted checks an outcome, not errors (Homework Act 3).
- If asked: ASP.NET Core dashboard 19924 is the generic view; ours answers our questions.

Check query: "Show the request rate for the last 5 minutes by page type, and any failures since go-live."

### 4. Act 2: load breaks submissions (13 min; shorten here)
1. Attendees submit for a minute.
2. **burst**: ~half 500. None: enable the fallback line, rebuild, re-run. Say the failure is planted.
3. Dashboard, left to right: rate jumps, 5xx rises, `failed` appears beside `stored` (all three forms:
   shared code path), Error logs show the SqlException. Nobody reported a bug; the dashboard did.
4. Prompt:
   > Since we went live, some visitors get an error when they submit a form. Use our production
   > observability in Grafana to find out what is failing and why, fix it, and tell me how we
   > will know it is fixed.

   Expected: Loki duplicate key, Tempo failing POSTs, explains the race, proposes the lock. Approve; Rebuild `site`.
5. Verify (skip if behind): re-run **burst**, no 500s, only `stored` grows. Agent confirms with `increase`.
6. Optional: "Who submitted forms today?" -> counts and references only.

### 5. Close (7 min)
Useful telemetry: outcome counters, spans with the reference, errors with context, nothing personal.
Homework: Acts 3 and 4 in README. Agent access: read-only Grafana MCP, restarts by the presenter, no database.
Console logs removed (ADR-0028): every output path can leak PII.
Optional: **smoke** `-- running`: `ALL CHECKS PASSED` with one N/A line.

### After the talk
Stop tunnel and traffic, **stop**, run the first three lines of Reset.

## Database

Credentials are in the `sql` resource properties in the Aspire dashboard, or:
```sh
export SQL_PASSWORD="$(dotnet user-secrets list --project src/SuperPower.AppHost | sed -n 's/^Parameters:sql-password = //p')"
export SQL_PORT="$(dotnet user-secrets list --project src/SuperPower.AppHost | sed -n 's/^Resources:sql:tcp:port = //p')"
```

```sql
-- Latest submissions
SELECT TOP 20 Id, ReferenceNumber, FormKey, SubmittedUtc FROM acmeFormSubmission ORDER BY Id DESC;
-- Last 10 minutes, per form
SELECT FormKey, COUNT(*) FROM acmeFormSubmission
WHERE SubmittedUtc > DATEADD(minute, -10, SYSUTCDATETIME()) GROUP BY FormKey;
-- The unique index behind Act 2
SELECT name, is_unique FROM sys.indexes WHERE object_id = OBJECT_ID('acmeFormSubmission');
```
