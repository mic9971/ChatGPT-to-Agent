# Documentation Rules

- One implementation-manageable behavior = one UC file.
- Reusable behavior appearing in 2+ UCs becomes a Business Rule (`BR-*`) in `docs/02-common`.
- UC documents reference BR IDs; do not fork/copy common rule text.
- Architecture documents explain system-wide structure; they do not become task checklists.
- Every UC owns acceptance criteria and test expectations.
- Every public contract/error/state mentioned in a UC must be traceable to a common contract or architecture document.
- If implementation intentionally changes an approved UC/BR, update design first or record an ADR; never silently drift docs after code.
- Status is updated in both the UC front matter/table and the catalog.
