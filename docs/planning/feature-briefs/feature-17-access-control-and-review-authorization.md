# Access Control and Review Authorization

**Priority:** Should-have; required before shared or production use

As an invoice-processing administrator, I want access and review actions limited by user role so that sensitive documents are protected and financial decisions have appropriate separation of duties.

Authenticate users before they access real invoice records. Define permissions for submitting documents, viewing cases, correcting extracted values, linking evidence, resolving exceptions, and changing verification or matching policies. Record the actor and action in the audit trail. A user must not gain payment-approval authority merely by correcting extraction or match data.

Clarify identity provider, roles, tenant or team boundaries, retention of access events, and any maker-checker requirements. Payment approval itself is outside this feature.

Evidence storage (Feature 08) and intake currently require only a signed-in user (`EvidenceStoragePolicy`, `SourceDocumentIntakePolicy`). The only sign-in is a development test scheme (`Authorization: Test <value>`, always the same test user), and the API refuses to start outside the Development environment until this feature provides real authentication. Define who may perform each evidence action:

- Create evidence packages and correct record values while a package is open.
- Change review status, finalize a package to `confirmed`, and replace confirmed evidence with a new version.
- Link match outcomes and record verification results (service accounts or users).
- Archive packages after the retention period.
- Query across all cases, rather than only cases the user is assigned to.

Decide whether the person who corrected a value may also finalize the same package (maker-checker). The audit actor is already taken from the authenticated identity, never from the request; keep that rule and map roles to the existing actor types (`user`, `reviewer`, `system`, `model`).