# Presenter runthrough (private; contains demo answers)

Talk: Fri 16 Oct 2026, 13:30-14:15 BST. Git-ignored; the agent's settings deny reading it.
Never copy into CLAUDE.md, PLANNING.md, openspec/, docs/ or a demo branch.

## Story

| # | Segment | Where | Min |
|---|---|---|---|
| 0 | Why: observability is added after prod breaks; use it while building | slides | 6 |
| 1 | Local X-ray: site, Aspire dashboard, a form post trace | Aspire | 5 |
| 2 | Act 1: agent finds personal data in logs and fixes it (Aspire MCP) | Aspire | 10 |
| 3 | Go-live: tunnel, QR code, Grafana + dashboard, traffic | Grafana | 4 |
| 4 | Act 2: load breaks submissions; agent diagnoses and fixes (Grafana MCP) | Grafana | 13 |
| 5 | Close: what telemetry is useful, PII stays out, read-only agent access | slides | 7 |

Act 2 is the one to shorten (skip step 4.6 and the verification burst if behind).

## The planted problems (on `demo/act-1-start` only)

1. **PII in logs (Act 1).** `AcmeFormsController`: the stored-submission log line
   `Form {Form} stored as {Reference}` becomes
   `Form {Form} submitted by {Name} <{Email}> as {Reference}` (Name and Email taken from the
   submitted fields). Shows in the Aspire structured logs as separate `Name` / `Email` fields.
   Expected fix: remove Name/Email from the message (log form + reference only).
2. **Duplicate reference under load (Act 2).** `FormSubmissionRepository`: the
   `scope.WriteLock(800001)` call is removed, and the controller sends the owner notification
   *before* inserting the row (allocate number -> send email -> insert). Two concurrent posts read
   the same max, both send an email with the same reference, the second insert hits unique
   index `IX_acmeFormSubmission_ReferenceNumber` -> SqlException 2601 -> 500 for that visitor.
   Visible: 5xx panel and form panel (`failed`), Loki error log with the duplicate key value,
   Tempo error span on the form POST, smtp4dev shows two emails with the same `ACME-nnnn`.
   Expected fix: serialise allocation (Umbraco write lock, as on `main`) and send the email
   after the row is stored. Any correct fix the agent proposes is fine if the burst then
   reports `failed=0`.

Schema is identical on all branches (same migration steps), so switching branches never needs
a database reset.

## Branches and stage clone (task 6.1, after the change is applied and merged)

- `demo/act-1-start` = `main` + one "stage" commit:
  - both plants above;
  - strip hints and planning: delete `docs/adrs/0026-submission-reference.md` and its references,
    the `WriteLock` comment, `openspec/changes/` (active changes), `PLANNING.md`,
    `.claude/agents/`, `.claude/commands/opsx/`, `docs/research/`;
  - CLAUDE.md reduced to a stage version: project description, talk promises, stack, telemetry
    rule ("never recorded: Name, Message, Notes, Company, Website, email address"), no Roles /
    Apply-phase team / PLANNING reference.
- `demo/act-2-start` = `demo/act-1-start` + revert of plant 1 only (Act 1 fallback).
- `main` = final (both fixed). Attendees clone this.
- Stage clone, no history, no `.presenter/`, no Claude memory (different path):
  `git clone --depth 1 --no-single-branch file://$HOME/Work/Talks/Observability-is-a-Superpower ~/Work/Talks/stage`
  then `git switch demo/act-1-start`. Copy `.cloudflared-token` into it.
- Stop the dev AppHost before starting the stage one (smtp4dev 2525, Grafana 3000, MCP 8091
  are fixed ports). The stage clone gets its own fresh SQL Server database; first boot imports
  content (allow a few minutes; do it before the talk).

## Before the talk (morning)

1. Stage clone on `demo/act-1-start`; `aspire run --apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj`.
2. First boot done, Home loads, all resources Running; `lgtm` NotStarted, `grafana-mcp` Waiting.
3. Delete any rehearsal submissions: reset per `docs/demo-setup.md`, or recreate the stage clone.
4. Claude Code in the stage clone, **default permission mode** (not auto), `/fast` on. `/mcp`
   shows aspire connected; grafana fails until go-live (expected).
5. Tabs: site, Aspire dashboard (Structured logs, Traces), smtp4dev inbox. Grafana tab opened
   at go-live. VSCodium on the stage clone.
6. Tunnel smoke test: `cloudflared tunnel run --token-file .cloudflared-token`, open the public
   URL on the phone, stop the tunnel again.
7. Fallback videos ready per act.

## Segment 1: Local X-ray (5 min)

1. Site: open a service page, submit Service enquiry with made-up data -> "Thank you, we'll be
   in touch. Your reference is ACME-0001."
2. smtp4dev: the owner notification with the same reference.
3. Aspire Traces: the POST trace: `acme.forms.submit` (form, outcome, reference), SQL spans,
   `acme.forms.notify`. Structured logs: the submission log line (do not linger on it).
4. Metrics: `acme.forms.submissions` by form and outcome.

## Act 1: personal data in logs (10 min)

Say: "Before this goes public tonight, let the agent check what our telemetry knows about
people."

Prompt:
> We are about to put this site on the public internet. Using the Aspire telemetry, check
> whether form submissions leave any personal data in our logs or traces. If they do, fix it at
> the source and prove the fix from the telemetry.

Expected:
- Agent lists structured logs, finds `Name` and `Email` fields on the submission log entry.
- Edits the log line in `AcmeFormsController` to form + reference only.
- Restart: agent asks to restart `site` (approve), or restart it in the dashboard.
- Re-submit a form (presenter or agent asks you); agent queries logs again: no Name/Email.
- Point: this was caught locally, before any of it reached the shared platform.

Fallback: `git switch demo/act-2-start`, restart `site`.

## Go-live (4 min)

1. Aspire dashboard: start `lgtm` (grafana-mcp follows). In Claude: `/mcp` -> reconnect grafana.
2. `cloudflared tunnel run --token-file .cloudflared-token`; show the QR code slide.
   Say: live demo, made-up details welcome, everything deleted after the talk.
3. Second terminal: `dotnet run tests/traffic/traffic.cs` (steady page traffic).
4. Grafana: Dashboards > Demo > Demo site. Request rate per page type fills in.
5. Point: Grafana only holds telemetry from now on; the personal data from before never got here.

## Act 2: load breaks submissions (13 min)

1. Let attendees submit for a minute; form panel shows `stored`.
2. Third terminal: `dotnet run tests/traffic/burst.cs -- --count 50 --parallel 10`
   Expected: `ok=4x failed=x`, failures are HTTP 500. (Rehearse: if no failures, raise
   `--parallel` to 20.)
3. Dashboard: 5xx panel spikes, form panel shows `failed`, Error logs panel shows SqlException.
4. smtp4dev: two notifications with the same `ACME-nnnn`. Say: "One of these customers got an error page."
5. Prompt:
   > Since we went live, some visitors get an error when they submit a form. Use our production
   > observability in Grafana to find out what is failing and why, fix it, and tell me how we
   > will know it is fixed.

   Expected: Loki errors -> duplicate key on `IX_acmeFormSubmission_ReferenceNumber`; Tempo
   failing POST traces; agent reads repository/controller, explains the read-max-then-insert
   race and the email-before-save ordering, proposes the fix. Approve, restart `site`.
6. Verify: re-run the burst -> `ok=50 failed=0`. Ask the agent to confirm in Grafana that no new
   failed submissions or 5xx occurred after the restart (counter is cumulative: it should use
   `increase`/`rate`).
7. Optional if time: "Who submitted forms today?" -> agent can only answer with counts and
   references; no identities in Grafana.

Fallback: `git switch main`, restart `site`, re-run burst.

## Close (7 min)
Useful telemetry: outcome-labelled counter, spans with business keys (reference), errors with
context, nothing personal. Agent access: read-only Grafana MCP, Aspire commands prompt, no DB.

## After the talk
1. Stop the tunnel (Ctrl+C) and `traffic.cs`.
2. Stop the AppHost. Delete the stage clone and its persistent SQL Server container and volume
   (`docker ps -a`, `docker volume ls`, names contain `sql`). smtp4dev and LGTM data are in memory.
3. Delete this folder's plant notes if publishing anything from the dev clone.

## Rehearsal log (task 6.2)
| Date | Act | Duration | Notes |
|---|---|---|---|
