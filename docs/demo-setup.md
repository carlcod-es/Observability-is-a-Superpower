# Demo setup

Commands run from the repo root. Full talk script: `docs/presenter/runthrough.md`.

## Setup

1. Docker Desktop running, images pulled (README "Pre-pull the container images"), port 2525 free.
2. Stop the AppHost and reset:
   ```sh
   aspire stop --apphost src/SuperPower.AppHost/SuperPower.AppHost.csproj
   docker rm -f oisp-sql && docker volume rm oisp-sql-data
   docker rm -f oisp-cache && docker volume rm oisp-cache-data
   docker rm -f -v oisp-queue
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

## Test Act 1: personal data in logs

1. Submit the Contact form with made-up details.
2. Grafana or Aspire Structured logs: the `Form Submitted` entry shows Name and Email in plain text.
3. Uncomment the `PersonalDataLogProcessor` line, Rebuild `site`.
4. Submit Contact again: the same entry shows `[REDACTED]` and `[REDACTED:<id>]`.
5. Comment the line out again and Rebuild `site` if you will run this on stage.

## Test Act 2: load breaks submissions

1. Run the burst:
   ```sh
   dotnet run tests/load/burst.cs -- 5 8
   ```
   About half the posts return `500`. None fail: try `-- 5 16`.
2. Grafana Demo site dashboard: 5xx error rate rises, `failed` submissions appear, Error logs show the SqlException.
3. Uncomment the `Scope.WriteLock` line, Rebuild `site`, run the burst again: every post returns `302 ACME-nnnn`.
4. Comment the line out again and Rebuild `site` if you will run this on stage.
