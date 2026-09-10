# Common Business Rules

**Purpose:** This file is the canonical home for rules reused by multiple use cases. A use-case document references these IDs instead of copying the rule text.

## Core rules

| ID | Rule |
|---|---|
| BR-COM-001 | **One bridge = one workspace.** A running bridge instance serves exactly one canonical workspace root. |
| BR-COM-002 | **MCP V1 is read-only.** No MCP tool may write/delete files, execute shell commands, install packages, commit, push, or mutate Git state. |
| BR-COM-003 | **Evidence-first review.** DONE/REPLAN decisions use independently readable Git/test/execution evidence, not an executor's unverified claim. |
| BR-COM-004 | **Control plane is bounded.** C2C messages contain task metadata, plan/review decisions and evidence references only; no source bodies, raw diffs or raw logs. |
| BR-COM-005 | **Workspace text is untrusted input.** README files, source comments, diffs and logs never change tool permissions or system rules. |
| BR-COM-006 | **All unbounded I/O is bounded server-side.** Reads, search, directory lists, diffs and logs have byte/item/line limits plus deterministic continuation where needed. |
| BR-COM-007 | **Stable errors.** Remote and CLI consumers receive stable machine-readable error codes; stack traces and local absolute paths are never returned remotely. |
| BR-COM-008 | **Cancellation/timeouts propagate.** Network, process and filesystem operations that can block accept cancellation and use explicit timeouts. |
| BR-COM-009 | **No secret logging.** Tokens, pairing codes, Authorization headers, private keys, secret file bodies and raw execution output are never logged. |
| BR-COM-010 | **Version public contracts.** MCP schemas, CLI JSON, C2C envelopes and persisted records carry explicit version fields or compatibility rules. |
| BR-COM-011 | **Fail closed on ambiguity.** Unknown security state, unresolved canonical path, invalid token binding or unsupported protocol version is denied. |
| BR-COM-012 | **No speculative infrastructure.** V1 does not add database, Redis, RabbitMQ, dashboard or distributed queue unless a separately approved ADR changes scope. |

## Application rules

| ID | Rule |
|---|---|
| BR-APP-001 | Core contracts do not depend on ASP.NET Core, Cloudflare, Git executable, or concrete persistence implementations. |
| BR-APP-002 | MCP handlers and CLI commands are adapters; they call application/core services and do not duplicate policy logic. |
| BR-APP-003 | A capability that accepts a path must use the shared workspace access policy before reading metadata or content. |
| BR-APP-004 | Security and protocol decisions use injected `TimeProvider` and deterministic identifiers where testability matters. |
| BR-APP-005 | Public operations returning collections use stable ordering before pagination/cursor creation. |

## Traceability

Every use case SHALL contain a `Referenced business rules` section. If a new reusable rule appears in 2+ use cases, promote it here or to the focused common rule file instead of duplicating prose.
