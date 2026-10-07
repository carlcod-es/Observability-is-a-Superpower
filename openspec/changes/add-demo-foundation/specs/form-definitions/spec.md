## Purpose

Lets editors define a form in the CMS from a list of field blocks, without custom back-office
screens, so that the site's Contact and Book a class forms are content, not code.

## ADDED Requirements

### Requirement: Field types
An editor SHALL be able to build a form from these field types: Text, Email, Textarea,
Checkbox and Select.

#### Scenario: Editor adds fields
- **WHEN** an editor edits a form definition
- **THEN** they can add, remove and reorder fields of each of the five types

### Requirement: Field settings
Every field SHALL have an alias, a label and a required flag, and MAY have a validation
pattern with an error message. A Select field SHALL also have a list of options.

#### Scenario: Field with pattern
- **WHEN** an editor gives a Text field a pattern and an error message
- **THEN** the form uses them when validating that field

#### Scenario: Select options
- **WHEN** an editor adds options to a Select field
- **THEN** the rendered field offers exactly those options in the given order

### Requirement: Unique field aliases
Field aliases SHALL be unique within a form.

#### Scenario: Duplicate alias
- **WHEN** a form definition contains two fields with the same alias
- **THEN** the form refuses to render and reports the duplicate in the site log, naming the
  form and alias

### Requirement: Package self-containment
The forms package SHALL create the field types and its own storage when it is installed,
independently of how the site populates its content.

#### Scenario: Package installed into an empty site
- **WHEN** the package is added to a site with no other content
- **THEN** after start-up the five field types are available to editors and submissions can
  be stored

### Requirement: Site form definitions
The site SHALL define two forms:
- Contact: Name (Text, required), Email (Email, required), Message (Textarea, required),
  Reply by phone? (Checkbox).
- Book a class: Name (Text, required), Email (Email, required), Class (Select, required;
  Wheel throwing, Hand building, Glazing), Experience notes (Textarea, optional), Join mailing
  list (Checkbox).

#### Scenario: Forms exist after first start
- **WHEN** the site has been populated on first start
- **THEN** both forms exist with exactly these fields, types and required flags
