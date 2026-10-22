# ADR-0032: Booking queue

## Status
Accepted (2026-10-22, `add-booking-api`). Proposed 2026-10-21. Task 1.2 findings: the `api`
`deliver booking-stored` span is a child of the publish (also linked), so the consumer fallback
span was not built; the Aspire client registers the RabbitMQ sources, so ServiceDefaults is
unchanged. Image `rabbitmq:4.3-management` (package default); the image declares an anonymous
volume, so the reset uses `docker rm -f -v oisp-queue`. Deviation (PO 2026-10-22): site span
`acme.forms.publish` around the publish (ADR-0030 Status).

## Context
FR-0006 asked for a trace that crosses services through a queue. The PO (2026-10-21) put the
queue on the booking path of `add-booking-api`: the site queues "booking stored", the booking
service (ADR-0030) consumes it and updates the count (ADR-0031). A few seconds' lag is
accepted; queue contents need not persist; the restart loop must stay short (PO 2026-10-21).
The site must keep working without the queue and run alone without it (spec
`event-booking-count`). Messages carry no personal data.

## Decision
- RabbitMQ via `Aspire.Hosting.RabbitMQ` 13.6.1 (AppHost): `AddRabbitMQ("queue")
  .WithManagementPlugin()`, container `oisp-queue`, `ContainerLifetime.Persistent`, no data
  volume. Persistent lifetime keeps the broker running between AppHost runs, so restarts skip
  its boot; messages are lost only when the container is removed (reset). Management UI
  linked from the dashboard (queue depth). Image tag: the package default.
- Client via `Aspire.RabbitMQ.Client` 13.6.1 (RabbitMQ.Client 7) in `site` and `api`. Its
  built-in ActivitySources (`RabbitMQ.Client.Publisher`, `RabbitMQ.Client.Subscriber`) are
  registered by the Aspire client; the client injects W3C context into message headers on
  publish and extracts it on delivery. If delivery spans are only linked, not parented, to the
  publish (verified in `add-booking-api` task 1.2), the consumer starts its handling span with
  the extracted context as parent so the booking stays one trace.
- Queue `booking-stored`, default exchange, declared by both sides with the same arguments
  (durable, not exclusive, not auto-delete), so messages wait while `api` is stopped. Body
  JSON `{"eventKey":"<guid>"}`. No other content, headers only from trace propagation.
- Consumer (in `api`, ADR-0030): manual ack after `INCR`; on failure log Error and nack
  without requeue (no hot retry loop; the count drifts, accepted).
- Site publisher behind `IBookingEvents.PublishStoredAsync(Guid)`: registered only when
  `ConnectionStrings:queue` is set (`AddRabbitMQClient("queue")` in `Program.cs` under the same
  condition), otherwise a no-op. Connection and channel are obtained lazily inside the call;
  the whole publish is capped at 2 s, any failure logged as a Warning and swallowed. Site
  RabbitMQ health check disabled (`DisableHealthChecks`), so a stopped queue does not mark the
  site unhealthy. `api` keeps the health check and `WaitFor(queue)`.

## Alternatives
- Redis Streams or pub/sub: no extra container, but no messaging spans and manual context
  propagation; loses the point of the queue; rejected.
- MassTransit over RabbitMQ: good spans and less plumbing, but a framework and, from v9, a
  commercial licence; rejected.
- Azure Service Bus emulator: heavy (needs its own SQL container); Azure dropped (PO
  2026-10-15); rejected.
- Kafka: heavier broker, more setup than a demo needs; rejected.
- Session lifetime (container recreated per run): slower restart loop; rejected (PO
  2026-10-21).
- Data volume: messages survive container removal, which nobody needs; rejected.

## Consequences
- One more container (~100-150 MB with the management plugin) and two packages.
- Booking traces show publish (`site`) -> deliver/handle (`api`) -> Redis `INCR`.
- With `api` stopped, queue depth grows in the management UI and drains on restart.
- The database reset in the presenter runthrough also removes `oisp-queue`.
- Generated queue password must stay stable across restarts of the persistent container
  (verified in `add-booking-api` tasks).
