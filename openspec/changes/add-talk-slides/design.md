## Context

See proposal.md - Why. Inputs: talk promises and stack in `CLAUDE.md`, `README.md`,
`docs/project-setup.md`, ADR-0007, ADR-0008, ADR-0010, ADR-0017. Slot: 45 minutes, live demo,
Claude Code CLI on stage. The demo is built in three changes (`add-demo-foundation`,
`add-form-emails`, `add-production-environment`); recovery points are `demo/act-N-*` branches,
not yet created (`PLANNING.md` Todos).

Rule carried from `PLANNING.md`: no demo answers (planted problems, causes, fixes) in repo
files; the on-stage agent may read them.

## Goals / Non-Goals

**Goals:**
- One outline that covers every talk promise, with a time budget that adds up to 45 minutes.
- Slide lead-ins name what the audience is about to see, not what the agent will find.
- Act boundaries match the planned `demo/act-N-*` branches, so a failed act has a restart point.

**Non-Goals:**
- Slide design, theme, final wording, images. The PO builds these in the slide tool.
- Demo script (prompts, clicks, planted problems). Presenter-only notes, outside the repo.
- Choosing the prod hosting; Act 5 is written for the tunnel baseline with an Azure variant
  note.

## Decisions

### D1. Outline is one Markdown file in this change directory
`openspec/changes/add-talk-slides/slides-outline.md`. The PO said work in the repo for now; the
change directory keeps it out of `docs/` (published attendee docs) and is easy to move out.
Alternatives: `docs/talk/` (lost: suggests it ships to attendees); outside the repo (the PO's
eventual target, deferred).

### D2. Slide tool neutral
Plain Markdown, one `##` heading per slide with fixed fields: On slide, Notes, Visual, Time.
No ADR: no technology is chosen; the PO picks the slide tool.

### D3. Act structure from the talk promises

| # | Segment | Type | Min | Promise covered | Branch |
|---|---|---|---|---|---|
| 0 | Opening | slides | 5 | why: observability after prod breaks | - |
| 1 | Local X-ray in one command | lead-in + demo | 1 + 5 | Umbraco + Aspire + OTel, one local environment | `demo/act-1-*` |
| - | What telemetry is useful | slides | 3 | what telemetry is genuinely useful | - |
| 2 | Build a feature, see it work | lead-in + demo | 1 + 8 | build a feature, diagnose, verify fixes | `demo/act-2-*` |
| 3 | The agent gets X-ray vision | lead-in + demo | 2 + 7 | agent uses telemetry via MCP to find, fix, validate | `demo/act-3-*` |
| 4 | Personal data stays out | lead-in + demo | 2 + 4 | avoid collecting personal form data | `demo/act-4-*` |
| 5 | Read-only agent on production | lead-in + demo | 1 + 3 | controlled read-only AI access to prod | `demo/act-5-*` |
| - | Close | slides | 3 | takeaways, repo link | - |

Total: slides 18 min (incl. lead-ins), demo 27 min. Leaves no buffer; Act 2 and Act 3 are the
places to cut if behind (noted per slide).

Content per act, drafted from the promises (planted problems are PO placeholders):
- Act 1: one `aspire run`; site, Aspire dashboard, a Home page trace with SQL spans and linked
  logs; same data in Grafana. Umbraco-specific points: Umbraco emits no spans itself; logs go
  through Serilog and are forwarded (ADR-0007); background SQL noise filtered (ADR-0008).
- Act 2: feature `add-form-emails` (owner notification, submitter confirmation) with its own
  spans and metrics; presenter injects a mail-path fault (Slow/Break, ADR-0010); diagnose from
  the trace, fix, prove with a metric. [PO: planted problem]
- Act 3: Claude Code with the Grafana MCP (22 read-only tools, no database, no commands,
  ADR-0017); the agent identifies a failure from telemetry, proposes a change, validates it
  against fresh telemetry. [PO: planted problem]
- Act 4: attendee submissions from the QR code (shown in the opening) produce telemetry; check
  it for personal data. [PO: planted leak and fix]. Slide content: what is never recorded
  (Name, Email, Message, Notes, Company, Website), source-level redaction first, pipeline
  filtering as defence in depth.
- Act 5: tunnel baseline: the public site at https://observability.carlcod.es/ is the
  "production"; the agent's access is the same read-only MCP allow-list. Azure variant: Azure
  MCP Server read-only against Application Insights. Content depends on the open prod hosting
  decision (`PLANNING.md` Open decisions).

### D4. QR code early
QR to the public site in the opening (after the claim slide), repeated before Act 4. Early
submissions give Act 4 data. Slide states the forms notice: live demo, data deleted within
7 days, made-up email allowed.

### D5. Audience depth
Umbraco developers: no Umbraco basics. One slide each for OpenTelemetry signals, Aspire, and
MCP, at "what it gives you" level.

### Bar and verification
Bar: prototype (a working draft the PO rewrites in the slide tool). Verification: PO
read-through against the promises table, and one timed rehearsal against the Time fields. No
automated checks.

## Risks / Trade-offs

- [27 min demo with no buffer] → each act slide notes what to cut; Act 5 is the first to drop.
- [Planted problems not yet decided] → placeholders; lead-ins worded so they hold for any
  problem in that act.
- [Outline in the repo can be read by the on-stage agent] → no demo answers in it; keep this
  change directory off `demo/act-N-*` branches (as for other later-act artifacts, `PLANNING.md`
  Todos).
- [Act 5 depends on the open prod hosting decision] → written for the tunnel baseline; Azure
  variant noted in the speaker notes.
- [Venue network or LLM failure] → each act's notes point to its fallback video (`PLANNING.md`
  Todos: record fallback videos).
