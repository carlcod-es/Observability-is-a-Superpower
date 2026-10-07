## Purpose

A small, believable website for a fictional pottery studio that gives the demo forms a
realistic home and produces readable, low-noise telemetry for normal page requests.

## ADDED Requirements

### Requirement: Studio pages
The site SHALL present a fictional pottery studio (working name Slipcast Studio) with exactly
three public pages: Home, Classes and Contact. Every page SHALL link to the other two.

#### Scenario: Visitor navigates the site
- **WHEN** a visitor opens the Home page
- **THEN** the page shows the studio's introduction and upcoming classes, and links to Classes
  and Contact

#### Scenario: Classes page lists the classes
- **WHEN** a visitor opens the Classes page
- **THEN** it lists Wheel throwing, Hand building and Glazing, and shows the Book a class form

#### Scenario: Contact page shows the contact form
- **WHEN** a visitor opens the Contact page
- **THEN** it shows the Contact form

### Requirement: Content is populated on first start
When the site starts against an empty database, it SHALL install itself and create all
document types, templates, pages and form definitions without manual steps. Content SHALL be
kept in source control; the database SHALL NOT be.

#### Scenario: Fresh database
- **WHEN** the site starts with an empty database
- **THEN** all three pages and both forms are available and published once start-up completes

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
