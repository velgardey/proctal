# Proctal implementation status

Updated 10 November 2026. Current architecture and research are documented in `design.md` and `assistant-ui-research.md`; UI tokens live in `../DESIGN.md`. Acceptance evidence and outstanding manual checks live in `acceptance-matrix.md`.

## Implemented and verified

- [x] Native WPF dashboard, dark overlay, tray lifecycle, F10 screenshot attachment, F11 submission, F12 visibility and optional clicking.
- [x] Project-local .NET 10.0.403 SDK, locked packages, portable app with .NET 10.0.14 runtime, source-preserving cleanup and complete-removal option.
- [x] Azure `gpt-6-luna` streamed text, no spoken answers, reasoning `none`, low verbosity, requested priority processing with fallback and accurate tier reporting.
- [x] Selected-output WASAPI audio and optional separate microphone, PCM16 mono 24 kHz conversion, VAD, live transcription configuration for `gpt-live-transcribe`, source/item reconciliation and bounded queues.
- [x] Pause drains committed transcription with a bounded wait and warns about incomplete captions. Automatic question assembly waits for queued speech and every outstanding interviewer transcription item.
- [x] PDF/DOCX/TXT/Markdown import, editable extracted text, role/job description/preferences, local context retrieval, mock questions and text feedback.
- [x] Optional atomic history, partial-answer preservation and coordinated deletion while answers stream.
- [x] F10 followed immediately by F11 waits for the captured image; failed answers retain attachments for retry.
- [x] Windows-account protected key and project-local profile. No global fonts, SDK installation, PATH changes, service or startup entry.
- [x] Embedded IBM Plex fonts, defined palette, native normal/minimum-size renders, explicit accessible field names and reviewed status contrast.
- [x] Rich Markdown/code, code-first answers, keyboard scrolling/resizing/opacity/zoom/history/follow-ups and saved glass layout.
- [x] Configurable coding response structure and trusted custom instructions, example insertion, saved preferences, retry/snapshot preservation and mock/connection-check isolation.
- [x] AltGr and hidden-overlay shortcut guards, measured wrapped code height, physical monitor placement and isolated diagnostics profiles.
- [x] Dark glass dashboard/overlay with separate Connection/Answers/Appearance pages and populated/minimum-size visual review.
- [x] WPF whole-window opacity with independent glass panels, native mouse edge hit-testing, lazy Setup and overlay-first startup, small explicit editor, caption paging/catch-up and synchronized audio/answer controls.
- [x] Release tests and cleanup checks; current results are in acceptance-matrix.md.

## Confirmed in the user's Windows session

- [x] Actual screenshot, capture affinity 0x11, keyboard-hook installation, read-only/clicking styles, window restoration and show/hide checks.
- [x] User saved the Azure key in Setup and reported the Luna/transcription connection check working.

## Remaining acceptance checks

These require the real meeting output, user's résumé and receiver's sharing view. They were not performed by this chat's restricted Windows connection.

- [ ] Run final `scripts/verify.ps1` from an ordinary unlocked Windows PowerShell session; this also sends a synthetic blue image to Luna.
- [ ] Import the résumé, inspect extracted text, and rehearse known/unknown questions to assess grounding and unsupported claims.
- [ ] Play representative interview audio through the selected output; check captions, technical vocabulary, trailing constraints, microphone separation and pause behavior.
- [ ] Rehearse physical F10/F11/F12 and clicking controls while another application has focus.
- [ ] Test the receiver's view in each intended sharing application and window/full-screen mode, monitor and display scaling.
- [ ] Check long/populated UI states, keyboard/screen-reader navigation, device removal/reconnection and a 60-minute rehearsal with measured first-text latency.

Windows capture exclusion is a supported API behavior; it cannot guarantee complete undetectability. No process masking, drivers, injection or general keystroke logging is implemented.
