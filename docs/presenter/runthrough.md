# Presenter runthrough (private; contains demo answers)

Fri 16 Oct 2026, 13:30-14:15 BST. Never copy into CLAUDE.md or other docs/. All commands run from the repo root.

## Scripts

| When | Command | Notes |
|---|---|---|
| Morning | `aspire start --apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj` | After the Release build; first boot imports content (minutes) |
| Morning | `dotnet run tests/smoke/smoke.cs` | Smoke #1: fixed state, right after start (`lgtm` starts with the AppHost), before loading a page |
| Any act | `aspire resource site rebuild --apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj` | Applies a code fix (`api` for Act 3). Dashboard: Rebuild, not Restart (Restart does not recompile) |
| Go-live | `cloudflared tunnel run --token-file .cloudflared-token` | Ctrl+C to stop |
| Go-live | `dotnet run tests/traffic/traffic.cs` | Steady GET traffic, ~1 req/s; Ctrl+C to stop |
| Optional | `node tests/load/load-test.mjs --users 10 --duration 60` | GET-only page load, default `--base https://observability.carlcod.es`; results in `tests/load/results/` |
| Act 2 | `dotnet run tests/load/burst.cs -- 5 8` | Rounds, posts per round. Concurrent Contact / service / event posts. Not `tests/traffic/burst.cs` (different output) |
| Close | `dotnet run tests/smoke/smoke.cs -- running` | Smoke #2, optional: all fixes applied; one N/A line expected |

Burst output: one line per post (`302 ACME-nnnn` or `500`), then
`<n> posts, <n> with reference, <n> without; duplicates: ...`. Bug present: about half return 500.
Fixed: every post returns 302. No failures: raise posts per round (`-- 5 16`).
Every successful post is stored and sends an owner email to smtp4dev; a failed post sends none.


## Planted problems (marked `// Demo Code : Act N`)

`main` is committed in the fixed state. Set the initial state by hand from this list
(Morning step 7, after smoke #1), following the comment above each line.

| Act | File | Line | Initial state | Fix on stage |
|---|---|---|---|---|
| 1 | `src/SuperPower.ServiceDefaults/Extensions.cs` | `logging.AddProcessor(new PersonalDataLogProcessor(redactionKey));` | Comment out | Uncomment |
| 1 | `src/SuperPower.Web/Forms/AcmeFormsController.cs` | `logger.LogInformation("Form Submitted {@Model}", model);` | Leave in | Leave in |
| 2 | `src/SuperPower.Web/Forms/Storage/FormSubmissionRepository.cs` | `Scope.WriteLock(AcmeLocks.Submissions);` | Comment out | Uncomment |
| 2 | `src/SuperPower.Web/Forms/Storage/FormSubmissionRepository.cs` | `if (DateTime.UtcNow.Second % 2 == 0) throw ...` | Leave commented | Low-traffic fallback only; re-comment to fix |
| 2 | `src/SuperPower.Web/Views/Contact.cshtml` | `await Task.Delay(TimeSpan.FromSeconds(3));` | Leave in | Not fixed in the talk |
| 3 | `src/SuperPower.Api/BookingStoredConsumer.cs` | `GetProperty("eventKey")` / `GetProperty("eventId")` lines | Comment out `eventKey`, uncomment `eventId` | Swap back; Rebuild `api` |

1. **Act 1, PII in logs.** `AcmeFormsController` logs `Form Submitted {@Model}` (every field, including Name
   and Email), and the `PersonalDataLogProcessor` line in `ServiceDefaults/Extensions.cs` is commented out, so
   the values reach the dashboard unmasked.
   Fix: uncomment the processor line. The `{@Model}` line stays: after the fix the same entry shows
   `[REDACTED]` and `[REDACTED:<id>]`, which demonstrates the fix.
2. **Act 2, duplicate reference.** `FormSubmissionRepository.InsertAsync` has `Scope.WriteLock` commented out.
   Concurrent posts read the same max; the second insert hits `IX_acmeFormSubmission_ReferenceNumber`
   (SqlException 2601) -> 500. The email is sent after the insert, so a failed post sends no email.
   Fix: uncomment the `WriteLock` line. Any fix is fine if the burst then shows no 500s.
   Low-traffic fallback: in `FormSubmissionRepository.InsertAsync`, uncomment the second `// Demo Code : Act 2`
   line (`DateTime.UtcNow.Second % 2 == 0` -> `InvalidOperationException "Duplicate submission reference N"`),
   Rebuild `site`. About half of posts fail, every 500 is an even second. Logs show `InvalidOperationException`,
   not SqlException 2601, and no duplicate emails. Re-comment it to fix.

3. **Act 2, slow Contact page.** `Contact.cshtml` waits 3 s before rendering: Contact us sits about 3 s above
   the other pages on p95, and burst posts to Contact take 3 s longer. Shown only through the optional
   "slowest pages" query; not fixed in the talk.

4. **Act 3, booking never counted.** The consumer reads `eventId`, the site sends `eventKey`: `GetProperty`
   throws `KeyNotFoundException` for every booking; the consumer logs `Booking update failed for (null)`, marks the
   deliver span as an error and drops the message (nack, no requeue). Visitor sees the confirmation, owner email
   arrives, count never moves; panel "Bookings: stored vs counted" shows counted at 0. Fix: read `eventKey`, Rebuild
   `api`. Bookings lost before the fix stay lost. The failing span has no `acme.event.key` (thrown before
   `SetTag`); the trace links it to the site's `acme.forms.publish` span. The `api` Error logs are not on the
   Demo site dashboard (site only); the bookings panel is the only dashboard sign.

Fallback for an act = apply its fix by hand, Rebuild `site` (`api` for Act 3). Code changes never need a database reset.

## Cloudflare tunnel

Public URL https://observability.carlcod.es/ -> the local site. Remotely managed tunnel: the hostname and
origin service are set in Cloudflare Zero Trust (Networks > Tunnels), not in the repo.

- Needs: `cloudflared` installed (`brew install cloudflared`), `.cloudflared-token` in the repo root
  (gitignored; token from the Zero Trust tunnel page). The token is a credential:
  never on screen, never committed; rotate in Zero Trust if it leaks.
- Start only after the AppHost is up and Home loads locally. In its own terminal, from the repo root:
  `cloudflared tunnel run --token-file .cloudflared-token`
- Up when the log shows "Registered tunnel connection" (normally 4). Check the public URL on the phone (mobile
  data, not venue Wi-Fi): Home loads, then a form page.
- Ctrl+C stops it; the public URL then returns a Cloudflare error page. Restart with the same command.
- Morning: start, check on phone, stop. Go-live: start again. After the talk: stop.
- Not working: `cloudflared` log errors -> network or token; 502 on the public URL -> site not running or
  origin service in Zero Trust points at the wrong local URL (`http://localhost:21258` or
  `https://localhost:44368`).

## Steps

Each act ends with check queries: prompts for asking the agent what telemetry recorded. Read-only;
none should prompt for approval. The agent has only the Grafana MCP, in every act (ADR-0033): Loki, Tempo,
Prometheus from the morning restart on; counters are cumulative and reset on a service restart, so ask for
`increase`/`rate`. The Aspire dashboard is the presenter's view. The agent cannot restart anything: it asks,
the presenter clicks Rebuild on the resource in the Aspire dashboard and says when it is Running.

Rule: no event bookings by the presenter before Act 3 (Local X-ray and Act 1 use Service enquiry and Contact).
Attendee and burst bookings in Act 2 are fine: they open the gap Act 3 explains.

What exists to query:
- Form post: request span (`acme.page_type` = `contact` / `service` / `event`) with child
  `acme.forms.submit` (`acme.form` = `contact` / `service_enquiry` / `event_booking`, `acme.form.outcome` =
  `stored` / `invalid` / `failed`, `acme.submission.reference`), SQL spans, `acme.forms.notify`.
- Event booking also: `acme.forms.publish` > RabbitMQ `publish booking-stored`, then `api` `deliver booking-stored`
  (`acme.event.key`) > Redis `INCR`. Event page GET: site client call > `api` `GET /events/{eventKey}/bookings` > Redis.
  Contact and service enquiry send no message.
- Log `Form {Form} for {Page} stored as {Reference}` (`Page` = page name, e.g. the service title).
- Metrics: `acme.forms.submissions` by `acme_form` and `acme_form_outcome`; `http.server.request.duration` by
  `acme_page_type` (`home`, `page`, `contact`, `events`, `event`, `services`, `service`, `none`);
  `acme_bookings_counted_total{job="api"}` (no labels; starts at 0 on each `api` start).
- `api` logs `Booking recorded for {EventKey}, count {Count}`, on failure `Booking update failed for {EventKey}`
  (error, with exception; deliver span marked error).

### Morning
1. Stop any running AppHost: `aspire stop` (frees ports 2525, 3000, 5290, 8091, 17210, 44368;
   5290 is `api`, fixed). The AppHost also runs `api`, `cache` (`oisp-cache`) and `queue` (`oisp-queue`);
   `site` does not wait for them.
2. Leave the code in the fixed state for now.
3. Clean up: reset the database, booking counts and queue (AppHost stopped; every clone shares `oisp-sql-data`,
   `oisp-cache-data` and the persistent containers, so a new clone is not enough). `-v` on `oisp-queue` removes
   its anonymous volume (queued messages):
   ```sh
   docker rm -f oisp-sql
   docker volume rm oisp-sql-data
   docker rm -f oisp-cache
   docker rm -f -v oisp-queue
   docker volume rm oisp-cache-data
   rm -rf src/SuperPower.Web/umbraco/Data/TEMP
   ```
4. Build and run:
   ```sh
   dotnet clean SuperPower.slnx
   dotnet build SuperPower.slnx -c Release
   aspire start --apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj
   ```
   Wait for first boot (minutes): all resources Running, including `lgtm` and `grafana-mcp` (`lgtm` starts
   first; `collector`, `site` and `api` wait for it). Do not load a page yet.
5. Smoke #1, fixed state:
   - Run `dotnet run tests/smoke/smoke.cs`, still before loading any page (its first request is the first
     Home load).
   - Expected: `ALL CHECKS PASSED`, including `counted bookings metric increases`, no N/A line. Any FAIL is a real problem. The check
     stores test submissions, so the first stage reference is not ACME-0001.
6. Tunnel check on the phone, then stop the tunnel ("Cloudflare tunnel").
7. Initial state: set each line in the "Planted problems" table to its initial state.
8. Restart for the talk: `aspire stop`; clear the smoke check's booking count and queue (`docker rm -f oisp-cache`,
   `docker rm -f -v oisp-queue`, `docker volume rm oisp-cache-data`); then the build and `aspire start`
   commands from step 4 (builds the initial state). This clears Grafana and the smoke traffic and makes the next
   Home load hit the database again. Event pages show no booking count; `booking-stored` is empty.
9. Wait until all resources are Running, including `lgtm` and `grafana-mcp`. Grafana runs from here to the
   end of the talk. Do not load a page until "Local X-ray"; no event bookings until Act 3.
10. Claude Code at the repo root, default permission mode, `/fast` on. The agent never starts, stops or
    restarts anything (`CLAUDE.md`); deny any `aspire` prompt. Check: `claude mcp list` shows only
    `grafana`, connected (the repo does not register `aspire`). `grafana` failed: `/mcp` -> `grafana` ->
    Reconnect.
11. Tabs: site, Aspire (Resources, Structured logs, Traces), Grafana (Demo site dashboard), smtp4dev. VSCodium on
    the repo. Fallback videos ready.

### 1. Local X-ray (5 min)
1. Submit a Service enquiry -> "Thank you, we'll be in touch. Your reference is ACME-nnnn."
2. smtp4dev: owner email, same reference.
3. Aspire Traces: POST with `acme.forms.submit`, SQL spans, `acme.forms.notify`.
4. Metrics: `acme.forms.submissions` by form and outcome.

Speaking notes (Aspire dashboard):
- Resources: one command started the site, the booking service with its cache and queue, SQL Server, smtp4dev
  and the collector; this page is the whole local system. `lgtm` is Grafana: it plays "production" and is where
  the agent looks, in every act.
- Traces: one form post, end to end. The request span, our own `acme.forms.submit` span with form, outcome
  and reference, the SQL insert, then `acme.forms.notify` for the email. The reference on screen is the
  same one the visitor saw and the email carries: one ID joins the user, the trace and the inbox.
- Structured logs: open the post's logs from the trace. Logs are tied to the trace, and the values are
  fields you can filter on, not text to grep.
- Metrics: the counter is by form and outcome only. That's the useful level for a dashboard: how many,
  which form, did it work. No who.
- Line: this is the developer loop before release. Nothing deployed, already observable.

Check queries (Grafana):
- "How many form submissions have been recorded since the start, by form and outcome? Use `increase`."
- "Show the trace for submission ACME-nnnn: its spans and durations, and how long the email took."
- "Which pages were the forms submitted from? Use the `stored as` log entries."
- "How long did each form post take end to end, and which span took the most time?"
- "Show the latest Home page trace and its database calls." (database spans only on the first Home load)

### 2. Act 1: personal data in logs (10 min)
Prompt:
> We are about to put this site on the public internet. Using our telemetry in Grafana, check
> whether form submissions leave any personal data in our logs or traces. If they do, fix it at
> the source and prove the fix from the telemetry.

Expected: finds `Name` / `Email` on the `Form Submitted` log entry in Loki, re-enables the
`PersonalDataLogProcessor` line in `Extensions.cs`, asks you to restart `site` (Rebuild `site` in the Aspire
dashboard, tell it when Running); you re-submit a form (Contact, made-up details); the same entry now shows
`[REDACTED]` / `[REDACTED:<id>]`. If it proposes deleting the `{@Model}` line, steer it: keep the line, fix
the log pipeline.
Fallback: apply Planted problems 1 by hand, Rebuild `site`.
The unmasked values from before the fix stay in Loki for the talk (PO accepted, ADR-0033): made-up details only.

Check queries (Grafana), before the fix:
- "List the log entries from the last form post with all their properties."
- "Do any logs or spans from form posts contain a name, email address or message text?"

After the fix and restart:
- "Find the form post I just made. Show its log entries and span attributes, and confirm none contain
  personal data."
- "Show the latest `Form Submitted` log entry. Are Name, Email and Message masked?"
- "Do two posts with the same email address show the same `[REDACTED:<id>]`?"
- "Does the `stored as` log still show the form, page and reference?"

### 3. Go-live (4 min)
1. Start the tunnel ("Cloudflare tunnel"); QR code slide. "Made-up details welcome, deleted after the talk."
2. Start `traffic.cs`.
3. Grafana: Dashboards > Demo > Demo site.

Speaking notes (Grafana, Demo site dashboard):
- Same instrumentation, second destination: the collector sends a copy of everything to Grafana. No code
  change between the developer view (Aspire) and "production" (Grafana); the agent has been reading Grafana
  all along.
- The dashboard is a file in the repo, loaded on start. Nobody clicked it together; it's reviewed like code.
- Request rate by page type: one line per kind of page (home, event, service...), not per URL. A small
  fixed label set keeps metrics cheap and carries no personal data in URLs or query strings.
- Error rate (5xx): a share of all requests, and it shows 0 %, not "no data". Zero is an answer.
- Request duration p95 by page type: the slow-but-not-failing view. The dots are exemplars: click one and
  you land on a real trace for that page type. Metric -> trace in one click.
- Error logs: only error level from the site. Empty is good.
- Form submissions by outcome: per minute, by form and stored / invalid / failed. Watch it when the QR code
  traffic arrives.
- If asked: the ASP.NET Core dashboard (community 19924) is the generic view; ours answers our questions.
- If someone spots it: Contact us sits about 3 s above the other pages on p95. Leave it for later or for
  the agent ("slowest pages" query in Act 2).
- Bookings: stored vs counted: stored by the site against counted by the booking service, per minute. Both
  at 0 until someone books an event.

Check queries (Grafana):
- "Is Grafana receiving the site's telemetry? Show the request rate for the last 5 minutes by page type."
- "How many requests has the traffic generator made since go-live, and any failures?"

### 4. Act 2: load breaks submissions (13 min; shorten here)
1. Attendees submit for a minute; form panel shows `stored`.
2. Run the burst; about half the posts return 500. No 500s: enable the low-traffic fallback (Planted
   problems 2), Rebuild `site`, re-run the burst. Before the Act 2 prompt, say the failure is planted
   (the agent will read the line).
3. Dashboard: Error rate (5xx) spike, Form submissions by outcome `failed`, Error logs panel SqlException
   (`InvalidOperationException` with the fallback).
   Speaking notes (Demo site dashboard during the burst):
   - Read the panels left to right as a story: request rate jumps (the burst), error rate goes from 0 % to a
     visible share, form submissions grows a `failed` series next to `stored`, error logs fill.
   - Nobody reported a bug; the dashboard did. That's the point of telemetry before customers tell you.
   - The `failed` series is by form: is it one form or all three? (All three: shared code path.)
   - Error logs give the "why": the exception message, with the trace one click away.
   - The panels tell us what and where. Now hand the same evidence to the agent for the why and the fix.
4. Optional: database queries above (latest submissions, unique index).
5. Prompt:
   > Since we went live, some visitors get an error when they submit a form. Use our production
   > observability in Grafana to find out what is failing and why, fix it, and tell me how we
   > will know it is fixed.

   Expected: Loki duplicate key error, Tempo failing POSTs, explains the race (no lock around read max +
   insert), proposes the fix. Approve; it asks for a restart: Rebuild `site`, tell it when Running.
6. Verify (skip if behind): re-run the burst, no 500s. Agent confirms in Grafana with `increase`/`rate`
   (counter is cumulative).
   Speaking notes (after the fix):
   - Re-run the burst: rate jumps again, error rate stays at 0 %, only `stored` grows. Same panels, the
     proof is the absence of the failure.
   - If anyone asks about the bookings panel (counted below stored): park it, "we'll come back to that one".
7. Optional: 
 - "Who submitted forms today?" -> counts and references only.
 - How many visits have we had to the Services page
 - What are the slowest pages on the website?
 
Fallback: apply Planted problems 2 by hand (re-comment the low-traffic fallback if on), Rebuild `site`,
re-run the burst.

### 5. Act 3: bookings never counted
1. Book an upcoming event on the site (made-up details): "Thank you ... ACME-nnnn"; smtp4dev has the owner
   email. Reload the event page: still no booking line (the page shows none at 0).
2. Dashboard: Bookings: stored vs counted shows stored above counted (counted at 0 since the morning). Error
   rate 0 %, Error logs empty, Form submissions `event_booking stored`.
   Speaking notes (Demo site dashboard):
   - Everything the site measures is green: the visitor got a confirmation, the email went out, no 5xx,
     no error logs. The site is fail-soft: the booking service works behind a queue.
   - Only one panel disagrees: stored vs counted. It doesn't measure errors, it measures an outcome:
     did what the site promised actually happen downstream? The lines should sit on top of each other.
   - The gap is every booking since this morning, including Act 2's burst; the panel shows the last 15
     minutes, the agent can total the rest. Nobody noticed, and no error
     panel would have shown it: the error is in another service's logs.
3. Prompt:
   > Visitors book our events and get a confirmation, but the booking count on the event pages never
   > goes up. Use our production observability in Grafana to find out where the bookings go, fix it, and
   > prove from the telemetry that new bookings are counted.

   Expected: Tempo booking trace spans `site` (`acme.forms.submit`, `acme.forms.publish`, RabbitMQ publish) and
   `api` `deliver booking-stored` with error status and `KeyNotFoundException` (no `acme.event.key` on it; linked
   through `acme.forms.publish` in the same trace); Loki `{service_name="api"}`
   `Booking update failed for (null)`; `acme_bookings_counted_total` flat at 0. Reads the consumer and the
   site's publish (`src/SuperPower.Web/Events/BookingEvents.cs`, `eventKey`), swaps the line, asks for a
   restart: Rebuild `api`, tell it when Running. You book again; the page shows "1 person has booked this event"; it confirms a new trace
   without error, the `Booking recorded` log and `increase(acme_bookings_counted_total[5m])` above 0.
   If it suggests recounting lost bookings: out of scope, they stay lost.
4. Dashboard after the fix: counted rises with stored for the new booking. Speaking notes:
   - The lines meet again for new bookings; the bookings lost before the fix stay lost. Telemetry told us
     how many, not who: the agent can count them, nobody can replay them.
   - Watch outcomes, not just errors.

Fallback: apply Planted problems 4 by hand (Act 3 row), Rebuild `api`, book again.

Check queries (Grafana):
- "How many bookings were stored and how many counted in the last 30 minutes? Use `increase`."
- "Show the latest booking trace end to end across site and api, with durations."
- "Are there any error logs from the api since the fix?"
- "How many bookings were lost before the fix?" (stored minus counted over the talk window)

### 6. Close (7 min)
Useful telemetry: outcome counters (stored vs counted), spans with the reference, errors with context, nothing
personal. Agent access: read-only Grafana MCP only, restarts by the presenter, no database.
Console logs removed (ADR-0028): every output path can leak PII; close the ones you don't need.
Optional smoke #2 (if time; good-state proof): `dotnet run tests/smoke/smoke.cs -- running`. Expected:
`ALL CHECKS PASSED` with one N/A line (first-load check). It makes test submissions and a booking.

### After the talk
1. Stop the tunnel and `traffic.cs`.
2. Stop the AppHost. Delete the SQL Server, Redis and RabbitMQ containers and volumes:

```sh
docker rm -f oisp-sql
docker volume rm oisp-sql-data
docker rm -f oisp-cache
docker rm -f -v oisp-queue
docker volume rm oisp-cache-data
```


## Database

To get access to the server, the aspire dashboard should be able to tell you the credentials under the properties of the container.

You can also export the password (and port, for a GUI client) from the AppHost user secrets (zsh/bash):

```sh
export SQL_PASSWORD="$(dotnet user-secrets list --project src/SuperPower.AppHost | sed -n 's/^Parameters:sql-password = //p')"
export SQL_PORT="$(dotnet user-secrets list --project src/SuperPower.AppHost | sed -n 's/^Resources:sql:tcp:port = //p')"
```
### SQL queries

```sql
-- Latest submissions
SELECT TOP 20 Id, ReferenceNumber, FormKey, SubmittedUtc FROM acmeFormSubmission ORDER BY Id DESC;

-- Count and last reference
SELECT COUNT(*) AS Submissions, MAX(ReferenceNumber) AS LastReference FROM acmeFormSubmission;

-- Stored in the last 10 minutes, per form
SELECT FormKey, COUNT(*) AS Submissions FROM acmeFormSubmission
WHERE SubmittedUtc > DATEADD(minute, -10, SYSUTCDATETIME()) GROUP BY FormKey;

-- The unique index that rejects the duplicate reference
SELECT name, is_unique FROM sys.indexes WHERE object_id = OBJECT_ID('acmeFormSubmission');
```