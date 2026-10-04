<!--
Sync Impact Report
Version: 1.2.1 -> 1.3.0 (MINOR: new obligation)
Principles modified: I. Data Integrity and Human Review (names the shared document contract); IX. Intuitive and Consistent Naming (new bullet: build on established shared contracts; record name mappings instead of parallel models)
Sections modified: none
Prior: 1.2.0 -> 1.2.1 clarified Architecture and Security Constraints (shared intake, pilot storage, development-only sign-in); 1.1.0 -> 1.2.0 added IX.
Templates: plan/spec/tasks templates read the constitution at runtime; no template edits required.
Follow-up TODO: Create the detailed frontend design-system document and shared MUI theme module (carried from 1.1.0); until then, frontend plans record the gap as a deviation. Set RATIFICATION_DATE.
-->

# Invoice Processing Constitution

## Core Principles

### I. Data Integrity and Human Review
OCR and LLM outputs are untrusted candidate data. Validate them against the shared, versioned document contract in `ILP.Shared` (`DocumentDto` and the extraction keys) and retain traceability to the source document. Uncertain, invalid, or mismatched results MUST be presented for human review before downstream use.

### II. Clear System Boundaries
The React and TypeScript dashboard MUST use the ASP.NET Core API for application operations. Provider credentials and OCR/LLM requests MUST remain on the backend. Production components use .NET by default; Python experiments require a clear benefit and a documented production boundary.

### III. Provider Independence
Provider-specific request and response formats MUST remain behind adapters. Ollama, OpenRouter, and other providers MUST NOT define or change the application invoice contract.

### IV. Deterministic Financial Decisions
Invoice arithmetic, tolerances, and three-way matching MUST be implemented as deterministic, testable business rules. An LLM MUST NOT approve an invoice or decide whether a financial discrepancy is acceptable.

### V. Privacy and Credential Security
Credentials MUST NOT be committed. Gmail access MUST be limited to the permissions needed for the feature. Logs MUST avoid invoice contents and other sensitive data unless explicitly required and protected.

### VI. Practical Verification
Every feature MUST define how it will be validated. Automated tests MUST cover invoice contracts and deterministic business rules. Unit tests MUST NOT call live Gmail or model services; external-service checks MUST be explicit integration tests.

### VII. Consistent Frontend Design
All React and TypeScript interfaces MUST use the shared Material UI theme and follow the project's frontend design-system documentation. Feature specifications and plans that include frontend work MUST reference that design system; implementations MUST reuse its palette, typography, spacing, component, interaction, and accessibility conventions rather than creating feature-specific alternatives. Deviations MUST be documented and justified.

### VIII. Usable and Accessible Workflows
Interfaces MUST make the next meaningful action easy to find, use plain and consistent language, and provide clear loading, empty, success, and error states. Errors MUST explain what happened and offer a useful recovery action where possible. Recoverable failures MUST preserve user-entered data and in-progress work. Destructive actions MUST be clearly distinguished and require confirmation when their effects are difficult to undo. Core workflows MUST support keyboard operation, visible focus, semantic labels, WCAG 2.2 AA contrast, and status communication that does not rely on color alone.

### IX. Intuitive and Consistent Naming
Names for entities, types, API resources and fields, statuses, and services MUST be understandable to an accounts-payable user or a new team member without a glossary, and MUST mean the same thing in every feature.

- Prefer names that say what a thing holds or does and that cannot be confused with a sibling type (for example `ProvenanceEntry` rather than `ProvenanceRecord` next to `StructuredRecord`). Where a spec defines a domain term such as "provenance", code SHOULD use the same term so specs and code can be searched together, and the feature's data model SHOULD define it once in plain language.
- One term per concept: an entity's type, ID field, API route, folder, user-facing messages, and spec wording MUST use the same term (for example `EvidencePackage`, `evidencePackageId`, `/api/evidence-packages`, "evidence package").
- Reuse established domain terms from the feature briefs and existing specs. A feature MUST NOT name a new entity with a term another feature owns; referring to the owner's concept by its identifier is allowed (for example `caseId` refers to the case owned by document packet tracking). Owned terms are listed in the [feature brief index](../../docs/planning/feature-briefs/feature-brief-index.md#owned-terms).
- Build on established shared contracts. Before adding a model, reuse or extend the existing one in `ILP.Shared` (for example, the typed extracted document is `DocumentDto`, and evidence header records use the extraction keys in `DocumentHeaderFields`). A parallel model MUST state its reason in the plan. Where an established contract names a concept differently (for example `RefNumber`, `document-number`, and `sourceReference`), record the mapping in the owned-terms table and rename only with the owning feature's agreement.
- Names MUST NOT imply a cardinality or ownership the model does not have (for example, do not call something "the case's file" when a case can have several).
- Inside a feature namespace, name types by their role (`RetentionRules`); keep a feature prefix only where the bare name would collide across features (`EvidenceDocument` versus the intake source document).
- API and JSON names MUST NOT use abbreviations (`purchase-order-commitment`, not `po-commitment`).
- Specs and plans that introduce a new entity or API resource SHOULD state the chosen name and, where the choice is not obvious, the alternatives considered.
- Renames MUST be applied across code, tests, contracts, and specs in the same change.

Rationale: AP users, reviewers, and auditors read these names in the API and audit trail; jargon or inconsistent names slow review and cause cross-feature confusion.

## Architecture and Security Constraints

The target architecture is a React and TypeScript dashboard, an ASP.NET Core API, and shared C# contracts. Every document channel (camera capture, file upload, and mailbox) enters through the shared source-document intake API; the mailbox collector is a backend worker that submits to that API rather than calling extraction providers directly. The API coordinates intake, OCR, LLM providers, validation, evidence storage, and review workflows. Feature specifications and documentation MUST distinguish implemented behavior from planned components; today evidence storage is a file-backed pilot store and sign-in is a development-only test scheme until access control (Feature 17) is delivered. Python may be used for bounded experiments, but production interfaces and ownership MUST be documented.

## Development Workflow and Quality Gates

Feature specifications MUST define inputs, outputs, failure cases, validation expectations, and review behavior. Frontend specifications MUST describe the primary user workflow and its loading, empty, success, and recoverable-error states. Plans MUST preserve the system boundaries and provider-independent invoice contract. Frontend plans MUST identify the shared frontend design-system document and MUI theme entry point they will use. Implementations MUST validate primary workflows, failure recovery, keyboard access, and contrast where applicable. Each implementation MUST include a validation plan and appropriate automated tests; strict test-driven development is not required. Tests that require Gmail, Ollama, or cloud providers MUST be opt-in and clearly identified. Uncertain or mismatched extraction and matching results MUST remain reviewable rather than silently accepted.

## Governance

This constitution governs feature specifications, plans, implementation, and reviews. Pull requests MUST identify and justify deviations from its principles. Amendments require team review and approval, a rationale, and a semantic version update: MAJOR for incompatible changes to principles, MINOR for new or materially expanded requirements, and PATCH for clarifications that do not change obligations.

**Version**: 1.3.0 | **Ratified**: TODO(RATIFICATION_DATE): Set when the team formally adopts this constitution. | **Last Amended**: 2026-10-04
