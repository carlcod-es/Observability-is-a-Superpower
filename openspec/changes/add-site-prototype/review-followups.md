# Review follow-ups (minors and nits)

Open items from block/section reviews. Read at the start of every apply session.

## Block A (section 1, reviewed 2026-10-14, APPROVED)
- [ ] nit `prototype/src/_includes/base.njk:51-55`: footer link list is a bare `<ul>`; wrap in
  `<nav aria-label="Footer">` for a labelled landmark.
- [ ] nit `prototype/src/_includes/base.njk:8-9`: no skip-to-content link; optional for a
  prototype.
- [ ] nit `prototype/src/_includes/base.njk:7`: root-absolute asset/nav URLs mean `file://`
  browsing loses CSS; document "use `npm run serve`" in a prototype README line.
- [ ] nit `prototype/package.json`: `npm audit` high advisory in transitive `braces`;
  build-time-only throwaway tooling, no action needed - recorded for awareness.

## Supervisor, section 1 (reviewed 2026-10-14, CHANGES REQUESTED)
Major (breadcrumb contract: Home cannot render its required breadcrumb without "Home › Home")
assigned to a fix block together with these same-file minors/nits:
- [x] minor `prototype/src/css/site.css:41`: global `ul, ol` reset strips bullets from body/rich
  text (and would carry into Umbraco); scope the reset to component list classes instead.
  (Fixed 2026-10-14, confirmed by Block B reviewer: reset scoped to the four component lists.)
- [x] minor `prototype/src/_includes/base.njk:16`: ancestor nav match gets `aria-current="page"`;
  use `aria-current="true"` for ancestors, `"page"` only for exact match; extend the site.css
  selector to `[aria-current]`. (Fixed 2026-10-14, confirmed by Block B reviewer.)
- [x] nit `prototype/package.json`: add `"engines": { "node": ">=18" }`. (Fixed 2026-10-14.)
The breadcrumb major itself is fixed (Home renders a single current item); supervisor re-review
APPROVED 2026-10-14.

## Block B + section 1 fixes (section 2, reviewed 2026-10-14, APPROVED)
- [x] minor `prototype/src/_data/listings.json`: superseded by the merged placeholder-URL item
  under "Block C" below (section 3 supervisor, 2026-10-14).
- [ ] minor `prototype/src/_includes/macros.njk:5`: cards use `<h3>`, skipping h2 on listing
  pages (banner h1 -> card h3); make the heading level a macro parameter (h2 on listings, h3
  under Home's section h2).
- [ ] nit `prototype/src/_includes/macros.njk:3,5`: card image alt repeats the adjacent title;
  `alt=""` (decorative) would be the more accessible choice.

## Supervisor, sections 1-2 re-review (2026-10-14, APPROVED)
- [x] minor design.md Goals vs code: no separate home/text layout files; design.md amended to
  match the as-built layout structure (Architect, 2026-10-14).
- [ ] nit `prototype/src/_includes/macros.njk:5,10`: each card has two links to the same URL
  (title + "Read more"); keyboard users tab it twice. Drop "Read more" or link only the title.
- [ ] nit `prototype/src/_includes/macros.njk:3`: `loading="lazy"` also hits above-the-fold
  cards; harmless here, consider removing in the Razor partial.
## Block C (section 3, reviewed 2026-10-14, APPROVED)
- [ ] minor `prototype/src/_data/listings.json:4-5,9-10,14-15`: non-first cards in every listing
  (services, events, blog) link to the single sample detail page whose title differs from the
  card. Decision: accepted placeholders (one sample detail page per type by design); PO sees it
  in task 4.1. Merged from the Block B item above.
- Visual checks deferred to PO task 4.1: rendering/responsive at 48em, form styling (checkbox
  rows, focus, button contrast), query-string values visible in the thank-you URL after submit,
  SVG imagery.

## Supervisor, section 3 (2026-10-14, CHANGES REQUESTED -> fix block)
Major (forms lacked `novalidate`, so the browser blocked empty/invalid submissions, contradicting
spec `form-submission` "Server-side validation only") assigned to a fix block together with:
- [x] minor: required fields not visibly marked (spec `form-submission` "Rendering"); fix block
  added `(required)` label markers + one CSS rule (Architect decision 2026-10-14; fix re-review
  APPROVED 2026-10-14, which also closed the novalidate major).
- [ ] minor `prototype/src/css/site.css` Forms block: no field-error/form-error/"This event has
  ended" notice styles. Known gap carried to `add-demo-foundation` view work (Architect decision
  2026-10-14; spec `site-prototype` does not require these states).
- Accepted nits: event/service detail pages repeat date and banner text from `listings.json`
  (throwaway duplication, PO iteration risk accepted); alt-text mismatch covered by the existing
  decorative-alt nit.

## Section 5 (theme + content, reviewed 2026-10-14, APPROVED)
- [x] nit `prototype/package.json:2,5`: package name/description still said Acme; renamed
  `rubber-duck-prototype` (fix worker 2026-10-14).
- [x] nit `prototype/src/css/site.css` `.site-header__brand`: brand mark sat on the text
  baseline; inline-flex alignment added (fix worker 2026-10-14).
- Note: `--color-bill` token defined but unused (kept for the brand palette); duck yellow
  appears only as the header accent - PO may want it louder (task 6.1).

## Supervisor, section 5 (2026-10-14, APPROVED)
- [ ] nit `prototype/src/css/site.css:175,182,213,240,241`: `font-weight: 600` renders as the
  700 face (only 400/700 loaded); change to 700 or self-host Nunito 600 (~16 KB).
- Carried to `add-demo-foundation` view integration: re-brand back to Acme Agency (name, copy,
  imagery; CSS/fonts/markup structure carry over), forms switch to POST + antiforgery (GET query
  strings would leak personal data into URL telemetry), form error / past-event notice styles
  still to design.

- Notes for PO browse (task 4.1): Home breadcrumb is a single unlinked "Home" (only form that
  satisfies the spec without "Home › Home"); page `<h1>` lives in the banner; every listing card
  links to the same sample detail page (by design, visible when browsing).

Architect decisions (2026-10-14), contracts for sections 2-3:
- Banner emits the page's only `<h1>` (text = `title`, overridable by `banner`; `bannerText` is
  a tagline). Page templates never emit their own `<h1>`.
- Boxed design accepted: `<main>` stays width-constrained; no full-width background bands.
- Detail pages nest under their listing (since the section 5 rebrand:
  `/services/rubber-duck-debugging/`, `/events/quack-and-learn/`,
  `/blog/why-explaining-your-bug-fixes-it/`).
- Note for PO at commit time: include `prototype/package-lock.json` for reproducible builds.
