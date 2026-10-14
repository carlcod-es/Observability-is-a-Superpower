# ADR-0020: Prototype is the source of the site's stylesheet and script

## Status
Accepted (2026-10-14, PO)

## Context
Change `add-mobile-responsive` makes the demo site and the static prototype responsive and adds
a small navigation script. The prototype's `site.css` (ADR-0018) was hand-copied into
`src/SuperPower.Web/wwwroot/css/site.css` in `add-demo-foundation` task 7.1 and has drifted:
class names (`form__field` vs `form-field`), token names (`--color-duck` vs `--color-accent`),
font paths (`/fonts/` vs `/css/fonts/`), prototype-only and site-only rules. The PO wants one
stylesheet and one script usable unchanged by both. The site has no npm build (ADR-0019
outcome); attendees run it with the .NET SDK only.

## Decision
- Authored once in the prototype: `prototype/src/css/site.css` and `prototype/src/js/site.js`.
- Copied verbatim to `src/SuperPower.Web/wwwroot/css/site.css` and
  `src/SuperPower.Web/wwwroot/js/site.js` by `npm run sync` in `prototype/` (a Node script
  using `fs.copyFileSync`, cross-platform). The copies are committed, so the site still needs
  no Node.
- An xUnit test in `tests/SuperPower.Web.Tests` compares each pair (CRLF normalised to LF) and
  fails naming the differing file. Locates the repo root by walking up to `SuperPower.slnx`.
- Both serve the same URLs: `/css/site.css`, `/css/fonts/*.woff2`, `/js/site.js`. The prototype
  passthrough-copies its npm fonts to `css/fonts/` (amends ADR-0019); `/css` and `/js` are
  already excluded from tracing (ADR-0008).
- The shared class vocabulary is the site's (Razor tag helpers and `Partials/Media.cshtml`
  fix it): the prototype's markup is renamed to match. Rules used by only one side
  (newsletter, blog; validation errors, success, event-ended, empty-image blocks) stay in the
  one file.

## Alternatives
- Site `wwwroot` as source, Eleventy passthrough-copies it into the prototype: no copy to
  drift, but authoring moves out of the prototype's dev server loop; not chosen by the PO.
- Two files kept identical by hand, no check: no tooling, drifts again (it already has);
  rejected.
- MSBuild target copying from `prototype/` on build: hides the copy, couples `dotnet build` to
  the prototype directory and re-dirties committed files on every build; rejected.

## Consequences
- `prototype/` is no longer deletable wholesale after integration (ADR-0018 consequence
  amended): it is the stylesheet and script source while it exists. Deleting it later means
  deleting the drift test and making `wwwroot` the source.
- Editing `wwwroot/css/site.css` or `wwwroot/js/site.js` directly fails the test; every
  front-end change is made in `prototype/` then synced.
- Root `dotnet test` reads files under `prototype/src/`; the test project needs no Node.
