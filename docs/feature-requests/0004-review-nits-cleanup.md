# FR-0004: Clean up review nits from `add-demo-foundation`

## Status
Open (2026-10-15). Source: `add-demo-foundation` `review-followups.md` open nits, extracted before
archive at the PO's request. Line numbers checked against `main` on 2026-10-15.

## Request
Fix the small code, test, view and doc nits that reviews accepted at the prototype bar, in one
tidy-up pass.

## Why
The repo is published for attendees after the talk. These nits do not affect the demo, but
they are visible to anyone who reads the code (dead CSS, template leftovers, obsolete-API
warnings, inaccurate comments, README gaps).

## Acceptance criteria
- Each item below is fixed or explicitly dropped by the PO.
- `dotnet build SuperPower.slnx` has no new warnings and `dotnet test --solution SuperPower.slnx`
  passes; the smoke check passes on both targets.
- No change in site behaviour or telemetry content.

### Site code (`src/SuperPower.Web/`)
1. `Events/UpcomingEvents.cs:29`: obsolete `IPublishedContent.Children` (CS0618); use the
   `Children()` extension from `Umbraco.Extensions`.
2. `Events/UpcomingEvents.cs:14`: `Filter<T>` is on the public interface only for tests; keep
   the interface to `GetUpcoming`.
3. `Forms/AcmeFormsComposer.cs:16-17`: fully qualified `SuperPower.Web.Events.*` registrations;
   add a `using`.
4. `Forms/AcmeFormsTelemetry.cs:8`: doc comment should say the ServiceDefaults literal must
   match `Name`.
5. `Forms/Storage/SubmissionJson.cs:56-61`: O(n) `FindIndex` per property; simplify if touched.
6. Submission repository: unused `cancellationToken`; pass it through or drop it.
7. Rendered forms carry inert `data-val*` attributes; set
   `HtmlHelperOptions.ClientValidationEnabled = false` in `Program.cs`.

### Views and front end (`src/SuperPower.Web/Views/`, `wwwroot/`)
8. `Views/Partials/blockgrid/*.cshtml`, `Views/Partials/singleblock/default.cshtml`: Umbraco
   template leftovers, unused; delete.
9. `wwwroot/css/site.css:258-259`: dead `.card__summary:empty` and unused `.event-list`;
   `.card__image` declared twice (233, 246); remove and merge.
10. `Views/master.cshtml:58`: banner guard duplicates `Partials/Media.cshtml:8`; call the Media
    partial and hide `.banner__image--empty`, or comment the guard.
11. `Views/Partials/Media.cshtml:12`: `Html.Raw` for width/height; use `width="@w"` attributes.
12. `Views/master.cshtml` nav: ancestor items get `aria-current="true"`; keep `aria-current` for
    the current page only.
13. Form partials: add `autocomplete` hints (`name`, `email`, `organization`, `url`).

### ServiceDefaults and AppHost
14. `src/SuperPower.ServiceDefaults/TelemetryPaths.cs:6`: doc comment lists "(health, static
    assets)"; add backoffice and media.
15. `src/SuperPower.ServiceDefaults/Extensions.cs:78`: comment quotes a measured rate (~75/min)
    that will go stale; drop the number.
16. `src/SuperPower.ServiceDefaults/SerilogForwardingSink.cs:38`: missing space in
    `ScalarValue { Value: string s}`.
17. `src/SuperPower.ServiceDefaults/SerilogForwardingSink.cs:92-108`: `PropertyToken.Alignment`
    ignored; honour it or comment.
18. `src/SuperPower.AppHost/AppHost.cs:42`: `HttpClient` per ready event, not disposed; `using var`.
19. `src/SuperPower.AppHost/AppHost.cs`: smtp4dev SMTP port 25 also published on a dynamic host
    port, bypassing Toxiproxy; drop or hide the endpoint if the container alias still resolves.
20. Build warning NU1507 (two package sources under central package management): root
    `nuget.config` with package source mapping (shared file; PO approval).

### Tests (`tests/`)
21. `tests/SuperPower.Web.Tests/FormValidationTests.cs`: three copy-pasted required-field tests;
    fold into a `[Theory]`.
22. `tests/SuperPower.Web.Tests/UpcomingEventsTests.cs`: third private `FakeTime` copy; share a
    helper.
23. `tests/SuperPower.Web.Tests/EventBookingGuardTests.cs`: add a start == now case.
24. Optional coverage: whitespace-only `[Required]` value; `Deserialize` on non-object input.
25. `tests/smoke/smoke.cs`: Service and Event `FieldsJson` checks test presence only; dispose
    `JsonDocument`s; regex form parsing is brittle.

### README and docs
26. `README.md:13,36`: Podman claim unverified; add `ASPIRE_CONTAINER_RUNTIME=podman` and a
    Podman note in the reset steps, or drop Podman.
27. `README.md` Tests section: say the full-environment smoke run starts `lgtm` itself and needs
    it not yet started.
28. `README.md:139-140`: "Home makes database calls only on its first load after start" is
    readable by the on-stage agent; keep or soften (PO).
29. `README.md:128`, `docs/project-setup.md:131`: `select_apphost` / `spikes/` notes; edit with
    the pre-talk spike removal.

### Content (PO, uSync)
30. `events.config` mainContent "Free talks and workshops" / "session" names event kinds; keep or
    reword (spec `agency-site`: no format).
31. Services `seo-audit`, `adwords-audit`, `umbraco-upgrade`: summary repeated as the first body
    sentence; keep or reword.

## Notes
- Not included (accepted as-is by reviews, no action): NU1902/NU1903 via Umbraco (ADR-0001),
  `FormSubmissionDto` double primary-key attributes (intentional), Grafana admin/admin (LGTM
  default), "Name * (required)" label rendering, `<img>` without size when media lacks it.
- Ownership on apply: items 1-7 and 14-25 worker-backend; 8-13 worker-frontend; 26-29 Architect
  brief (shared files and `docs/`); 30-31 PO.
- After the talk, unless the PO pulls any item forward.
