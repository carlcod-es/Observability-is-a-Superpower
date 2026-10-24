# ADR-0019: Self-hosted open-licence webfonts

## Status
Accepted (2026-10-14).

## Context
The site's brand uses Baloo 2 (headings) and Nunito (body), both SIL Open Font License.
Page-request telemetry should contain only the site's own work, so fonts must be served from
the site's own files, never a CDN. The repo is published for attendees, so licences must
permit redistribution.

## Decision
- The latin-subset WOFF2 files of Baloo 2 and Nunito and their OFL licences are committed under
  `src/SuperPower.Web/wwwroot/css/fonts/` (the site has no npm build).
- `site.css` declares the `@font-face` rules by hand with `font-display: swap` and a system font
  stack as fallback.
- Served from `/css/fonts/`: `/css` is already excluded from telemetry (ADR-0008), so font
  requests add no traces.

## Alternatives
- System font stack: no payload, but no brand typography; rejected.
- Google Fonts CDN: an external resource in every page load and visitor requests leaked to a
  third party; rejected.
- Fonts from npm packages (`@fontsource/*`): needs a Node build the site does not have; rejected.

## Consequences
- Page weight grows by the WOFF2 files (~100-200 KB total, latin subsets only).
- Binary font files and their licence copies live in the repo.
