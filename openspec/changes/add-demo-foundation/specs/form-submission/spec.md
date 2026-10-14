## Purpose

The site's three fixed forms: rendering, server-side validation, and storing valid submissions,
so that every submission attempt reaches the server and is observable. Forms are part of the
site, not content: editors cannot define forms, fields or form text.

Decisions: ADR-0016 (fixed site forms); in `docs/adrs/`.

## ADDED Requirements

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
time of submission, and the submitted field values keyed by field. It SHALL NOT store the
visitor's IP address or browser details.

#### Scenario: Valid submission
- **WHEN** a visitor submits a valid Contact form
- **THEN** one submission is stored with form, page, UTC timestamp and all field values, and
  no IP address or browser details

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
After a valid submission the visitor SHALL see the message "Thank you, we'll be in touch.", and
refreshing the page SHALL NOT submit the form again.

#### Scenario: Refresh after submit
- **WHEN** a visitor refreshes the page after a successful submission
- **THEN** no second submission is stored

### Requirement: Database portability
Submissions SHALL work identically on the file-based database and on the server database.

#### Scenario: Both databases
- **WHEN** the same valid submission is made on each database type
- **THEN** both store an equivalent submission
