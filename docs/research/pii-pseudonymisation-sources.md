# Pseudonymising personal data in telemetry - sources

Research behind the approach used for personal data in this repo's telemetry:
identifiers (email) are replaced by a keyed one-way stand-in shown as
`[REDACTED:<id>]`; free text and names are never recorded.

Claims are paraphrased; check exact wording against the source before quoting.
Retrieved 2026-10-06.

## Keyed hashing is the recommended technique

- **EDPB Guidelines 01/2025 on Pseudonymisation** (European Data Protection Board, adopted 16 Jan 2025)
  https://www.edpb.europa.eu/system/files/2025-01/edpb_guidelines_202501_pseudonymisation_en.pdf
  Claim: a keyed hash (secret key + one-way function) is deterministic, so records stay
  linkable, and cannot be reversed without the key. A compromised key re-identifies
  every record at once, so the key must be kept separate and access-controlled.
  Commentary: https://www.hunton.com/insights/publications/edpb-advises-on-pseudonymisation-for-gdpr-compliance

## Plain hashing of emails is not enough

- **AEPD / EDPS - Introduction to the hash function as a personal data pseudonymisation technique** (2019)
  https://www.edps.europa.eu/sites/default/files/publication/19-10-30_aepd-edps_paper_hash_final_en.pdf
  Claim: an unkeyed hash of a guessable value (such as an email address) can be reversed by
  hashing candidate values and comparing (dictionary attack). Keyed hashing with a
  non-trivial secret key prevents this.

## Industry security guidance

- **OWASP Logging Cheat Sheet**
  https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html
  Claim: personal and sensitive data should usually not be logged directly; remove, mask,
  sanitise, hash or encrypt it. Use a hashed or tokenised representation when some form of
  the value is needed for debugging or audit.

## The `[REDACTED:<hash>]` format in practice

- **OpenObserve - How to redact PII from LLM telemetry without losing debuggability**
  https://openobserve.ai/blog/redact-pii-llm-telemetry/
  Claim: a hash-based redaction mode emits `[REDACTED:<hash>]`, so repeated values can be
  traced across spans and logs without exposing them. Their example uses an unkeyed hash,
  which is the weakness described by AEPD/EDPS above.
- **alkem-io/kratos-webhooks PR #21 - pseudonymise identifiers (keyed HMAC) and truncate client IPs in login logs**
  https://github.com/alkem-io/kratos-webhooks/pull/21
  Claim: a real codebase replacing identifiers in logs with keyed hashes, normalising emails
  (trim, lowercase) first, and testing that logs contain no emails or full IP addresses.

## Pseudonymised data and who it is personal data for

- **CJEU, Case C-413/23 P, EDPS v SRB** (judgment 4 Sep 2025)
  https://www.goodwinlaw.com/en/insights/publications/2025/09/alerts-technology-dpc-personal-data-or-not
  https://www.twobirds.com/en/insights/2025/can-pseudonymisation-make-data-anonymous
  Claim: pseudonymised data remains personal data for the controller who holds the key, but
  may not be personal data for a recipient who has no realistic means to re-identify it.
  Use: supports the wording "the agent sees nothing that identifies a person" rather than
  "there is no personal data".

## Takeaways for slides

- Plain hash is a disguise; a keyed hash is protection.
- Normalise before hashing or correlation silently breaks.
- Stand-ins suit identifiers; free text cannot be made safe by hashing - don't record it.
- Pseudonymised is still personal data for the key holder; for a party without the key it may not be.
