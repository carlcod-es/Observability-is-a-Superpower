# PO backoffice steps: add-demo-foundation

Checklist for the PO tasks in `tasks.md`: `add-demo-foundation` tasks 6.1-6.3. Work top to
bottom, only after task 3.1 (forms package removal) has landed. Tell the Architect when done;
no agent runs the site meanwhile.

Rewritten 2026-10-13 for ADR-0016 (fixed site forms: no form nodes, folder or picker) and
ADR-0014 (one `event` type, no format or capacity). The earlier package-plan version, and its
completed 6.1/6.2 steps, are in git history. The `form` picker it added to `compPage` is
removed in 6.2 below.

Aliases in **bold** are read by code (tasks 3.4, 3.5, 5.1, 7.1). Use them exactly. Names are
free text.

Backoffice: `https://localhost:44368/umbraco` (standalone run). Login: the Unattended user in
`src/SuperPower.Web/appsettings.Development.json`.

Re-verified against uSync 2026-10-14 (Architect): 6.1 done; 6.2 structure done; the boxes
still unticked below are what remains. Work on the standalone SQLite run, let uSync export,
then hand back for re-verification.

## 6.1 Fresh install (after task 3.1)

- [x] Stop any running site and AppHost.
- [x] Delete `src/SuperPower.Web/umbraco/Data/Umbraco.sqlite.db*` and
      `src/SuperPower.Web/umbraco/Data/TEMP/`. This also removes old test rows.
- [x] Remove the SQL Server data volume so the next AppHost run also starts fresh.
- [x] `dotnet run --project src/SuperPower.Web`.
- [x] Check: the site installs; the existing Home, About us and Contact us pages are there and
      published; Settings > Document Types shows no "Form" type and no field element types.
      (Ticked as task 6.1, 2026-10-14.)

Author all content on this standalone SQLite run. The SQL Server path is verified later from
the uSync files (task 9.2).

## 6.2 Document types and templates (ADR-0014, design.md "Site structure")

- [x] `compPage`: remove the **`form`** picker property (verified gone from uSync).
- [x] ~~Remove the Image Block / delete the "products" media folder~~ — PO decision
      2026-10-14: both stay (media pickers and content planned later). Recorded in
      review-followups.md.
- [x] Data type **"Date Picker with time"** (the one `event.startDate` uses): limited to
      Europe/London (verified in uSync 2026-10-14).
- [x] ~~Delete the data type "Date Time Picker (with time zone)"~~ — PO decision 2026-10-14:
      stays (could not be deleted). Recorded in review-followups.md.
- [x] Document type **`event`** (name "Event"): composition `compPage`; property
      **`startDate`** (the data type above, mandatory); template Event.
- [x] Document type **`events`** (name "Events"): composition `compPage`; template Events;
      allowed child `event`.
- [x] Document type **`service`** (name "Service"): composition `compPage`; template Service.
- [x] Document type **`services`** (name "Services"): composition `compPage`; template
      Services; allowed child `service`.
- [x] Document type **`contact`** (name "Contact"): composition `compPage`; template Contact.
- [x] Home (`home`): allow `events`, `services` and `contact` as children, next to `page`.
- [x] Templates with aliases **`events`**, **`event`**, **`services`**, **`service`**,
      **`contact`**: empty bodies are fine; worker-frontend fills them in task 7.1.
- [x] Check: saving an event without a start date is refused (`startDate` mandatory in uSync;
      PO confirmed). Task 6.2 ticked 2026-10-14.

## 6.3 Content (spec `agency-site`)

No form nodes: the three forms are site code (ADR-0016); pages show them by page type once
task 7.1 lands.

Re-verified 2026-10-14 after the PO's clean export: tree, types, titles, dates and published
state are all right. What remains is text (spec `agency-site`: events and services each need
a description; Home needs the introduction).

- [ ] Home: replace the placeholder body text (currently pasted filler about Borderlands
      vault cards) with an Acme Agency introduction. Must not survive to the talk or the
      published repo.
- [x] About us: keep or adjust (PO's call; kept).
- [x] Contact us: recreate as document type `contact` (verified: node is type `contact`).
- [x] Events: five `event` children with real titles, every **`startDate`** after
      16 Oct 2026 (verified: 20 Oct – 13 Nov 2026), published.
- [ ] Events: each event needs a description (all five have empty `summary` and
      `mainContent`).
- [x] Services: Umbraco upgrade, SEO audit, AdWords audit, published.
- [ ] Services: each service needs a short description (all three have empty `summary` and
      `mainContent`).
- [x] Publish everything (pages, events, services).
- [x] Check: uSync content files exist under `src/SuperPower.Web/uSync/v17/Content/` with no
      test-named nodes left.

Pages render with the old Page template or blank templates until task 7.1; forms appear once
task 7.1 renders the partials. That is expected.

## Hand-back

- [ ] Optional, saves the 7.1 worker a step: Settings > Models Builder > Generate models
      (SourceCodeManual into `src/SuperPower.Web/Models/`, configured 2026-10-14).
- [ ] Stop the site.
- [ ] Tell the Architect: anything that differed from this list.
- [ ] Commit when ready (your call). The uSync files are yours.
