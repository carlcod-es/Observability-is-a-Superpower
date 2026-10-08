# ADR-0005: Form rendering and server-side validation

## Status
Accepted (2026-10-07)

## Context
Specs `form-submission` require rendering without client scripts, server-side validation only,
forgery protection, re-display with entered values and errors, and no duplicate submission on
refresh.

## Decision
- Rendering: a ViewComponent `SuperPowerForm` invoked from site templates with the picked form
  node; default view shipped in the package (Razor class library), overridable by the site.
- Posting: an Umbraco `SurfaceController` action with `[ValidateAntiForgeryToken]`. Posted
  values are bound by field alias.
- Validation: a pure validator in the package (required, email address, pattern with
  timeout, select option membership). No `required`/`pattern` HTML attributes; inputs use
  `novalidate` so the browser never blocks a post.
- Submission handlers (ADR-0013, added 2026-10-08): `Prepare` runs on render and before
  validation on post; a closed form shows its message instead of fields and rejects posts;
  fixed fields are not rendered and must not be overridden by the post. `OnStored` runs in the
  same scope as the submission insert.
- Failure: return the current page with ModelState errors and entered values.
- Success: store, set a TempData flag, redirect to the current page (post/redirect/get); the
  view shows the form's `successMessage` (ADR-0004) when the flag is present.

Alternatives considered:
- Client-side plus server-side validation: better UX, but invalid attempts would never reach
  the server and could not be observed; rejected (validation outcomes are useful telemetry
  in a later change).
- Minimal API endpoint + JS fetch: needs client scripting; rejected.

## Consequences
- Regex patterns run with a match timeout to bound cost of editor-supplied patterns.
- The validator is unit-testable without Umbraco (ADR-0012).
