# V0.6 — CLI & Local Lifecycle Implementation Pack

## Goal

Turn the existing C2C.NET Core/Infrastructure capabilities into a stable local command surface for humans and execution agents.

The CLI is an adapter, not a second business layer. It must reuse Workspace, Tunnel/Runtime, Authorization and Execution services instead of duplicating their policy.

## Current baseline

At the time this pack is authored, `main` contains V0.4 Tunnel implementation and the V0.5 Authorization/Pairing implementation design pack. Runtime work for V0.6 must not begin until the V0.5 gate is complete and protected MCP/auth behavior is accepted.

## Scope

Phase V0.6 includes:

- `UC-CLI-01` Setup CLI
- `UC-CLI-02` Start and Stop Runtime
- `UC-CLI-03` Status and Doctor
- `UC-CLI-04` Ensure Runtime Ready
- `UC-CLI-05` Pair and Unpair CLI
- `UC-CLI-07` Record Evidence and Read Local Logs

`UC-CLI-06 Manage C2C Session` is deliberately **not** implemented in V0.6. Its declared phase is V0.7 and it depends on C2C protocol/checkpoint use cases.

## Implementation waves

```text
V0.6A CLI substrate + stable JSON/exit contract
  -> UC-CLI-01 setup
  -> UC-CLI-03 status/doctor

V0.6B owned runtime lifecycle
  -> UC-CLI-02 start/stop
  -> UC-CLI-04 ensure

V0.6C auth-facing commands
  -> UC-CLI-05 pair/unpair

V0.6D execution/local diagnostics
  -> UC-CLI-07 record/logs

Final CLI gate
  -> V0.7 C2C protocol/checkpoint
```

## Primary architecture

```text
Human / Execution Agent
        |
        v
     C2C.Cli
        |
        +--> Workspace services
        +--> Runtime/Tunnel services
        +--> Authorization services
        +--> Execution services
        +--> Local diagnostics reader

C2C.Cli must not contain workspace security, OAuth/token, tunnel-provider,
execution sanitizer or C2C protocol business rules.
```

## Non-goals

- no C2C session/checkpoint implementation yet;
- no browser automation;
- no Antigravity/ChatGPT control-plane driver;
- no installer/updater/package manager;
- no silent package installation;
- no arbitrary shell execution;
- no arbitrary log/file reader;
- no new remote MCP tools;
- no weakening of existing auth/tunnel/workspace boundaries.

## Required project shape

A dedicated executable adapter is justified:

```text
src/
  C2C.Cli/
    Program.cs
    Composition/
    Output/
    Setup/
    Runtime/
    Diagnostics/
    Authorization/
    Execution/

tests/
  C2C.IntegrationTests/
    Cli/
```

Do not introduce global `Services/`, `Models/`, `Helpers/` or `Interfaces/` dumping folders.

## Read order

1. `01-BASELINE-AND-ENTRY-GATE.md`
2. `02-CLI-ARCHITECTURE.md`
3. `03-CLI-CONTRACT-AND-EXIT-CODES.md`
4. UC implementation files `04` through `09`
5. `10-TEST-AND-SECURITY-GATE.md`
6. `11-ANTIGRAVITY-EXECUTION-ORDER.md`
7. `12-PHASE-DONE-AND-HANDOFF.md`

## Phase invariant

The most important rule is:

> Human-friendly output may evolve cosmetically; `--json` field meaning, status mapping and exit-code semantics are versioned contracts for agents.
