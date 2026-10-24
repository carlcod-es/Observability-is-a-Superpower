# ADR-0016: Fixed site forms

## Status
Accepted (2026-10-13).

## Context
The talk no longer promises a custom forms package (PO 2026-10-10). The three forms are fixed:
editors cannot define forms, fields or form text; the page type decides which form a page shows.
An Event booking is an ordinary submission: no places, capacity or
booking status (PO 2026-10-13). The package built for editor-defined forms
(`src/SuperPower.Forms`) and the booking table no longer have a requirement behind them.
CLAUDE.md principle: keep it simple.

## Decision
- Three typed form models in `SuperPower.Web`: `ContactForm`, `ServiceEnquiryForm`,
  `EventBookingForm`. Validation via DataAnnotations: `[Required]`, `[EmailAddress]`;
  `[Display]` carries the fixed labels. No other format rules.
- One partial view per form: tag helpers (`asp-for`), `novalidate`, antiforgery token;
  ModelState re-displays the entered values on failure.
- One SurfaceController `AcmeFormsController`, one action per form, `[ValidateAntiForgeryToken]`;
  `CurrentUmbracoPage()` on validation failure; TempData + redirect (post/redirect/get) on
  success. Success message for all three forms: "Thank you, we'll be in touch."
- Past-event rule (`TimeProvider`): when an event's `startDate` is at or before UTC now, the
  page shows "This event has ended" and no form, and a post for it is rejected and stores
  nothing. Checked by the Event booking action and the Event view.
- A valid submission is stored as one row: form, page key, UTC timestamp, JSON of the model's
  field values; no IP or browser info. One site migration plan "Acme.Site" with step
  `acmeFormSubmission` (same columns as the package's table); NPoco `Sql<ISqlContext>` builders
  (works on SQLite and SQL Server).
- Telemetry names `Acme.Forms` for the site's `ActivitySource` and `Meter`; ServiceDefaults
  registers them. Spans and instruments: ADR-0027. Submission reference:
  ADR-0026.
- The page type decides the form: new document type `contact` (template Contact) shows the
  Contact form; the Event template shows Event booking; the Service template shows Service
  enquiry. No form picker, no form content nodes.

## Alternatives
- Generic form engine with code-defined field lists: keeps the package's complexity (alias
  dictionaries, looping view, regex timeout, handler hook) with no backoffice story left to
  justify it; rejected.
- Separate forms project holding the fixed forms: project separation with nothing shipped as a
  package; rejected by the PO 2026-10-10.

## Consequences
- No forms project: site tests live in `tests/SuperPower.Web.Tests` (ADR-0012).
- No form content nodes or picker for editors to maintain.
