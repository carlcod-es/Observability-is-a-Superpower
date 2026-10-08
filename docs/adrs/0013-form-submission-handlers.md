# ADR-0013: Form submission handlers

## Status
Accepted (2026-10-08)

## Context
The Event booking form needs rules that depend on the page it sits on (spec `form-submission`,
"Storing an Event booking"; spec `form-definitions`, "Booking an online event"; spec
`agency-site`, "Past event page"):
- online event: Places is not shown, the booking is for one place, any other posted value fails;
- past event: no form, posts rejected with "This event has ended";
- valid booking: a pending booking is stored with the submission.

The package (ADR-0004, ADR-0005) is generic and knows nothing about events. Later changes need
the same kind of hook: `add-event-bookings` (one booking per email per event) and the production
switch that closes every form.

## Decision
- The package defines `IFormSubmissionHandler`, registered through DI (zero or more). The site
  implements it.
- `Prepare(form, page)` runs on render and on post, before validation. It returns:
  - closed with a message: the ViewComponent shows the message instead of the form; a post is
    rejected with that message and nothing is stored;
  - fixed field values: fixed fields are not rendered; on post the fixed value is used, and a
    different posted value is a validation error on that field.
- `OnStored(submission, page)` runs after the submission insert, inside the same Umbraco scope.
  An exception rolls back the submission too.
- Each handler decides whether it applies (for example by the page's document type). Results
  from several handlers combine: any closed wins; fixed values merge.
- Handlers get the current time from `TimeProvider` so date rules are testable.

## Alternatives
- Booking logic inside the package: couples a generic forms package to the site's events;
  rejected.
- Umbraco notifications (`INotificationHandler<...>`): run after the fact; no clean way to veto
  before validation or to hide a field; rejected.
- A separate booking controller and view in the site: duplicates rendering, validation and
  antiforgery handling; rejected.
- Hide Places in a site-overridden view only: the server would still require and accept any
  Places value; rejected.

## Consequences
- The package gains a small public extension surface that must stay stable across the demo
  branches.
- Rules that must veto a submission (for example one booking per email per event in
  `add-event-bookings`) can run in `Prepare` or throw from `OnStored`; that change decides which.
- Package spans and metrics (`add-form-emails`) should record a closed-form rejection as its own
  outcome.
