# Proctal audit — November 21, 2026

This audit concentrates on reliability, data preservation and useful follow-up context. Four reproduced problems were fixed without adding dependencies, settings or shortcuts. The keyboard-first overlay, Azure deployments, opt-in history and capture controls retain their existing behavior.

## Fixed findings

| Priority | Problem and trigger | Change | Regression evidence |
| --- | --- | --- | --- |
| High | Importing multiple documents could update the visible list before a later import failed. Add/edit/remove could also change the list before a failed save. Visible and saved context could diverge. | Stage the whole import and enforce the document limit, then save before changing the collection. Add/edit/remove use the same save-first order. Local file/access errors have specific messages. | Failed batch import and failed add/edit/remove preserve existing documents and saved bytes. |
| High | Malformed history or entries with missing required fields could prevent loading and subsequent history saves, with a generic error. The existing catch-all already handled the load error; this was not a confirmed startup crash. | Move the unreadable file intact to a unique `answers.corrupt-<UTC timestamp>-<GUID>.json` backup and start an empty history list. Show a recovery notice. | Incomplete JSON, null entries and null questions preserve exact backup bytes and allow subsequent saving; controller shows the notice. |
| Medium | Trimming all previous answers could omit code in the middle of the latest solution. Explicit follow-ups could also truncate a selected long saved answer. | Include the entire latest completed pair and the entire displayed question/answer for Explain, Edge cases and Optimize. Keep the existing limits for the older two pairs. | Outgoing request contains the full latest question and middle code; selected old history is included in full. Older context remains bounded. |
| Medium | Refreshing devices after a saved headset/output disappeared could silently select the first listed device. Saving other settings could overwrite the intended selection. | Leave a missing saved device unselected, warn, preserve its ID on unrelated saves and validate selections before saving or connecting. Only a profile without a saved ID uses the first listed device. | Missing microphone fails before key access/network; unrelated saves preserve IDs; native refresh check uses unavailable IDs and restores the fixture afterward. |

All new failure cases were reproduced against the previous implementation before the fixes. The native baseline failed the added missing-device check. Backup files preserve damaged bytes for manual recovery; they do not reconstruct lost or never-saved sessions. History still retains up to 250 answer records when saving is enabled.

## Most useful next improvements

1. **Start a new practice session without restarting.** Add one explicit action that clears conversational/transcription state while retaining saved history, documents and configuration. This would help prevent unrelated mock interviews sharing assumptions. Current automatic context is limited to three completed pairs from this app launch. This is a feature opportunity, not a reproduced defect in the current session contract.
2. **Checkpoint streaming answers when history saving is enabled.** Partial text is currently saved after completion, cancellation or a handled interruption. A process crash or power loss during streaming can lose the current answer. Periodic atomic checkpoints could reduce that window without storing raw audio or enabling history by default.
3. **A short audio-source calibration flow.** Let users confirm output audio, microphone level and recognized text separately before practice. This is more valuable than replacing transcription blindly. Actual headset accuracy remains unresolved; the earlier microphone test was very faint, and the user chose to skip further retesting.
4. **Profile large Markdown answers before changing the renderer.** Streaming, rich code/math and unlimited app output need measurement with long examples. Measure UI-thread rendering time and scrolling, then reduce repeated parsing or render in chunks if needed. No performance bottleneck was reproduced in this audit.
5. **Avoid duplicate references on explicit follow-ups.** The selected answer can appear both in automatic recent context and the explicit reference. A focused request-builder regression would permit removing the duplicate while preserving selected historical answers. This is an input-cost opportunity, not a correctness fix in this release.

The initial audit left these items as recommendations. The subsequent audio/session reliability work implements streaming checkpoints and removes duplicate explicit references; it also expands earlier-session context and detects stalled transcription. See [session reliability](session-reliability.md) for current behavior and limits. Session reset and hardware calibration remain future work. No new controls were added to the overlay.

## Security and practical limits

Existing code uses Windows current-user encryption for the saved Azure key, normal TLS checks, Azure endpoint validation, disabled HTTP/WebSocket redirects and `store:false` for answer requests. Documents/history remain plain local JSON; raw audio and screenshots are not written to disk by the app. Azure retention/billing and Windows clipboard history are separate from these controls. These are implementation controls, not a certification or exhaustive security assessment.

Windows capture exclusion and nonactivating window behavior can be checked locally. They cannot establish universal invisibility or receiver-side behavior for every screen-sharing application. No process concealment, anti-monitoring or additional keystroke evasion was added. Verify the actual sharing app and mode before relying on capture exclusion.

Long full answer references can increase billed input, latency and the chance of exceeding the deployment context limit. The model still has finite context/output limits even though Proctal no longer sends an app output-token cap. The two older automatic pairs remain bounded. Request grounding and conversation references help with follow-ups; they cannot guarantee correct problem interpretation or code.

## Verification

- Baseline: 182 tests passed before the changes.
- Updated suite: 194 tests passed, zero failed or skipped, plus cleanup preview/scoping/data/source-preservation checks. Result: `work/test-results/core.trx`.
- Staged Release build succeeded. All 83 native checks passed, including keyboard routing, nonactivation, capture affinity, rich rendering and missing-device handling. Result: `work/verification/deep-audit/after-native.json`.
- Installation evidence, installed/staged binary hashes and before/after private-profile hash comparison are recorded in `work/verification/deep-audit/installation.json` by the release workflow. Archive/file checksums are recorded alongside the portable/source release bundles. Consult those receipts for installation/package results; the checks above describe the staged build.

No live audio was captured, no new Azure answer/transcription check was run and no receiver-side screen-sharing test or long hardware soak was performed in this audit. Synthetic controller payloads, temporary profiles and native rendering/interaction checks do not establish real interview accuracy.
