## Purpose

Renders a defined form on a page, validates submissions on the server and stores valid
submissions, so that every submission attempt reaches the server and is observable.

## ADDED Requirements

### Requirement: Rendering
A page that includes a form SHALL render all its fields with labels, mark required fields, and
include forgery protection. Rendering SHALL NOT require client-side scripting.

#### Scenario: Form renders
- **WHEN** a visitor opens a page with a form
- **THEN** each field is shown with its label and type, required fields are marked, and the
  form posts back to the site

### Requirement: Server-side validation only
Validation SHALL happen on the server only; the browser SHALL NOT block submission for
missing or malformed values. A field fails validation when it is required and empty, when an
Email value is not a valid address, when a value does not match the field's pattern, or when a
Select value is not one of its options.

#### Scenario: Missing required field
- **WHEN** a visitor submits the Contact form without a Message
- **THEN** the form is shown again with the entered values kept and an error next to Message

#### Scenario: Pattern mismatch
- **WHEN** a value does not match its field's pattern
- **THEN** the form is shown again with that field's configured error message

#### Scenario: Tampered select value
- **WHEN** a submission contains a Class value that is not one of the options
- **THEN** the submission fails validation and is not stored

### Requirement: Forgery protection
The site SHALL reject submissions without a valid forgery-protection token.

#### Scenario: Missing token
- **WHEN** a submission arrives without a valid token
- **THEN** it is rejected and not stored

### Requirement: Storing a submission
A valid submission SHALL be stored with: the form, the page it was submitted from, the UTC
time of submission, and the submitted field values keyed by field alias. It SHALL NOT store the
visitor's IP address or browser details.

#### Scenario: Valid submission
- **WHEN** a visitor submits a valid Book a class form
- **THEN** one submission is stored with form, page, UTC timestamp and all field values, and
  no IP address or browser details

#### Scenario: Invalid submission
- **WHEN** a submission fails validation
- **THEN** nothing is stored

### Requirement: Confirmation after submit
After a valid submission the visitor SHALL see a confirmation message, and refreshing the page
SHALL NOT submit the form again.

#### Scenario: Refresh after submit
- **WHEN** a visitor refreshes the page after a successful submission
- **THEN** no second submission is stored

### Requirement: Database portability
Submissions SHALL work identically on the file-based database and on the server database.

#### Scenario: Both databases
- **WHEN** the same valid submission is made on each database type
- **THEN** both store an equivalent submission
