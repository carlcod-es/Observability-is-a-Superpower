# Review follow-ups (minors and nits)

## Section 2 (2.1)
- nit: `dotnet build tests/smoke/smoke.cs` warns NU1902/NU1903 for System.Security.Cryptography.Pkcs 9.0.11
  (transitive through Microsoft.Data.SqlClient). Pre-existing. Recheck in section 3; "Clean build" covers the
  solution, the smoke script is outside it. If it remains after 3.1, Architect decides.
- nit: `Directory.Packages.props` has no trailing newline (pre-existing). No action.

## Section 1
- ~~minor: `EventDateShift.cs:30` formats the date with the current culture~~ Fixed (supervisor: also wrong
  calendar under th-TH): `CultureInfo.InvariantCulture` + test `Json_UnderThaiCulture_WritesGregorianIsoDate`.
- nit: test uses `new CultureInfo("th-TH")`, which throws in invariant-globalization mode. No action.
- nit: a shifted local time in the spring-forward gap (01:00-01:59) gets an odd offset; no event at those times. No action.
- risk (PO): a backoffice content export writes the shifted dates into the `.config` files; revert `startDate`
  changes before committing.
- nit: `EventDateShift.cs:14` `FindSystemTimeZoneById("Europe/London")` relies on ICU IANA ids on Windows
  (.NET 10 default). No action.
- nit: `EventImportComposer.cs` holds composer and handler; matches `AcmeFormsComposer`. No action.

## Section 6
- nit: README.md:229-231 names `aspire agent init --skills aspire` then `aspire agent init`; use one form.
- nit: README.md:230 "without the approval prompt" — more precisely "outside the MCP approval prompts".
- nit (pre-existing): README.md:226 "The agent cannot restart resources" vs the opt-in Aspire MCP's prompted
  resource commands; consider "Without approval the agent cannot restart resources".
- check: confirm `get_integration_docs` is read-only from its description in `/mcp` next time the Aspire MCP
  is connected.
