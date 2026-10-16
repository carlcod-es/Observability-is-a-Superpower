## Why

The talk promises to avoid collecting personal form data in telemetry. Today that rests on
one rule followed by hand: the site's log entries name only the form and the reference. A
single careless log line (for example one that logs the whole submitted form) would send
names, email addresses and messages to the local dashboard, to the production-stage platform
and to the agent reading both. The demo needs a safety net that holds even when the rule is
broken, and a way to still tell entries about the same person apart without seeing who it is
(`docs/research/pii-pseudonymisation-sources.md`).

PO 2026-10-15: both layers go on `main`; the PO comments parts out for demos.

## What Changes

- **Layer 1, rule:** site code logs what happened (form, outcome, reference), never the
  submitted values. Written down for developers and the agent; existing log entries checked
  against it.
- **Layer 2, safety net:** every log entry the site produces (site code and Umbraco) is
  cleaned before it leaves the site:
  - values of personal form fields (Name, Email, Message, Notes, Company, Website), including
    fields inside a logged object, are replaced by `[REDACTED]`;
  - any email address in an entry's values or message is replaced by `[REDACTED:<id>]`, where
    the id is the same for the same address and cannot be worked out without a secret held
    by the environment.
- Log entries leave the site only through the cleaned path: the site no longer writes its log
  entries to its console output, which the local dashboard also shows (PO 2026-10-15; the
  presenter talks about this gap on stage).
- The safety net can be switched off for a demo by commenting out one line.
- Applies to logs only. Traces and metrics keep their current rule (no personal data).

## Capabilities

### New Capabilities
- `log-privacy`: personal data in the site's log entries is masked or pseudonymised before it
  reaches any observability platform.

### Modified Capabilities
None. The "Submission telemetry" rule in `add-form-emails` (no Name, Email, Message, Notes,
Company or Website values in telemetry) stays as it is; this change adds the mechanism that
enforces it for logs.

## Impact

- Site start-up configuration (shared telemetry setup), the site's development log settings,
  and the local environment, which supplies the secret.
- Console output of the site run alone no longer shows log entries; they remain in the
  site's own log files.
- Log entries in the local dashboard and the production-stage platform: masked values where
  personal data would have appeared; no change to entries without personal data.
- Developer and agent guidance (project instructions, project setup doc).
- Out of scope: traces, metrics, the database, the notification email, a collector-level
  filter in front of the production stage.
