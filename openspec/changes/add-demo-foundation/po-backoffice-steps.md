# PO backoffice steps: add-demo-foundation

Checklist for the PO tasks in `tasks.md`: `add-demo-foundation` task 3.3 (editor check) and
tasks 6.1–6.3. Work top to bottom. Tell the Architect when done; no agent runs the site
meanwhile.

Aliases in **bold** are read by code (tasks 3.8, 5.2, 5.4, 7.1). Use them exactly. Names are
free text.

Backoffice: `https://localhost:44368/umbraco` (standalone run). Login: the Unattended user in
`src/SuperPower.Web/appsettings.Development.json`.

## 6.1 Fresh install

- [x] Stop any running site.
- [x] Delete `src/SuperPower.Web/umbraco/Data/Umbraco.sqlite.db*` and
      `src/SuperPower.Web/umbraco/Data/TEMP/`. This also removes the round-trip test rows.
- [x] `dotnet run --project src/SuperPower.Web`.
- [x] Check: the site installs; Settings > Document Types shows "Form" (`superPowerForm`) and
      the five field element types (Text, Email, Textarea, Checkbox, Select field); the
      existing Home, About us and Contact us pages are there and published.

Author all content on this standalone SQLite run. The SQL Server path is verified later from
the uSync files (task 9.2).

## 6.2 Document types and templates (ADR-0014, design.md "Site structure")

- [ ] `compPage`: add property **`form`**: a picker for one document, allowed type Form
      (`superPowerForm`) only, optional. If the plain Document Picker cannot restrict the
      type, use a Multinode Treepicker (max 1 item, allowed type `superPowerForm`).
- [ ] `compPage`: remove the image block from the `mainContent` Block List. Delete the empty
      media folder (spec `agency-site` "No media library dependency").
- [ ] Data type for start dates: Date Time with time zone, time zones limited to
      Europe/London.
- [ ] Composition **`eventBase`**: compositions `compPage`; property **`startDate`** (the data
      type above, mandatory).
- [ ] Document type **`eventInPerson`** (name e.g. "Event (in person)"): composition
      `eventBase`; property **`capacity`** (Numeric, mandatory, minimum 1); template Event.
- [ ] Document type **`eventOnline`** (name e.g. "Event (online)"): composition `eventBase`
      only; template Event.
- [ ] Document type **`events`** (name "Events"): composition `compPage`; template Events;
      allowed children `eventInPerson`, `eventOnline`.
- [ ] Document type **`service`**: composition `compPage`; template Service.
- [ ] Document type **`services`** (name "Services"): composition `compPage`; template
      Services; allowed child `service`.
- [ ] Document type **`formsFolder`** (name "Forms folder"): allowed at root, no template, no
      properties; allowed child Form (`superPowerForm`).
- [ ] Home (`home`): allow `events` and `services` as children, next to `page`.
- [ ] Templates with aliases **`events`**, **`event`**, **`services`**, **`service`**: empty
      bodies are fine; worker-frontend fills them in task 7.1.
- [ ] Check: saving an in-person event with no capacity, or capacity 0, is refused; an online
      event shows no capacity property; uSync files appear under
      `src/SuperPower.Web/uSync/v17/`.

## 6.3 Content (spec `agency-site`, spec `form-definitions` "Site form definitions")

### Forms (create first; pages pick them)

- [ ] At the root: a Forms folder node named "Forms".
- [ ] Under it, three Form nodes. Field aliases in bold; types and required flags exactly as
      listed. Labels are free text.

**Contact** (success message e.g. "Thanks, we will be in touch.")

| Field type | Alias | Label | Required |
|---|---|---|---|
| Text | **`name`** | Name | yes |
| Email | **`email`** | Email | yes |
| Textarea | **`message`** | Message | yes |
| Checkbox | **`replyByPhone`** | Reply by phone? | no |

**Service enquiry** (success message e.g. "Thanks for your enquiry.")

| Field type | Alias | Label | Required |
|---|---|---|---|
| Text | **`name`** | Name | yes |
| Email | **`email`** | Email | yes |
| Text | **`company`** | Company | no |
| Text | **`website`** | Website | no |
| Textarea | **`message`** | Message | yes |

**Event booking** (success message exactly **"Booking received"**)

| Field type | Alias | Label | Required | Options |
|---|---|---|---|---|
| Text | **`name`** | Name | yes | |
| Email | **`email`** | Email | yes | |
| Select | **`places`** | Places | yes | `1`, `2`, `3`, `4` in this order |
| Textarea | **`notes`** | Notes | no | |
| Checkbox | **`joinMailingList`** | Join mailing list | no | |

No patterns are needed. Optional: add one to see it work, e.g. Website pattern `https?://.+`
with message "Enter a full web address".

### Task 3.3 editor check (while creating the forms)

- [ ] On a form, add, remove and reorder fields of all five types; set the success message;
      save and reload: the order and values persist.
- [ ] On the Places Select field: the options keep the order entered.
- [ ] Report the result to the Architect (ticks task 3.3).

### Pages

- [ ] Home: introduction text (Acme Agency, fictional marketing agency).
- [ ] About us: keep or adjust.
- [ ] Contact us: **`form`** → Contact.
- [ ] Events (under Home): **`form`** → Event booking. Children:
  - [ ] at least two `eventInPerson`, each with a capacity, e.g. "How to get the most out of
        your CMS" (capacity 20) and a second one (capacity 12);
  - [ ] at least one `eventOnline`;
  - [ ] every `startDate` after 16 Oct 2026, Europe/London. Leave at least one in-person event
        with a small capacity for later demo acts.
- [ ] Services (under Home): **`form`** → Service enquiry. Children (`service`): Umbraco
      upgrade, SEO audit, AdWords audit, each with a short description.
- [ ] Publish everything (pages, events, services, forms folder and forms).
- [ ] Check: uSync content files exist under `src/SuperPower.Web/uSync/v17/Content/`.

Pages render with the old Page template or blank templates until task 7.1; forms appear once
task 7.1 adds the ViewComponent call. That is expected.

## Hand-back

- [ ] Stop the site.
- [ ] Tell the Architect: 3.3 result, anything that differed from this list.
- [ ] Commit when ready (your call). The uSync files are yours.
