## Context

See proposal.md - Why. The prototype precedes the Umbraco view work in `add-demo-foundation`;
its markup and CSS must transplant cleanly into Razor templates later. Constraints carried over
from spec `agency-site` "Minimal page dependencies": no third-party front-end frameworks,
external fonts or tracking scripts at runtime, pages work without client-side scripting. Code
must stay portable across Windows, macOS and Linux; Node.js is the preferred scripting runtime.

## Goals / Non-Goals

**Goals:**
- Shared layout sources so nav, banner, breadcrumb and footer stay consistent across ~10 pages
  without hand-copying. In practice (as built): `base.njk` is the base and the text/Home layout;
  `listing.njk` extends it for the three listings; detail-with-form pages build on the text
  layout. No separate `home.njk`/`text.njk` files (supervisor review 2026-10-14).
- Output is plain HTML + CSS files (plus static images) a browser can open from any static
  server, and a developer can copy section-by-section into Razor views.
- Fast PO review loop: one command to build and serve locally.

**Non-Goals:**
- No reuse of the build tooling in the Umbraco site; only the emitted markup and CSS carry over.
- No JavaScript behaviour. Nothing in the prototype requires client-side scripting; if a small
  enhancement is wanted later (for example a mobile nav toggle), it is additive and the page
  must work without it.
- No automated tests (see bar).

## Decisions

### D1: Eleventy as the static site generator (ADR-0018)
Use Eleventy (11ty) with Nunjucks templates to render the pages from shared layouts, emitting
plain HTML. Alternatives: hand-written HTML per page (no tooling, but nav/footer duplicated ten
times and guaranteed to drift during PO iteration); Vite + templating plugin (build tool first,
templating bolted on); Astro (component model and client tooling the prototype cannot use).
Eleventy is Node-based, zero-config for this size, and its output is exactly the deliverable:
static files. See ADR-0018.

### D2: Location and layout of the prototype
Lives in `prototype/` at the repo root, outside the solution: `prototype/src/` holds templates,
CSS and images; the build emits to `prototype/_site/` which is gitignored (the source templates
are the artefact of record; anyone can rebuild). `package.json` in `prototype/` with `npm run
build` and `npm run serve` (Eleventy's dev server). Alternatives: under
`src/SuperPower.Web/wwwroot/` (couples a throwaway build to the shipping site) or `docs/`
(it is not documentation). Covered by ADR-0018.

### D3: Hand-written CSS, one stylesheet, system fonts (fonts amended by D6)
A single `site.css` written by hand: CSS custom properties for colours and spacing, system font
stack, mobile-first, no preprocessor and no utility framework. The spec forbids third-party
frameworks and external fonts; a preprocessor would add tooling for one file. The stylesheet is
copied verbatim into the Umbraco site later.

### D4: Local SVG placeholder images
Listing and section images are simple local SVG placeholders (coloured block, label text)
checked in under `prototype/src/img/`. No external image services (would violate "no
third-party requests") and no binary stock photos to licence. Umbraco later ships the same
files as static assets (spec `agency-site` "No media library dependency").

### D5: Forms point at the thank-you page
Each form is `<form method="get" action="/thank-you/">` with plain inputs and labels; the
browser navigates to the static thank-you page on submit and no server endpoint receives the
field values. Field names match spec `form-submission` so the markup drops into the Umbraco
forms with minimal change. GET to a static page keeps "no data sent to any server endpoint"
honest without JavaScript.

### D6: Rubber Duck Consulting theme (prototype-only; PO 2026-10-14)
Brand: Rubber Duck Consulting, tagline "We listen.", playful listening/debugging-themed copy.
Prototype-only: spec `agency-site` and the Umbraco demo scope stay Acme Agency; this change's
spec delta records the prototype brand. Typography: Baloo 2 (headings) + Nunito (body),
self-hosted via `@fontsource` packages (ADR-0019), system stack as fallback - amends D3's
system-font point; the single hand-written stylesheet stands. Palette: CSS custom properties
move to duck-brand tokens - duck yellow `#F5B700`, bill orange `#FF8C42`, deep teal `#1B3A4B`
(text/dark surfaces), warm paper `#FFF9EC` background; workers may tune shades to keep WCAG AA
contrast. Imagery: the D4 placeholder SVGs are replaced by custom flat duck-themed SVG
illustrations drawn in the palette, still local static assets - no stock photos, no licensing
exposure (alternatives: CC0 photos - mixed look, per-file licence checks; rejected by PO).

## Bar and test strategy

Bar: prototype. The markup and CSS are reused later, so they are written clean (semantic HTML,
accessible labels, breadcrumbs as nav); the Eleventy project itself is throwaway. No automated
tests: validation is `npm run build` succeeding plus the PO browsing the served site against the
spec's scenarios.

## Risks / Trade-offs

- [Prototype and Umbraco views drift once templates are hand-copied] → accepted: the prototype
  is the starting point, not a synchronised source; after integration the Razor views are the
  truth.
- [Blog and newsletter are prototype-only, risking accidental scope creep into Umbraco] →
  proposal and spec mark them PO 2026-10-14 prototype-only; `agency-site` spec unchanged.
- [Eleventy adds a Node toolchain to a .NET repo] → confined to `prototype/`; nothing in the
  solution references it, and it can be deleted wholesale after integration.
