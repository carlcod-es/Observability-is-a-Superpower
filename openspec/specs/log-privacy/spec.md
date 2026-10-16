# log-privacy Specification

## Purpose

Keeps personal form data out of the site's log entries on every observability platform, even
when a log line in the code includes it, while still letting entries about the same email
address be linked (ADR-0028).

Decisions: ADR-0028 (log redaction); in `docs/adrs/`.

## Requirements

### Requirement: Personal field values masked in logs
Before a log entry leaves the site, the value of any entry property named Name, Email,
Message, Notes, Company or Website (case-insensitive), including such fields inside a logged
object, SHALL be replaced by `[REDACTED]`, or by the email stand-in when the value is an email
address. Where the same value appears in the entry's rendered message, it SHALL be replaced
there too. This SHALL apply to entries from the site's own code and from Umbraco. Entries
without such properties SHALL be unchanged.

#### Scenario: Whole form logged
- **WHEN** site code logs a submitted Contact form as one object, with name `Canary Person`,
  email `canary@test.invalid` and a message
- **THEN** the entry in the local dashboard and in the production-stage platform shows the
  form's fields with masked name, email and message, and neither `Canary Person` nor
  `canary@test.invalid` appears in the entry's properties or message

#### Scenario: Ordinary entry unchanged
- **WHEN** a valid form is stored
- **THEN** the entry "Form <form> for <page> stored as <reference>" appears with form, page
  and reference as before

### Requirement: Email addresses pseudonymised in logs
Before a log entry leaves the site, every email address in the entry's property values and
rendered message SHALL be replaced by `[REDACTED:<id>]`. The id SHALL be the same for the same
address (ignoring case) for as long as the environment keeps its secret, SHALL differ for
different addresses, and SHALL NOT be computable from the address without that secret. The
local environment SHALL create and keep its secret without manual setup.

#### Scenario: Same person, two entries
- **WHEN** two log entries contain `canary@test.invalid` and a third contains
  `other@test.invalid`
- **THEN** the first two show the same `[REDACTED:<id>]` and the third a different one

#### Scenario: Email in free text
- **WHEN** a log entry's message contains an email address in running text
- **THEN** the address is replaced by its stand-in and the rest of the message is unchanged

### Requirement: Logs leave the site only through the cleaned path
The site SHALL NOT write its log entries to its console output in any environment, so the
local dashboard's console view and the agent's console-log access never show entries that
bypass the masking.

#### Scenario: Console view
- **WHEN** a valid form is stored on the local environment
- **THEN** the site's structured logs show "Form <form> for <page> stored as <reference>" and the site's
  console output in the local dashboard does not

### Requirement: Safety net can be switched off for a demo
The masking SHALL be enabled by default and SHALL be disabled by commenting out a single,
clearly marked line of site start-up code, with no other change needed for the site to start.

#### Scenario: Masking off
- **WHEN** the marked line is commented out and site code logs a submitted form as one object
- **THEN** the entry shows the submitted values unmasked, and the site starts and serves forms
  as before
