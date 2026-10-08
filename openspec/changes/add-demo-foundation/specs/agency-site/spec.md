## Purpose

A small, believable website for a fictional marketing agency that gives the demo forms a
realistic home and produces readable, low-noise telemetry for normal page requests.

## ADDED Requirements

### Requirement: Agency pages
The site SHALL present a fictional marketing agency named Acme Agency with these public pages:
Home, About us, Contact us, Events (list), one page per event, Services (list) and one page per
service. The site navigation SHALL link to Home, About us, Events, Services and Contact us from
every page.

#### Scenario: Visitor navigates the site
- **WHEN** a visitor opens the Home page
- **THEN** the page shows the agency's introduction and upcoming events, and the navigation
  links to About us, Events, Services and Contact us

#### Scenario: Contact page shows the contact form
- **WHEN** a visitor opens the Contact us page
- **THEN** it shows the Contact form

### Requirement: Events
Each event SHALL have a title, a description, a start date and time, and a format of either in
person or online. An in-person event SHALL have a capacity in places, set by the editor; an
online event SHALL have no capacity. Events are free; the site SHALL NOT show or take payment.
The site SHALL include at least two in-person events and one online event, for example "How to
get the most out of your CMS", all starting after 16 Oct 2026 so they are bookable during the
talk.

#### Scenario: Events list
- **WHEN** a visitor opens the Events page
- **THEN** it lists upcoming events in date order with title, date, time and format, each
  linking to its event page

#### Scenario: Upcoming event page
- **WHEN** a visitor opens the page of an event that has not started
- **THEN** it shows the event's details and the Event booking form

#### Scenario: Past event page
- **WHEN** a visitor opens the page of an event whose start time has passed
- **THEN** it shows "This event has ended" and no booking form

#### Scenario: Editor sets capacity
- **WHEN** an editor creates an in-person event
- **THEN** they set its capacity in places; an online event offers no capacity setting

### Requirement: Services
Each service SHALL have a title and a description. The site SHALL include at least Umbraco
upgrade, SEO audit and AdWords audit. Services have no capacity.

#### Scenario: Services list
- **WHEN** a visitor opens the Services page
- **THEN** it lists every service with its title, each linking to its service page

#### Scenario: Service page
- **WHEN** a visitor opens a service page
- **THEN** it shows the service's description and the Service enquiry form

### Requirement: Content is populated on first start
When the site starts against an empty database, it SHALL install itself and create all
document types, templates, pages, events, services and form definitions without manual steps.
Content SHALL be kept in source control; the database SHALL NOT be.

#### Scenario: Fresh database
- **WHEN** the site starts with an empty database
- **THEN** all pages, events, services and the three forms are available and published once
  start-up completes

#### Scenario: Existing database
- **WHEN** the site starts with a database that already holds the content
- **THEN** existing content is not overwritten or duplicated

### Requirement: Minimal page dependencies
Pages SHALL use simple styling with no third-party front-end frameworks, external fonts or
tracking scripts, and SHALL work without client-side scripting, so that page-request telemetry
contains only the site's own work.

#### Scenario: Page request makes no third-party calls
- **WHEN** a visitor loads any page
- **THEN** the browser requests resources only from the site itself

### Requirement: No media library dependency
The site SHALL NOT depend on uploaded media; any imagery SHALL be static site assets.

#### Scenario: Fresh environment has complete pages
- **WHEN** the site runs against a freshly populated database
- **THEN** no page shows a missing image
