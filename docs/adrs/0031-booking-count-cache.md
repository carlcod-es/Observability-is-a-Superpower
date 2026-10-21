# ADR-0031: Booking count cache

## Status
Proposed (2026-10-21, `add-booking-api`)

## Context
The booking service (ADR-0030) keeps a count per event in its own store, and the cache calls
must show in traces (spec `event-booking-count`, "Booking service telemetry"). Counts survive
environment restarts and reset with the database (spec `local-environment`, "One-command
start"). Packages already in the Aspire 13.6.1 line used by the AppHost (ADR-0001).

## Decision
- Redis via `Aspire.Hosting.Redis` 13.6.1 (AppHost): `AddRedis("cache")`, container
  `oisp-cache`, data volume `oisp-cache-data`, `ContainerLifetime.Persistent` (as `oisp-sql`,
  ADR-0009). Image tag: the package default.
- Client via `Aspire.StackExchange.Redis` 13.6.1 in `api` only: `AddRedisClient("cache")`,
  tracing on (package default), so each command is a span on the `api` resource.
- Keys `event:{eventKey}:bookings`; `INCR` per `booking-stored` message (ADR-0032), `GET` on
  read; no expiry.
- Agents get no cache access (CLAUDE.md "Scope"); no Redis UI resource.

## Alternatives
- In-memory cache inside `api`: no separate resource and no cache spans; rejected.
- Valkey or Garnet: Redis-compatible, but less familiar on stage and the Aspire Redis
  integration is first-party; rejected.
- SQL Server table owned by `api`: shares the site's database, adds migrations; the PO asked
  for a cache; rejected.
- Non-persistent cache: counts reset on every restart while bookings stay; confusing for
  attendees; rejected.

## Consequences
- Two new packages under central package management.
- Database reset in the presenter runthrough also removes `oisp-cache` and `oisp-cache-data`.
- Redis persistence snapshots on an interval: counts from the last seconds before a hard stop
  can be lost; accepted.
- StackExchange.Redis spans are collected by a profiler and flushed on an interval, so they
  can reach the dashboard a few seconds after the request spans.
- Generated cache password must stay stable across restarts of the persistent container
  (verified in `add-booking-api` tasks).
