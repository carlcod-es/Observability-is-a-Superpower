# ADR-0014: Event content model

## Status
Accepted (2026-10-08)

## Context
Spec `agency-site` "Events": each event has a title, description, start date and time, and is in
person or online. Only in-person events have a capacity, and an online event offers no capacity
setting. Past events show "This event has ended". The site has no custom backoffice UI
(ADR-0004). The talk is in the UK (BST); the production container runs in UTC. The existing site
content uses the composition `compPage` (`pageTitle`, `summary`, `mainContent`).

Verified in the site's uSync files: Umbraco 17.7.1 ships a Date Time with time zone editor
(`Umbraco.DateTimeWithTimeZone`).

## Decision
- Composition `eventBase`: `compPage` + `startDate` (Date Time with time zone data type limited
  to Europe/London).
- Document types `eventInPerson` (`eventBase` + `capacity`, Numeric, required, minimum 1) and
  `eventOnline` (`eventBase` only). The format is the document type.
- Both are allowed under `events`; both use the template Event.
- An event is past when `startDate` (as an absolute instant) is at or before UTC now.
- Event and service pages host the form picked on their parent list node (`events`,
  `services`); `compPage` gains an optional `form` picker limited to `superPowerForm`.

Alternatives considered:
- One `event` type with a format dropdown and a capacity field: online events would show a
  capacity field; hiding it needs custom UI; rejected.
- Plain Date Time picker interpreted as Europe/London: works, but the stored value carries no
  zone and every reader must know the convention; rejected.
- Form picker on every event and service page: repeats the same choice per page; rejected.

## Consequences
- Code distinguishes formats by document type alias (`eventInPerson`, `eventOnline`).
- Changing an event's format means recreating it as the other type.
- The stored value shape of Date Time with time zone is checked before the past-event rule is
  written (`add-demo-foundation` task 5.2).
