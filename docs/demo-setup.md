# Demo setup

Commands run from the repo root. Full talk script: `docs/presenter/runthrough.md`.

## Setup

1. Docker Desktop running, images pulled (README "Pre-pull the container images"), port 2525 free.
   Fresh clone: copy the tunnel token from your working clone, e.g.
   `cp ../Observability-is-a-Superpower/.cloudflared-token .`
2. Stop every AppHost (from any clone: they share fixed ports and `oisp-*` containers) and reset
   containers, volumes and Umbraco TEMP:
   ```sh
   pkill -f 'SuperPower.AppHost/bin/.*/SuperPower.AppHost'; sleep 5
   pkill -f 'aspire run .*SuperPower.AppHost.csproj'
   docker ps -aq --filter name=oisp- | xargs docker rm -f -v
   docker volume rm oisp-sql-data oisp-cache-data
   rm -rf src/SuperPower.Web/umbraco/Data/TEMP
   ```
3. Start and wait until all resources are Running (first start takes a few minutes):
   ```sh
   aspire start --apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj
   ```
4. Before loading any page: `dotnet run tests/smoke/smoke.cs` -> `ALL CHECKS PASSED`.
5. Tunnel (needs `.cloudflared-token` in the repo root):
   ```sh
   cloudflared tunnel run --token-file .cloudflared-token
   ```
   Load the public URL on your phone, then Ctrl+C.
6. Break the code (lines marked `// Demo Code : Act N`):
   - `src/SuperPower.ServiceDefaults/Extensions.cs`: comment out `logging.AddProcessor(new PersonalDataLogProcessor(...))`.
   - `src/SuperPower.Web/Forms/Storage/FormSubmissionRepository.cs`: comment out `Scope.WriteLock(AcmeLocks.Submissions);`.
7. Rebuild `site` in the Aspire dashboard (Rebuild, not Restart).
8. Move `docs/` out of the repo, then start `claude` in the repo root. `claude mcp list` shows `grafana` connected.

## Test Local X-ray

1. Submit a Service enquiry with made-up details: a reference is shown, and smtp4dev has the same reference.
2. Ask Claude:
   ```text
   How many form submissions since the start, by form and outcome? Use `increase`.
   ```
   ```text
   Show the trace for ACME-nnnn: spans, durations, how long the email took.
   ```
   ```text
   Which pages were forms submitted from? Use the `stored as` logs.
   ```
   First Home load only:
   ```text
   Show the latest Home page trace and its database calls.
   ```

## Test Act 1: personal data in logs

1. Submit the Contact form with made-up details.
2. Grafana or Aspire Structured logs: the `Form Submitted` entry shows Name and Email in plain text.
   Ask Claude:
   ```text
   Do any logs or spans from form posts contain a name, email address or message text?
   ```
   Main prompt:
   ```text
   We are about to put this site on the public internet. Using our telemetry in Grafana, check whether form submissions leave any personal data in our logs or traces. If they do, fix it at the source and prove the fix from the telemetry.
   ```
3. Uncomment the `PersonalDataLogProcessor` line, Rebuild `site`.
4. Submit Contact again: the same entry shows `[REDACTED]` and `[REDACTED:<id>]`.
   Ask Claude:
   ```text
   Show the latest `Form Submitted` entry. Are Name, Email and Message masked?
   ```
   ```text
   Do two posts with the same email show the same `[REDACTED:<id>]`?
   ```
   ```text
   Does the `stored as` log still show form, page and reference?
   ```
5. Comment the line out again and Rebuild `site` if you will run this on stage.

## Test Go-live

1. Start the tunnel and `dotnet run tests/traffic/traffic.cs`.
2. Ask Claude:
   ```text
   Show the request rate for the last 5 minutes by page type, and any failures since go-live.
   ```

## Test Act 2: load breaks submissions

1. Run the burst:
   ```sh
   dotnet run tests/load/burst.cs -- 5 8
   ```
   About half the posts return `500`. None fail: try `-- 5 16`.
2. Grafana Demo site dashboard: 5xx error rate rises, `failed` submissions appear, Error logs show the SqlException.
   Main prompt:
   ```text
   Since we went live, some visitors get an error when they submit a form. Use our production observability in Grafana to find out what is failing and why. Don't fix it - just identify the issue.
   ```
3. Uncomment the `Scope.WriteLock` line, Rebuild `site`, run the burst again: every post returns `302 ACME-nnnn`.
   Ask Claude:
   ```text
   Did any form submissions fail since the fix? Use `increase`.
   ```
   Optional:
   ```text
   Who submitted forms today?
   ```
   Expect counts and references only.
4. Comment the line out again and Rebuild `site` if you will run this on stage.
