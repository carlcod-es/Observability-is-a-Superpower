# ADR-0015: Event booking storage

## Status
Accepted (2026-10-08; custom table and Umbraco migration directed by the Product Owner)

## Context
Spec `form-submission` "Storing an Event booking": a valid Event booking is stored as a booking
for its event with the places requested and status pending. Change `add-event-bookings` will
query bookings by event and status (places left, allocation) and adds a confirmation link and
one booking per email per event. Both databases must work (spec `form-submission`, "Database
portability"). The forms package stays generic (ADR-0013).

## Decision
- The site owns table `acmeEventBooking`:
  Id (int identity PK), EventKey (guid, indexed), SubmissionId (int, indexed, no FK),
  Places (int), Status (`NVARCHAR(16)`, `Pending`), CreatedUtc (datetime).
- Created by a site `MigrationPlan` ("Acme.EventBookings") executed with `Upgrader` from an
  `UmbracoApplicationStartingNotification` handler when the runtime level is Run. Steps check
  for the table before creating it.
- Data access through an NPoco repository with `Sql<ISqlContext>` builders and Umbraco's scope
  provider; no raw SQL strings.
- Rows are written by the site's `EventBookingHandler.OnStored` (ADR-0013), in the submission's
  scope.
- Columns needed by `add-event-bookings` (confirmation token, email identity, confirmed time)
  are added there by new migration steps; this change creates only the columns above.

Alternatives considered:
- Table in the forms package: puts event concepts into the package; rejected.
- Bookings only as submission JSON: allocation and places left need indexed queries by event
  and status; rejected.
- FK from SubmissionId to `superPowerFormSubmission`: the package and site migration plans have
  no guaranteed order; rejected.
- `PackageMigrationPlan` in the site assembly: runs unattended too, but lists the site as an
  installed package in the backoffice; kept as fallback if `Upgrader` runs too early on a fresh
  unattended install.
- EF Core: per-provider migration sets (ADR-0004); rejected.

## Consequences
- Two migration plans run at start-up: `SuperPower.Forms` (package) and `Acme.EventBookings`
  (site).
- Orphaned booking rows are possible only if submissions are deleted outside the site; the
  production deletion routine (production change) deletes both.
- The agent has no database access (spec `agent-telemetry-access`); bookings are observable only
  through telemetry.
