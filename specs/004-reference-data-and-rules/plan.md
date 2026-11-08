# Implementation Plan: Reference Data and Business Rules

**Branch**: `004-reference-data-and-rules` | **Date**: 2026-11-07 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/004-reference-data-and-rules/spec.md`

## Summary

Add controlled ERP and IRAS reference-data synchronization, versioned business-rule packages, effective-date selection, atomic snapshot publication, and decision provenance to the existing ASP.NET Core API and shared contracts. Reference-dependent checks use approved local snapshots; source updates trigger refresh where supported, daily reconciliation detects missed updates, and manual refresh is authorized. Missing, unavailable, conflicting, partial, or out-of-coverage data produces an explicit unverified result rather than a financial pass.

## Technical Context

**Language/Version**: C# / .NET 10; TypeScript only for future review UI integration, not required for the initial server contract.

**Primary Dependencies**: ASP.NET Core minimal APIs, Microsoft.Extensions.Configuration, Microsoft.Extensions.Hosting, existing persistence abstractions, and shared C# contracts. ERP adapters and source notification handling are feature-specific integration boundaries.

**Storage**: The pilot MUST use a deterministic file-backed repository with immutable staged snapshot files, atomic file replacement, and durable synchronization-run/audit records. Database migration is deferred until operational scale, concurrency, or query requirements justify it.

Source notifications are capability-based: adapters that support an update notification may trigger reconciliation immediately; adapters without notifications are reconciled by the daily fallback. Notification capability and adapter protocol do not form part of shared reference-data contracts.

**Testing**: xUnit or existing .NET test framework in `tests/ILP.Server.Tests`, deterministic unit tests, integration tests for adapters and synchronization, contract tests, and command-line/HTTP validation in `quickstart.md`.

**Target Platform**: Backend web service; source integration adapters may run in the server or a dedicated background worker.

**Project Type**: Full-stack server feature with shared contracts; frontend review components are covered only where required by existing workflows.

**Performance Goals**: Snapshot lookup must be suitable for normal invoice-check volume; synchronization is asynchronous and must not block invoice checks. No latency threshold is specified in the feature brief.

**Constraints**: Atomic publication, effective-date correctness, source provenance, deterministic financial rules, separation of duties, no payment approval, and no edits to authoritative external systems.

**Scale/Scope**: Multiple source categories, multiple document variants, multiple effective rule versions, and representative pilot reference volumes. Scale targets are deferred until operational source volumes are known.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Gate | Assessment |
|---|---|---|
| I. Data Integrity and Human Review | Preserve provenance and unverified outcomes. | Pass: snapshots, traces, effective dates, and explicit unverified reasons are required. |
| II. Clear System Boundaries | Backend owns credentials and source integration. | Pass: adapters and synchronization run in the server/worker; clients use application APIs. |
| III. Provider Independence | Integrations remain behind adapters. | Pass: ERP and IRAS source formats do not define shared contracts. |
| IV. Deterministic Financial Decisions | Financial checks are deterministic. | Pass: GST selection and arithmetic use versioned rules; no LLM decision path. |
| V. Privacy and Credential Security | Credentials stay server-side; logs avoid sensitive values. | Pass: source credentials are configuration secrets; audit logs record identities and reasons, not source payloads. |
| VI. Practical Verification | Tests cover normal and failure paths. | Pass: unit, integration, contract, and quickstart validation are defined. |
| VII. Consistent Frontend Design | Use shared design system when frontend work is added. | Pass for this server-focused phase; frontend source status and review work is subject to the existing shared theme. |
| VIII. Usable and Accessible Workflows | Explain unverified states and recovery. | Pass when status/decision UI is added; contract responses include reason codes and recovery guidance. |
| IX. Intuitive and Consistent Naming | Reuse shared terms; no parallel models. | Pass: canonical names are defined in this plan and data model; implementation must reuse shared contracts and owned terms. |

## Project Structure

### Documentation (this feature)

```text
specs/004-reference-data-and-rules/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── reference-data-and-rules.md
├── checklists/
│   └── requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── server/ILP.Server/Features/ReferenceData/
│   ├── Adapters/
│   ├── Endpoints/
│   ├── Repository/
│   └── Service/
src/shared/ILP.Shared/ReferenceData/
├── Dto/
└── Contracts/

tests/ILP.Server.Tests/
└── ReferenceDataAndRulesTests.cs
```

**Structure Decision**: Keep source adapters behind the server `ReferenceData` feature, place shared wire contracts in `ILP.Shared`, and keep synchronization, deterministic rule selection, publication, and audit logic server-owned. Existing evidence and invoice contracts are reused rather than duplicated.

## Post-Design Constitution Check

| Principle | Post-design result |
|---|---|
| I. Data Integrity and Human Review | Pass: immutable snapshots and decision traces support deterministic replay; all incomplete inputs produce explicit outcomes. |
| II. Clear System Boundaries | Pass: ERP/IRAS credentials and source payload retrieval remain server-side. |
| III. Provider Independence | Pass: source-specific adapters terminate before shared contracts and deterministic rule services. |
| IV. Deterministic Financial Decisions | Pass: effective rule packages are versioned and selection is deterministic; no probabilistic decision is stored as authoritative policy. |
| V. Privacy and Credential Security | Pass: source credentials remain outside repository and source payload contents are excluded from ordinary logs. |
| VI. Practical Verification | Pass: quickstart and test layers cover atomicity, effective dates, variants, failures, and provenance. |
| VIII. Usable and Accessible Workflows | Pass: contract status includes reasons and recovery guidance for later UI integration. |
| IX. Intuitive and Consistent Naming | Pass: canonical entities and identifiers are documented consistently across artifacts. |

## Complexity Tracking

No constitution violations require a complexity waiver.
