# ADR-0021: Site owns its stylesheet and script; prototype removed

## Status
Accepted (2026-10-14, PO); supersedes ADR-0020

## Context
ADR-0020 made `prototype/` the source of `site.css` and `site.js`, copied to the site by
`npm run sync` and guarded by a drift test. The prototype's job (seed the Umbraco views and
the shared stylesheet) is done. The PO deleted `prototype/` (commit 7b27ce8) and now edits
`wwwroot` directly (commit 0c594da).

## Decision
- `src/SuperPower.Web/wwwroot/css/site.css` and `src/SuperPower.Web/wwwroot/js/site.js` are
  the only source; edited in place.
- Drift test `SharedAssetsTests` and the `npm run sync` step are removed. No Node toolchain
  remains in the repo.
- Unchanged from ADR-0020: URLs `/css/site.css`, `/css/fonts/*.woff2`, `/js/site.js`; `/css`
  and `/js` excluded from tracing (ADR-0008); no external resources.

## Alternatives
- Keep the prototype as source (ADR-0020): a second copy of every page to maintain for a
  demo that only shows the site; rejected by the PO.

## Consequences
- One stylesheet, one script, no copy step and no drift check.
- `add-mobile-responsive` checks run on the site only.
- ADR-0018 and ADR-0019 describe the removed prototype; their site outcomes (fonts under
  `wwwroot/css/fonts/`, served from `/css/fonts/`) stand.
