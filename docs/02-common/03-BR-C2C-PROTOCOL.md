# Common C2C Protocol Rules

| ID | Rule |
|---|---|
| BR-C2C-001 | `TASK_ID` remains stable for the lifetime of one logical task. |
| BR-C2C-002 | `ITERATION` is monotonic and changes only when a new executable/review iteration begins. |
| BR-C2C-003 | PLAN is finite and bounded: one coherent iteration, exact scope, tests and success criteria. |
| BR-C2C-004 | EXECUTED references local evidence by id; it does not paste diff/log/source bodies. |
| BR-C2C-005 | DONE requires stated success criteria to be satisfied and review evidence to be available. |
| BR-C2C-006 | REPLAN is required when evidence shows defects, missing acceptance criteria, scope drift or incomplete tests. |
| BR-C2C-007 | BLOCKED is used for missing external input/capability; ERROR is used for protocol/infrastructure failure. |
| BR-C2C-008 | Resume is local checkpoint behavior, not a protocol state. If the planning conversation is unavailable, use HANDOFF. |
| BR-C2C-009 | HANDOFF contains a bounded brief: goal, constraints, last accepted evidence, open issues and next expected action. |
| BR-C2C-010 | Unknown required fields or incompatible major protocol version fail validation; safe optional extension fields may be ignored by compatible versions. |
