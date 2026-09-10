# Use Case Implementation Rules

For one target UC:

1. Read metadata/dependencies/BRs.
2. Inspect current code and tests.
3. State exact file scope and non-goals.
4. Implement application contract/policy first; adapters stay thin.
5. Add unit tests for rules/state and integration tests for actual boundary.
6. Run focused tests/build; then broader suite only when warranted.
7. Review diff against UC acceptance checklist.
8. Do not start the next UC in the same change unless explicitly approved.
