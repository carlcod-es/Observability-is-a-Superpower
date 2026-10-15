## Why

The Umbraco demo site needs a visual design before its views are built: today `add-demo-foundation`
defines content and behaviour but no look. A standalone static prototype lets the PO see and adjust
the whole site quickly, then serves as the markup and style basis for the Umbraco templates, instead
of designing inside Umbraco where iteration is slower.

## What Changes

- A standalone static site (plain HTML, CSS and JS; no CMS, no backend) with the pages: Home,
  About us, Services, Events, Blog, Contact us, plus one sample detail page each for a service,
  an event and a blog post, and a thank-you page.
- Four layouts:
  - Home: main banner, breadcrumb, sections for Services, Events and Latest articles, a
    newsletter signup box, footer.
  - Text page: banner, breadcrumb, title, body, footer (About us; basis for detail pages).
  - Listing page: banner, breadcrumb, list of children each with title, image, summary and a
    link (Services, Events, Blog).
  - Contact page: banner, breadcrumb, Contact form, footer.
- The three demo forms as static markup on their pages: Contact form on Contact us, Service
  enquiry form on the sample service page, Event booking form on the sample event page. Forms do
  not submit anywhere; each links to the static thank-you page.
- Blog and the newsletter signup are prototype-only visual content (PO 2026-10-14): they do not
  join the Umbraco demo scope, and the newsletter box has no form behaviour.
- Clean, simple styling consistent with the demo site's constraints: no third-party front-end
  frameworks, externally hosted fonts or tracking scripts at runtime, and pages work without
  client-side scripting (spec `agency-site` "Minimal page dependencies").
- Rubber Duck Consulting theme (PO 2026-10-14, prototype-only; the Umbraco demo scope stays
  Acme Agency): brand, tagline "We listen.", rubber-duck copy and illustrations, self-hosted
  Baloo 2/Nunito webfonts and duck palette (design D6, ADR-0019).

Out of scope:
- Any Umbraco integration (templates, document types); that happens in the existing demo changes.
- Form submission handling, validation or any backend behaviour.
- Changing the demo site's product scope: no Blog or newsletter in `agency-site`.

## Capabilities

### New Capabilities
- `site-prototype`: the standalone static prototype - its pages, layouts, static form markup and
  styling constraints.

### Modified Capabilities
<!-- none: the prototype does not change the Umbraco demo site's requirements -->

## Impact

- New standalone directory in the repo holding the prototype and any build tooling; no change to
  the solution, the Umbraco site or existing tests.
- `add-demo-foundation` view work later reuses the prototype's markup and CSS; blog and
  newsletter pieces are dropped at that point.
- Build tooling (if any) is a technology decision for design, recorded as an ADR.
- Two font dev dependencies (`@fontsource/baloo-2`, `@fontsource/nunito`) confined to
  `prototype/` (design D6, ADR-0019); fonts arrive via `npm install`, no binaries committed.
