# V0.7 — UC-CLI-06 Manage C2C Session

## Purpose

Expose the new C2C session capability through the stable V0.6 CLI/JSON contract without teaching the CLI protocol business rules.

## Command shape

Recommended V1 surface:

```bash
c2c session new [options]
c2c session status --task <task-id>
c2c session executing --task <task-id> --iteration <n>
c2c session accept --task <task-id> --file <planner-message-file>
c2c session executed --task <task-id> --iteration <n> --execution-id <id>
c2c session handoff --task <task-id>
```

The final shape may be simplified after reviewing the implemented V0.6 CLI conventions. Avoid accepting secrets or large control messages directly on the command line when that would leak through process listings/shell history. Prefer stdin or a bounded local file for planner message input where appropriate.

`c2c session status` is the primary restart/recovery query for agents.

## Agent-facing JSON

Reuse the V0.6 envelope. Example:

```json
{
  "schemaVersion": 1,
  "command": "session status",
  "status": "ready",
  "errorCode": null,
  "actionRequired": "wait_for_plan",
  "data": {
    "taskId": "c2c_ab12",
    "iteration": 1,
    "checkpoint": "init_sent",
    "lastExecutionId": null
  },
  "warnings": []
}
```

Do not create a second unrelated JSON envelope just for C2C.

## `session new`

Delegates to `IC2CSessionService.CreateTaskAsync`.

Inputs are bounded goal/constraints and optional caller idempotency key. Output contains:

- task id;
- iteration 0;
- checkpoint;
- rendered INIT control message or a safe reference to it according to CLI output policy;
- next action.

Do not start browser automation.

## `session status`

Read-only. Returns bounded safe metadata:

```text
taskId
iteration
checkpoint/protocol state
waitingFor
lastExecutionId
known issue count or bounded summaries
nextExpectedAction
terminal status
```

Default status must not dump full PLAN text or full conversation transcript.

## `session accept`

Used by manual/future integration layers to submit planner C2C text into parser/validator/session service.

Input rules:

- hard 4 KB cap before parse;
- stdin/local bounded file preferred over giant command argument;
- input file path itself is local CLI input and must follow explicit local-file policy; do not turn this command into an arbitrary source file reader;
- parser errors return stable machine-readable code.

This command is transport-neutral: the caller obtained the planner text elsewhere.

## `session executing`

Thin adapter for UC-C2C-03. Requires current executor lease or invokes the approved lease acquisition path. Does not run the PLAN.

## `session executed`

Thin adapter for UC-C2C-04. Validates the referenced execution record through Core services and renders the bounded EXECUTED control message.

It does not read/paste execution output into the JSON response beyond approved safe metadata.

## `session handoff`

Delegates to recovery/handoff service and returns a bounded HANDOFF message. It never includes transcript/source/diff/log bodies.

## Exit code mapping

Use V0.6 stable categories. Suggested mappings:

```text
0  success / idempotent success
2  invalid CLI/protocol input
3  not ready / expected external action
4  security/policy denial
5  dependency/evidence unavailable
10 internal failure
```

Specific `errorCode` remains authoritative for agent decisions.

## Security

- Never print access/refresh tokens.
- Never print browser cookies/session tokens.
- Do not expose arbitrary app-state paths in JSON.
- Planner message is untrusted input.
- Session status is local metadata, not a backdoor MCP evidence endpoint.
- Avoid command-line args for sensitive/large text where shell history/process inspection would be risky.

## Tests

- JSON schema stability for every subcommand;
- `session new` + restart + status;
- valid/invalid PLAN through `session accept`;
- stale iteration exit/error mapping;
- `session executed` cross-workspace evidence rejection;
- handoff bounds and redaction;
- no source/diff/log body in default JSON;
- invalid/corrupt checkpoint yields safe recovery error;
- human and JSON modes share behavior, not duplicated business logic.
