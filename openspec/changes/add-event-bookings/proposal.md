## Why

After `add-demo-foundation`, an Event booking is stored as pending and nothing else happens.
In-person events have limited places, so a booking needs a rule for who gets a place. During
the talk the room books from their phones at the same time, so the last places of an event
will be contested live, and the outcome must be correct and visible.

## What Changes

- Each pending booking gets a personal confirmation link. Places are allocated only when the
  link is opened, not when the form is submitted.
- Opening the link confirms the booking if the event has enough places left for the whole
  booking, otherwise the booking goes on the event's waiting list. All or nothing: a booking is
  never partly confirmed.
- Online events have no capacity: opening the link always confirms.
- Opening a link again shows the booking's current status; links do not expire.
- The waiting list is a status only. Nobody is moved off it; there is no cancellation.
- One booking per email address per event.
- In-person event pages show places left, counted from confirmed bookings, and "Fully booked,
  join the waiting list" when none remain.
- The confirmation link never appears in telemetry.
- After a valid Event booking the visitor sees "Check your email to confirm your place",
  replacing "Booking received".

Out of scope:
- Sending any email, including the one carrying the link (change `add-form-emails`).
- Cancellation, waiting-list promotion, link expiry, payment.
- Owner dashboard of bookings (stretch goal).

## Capabilities

### New Capabilities
- `event-booking`: confirming a booking by link, place allocation, waiting list, places left,
  one booking per email per event, and what the visitor sees at each step.

### Modified Capabilities
<!-- none: `form-submission` is not yet archived to openspec/specs; the post-submit message
     for Event booking is specified in `event-booking` -->

## Impact

- Depends on `add-demo-foundation` (events with format and capacity, pending bookings).
- `add-form-emails` delivers the link to the visitor and sends the confirmed and waitlisted
  emails; until then the link exists but is not delivered.
- Production attendee notice: bookings need a real email address (attendee safeguards,
  production change).
