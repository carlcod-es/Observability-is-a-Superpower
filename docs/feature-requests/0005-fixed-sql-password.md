# FR-0005: Fixed SQL Server password

## Status
Rejected (PO 2026-10-15): the generated password stays in AppHost user secrets; README
"Connect to the database" says how to find it (change `add-demo-dashboard`, design.md D8).
Source: PO 2026-10-15.

## Request
The local SQL Server container uses one fixed `sa` password, set in the repo and published with it.
A fresh clone needs no secret, and anyone running the demo can connect a SQL client.

## Why
Aspire generates the password per machine and keeps it in AppHost user secrets: attendees cannot
find it easily, and the stage rule "do not show `aspire describe` output" exists only to hide it.
The database holds demo data only; the port is reachable on localhost only and the tunnel exposes
only the site.

## Acceptance criteria
- The password is in the AppHost configuration in the repo; no user secret is needed.
- The README says how to connect: server `localhost,<port>`, user `sa`, the password, the port
  from the Aspire dashboard.
- The port stays dynamic (PO 2026-10-15).

## Notes
- Existing volumes keep the old password; reset the volume (PO approved 2026-10-15).
- Agents still get no database access (permission settings, CLAUDE.md "Scope").
