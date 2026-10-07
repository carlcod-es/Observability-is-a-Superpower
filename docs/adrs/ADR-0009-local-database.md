# ADR-0009: Local database in the AppHost and file-based fallback

## Status
Accepted (2026-10-07)

## Context
CLAUDE.md fixes `AddAzureSqlServer(...).RunAsContainer()` so the same AppHost model deploys to
Azure SQL. Spec `local-environment` requires a file-based mode for attendees without a
container runtime. Umbraco reads `ConnectionStrings:umbracoDbDSN` and
`umbracoDbDSN_ProviderName`.

## Decision
- AppHost: `AddAzureSqlServer("sql").RunAsContainer(c => c.WithDataVolume()
  .WithLifetime(ContainerLifetime.Persistent)).AddDatabase("umbraco")`.
- Site reference: `WithReference(db, connectionName: "umbracoDbDSN")` plus environment
  `ConnectionStrings__umbracoDbDSN_ProviderName=Microsoft.Data.SqlClient`.
- Standalone site run: `appsettings.Development.json` keeps the SQLite connection string; the
  AppHost's environment variables override it.
- Reset: README documents removing the data volume to get a fresh, re-seeded database.

Alternatives considered:
- `AddSqlServer` only: simpler locally but does not map to Azure SQL on deploy; rejected.
- Session lifetime without a volume (fresh DB every start): deterministic but adds install and
  seeding time to every start on stage; rejected in favour of an explicit reset.

## Consequences
- Stage starts are fast; a rehearsal reset is one documented command.
- Persistent containers outlive the AppHost; README explains how to stop and remove them.
