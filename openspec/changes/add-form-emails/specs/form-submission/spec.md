## ADDED Requirements

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
and outcome; and one information log entry per stored submission naming the form and the
reference. A post that fails after validation (storing or sending) SHALL be recorded with
outcome `failed` and its error. Telemetry SHALL NOT contain Name, Email, Message, Notes,
Company or Website values.

#### Scenario: Stored submission in telemetry
- **WHEN** a valid Contact form is submitted
- **THEN** the local dashboard shows the post's trace with a submission span (form Contact,
  outcome stored, its reference) and a notification span, and a log entry with form and
  reference

#### Scenario: No personal data
- **WHEN** a form is submitted with a distinctive name and email address
- **THEN** neither value appears in any trace, log or metric in the local dashboard or the
  production-stage platform

## MODIFIED Requirements

### Requirement: Confirmation after submit
After a valid submission the visitor SHALL see the message "Thank you, we'll be in touch. Your
reference is <reference>." with the submission's reference, and refreshing the page SHALL NOT
submit the form again.

#### Scenario: Reference shown
- **WHEN** a visitor submits a valid form
- **THEN** the page shows "Thank you, we'll be in touch. Your reference is ACME-…" with the
  reference stored for that submission

#### Scenario: Refresh after submit
- **WHEN** a visitor refreshes the page after a successful submission
- **THEN** no second submission is stored

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
