# ADR-0037: Event dates relative to the import day

## Status
Proposed (2026-11-25, FR-0007, PO 2026-11-25). Amends ADR-0003 (content seeding)
and ADR-0014 (event content model).

## Context
The committed content (`src/SuperPower.Web/uSync/v17/Content/`) holds five events with fixed dates
around the first talk (16 Oct 2026): 6 Oct, 20 Oct, 28 Oct, 5 Nov and 13 Nov. They are now all past:
- There is no booking form or booking count.
- There is no booking telemetry, so Act 3 cannot run.
- The smoke check's booking checks fail.

The PO gives the talk again and keeps giving it, and attendees run the repo at any time.

A fresh database imports the content on first boot (ADR-0003, `ImportOnFirstBoot`). uSync exports
content on save, so any content save at runtime rewrites the committed `.config` files. `startDate`
is stored as JSON: `{"date": "2026-10-20T12:50:00+01:00", "timeZone": "Europe/London"}` (ADR-0014).

## Decision
- **When:** when uSync imports an `event`, the site shifts its `startDate` before the item is saved.
- **By how much:** the whole number of days between the reference day (16 Oct 2026, the day the
  committed dates were written for) and the import day. Both are taken as Europe/London calendar dates.
- **Time of day:** kept in Europe/London local time. The offset is recomputed for the new date, so
  summer/winter time is right.
- **Result:** relative to import day D, the events fall on D−10 (past), D+4, D+12, D+20 and D+28.
- **Source:** the shift reads the date from the imported file, never from the database. A repeat import
  on the same day gives the same dates.
- **Files untouched:** the change is made to the item being imported, so no extra save happens and the
  committed files keep the original dates.
- **Mechanism:** a handler for uSync's importing-item notification.
  - If that notification cannot change the item, fall back to updating dates after the import with uSync
    export disabled for that save.
  - Either way, the committed files must not change.
- The reference day is a constant in the site code, next to the past-event rule (ADR-0014).

## Alternatives
- **PO moves the dates in the backoffice and re-exports.** No code, but the dates expire again; rejected
  (PO 2026-11-25).
- **Shift dates at render time.** No content writes, but every view, the upcoming-events query, the
  past-event rule and the booking form would all have to use the shifted value, and the backoffice would
  show different dates from the site; rejected.
- **Shift dates on every start.** Keeps old databases current too, but writes content on each start. That
  triggers uSync export-on-save into the committed files and adds start-up noise to the telemetry;
  rejected.
- **Seed events from code instead of uSync.** Duplicates the content model outside uSync; rejected.

## Consequences
- An existing database keeps the dates it was imported with. Resetting the database (README "Reset the
  databases"; the presenter does this the morning of a talk) gives fresh dates.
- Days are counted from the import, not the talk: an import on talk day D keeps the first upcoming event
  4 days ahead.
- Editors who re-import content from the backoffice get dates shifted to that day.
- The events requirement (CLAUDE.md "Demo site") drops "all starting after 16 Oct 2026" in favour of import-relative dates.
