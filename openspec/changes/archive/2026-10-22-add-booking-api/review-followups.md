# Review follow-ups: add-booking-api

Minors and nits from block and section reviews. Read at the start of every apply session.

## Block 2 (tasks 2.1-2.4), reviewer APPROVED 2026-10-22
- minor `tests/SuperPower.Web.Tests/BookingEventsTests.cs:25`, `:33`: timing assertions (< 2 s throwing case; 1.5-3.5 s blocking case) could flake on a slow machine. Widen only if flakiness shows.
- nit `src/SuperPower.Web/Events/BookingEvents.cs:19`: abandoned `Task.Run` continues after the 2 s timeout; later faults unobserved. Harmless; matches design.md D7.
- nit Tests use `NullLogger`, so the Warning logs are not asserted. Not required by design.md D10.
- note Resilience options (2 s total / 1 s attempt) are validated lazily on first client creation; confirm in the AppHost run (section 3/4).

## Section 2 supervisor APPROVED 2026-10-22
- minor `src/SuperPower.Web/Forms/AcmeFormsComposer.cs:29-37`: 2 s cap relies on `ConfigureHttpClientDefaults` (ServiceDefaults) being applied before the typed client's `RemoveAllResilienceHandlers()`. Task 4.2(a) must confirm: `api` stopped -> event page ~2 s extra, trace shows ~1 s attempts, no 10 s attempt.
- minor `src/SuperPower.Web/Events/BookingEvents.cs:21,31`: with `queue` down, each booking parks a thread-pool thread in `IConnection` resolution (Aspire connect retries). Task 4.2(b): book several times in a row with `queue` stopped; event pages stay responsive.
- nit `src/SuperPower.Web/Forms/AcmeFormsComposer.cs:18-19`: fully qualified `SuperPower.Web.Events.*` names now redundant with the using.
- nit `src/SuperPower.Web/Forms/AcmeFormsController.cs:114`: `formKey == AcmeFormKeys.EventBooking` would be sturdier than the slug string.

## Blocks 1a + 1b (tasks 1.1-1.5), reviewer APPROVED 2026-10-22
- nit `src/SuperPower.Api/BookingStoredConsumer.cs:30`: `JsonDocument.Parse(ea.Body)` not disposed; `using var doc = ...`.
- nit `src/SuperPower.Api/BookingStoredConsumer.cs:41`: `BasicNackAsync` in the catch can throw if the channel closed; RabbitMQ.Client logs and swallows it.
- nit `src/SuperPower.Api/BookingStoredConsumer.cs:21`: exception in `ExecuteAsync` stops the host; acceptable with `WaitFor(queue)`.
- note `api` keeps `Properties/launchSettings.json` (http://localhost:5290, local only, no external endpoint).
- note task 1.2 findings: deliver span is a child of the publish (also linked), RabbitMQ/Redis spans appear without ServiceDefaults changes; neither D5 nor D9 fallback applied. Restart-to-`api`-Running 14 s.

## Block 3 (task 3.1), reviewer APPROVED 2026-10-22
- nit `src/SuperPower.Web/Views/Event.cshtml:12`: fully qualified `SuperPower.Web.Events.BookingCountText`; consistent with the file's `IEventSchedule` inject, no change.

## Section 1 supervisor APPROVED 2026-10-22
- minor Spec "Booking service telemetry" production-stage logs/metrics (Loki `{service_name="api"}`, Prometheus `job="api"`) not checked by any task; Architect observes during the 4.1 AppHost smoke run.
- minor `tests/smoke/smoke.cs` booking-trace check must target the deliver span (an event page trace also has site + api spans with `acme.event.key`); fixed in block 4a rework.
- minor `api` recovery after `queue` / `cache` restart (without restarting `api`) not checked in 4.2(b)/(c); Architect checks after block 4b.
- nit `src/SuperPower.Api/BookingStoredConsumer.cs:30`, `src/SuperPower.Api/Program.cs:19`: `SetTag` passes a boxed Guid; exporter stringifies. Optional `.ToString()`.
- nit Block 1a+1b line refs above are off by 1-3 (`JsonDocument.Parse` :29, `BasicNackAsync` :38, `ExecuteAsync` :19).
- nit `src/SuperPower.Api/Properties/launchSettings.json:6`: fixed host port 5290; clash would stop `api`. Low risk.
- note README/runthrough reset: `docker rm -f -v oisp-queue` (RabbitMQ image declares an anonymous volume); fixed in block 4a rework; PO task 6.1 needs the same.
- note No ServiceDefaults change (both fallbacks unnecessary): record in ADR-0032 (task 5.1).

## Block 4a (tasks 4.1, 4.4), reviewer APPROVED 2026-10-22
- minor `README.md` "Run the full environment", `queue` bullet: "Aspire dashboard, Parameters/Resources view" is vague; name where `queue-password` shows.
- nit `README.md` "Run the full environment" intro and "Run the site on its own": long unwrapped lines.
- nit `README.md` pre-pull size 5.2 GB is an estimate.

## Block 4b (tasks 4.2, 4.3) runtime observations 2026-10-22
- note 4.2(c): failed Redis client span has no Error status (instrumentation); parent `api` server span is Error.
- note 4.2(c): count dropped by one across a `cache` restart (Redis `--save 60 1`); accepted drift.
- note Test bookings (ACME-0201..0216) and owner emails remain in the local DB/inbox; PO morning reset clears them.

## Rework block 2 (`acme.forms.publish`), reviewer APPROVED 2026-10-22
- nit `tests/SuperPower.Web.Tests/BookingEventsTests.cs:33`: listener filter literal "Acme.Forms"; use the telemetry name constant.
- nit `tests/SuperPower.Web.Tests/BookingEventsTests.cs:42`: test does not assert the `acme.event.key` tag.

## Section 4 supervisor APPROVED 2026-10-22 (sections 1-4 integration)
- minor `.gitignore:24`: `tests/load/results/` ignored (not from a section 1-4 task); design.md D11 / task 7.1 expect results there. PO to confirm intent; if kept, note in design.md D11 that results are local only.
- minor `tests/smoke/smoke.cs:471-477`: count check needs exactly before+1; another booking on the same event within 5 s (traffic generator, load burst) fails it. Add "traffic generator not running" to the smoke header preconditions, or accept `now > before`.
- nit `src/SuperPower.Web/Forms/AcmeFormsController.cs:87-91, 114`: spec scenario "Invalid booking: no message queued" holds by code order (early return before publish); no test or recorded check.
- nit `.gitignore:24`: no trailing newline.
- nit `README.md:153`: long unwrapped line (same as Block 4a nit).
- note `dotnet test SuperPower.slnx` needs `--solution` on this SDK: `dotnet test --solution SuperPower.slnx` -> 75 passed.
- note Section 1 supervisor follow-ups all resolved (verification.md 4.1, 4.2(b)/(c); `smoke.cs` deliver-span check; README `-v`). Section 2 supervisor minors resolved by verification.md 4.2(a)/(b).
