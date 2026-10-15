# Review follow-ups: add-mobile-responsive

Minor and nit findings from block and section reviews. Read at the start of every apply session.

## Open

- [ ] nit (block 1A, task 1.2) `src/SuperPower.Web/wwwroot/css/site.css` banner: `overflow-wrap: anywhere` is only on `.main`; a long unbroken word in the banner heading could overflow at 320 px. Check in PO task 4.2; if it shows, add `overflow-wrap: anywhere` to `.banner, .breadcrumb` (section supervisor: the breadcrumb last item, `master.cshtml:65`, has the same gap).
- [x] nit (block 3A, task 3.1) `tests/SuperPower.Web.Tests/SharedAssetsTests.cs:20`: failure shows no diff. Obsolete: test removed (task 3.2, ADR-0021).
- [ ] minor (section 1-3 supervisor) `src/SuperPower.Web/wwwroot/css/site.css` header rules (was prototype `site.css:91-106,120`): at 320-375 px brand + Menu button may not fit one row, so the button wraps left-aligned under the brand (no scroll, looks off). Check in PO task 4.2; fix: `margin-left: auto` on `.site-nav__toggle`, or smaller `.site-header__brand` below 48em.
- [x] nit (section 1-3 supervisor) newsletter input/button min-height. Obsolete: prototype removed (ADR-0021).
- [ ] nit (section 1-3 supervisor) `src/SuperPower.Web/wwwroot/js/site.js:3`: top-level `const toggle` is a page global; optional: wrap in a block.
- [ ] note (section 1-3 supervisor) Below 48em with JS the nav renders open, then collapses when the deferred script runs (brief layout shift); accepted with design D3.
- [x] note (section 1-3 supervisor) untracked files to commit. Done: merged via PR #3; prototype since removed.
- [ ] minor (2026-10-14, ADR-0021) `src/SuperPower.Web/wwwroot/css/site.css:227`: newsletter rules commented "prototype only" are now dead CSS (no newsletter in the site); remove them (worker-frontend).
