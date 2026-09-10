# Testing Reference

Test observable behavior and invariants.

Unit-test especially:

- path canonicalization/containment;
- sensitive policy;
- protocol transitions;
- idempotency/leases;
- token/pairing expiry;
- sanitization/cursors/limits.

Integration-test:

- ASP.NET/MCP endpoints with `WebApplicationFactory`;
- real temporary Git repository adapters;
- auth discovery/token flows;
- CLI application service with fake process/tunnel adapters.

Testing principles:

- use `TimeProvider` rather than sleeps;
- temp dirs are created per test and cleaned;
- no dependency on developer machine home or network in ordinary test run;
- every security bug gets a regression test first/with fix;
- failure-path assertions include stable error code, not fragile full prose.
