# Reference Data and Rules Architecture

## Storage

Reference data is kept in approved local snapshots in the server file store. Source payloads remain behind read-only adapters. A synchronization run stages and validates a complete candidate snapshot before atomic publication.

## Observability

Do not log source credentials, customer payment data, or complete source payloads. Log synchronization run identifiers, source identity, trigger type, outcome, record counts, validation reason codes, publication snapshot identifier, actor, approval identifier, and trace identifier.

## Unverified outcomes

A check that depends on missing, unavailable, conflicting, failed, or out-of-effective-period data must return an explicit unverified result with a reason code and recovery guidance. A failed synchronization never replaces the previous complete snapshot.

## Scope boundary

This feature reads ERP and policy sources only. It does not approve payments and does not write to authoritative ERP or policy systems.
