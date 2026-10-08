# ADR-0004: Forms package schema and submission storage

## Status
Accepted (2026-10-07)

## Context
The package must create its field types and storage itself (spec `form-definitions`,
"Package self-containment") and work identically on SQLite and SQL Server
(spec `form-submission`, "Database portability"). No custom backoffice UI.

Verified for Umbraco 17.7.1: package migrations inherit `AsyncPackageMigrationBase`
(synchronous base removed); `PackageMigrationPlan.IgnoreCurrentState` is true by default, so
steps can re-run; `PackageMigrationsUnattended` defaults to true; content types can be created
with fixed keys (`ContentTypeCreateModel.Key`).

## Decision
- One `PackageMigrationPlan` ("SuperPower.Forms") with idempotent async steps:
  1. Element types (fixed GUID keys): Text, Email, Textarea, Checkbox, Select field. Shared
     properties: alias, label, required, pattern, pattern error message. Select adds options.
  2. Block List data type (fixed key) allowing the five element types.
  3. Document type `superPowerForm` (fixed key, element-free, no template) with a `fields`
     property using that data type and a `successMessage` text property (shown after a valid
     submission; revised 2026-10-08).
  4. Table `superPowerFormSubmission`: Id (int identity PK), FormKey (guid, indexed), PageKey
     (guid), SubmittedUtc (datetime), FieldsJson (`NVARCHARMAX`). No IP or client columns.
- Steps check for existence by key/table name before creating.
- Data access uses NPoco `Sql<ISqlContext>` builders through Umbraco's scope provider; no raw
  SQL strings.
- The site owns where forms live: a "Forms" folder and the three form nodes (Contact, Service
  enquiry, Event booking) are site content (ADR-0003); pages reference a form through a content
  picker (ADR-0014).
- uSync may export the package's types; fixed keys keep the export and the migration aligned.

## Alternatives
- EF Core for submissions: needs per-provider migration sets; Umbraco's own migrations are
  provider-agnostic from one definition; rejected.
- Field definitions stored as JSON on the form node with a custom editor: needs backoffice
  UI work (Lit/TypeScript); rejected.
- Forms embedded directly on pages as a Block List: duplicates definitions and makes form
  identity unclear for submissions; rejected.

## Consequences
- Changing a type after first install requires a new migration step with a new key.
- Email notification settings (owner email, subjects, bodies, send-confirmation toggle) are
  added to the form document type by a later step in change `add-form-emails`.
- Event bookings are stored by the site in its own table, not by the package (ADR-0015); the
  package exposes submission handlers for that (ADR-0013).
