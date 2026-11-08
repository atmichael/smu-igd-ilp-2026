# Phase 0 Research: Reference Data and Business Rules

## Decision 1: Store authoritative reference snapshots locally

**Decision:** Maintain approved, synchronized snapshots in persistent product storage; do not read authoritative ERP or IRAS sources live for each invoice check.

**Rationale:** Persistent snapshots provide deterministic replay, effective-date selection, audit traceability, and continued operation when a source is unavailable. Source availability is reflected separately in synchronization status.

**Alternatives considered:** Live source lookup for every check (rejected because it risks nondeterminism, latency, and availability-dependent decisions); environment variables (rejected because they lack provenance, lifecycle, effective dates, and audit history).

## Decision 2: Synchronization mechanism

**Decision:** Use source update notifications when supported, daily reconciliation as a fallback, and authorized on-demand refresh. Unsupported notification integrations rely on reconciliation only.

**Rationale:** Event-triggered refresh reduces delay while reconciliation prevents missed updates. A manual refresh provides operational recovery.

**Alternatives considered:** Daily pull only (rejected because updates can be delayed); real-time polling only (rejected because source capability and load vary); live evaluation reads (rejected for lack of replayability).

## Decision 3: Atomic publication of snapshots

**Decision:** Synchronization stages an import, validates all required records and policy invariants, then publishes it atomically as an approved snapshot. Failed or partial imports preserve the last complete snapshot and create an unsuccessful synchronization run.

**Rationale:** Checks require a complete, internally consistent reference set; partial publication can create false combinations and contradictions.

**Alternatives considered:** Incremental publication (rejected because it permits partial snapshots); replacing only changed records (rejected unless consistency and approval are revalidated across the full affected scope).

## Decision 4: Authoritative operational sources

**Decision:** Vendor identities and aliases come from the ERP vendor master; purchasing references come from ERP purchasing records; Singapore GST policy comes from the named IRAS invoicing guidance. Other categories require category-specific source ownership at rollout.

**Rationale:** These decisions preserve external-system authority while keeping synchronization controlled locally.

**Alternatives considered:** A single internal master for all categories (deferred; requires a separately governed source-of-truth owner); live use of external records without snapshots (rejected).

## Decision 5: Source and snapshot metadata

**Decision:** Store source identity, source record identity, source URL or integration reference, retrieval time, synchronization run, last successful synchronization, effective period, review status, review owner, review date, data version, and publication status.

**Rationale:** This metadata is necessary to reproduce checks and demonstrate source authority.

## Decision 6: Failure semantics

**Decision:** Missing, unavailable, conflicting, failed, partial, out-of-effective-period, or ambiguous reference inputs produce an explicit unverified result rather than pass, fail, or inferred success. Conflict handling may require human resolution under the approved governance workflow.

**Rationale:** This prevents unsupported financial conclusions and follows the project's data-integrity principle.

## Decision 7: Rule governance

**Decision:** The tax-policy owner and AP operations lead approve governed GST rules, policy rules, and tolerances. Requesters and approvers must be distinct people.

**Rationale:** It preserves separation of duties and business ownership without implementing payment approval.

## Technology fit

The repository already uses ASP.NET Core, .NET 10, shared C# contracts, persistent repositories, deterministic tests, and React. The reference-data feature should extend server-side services and shared contracts, with synchronization adapters kept behind integration boundaries. The local snapshot store can use the repository's existing persistence approach selected in implementation planning; the specification intentionally leaves the storage technology undecided.
