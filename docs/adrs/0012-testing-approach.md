# ADR-0012: Testing approach

## Status
Accepted (2026-10-07)

## Context
Nine days to the talk. The riskiest logic in this change is validation and submission mapping;
the riskiest integration is startup (install, migrations, seeding) on two databases.

## Decision
- xUnit v3 test project `tests/SuperPower.Forms.Tests` for package logic that needs no
  Umbraco runtime: validator rules, alias uniqueness, posted-value mapping, submission JSON.
- xUnit v3 test project `tests/SuperPower.Web.Tests` for site logic that needs no Umbraco
  runtime: the event booking handler's rules (past event, online fixed place, booking row
  values) with a fake `TimeProvider` (added 2026-10-08; ADR-0013, ADR-0015).
- Startup and end-to-end behaviour verified by scripted smoke checks against the running
  AppHost and the standalone SQLite site (HTTP requests for pages, a valid and an invalid
  submission), recorded as tasks, not automated test suites.
- No Umbraco integration-test harness in this change.

## Alternatives
- Umbraco integration tests (`Umbraco.Cms.Tests.Integration`): high setup cost for the time
  available; rejected for now.
- Aspire testing (`Aspire.Hosting.Testing`) for an automated end-to-end check: valuable, but
  container start time and setup; candidate for a later change.

## Consequences
- Regression risk on startup paths is covered by manual smoke checks before each demo branch
  is cut.
