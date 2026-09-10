# C2C.NET Agent Entry Rules

This repository is **design-first and use-case-driven**.

## Before any code change

1. Read `.agent/rules/00-GLOBAL.md`.
2. Find the target UC in `docs/00-governance/01-USE-CASE-CATALOG.md`.
3. Read that UC file only plus its explicit dependencies.
4. Read every referenced `BR-*` in `docs/02-common/`.
5. Read the minimum architecture references listed by the UC.
6. Inspect existing source/tests nearest the change.

## Hard constraints

- Implement one UC (or one explicitly bounded dependency slice) at a time.
- Do not redesign architecture while coding. Stop and propose a design change/ADR first.
- MCP remains read-only in V1.
- One bridge = one workspace.
- Security ambiguity fails closed.
- Do not claim build/test success without actually running commands.
- Do not commit unless explicitly requested.

## Completion report

Return: target UC, files changed, BRs preserved, tests/commands run, acceptance criteria status, remaining risks, and any design deviation.
