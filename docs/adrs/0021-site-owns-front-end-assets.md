# ADR-0021: Site owns its stylesheet and script

## Status
Accepted (2026-10-14).

## Context
The site's views and stylesheet were first built as a separate static prototype. That job is
done and the prototype is removed; the site's front-end files need one source.

## Decision
- `src/SuperPower.Web/wwwroot/css/site.css` and `src/SuperPower.Web/wwwroot/js/site.js` are
  the only source; edited in place. No Node toolchain or copy step for them.
- URLs `/css/site.css`, `/css/fonts/*.woff2` (ADR-0019), `/js/site.js`; `/css` and `/js`
  excluded from tracing (ADR-0008); no external resources.

## Alternatives
- Keep the prototype as source with a copy step and drift test: a second copy of every page to
  maintain for a demo that only shows the site; rejected.

## Consequences
- One stylesheet, one script, no copy step and no drift check.
