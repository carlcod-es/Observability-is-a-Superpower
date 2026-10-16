# ADR-0010: Local mail path and fault injection

## Status
Accepted (2026-10-07). Toxiproxy and the Slow / Break / Heal commands superseded by ADR-0025
(2026-10-15); smtp4dev decision still holds.

## Context
Spec `local-environment` requires a local mail catcher and Slow / Break / Heal controls on
the mail path. Localhost SMTP is too fast to show realistic latency. Umbraco 17 sends mail with
MailKit through `Umbraco:CMS:Global:Smtp` (`SecureSocketOptions` defaults to `Auto`), one
connection per message, MailKit default I/O timeout 120 s. SMTP is lock-step (about 6-7 server
replies per message), so a downstream latency toxic multiplies per message.

## Decision
- smtp4dev `rnwood/smtp4dev:3.15.0` container: web UI port 80, SMTP 25, TLS off, in-memory
  store, settings locked.
- Toxiproxy `ghcr.io/shopify/toxiproxy:2.12.0` container: API port 8474 (health `/version`),
  proxy `smtp` listening on 2525 with upstream `smtp4dev:25` (container network alias). The
  2525 endpoint is not proxied by Aspire (fixed port) so connection resets reach the client
  unchanged.
- Proxy creation: on Toxiproxy resource ready, `POST /populate` (idempotent).
- Dashboard commands on the Toxiproxy resource via `WithHttpCommand`:
  - Slow: latency toxic, downstream, fixed default value set in the AppHost.
  - Break: `reset_peer` toxic (fast failure; avoids the 120 s hang of a timeout toxic).
  - Heal: `POST /reset` (removes all toxics).
- Site SMTP settings come from AppHost environment variables pointing at the Toxiproxy
  endpoint, `SecureSocketOptions=None`.

## Alternatives
- MailPit (has an Aspire toolkit integration and its own chaos features): presenter knows
  smtp4dev; Toxiproxy gives one fault mechanism for any TCP dependency; rejected.
- A .NET Toxiproxy client: main client unmaintained since 2018; four HTTP calls do not need a
  library; rejected.
- Timeout toxic for Break: two-minute hangs on stage; rejected.

## Consequences
- No external mail can leave the machine.
- Fixed port 2525 must be free on the presenter's machine.
- Resource commands are also callable by the agent through MCP; ADR-0011 requires approval.
