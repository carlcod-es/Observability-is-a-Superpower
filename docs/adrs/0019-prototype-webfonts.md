# ADR-0019: Self-hosted open-licence webfonts in the prototype

## Status
Accepted (2026-10-14)

## Context
The PO rebranded the prototype to Rubber Duck Consulting (prototype-only; the Umbraco demo site
remains Acme Agency) and chose Baloo 2 (headings) + Nunito (body), both SIL Open Font License.
ADR-0018 specified a system font stack; the PO overrides that for the prototype. Spec
`site-prototype` "Minimal page dependencies" exists so page-request telemetry contains only the
site's own work: fonts must therefore be served from the site's own files, never a CDN. The repo
is published for attendees, so licences must permit redistribution.

## Decision
- Baloo 2 and Nunito as `@fontsource/baloo-2` and `@fontsource/nunito` dev dependencies in
  `prototype/package.json` (latin subset WOFF2, OFL licence files included in the packages).
- Eleventy passthrough copies the needed WOFF2 files to `_site/fonts/`; `site.css` declares the
  `@font-face` rules by hand (one stylesheet stays the deliverable, design D3) with
  `font-display: swap` and the previous system stack as fallback.
- No binary font files committed to the repo; they arrive via `npm install` like Eleventy itself.
- Umbraco integration later copies the built `fonts/` output and the `@font-face` CSS as static
  site assets.

## Alternatives
- Keep the system font stack (ADR-0018): no payload, but the PO wants the brand typography;
  overridden by the PO.
- Google Fonts CDN: violates "no externally hosted fonts" and leaks visitor requests to a third
  party; rejected.
- Commit downloaded WOFF2 binaries: works, but adds binaries to the repo and hand-managed
  licence copies; npm packages version and licence them cleanly; rejected.

## Consequences
- ADR-0018's "system font stack" point is superseded for typography; the single-stylesheet and
  no-preprocessor decisions stand.
- Page weight grows by the WOFF2 files (~100-200 KB total, latin subsets only).
- Attendees need nothing new: `npm install` already required for Eleventy.
