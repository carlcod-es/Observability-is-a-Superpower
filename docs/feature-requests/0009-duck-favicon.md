# FR-0009: Duck favicon for the demo site

## Status
In progress (2026-11-25); requirements and plan approved by the PO 2026-11-25 ("plan and build"). Source: FR-0008 item 7 (favicon part); PO request 2026-11-25.

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
Bar: demo. One SVG and one ICO, no new dependency, no code outside the layout. Validation:
`dotnet build SuperPower.slnx`, `dotnet test SuperPower.slnx`; with the site running, `/favicon.ico`
and `/assets/favicon.svg` return the duck and a page's `<head>` links both.

### 1. Icon files (Architect; `src/SuperPower.Web/wwwroot/`; no dependencies)
- [x] 1.1 `wwwroot/assets/favicon.svg`: the header logo's shapes and colours unchanged, square viewBox
  around the whole duck, centred.
- [x] 1.2 `wwwroot/favicon.ico` replaced: 16 px and 32 px PNG images rendered from 1.1 (one-off, macOS
  `sips` plus a short script in the scratchpad; nothing added to the repo).

### 2. Layout (Architect; `src/SuperPower.Web/Views/master.cshtml`; depends on 1)
- [x] 2.1 `<head>` links `/favicon.ico` (sizes 32x32) and `/assets/favicon.svg` (type image/svg+xml).

### Follow-ups
- None yet.

## Notes
- Open question: none.
- Plan (Architect, 2026-11-25): SVG for browsers that take it (sharp at any size and pixel density),
  ICO for the rest and for direct `/favicon.ico` requests. The SVG goes under `/assets`, already excluded
  from tracing (ADR-0008), so `TelemetryPaths` needs no change. Alternatives: ICO only (blurry on
  high-density screens); PNG set or web manifest (more files; manifest out of scope). Dark theme: the
  yellow duck has enough contrast on dark tab strips; no theme-specific variant.
