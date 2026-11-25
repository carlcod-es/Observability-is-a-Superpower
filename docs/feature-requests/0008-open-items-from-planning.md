# FR-0008: Open items carried from PLANNING.md

## Status
Open (2026-11-25). Source: the open items in `PLANNING.md` when it was deleted (2026-11-25). Library
and AppHost updates are in FR-0007; review nits are in FR-0004.

## Request
Close or drop each item below. Before-the-talk items first.

## Why
`PLANNING.md` was the working list of todos and open decisions. It was deleted along with OpenSpec, and
these items were still open.

## Acceptance criteria
- Each item is done, moved to the presenter runthrough, or dropped by the PO.

### Before the talk (PO)
1. Claude memory: remove the demo-plan entries from Claude memory, or run the demo from a clone at a
   different path. Memory is keyed to the folder path and loads on stage.
2. Record fallback videos of the agent segments (runthrough Morning step 11 expects them).
3. Script and rehearse the agent prompts with `/fast` on, in default permission mode.
4. Talk timing: Act 3 had no slot of its own and is now homework, so the 45 min plan has spare time.
5. `demo` branch:
   - Decide whether it strips the explanatory and `Fix:` lines under each `// Demo Code : Act N`. The
     on-stage agent can read `src/`, including `src/SuperPower.Api/BookingStoredConsumer.cs`.
   - Strip README "The acts": it names the files for Acts 1 and 2.
   - CLAUDE.md cites ADRs that the branch removes with `docs/`; strip the citations or use a shorter
     stage CLAUDE.md.
6. Stage PII rules (add to the runthrough):
   - Never lower Umbraco or Serilog levels to Debug while forms are submitted: failed inserts log
     `FieldsJson`.
   - Keep personal data out of query strings: Development "Request starting" logs record the full query.
   - Do not show `aspire describe` output on screen: it shows the SQL Server sa password.
7. Through the tunnel, attendees see Umbraco's defaults. Decide whether to replace the default
   `favicon.ico` and the default 404 page (umbra.co links).
8. Decide whether backoffice page loads (`/umbraco`, `/umbraco/login`) are traced.

### After the talk
9. Rename the Act 3 and slow-page `// Demo Code` comments to Homework : Act 3 / Act 4; they still
   describe a stage initial state, and the slow page is marked "Act 2 - slow page" (PO: not yet).
10. Windows is unverified for `api`, `cache`, `queue` and the LGTM stack (PO 2026-10-22: not before the
    talk). Attendees on Windows may hit problems.
11. `/health` and `/alive` are mapped only in Development; smtp4dev has `.WithExternalHttpEndpoints()`.
    Check both are fine for attendees.
12. Add the slides to the repo for attendees.

## Notes
- Dropped as obsolete when moved: spike cleanup (`spikes/` already deleted; `Microsoft.Data.SqlClient`
  is now needed by the smoke check, FR-0007), Azure stretch items (Azure dropped, PO 2026-10-15), and
  spec updates (OpenSpec removed).
