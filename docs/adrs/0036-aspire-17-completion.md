# ADR-0036: Aspire 17 completion: AppHost model, versions, agent skills

## Status
Proposed (2026-11-25, change `aspire-17-follow-ups`, FR-0007). Supersedes ADR-0035's "keep `Aspire.AppHost.Sdk`".

## Context
ADR-0035 moved the demo to Aspire 17.0.0 and left these problems:
- **Deprecated AppHost model.** It still uses `Aspire.AppHost.Sdk` (ASPIRE012) and
  `AddProject<TProject>` (CS0618).
- **Vulnerable transitive packages.** The build reports NuGet audit warnings for packages that come in
  through Umbraco 17.7.1:
  - High: SixLabors.ImageSharp 3.1.12, System.Security.Cryptography.Pkcs and System.Security.Cryptography.Xml.
  - Moderate: MessagePack 3.1.8.
- **Smoke check.** `tests/smoke/smoke.cs` uses `#:package Microsoft.Data.SqlClient` without a central
  version (NU1010).
- **Aspire skills.** Aspire 17 recommends installing them (`aspire agent init --skills aspire`). The
  `aspire-orchestration` skill starts, stops and restarts resources through the CLI.

The platform stays .NET 10 and Umbraco 17 LTS (PO 2026-11-24).

## Decision
- **AppHost model.** `Sdk="Microsoft.NET.Sdk"` with `Aspire.Hosting.AppHost` and `Aspire.Hosting.Dotnet`
  17.0.0. No `ProjectReference` to the services. `AddDotnetProject("api", "../SuperPower.Api/SuperPower.Api.csproj")`
  and `AddDotnetProject("site", "../SuperPower.Web/SuperPower.Web.csproj")`; names and chained calls are
  unchanged. `AspireUseCliBundle` stays.
- **Library versions:**

  | Package | Version |
  |---|---|
  | Umbraco.Cms.* | 17.8.2 |
  | uSync | 17.6.3 |
  | OpenTelemetry.* | 1.21.0 |
  | Microsoft.Extensions.Http.Resilience | 10.11.1 |
  | Microsoft.Extensions.ServiceDiscovery | 10.11.1 |
  | xunit.v3 | 4.0.3 |
  | Microsoft.Data.SqlClient (new `PackageVersion`, smoke check only; the version Umbraco resolves) | 6.1.6 |

  Azure.Monitor.OpenTelemetry.AspNetCore 1.6.0 and Serilog 4.4.0 are already current.
- **Microsoft.Extensions stays on the 10.x line.** 11.x targets net10.0 but depends on 11.0 runtime
  libraries, which would mix .NET 11 assemblies into a .NET 10 Umbraco site.
- **Container images:**

  | Image | Version |
  |---|---|
  | `grafana/otel-lgtm` | 0.41.1 |
  | `grafana/mcp-grafana` | 2.4.0 |
  | OpenTelemetry Collector (`CollectorTag`) | 0.165.0 |
  | `rnwood/smtp4dev` | 3.16.1 |

  These ship after the demo, each verified against all three acts.
- **Aspire agent skills are not installed in the repo.** The README says they exist, why the repo leaves
  them out, and that attendees may use them in their own projects.
- **New read tool pre-approved.** `.claude/settings.json` pre-approves `mcp__aspire__get_integration_docs`,
  the Aspire MCP's new read tool (ADR-0022 rule: read tools pre-approved, resource commands prompt).

## Alternatives
- **Keep `Aspire.AppHost.Sdk`, or suppress ASPIRE012 and CS0618.** Less work now, but the SDK will be
  removed in a later release and attendees see warnings; rejected.
- **Microsoft.Extensions 11.x.** Newest, but mixes .NET 11 runtime libraries into a .NET 10 site; rejected.
- **Pin the vulnerable transitive packages directly.** Overrides Umbraco's own choices. Not needed:
  Umbraco 17.8.2 clears them (scratch build, 2026-11-24). Kept as the fallback if one remains.
- **Install the Aspire skills.** This is Aspire 17's recommended agent setup. Rejected (PO 2026-11-25):
  - The agent could restart resources itself, which removes the talk's "agent asks, presenter restarts"
    moment (ADR-0033).
  - Skills act through shell commands, bypassing the MCP approval prompts (ADR-0022).
- **Update the images before the next demo.** Newest observability stack, but the most risk to the acts
  just before a demo; deferred until after it.

## Consequences
- Building the AppHost no longer builds `site` and `api` through project references. `aspire run` /
  `aspire start` build them. `dotnet build SuperPower.slnx` still builds everything.
- Package updates resume after the ADR-0001 freeze for this change only. Package versions stay pinned.
- New images may change Grafana provisioning paths, MCP flags or collector config keys. Each act is
  re-checked before the new images are used on stage.
- Attendees who want the skills install them themselves. The repo's agent stays prompt-gated for
  resource commands.
