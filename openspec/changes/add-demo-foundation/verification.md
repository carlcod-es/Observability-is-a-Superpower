# Verification log: add-demo-foundation

Recorded observations for spec scenarios not covered by unit tests (design.md "Bar and test
strategy"). Evidence comes from the block reviews; the reviewer ran each check independently.

## Section 1

- `add-demo-foundation` task 1.1, 2026-10-08: `aspire --version` prints 13.6.0.
- `add-demo-foundation` task 1.2, 2026-10-08: root `dotnet build` 0 errors; AppHost started,
  `aspire describe` shows `site` Running/Healthy; `/health`, `/alive`, `/`, `/umbraco` return 200.
- `add-demo-foundation` task 1.3, 2026-10-08, default config, no env overrides (spec
  `local-environment` "Site logs are structured", log half of "Page request trace"):
  - start-up entries in `aspire otel logs site` with named properties as separate attributes
    (`CacheName`, `address`, `ContentRoot`); no message contains stray quotes;
  - `GET /`: "Request starting" and "Request finished" entries carry the trace id and span id of
    the GET / server span in `aspire otel traces`;
  - no duplicate entries; only `Microsoft.Hosting.Lifetime` and
    `Microsoft.AspNetCore.Hosting.Diagnostics` among `Microsoft.*` categories.
  - Database spans in that trace: pending task 2.4 (SQL resource).

## Section 3

- `add-demo-foundation` task 3.2, 2026-10-09, fresh SQLite (reviewer, scratch database): migration
  "SuperPower.Forms" ran once; five element types with `fieldAlias`, `label` (mandatory),
  `required`, `pattern`, `patternMessage`; Select has `options` (Repeatable Textstrings); second
  start: no migration run, counts unchanged (6 content types, 2 data types, 28 property types).
- `add-demo-foundation` task 3.3, migration part, same run: Block List "SuperPower Forms - Fields"
  lists the five element type keys (config shape as the PO's backoffice-created
  `compPageMainContentBlockList`); `superPowerForm` has `fields` and `successMessage`, no
  template, not allowed at root. Editor UI check: pending PO observation.

## Section 2

- `add-demo-foundation` task 2.1, 2026-10-09 (reviewer, fresh volume): `sql`, `umbraco`, `site`
  Running/Healthy; site env targets `Database=umbraco` on the container with provider
  `Microsoft.Data.SqlClient`; unattended install, "SuperPower.Forms" migration and uSync first
  boot in the logs; `/` and `/umbraco` 200; 77 tables in `umbraco`. Restart: no reinstall.
- `add-demo-foundation` task 2.2, 2026-10-09: worker sent mail from the site through a temporary
  endpoint (removed): arrived in smtp4dev, 13 ms. Reviewer: site env has SMTP Host localhost,
  Port 2525, SecureSocketOptions None, From; curl SMTP via 2525 arrives in the Inbox; smtp4dev
  `/api/server`: TLS None, settings locked; in-memory store per container log.
- `add-demo-foundation` task 2.3, 2026-10-09 (reviewer, curl SMTP via 2525): baseline 0.013 s;
  Slow 6.04 s, arrives; Break fails in 0.005 s (connection reset), nothing arrives; Heal 0.007 s,
  arrives; proxy starts with no toxics; Slow and Break repeat-safe. Worker, from the site: Slow
  6052 ms, Break fails in 14 ms (`SmtpProtocolException`), Heal 11 ms.
- `add-demo-foundation` task 3.4, 2026-10-09: new step `...4002` ran on a fresh SQLite database,
  on the existing SQLite database (only the new step) and on the existing SQL Server database.
  Columns Id, FormKey (indexed), PageKey, SubmittedUtc, FieldsJson (NVARCHAR(MAX) / TEXT); no
  client columns. Reviewer: raw `FieldsJson` byte-identical on SQLite and SQL Server for the
  same submission (quotes, non-ASCII, newline).
- `add-demo-foundation` task 2.4, 2026-10-09 (spec `local-environment` "Page request trace"):
  first GET / after start: server span + 2 SqlClient SELECT child spans, `db.query.text`
  parameterised; cached GET /: 1 span. Background database traces unfiltered ~76/min (worker,
  4-minute idle window: distributed jobs, cache instructions, locks, webhooks), filtered 0
  (worker 4 min, reviewer 2.5 min). `/health`, `/alive`, `/css`, `/umbraco/backoffice`: no
  traces, no Request logs. Exporter switch: code only (Azure Monitor path in
  `add-production-environment`).
