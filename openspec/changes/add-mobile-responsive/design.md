## Context

See proposal.md - Why. Current state (branch `sample-site`):
- `prototype/src/css/site.css` is already mobile-first: header stacks below 48em, nav wraps,
  cards one column below 48em, three above. No script. Fonts served from `/fonts/`.
- `src/SuperPower.Web/wwwroot/css/site.css` is a hand-edited copy: `--color-accent` instead of
  `--color-duck`/`--color-bill`, fonts from `/css/fonts/`, site-only rules (`.form-field`,
  `.form-check`, `.field-error`, `.form-errors`, `.form-success`, `.required-marker`,
  `.event-ended`, `.card__image--empty`, `.banner__image`, `.content-image[--empty]`), no
  newsletter rules. No `wwwroot/js/`.
- Site markup class names are fixed by Razor tag helpers and `Views/Partials/Media.cshtml`
  (`<class>--empty` blocks). Prototype forms use BEM names (`form__field`, `form__submit`,
  `form__required`, `thank-you`).
- `/css` and `/js` are excluded from tracing (`TelemetryPaths`, ADR-0008).
- Specs `agency-site` and `site-prototype` "Minimal page dependencies": pages work without
  client-side scripting. Satisfied here by progressive enhancement (D3).

## Goals / Non-Goals

**Goals:**
- One `site.css` and one `site.js`, identical in prototype and site (ADR-0020).
- Requirements in spec `responsive-layout`.

**Non-Goals:**
- No restyling beyond what fit-to-width needs; colours, type and spacing tokens stay.
- No JS tests or browser automation; manual check at fixed widths (see Bar).
- Prototype content stays as is (including its Contact "Reply by phone?" checkbox, which the
  site dropped; PO may remove it separately).

## Decisions

### D1: Prototype is the source, copied by `npm run sync`, guarded by a test (ADR-0020)
PO choice. `prototype/scripts/sync-to-site.mjs` copies `src/css/site.css` and `src/js/site.js`
to `../src/SuperPower.Web/wwwroot/{css,js}/` with `path.join` and `fs.copyFileSync`.
`tests/SuperPower.Web.Tests/SharedAssetsTests.cs` compares both pairs after CRLF -> LF.
Alternatives (wwwroot as source, manual, MSBuild copy): see ADR-0020.

### D2: Site class vocabulary wins; one file holds both sides' rules
The site's names cannot change without fighting tag helpers; the prototype's can. Rename in the
prototype: `form__field` -> `form-field`, `form__field--check` -> `form-field form-check`,
`form__required` -> `required-marker` (plus visually-hidden "(required)" as in the site),
`form__submit` -> plain `button[type="submit"]`, `.form` class dropped (rule targets `form`),
`thank-you` -> `form-success`. Tokens: keep `--color-duck` and `--color-bill` (prototype brand
names), replace `--color-accent` uses with `--color-duck`. Fonts: `/css/fonts/`. Merged file =
prototype rules + site-only rules, newsletter kept and commented "prototype only". Alternative:
two class vocabularies in one file (aliases) - doubles selectors for no gain; rejected.

### D3: Collapsible nav as progressive enhancement
Markup (both layouts), inside `.site-header__inner` before `<nav>`:
`<button class="site-nav__toggle" type="button" aria-controls="site-nav" hidden>Menu</button>`;
`<nav id="site-nav" ...>`. Script loaded `<script src="/js/site.js" defer></script>` in
`<head>`.

`site.js` (~15 lines, no dependencies): find the toggle; if absent, stop. Remove `hidden`, set
`aria-expanded="false"`, toggle `aria-expanded` on click. No resize handling.

CSS keys only on the button state, so no-JS needs no extra rule:
- Below 48em: `.site-nav__toggle[aria-expanded="false"] + .site-nav { display: none; }`;
  header inner is a wrapping row (brand left, button right, nav full width below); nav links
  stacked, min-height 44px.
- 48em and up: `.site-nav__toggle { display: none; }` and the nav shows regardless of state
  (selector scoped to the narrow media query).
- No JS: button keeps `hidden`, no `aria-expanded`, nav always shown (current stacked layout).

A native `<button>` gives Enter/Space and focus for free. Alternatives: CSS-only checkbox hack
(not an accessible button, no expanded state); `<details>/<summary>` (no JS, but renders a
disclosure widget at desktop widths and styling the marker per breakpoint is awkward);
rejected. PO chose a JS toggle.

### D4: Fit-to-width rules
- Global: `img, svg, video { max-width: 100%; height: auto; }`; `.main { overflow-wrap:
  anywhere; }`.
- Cards: one column, two from 36em, three from 56em (three at 48em is cramped on 768px
  tablets).
- Forms: inputs, textarea and submit button `min-height: 2.75rem` (44px), submit full width
  below 36em; input font size stays `1rem` (16px; iOS zooms below that).
- Banner, breadcrumb, footer: already wrap; tune padding only if the width check shows a
  problem.
Breakpoints in `em` as today; 48em stays the nav breakpoint (768px = tablet in the spec).

### D5: Ownership
`prototype/` is not in CLAUDE.md "Apply-phase team" ownership; it becomes worker-frontend's
(added to CLAUDE.md with this change). The drift test is worker-backend's (`tests/`).

## Bar and test strategy

Bar: prototype (demo site, live on stage and on attendees' phones). Automated: `npm run build`
in `prototype/`, `dotnet build`, `dotnet test` (includes the drift test). Manual, by the PO in
browser dev tools responsive mode (no browser automation): each page type in prototype and
site at 320, 375, 768, 1280 px; menu open/close by mouse and keyboard; scripting disabled; a
form with validation errors at 375 px; Aspire dashboard shows no trace for `/js/site.js`.
Results recorded in `verification.md`.

## Risks / Trade-offs

- [Someone edits `wwwroot` CSS directly, e.g. a worker fixing a site-only bug] → drift test
  fails; CLAUDE.md and briefs state "edit in `prototype/`, run `npm run sync`".
- [Git `autocrlf` on Windows gives CRLF copies] → test normalises line endings.
- [Script fails to load] → nav stays visible (button hidden, no `aria-expanded`).
- [Renaming prototype classes breaks a prototype page] → `npm run build` plus the manual width
  check covers every page type.
- [Live demo branches `demo/act-N-*` cut before this lands lack the change] → merge order is
  the PO's call; this change touches no telemetry or form behaviour.

## Migration Plan

Land on `mobile-responsive` (from `sample-site`); PO merges. Rollback: revert the branch; the
site's old stylesheet is restored with it.
