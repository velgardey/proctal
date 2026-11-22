# Recent Session Context Implementation Plan

> Use superpowers:executing-plans for inline implementation and one fresh whole-change review.

Goal: give Luna the previous three completed question/answer pairs from the current app session.
Architecture: track IDs of successful current-session answers in ProctalController. Build bounded conversational reference from existing History, with no new persistence or API schema.
Tech stack: existing WPF/.NET 10 and Responses request builder.
Spec: docs/superpowers/specs/2026-11-13-session-context.md

Original November 13 constraints: three pairs, questions1200/answers4000 characters, keep head/tail when trimmed; exclude partial/failed/deleted/loaded/current turns; preserve storage preference, fast mode and controls; no new dependencies. November 21 audit supersedes the trimming constraint for the latest completed pair and explicit follow-up references: those are now sent in full. The older two pairs retain the bounds.

1. Write failing payload/controller tests for previous answer inclusion, chronological recency and bounded head/tail; also exercise failure/deletion/loaded history isolation and image/deep requests. Run targeted tests and observe feature failures.
2. Add session completion IDs and bound context construction in ProctalController; extend model instructions in ResponsesProtocol. Run targeted tests, full suite and native check, then a focused fresh reviewer.
3. Document behavior, package directly when Proctal is closed, refresh source/portable archives and update manifest against the saved current release, verify hashes/profile preservation. Preserve the dedicated implementation checkout (prior implementation is untracked; no commit/merge requested).
