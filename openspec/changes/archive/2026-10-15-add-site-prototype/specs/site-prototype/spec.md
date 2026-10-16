## Purpose

A standalone static prototype of the demo site: the pages, layouts, form markup and styling
that later become the basis of the Umbraco demo site's templates. The prototype is branded
Rubber Duck Consulting, tagline "We listen." (PO 2026-10-14, prototype-only); the Umbraco demo
site remains Acme Agency (spec `agency-site`).

## ADDED Requirements

### Requirement: Static pages
The prototype SHALL consist of plain HTML, CSS and JS files that work when served by any static
file server, with no backend and no CMS. It SHALL provide these pages for Rubber Duck
Consulting: Home,
About us, Services, Events, Blog, Contact us, one sample service detail page, one sample event
detail page, one sample blog post page, and a thank-you page. Every page except the thank-you
page SHALL show navigation linking to Home, About us, Services, Events, Blog and Contact us, and
a footer.

#### Scenario: Visitor browses the prototype
- **WHEN** a visitor opens any top-level page from a static file server
- **THEN** the page renders complete with navigation and footer, and every navigation link leads
  to an existing page

### Requirement: Home layout
The Home page SHALL show a main banner, a breadcrumb, a Services section, an Events section, a
Latest articles section, a newsletter signup box and a footer. Each section SHALL show a small
number of items, each linking to the matching listing or detail page. The newsletter signup box
is visual only: it SHALL NOT submit anywhere (prototype-only, PO 2026-10-14).

#### Scenario: Home shows its sections
- **WHEN** a visitor opens the Home page
- **THEN** the banner, breadcrumb, Services, Events and Latest articles sections, newsletter
  signup box and footer are all visible

### Requirement: Text page layout
Text pages (About us; also the blog post page) SHALL show a banner, a breadcrumb, the page
title, the page body and a footer.

#### Scenario: About us
- **WHEN** a visitor opens About us
- **THEN** the page shows banner, breadcrumb, its title and body text, and the footer

### Requirement: Listing page layout
The Services, Events and Blog pages SHALL use one listing layout: a banner, a breadcrumb, and a
list of child items, each with a title, an image, a summary and a link to a detail page. Events
SHALL show their start date and time. Each listing SHALL contain at least three items.

#### Scenario: Services listing
- **WHEN** a visitor opens the Services page
- **THEN** it lists at least three services on the Rubber Duck Consulting theme (PO 2026-10-14;
  for example Rubber Duck Debugging), each with title, image, summary and a link

#### Scenario: Events listing
- **WHEN** a visitor opens the Events page
- **THEN** it lists at least three events with title, image, summary, start date and time, and a
  link

### Requirement: Detail pages with forms
The sample service detail page SHALL show the service's title and description and the Service
enquiry form. The sample event detail page SHALL show the event's title, description, start date
and time and the Event booking form. The sample blog post SHALL use the text page layout with no
form. The Contact us page SHALL show a banner, a breadcrumb and the Contact form.

#### Scenario: Service detail
- **WHEN** a visitor opens the sample service page
- **THEN** it shows the service's description and the Service enquiry form

#### Scenario: Event detail
- **WHEN** a visitor opens the sample event page
- **THEN** it shows the event's details and the Event booking form

### Requirement: Form markup
Forms SHALL be static markup matching the demo forms (spec `form-submission`): Contact (Name,
Email, Message, "Reply by phone?" checkbox), Service enquiry (Name, Email, Company optional,
Website optional, Message), Event booking (Name, Email, Notes optional, "Join mailing list"
checkbox). Forms SHALL NOT submit data anywhere; submitting SHALL lead to the static thank-you
page, which SHALL show "Thank you, we'll be in touch."

#### Scenario: Form leads to thank-you page
- **WHEN** a visitor submits any form on the prototype
- **THEN** the browser navigates to the thank-you page showing "Thank you, we'll be in touch."
  and no data is sent to any server endpoint

### Requirement: Minimal page dependencies
Prototype pages SHALL follow the demo site's constraints (spec `agency-site` "Minimal page
dependencies"): no third-party front-end frameworks, externally hosted fonts or tracking
scripts at runtime; pages SHALL work without client-side scripting; imagery and font files
SHALL be static assets served from the prototype's own files (self-hosted open-licence
webfonts allowed, PO 2026-10-14; ADR-0019).

#### Scenario: No third-party requests
- **WHEN** a visitor loads any prototype page
- **THEN** the browser requests resources only from the prototype's own files
