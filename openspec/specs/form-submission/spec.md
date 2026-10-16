# form-submission Specification

## Purpose

The site's three fixed forms: rendering, server-side validation, and storing valid submissions,
so that every submission attempt reaches the server and is observable. Forms are part of the
site, not content: editors cannot define forms, fields or form text.

Decisions: ADR-0016 (fixed site forms); in `docs/adrs/`.

Decisions: ADR-0026 (submission reference), ADR-0027 (owner notification and form telemetry);
in `docs/adrs/`.

## Requirements

### Requirement: Site forms
The site SHALL have exactly three forms with these fields:
- Contact: Name (text, required), Email (email, required), Message (multi-line text, required).
  No phone checkbox: no phone number is collected (PO 2026-10-14).
- Service enquiry: Name (text, required), Email (email, required), Company (text, optional),
  Website (text, optional), Message (multi-line text, required).
- Event booking: Name (text, required), Email (email, required), Notes (multi-line text,
  optional), Join mailing list (checkbox).

The service or event a submission is about SHALL come from the page the form is on, not from a
field the visitor fills in.

#### Scenario: Forms available without setup
- **WHEN** the site has started against an empty database
- **THEN** all three forms exist with exactly these fields and required flags, with no editor
  action

### Requirement: Fixed form text
Form labels, messages and notices SHALL be fixed by the site. Editors SHALL NOT be able to add,
remove or change form fields or form text.

#### Scenario: Editor edits a page with a form
- **WHEN** an editor edits the Contact us page, an event or a service
- **THEN** no form fields or form text are offered for editing

### Requirement: Rendering
A page that includes a form SHALL render all its fields with labels, mark required fields, and
include forgery protection. Rendering SHALL NOT require client-side scripting.

#### Scenario: Form renders
- **WHEN** a visitor opens a page with a form
- **THEN** each field is shown with its label and type, required fields are marked, and the
  form posts back to the site

### Requirement: Server-side validation only
Validation SHALL happen on the server only; the browser SHALL NOT block submission for
missing or malformed values. A field fails validation when it is required and empty, or when an
email value is not a valid address. No other format rules apply.

#### Scenario: Missing required field
- **WHEN** a visitor submits the Contact form without a Message
- **THEN** the form is shown again with the entered values kept and an error next to Message

#### Scenario: Invalid email
- **WHEN** a visitor submits a form with an Email value that is not a valid address
- **THEN** the form is shown again with an error next to Email

### Requirement: Forgery protection
The site SHALL reject submissions without a valid forgery-protection token.

#### Scenario: Missing token
- **WHEN** a submission arrives without a valid token
- **THEN** it is rejected and not stored

### Requirement: Storing a submission
A valid submission SHALL be stored with: the form, the page it was submitted from, the UTC
time of submission, its reference, and the submitted field values keyed by field. It SHALL
NOT store the visitor's IP address or browser details.

#### Scenario: Valid submission
- **WHEN** a visitor submits a valid Contact form
- **THEN** one submission is stored with form, page, UTC timestamp, reference and all field
  values, and no IP address or browser details

#### Scenario: Invalid submission
- **WHEN** a submission fails validation
- **THEN** nothing is stored

### Requirement: Booking a past event
An Event booking SHALL be stored like any other submission. An Event booking for an event that
has started SHALL be rejected. There are no places, capacity or booking status.

#### Scenario: Booking a past event
- **WHEN** an Event booking arrives for an event whose start time has passed
- **THEN** the submission is rejected, nothing is stored, and the visitor sees "This event has
  ended"

### Requirement: Confirmation after submit
After a valid submission the visitor SHALL see the message "Thank you, we'll be in touch. Your
reference is <reference>." with the submission's reference in place of the form, and
refreshing the page SHALL NOT submit the form again.

#### Scenario: Reference shown
- **WHEN** a visitor submits a valid form
- **THEN** the page shows "Thank you, we'll be in touch. Your reference is ACME-…" with the
  reference stored for that submission

#### Scenario: Form hidden after submit
- **WHEN** a visitor submits a valid form
- **THEN** the page shows the success message and not the form

#### Scenario: Refresh after submit
- **WHEN** a visitor refreshes the page after a successful submission
- **THEN** no second submission is stored

### Requirement: Database portability
Submissions SHALL work identically on the file-based database and on the server database.

#### Scenario: Both databases
- **WHEN** the same valid submission is made on each database type
- **THEN** both store an equivalent submission

### Requirement: Submission reference
Each stored submission SHALL get a reference of the form `ACME-` followed by a number of at
least four digits (e.g. `ACME-0042`). References SHALL be unique across all stored
submissions of all three forms. Submissions stored before this change SHALL also get a
reference.

#### Scenario: Reference on a valid submission
- **WHEN** a visitor submits a valid form
- **THEN** the stored submission has a reference that no other stored submission has

#### Scenario: Many visitors at once
- **WHEN** twenty valid forms are submitted at the same moment
- **THEN** all twenty are stored, each with a different reference, and no visitor sees an
  error

### Requirement: Owner notification
After a submission is stored, the site SHALL send one email to the owner address set for the
environment, for each of the three forms. Subject: `New <form name> submission <reference>`.
Body: fixed text naming the form, the page title, the reference, the UTC time of submission
and each submitted field with its label and value. When no owner address or no mail server
is set (site run alone), no email is sent and the submission still succeeds. No email is sent
to the submitter.

#### Scenario: Notification arrives
- **WHEN** a visitor submits a valid Service enquiry in the local environment
- **THEN** the mail catcher shows one email to the owner address whose subject and body carry
  the same reference the visitor was shown

#### Scenario: Invalid submission sends nothing
- **WHEN** a submission fails validation
- **THEN** no email is sent

#### Scenario: Site run alone
- **WHEN** a valid form is submitted on the site run alone without a mail server
- **THEN** the submission is stored, the visitor sees the success message and no error occurs

### Requirement: Submission telemetry
Each form post SHALL produce telemetry the dashboard and the agent can use: a span for the
submission, carrying the form, the outcome (`stored`, `invalid` or `failed`) and, when
stored, the reference; a span for sending the notification; a count of submissions by form
and outcome; and one information log entry per stored submission naming the form, the page
and the reference. A post that fails after validation (storing or sending) SHALL be recorded with
outcome `failed` and its error. Telemetry SHALL NOT contain Name, Email, Message, Notes,
Company or Website values.

#### Scenario: Stored submission in telemetry
- **WHEN** a valid Contact form is submitted
- **THEN** the local dashboard shows the post's trace with a submission span (form Contact,
  outcome stored, its reference) and a notification span, and a log entry with form, page
  and reference

#### Scenario: No personal data
- **WHEN** a form is submitted with a distinctive name and email address
- **THEN** neither value appears in any trace, log or metric in the local dashboard or the
  production-stage platform
