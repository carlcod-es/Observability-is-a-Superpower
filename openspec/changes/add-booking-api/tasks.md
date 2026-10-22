Bar: demo (design.md Context). No planted problems in this change (PO 2026-10-21). Order: 7.1
(baseline, before any code) -> 1 and 2 in parallel (disjoint files; contracts fixed in design.md
D1, D3, D6, D7) -> 3 -> 4 -> 7.2 -> 5; 6 (PO) after 4.

## 1. AppHost, booking service, cache and queue (worker-backend; `src/SuperPower.AppHost/`, new `src/SuperPower.Api/`, `src/SuperPower.ServiceDefaults/` only for D9's conditional source; shared by brief: `Directory.Packages.props`, `SuperPower.slnx`; no dependencies)

- [x] 1.1 Add `Aspire.Hosting.Redis`, `Aspire.StackExchange.Redis`, `Aspire.Hosting.RabbitMQ` and `Aspire.RabbitMQ.Client` 13.6.1 to `Directory.Packages.props` (ADR-0031, ADR-0032); verify `dotnet restore SuperPower.slnx` succeeds with no version warnings
- [x] 1.2 Create `src/SuperPower.Api` (.NET 10 web, minimal API) referencing ServiceDefaults, added to `SuperPower.slnx`; `AddServiceDefaults`, `AddRedisClient("cache")`, `AddRabbitMQClient("queue")`, `MapDefaultEndpoints`; `GET /events/{eventKey:guid}/bookings` and the `booking-stored` consumer as design.md D1, D2, D5; add AppHost resources as D4; publish one test message from a scratch snippet or the management UI (with a `traceparent` header) and check in the Aspire dashboard whether the `api` deliver span is a child of the publish or only linked, and whether RabbitMQ spans appear without adding sources to ServiceDefaults; apply the D5/D9 fallbacks only if needed; verify `dotnet build SuperPower.slnx` passes and report both findings
- [x] 1.3 AppHost: `site.WithReference(api).WithReference(queue)` with no `WaitFor` on either, no external endpoint on `api` (design.md D4); verify `dotnet build SuperPower.slnx` passes
- [x] 1.4 Run the AppHost: `cache`, `queue` and `api` reach Running; a message `{"eventKey":"<fixed guid>"}` published to `booking-stored` from the management UI makes `GET` on `api` for that GUID return 1; the RabbitMQ management UI opens from the dashboard; verify by reporting each observation
- [x] 1.5 Stop and restart the AppHost: `cache` and `queue` are not recreated (container start times unchanged), `GET` still returns 1, a second message makes it 2 (persistent passwords); the `api` resource shows traces with Redis spans and logs in the Aspire dashboard; with `lgtm` started, Grafana Tempo has a trace with `resource.service.name="api"`; verify by reporting each observation and the AppHost restart-to-`api`-Running time (design.md Risks)

## 2. Site client, publisher and booking post (worker-backend; `src/SuperPower.Web/` C# only incl. `Program.cs`, `tests/SuperPower.Web.Tests/`; depends on design.md D1, D3 contracts, not on section 1 files)

- [x] 2.1 Add `IBookingCounts`, `BookingCountsClient`, `NoBookingCounts` and `BookingCountText` in `src/SuperPower.Web/Events/BookingCounts.cs`, registered in `AcmeFormsComposer` with the `services:api` switch and the 2 s / 1 s resilience pipeline (design.md D6, ADR-0030); verify `dotnet build SuperPower.slnx` passes
- [x] 2.2 Add `IBookingEvents`, `RabbitBookingEvents`, `NoBookingEvents` in `src/SuperPower.Web/Events/BookingEvents.cs`; `Program.cs` adds `AddRabbitMQClient("queue", …DisableHealthChecks = true)` only when `ConnectionStrings:queue` is set; composer registers under the same condition (design.md D7, ADR-0032); verify `dotnet build SuperPower.slnx` passes
- [x] 2.3 `AcmeFormsController`: call `PublishStoredAsync(CurrentPage.Key)` only after an event booking is stored, before `Notify`; contact and service enquiry unchanged (design.md D7); verify `dotnet build SuperPower.slnx` passes and the diff touches no other form path
- [x] 2.4 xUnit tests per design.md D10 (`BookingCountText`, `BookingCountsClient`, `RabbitBookingEvents` fail-soft and 2 s cap, registration switches if testable without Umbraco runtime); verify `dotnet test SuperPower.slnx` passes, output reported verbatim

## 3. Event page (worker-frontend; `src/SuperPower.Web/Views/Event.cshtml`, `src/SuperPower.Web/wwwroot/css/site.css`; depends on 2)

- [x] 3.1 `Event.cshtml`: inject `IBookingCounts`, render `<p class="event-bookings">` with `BookingCountText.Format(await GetAsync(Model.Key))` when non-null, under the event date, upcoming and past events; style to match `.event-when` only if needed (design.md D8, ADR-0021); verify `dotnet build SuperPower.slnx` passes and, with the AppHost running, an event page with no bookings shows no count text and within 5 s of one booking shows "1 person has booked this event"

## 4. Smoke check and README (worker-backend by brief; shared: `tests/smoke/smoke.cs`, `README.md`; depends on 1, 2, 3)

- [x] 4.1 `tests/smoke/smoke.cs` per design.md D10 (apphost: count rises within 5 s, `api` spans with `acme.event.key`, one booking trace with `site` and `api` spans; standalone: no `event-bookings`, booking returns a reference); verify `dotnet run tests/smoke/smoke.cs` and `dotnet run tests/smoke/smoke.cs -- standalone` report the new checks as passed (existing expected results unchanged)
- [x] 4.2 Fail-soft checks, AppHost running (spec `event-booking-count`, "Site works without the booking service", "Service back after an outage"): (a) stop `api`, open an event page and book twice: page renders without count text within ~2 s extra, bookings show references, owner emails arrive, trace shows the failed client call and a site Warning log, management UI shows 2 messages waiting; start `api`: within 5 s the count includes both. (b) stop `queue`, book: reference and email as normal, trace shows the failed publish and a site Warning log, `/health` on site stays healthy; start `queue`. (c) stop `cache`, open an event page: no count text, failed Redis span on `api`; start `cache`. Verify by reporting each observation
- [x] 4.3 PII check: book with name `Canary Person`, email `canary@test.invalid`; search `api` traces and logs in the Aspire dashboard and the message body in the management UI (get message, before consumption with `api` stopped) for both values; verify neither appears (spec `event-booking-count`, "No personal data to the booking service")
- [x] 4.4 README: add `api`, `cache` and `queue` (with the management UI link) to the resource list and "Run the full environment"; "Reset the databases" also removes `oisp-cache`, `oisp-cache-data` and `oisp-queue`; "Pre-pull the container images" adds the Redis and RabbitMQ images; "Run the site on its own" notes no booking count; no planted problems described; verify every command added runs as written on macOS (PO checks Windows in 6.2)

## 5. Docs and planning (Architect; `docs/`, `openspec/`, `PLANNING.md`, `CLAUDE.md`; depends on 4)

- [x] 5.1 ADR-0030, ADR-0031 and ADR-0032 Status -> Accepted with any deviations (incl. task 1.2 findings); FR-0006 Status -> delivered by `add-booking-api`; CLAUDE.md "Stack", "Local environment" and "Telemetry" mention `api`, `cache` and `queue`; PLANNING.md change order and todos updated; verify `openspec validate add-booking-api --strict` passes and no doc describes a planted problem

## 6. Presenter (PO; `docs/presenter/`; depends on 4)

- [ ] 6.1 (PO) Runthrough: morning database reset also runs `docker rm -f oisp-cache oisp-queue` and `docker volume rm oisp-cache-data`; Morning step 1 notes the new resources; verify a reset followed by start shows all event pages without count text and an empty `booking-stored` queue
- [ ] 6.2 (PO) Run the AppHost on Windows; verify `cache`, `queue` and `api` reach Running, an event page trace shows `site` and `api`, and a booking trace shows both
- [ ] 6.3 (PO) Choose acts and problems for the API/queue/cache layers (later; not in this change's acceptance); verify they are recorded only in the runthrough

## 7. Load comparison (Architect runs local; PO runs tunnel and watches memory; no file changes except `verification.md` and `tests/load/results/`; 7.1 before section 1, 7.2 depends on 4)

- [x] 7.1 Baseline on `main` before any change code, presenter Mac, conditions as design.md D11: local run, tunnel run (PO), each with the concurrent booking burst; record per-page avg/p50/p95/failures, burst outcome, Docker and site memory, swap in `verification.md`; verify both result files exist in `tests/load/results/` with labels `baseline-local` / `baseline-tunnel` and the table is complete
- [x] 7.2 Repeat 7.1 after section 4 under the same conditions with labels `after-local` / `after-tunnel` and `--compare` against the baseline files; also record `queue` drain time after the burst and `api` memory; verify the comparison table in `verification.md` against the D11 criteria and report each as met / not met to the PO (PO decides on any not met)
