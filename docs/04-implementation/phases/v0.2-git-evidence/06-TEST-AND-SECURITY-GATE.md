# V0.2 Test and Security Gate

## Goal

Prove that Git evidence is useful to the planner without expanding the C2C workspace boundary or disclosing denied content.

This phase is security-critical. A passing happy-path Git test is insufficient.

## Test layers

```text
Unit
  parser / normalization / pagination / revision validation

Integration
  real temporary Git repository + real git executable

Host integration
  MCP adapter + DI + serialization + error mapping

Adversarial
  sensitive files / rename boundary / workspace subdirectory / oversized output
```

## UC-GIT-01 matrix

| Scenario | Expected |
|---|---|
| clean repository | empty success |
| allowed modified file | visible |
| allowed staged file | visible |
| allowed untracked file | visible |
| allowed delete | visible |
| allowed rename -> allowed | visible with safe metadata |
| `.env` modified | completely omitted |
| `.env.local` modified | completely omitted |
| allowed sample env file | follows existing sensitive policy |
| private key file modified | omitted |
| `.c2cignore` denied file modified | omitted |
| allowed -> denied rename | omitted/fail closed |
| denied -> allowed rename | omitted/fail closed |
| workspace subfolder, sibling changed | sibling omitted |
| spaces in path | parsed correctly |
| newline in path where supported | parsed through NUL format |
| malformed status payload unit fixture | safe parser error |
| non-Git workspace | `GIT_NOT_AVAILABLE` |
| timeout | `C2C_TIMEOUT` |
| cancellation | owned process stops; no orphan |
| > server max entries | bounded page + continuation |

## UC-GIT-02 matrix

| Scenario | Expected |
|---|---|
| one allowed text modification | bounded patch returned |
| multiple allowed modifications | stable order/page |
| allowed + `.env` modification | only allowed patch returned |
| allowed + private-key modification | secret path/body absent |
| denied-only changes | empty success |
| allowed rename -> allowed | safe patch/metadata |
| allowed rename -> denied | entire rename diff omitted |
| denied rename -> allowed | entire rename diff omitted |
| large patch | truncated/paginated within hard bound |
| binary change | metadata only; no binary payload |
| invalid/unsupported base | stable invalid argument |
| workspace subfolder in larger repo | no sibling patch |
| file removed between candidate and read | safe skip/error per policy |
| file becomes denied between stages | fail closed |
| timeout/cancellation | stable safe outcome |

## Canary-secret tests

Use explicit canaries such as:

```text
C2C_TEST_SECRET_ENV_7b82
C2C_TEST_PRIVATE_KEY_CANARY_91ac
```

Place them only in denied test fixtures.

Assertions should verify the canary is absent from:

```text
MCP result text
serialized error body
returned Git DTOs
captured structured logs when test harness supports log inspection
```

Do not rely only on checking that `.env` is absent from the final item list.

## Git mutation guard

Before and after each Git status/diff integration scenario, verify read operations did not intentionally mutate repository state.

At minimum preserve:

- `HEAD` revision;
- index/staged content;
- working-tree fixture content except changes deliberately made by the test itself.

No test implementation may call `git add`, `checkout`, `reset`, `clean`, `commit`, or similar as part of the system under test. Test fixture setup may use Git mutation before invoking C2C behavior.

## Workspace-boundary test

Required layout:

```text
repo-root/
├── workspace/
│   └── visible.cs
├── sibling-secret/
│   └── .env
└── .git/
```

Bind C2C to `repo-root/workspace`.

Modify both `visible.cs` and the sibling secret.

Expected:

```text
git_status -> visible.cs only
git_diff   -> visible.cs patch only
```

No output may reveal `sibling-secret`, `.env`, its body, or an absolute repo-root path.

## Rename boundary tests

Renames are blocking security scenarios because Git metadata and patch headers can reveal both old and new names.

Required cases:

```text
allowed -> allowed   : visible
allowed -> denied    : not visible
denied  -> allowed   : not visible
denied  -> denied    : not visible
```

Both source and destination must pass the visibility policy before a rename record/body can be exposed.

## Performance/bounds gate

Use generated fixtures to prove:

- changed-file count cap;
- diff byte cap;
- process output cap;
- cursor/page continuation;
- cancellation under long-running process fake or controlled integration fixture.

Do not use sleeps for timing logic when a fake clock/controlled process can prove behavior deterministically.

## Build gate

Before V0.2 is declared done:

```bash
dotnet build C2CNet.sln
dotnet test C2CNet.sln
```

Focused security/integration test commands should also be included in the completion report.

## Blocking failures

Do not continue to V0.3 if any of these occur:

```text
denied filename visible
secret canary visible
broad diff captured before filtering
workspace sibling path visible
shell command construction introduced
Git mutation capability introduced
unbounded process output
raw stderr/local absolute path returned remotely
existing workspace security regression
```

A test must never be weakened to make one of these failures pass.