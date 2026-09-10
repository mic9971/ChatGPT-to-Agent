# UC-CTRL-08 — Recover Control Driver Failure

## Metadata

| Field | Value |
|---|---|
| Status | DESIGN-READY |
| Implementation status | NOT-STARTED |
| Priority | P1 |
| Phase | V0.8 |
| Module | Control Plane Automation |
| Primary actor | Execution Agent / Control Driver |
| Depends on | UC-CTRL-03, UC-CTRL-04, UC-CTRL-06, UC-CTRL-07 |

## Goal

Recover from browser/UI automation failures without corrupting C2C state, leaking credentials or forcing the user to abandon the task.

## Main flow

1. Classify failure: auth-required, navigation, composer unavailable, send ambiguity, response timeout or browser crash.
2. Keep the last accepted protocol checkpoint unchanged.
3. Retry only bounded safe operations.
4. For send ambiguity, inspect the conversation before any resend.
5. If automation remains unavailable, switch to `ManualControlDriver` with the same validated envelope/session.
6. If the conversation is lost, route to HANDOFF.

## Referenced business rules

`BR-CTRL-003`, `BR-CTRL-006`, `BR-CTRL-007`, `BR-CTRL-009`, `BR-CTRL-012`.

## Error codes

- `CTRL_USER_AUTH_REQUIRED`
- `CTRL_CONVERSATION_UNAVAILABLE`
- `CTRL_COMPOSER_UNAVAILABLE`
- `CTRL_SEND_AMBIGUOUS`
- `CTRL_RESPONSE_TIMEOUT`
- `CTRL_DRIVER_UNAVAILABLE`

## Tests

- browser crash;
- composer unavailable;
- timeout after submit;
- authentication challenge;
- fallback to manual;
- repeated recovery stays idempotent.

## Acceptance criteria

- [ ] Driver failure alone never advances C2C state.
- [ ] MFA/CAPTCHA/password flows always require user action.
- [ ] Manual fallback preserves protocol validation and task identity.

## Architecture references

- `01-architecture/12-ERROR-RECOVERY.md`
- `01-architecture/20-CONTROL-PLANE-AUTOMATION.md`
