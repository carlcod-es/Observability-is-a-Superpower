# ADR-0025: Mail path without fault injection

## Status
Accepted (2026-10-15, PO). Supersedes the Toxiproxy parts of ADR-0010.

## Context
The acts were simplified the day before the talk (PO 2026-10-15). No act injects mail faults;
the production-stage problem comes from load (`add-form-emails`). Toxiproxy, its proxy set-up
and the Slow / Break / Heal commands have no remaining use. The mail catcher stays: the
presenter shows notification emails in its inbox.

## Decision
- Remove the Toxiproxy container, its proxy population hook and its three commands from the
  AppHost.
- smtp4dev publishes SMTP on fixed host port 2525 (container port 25, not proxied by Aspire);
  the site's SMTP settings keep `localhost:2525`, `SecureSocketOptions=None`. The site waits
  for smtp4dev instead of Toxiproxy.
- smtp4dev image, web UI, in-memory store and locked settings unchanged (ADR-0010).

## Alternatives
- Keep Toxiproxy idle: no code change, but an unused resource and image to explain and pull;
  rejected.
- Aspire-proxied dynamic SMTP port passed to the site by endpoint reference: equivalent, but
  changes the documented port; rejected.

## Consequences
- One container fewer; README prerequisites drop the Toxiproxy image pull.
- Smoke check drops its Slow / Break / Heal checks.
- The agent has no mail-path commands; remaining resource commands (start, stop, restart)
  still prompt (ADR-0022).
