# Spec Delta

## MODIFIED Requirements

### Requirement: Events
Each event SHALL have a title, a description, and a start date and time. Events have no format,
capacity or places. Events are free; the site SHALL NOT show or take payment. The site SHALL
include at least three events, for example "How to get the most out of your CMS". When the site
imports its content, event start dates SHALL be set relative to the day of the import, keeping
their order, spacing and time of day, so that one event is past and at least three start within
the following four weeks (ADR-0037).

#### Scenario: Events list
- **WHEN** a visitor opens the Events page
- **THEN** it lists upcoming events in date order with title, date and time, each
  linking to its event page

#### Scenario: Upcoming event page
- **WHEN** a visitor opens the page of an event that has not started
- **THEN** it shows the event's details and the Event booking form

#### Scenario: Past event page
- **WHEN** a visitor opens the page of an event whose start time has passed
- **THEN** it shows "This event has ended" and no booking form

#### Scenario: Fresh site on any date
- **WHEN** the site starts on a new database on any date
- **THEN** the Events page lists at least three upcoming events, all starting within four weeks of
  that day, and one event page shows "This event has ended"

#### Scenario: Committed content unchanged
- **WHEN** the site has imported its content and set the event dates
- **THEN** the committed content files still hold the original event dates (no change in source
  control)
