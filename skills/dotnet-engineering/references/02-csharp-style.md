# C# 14 Style Reference

Follow Microsoft/.NET conventions and the repository `.editorconfig`.

- PascalCase public members/types; camelCase locals/parameters; `_camelCase` private fields if repository convention uses it.
- One type per file when it improves discoverability; small closely-related private/nested types are exceptions.
- Prefer expression clarity over clever language features.
- C# 14 extension members, `field`, Span conversions, etc. are allowed but not required.
- Use `var` when type is evident and readability improves; explicit type when it conveys important information.
- Prefer guard clauses for invalid inputs/security boundaries.
- Keep methods small enough that error/cancellation/security behavior is obvious.
- Use `required`/constructors intentionally; never use null-forgiving `!` to hide design problems without proof.
- Nullable warnings are design feedback; do not blanket-disable them.
- Use `readonly`/immutability where it makes state reasoning easier.
