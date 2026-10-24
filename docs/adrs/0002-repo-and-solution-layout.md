# ADR-0002: Repo and solution layout

## Status
Accepted (2026-10-07).

## Context
The site exists as `src/SuperPower.Web` with `src/SuperPower.slnx` and a
`Directory.Packages.props` inside the web project folder. Tests will live outside `src/`,
and every project must share one set of package versions.

## Decision
```
SuperPower.slnx                  (moved from src/)
Directory.Packages.props         (moved from src/SuperPower.Web/; central versions)
Directory.Build.props            (net10.0, nullable, implicit usings)
src/SuperPower.AppHost           (Aspire AppHost, project-based)
src/SuperPower.ServiceDefaults   (Aspire ServiceDefaults)
src/SuperPower.Web               (Umbraco site; uSync files; views; the three fixed forms)
tests/SuperPower.Web.Tests       (xUnit; site logic incl. forms, ADR-0012)
docs/adrs, docs/research
```
Casing is `SuperPower.*` for all identifiers.

## Alternatives
- Keep the solution and props under `src/`: tests outside `src/` would not inherit central
  versions; rejected.
- File-based AppHost (`apphost.cs`): fewer files, but a solution already exists and a project
  AppHost is easier to show and debug in an IDE; rejected.
- Separate package and site repos: no release is planned; rejected.

## Consequences
- One `dotnet build` at the root builds everything; one place to pin versions (ADR-0001).
- The web project's template `Directory.Packages.props` comment about the ICU version moves
  with the file.
