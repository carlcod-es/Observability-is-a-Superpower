# Review follow-ups: rework-aspire-first

Minors and nits from block reviews (CLAUDE.md "Reviews"). Read at the start of every apply session.

## Block 2 (tasks 2.1-2.3, AppHost + collector), reviewer APPROVED 2026-10-15
- RESOLVED 2026-10-15 (PO: reword; spec + README updated). minor (PO): before go-live `grafana-mcp` shows Waiting, not "not started"; spec
  `local-environment` "One-command start" says the production-stage resources show as not
  started. Design D2 anticipates Waiting. Proposed: reword the spec to "not running (not
  started or waiting for the platform)" via `/opsx:update`; PO decides.
- nit: collector logs a "no such host" export error per LGTM batch before go-live (accepted,
  design Risks).
- RESOLVED 2026-10-15 (superseded by supervisor M1): metric counters are cumulative and include
  pre-go-live totals; act prompts must not claim clean metrics.

## Block 3 (task 3.1, agent config), reviewer APPROVED 2026-10-15
- none. Reviewer checked against the brief's contract only; section supervisor covers spec/ADR.

## Docs block (tasks 5.1, 5.2), reviewer CHANGES REQUESTED (major: port wording) -> fixed, re-review APPROVED 2026-10-15
- RESOLVED 2026-10-15. nit: README intro (~line 47) says LGTM and the Grafana MCP server "show as not started";
  `grafana-mcp` shows Waiting. Fix with the block 2 spec-wording minor: "`lgtm` shows as not
  started and `grafana-mcp` as waiting for it".
- nit: project-setup uses "go-live" (defined inline by context); left as is.

## Block 4 (task 4.1, smoke check), reviewer APPROVED 2026-10-15
- Deviation accepted by the Architect: pre-request `/smoke-pre/` (404) instead of `/about-us/`,
  which warmed the content cache and removed the Home DB spans; design D4 and task 4.1 updated.
- minor: `tests/smoke/smoke.cs` ~170-173 treats every `lgtm` state other than `NotStarted` as
  already running; `Starting`/`Waiting` get no readiness wait. Optional: wait for ready on those.
- minor: `aspire` CLI calls via `Run` have no timeout (same as existing Toxiproxy calls).
- nit: pre-request failure skips the `lgtm` start with a visible note.

## Supervisor, sections 1-5, CHANGES REQUESTED 2026-10-15
- RESOLVED 2026-10-15 (PO option a: specs, task 6.3, proposal, ADR-0022, README, project-setup
  reworded; supervisor re-review APPROVED). MAJOR M1: cumulative metrics exported after go-live include requests from
  before it (e.g. `http.server.request.duration` count includes the pre-go-live Contact POST),
  contradicting spec `local-environment` "No earlier telemetry" and `agent-telemetry-access`
  "Nothing from before go-live" for metrics; risk for task 6.3. Upgrades the block 2 nit.
- RESOLVED 2026-10-15. minor m1: `grafana-mcp` Waiting vs "not started" (spec + README:47-48); see block 2/docs entries.
- minor m2: fixed (Architect): `openspec/config.yaml` context, PLANNING.md Azure unknowns.
- minor m3: fixed (Architect): `add-demo-foundation` design.md D-entry and review-followups
  `umbracoDbDSN` item annotated with ADR-0022 / task 6.2.
- minor m4: smoke "no pre-start telemetry" checks Loki only; traces covered by task 6.3
  (note in verification.md).
- nit n1: fixed (Architect): CLAUDE.md Toxiproxy commands wording.
- nit n2: README may overstate `select_apphost` need; observe in task 6.1, correct if wrong.
- nit n3: record task 2.3 timings (Grafana health 9 s, grafana-mcp Running 13 s, site pid
  unchanged) in verification.md.
- PO note (6.2): `site` env holds `ConnectionStrings__umbracoDbDSN` with the sa password; unknown
  whether `list_resources` returns env vars. `collector` (`ASPIRE_API_KEY`) and `grafana-mcp`
  (`GRAFANA_PASSWORD=admin`) also carry non-database secrets.

## Supervisor re-review, sections 1-5, APPROVED 2026-10-15
- minor: design.md Goals and CLAUDE.md Stack lacked the metrics exception: fixed (Architect).
- nit: stale review-followups entries marked resolved; PLANNING.md Azure deploy sentence removed.
- PO risk: task 6.3 metric answers include pre-go-live requests (allowed by spec).
- Open: m4 (traces not separately checked in smoke), n2 (`select_apphost` need, observe in 6.1).
