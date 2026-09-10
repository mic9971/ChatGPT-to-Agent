# V0.6 Antigravity Execution Order

## Operating rule

Implement one bounded slice at a time. Do not let spare context expand V0.6 into C2C protocol/session or browser automation.

## Step 0 — Entry verification

Prompt:

```text
Verify the current ChatGPT-to-Agent main branch before Phase V0.6 implementation.

Read AGENTS.md, applicable .agent/rules and the dotnet project-conventions skill.
Read docs/04-implementation/phases/v0.6-cli/00-README.md through 03-CLI-CONTRACT-AND-EXIT-CODES.md.

Do not modify files.

Confirm:
- V0.4 Tunnel is fully present
- V0.5 runtime Authorization/Pairing is implemented and accepted, not merely documented
- all existing tests are green
- no existing C2C.Cli implementation conflicts with this design
- actual Core/Infrastructure service contracts to reuse
- current logging sink/runtime ownership behavior

Run full build and test.
Return STATE: READY_FOR_V0_6 or BLOCKED with exact missing gate.
Stop.
```

If V0.5 is not implemented, stop. Do not implement Phase V0.6 runtime code yet.

## Step 1 — V0.6A PLAN

```text
Prepare an implementation PLAN for V0.6A only:
- CLI substrate/common output contract
- UC-CLI-01 Setup
- UC-CLI-03 Status and Doctor

Read the V0.6 phase pack, UC-CLI-01, UC-CLI-03 and referenced BRs.
Inspect actual source before proposing files.

Do not edit yet.

Return:
- exact files/projects to create/change
- selected CLI parsing package and compatibility evidence
- DI/composition strategy
- JSON envelope and exit-code mapping implementation
- setup delegation flow
- status/doctor diagnostic contracts
- tests and non-goals

No start/stop/ensure/auth/record/log implementation yet.
```

## Step 2 — V0.6A IMPLEMENT

```text
Implement the approved V0.6A plan only.

Required:
- dedicated C2C.Cli executable adapter
- capability-oriented folders
- no hand-parsed args if approved framework is available
- stable --json envelope
- stdout clean in JSON mode
- setup delegates workspace policy
- setup does not silently expose remote tunnel or create credentials
- status and doctor are read-only
- bounded diagnostics and safe output
- no unrelated refactor
- no commit unless explicitly requested

Run focused tests, full build and full tests.
Stop after V0.6A.
```

## Step 3 — V0.6B PLAN / runtime lifecycle

```text
Prepare PLAN for UC-CLI-02 and UC-CLI-04 only.

Inspect the actual V0.4 Tunnel implementation, especially:
- IBridgeRuntime
- LoopbackBridgeRuntime
- IRuntimeEnsurer / RuntimeEnsurer
- owned-process runner/validator
- tunnel session store/service

Explain how c2c start launches an owned background C2C.Host process and how c2c stop proves ownership before termination.
PID alone is forbidden as ownership proof.

Explain whether existing V0.4 owned-process abstractions should be reused, promoted to a neutral runtime capability, or kept separate. Do not duplicate them without justification.

Cover concurrency, stale PID/PID reuse, port conflict, tunnel sequencing, cancellation and tests.
Do not edit yet.
```

## Step 4 — V0.6B IMPLEMENT

```text
Implement approved UC-CLI-02 and UC-CLI-04 only.

Critical invariants:
- bridge remains loopback-only
- no shell
- no kill by process name
- validate process identity before stop/force-kill
- repeated/concurrent start produces one owned runtime
- ensure reuses IRuntimeEnsurer/state machine rather than duplicating policy
- missing pairing returns action required; never bypass auth
- no infinite retry
- runtime state persistence is atomic

Run adversarial ownership tests and all prior suites.
Stop after V0.6B.
```

## Step 5 — V0.6C PLAN + IMPLEMENT

Plan first, review, then implement UC-CLI-05 only.

Required checks:

```text
pair -> existing V0.5 pairing service
unpair -> existing V0.5 revoker
no CLI token logic
no long-lived credential output
pairing code only in explicit pair result and never normal logs
cross-workspace revoke denied
```

Stop after auth CLI tests are green.

## Step 6 — V0.6D PLAN + IMPLEMENT

Plan first, review, then implement UC-CLI-07 only.

Required:

```text
record reuses V0.3 IExecutionRecorder
logs reads C2C-owned diagnostics only
no arbitrary file option
bounded tail/time/bytes
redaction before persisted diagnostic log
no raw artifact body echoed by record
```

Stop after tests are green.

## Step 7 — Final phase review

```text
Review Phase V0.6 against:
- UC-CLI-01,02,03,04,05,07
- all referenced BRs
- V0.6 CLI contract/exit codes
- process ownership security gate
- auth/output canary gate
- dotnet-project-conventions

Explicitly verify UC-CLI-06 was NOT implemented; it belongs to V0.7.
Run full build/tests.
Return PASS or BLOCKED with exact acceptance failures.
Do not start V0.7.
```

## Global stop conditions

Antigravity must stop rather than improvise if:

- V0.5 runtime auth is incomplete;
- background process lifetime cannot be made deterministic cross-platform without a design decision;
- implementation would kill a process without strong ownership evidence;
- CLI would need a generic shell/exec capability;
- a token/code/private key appears in unintended output/log state;
- `logs` would require arbitrary path reading;
- existing V0.4/V0.5 contracts need a broad redesign;
- completing the task requires C2C session/checkpoint or browser automation.
