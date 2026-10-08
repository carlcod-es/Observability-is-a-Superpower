## Why

The talk on 16 Oct 2026 needs a working demo base: a believable site with forms, a local
environment that brings logs, traces and metrics into one place, and an agent that can read
that telemetry. Every demo branch starts from this state (Act 1), so it must exist and be
stable before any feature, fault or fix is built on top of it. Nothing is built yet.

## What Changes

- A fictional marketing agency site (Acme Agency) that offers services (for example Umbraco
  upgrade, SEO audit, AdWords audit) and runs free events (for example "How to get the most
  out of your CMS"), in person or online. Pages: Home, About us, Contact us, Events list, event
  pages, Services list, service pages. Populated automatically when the site starts against an
  empty database.
- A forms package (`SuperPower.Forms`) that lets editors define forms from field blocks and
  renders, validates and stores submissions. Three forms on the site: Contact, Service enquiry
  and Event booking.
- Storage for event bookings: a valid Event booking is kept as a booking for that event, with
  its number of places and the status pending. Nothing moves a booking out of pending yet.
- A local environment the presenter starts with one command: site, database, mail catcher and
  a fault-injection hop on the mail path, all visible in one observability dashboard.
- Baseline telemetry from the site and its database calls, without any package-specific
  instrumentation.
- Agent access to local telemetry over MCP, with the configuration committed for attendees.
- A file-based database mode so attendees can run the site without a container runtime.
- A short attendee README.

Out of scope (later changes):
- Booking workflow: confirmation link, place allocation, waiting list, places left, one booking
  per email per event (change `add-event-bookings`).
- All emails, including the booking confirmation link, and the package's own spans and metrics
  (change `add-form-emails`).
- Production environment, production agent access, attendee safeguards, history generation.
- Protection of personal data in telemetry beyond what the baseline records.
- Owner dashboard of bookings (stretch goal, no change planned yet).

## Capabilities

### New Capabilities
- `agency-site`: the Acme Agency site, its pages, events and services, and how it is populated
  on first start.
- `form-definitions`: how editors define a form and its fields (types, alias, label, required,
  pattern with error message), and the site's three forms.
- `form-submission`: rendering a form, server-side validation, error display, forgery
  protection, storing a submission, and storing an Event booking as a pending booking.
- `local-environment`: one-command start of all components, the observability dashboard, the
  mail catcher, mail-path fault controls, and the file-based database mode.
- `agent-telemetry-access`: the coding agent reading local telemetry and resource state over MCP.

### Modified Capabilities
<!-- none: no existing specs -->

## Impact

- The existing empty site gains its content, the forms package and the local environment;
  automated tests cover the package and booking logic.
- Repo gains the attendee agent configuration and README; site content is kept in source
  control, the database is not.
- New stored data: form submissions (owned by the package) and event bookings (owned by the
  site).
- Presenters and attendees need the talk's toolchain (see design), plus a container runtime
  for the full environment; the file-based mode needs only the SDK.
