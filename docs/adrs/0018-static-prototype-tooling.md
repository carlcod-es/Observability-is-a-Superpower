# ADR-0018: Static site prototype built with Eleventy

## Status
Accepted (2026-10-14); typography point superseded by ADR-0019 (2026-10-14); "deletable wholesale" consequence amended by ADR-0020 (2026-10-14)

## Context
Change `add-site-prototype` delivers a standalone static prototype of the Acme Agency site (four
layouts, ~10 pages) whose markup and CSS later seed the Umbraco templates. Spec `site-prototype`
forbids third-party front-end frameworks, external fonts and tracking scripts at runtime, and
pages must work without client-side scripting. User preference: Node.js for tooling; the repo is
otherwise .NET.

## Decision
- Eleventy (11ty, latest stable 3.x) with Nunjucks templates renders shared layouts to plain
  HTML. Build tooling only; nothing from it ships at runtime.
- Lives in `prototype/` at the repo root, outside the solution. `prototype/src/` holds
  templates, `site.css` and SVG placeholder images; `prototype/_site/` is the build output and
  is gitignored.
- `package.json` scripts: `npm run build` (one-off build), `npm run serve` (Eleventy dev server
  with reload for PO review).
- Hand-written single stylesheet, CSS custom properties, system font stack; no preprocessor.
  (Fonts since amended: self-hosted Baloo 2 + Nunito per ADR-0019; the rest stands.)

## Alternatives
- Hand-written HTML per page, no tooling: simplest, but nav, banner and footer are duplicated
  across ten pages and drift during iteration; rejected.
- Vite + a templating plugin: bundler-first, templating is an add-on, and the prototype has
  nothing to bundle; rejected.
- Astro: component model and client-side tooling the prototype must not use; heavier than the
  job; rejected.

## Consequences
- One Node toolchain appears in the repo, confined to `prototype/`; deletable wholesale once the
  markup is in Umbraco. (Amended by ADR-0020: `prototype/` is now the source of the site's
  stylesheet and script.)
- The emitted HTML and `site.css` are the deliverable; integration copies markup into Razor
  views section by section.
- Anyone reviewing the prototype needs Node.js installed; attendees do not (the prototype is a
  design artefact, not part of the demo environment).
