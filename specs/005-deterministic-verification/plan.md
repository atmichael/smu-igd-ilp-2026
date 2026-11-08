# Implementation Plan: Deterministic Invoice Verification

**Branch**: `005-deterministic-verification` | **Date**: 2026-11-07 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/005-deterministic-verification/spec.md`

## Summary

Implement an evidence-driven verification feature in the ASP.NET Core server that deterministically evaluates classified invoices against effective versioned rules. Verification will reuse `DocumentDto`, evidence records, and reference-data rule packages; produce pass, fail, unavailable, and not-applicable check outcomes; preserve calculation and source provenance; and expose blocked runs and MVP review resolution. The React dashboard will consume these results and provide accessible review actions for signed-in users. LLM and confidence output will never determine verification results or payment approval.

## Technical Context

**Language/Version**: C# / .NET 10; TypeScript / React for the existing web dashboard.

**Primary Dependencies**: ASP.NET Core minimal APIs, existing evidence and transaction-document services, `ILP.Shared` contracts, reference-data rule packages, React, Material UI, and existing Vitest/React tests.

**Storage**: Verification runs, checks, calculation traces, review resolutions, and immutable input references will use the existing SQLite/EF server persistence pattern, matching the current transaction-document and evidence pilot architecture. Completed runs are immutable; changed inputs create a new run and retain superseded-run history for replay and audit.

**Testing**: xUnit/Microsoft Testing Platform tests in `tests/ILP.Server.Tests`; frontend Vitest tests; deterministic arithmetic fixtures; contract and HTTP tests; IRAS-derived representative-document tests.

**Target Platform**: ASP.NET Core backend and existing browser web application.

**Project Type**: Full-stack server feature with a focused verification review workflow.

**Performance Goals**: Normal invoice verification should complete within the existing synchronous document-processing boundary; no explicit latency target is available in the brief. Large calculation sets remain linear and evidence lookups must be snapshot-based.

**Constraints**: Deterministic decimal arithmetic and rounding; effective-date rule selection; evidence for every required-field pass; immutable verification provenance; unavailable dependencies fail closed; re-verification after input changes; no payment approval; any signed-in user may resolve review items only in the MVP.

**Scale/Scope**: Singapore GST document variants, simplified/regular invoices, credit notes, representative foreign currencies, subtotal/item methods, mixed supplies, and review resolution.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Gate | Assessment |
|---|---|---|
| I. Data Integrity and Human Review | Candidate data remains untrusted and uncertain results require review. | Pass. Evidence-backed checks, immutable traces, blocked unavailable runs, and review resolution are required. |
| II. Clear System Boundaries | Invoice operations cross React to ASP.NET Core; credentials remain backend-side. | Pass. Calculation and reference lookup remain server-owned. |
| III. Provider Independence | LLM/OCR outputs remain inputs behind existing adapters. | Pass. No provider format enters verification contracts. |
| IV. Deterministic Financial Decisions | Arithmetic and financial discrepancies use deterministic rules. | Pass. GST, rounding, currency, and totals are server-computed business rules. |
| V. Privacy and Credential Security | Avoid sensitive invoice values in logs. | Pass. Audit metadata avoids full source payloads where unnecessary. |
| VI. Practical Verification | Automated normal and failure tests are required. | Pass. Unit, contract, integration, and browser validation are planned. |
| VII. Consistent Frontend Design | Reuse the shared Material UI theme. | Pass. Review UI will use `src/client/web/src/theme.ts`, with the existing document-detail workflow and accessible Material UI components. |
| VIII. Usable and Accessible Workflows | Review states and actions are understandable and keyboard accessible. | Pass. Outcome reasons and next actions must accompany every unresolved check. |
| IX. Intuitive and Consistent Naming | Reuse document and evidence contracts and document owned terms. | Pass. Verification names use existing `DocumentDto`, evidence, and reference-data terminology; no parallel invoice contract. |

## Project Structure

### Documentation (this feature)

```text
specs/005-deterministic-verification/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── deterministic-verification.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
src/server/ILP.Server/Features/Verification/
├── Contracts/
├── Endpoints/
├── Persistence/
├── Review/
└── Service/

src/shared/ILP.Shared/Verification/
├── Contracts/
└── Enums/

src/client/web/src/features/verification/
├── components/
├── model/
├── service/
└── verification-review/

tests/ILP.Server.Tests/
├── DeterministicVerificationTests.cs
└── VerificationReviewTests.cs
```

**Structure Decision**: Add a server `Verification` feature for orchestration, deterministic engines, persistence, and review resolution; add minimal shared verification contracts; integrate review results into the existing React document workflow. Existing `DocumentDto`, evidence packages, reference-data packages, and transaction-document endpoints remain the canonical inputs.

## Post-Design Constitution Check

| Principle | Post-design result |
|---|---|
| I | Pass: verification runs preserve input references, outcomes, calculation traces, and review resolution. |
| II | Pass: backend owns rule selection, calculations, persistence, and review authorization; frontend uses application APIs. |
| III | Pass: candidate extraction providers remain outside verification contracts. |
| IV | Pass: decimal business rules produce deterministic outcomes without LLM adjudication. |
| V | Pass: review audit contains actor, reason, and references rather than unnecessary document payloads. |
| VI | Pass: Phase 1 includes deterministic fixtures, contract tests, API tests, and browser workflow tests. |
| VII | Pass: frontend follows the existing Material UI theme and document-detail accessibility patterns. |
| VIII | Pass: every blocked check exposes a reason and recovery action. |
| IX | Pass: canonical shared terms are mapped and reused; verification-specific models are new only for verification-owned concepts. |

## Complexity Tracking

No constitution violations require a complexity waiver.
