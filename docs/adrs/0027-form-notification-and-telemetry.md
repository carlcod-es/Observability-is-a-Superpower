# ADR-0027: Form notification and telemetry

## Status
Accepted (2026-10-15)

## Context
Spec `form-submission`: owner notification for every stored submission, and submission
telemetry (spans, a count by form and outcome, a log entry) without personal data. ADR-0016
reserved the names `Acme.Forms` for the site's `ActivitySource` and `Meter`; ServiceDefaults
already registers both. Umbraco 17 sends mail through `IEmailSender` (MailKit) using
`Umbraco:CMS:Global:Smtp`. Owner address is an environment setting (CLAUDE.md).

## Decision
- Notification via Umbraco `IEmailSender.SendAsync`, after the submission's scope completes,
  in the request. Recipient from `Acme:Forms:OwnerEmail` (AppHost sets
  `Acme__Forms__OwnerEmail=owner@acme-agency.example`). Skipped when the address is empty or
  `IEmailSender.CanSendRequiredEmail()` is false (site run alone). Plain-text body built by a
  pure function (testable). Failures are not caught for the visitor (error page; the
  submission is already stored) but are recorded in telemetry.
- Spans on `Acme.Forms`: `acme.forms.submit` (tags `acme.form`, `acme.form.outcome`,
  `acme.submission.reference` when stored) and child `acme.forms.notify` (`acme.form`,
  `acme.submission.reference`). Errors: span status Error plus the exception event.
- Counter `acme.forms.submissions` (Prometheus `acme_forms_submissions_total`), tags
  `acme.form` (`contact`, `service_enquiry`, `event_booking`) and `acme.form.outcome`
  (`stored`, `invalid`, `failed`).
- Log: Information `Form {Form} for {Page} stored as {Reference}` per stored submission (`Page`
  = page name, added by the PO 2026-10-16 to filter by event or service). No field values
  in tags, span names, metrics or logs.
- MailKit's own `MailKit.Net.SmtpClient` activity source is not enabled: the notify span is
  enough at demo scale.

## Alternatives
- Background queue for mail: keeps the request fast, but adds a hosted service and trace
  links for no demo benefit at local SMTP speeds; rejected.
- Submitter confirmation emails: dropped by the PO (2026-10-15).
- Enabling MailKit tracing: more spans per email than the demo needs; rejected.

## Consequences
- A slow or failing mail server slows or fails the post; acceptable with a local mail catcher.
- The dashboard's form panel queries `acme_forms_submissions_total` (ADR-0023 amended).
