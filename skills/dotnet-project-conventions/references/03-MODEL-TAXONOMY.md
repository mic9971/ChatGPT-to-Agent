# Model Taxonomy

Every new data-holding type must be classified before placement/naming.

## Categories

### 1. Domain / core model
Has identity, invariant, lifecycle, or durable business/system meaning.

Examples: `WorkspaceId`, `ExecutionRecord`, `ControlConversationBinding`.

### 2. Use-case input/output
Exists to execute one application operation.

Examples: `ReadFileRequest`, `ReadFileResult`, `ConfigureWorkspaceRequest`.

Place next to the use case/capability that owns it.

### 3. Transport contract/schema
Shape belongs to MCP, HTTP, CLI JSON, or another wire/adapter boundary.

Examples: `ReadFileToolResponse`, `DoctorJsonResponse`.

Keep it in the adapter project. Do not leak it into Core unless the protocol itself is a Core contract.

### 4. Persistence representation
Shape exists because of file/database/storage serialization.

Example: `WorkspaceConfigDocument` if storage shape differs from Core `WorkspaceConfig`.

Place with persistence implementation.

### 5. Configuration/options
Runtime configuration bound from config/environment.

Examples: `TunnelOptions`, `McpServerOptions`.

Place with the capability/provider that consumes it; validate at startup.

### 6. External integration/provider DTO
Shape mirrors a third-party API/process.

Example: Cloudflare API/tunnel process response DTO.

Keep it inside provider-specific Infrastructure. Never treat provider DTOs as Domain Models.

### 7. Protocol/evidence envelope
A versioned system contract with explicit semantics, such as `C2CEnvelope` or execution evidence records.

Place where the protocol owner lives and version it explicitly.

## Naming rule

Avoid generic `*Model` unless the external framework requires that exact concept. Name by semantic role (`Request`, `Result`, `Options`, `Record`, `Envelope`, `Document`, `Dto`) only when the suffix clarifies the boundary.

Two classes with the same properties are not automatically the same type; semantic ownership wins over shape reuse.