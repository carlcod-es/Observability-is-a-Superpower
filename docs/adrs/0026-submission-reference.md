# ADR-0026: Submission reference

## Status
Accepted (2026-10-15)

## Context
Spec `form-submission`: each stored submission gets a reference `ACME-` + at least four
digits, unique across all forms, shown to the visitor and used in the owner notification.
Submissions must work on SQLite and SQL Server with NPoco `Sql<ISqlContext>` builders, no raw
SQL (CLAUDE.md). The demo database is SQL Server under concurrent submissions.

## Decision
- New int column `ReferenceNumber` on `acmeFormSubmission` with unique index
  `IX_acmeFormSubmission_ReferenceNumber`. Displayed as `ACME-{n:D4}`; not stored as text.
- Allocation: inside the submission's Umbraco scope, take an Umbraco write lock on a site lock
  row, read the highest `ReferenceNumber`, add one, insert. The lock row (id 800001, name
  `AcmeSite`) is inserted into `umbracoLock` by the migration. The write lock
  serialises allocation on both providers; the unique index is the backstop.
- Migration plan "Acme.Site" gains a step: add the column (nullable), set `ReferenceNumber =
  Id` for existing rows, create the unique index, insert the lock row. Each part is skipped if
  already present. The table DTO carries the column and index, so a fresh install creates them
  in the first step.

## Alternatives
- Reference derived from the identity `Id` after insert: needs insert-then-update, and a
  unique index on a column that is NULL between the two statements collides on SQL Server;
  rejected.
- Random code (e.g. 6 characters): unique enough, but not readable or ordered on stage;
  rejected.
- Retry on duplicate key: provider-specific exception handling inside an Umbraco scope;
  rejected.
- Database sequence: not available on SQLite; rejected.

## Consequences
- Submissions are serialised for the length of one insert; irrelevant at demo volumes.
- Lock id 800001 must not clash with Umbraco's lock ids (Umbraco uses negative ids).
