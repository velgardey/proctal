# Overlay Reliability Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Reliable window transparency and an overlay-first workflow with streaming captions and synchronized settings.

**Architecture:** Keep the native WPF app; use WPF per-pixel transparency and whole-window opacity, create Setup lazily, and add native caption/editor controls using the existing controller. Improve settings notifications and audit the actual view bindings/native window state.

**Tech Stack:** C#/.NET 10 WPF, existing locked packages, WPF compositor and Win32 edge hit-testing.

**Spec:** docs/2026-11-10-overlay-reliability.md

## Global constraints

- Work in the authorized C:\Projects\proctal project; preserve private data and local dependencies.
- F10/F11/F12 and nonactivation remain; only explicit text/editor/dialog actions can take focus.
- No automatic audio capture at startup; Setup is configuration, overlay is daily use.
- Native transparency affects the whole window, independent of glass; stop claiming text stays fully opaque.
- Git index is inaccessible in this connection; retain a source archive instead of a commit.

## Review focus

- Native alpha survives resizing, show/hide, glass changes and clicking-style changes.
- Minimum-size/compact/Tune layouts retain readable code and reachable caption/actions.
- Settings updates in one view appear in other views, with correct persistence and request snapshots.
- Captions reconcile partial/final/source events and retain a manual reading position without activation.
- Setup-to-overlay handoff, repeated launch, typing, dialog ownership and shutdown retain correct lifecycle/privacy.

### Task 1: Opacity and synchronized preferences

Files: WindowAppearance.cs, OverlayWindow.xaml.cs, AzureSettings.cs, controller selections, regression tests and Diagnostics.cs.

- [x] Add failing native-alpha regression at .45/1 with glass false; add settings-event/snapshot/roundtrip tests.
- [x] Run focused tests and read failing results.
- [x] Enable WPF AllowsTransparency/Opacity and verify compositor state/rendered alpha; implement settings notifications without shared snapshot subscribers.
- [x] Run tests; native affinity/nonactivation/alpha checks must pass.

### Task 2: Overlay startup, captions and daily actions

Files: App.xaml.cs, OverlayWindow.xaml/.cs, DashboardWindow.xaml/.cs, new LiveCaptionView and QuestionEditorWindow, preferences/shortcuts/controller and tests.

- [x] Add failing saved-config startup and reconciled live-caption/reading-position/shortcut regressions.
- [x] Implement overlay-first startup, configuration handoff, launcher command and lazy Setup.
- [x] Add caption and listening/auto-answer/microphone controls, explicit question editor and keyboard paging.
- [x] Verify UI binding paths, saved options, normal/minimum/Tune/caption/attachment layouts and the full suite.

### Task 3: Reliability audit and delivery

- [x] Audit every visible setting/action against its binding/handler and runtime effect; fix reproducible defects with regressions.
- [x] Fresh whole-change review, one focused Important fix pass, green full suite and native checks.
- [x] Update guide/acceptance/ledger, regenerate portable and source ZIPs, validate private-data exclusion and launch.

Run commands from explicit C:\Projects\proctal: scripts/test.ps1, scripts/package.ps1, Proctal.exe --self-check work/ui-upgrade/native-reliability.json. Expected: all Release/cleanup tests green; all native checks except session-unavailable physical screenshot green. Export native previews and final deliverables to the chat outputs folder.
