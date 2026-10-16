# Presenter runthrough (private; contains demo answers)

Fri 16 Oct 2026, 13:30-14:15 BST. Git-ignored; the agent's settings deny reading it. Never copy into
CLAUDE.md, PLANNING.md, openspec/, docs/ or a demo branch. All commands run from the stage clone root.

## Scripts

| When | Command | Notes |
|---|---|---|
| Morning | `aspire run --apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj` | First boot imports content (minutes) |
| Morning | `dotnet run tests/smoke/smoke.cs` | Right after start, before loading a page; starts `lgtm` itself |
| Go-live | `cloudflared tunnel run --token-file .cloudflared-token` | Ctrl+C to stop |
| Go-live | `dotnet run tests/traffic/traffic.cs` | Steady GET traffic, ~1 req/s; Ctrl+C to stop |
| Optional | `node tests/load/load-test.mjs --users 10 --duration 60` | GET-only page load, default `--base https://observability.carlcod.es`; results in `tests/load/results/` |
| Act 2 | `dotnet run /Users/carl/Work/Talks/Observability-is-a-Superpower/tests/load/burst.cs -- 5 8` | Rounds, posts per round. Concurrent Contact / service / event posts. Dev clone path: untracked, not in the stage clone |

Burst output: one line per post (`302 ACME-nnnn` or `500`), then
`<n> posts, <n> with reference, <n> without; duplicates: ...`. Bug present: about half return 500.
Fixed: every post returns 302. No failures: raise posts per round (`-- 5 16`).
Every successful post is stored and sends an owner email to smtp4dev.

## Database

No local `sqlcmd`; use the one inside the SQL Server container. Run in the clone whose AppHost is running.

Export the password (and port, for a GUI client) from the AppHost user secrets (zsh/bash):

```sh
export SQL_PASSWORD="$(dotnet user-secrets list --project src/SuperPower.AppHost | sed -n 's/^Parameters:sql-password = //p')"
export SQL_PORT="$(dotnet user-secrets list --project src/SuperPower.AppHost | sed -n 's/^Resources:sql:tcp:port = //p')"
```

Connect (interactive; `GO` runs a batch, `exit` quits):

```sh
docker exec -it -e SQLCMDPASSWORD="$SQL_PASSWORD" $(docker ps -q --filter name=sql-) \
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

## Planted problems (`demo/act-1-start`)

1. **Act 1, PII in logs.** `AcmeFormsController` logs `Form {Form} submitted by {Name} <{Email}> as {Reference}`.
   Fix: log form + reference only.
2. **Act 2, duplicate reference.** `FormSubmissionRepository` has no `WriteLock`, and the controller sends
   the email before inserting. Concurrent posts read the same max: both email the same `ACME-nnnn`,
   the second insert hits `IX_acmeFormSubmission_ReferenceNumber` (SqlException 2601) -> 500.
   Fix: write lock (as on `main`) and email after insert. Any fix is fine if the burst then shows no 500s.

Branches: `demo/act-2-start` = Act 1 fixed (fallback); `main` = both fixed. Same schema on all
branches; switching never needs a database reset.

## Steps

### Morning
1. Stop the dev AppHost (fixed ports 2525, 3000, 8091).
2. Stage clone: `git clone --depth 1 --no-single-branch file://$HOME/Work/Talks/Observability-is-a-Superpower ~/Work/Talks/stage`,
   `git switch demo/act-1-start`, copy `.cloudflared-token` in.
3. Start the AppHost; wait for first boot. Home loads, resources Running, `lgtm` NotStarted.
4. No rehearsal submissions left: reset per `docs/demo-setup.md` or recreate the clone.
5. Claude Code in the stage clone, default permission mode, `/fast` on. `/mcp`: aspire connected,
   grafana fails until go-live.
6. Tabs: site, Aspire (Structured logs, Traces), smtp4dev. VSCodium on the stage clone.
7. Tunnel check on the phone, then stop it. Fallback videos ready.

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

Expected: finds `Name` / `Email` on the log entry, edits `AcmeFormsController`, asks to restart
`site` (approve), re-submit a form, logs clean.
Fallback: `git switch demo/act-2-start`, restart `site`.

### 3. Go-live (4 min)
1. Aspire: start `lgtm`. Claude: `/mcp` -> reconnect grafana.
2. Start the tunnel; QR code slide. "Made-up details welcome, deleted after the talk."
3. Start `traffic.cs`.
4. Grafana: Dashboards > Demo > Demo site.
5. Point: Grafana holds only telemetry from go-live on.

### 4. Act 2: load breaks submissions (13 min; shorten here)
1. Attendees submit for a minute; form panel shows `stored`.
2. Run the burst; about half the posts return 500.
3. Dashboard: 5xx spike, form panel `failed`, Error logs panel SqlException.
4. smtp4dev: two emails with the same `ACME-nnnn`. "One of these customers got an error page."
5. Optional: database queries above (latest submissions, unique index).
6. Prompt:
   > Since we went live, some visitors get an error when they submit a form. Use our production
   > observability in Grafana to find out what is failing and why, fix it, and tell me how we
   > will know it is fixed.

   Expected: Loki duplicate key error, Tempo failing POSTs, explains the race and the email order,
   proposes the fix. Approve, restart `site`.
7. Verify (skip if behind): re-run the burst, no 500s. Agent confirms in Grafana with `increase`/`rate`
   (counter is cumulative).
8. Optional: "Who submitted forms today?" -> counts and references only.

Fallback: `git switch main`, restart `site`, re-run the burst.

### 5. Close (7 min)
Useful telemetry: outcome counter, spans with the reference, errors with context, nothing personal.
Agent access: read-only Grafana MCP, Aspire commands prompt, no database.
Console logs removed (ADR-0028): every output path can leak PII; close the ones you don't need.

### After the talk
1. Stop the tunnel and `traffic.cs`.
2. Stop the AppHost. Delete the stage clone, its SQL Server container and volume (`docker ps -a`,
   `docker volume ls`, names contain `sql`).
