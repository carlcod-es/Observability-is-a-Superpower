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

## 6.1 Fresh install (after task 3.1)

- [ ] Stop any running site and AppHost.
- [ ] Delete `src/SuperPower.Web/umbraco/Data/Umbraco.sqlite.db*` and
      `src/SuperPower.Web/umbraco/Data/TEMP/`. This also removes old test rows.
- [ ] Remove the SQL Server data volume so the next AppHost run also starts fresh.
- [ ] `dotnet run --project src/SuperPower.Web`.
- [ ] Check: the site installs; the existing Home, About us and Contact us pages are there and
      published; Settings > Document Types shows no "Form" type and no field element types.

Author all content on this standalone SQLite run. The SQL Server path is verified later from
the uSync files (task 9.2).

## 6.2 Document types and templates (ADR-0014, design.md "Site structure")

- [ ] `compPage`: remove the **`form`** picker property (added under the old plan; still in
      uSync).
- [ ] `compPage`: remove the image block from the `mainContent` Block List. Delete the empty
      media folder (spec `agency-site` "No media library dependency").
- [ ] Data type for start dates: Date Time with time zone, time zones limited to
      Europe/London.
- [ ] Document type **`event`** (name "Event"): composition `compPage`; property
      **`startDate`** (the data type above, mandatory); template Event.
- [ ] Document type **`events`** (name "Events"): composition `compPage`; template Events;
      allowed child `event`.
- [ ] Document type **`service`** (name "Service"): composition `compPage`; template Service.
- [ ] Document type **`services`** (name "Services"): composition `compPage`; template
      Services; allowed child `service`.
- [ ] Document type **`contact`** (name "Contact"): composition `compPage`; template Contact.
- [ ] Home (`home`): allow `events`, `services` and `contact` as children, next to `page`.
- [ ] Templates with aliases **`events`**, **`event`**, **`services`**, **`service`**,
      **`contact`**: empty bodies are fine; worker-frontend fills them in task 7.1.
- [ ] Check: saving an event without a start date is refused; uSync files appear under
      `src/SuperPower.Web/uSync/v17/` and `compPage` no longer has the `form` property.

## 6.3 Content (spec `agency-site`)

No form nodes: the three forms are site code (ADR-0016); pages show them by page type once
task 7.1 lands.

- [ ] Home: introduction text (Acme Agency, fictional marketing agency).
- [ ] About us: keep or adjust.
- [ ] Contact us: recreate as document type `contact` (replace the old `page` node), name
      "Contact us".
- [ ] Events (under Home, type `events`): at least three `event` children, e.g. "How to get
      the most out of your CMS"; every **`startDate`** after 16 Oct 2026, Europe/London.
- [ ] Services (under Home, type `services`): `service` children Umbraco upgrade, SEO audit,
      AdWords audit, each with a short description.
- [ ] Publish everything (pages, events, services).
- [ ] Check: uSync content files exist under `src/SuperPower.Web/uSync/v17/Content/`.

Pages render with the old Page template or blank templates until task 7.1; forms appear once
task 7.1 renders the partials. That is expected.

## Hand-back

- [ ] Stop the site.
- [ ] Tell the Architect: anything that differed from this list.
- [ ] Commit when ready (your call). The uSync files are yours.
