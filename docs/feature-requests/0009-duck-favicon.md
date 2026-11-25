# FR-0009: Duck favicon for the demo site

## Status
Open (2026-11-25), Requirements. Source: FR-0008 item 7 (favicon part); PO request 2026-11-25.

## Request
The demo site's browser tab icon shows the Rubber Duck Agency duck logo instead of the default icon.

## Why
Attendees open the site through the tunnel on their phones and laptops. A default icon makes the site
look unfinished; the duck matches the header logo and the agency brand.

## Acceptance criteria
- Every public page (Home, About us, Contact us, Events list, event pages, Services list, service
  pages, past events, form success pages) shows the duck as its tab icon.
- The icon is the whole duck from the header logo (`src/SuperPower.Web/wwwroot/assets/rubber-duck.svg`),
  same shape and colours, scaled to fit a square and centred (PO 2026-11-25: whole duck, not head only).
- The duck is recognisable at 16 px and 32 px, in light and dark browser themes.
- A direct request for `/favicon.ico` returns the duck, not the old default icon.
- The icon files live in the site's source folder, `src/SuperPower.Web/` (PO 2026-11-25), not in
  `banners/`, `docs/` or elsewhere in the repo.
- Icon requests leave no traces or logs (existing exclusion of `/favicon.ico` and `/assets`, ADR-0008).
- Works with the site run through the AppHost, standalone, and through the Cloudflare tunnel.

## Out of scope
- The Umbraco backoffice icon (`/umbraco`).
- The default 404 page (stays open in FR-0008 item 7).
- App or home-screen icons, web manifest, social sharing images.

## Tasks
Added in the Plan phase.

## Notes
- Open question: none.
