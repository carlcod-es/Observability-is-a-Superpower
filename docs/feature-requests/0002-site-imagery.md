# FR-0002: Images on the site

## Status
Open (2026-10-14). Source: `add-demo-foundation` task 6.4, deferred by the PO.

## Request
Pages show banner and card images from the media library instead of the plain colour block.

## Why
The site looks more believable on stage with imagery. The views already support it
(`add-demo-foundation` design.md Decision 12, spec `agency-site` "Imagery from the media library");
only the media and the page selections are missing.

## Acceptance criteria
- Events and services each have a `mainBanner` image; other pages optional.
- Images are licence-free or generated, and committed: uSync media `.config` files and the files
  under `src/SuperPower.Web/wwwroot/media/`.
- On a fresh database (SQL Server and SQLite) media items import and pages show their images; no
  broken images, no duplicates after a second start.
- Every page type renders after the change (the `<img>` branch of `Views/Partials/Media.cshtml`
  has never run).
- Image requests leave no traces or logs (`/media` is excluded, ADR-0008).

## Notes
- PO 2026-10-14: skipped for now; may come back later.
- Until then the colour-block fallback covers every page (task 9.2 checks it).
- 2026-10-15 (PO request): banner illustrations generated as SVG, one per page, in `banners/`
  (`banners/generate.mjs`, Node, no dependencies; file name = page node name). SVG is the
  smallest format Umbraco 17 handles (default "Vector Graphics (SVG)" media type; AVIF is not
  processed by ImageSharp). Next: PO uploads them in the backoffice and picks `mainBanner` per
  page; uSync exports the media and content.
