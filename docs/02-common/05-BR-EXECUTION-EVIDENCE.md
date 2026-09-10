# Common Execution Evidence Rules

| ID | Rule |
|---|---|
| BR-EXE-001 | A finalized execution record is immutable. Corrections create a new record/version; they do not silently rewrite evidence. |
| BR-EXE-002 | Changed-file paths exposed remotely pass visibility/sensitive policy before disclosure. |
| BR-EXE-003 | Test status prefers structured adapter metadata over parsing free-form console text. |
| BR-EXE-004 | Raw process output is never exposed directly; it passes sanitizer/classification/size limits first. |
| BR-EXE-005 | Restricted artifacts expose safe metadata only (type/status/size/reason code), never body. |
| BR-EXE-006 | Executor commands are not re-executed by the bridge; the bridge only records evidence supplied by the local executor. |
| BR-EXE-007 | Exit code alone does not imply acceptance; review combines exit status, test summary, changed files and Git diff. |
