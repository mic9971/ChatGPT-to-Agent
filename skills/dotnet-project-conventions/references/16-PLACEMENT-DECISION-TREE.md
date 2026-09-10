# Placement Decision Tree

Use this before every new file/folder.

```text
START
  |
  |-- Is there a nearest existing pattern for the same capability/role?
  |       |-- YES -> follow it unless it violates approved architecture/security.
  |
  |-- What capability owns the behavior?
  |       -> Workspace / Execution / Authorization / Tunnel / ControlPlane / ...
  |
  |-- What role is the type?
  |       |-- core invariant/value/policy -> Core/<Capability>
  |       |-- use-case request/result/orchestration -> Core/Application/<Capability>/<UseCase> or nearest approved equivalent
  |       |-- filesystem/process/Git/provider implementation -> Infrastructure/<Capability>[/<Provider>]
  |       |-- auth/token implementation -> Security/<Capability>
  |       |-- MCP/HTTP schema/endpoint -> Host/<Transport>/<Capability>
  |       |-- CLI schema/command -> Cli/<Capability-or-command-group>
  |       |-- test -> matching test project/<Capability>[/<UseCase>]
  |
  |-- Is this a data-holding type?
  |       -> classify with MODEL-TAXONOMY before naming.
  |
  |-- Does it appear shared?
  |       |-- same shape only -> DO NOT SHARE
  |       |-- same stable semantics across capabilities -> Common candidate; justify ownership.
  |
  |-- Does it need an interface?
  |       |-- real boundary/provider/nondeterminism -> interface near abstraction owner
  |       |-- only for mocking/convention -> concrete type first
  |
  `-- Still ambiguous?
          -> STOP. Report 2 placements, trade-offs, and which existing convention each follows.
```

## Examples

### `RefreshTokenResult`
Capability = Authorization; role = use-case output.

```text
Authorization/RefreshToken/RefreshTokenResult.cs
```

not `Models/TokenModel.cs`.

### `CloudflareTunnelProvider`
Capability = Tunnel; role = external provider implementation.

```text
Infrastructure/Tunnel/Cloudflare/CloudflareTunnelProvider.cs
```

### `ReadFileToolResponse`
Capability = Workspace; role = MCP transport schema.

```text
Host/Mcp/Workspace/ReadFileToolResponse.cs
```

### `OperationResult<T>`
If already proven as a stable cross-capability primitive, `Core/Common` is acceptable. Do not use this as precedent to move capability-specific results into Common.