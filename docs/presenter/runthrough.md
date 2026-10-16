# Presenter runthrough (private; contains demo answers)

Fri 16 Oct 2026, 13:30-14:15 BST. Tracked on `main` in `docs/presenter/`; the stage clone's `demo` branch
deletes it (see "Demo branch"), so the stage agent never sees it. Never copy into CLAUDE.md, PLANNING.md, openspec/
or other docs/. All commands run from the stage clone root.

## Scripts

| When | Command | Notes |
|---|---|---|
| Morning | `aspire start --apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj` | After the Release build; first boot imports content (minutes) |
| Morning | `aspire resource lgtm start --apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj` | Start `lgtm` by hand before the smoke check |
| Morning | `dotnet run tests/smoke/smoke.cs` | Right after start, `lgtm` running, before loading a page |
| Go-live | `cloudflared tunnel run --token-file .cloudflared-token` | Ctrl+C to stop |
| Go-live | `dotnet run tests/traffic/traffic.cs` | Steady GET traffic, ~1 req/s; Ctrl+C to stop |
| Optional | `node tests/load/load-test.mjs --users 10 --duration 60` | GET-only page load, default `--base https://observability.carlcod.es`; results in `tests/load/results/` |
| Act 2 | `dotnet run tests/load/burst.cs -- 5 8` | Rounds, posts per round. Concurrent Contact / service / event posts. Not `tests/traffic/burst.cs` (different output) |

Burst output: one line per post (`302 ACME-nnnn` or `500`), then
`<n> posts, <n> with reference, <n> without; duplicates: ...`. Bug present: about half return 500.
Fixed: every post returns 302. No failures: raise posts per round (`-- 5 16`).
Every successful post is stored and sends an owner email to smtp4dev; a failed post sends none.

## Database

No local `sqlcmd`; use the one inside the SQL Server container. Run in the clone whose AppHost is running.

Export the password (and port, for a GUI client) from the AppHost user secrets (zsh/bash):

```sh
export SQL_PASSWORD="$(dotnet user-secrets list --project src/SuperPower.AppHost | sed -n 's/^Parameters:sql-password = //p')"
export SQL_PORT="$(dotnet user-secrets list --project src/SuperPower.AppHost | sed -n 's/^Resources:sql:tcp:port = //p')"
```

Connect (interactive; `GO` runs a batch, `exit` quits):

```sh
docker exec -it -e SQLCMDPASSWORD="$SQL_PASSWORD" oisp-sql \
  /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -d umbraco -W
```

GUI client: `127.0.0.1,$SQL_PORT`, user `sa`, database `umbraco`, trust server certificate.

Queries (`FormKey` ends `...01` Contact, `...02` Service enquiry, `...03` Event booking).
Leave `FieldsJson` off screen: it holds what attendees typed.

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

## Planted problems (marked `// Demo Code : Act N`)

`main` is committed in the fixed state. Set the initial state by hand in the stage clone from this list
(Morning step 3), following the comment above each line.

| Act | File | Line | Initial state | Fix on stage |
|---|---|---|---|---|
| 1 | `src/SuperPower.ServiceDefaults/Extensions.cs` | `logging.AddProcessor(new PersonalDataLogProcessor(redactionKey));` | Comment out | Uncomment |
| 1 | `src/SuperPower.Web/Forms/AcmeFormsController.cs` | `logger.LogInformation("Form Submitted {@Model}", model);` | Leave in | Leave in |
| 2 | `src/SuperPower.Web/Forms/Storage/FormSubmissionRepository.cs` | `Scope.WriteLock(AcmeLocks.Submissions);` | Comment out | Uncomment |
| 2 | `src/SuperPower.Web/Forms/Storage/FormSubmissionRepository.cs` | `if (DateTime.UtcNow.Second % 2 == 0) throw ...` | Leave commented | Low-traffic fallback only; re-comment to fix |
| 2 | `src/SuperPower.Web/Views/Contact.cshtml` | `await Task.Delay(TimeSpan.FromSeconds(3));` | Leave in | Not fixed in the talk |

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
   restart `site`. About half of posts fail, every 500 is an even second. Logs show `InvalidOperationException`,
   not SqlException 2601, and no duplicate emails. Re-comment it to fix.

3. **Act 2, slow Contact page.** `Contact.cshtml` waits 3 s before rendering: Contact us sits about 3 s above
   the other pages on p95, and burst posts to Contact take 3 s longer. Shown only through the optional
   "slowest pages" query; not fixed in the talk.

One `demo` branch for stage (see "Demo branch"), no per-act branches. Fallback for an act = apply its fix by hand in the stage clone, restart `site`. Code changes
never need a database reset.

## Demo branch

`demo` = `main` minus everything that tells the stage agent the planted problems or their fixes. Built in the
stage clone only, never pushed, never merged into `main`. Rebuild (new clone) after any `main` change.

Delete:
- `docs/` (ADRs incl. ADR-0026 write lock and ADR-0028 redaction, `research/`, `feature-requests/`,
  `presenter/`, `project-setup.md`, `demo-setup.md`; the dev repo keeps them)
- `openspec/` (specs and archived changes describe the lock and the redaction)
- `spikes/`
- `PLANNING.md`
- `README.md` (describes the redaction key, ADR-0028)
- `.claude/agents/`, `.claude/commands/`, `.claude/skills/` (planning workflow; makes the agent role-play)

Keep: `.claude/settings.json`, `.mcp.json`, `src/`, `tests/`, `banners/`, build files.

Replace: `CLAUDE.md` with `claude-stage.md` (committed on `main` under that name so it is not loaded there;
site summary, diagnose and verify from telemetry; no redaction processor, `Demo Code` or fix details).

Decide: strip the explanatory lines under each `// Demo Code : Act N` (Act 2 says "Fix: take the lock").
Keep the Act 2 low-traffic fallback line either way.

### Building the demo branch



```sh
cd ~/Work/Talks
rm -rf stage
git clone --depth 1 git@github.com:carlcod-es/Observability-is-a-Superpower.git ~/Work/Talks/stage
cd ~/Work/Talks/stage
git switch -c demo
git rm -r -q docs openspec spikes PLANNING.md README.md .claude/agents .claude/commands .claude/skills
git rm -q CLAUDE.md && git mv claude-stage.md CLAUDE.md
git commit -a -q -m "demo: strip docs, planning and agent workflow"
cp ~/Work/Talks/Observability-is-a-Superpower/.cloudflared-token .
```

Optional comment strip: edit `AcmeFormsController.cs`, `Extensions.cs`, `FormSubmissionRepository.cs`, then
`git commit -a -q -m "demo: strip comments"`.

Check:
- `ls` shows no `docs/`, `openspec/`, `spikes/`, `PLANNING.md`, `README.md`, `claude-stage.md`; `CLAUDE.md` starts
  "# Rubber Duck Agency site"; `ls .claude` shows only `settings.json`.
- `git branch` shows `* demo`; `git remote -v` points at the dev repo. Never `git push`.
- `dotnet build SuperPower.slnx` succeeds.

## Cloudflare tunnel

Public URL https://observability.carlcod.es/ -> the local site. Remotely managed tunnel: the hostname and
origin service are set in Cloudflare Zero Trust (Networks > Tunnels), not in the repo.

- Needs: `cloudflared` installed (`brew install cloudflared`), `.cloudflared-token` in the stage clone root
  (copied in "Building the demo branch"; token from the Zero Trust tunnel page). The token is a credential:
  never on screen, never committed; rotate in Zero Trust if it leaks.
- Start only after the AppHost is up and Home loads locally. In its own terminal, from the stage clone root:
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
none should prompt for approval. Before go-live the agent has the Aspire MCP (traces and logs, no metrics,
so counts come from traces); from go-live, Grafana MCP (Loki, Tempo, Prometheus; go-live onwards only,
counters cumulative, so ask for `increase`/`rate`).

What exists to query:
- Form post: request span (`acme.page_type` = `contact` / `service` / `event`) with child
  `acme.forms.submit` (`acme.form` = `contact` / `service_enquiry` / `event_booking`, `acme.form.outcome` =
  `stored` / `invalid` / `failed`, `acme.submission.reference`), SQL spans, `acme.forms.notify`.
- Log `Form {Form} for {Page} stored as {Reference}` (`Page` = page name, e.g. the service title).
- Metrics: `acme.forms.submissions` by `acme_form` and `acme_form_outcome`; `http.server.request.duration` by
  `acme_page_type` (`home`, `page`, `contact`, `events`, `event`, `services`, `service`, `none`).

### Morning
1. Stop the dev AppHost: `aspire stop` in the dev clone (frees ports 2525, 3000, 8091, 17210, 44368).
2. Clone: "Building the demo branch", including its checks.
3. Initial state: in the stage clone, set each line in the "Planted problems" table to its initial state.
   Then `git commit -a -q --amend --no-edit`, so `git status` and `git diff` show nothing for the agent to read.
4. Clean up: reset the database (AppHost stopped; all clones share `oisp-sql-data`, so a new clone is not
   enough):
   ```sh
   docker rm -f oisp-sql
   docker volume rm oisp-sql-data
   rm -rf src/SuperPower.Web/umbraco/Data/TEMP
   ```
5. Build and run:
   ```sh
   dotnet clean SuperPower.slnx
   dotnet build SuperPower.slnx -c Release
   aspire start --apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj
   ```
   Wait for first boot (minutes): resources Running, `lgtm` NotStarted. Do not load a page yet.
6. Smoke check:
   - Start `lgtm` by hand: Start on `lgtm` in the Aspire dashboard, or
     `aspire resource lgtm start --apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj`. Wait until
     `lgtm` and `grafana-mcp` are Running.
   - Run `dotnet run tests/smoke/smoke.cs`, still before loading any page (its first request is the first
     Home load).
   - Expected: `ALL CHECKS PASSED`; `no pre-start telemetry in Grafana` reports not applicable (`lgtm` was
     already running). Any FAIL is a real problem. The check stores test submissions, so the first stage
     reference is not ACME-0001.
7. Tunnel check on the phone, then stop the tunnel ("Cloudflare tunnel").
8. Restart for the talk: `aspire stop`, then the `aspire start` command from step 5. This clears Grafana and
   the smoke traffic and makes the next Home load hit the database again. Resources Running, `lgtm`
   NotStarted. Do not load a page until "Local X-ray".
9. Claude Code in the stage clone, default permission mode, `/fast` on. The agent never starts the AppHost
   (`claude-stage.md`); deny any `aspire run` / `start` / `stop` prompt. `/mcp`: aspire connected;
   select grafana -> Disable (saved per user per folder in `~/.claude.json`, so once per stage clone; it
   persists across restarts). Check: `claude mcp list` shows grafana `Disabled for this project`, no
   connection error at startup.
10. Tabs: site, Aspire (Structured logs, Traces), smtp4dev. VSCodium on the stage clone. Fallback videos ready.

### 1. Local X-ray (5 min)
1. Submit a Service enquiry -> "Thank you, we'll be in touch. Your reference is ACME-nnnn."
2. smtp4dev: owner email, same reference.
3. Aspire Traces: POST with `acme.forms.submit`, SQL spans, `acme.forms.notify`.
4. Metrics: `acme.forms.submissions` by form and outcome.

Speaking notes (Aspire dashboard):
- Resources: one command started the site, SQL Server, smtp4dev and the collector; this page is the whole
  local system. `lgtm` is not started: that's "production", later.
- Traces: one form post, end to end. The request span, our own `acme.forms.submit` span with form, outcome
  and reference, the SQL insert, then `acme.forms.notify` for the email. The reference on screen is the
  same one the visitor saw and the email carries: one ID joins the user, the trace and the inbox.
- Structured logs: open the post's logs from the trace. Logs are tied to the trace, and the values are
  fields you can filter on, not text to grep.
- Metrics: the counter is by form and outcome only. That's the useful level for a dashboard: how many,
  which form, did it work. No who.
- Line: this is the developer loop before release. Nothing deployed, already observable.

Check queries (Aspire):
- "How many form submissions are in the Aspire traces, by form and outcome?"
- "Show the trace for submission ACME-nnnn: its spans and durations, and how long the email took."
- "Which pages were the forms submitted from? Use the `stored as` log entries."
- "How long did each form post take end to end, and which span took the most time?"
- "Show the latest Home page trace and its database calls." (database spans only on the first Home load)

### 2. Act 1: personal data in logs (10 min)
Prompt:
> We are about to put this site on the public internet. Using the Aspire telemetry, check
> whether form submissions leave any personal data in our logs or traces. If they do, fix it at
> the source and prove the fix from the telemetry.

Expected: finds `Name` / `Email` on the `Form Submitted` log entry, re-enables the `PersonalDataLogProcessor`
line in `Extensions.cs`, asks to restart `site` (approve), re-submits a form; the same entry now shows
`[REDACTED]` / `[REDACTED:<id>]`. If it proposes deleting the `{@Model}` line, steer it: keep the line, fix
the log pipeline.
Fallback: apply Planted problems 1 by hand, restart `site`.

Check queries (Aspire), before the fix:
- "List the log entries from the last form post with all their properties."
- "Do any logs or spans from form posts contain a name, email address or message text?"

After the fix and restart:
- "Find the form post I just made. Show its log entries and span attributes, and confirm none contain
  personal data."
- "Show the latest `Form Submitted` log entry. Are Name, Email and Message masked?"
- "Do two posts with the same email address show the same `[REDACTED:<id>]`?"
- "Does the `stored as` log still show the form, page and reference?"

### 3. Go-live (4 min)
1. Aspire: start `lgtm` (starts `grafana-mcp` too); wait for Running. Claude: `/mcp` -> grafana -> Enable
   (no restart).
2. Start the tunnel ("Cloudflare tunnel"); QR code slide. "Made-up details welcome, deleted after the talk."
3. Start `traffic.cs`.
4. Grafana: Dashboards > Demo > Demo site.
5. Point: Grafana holds only telemetry from go-live on.

Speaking notes (Grafana, Demo site dashboard):
- Same instrumentation, second destination: the collector now sends a copy to Grafana. No code change, no
  site restart. The Aspire dashboard keeps everything; Grafana starts empty, like a fresh production.
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

Check queries (Grafana):
- "Is Grafana receiving the site's telemetry? Show the request rate for the last 5 minutes by page type."
- "What's the earliest trace in Tempo for the site?" (should match go-live, nothing earlier)
- "How many requests has the traffic generator made since go-live, and any failures?"

### 4. Act 2: load breaks submissions (13 min; shorten here)
1. Attendees submit for a minute; form panel shows `stored`.
2. Run the burst; about half the posts return 500. No 500s: enable the low-traffic fallback (Planted
   problems 2), restart `site`, re-run the burst. Before the Act 2 prompt, say the failure is planted
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
   insert), proposes the fix. Approve, restart `site`.
6. Verify (skip if behind): re-run the burst, no 500s. Agent confirms in Grafana with `increase`/`rate`
   (counter is cumulative).
   Speaking notes (after the fix):
   - Re-run the burst: rate jumps again, error rate stays at 0 %, only `stored` grows. Same panels, the
     proof is the absence of the failure.
7. Optional: 
 - "Who submitted forms today?" -> counts and references only.
 - How many visits have we had to the Services page
 - What are the slowest pages on the website?
 
Fallback: apply Planted problems 2 by hand (re-comment the low-traffic fallback if on), restart `site`,
re-run the burst.

### 5. Close (7 min)
Useful telemetry: outcome counter, spans with the reference, errors with context, nothing personal.
Agent access: read-only Grafana MCP, Aspire commands prompt, no database.
Console logs removed (ADR-0028): every output path can leak PII; close the ones you don't need.

### After the talk
1. Stop the tunnel and `traffic.cs`. Optional: `/mcp` -> grafana -> Disable, ready for the next run.
2. Stop the AppHost. Delete the stage clone, its SQL Server container and volume:

```sh
docker rm -f oisp-sql
docker volume rm oisp-sql-data
```
