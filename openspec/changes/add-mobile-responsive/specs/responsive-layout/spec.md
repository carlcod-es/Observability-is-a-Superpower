## Purpose

Makes the demo site and the static prototype usable from phone to desktop widths, with one
stylesheet and one script shared unchanged between them.

## ADDED Requirements

### Requirement: Layout fits every screen width
Every page of the demo site and of the prototype SHALL fit the viewport from 320 CSS pixels
wide upwards: no horizontal scrolling, text readable without zooming, content images and
embedded media no wider than the screen, long words and URLs wrapping instead of overflowing.
Listings SHALL show one card per row on narrow screens and several per row on wide screens.

#### Scenario: Phone width
- **WHEN** a visitor opens any page at 320, 375 or 390 CSS pixels wide
- **THEN** the page has no horizontal scroll and all content is visible by scrolling down only

#### Scenario: Tablet and desktop widths
- **WHEN** a visitor opens a listing page (Services, Events, or the prototype's Blog) at 768
  and at 1280 CSS pixels wide
- **THEN** cards sit several per row and the page has no horizontal scroll

#### Scenario: Wide content in page body
- **WHEN** a page body contains an image wider than the screen or an unbroken long URL
- **THEN** the image scales down to the content width and the URL wraps, with no horizontal
  scroll

### Requirement: Forms usable on phones
On narrow screens the Contact, Service enquiry and Event booking forms SHALL use the full
content width; inputs, text areas and the submit button SHALL be at least 44 CSS pixels tall;
and focusing an input SHALL NOT make a phone browser zoom the page. Validation messages,
the success message and the "This event has ended" notice SHALL stay readable at phone
widths.

#### Scenario: Submit a form on a phone
- **WHEN** a visitor at 375 CSS pixels wide fills in and submits any of the three forms
- **THEN** every field and the submit button span the content width, are easy to tap, and the
  page does not zoom when a field is focused

#### Scenario: Validation errors on a phone
- **WHEN** a visitor at 375 CSS pixels wide submits a demo site form with missing required
  fields
- **THEN** the error messages appear next to their fields without horizontal scroll

### Requirement: Collapsible main navigation
Below tablet width, when client-side scripting runs, the main navigation SHALL be hidden behind
a "Menu" button in the header; activating the button SHALL show the navigation, activating it
again SHALL hide it, and the button SHALL expose its open or closed state to assistive
technology. The button SHALL be operable by keyboard. At tablet width and above, the full
navigation SHALL show in the header with no button. Without client-side scripting, no button
SHALL show and the full navigation SHALL be visible at every width.

#### Scenario: Open and close the menu on a phone
- **WHEN** a visitor at 375 CSS pixels wide activates "Menu", then activates it again
- **THEN** the navigation links appear, then disappear, and the button reports expanded, then
  collapsed

#### Scenario: Keyboard operation
- **WHEN** a keyboard user tabs to "Menu" and presses Enter or Space
- **THEN** the navigation opens and its links are next in the tab order

#### Scenario: Wide screen
- **WHEN** a visitor opens any page at 768 CSS pixels wide or more
- **THEN** all navigation links show in the header and no "Menu" button is visible

#### Scenario: Scripting disabled
- **WHEN** a visitor with client-side scripting disabled opens any page at any width
- **THEN** no "Menu" button shows, all navigation links are visible, and every page and form
  works

### Requirement: Shared stylesheet and script
The demo site SHALL use the prototype's stylesheet and script byte-for-byte (line endings
aside), authored once in the prototype and copied to the site (ADR-0020). An automated check in
the site's test suite SHALL fail when the site's copy of either file differs from the
prototype's. Fonts SHALL be requested from the same paths in both. The script SHALL be served
by the site itself and SHALL NOT add traces for its own request (ADR-0008 excluded paths).

#### Scenario: Copy is in sync
- **WHEN** the site's test suite runs after the prototype's stylesheet or script changed and
  the copy step ran
- **THEN** the check passes

#### Scenario: Copy drifted
- **WHEN** the site's stylesheet or script is edited directly, or the prototype's is changed
  without running the copy step
- **THEN** the check fails and names the file that differs

#### Scenario: No third-party or traced requests
- **WHEN** a visitor loads any demo site page
- **THEN** the browser requests the stylesheet, script and fonts only from the site itself,
  and no trace is recorded for those requests
