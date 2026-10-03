<!--
Sync Impact Report
Version: uninitialized -> 1.0.0 (initial constitution)
Principles added: I-VI (initial set)
Added sections: Architecture and Security Constraints; Development Workflow and Quality Gates
Removed sections: template examples and placeholders
Follow-up TODO: Set the ratification date when the team formally adopts this constitution.
-->

# Invoice Processing Constitution

## Core Principles

### I. Data Integrity and Human Review
OCR and LLM outputs are untrusted candidate data. Validate them against a versioned invoice contract and retain traceability to the source document. Uncertain, invalid, or mismatched results MUST be presented for human review before downstream use.

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

## Architecture and Security Constraints

The target architecture is a React and TypeScript dashboard, an ASP.NET Core API, and shared C# contracts. The API coordinates Gmail or image inputs, OCR, LLM providers, validation, and review workflows. Feature specifications and documentation MUST distinguish implemented behavior from planned components. Python may be used for bounded experiments, but production interfaces and ownership MUST be documented.

## Development Workflow and Quality Gates

Feature specifications MUST define inputs, outputs, failure cases, validation expectations, and review behavior. Plans MUST preserve the system boundaries and provider-independent invoice contract. Each implementation MUST include a validation plan and appropriate automated tests; strict test-driven development is not required. Tests that require Gmail, Ollama, or cloud providers MUST be opt-in and clearly identified. Uncertain or mismatched extraction and matching results MUST remain reviewable rather than silently accepted.

## Governance

This constitution governs feature specifications, plans, implementation, and reviews. Pull requests MUST identify and justify deviations from its principles. Amendments require team review and approval, a rationale, and a semantic version update: MAJOR for incompatible changes to principles, MINOR for new or materially expanded requirements, and PATCH for clarifications that do not change obligations.

**Version**: 1.0.0 | **Ratified**: TODO(RATIFICATION_DATE): Set when the team formally adopts this constitution. | **Last Amended**: 2026-10-03
