# MCP Authorization Integration — Complete UC-MCP-02

## Goal

Protect the existing nine read-only MCP tools without moving business authorization logic into `McpServerConfigurator` or duplicating per-tool token parsing.

## Current tool surface

```text
workspace_info
read_file
list_directory
search_workspace
git_status
git_diff
execution_summary
test_status
execution_output
```

No new MCP capability is added in V0.5.

## Architecture

```text
HTTP /mcp
  -> authentication middleware / MCP auth challenge
  -> workspace/resource validation
  -> tool-scope requirement resolver
  -> existing MCP handler
  -> existing Core capability
```

Authentication is a Host concern. Scope names/policy mapping can be represented by small Core authorization contracts/constants if they are transport-independent.

## Scope requirements

```text
workspace_info      workspace.read
read_file           workspace.read
list_directory      workspace.read
search_workspace    workspace.search
git_status          git.read
git_diff            git.read
execution_summary   execution.read
test_status          execution.read
execution_output    execution.read
```

The tool handler must not be invoked when the required scope is absent.

## Protected Resource Metadata

Serve RFC 9728 metadata for the canonical MCP resource URI. Metadata must advertise only real authorization server(s) and supported operational scopes.

Do not advertise `offline_access` as a protected-resource requirement. Refresh capability belongs to authorization-server/client semantics.

## Challenges

For missing/invalid credentials, return HTTP 401 with a Bearer challenge containing `resource_metadata` pointing to the protected-resource metadata document.

For authenticated callers with insufficient scope, return HTTP 403 with `error="insufficient_scope"`, the minimum required scope(s), and `resource_metadata`.

Never return local paths, token values, stack traces or raw exception messages in auth errors.

## Local-only development profile

A deliberate local test profile may bypass remote OAuth for loopback development, but:

- it is explicit configuration;
- it cannot be automatically selected merely because token validation failed;
- it cannot be used on a public/tunnel endpoint;
- integration tests prove public profile fails closed if auth is misconfigured.

## Metadata/discovery integration tests

Test:

- protected-resource metadata shape/resource value;
- authorization-server metadata discovery;
- `iss` behavior required by selected profile;
- supported scopes are accurate;
- refresh/offline metadata is accurate;
- no DCR/CIMD capability is advertised before implementation is enabled;
- 401 challenge points to correct metadata;
- 403 insufficient-scope challenge names exact required scope;
- every one of nine tools has allow/deny coverage.

## Service invocation guard

For each tool, add a test seam/fake proving authorization failure occurs before the Core service call. This is part of UC-MCP-02 acceptance, not optional adapter polish.
