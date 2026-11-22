# Native math, deep answers and history implementation

Spec: docs/superpowers/specs/2026-11-13-math-deep-history.md

1. Write failing native Markdown tests for rendered math, incomplete/malformed fallback and unchanged programming code. Add locked WpfMath 2.1.0, native cached vector rendering, math delimiter normalization outside code, notices. Run targeted tests.
2. Write failing protocol/shortcut/controller tests. Add F7 deep action with request snapshot, reasoning-aware duplicate identity, token allowance, timeout and explicit Azure errors. Add History action, More buttons and Ctrl+Alt+H. Run targeted tests.
3. Add native diagnostics and documentation. Run suite/cleanup, native checks/visual inspection and synthetic Azure xhigh check. Use a fresh whole-change reviewer, address important findings and verify again. Package when app is closed; refresh source/portable exports and matching manifests without touching profile data.

Global constraints: use project-local .NET/cache; no global tool installs. Existing project is on implementation branch with all files untracked from earlier work; snapshot current source before editing rather than commit unrelated prior work. Keep persistence opt-in. No universal capture invisibility claims.
