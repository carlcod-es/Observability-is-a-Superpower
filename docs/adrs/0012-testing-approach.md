# ADR-0012: Testing approach

## Status
Accepted (2026-10-07; revised 2026-10-13 — ADR-0016 removed the forms package, so all tests
live in `tests/SuperPower.Web.Tests`)

## Context
Days to the talk. The riskiest logic in this change is validation and submission mapping;
the riskiest integration is startup (install, migrations, seeding) on two databases.

## Decision
- One xUnit v3 test project `tests/SuperPower.Web.Tests` for site logic that needs no Umbraco
  runtime: the form models' DataAnnotations rules, field-values-to-JSON mapping, the
  past-event rule and upcoming-events ordering with a fake `TimeProvider` (ADR-0016,
  ADR-0014).
- Startup and end-to-end behaviour verified by scripted smoke checks against the running
  AppHost and the standalone SQLite site (HTTP requests for pages, a valid and an invalid
  submission), recorded as tasks, not automated test suites.
- No Umbraco integration-test harness in this change.
- Runner (added 2026-10-08): xunit.v3 4.x on
  Microsoft.Testing.Platform, opted in by a root `global.json`
  (`"test": { "runner": "Microsoft.Testing.Platform" }`, no SDK pin). The .NET 10 SDK's
  `dotnet test` no longer runs MTP projects in VSTest mode. Only `xunit.v3` is referenced;
  `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio` (VSTest only) are not.

## Alternatives
- Umbraco integration tests (`Umbraco.Cms.Tests.Integration`): high setup cost for the time
  available; rejected for now.
- Aspire testing (`Aspire.Hosting.Testing`) for an automated end-to-end check: valuable, but
  container start time and setup; candidate for a later change.

## Consequences
- Regression risk on startup paths is covered by manual smoke checks before each demo branch
  is cut.
