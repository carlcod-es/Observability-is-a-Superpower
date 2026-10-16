## Purpose

Standalone AI demo: an Umbraco site with sample content whose built-in AI prompt features are
visible in local telemetry, with prompt and response text kept out by default.

Decisions: ADR-0030 (standalone AI demo solution and starter content, runs instead of the main
demo), ADR-0031 (AI features, provider, AI telemetry and content capture switch); in
`docs/adrs/`.

## ADDED Requirements

### Requirement: Standalone AI demo
The AI demo SHALL live in its own top-level folder with its own solution, and SHALL build and
run without changing or depending on the main demo's projects.

#### Scenario: Build on its own
- **WHEN** the presenter builds the AI demo solution
- **THEN** the build succeeds and the main demo's solution is unchanged

### Requirement: One-command start
The presenter SHALL start the AI demo site and its server database with one command, and the
local dashboard SHALL show each as a resource with its state.

#### Scenario: Start the AI demo
- **WHEN** the presenter runs the AI demo start command on a machine with the prerequisites and
  the provider key set
- **THEN** the site and database reach a running state and the site is reachable from the
  dashboard

#### Scenario: Missing provider key
- **WHEN** the provider key has not been set on the machine
- **THEN** the dashboard asks for it before the site starts, and the key is stored outside the
  repo

### Requirement: Runs instead of the main demo
The AI demo SHALL run only after the main demo has been stopped, including the main demo's
database container. It SHALL NOT need the main demo for anything.

#### Scenario: Switch over after act 2
- **WHEN** the presenter follows the presenter notes' switch-over steps with the main demo running
- **THEN** the main demo's resources, including its database container, are stopped and the AI
  demo's site and dashboard are reachable

### Requirement: Prepared before the talk
Install, sample content, the provider key and the backoffice AI setup SHALL be completed before
the talk. A start of the prepared AI demo SHALL run no install, no content or AI setup step and
ask for no key, and the prepared prompt SHALL work straight away.

#### Scenario: Start on stage
- **WHEN** the presenter starts the prepared AI demo
- **THEN** the site comes up with existing content, the dashboard asks for nothing, and the
  administrator can run the prepared prompt without any setup

### Requirement: Presenter notes
The AI demo folder SHALL contain presenter notes covering: the preparation checklist with its
timing before the talk, the switch-over from the main demo, the stage steps (run the prompt, show
the span and its attributes, the provider call, the metrics, flip the capture switch live and
back), what to do when the provider or network fails, and how to reset after the talk.

#### Scenario: Rehearsal from the notes
- **WHEN** the presenter rehearses the segment using only the presenter notes
- **THEN** every step can be carried out as written, with no step needing information outside
  the notes

### Requirement: Sample content on first start
On first start with an empty database the site SHALL install itself and populate ready-made
sample content (home, content pages, blog articles) without manual steps. Data SHALL survive a
restart.

#### Scenario: Fresh database
- **WHEN** the AI demo starts with no existing database data
- **THEN** the public site shows the sample content and the backoffice accepts the demo
  administrator login

#### Scenario: Restart keeps data
- **WHEN** the presenter stops and starts the AI demo
- **THEN** content and AI setup made before the restart are still present

### Requirement: Editor runs an AI prompt
An administrator SHALL be able to open the AI section of the backoffice and run a prepared
prompt against a sample content item, getting a model response in the editor. The connection,
profile and prompt SHALL be set up once and kept in the database; the provider key SHALL be
referenced from configuration, not stored in the database or the repo.

#### Scenario: Run the prepared prompt
- **WHEN** the administrator runs the prepared prompt on a sample blog article
- **THEN** the backoffice shows the model's response

#### Scenario: Key not in the database
- **WHEN** the connection is inspected in the backoffice
- **THEN** its key field shows a configuration reference, not the key

### Requirement: AI calls in telemetry
Each AI call SHALL appear in the local dashboard as a trace span that names the model and carries
the Umbraco profile, the feature (prompt) that started it, the content item and the user, with
the outbound call to the provider beneath it. Token usage and call duration SHALL appear as
metrics.

#### Scenario: Trace for a prompt run
- **WHEN** the administrator runs the prepared prompt
- **THEN** the dashboard shows a chat span for the model with profile, feature, content item and
  user attributes, and a child HTTP span to the provider

#### Scenario: Metrics for a prompt run
- **WHEN** the administrator has run the prompt at least once
- **THEN** the dashboard metrics show token usage (input and output) and operation duration for
  the model

### Requirement: Prompt content kept out by default
By default, prompt and response text SHALL NOT appear in any telemetry. One marked switch in the
AI demo's code SHALL turn capture on for a demo; turning it on SHALL show prompt and response text
on the AI span.

#### Scenario: Default run
- **WHEN** the administrator runs the prompt with the switch in its default position
- **THEN** no span, log or metric in the dashboard contains the prompt or response text

#### Scenario: Capture switched on
- **WHEN** the presenter flips the marked switch, restarts and runs the prompt
- **THEN** the AI span shows the prompt and response text
