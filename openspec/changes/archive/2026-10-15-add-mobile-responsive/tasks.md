## 1. Prototype: shared stylesheet, script and markup (`prototype/`)

Owner: worker-frontend. Depends on: none. 1.1 fixes the class vocabulary and asset paths
(design D2) that sections 2 and 3 rely on. Spec `responsive-layout`; ADR-0020; design D2-D4.

- [x] 1.1 Merge the site's `src/SuperPower.Web/wwwroot/css/site.css` rules into `prototype/src/css/site.css` per design D2 (site class names, site-only rules added, `--color-accent` uses moved to `--color-duck`, font URLs `/css/fonts/`, newsletter rules kept and commented "prototype only"); change `prototype/eleventy.config.js` to copy the fonts to `css/fonts/`; rename the prototype form and thank-you markup to the site's classes (forms on Contact us, the sample service and the sample event; thank-you uses `form-success`); verify `npm run build` succeeds, `_site/css/fonts/` holds the three WOFF2 files, and `grep -r "form__\|thank-you\"\|color-accent\|url(\"/fonts" prototype/src` finds nothing
- [x] 1.2 Add fit-to-width rules per design D4 (global media max-width, `overflow-wrap` in main, cards 1/2/3 columns at <36em/36em/56em, 44px form controls, full-width submit below 36em); verify `npm run build` succeeds and, in `npm run serve`, Home, Services, Blog post, sample event and Contact us show no horizontal scroll at 320 px in dev tools responsive mode. Status 2026-10-14: code met (block 1A review APPROVED); browser check pending PO task 4.2. Prototype removed 2026-10-14 (PO, ADR-0021): prototype build and checks dropped; the rules live in `src/SuperPower.Web/wwwroot/`; behaviour checked on the site in task 4.2, which closes this task. Closed by task 4.2 (PO 2026-10-15)
- [x] 1.3 Add `prototype/src/js/site.js` and the nav toggle per design D3 (button with `hidden` and `aria-controls`, `id="site-nav"`, deferred script in `base.njk`, passthrough copy of `src/js`, CSS keyed on `aria-expanded` below 48em); verify at 375 px the Menu button opens and closes the nav by click and by keyboard with `aria-expanded` changing, at 768 px no button shows, and with JavaScript disabled no button shows and the nav is visible. Status 2026-10-14: code met (block 1A review APPROVED); behaviour checks pending PO task 4.2. Prototype removed 2026-10-14 (PO, ADR-0021): prototype build and checks dropped; the rules live in `src/SuperPower.Web/wwwroot/`; behaviour checked on the site in task 4.2, which closes this task. Closed by task 4.2 (PO 2026-10-15)
- [x] 1.4 Add `prototype/scripts/sync-to-site.mjs` and `npm run sync` copying `src/css/site.css` and `src/js/site.js` to `../src/SuperPower.Web/wwwroot/css/` and `../js/` (create the folder; `path.join`, `fs.copyFileSync`, no new dependencies; ADR-0020); verify `npm run sync` then `git diff --no-index prototype/src/css/site.css src/SuperPower.Web/wwwroot/css/site.css` and the same for `site.js` report no differences

## 2. Demo site layout (`src/SuperPower.Web/Views/`, `src/SuperPower.Web/wwwroot/`)

Owner: worker-frontend. Depends on: section 1 (synced files, class names, toggle markup).

- [x] 2.1 Add the nav toggle button, `id="site-nav"` and `<script src="/js/site.js" defer></script>` to `Views/master.cshtml` exactly as in `prototype/src/_includes/base.njk` (design D3; prototype removed, ADR-0021; button now an icon with `aria-label="Menu"`, PO commit 0c594da); commit the synced `wwwroot/css/site.css` and `wwwroot/js/site.js` from task 1.4 unchanged; verify `dotnet build` succeeds and, with the AppHost running, Home, Services, a service page, an event page and Contact us render styled at 320 and 1280 px, the menu opens and closes at 375 px, and `/js/site.js` returns 200. Status 2026-10-14: markup and build met (block 2A review APPROVED); runtime checks pending PO task 4.2. Closed by task 4.2 (PO 2026-10-15)

## 3. Drift check (`tests/SuperPower.Web.Tests`)

Owner: worker-backend. Depends on: 1.4 (files exist in both places). Superseded 2026-10-14 by
ADR-0021 (prototype removed): 3.2 removes the test.

- [x] 3.1 Add `SharedAssetsTests` comparing `prototype/src/css/site.css` with `src/SuperPower.Web/wwwroot/css/site.css` and `prototype/src/js/site.js` with `src/SuperPower.Web/wwwroot/js/site.js` after CRLF -> LF normalisation, repo root found by walking up from `AppContext.BaseDirectory` to `SuperPower.slnx`, failure message naming the differing file (ADR-0020); verify root `dotnet test` passes, then appending a comment to the `wwwroot` copy of `site.css` makes the test fail naming that file, then reverting makes it pass
- [x] 3.2 (added 2026-10-14, ADR-0021) Delete `tests/SuperPower.Web.Tests/SharedAssetsTests.cs` and the `prototype/` entries in `.gitignore`; verify `dotnet test --solution SuperPower.slnx` passes and `git grep -i -e prototype -e SharedAssets -- src tests .gitignore` finds only the `site.css` newsletter comment (review-followups)

## 4. Verification and docs

Owner: Architect, with PO checks marked (PO). Depends on: sections 1-3.

- [x] 4.1 (Architect) Update `CLAUDE.md` "Apply-phase team" ownership (`prototype/` to worker-frontend; front-end CSS/JS edited in `prototype/` then `npm run sync`) and `docs/project-setup.md` (`wwwroot/css/` and `wwwroot/js/` are synced copies, ADR-0020; `prototype/` description); verify both files name ADR-0020 and `npm run sync`
- [x] 4.2 (PO) Check spec `responsive-layout` scenarios by hand in browser dev tools responsive mode on the site (prototype removed, ADR-0021): each page type at 320, 375, 768, 1280 px; menu by mouse and keyboard; JavaScript disabled; a site form submitted empty at 375 px; Aspire dashboard shows no trace for `/js/site.js` or `/css/fonts/*`; verify results recorded in `openspec/changes/add-mobile-responsive/verification.md` with any failures fed back as fix blocks. Done 2026-10-15 (PO): checked on the site on mobile and desktop, "works fine"; includes the 2026-10-15 header changes (menu button pinned top right, nav panel below it); verification.md
