# ADR-0003: Site content seeded by uSync first-boot import

## Status
Accepted (2026-10-07).

## Context
The site must populate itself on an empty database (fresh SQL container, SQLite clone, first
Azure deploy) and keep content in source control while the database is not. The Umbraco 17.7.1
template already references uSync 17.4.3.

Verified for uSync 17.4.3: `uSync:Settings:ImportOnFirstBoot` (default false),
`FirstBootGroup` (default `All`), root folder `uSync/v17/`, `ExportOnSave` default `All`,
content and settings handlers on by default, published state restored on import, template
`.cshtml` bodies not exported by default. First boot is tracked per database by a migration
plan that records itself even when the flag is off. Package migrations run before the uSync
first-boot import.

## Decision
- Commit `src/SuperPower.Web/uSync/v17/` (document types, data types, templates metadata,
  content including the Forms folder and the three form definitions).
- Set `ImportOnFirstBoot: true` in the site's base `appsettings.json` from the first commit
  that boots against a new database.
- View files (`Views/*.cshtml`) are ordinary source files in the site project.
- Content is authored in the backoffice; uSync exports on save; changes are committed.

## Alternatives
- Clean starter kit pattern (embedded `package.zip` + `PackageMigrationPlan` + publish
  handler): proven, but opaque in diffs and needs a new migration step per content change;
  rejected.
- Content created in code in a migration: diffable, but much more code for document types and
  templates; rejected.
- Committed SQLite database: not portable to SQL Server, binary diffs; rejected.

## Consequences
- A database that has booted once with the flag off never imports; reset by deleting the
  SQLite file or the SQL data volume. Documented in the README.
- uSync import failures are logged and boot continues; the smoke check verifies that pages
  exist.
- First boot on SQL Server shows a burst of database spans in telemetry.
