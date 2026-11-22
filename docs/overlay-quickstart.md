# Proctal — updated overlay

The installed build is in C:\Projects\proctal\dist\Proctal.Windows. Run Proctal.exe or Launch-Proctal.cmd. Setup hides after Launch overlay; later launches open the overlay directly. Reopening the launcher reveals the existing overlay. Audio always starts paused.

Your saved Azure key, résumé and settings remain in the project's .data folder. They are excluded from the downloadable app and source archives.

## Daily controls

| Shortcut | Action |
| --- | --- |
| F12 | Show / hide overlay |
| F10 | Capture monitor under pointer and attach screenshot |
| F11 | Answer stored transcription / typed question immediately; close editor |
| F9 / double F11 | Answer attached images; double-press within 500 ms |
| F8 / Ctrl+Shift+P | Start / pause speech transcription |
| Ctrl+Shift+I | Switch mouse controls |
| Ctrl+Alt+Q | Open small question / response editor |
| Ctrl+Alt+S | Answer screenshots; waits for capture only in this action |
| Ctrl+Alt+V | Open / close More |
| Ctrl+Alt+[ / ] | Window opacity down / up |
| Ctrl+Alt+− / + | Answer text size |
| Ctrl+Alt+Up / Down | Scroll answer |
| Ctrl+Alt+PageUp / PageDown | Page answer |
| Ctrl+Alt+Home / End | Answer top / follow latest |
| Ctrl+Alt+Shift+PageUp / PageDown | Page captions |
| Ctrl+Alt+Shift+Home / End | Caption top / follow latest |
| Ctrl+Alt+Shift+arrows | Resize |
| Ctrl+Shift+arrows | Move |
| Ctrl+Alt+T | Show / hide live captions |
| Ctrl+Alt+A / B / P | Auto answer / always on top / pin answer |
| Ctrl+Alt+M / W / F / G | Compact / wrap code / follow / glass |
| Ctrl+Alt+N / R | Practice question / review typed response |
| Ctrl+Alt+C | Copy code |
| Ctrl+Alt+Left / Right / L | Browse history / return live |
| Ctrl+Alt+1 / 2 / 3 | Explain / edge cases / optimize |
| Ctrl+Alt+Backspace / Escape | Clear images / cancel answer |
| Ctrl+Alt+E / 0 | Export Markdown / reset layout |

Normal navigation and adjustment shortcuts retain focus in your editor. Setup and the question editor take focus when explicitly opened. Enable mouse controls for clicking or edge resizing; mouse mode also retains overlay nonactivation.

## Opacity and captions

More and Setup → Appearance share saved preferences. Window opacity fades the entire overlay, including text and code. Glass surface separately makes panels translucent. Set opacity to 100% and turn Glass surface off for a solid window. The overlay uses per-pixel transparency; it does not apply native acrylic blur. Setup keeps acrylic where Windows supports it.

Start listening from the overlay. Live captions show provisional and final words with interviewer / microphone source labels. Paging preserves your reading position as new text arrives; Caption End resumes following. Captions and partial answers update after hiding and reopening the overlay. Reading holds surviving caption text when old turns roll off or provisional words become final. Caption height and visibility are configurable. On smaller windows, caption height shrinks to preserve a readable answer viewport; the preferred caption height remains saved. More scrolls within its own menu instead of enlarging the window.

Quiet audio and word beginnings are transmitted without a local volume cutoff. Speech activity only segments turns; a 25-second buffer limit and pause-time flush retain soft utterances. Partial captions also hold automatic answers while soft speech is still in progress. This does not make F11 wait for a final caption. Continuous captured audio can increase billable audio, including silence; pause with F8 when no longer needed. Select your actual headset microphone in Setup → Audio; the laptop microphone array is a separate device.

Auto answer and microphone choices persist without saving incomplete Setup edits. Pause before changing output devices or microphone capture. Hiding the overlay does not pause audio. Pin holds the answer against new automatic questions; F11 still submits manually.

## Configuration and coding answers

Use Setup for Azure connection, devices, résumé/context, role/company and answer instructions. Deployments remain gpt-6-luna and gpt-live-transcribe. Custom answer instructions and coding presets control response structure. Reasoning none, low verbosity and streaming remain defaults; Fast processing requests priority and falls back when Azure rejects it.

## Immediate answers and daily conveniences

Answer / F11 immediately sends all stored transcription from both speakers (up to 300 retained turns), including partial captions. It works with microphone-only text and while listening is paused; it does not wait for new interviewer speech, final captions, audio drain or screenshot capture. Typed text takes priority as the question, followed by the available interviewer question, current practice question, or latest stored turn. Later captions do not change the submitted snapshot. Press F11 again after new captions arrive to send updated context; an unchanged streaming request stays running. Only words already received as transcription are included. F10 attaches images; use F9, double F11, Answer images or Ctrl+Alt+S to submit them. F9 goes directly to images. Double F11 answers speech/text on the first press and replaces it with the image request on the second press within 500 ms. Holding F11 does not trigger images. Setup focuses on configuration; image answering stays in the overlay.

The primary action stays visible in compact mode and becomes Stop while streaming. Identical repeated requests keep the existing stream. The question attached to an answer stays stable as new speech arrives. New automatic questions wait for the current answer to finish, and manually answered speech is not folded into a later question. Pausing retains final words that arrive while audio drains; repeated speech answers retain the whole question and its updated continuation.

Text appears as soon as it arrives, with later updates batched to keep keyboard input and scrolling responsive. Azure rate-limit retries show progress. Cancel connection stops audio startup; Clear images also discards pending captures. End resumes following. Opacity, text size, wrap and follow changes retain layout and window position. Cloud latency still depends on Azure.

## Verification and cleanup

The updated build passed 154 Release tests and cleanup checks. All 71 local Windows checks passed, including screen capture, hover popup prevention, F8 and image shortcuts. The user previously passed normal-session capture and reported Azure connections working. Final real audio, answer quality, desktop transparency, mixed-DPI monitors, receiver-side sharing and a long rehearsal still require your Windows session.

Close Proctal and run:
& 'C:\Projects\proctal\scripts\verify.ps1'

For offline checks, add -SkipAzure. This still requires an ordinary unlocked desktop for screen capture. Capture exclusion is Windows best-effort support, not a complete invisibility guarantee.

Exit from the tray before cleanup. Preview:
& 'C:\Projects\proctal\scripts\cleanup.ps1' -Mode All -WhatIf

All removes local SDK, caches, builds and profile while preserving source. -RemoveProject -WhatIf previews complete project removal. Remove -WhatIf to perform the selected cleanup. No global dependency changes were made. Portable copies outside the project must be removed separately.


## Simplified glass layout

The slim toolbar keeps Listen, Answer/F11, Capture/F10, Chat, More and Hide. Answer becomes Stop during streaming. Chat opens the small editor and deliberately takes focus; navigation/scrolling otherwise stays in your foreground app. More (··· or Ctrl+Alt+V) holds secondary controls and Setup. It overlays part of the answer while open without resizing the window; close it with the same control/shortcut.

Captions start as a short rail. Click its chevron to expand/collapse; caption reading shortcuts expand it automatically. Image thumbnails and Answer images appear only with attachments/pending capture. Advanced settings, including saved caption height and size presets, live in Setup → Appearance.

The new overlay uses neutral charcoal translucency and the same embedded fonts. It has no native backdrop blur. Keys remain Windows-account protected, but stored context/history are plain JSON. HTTP answer requests and transcription handshakes now refuse redirects to protect api-key headers. The product/security comparison lists other products' public claims separately from Proctal's verified controls.


## Hover privacy

Keep Capture exclusion enabled in Setup. Proctal suppresses floating hover tooltips for the overlay, Setup and question editor, so hovering does not create a separate tooltip window. Controls retain their labels and keyboard shortcuts. Enabling exclusion also closes existing tracked tooltips. Verify hover behavior on the receiving screen in your actual sharing app; Windows capture exclusion has compatibility limits.


F8 starts or pauses speech transcription, including while the overlay is hidden. During connection setup, F8 cancels the connection. Holding the key toggles only once; press and release it again to toggle back. Hiding with F12 does not stop listening.


Custom prompts: More (Ctrl+Alt+V) → Setup → Answers → Custom response instructions → Save changes. The default coding structure is Problem → brute force → optimize, with concise explanations and small examples; custom instructions take priority over that structure.

## History, math and F7

More → History or Ctrl+Alt+H opens dated, searchable questions and answers. Ctrl+Alt+Left/Right browses in the overlay and Ctrl+Alt+L returns live. History → Save answer history locally for future sessions, then Save changes, retains future answers across launches. Saving stays off unless you enable it; earlier unsaved sessions cannot be recovered.

F7 makes one Luna xhigh request: attached/pending images, then typed/fresh speech, otherwise the displayed question. Normal F11/F9 behavior and saved reasoning are unchanged. F8 remains speech on/off. Answers use the Azure model output limit with no app token cap. Xhigh has up to five minutes to start returning text, followed by a 90-second text-stream idle timeout; Ctrl+Alt+Escape cancels.

Code supports highlighted programming fences, wrap, selection and code-only copy. Dollar delimiters, TeX parentheses/brackets and math/latex/tex fences render equations, fractions, sums and supported matrices. Wide display formulas scroll horizontally. Unsupported or incomplete TeX stays readable. Original Markdown and backslashes survive copying/export/history. Details: docs/math-deep-history.md.

Follow-ups automatically use recent pairs, a compact index of earlier completed exchanges from this app launch and relevant earlier excerpts. F8 pauses/restarts preserve session memory. All answer routes snapshot both transcription sources. Deleted, partial, superseded and previously loaded answers are excluded from automatic memory. Explicit selected-history follow-ups remain available and avoid duplicate references. Very long references and session input use labelled omissions; full retained answers remain in History. See docs/session-reliability.md for exact limits. Saving stays opt-in, with streamed partial checkpoints when enabled.

If a saved headset/output device is unavailable, pause, open Setup → Audio, refresh and select your current device before listening. Proctal does not silently choose another device. Saving unrelated options preserves unavailable saved device IDs.

Unreadable saved history is preserved intact in `.data/answers.corrupt-<timestamp>-<id>.json`, and a status notice identifies the backup. New answers can be saved afterward. This does not reconstruct missing answers; keep the backup for manual recovery. Context import/edit/remove operations also leave the existing visible list unchanged if saving fails.
