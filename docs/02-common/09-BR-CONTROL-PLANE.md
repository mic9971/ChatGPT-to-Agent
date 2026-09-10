# Control Plane Business Rules

These rules apply to automated or manual transport of C2C messages between an execution agent and the planning/review client.

| ID | Rule |
|---|---|
| BR-CTRL-001 | **Bounded control only.** Control messages carry protocol/task metadata, plan/review decisions and evidence references only. |
| BR-CTRL-002 | **No source/diff/log relay.** Source bodies, raw diffs and raw logs remain on the MCP data plane. |
| BR-CTRL-003 | **User owns browser authentication.** Agents may navigate to login but SHALL pause for password, passkey, CAPTCHA, MFA/2FA or equivalent challenges and SHALL NOT capture/persist those secrets. |
| BR-CTRL-004 | **Conversation binding.** An automated conversation reference is bound to exactly one `workspace_id + task_id`; reuse across tasks/workspaces is forbidden. |
| BR-CTRL-005 | **Protocol validation precedes acceptance.** `TASK_ID`, `ITERATION`, `STATE`, version and allowed transition are validated before local checkpoint advancement. |
| BR-CTRL-006 | **Browser failure is not a state transition.** UI timeout/crash/navigation failure leaves the last accepted C2C checkpoint unchanged. |
| BR-CTRL-007 | **Duplicate-safe delivery.** Identical message hashes may be retried idempotently; conflicting content for the same transition is rejected. |
| BR-CTRL-008 | **Evidence-before-EXECUTED.** `EXECUTED` may be relayed only after its referenced execution evidence is persisted successfully. |
| BR-CTRL-009 | **Semantic automation first.** Prefer accessibility/semantic selectors and response boundaries over fixed coordinates or brittle visual timing. |
| BR-CTRL-010 | **Replaceable transport.** C2C.Core SHALL NOT depend on Antigravity, Codex, ChatGPT DOM details or a proprietary browser SDK. |
| BR-CTRL-011 | **Authentication separation.** MCP OAuth credentials, local runtime/admin credentials and ChatGPT browser-account authentication remain separate trust domains. |
| BR-CTRL-012 | **Manual fallback.** Failure of an automated driver SHALL permit an operator-assisted control path without weakening protocol validation or security rules. |
| BR-CTRL-013 | **No hidden credential material.** Conversation/session persistence contains no cookies, browser storage, passwords, passkeys or ChatGPT account tokens. |
| BR-CTRL-014 | **No speculative implementation.** Control-plane driver code is introduced only in the approved V0.8 slice after C2C protocol/session contracts exist. |

## Relationship to existing BRs

`BR-COM-004`, `BR-C2C-*`, `BR-AUTH-*` and `BR-EXE-*` remain normative. These rules define transport/automation behavior and do not replace protocol, OAuth or execution-evidence rules.
