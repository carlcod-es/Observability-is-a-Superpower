## 1. Prototype scaffold (worker-frontend; no dependencies)

- [x] 1.1 Create `prototype/` with `package.json` (Eleventy 3.x dev dependency, `build` and
  `serve` scripts) and Eleventy config reading `prototype/src/` and emitting `prototype/_site/`
  (ADR-0018); verify `npm run build` succeeds on a placeholder index page.
- [x] 1.2 Add `prototype/_site/` and `prototype/node_modules/` to `.gitignore` (shared file,
  assigned here); verify `git status` shows neither after a build.
- [x] 1.3 Create the base layout: HTML skeleton, site header with navigation (Home, About us,
  Services, Events, Blog, Contact us), banner block, breadcrumb block, footer; verify the built
  index page shows all of them and every nav link targets a defined page path.
- [x] 1.4 Create `site.css`: custom properties for colours and spacing, system font stack,
  mobile-first base styles for header, nav, banner, breadcrumb, footer (design D3); verify the
  built index renders styled with no external requests (view source: no third-party URLs).

## 2. Layouts, pages and content (worker-frontend; depends on section 1)

- [x] 2.1 Text page layout (banner, breadcrumb, title, body, footer) and the About us page with
  believable Acme Agency copy; verify the built page matches spec `site-prototype` "Text page
  layout".
- [x] 2.2 Listing page layout (banner, breadcrumb, child items with title, image, summary,
  link) and local SVG placeholder images (design D4); verify a built listing shows items with
  all four elements and images load from `prototype` files only.
- [x] 2.3 Services page with at least three services (Umbraco upgrade, SEO audit, AdWords
  audit) linking to the sample service page; verify spec scenario "Services listing".
- [x] 2.4 Events page with at least three events, each with start date and time after
  16 Oct 2026, linking to the sample event page; verify spec scenario "Events listing".
- [x] 2.5 Blog page with at least three article teasers linking to the sample blog post; blog
  post page on the text layout; verify both build and the listing links resolve.
- [x] 2.6 Home page: main banner, breadcrumb, Services, Events and Latest articles sections
  (items linking to their pages), newsletter signup box with no action, footer; verify spec
  scenario "Home shows its sections" and that the newsletter box submits nowhere.

## 3. Forms and thank-you page (worker-frontend; depends on section 2)

- [x] 3.1 Thank-you page showing "Thank you, we'll be in touch."; verify it builds and is
  reachable at the path the forms use.
- [x] 3.2 Contact us page: banner, breadcrumb, Contact form (Name, Email, Message, "Reply by
  phone?" checkbox) with labelled inputs, action pointing at the thank-you page (design D5);
  verify submitting in a browser lands on the thank-you page with no server endpoint receiving
  values.
- [x] 3.3 Sample service page: title, description, Service enquiry form (Name, Email, Company
  optional, Website optional, Message); verify spec scenario "Service detail" and submit leads
  to thank-you.
- [x] 3.4 Sample event page: title, description, start date and time, Event booking form (Name,
  Email, Notes optional, "Join mailing list" checkbox); verify spec scenario "Event detail" and
  submit leads to thank-you.
- [x] 3.5 Full pass: `npm run build` clean, then browse every page checking navigation,
  breadcrumbs, styling, and that no page requests third-party resources or needs JS; verify
  every spec `site-prototype` scenario by observation.

## 4. PO review (PO; depends on section 3)

- [x] 4.1 (PO) Review the served prototype (`npm run serve`) against the spec and adjust copy,
  colours or layout requests back to the Architect; verify PO sign-off recorded in
  `PLANNING.md`. (PO approved the Acme-branded version 2026-10-14 and commissioned the theme
  below.)

## 5. Rubber Duck Consulting theme and content (worker-frontend; depends on section 3; design D6, ADR-0019)

- [x] 5.1 Add `@fontsource/baloo-2` and `@fontsource/nunito` dev dependencies, Eleventy
  passthrough of the latin WOFF2 files to `/fonts/`, and hand-written `@font-face` rules in
  `site.css` (Baloo 2 headings, Nunito body, system-stack fallback, `font-display: swap`);
  verify built pages reference fonts only from the site's own `/fonts/` paths and the build is
  clean.
- [x] 5.2 Re-theme `site.css` custom properties to the duck palette (design D6) keeping WCAG AA
  contrast for text and controls; verify tokens changed in one place and no rule references the
  old palette hexes.
- [x] 5.3 Replace the nine placeholder SVGs with custom flat duck-themed illustrations in the
  palette (3 services, 3 events, 3 blog posts) plus any banner/brand mark used; verify all
  images remain local files with alt text and the no-external-requests grep stays clean.
- [x] 5.4 Rewrite all content as Rubber Duck Consulting ("We listen."): site name, nav/footer,
  Home copy, About us, listings data, detail pages and blog post, renaming the sample detail
  slugs to `/services/rubber-duck-debugging/`, `/events/quack-and-learn/` and a fitting blog
  slug; event dates stay after 16 Oct 2026; forms and their fields unchanged; verify the built
  site contains no "Acme" occurrences and every internal link still resolves.

## 6. PO review of the theme (PO; depends on section 5)

- [x] 6.1 (PO) Browse the re-themed prototype and approve or request adjustments; verify PO
  sign-off recorded in `PLANNING.md`. (PO approved the themed prototype 2026-10-14.)
