> Amended 2026-10-14 (PO, ADR-0021): `prototype/` removed. The site's `wwwroot` holds the only
> stylesheet and script; the copy step and drift check are dropped. Prototype items below are
> historical.

## Why

Attendees open the demo site on their phones (QR code to the forms during the talk), and the
static prototype is the design reference for the site. Neither is tuned for phone screens: the
main navigation wraps over several lines, and the two stylesheets have drifted apart (different
class names, colour names and asset paths), so a styling fix in one does not carry to the
other.

## What Changes

- The demo site and the static prototype work on phone, tablet and desktop widths: no sideways
  scrolling, readable text, full-width forms, comfortable tap targets, content images that fit
  the screen.
- On narrow screens the main navigation collapses behind a "Menu" button that opens and closes
  it. Without client-side scripting the navigation stays visible and every page still works.
- One stylesheet and one script serve both the prototype and the demo site, used unchanged by
  both. The prototype is where they are authored; the site receives an exact copy, and an
  automated check fails when the site's copy differs from the prototype's.
- The prototype's markup adopts the demo site's class names (forms, imagery, messages), so the
  shared stylesheet styles both without per-site rules.
- Prototype-only pieces (Blog, newsletter box) and site-only states (validation errors, success
  message, "This event has ended", missing-image blocks) all live in the one shared
  stylesheet.

Out of scope:
- Visual redesign, new pages, new content or brand changes (the prototype's and the site's
  brand names stay as they are).
- Backoffice (Umbraco admin) layout.
- Print styles, dark mode, right-to-left layouts.
- Automated cross-browser or visual-regression testing.

## Capabilities

### New Capabilities
- `responsive-layout`: layout behaviour across screen widths for the demo site and the
  prototype, the collapsible main navigation, and the single shared stylesheet and script.

### Modified Capabilities
<!-- none: `agency-site` and `site-prototype` are still in unarchived changes
     (`add-demo-foundation`, `add-site-prototype`); their "Minimal page dependencies"
     requirements still hold (pages work without client-side scripting) and are not changed. -->

## Impact

- Prototype: stylesheet, a new small script, page layout and form markup.
- Demo site: master layout (menu button, script reference), stylesheet replaced by the shared
  copy, one script added. No change to content, document types, controllers or telemetry.
- A new automated check in the site's test suite.
- Supersedes the `add-site-prototype` expectation that the prototype is a throwaway starting
  point: it becomes the source of the site's stylesheet and script.
