# ADR-0014: Event content model

## Status
Accepted (2026-10-08).

## Context
Spec `agency-site` "Events": each event has a title, description, and start date and time;
events have no format, capacity or places (PO 2026-10-13). Past events show "This event has
ended". The page type decides which form a page shows (ADR-0016); editors cannot choose a
form. The talk is in the UK (BST); the production container runs in UTC. The existing site
content uses the composition `compPage` (`pageTitle`, `summary`, `mainContent`).

Verified in the site's uSync files: Umbraco 17.7.1 ships a Date Time with time zone editor
(`Umbraco.DateTimeWithTimeZone`).

## Decision
- One document type `event`: composition `compPage` + `startDate` (Date Time with time zone
  data type limited to Europe/London, mandatory); template Event; allowed under `events`.
- An event is past when `startDate` (as an absolute instant) is at or before UTC now.
- No `form` picker anywhere: the Event template renders the Event booking form, the Service
  template the Service enquiry form, and the `contact` document type's template the Contact
  form (ADR-0016).

## Alternatives
- Two types `eventInPerson`/`eventOnline` with capacity (the 2026-10-08 decision): format and
  capacity left the requirements on 2026-10-13; rejected.
- Plain Date Time picker interpreted as Europe/London: works, but the stored value carries no
  zone and every reader must know the convention; rejected.
- Form picker on pages: editors must not choose forms; rejected.

## Consequences
- The past-event rule depends on the stored value shape of Date Time with time zone.
