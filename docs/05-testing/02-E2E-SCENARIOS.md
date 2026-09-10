# End-to-End Scenarios

## E2E-01 Local MCP secure read
Setup temp workspace -> start bridge -> call workspace_info/read_file -> verify allowed file -> verify outside/sensitive denial.

## E2E-02 Git review evidence
Create temp Git repo -> change normal + `.env` -> git_status/git_diff -> normal change visible, secret path/body absent.

## E2E-03 Execution evidence
Record failed tests + sanitized log -> planner reads summary/test/output -> restricted artifact body unavailable.

## E2E-04 Remote protected MCP
Start fake/real tunnel profile -> pair -> authorize -> call MCP -> wrong scope/workspace denied -> unpair -> subsequent access denied.

## E2E-05 Two-iteration C2C
INIT -> PLAN -> agent change/test fail -> EXECUTED -> planner REPLAN -> agent fix/test pass -> EXECUTED -> planner DONE.

## E2E-06 Crash/restart
Crash after PLAN, after EXECUTING, and after record-before-relay. Restart and verify deterministic next action with no duplicate execution.
