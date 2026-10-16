# Review follow-ups: add-form-emails

Minors and nits from block reviews. Read at the start of every apply session.

## Block 1A (tasks 1.1, 1.2) - reviewer APPROVED 2026-10-15
- [x] minor: `tests/smoke/smoke.cs` and `README.md` still use / describe Toxiproxy. Resolved in
  `add-form-emails` tasks 5.1 and 5.4.

## Block 4A (task 4.1) - reviewer APPROVED 2026-10-15
- [x] minor: `tests/traffic/burst.cs:19-20` `int.Parse` throws on non-numeric `--count` /
  `--parallel`; `--parallel 0` throws. Fix: `int.TryParse` and `> 0`, else exit 2.
- [x] minor: `tests/traffic/burst.cs:101-104` 302 without `ACME-` on the redirect page counts as
  ok with reference `?`; redirect GET failure counts as failed with exception text. Fix: count
  a missing reference as failure. (Runtime check in task 4.1 looks for 50 distinct references.)
- [x] nit: `tests/traffic/burst.cs:73` `.First(...)` gives an unhelpful message when no form has
  `ufprt`. Fix: `FirstOrDefault` + clear message.
  Fixed in block 5B (reviewer APPROVED 2026-10-15).

## Blocks 2A (tasks 2.1, 2.2) and 3A (task 3.1) - reviewer APPROVED 2026-10-15
- [ ] minor: `src/SuperPower.Web/Forms/AcmeFormsController.cs:113-137` no `acme.forms.notify` span
  when the notification is skipped (site run alone). Accepted: spec scenario covers the stored
  post only.
- [ ] minor: no tests for controller outcome counting or the migration step; covered at runtime by
  smoke and burst on SQL Server (tasks 4.1, 5.1).
- [ ] nit: `src/SuperPower.Web/Forms/AcmeFormsController.cs:3-16` using order.
- [ ] nit: form partials render "Your reference is ." if the reference TempData is missing (not
  reachable: both keys set together).

## Block 5B (task 5.2, burst fixes) - reviewer APPROVED 2026-10-15
- [ ] nit: `.claude/settings.json` deny covers the Read tool only; a shell `cat` is not blocked.
  Accepted in design.md D7 (stage runs from a separate clone without the presenter notes).
- [ ] nit: `tests/traffic/burst.cs:19-20` failed `int.TryParse` overwrites the default before exit; harmless.

## Block 5A (task 5.1) - reviewer APPROVED 2026-10-15
- [ ] minor: `tests/smoke/smoke.cs:430` `subject.EndsWith(reference)` culture-sensitive; use
  `StringComparison.Ordinal`.
- [ ] nit: `tests/smoke/smoke.cs:430` `to` matched with `Contains`.
- [ ] nit: `tests/smoke/smoke.cs:40` `Success` omits the trailing reference (regex covers it).
- [ ] PO-accepted (2026-10-15): smoke `no pre-start telemetry in Grafana` FAILs reproducibly on the
  AppHost target: `/smoke-pre/` request logs made seconds before `lgtm` start reach Loki. Not
  caused by this change. Likely the Aspire proxy on the `lgtm` otlp-http endpoint holds the
  collector's export (otlphttp default timeout 30 s) until lgtm is up. Possible fixes after the
  talk: `timeout: 2s` on exporter `otlphttp/lgtm`, or an unproxied lgtm OTLP endpoint.

## Docs block (task 5.4) - reviewer APPROVED 2026-10-15
- [x] minor: `CLAUDE.md` named the reference lock mechanism (hint for the stage agent). Removed.
- [x] minor: `PLANNING.md` stale Toxiproxy / confirmation / fault lines. Annotated.
- [x] minor: `PLANNING.md` success text without reference. Fixed.
- [x] nit: `PLANNING.md` "Form-meter panels go in add-form-emails". Updated.
- [ ] nit: `docs/project-setup.md` smoke bullet long line with mid-sentence ADR citation.
- [ ] nit: `README.md` standalone "No mail (no notification email)" redundant.

## Sections 1-4 supervisor - 1, 3, 4 APPROVED; 2 CHANGES REQUESTED (evidence only) 2026-10-15
- [x] major: fresh-database path unproven. Run on a fresh SQLite database (verification.md);
  fresh SQL Server path runs at the PO's pre-talk reset.
- [x] minor: "Many visitors at once" evidence only 10 in flight. 40/20 run recorded.
- [ ] nit: `src/SuperPower.Web/Forms/AcmeFormsController.cs:109-118` `Finish(..., Stored)` before
  TempData/redirect; a throw there would count the post twice (not reachable).
- [ ] nit: `src/SuperPower.Web/Forms/SubmissionNotification.cs:31-35` `LabelledFields` pairs with
  `SubmissionJson.ToFields` by index.
- [ ] nit: `tests/SuperPower.Web.Tests/SubmissionNotificationTests.cs:8-13` reference tests in the
  notification test class.
- PO risks: demo branches must carry `AddSubmissionReferenceStep` (else the site will not start on
  a migrated database); failed notification = stored row + error page (ADR-0027); first sample of
  a new counter series shows 0 on the panel; smtp4dev keeps a limited number of messages; port
  2525 must be free.

## Section 5 supervisor - APPROVED 2026-10-15
- [x] minor: spec scenario "Invalid submission sends nothing" has no runtime check. Accepted on code
  inspection (verification.md).
- [x] minor: `CLAUDE.md` old success text contradicted the new one. Removed.
- [x] minor: `PLANNING.md` stored-submission line without reference; "real emails sent" stale. Fixed.
- [x] minor: Block 1A Toxiproxy follow-up resolved. Ticked.
- [x] nit: `.claude/settings.json` `Read(./.presenter/**)` resolves against the start directory;
  `/.presenter/**` would anchor it to the project root. Resolved: folder moved to `docs/presenter/`,
  both patterns denied (PO 2026-10-15).
- [ ] nit: `tests/smoke/smoke.cs:18-21` header "standalone sends no email" is not checked; reword.
- [x] nit: verification.md 5.3 metrics checked in Prometheus only. Note added.
- PO risks: ADR-0025 ("production-stage problem comes from load"), ADR-0026/0027 Consequences and
  the local-environment spec delta are readable by the stage agent on `main`; decide per demo
  branch / stage clone in section 6.1. Smoke on the AppHost prints exactly one FAIL (pre-start).
