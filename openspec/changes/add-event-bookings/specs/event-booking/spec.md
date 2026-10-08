## Purpose

Decides who gets a place at an event: a booking is confirmed by the visitor opening a personal
link, places are allocated at that moment, all or nothing, and bookings that do not fit go on
the waiting list.

## ADDED Requirements

### Requirement: Confirmation link
Every pending booking SHALL have its own confirmation link. The link SHALL be unguessable, SHALL
identify exactly one booking, and SHALL NOT expire. Submitting the Event booking form SHALL NOT
allocate places.

#### Scenario: Booking submitted
- **WHEN** a visitor submits a valid Event booking
- **THEN** the booking is pending with its own confirmation link, places left for the event are
  unchanged, and the visitor sees "Check your email to confirm your place"

#### Scenario: Unknown link
- **WHEN** someone opens a confirmation link that matches no booking
- **THEN** they see "This link is not valid" and no booking changes

### Requirement: Allocation when the link is opened
When the link of a pending booking for an in-person event is opened, the booking SHALL become
confirmed if the event's places left are at least the booking's places, and waitlisted
otherwise. A booking SHALL never be partly confirmed. Places left SHALL be the event's capacity
minus the places of its confirmed bookings, and SHALL never go below zero.

#### Scenario: Enough places
- **WHEN** the link of a pending booking for 2 places is opened and 5 places are left
- **THEN** the booking is confirmed, 3 places are left, and the visitor sees that their places
  are confirmed

#### Scenario: Not enough places for the whole booking
- **WHEN** the link of a pending booking for 3 places is opened and 2 places are left
- **THEN** the booking is waitlisted, 2 places are still left, and the visitor sees that they
  are on the waiting list

#### Scenario: Event full
- **WHEN** the link of a pending booking is opened and no places are left
- **THEN** the booking is waitlisted

#### Scenario: Contested last places
- **WHEN** the links of several pending bookings for the same event are opened at the same
  moment and their places together exceed the places left
- **THEN** confirmed places never exceed capacity, every booking ends confirmed or waitlisted,
  and each visitor sees their own booking's outcome

### Requirement: Online events
Bookings for online events SHALL be for one place and SHALL always be confirmed when their link
is opened. Online events SHALL have no waiting list and SHALL NOT show places left.

#### Scenario: Online booking confirmed
- **WHEN** the link of a pending booking for an online event is opened
- **THEN** the booking is confirmed

### Requirement: Opening a link again
Opening the link of a booking that is already confirmed or waitlisted SHALL show its current
status and SHALL NOT change any booking.

#### Scenario: Second click on a waitlisted booking
- **WHEN** the link of a waitlisted booking is opened again after places have become available
- **THEN** the visitor sees that they are on the waiting list and the booking stays waitlisted

### Requirement: Waiting list is final
A waitlisted booking SHALL stay waitlisted. Nothing moves a booking off the waiting list, and
visitors cannot cancel bookings.

#### Scenario: No promotion
- **WHEN** an event has waitlisted bookings
- **THEN** none of them ever becomes confirmed

### Requirement: One booking per email per event
An email address SHALL have at most one booking per event, whatever its status. Email
addresses SHALL be compared ignoring case and surrounding spaces.

#### Scenario: Repeat booking
- **WHEN** a visitor submits an Event booking with an email address that already has a booking
  for that event
- **THEN** no new booking is stored and the form is shown again with the message "You have
  already booked this event", keeping the entered values

#### Scenario: Same email, different event
- **WHEN** a visitor books a second event with the same email address
- **THEN** the booking is stored as normal

### Requirement: Places left on the event page
An in-person event page SHALL show its places left as "N of C places left", counted from
confirmed bookings only. When no places are left it SHALL show "Fully booked, join the waiting
list" and SHALL keep showing the booking form.

#### Scenario: Pending bookings do not count
- **WHEN** an event with capacity 20 has 5 confirmed places and 4 pending places
- **THEN** its page shows "15 of 20 places left"

#### Scenario: Fully booked
- **WHEN** an event's confirmed places equal its capacity
- **THEN** its page shows "Fully booked, join the waiting list" and the booking form

### Requirement: Confirmation link kept out of telemetry
The confirmation link and any part of it that identifies a booking SHALL NOT appear in logs,
traces or metrics. A booking MAY be referred to in telemetry by an identifier that cannot be
used to open its link.

#### Scenario: Link opened
- **WHEN** a confirmation link is opened
- **THEN** no log entry, trace or metric records the link or its identifying part
