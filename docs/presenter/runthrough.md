# Presenter runthrough (private; contains demo answers)

Fri 16 Oct 2026, 13:30-14:15 BST. Tracked on `main` in `docs/presenter/`; the stage clone's `demo` branch
deletes it (see "Demo branch"), so the stage agent never sees it. Never copy into CLAUDE.md, PLANNING.md, openspec/
or other docs/. All commands run from the stage clone root.

## Scripts

| When | Command | Notes |
|---|---|---|
| Morning | `aspire run --apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj` | First boot imports content (minutes) |
| Morning | `dotnet run tests/smoke/smoke.cs` | Right after start, before loading a page; starts `lgtm` itself |
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

## Planted problems (on `main`, marked `// Demo Code : Act N`)

1. **Act 1, PII in logs.** `AcmeFormsController` logs `Form Submitted {@Model}` (every field, including Name
   and Email), and the `PersonalDataLogProcessor` line in `ServiceDefaults/Extensions.cs` is commented out.
   Fix: delete the `{@Model}` log line (the `stored as {Reference}` log stays); uncomment the processor line
   as the safety net.
2. **Act 2, duplicate reference.** `FormSubmissionRepository.InsertAsync` has `Scope.WriteLock` commented out.
   Concurrent posts read the same max; the second insert hits `IX_acmeFormSubmission_ReferenceNumber`
   (SqlException 2601) -> 500. The email is sent after the insert, so a failed post sends no email.
   Fix: uncomment the `WriteLock` line. Any fix is fine if the burst then shows no 500s.
   Low-traffic fallback: in `FormSubmissionRepository.InsertAsync`, uncomment the second `// Demo Code : Act 2`
   line (`DateTime.UtcNow.Second % 2 == 0` -> `InvalidOperationException "Duplicate submission reference N"`),
   restart `site`. About half of posts fail, every 500 is an even second. Logs show `InvalidOperationException`,
   not SqlException 2601, and no duplicate emails. Re-comment it to fix.

   Also a slowdown on the contact page (Contact.cshtml)

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

Prerequisites: everything wanted on stage committed on `main` in the dev repo (the clone takes committed
`main` only), including `claude-stage.md`.
Stop the dev AppHost first (same fixed ports).

```sh
git clone --depth 1 file://$HOME/Work/Talks/Observability-is-a-Superpower ~/Work/Talks/stage
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
- `grep -rn "Demo Code" src` shows the three planted spots (and their fix lines unless stripped).
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

First rebuild the project

```
aspire stop
dotnet clean SuperPower.slnx
dotnet build SuperPower.slnx -c Release
aspire start --apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj
```

### Morning
1. Stop the dev AppHost (fixed ports 2525, 3000, 8091).
2. Stage clone on `demo`: "Building the demo branch", including its checks.
3. Start the AppHost; wait for first boot. Home loads, resources Running, `lgtm` NotStarted.
4. No rehearsal submissions left: reset per `docs/demo-setup.md`; recreating the clone is not enough (all clones share `oisp-sql-data`).
5. Claude Code in the stage clone, default permission mode, `/fast` on. The agent never starts the AppHost
   (`claude-stage.md`); deny any `aspire run` / `start` / `stop` prompt. `/mcp`: aspire connected,
   grafana fails until go-live.
6. Tabs: site, Aspire (Structured logs, Traces), smtp4dev. VSCodium on the stage clone.
7. Tunnel check on the phone, then stop it ("Cloudflare tunnel"). Fallback videos ready.

### 1. Local X-ray (5 min)
1. Submit a Service enquiry -> "Thank you, we'll be in touch. Your reference is ACME-nnnn."
2. smtp4dev: owner email, same reference.
3. Aspire Traces: POST with `acme.forms.submit`, SQL spans, `acme.forms.notify`.
4. Metrics: `acme.forms.submissions` by form and outcome.

### 2. Act 1: personal data in logs (10 min)
Prompt:
> We are about to put this site on the public internet. Using the Aspire telemetry, check
> whether form submissions leave any personal data in our logs or traces. If they do, fix it at
> the source and prove the fix from the telemetry.

Expected: finds `Name` / `Email` on the `Form Submitted` log entry, edits `AcmeFormsController` (may also
re-enable the processor), asks to restart `site` (approve), re-submit a form, logs clean.
Fallback: apply Planted problems 1 by hand, restart `site`.

### 3. Go-live (4 min)
1. Aspire: start `lgtm`. Claude: `/mcp` -> reconnect grafana.
2. Start the tunnel ("Cloudflare tunnel"); QR code slide. "Made-up details welcome, deleted after the talk."
3. Start `traffic.cs`.
4. Grafana: Dashboards > Demo > Demo site.
5. Point: Grafana holds only telemetry from go-live on.

### 4. Act 2: load breaks submissions (13 min; shorten here)
1. Attendees submit for a minute; form panel shows `stored`.
2. Run the burst; about half the posts return 500. No 500s: enable the low-traffic fallback (Planted
   problems 2), restart `site`, re-run the burst. Before the Act 2 prompt, say the failure is planted
   (the agent will read the line).
3. Dashboard: Error rate (5xx) spike, Form submissions by outcome `failed`, Error logs panel SqlException
   (`InvalidOperationException` with the fallback).
4. Optional: database queries above (latest submissions, unique index).
5. Prompt:
   > Since we went live, some visitors get an error when they submit a form. Use our production
   > observability in Grafana to find out what is failing and why, fix it, and tell me how we
   > will know it is fixed.

   Expected: Loki duplicate key error, Tempo failing POSTs, explains the race (no lock around read max +
   insert), proposes the fix. Approve, restart `site`.
6. Verify (skip if behind): re-run the burst, no 500s. Agent confirms in Grafana with `increase`/`rate`
   (counter is cumulative).
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
1. Stop the tunnel and `traffic.cs`.
2. Stop the AppHost. Delete the stage clone, its SQL Server container and volume (

``` 
docker rm -f oisp-sql 
docker volume rm oisp-sql-data
```