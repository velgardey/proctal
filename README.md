# Proctal

A native Windows 11 x64 interview-practice companion. It transcribes selected interview audio, uses your résumé and context, and streams text answers from your Azure `gpt-6-luna` or optional `gpt-6.1-sol` deployment. It produces no spoken answers.

## Download Windows

Download **[Proctal.Windows.zip from the latest release](https://github.com/velgardey/proctal/releases/latest)** and extract the entire ZIP to a writable local folder. Run `Launch-Proctal.cmd` or `Proctal.exe`. The ZIP includes the .NET runtime; no administrator access or global runtime installation is needed. Enter your own Azure endpoint, deployment names and key in Setup. The optional Sol deployment uses the same resource and key.

Each release includes `Proctal.Windows.sha256`. Verify the ZIP with `Get-FileHash .\Proctal.Windows.zip -Algorithm SHA256` before extracting. This build is unsigned, so Windows may show a reputation prompt. Use it for mock interviews and other permitted practice.

The public repository and release contain no user keys, imported documents or session history. Moving your personal profile is a separate step; Windows-protected keys must be entered again on another PC.

## Start

Double-click `C:\Projects\proctal\dist\Proctal.Windows\Proctal.exe`, or use the portable `Launch-Proctal.cmd` shortcut. The launcher returns immediately. The portable folder includes its own .NET runtime; no global .NET installation or administrator access is needed.

1. Open **Setup**. Confirm your Azure resource endpoint and deployment names: `gpt-6-luna` and `gpt-live-transcribe`. Add a key or launch with `AZURE_OPENAI_API_KEY` set. A stored key is protected with Windows DPAPI for your account.
2. Open **Context**, import your résumé, and inspect/edit the extracted text. Paste text for scanned documents. Add the role, job description, and answer preferences in Setup → Answers. Save changes.
3. Choose the output device actually playing interview audio. Turn on microphone capture only for your own practice responses. Click **Launch overlay** to save settings. Setup hides; later launches open the overlay directly unless you disable that option. Listening always starts paused. Use **Start listening** in the overlay and check its streaming captions.
4. Use the overlay or type a question. **F10** captures the monitor under the pointer and attaches an image; **F9** (or double **F11**) answers attached images. **Answer/F11** immediately submits stored transcription from both speakers, including partial captions; a typed question takes priority. Up to **50 images** can be attached, matching the [Azure Responses API limit](https://learn.microsoft.com/azure/foundry/openai/how-to/responses#image-input). Images are resized to at most 2560 pixels and 5 MB each for responsiveness; larger batches use more input tokens and may take longer. Failed image requests retain them for retry. Clearing images also discards a pending capture.

## Controls

| Shortcut | Action |
| --- | --- |
| F6 | Answer with Sol (high reasoning) using attached images, fresh text/speech, or the displayed question |
| F7 | Answer with Luna xhigh reasoning for this request |
| F12 / Ctrl+Shift+Space | Show or hide overlay |
| F10 | Capture the monitor under the pointer; temporarily hide Proctal's windows |
| F11 | Answer typed text / stored transcription immediately |
| F9 / double F11 | Answer attached images; double-press within 500 ms |
| Ctrl+Shift+I | Switch between clicking and read-only overlay |
| F8 / Ctrl+Shift+P | Start / pause speech transcription |
| Ctrl+Shift+C | Copy current answer |
| Ctrl+Shift+arrows | Move overlay |
| Ctrl+Alt+Up / Down | Scroll answer; hold to repeat |
| Ctrl+Alt+PageUp / PageDown | Page through answer |
| Ctrl+Alt+Home / End | Top / end; End resumes streaming follow |
| Ctrl+Alt+Shift+arrows | Resize overlay |
| Ctrl+Alt+[ / ] | Decrease / increase window opacity |
| Ctrl+Alt+− / + | Decrease / increase answer text size |
| Ctrl+Alt+C | Copy first code block without fences |
| Ctrl+Alt+M / W / F / G | Compact / wrap code / follow / glass |
| Ctrl+Alt+Left / Right | Previous / next answer |
| Ctrl+Alt+L | Return to live answer |
| Ctrl+Alt+1 / 2 / 3 | Explain / edge cases / optimize |
| Ctrl+Alt+Q | Open small question editor |
| Ctrl+Alt+E | Export displayed answer as Markdown |
| Ctrl+Alt+S | Answer attached images; wait for explicit capture if needed |
| Ctrl+Alt+V | Open / close More |
| Ctrl+Alt+T | Show / hide captions |
| Ctrl+Alt+Shift+PageUp / PageDown | Page through captions |
| Ctrl+Alt+Shift+Home / End | Caption top / follow latest |
| Ctrl+Alt+A / B / P | Auto answer / topmost / pin answer |
| Ctrl+Alt+N / R | Practice question / review typed response |
| Ctrl+Alt+Backspace / Escape | Clear images / cancel response |
| Ctrl+Alt+0 | Reset overlay layout |

The overlay starts in keyboard mode and stays nonactivating, including when mouse controls are enabled. Type through the explicitly requested question editor with Ctrl+Alt+Q. Enable mouse controls only to drag or use buttons. Scroll, resize and adjust appearance through shortcuts. The tray menu opens Setup, toggles visibility, pauses listening, or exits. Hiding a window does not pause audio. Pin prevents automatic questions from replacing an answer; manual submission still works. Repeated clicks on an already-streaming identical request do not restart it. F6/F7/F8/F9/F10/F11/F12 remain reserved while Proctal runs. Overlay navigation/copy commands apply when it is visible; AltGr typing is ignored by overlay command recognition.

Pausing waits up to four seconds for committed transcription to finish after audio has drained. If final captions do not arrive, Proctal reports that the last words may be missing. Automatic answers wait for pending interviewer audio and all outstanding transcription items. F11 immediately snapshots all retained transcription (up to 300 turns) from both speakers, including partial captions, without waiting for new speech, final captions, audio drain or screenshot capture. The primary question uses typed text first, then the available interviewer question, the current practice question, or the latest stored turn. It works while listening is paused and when only microphone text is stored. Later captions do not alter a request already submitted; another F11 press submits updated context, while an unchanged streaming request stays running. Only transcription already received can be included. Images stay attached until you explicitly answer images with F9, double F11 or Ctrl+Alt+S. Double F11 starts a speech/text answer immediately on the first press and replaces it with the image request on the second; use F9 to go directly to images. Held keys never trigger double-presses.

## Appearance and responses

Overlay **More (···)** offers quick opacity/text size/compact controls; Setup → Appearance keeps full presets, wrapping, follow, topmost and caption preferences. Window opacity fades the entire overlay, including text; 100% is solid with Glass surface off. Glass surface makes the charcoal panels translucent. The overlay uses WPF per-pixel transparency, not native acrylic blur. Setup retains Windows acrylic where supported. Mouse mode supports edge resizing, and keyboard resizing works without activation. Placement uses physical monitor coordinates; rehearse mixed-DPI hardware.

Live captions show interviewer and optional microphone sources, including provisional words. Use Ctrl+Alt+T to show/hide captions and Ctrl+Alt+Shift+PageUp/PageDown to browse them; Ctrl+Alt+Shift+End follows the latest words. Captions and partial answers catch up after hiding/showing. Manual caption reading anchors surviving text as older turns roll off. On smaller windows, captions shrink to protect the answer viewport without changing the saved preferred height. More scrolls within the answer region. Auto answer and microphone choices persist independently from unfinished Setup drafts. Pause before changing audio devices or microphone capture. Q opens a small explicit editor; F11 submits and closes it. Repeated launches reveal the existing overlay.

Audio capture sends every captured frame, including quiet speech and word beginnings, to the configured Azure transcription deployment. Local speech activity controls turn boundaries rather than discarding low-volume audio. Turns are committed after detected pauses or at most 25 seconds of buffered audio; pausing flushes remaining audio, including soft utterances. Incoming partial captions also keep automatic answers from firing before an ongoing soft utterance finishes. F11 still submits received captions immediately. Continuous input can use more billable audio than volume-gated capture, especially during silence; use F8 to pause when finished. For headset input, select the headset microphone rather than the separate laptop microphone array.

Transcription hints prioritize common coding phrases, then terminology from your target role/job description and imported context. This includes phrases such as hash map, left join and sigmoid. Pause and restart listening to apply updated context or terminology hints.

The dark glass dashboard and overlay use embedded IBM Plex fonts. Setup → Answers controls coding language, code-first responses, reasoning, Fast requests and output limits. The default coding response explains the problem, shows the obvious brute-force approach, identifies its bottleneck, and builds toward optimized code with small examples and concise complexity/edge cases. Native Markdown supports headings, lists, tables, quotes, inline code and colored fenced code. Copy buttons remove the fences. Long answers scroll; scrolling upward pauses streaming follow and End resumes it. History is searchable and renders the same Markdown.

## Custom response instructions

Setup → Answers shows Answer mode, Response structure, and the editable Initial prompt without expanding Model options. Start has an Answer mode & prompt link. Insight → solution explains the approach, complete code, important variables and operations, a worked example, complexity and a short spoken explanation. Proctal sends no output-token cap; Azure model limits and quotas still apply. The response structures are Code first, Insight → solution, and Problem → brute force → optimize. The teaching preset is the default for new profiles. It covers problem/constraints, the obvious correct approach with code and complexity, bottleneck and intuitive optimization with a tiny worked example, optimized code, and a short input/output example with important edge cases—all concisely.

Use the Custom response instructions field for your own section order, tone or detail. Custom instructions override the selected preset's format; use Save changes to retain them. They are sent as trusted instructions separately from résumé/job/transcript references, survive Fast-tier fallback, and do not change an answer already streaming. Mock-interviewer question generation and connection checks retain their own tasks. See docs/response-instructions.md.

## Fast mode

Default Luna requests use `reasoning.effort: none`, low verbosity, and streamed text. This minimizes reasoning latency. Setup also requests Fast processing with `service_tier: priority`; Azure may reject that option. Proctal retries without it and displays the returned tier when available. **Fast requested** is not confirmation of Azure Fast capacity. Choose `low` reasoning for questions where accuracy needs more consideration. Latency shown is measured from submission to first non-whitespace text; no speed guarantee is made.

## Screen sharing and data

Capture exclusion uses Windows `WDA_EXCLUDEFROMCAPTURE` (`0x11`) on Proctal's owned windows. Its status is shown in Setup. Test the actual receiver's view for each meeting app, window/full-screen share mode, monitor, and display scaling. The API does not make the process, UI Automation, keyboard hook, or keystrokes invisible to Windows. It cannot guarantee complete undetectability.

Proctal uses ordinary keyboard controls, without general keystroke logging, process masking, drivers, or injection. Physical shortcuts are recognized; injected keyboard events are ignored. Use for practice and sessions where assistance is permitted.

Documents, settings, and optional answer history live in the project's `.data` directory. If the portable folder is moved outside the source project, data lives beside its executable in `.data`. Raw audio and screenshots are not written to disk by the app. Audio is sent to your Azure transcription endpoint while listening. Relevant document extracts, recent captions, questions, and explicitly submitted images go to Luna. History saving is off by default; removing a source document does not remove text already included in saved history. Cloud retention and billing follow your Azure configuration. Copy uses the Windows clipboard.

## Build and verify

Run scripts from PowerShell; paths with spaces must be quoted. All SDK, package caches, CLI-home and temporary files stay within Proctal. No global PATH, services, drivers, or startup entries are installed.

```powershell
& 'C:\Projects\proctal\scripts\setup.ps1'
& 'C:\Projects\proctal\scripts\restore.ps1'
& 'C:\Projects\proctal\scripts\test.ps1'
& 'C:\Projects\proctal\scripts\package.ps1'
& 'C:\Projects\proctal\scripts\verify.ps1'
```

Close Proctal before packaging or verification. Verification briefly opens the app, tests native window controls/capture/image attachment, and sends a synthetic blue image plus a short harmless question to Azure. It connects to live transcription without sending microphone audio. Results and UI previews are in `work\verification`. Use `-SkipAzure` for offline checks. To check connections without screen capture: `Proctal.exe --check-azure "C:\Projects\proctal\work\azure-check.json"`.

The current desktop automation session lacks screen access and outbound connectivity. Its partial native checks and unit-test evidence do not establish live microphone transcription or receiver-side sharing. See `docs\acceptance-matrix.md`.

## Cleanup

Exit Proctal first. Preview every cleanup with `-WhatIf`.

```powershell
# Remove build outputs and cached dependencies; preserve SDK, source and profile.
& 'C:\Projects\proctal\scripts\cleanup.ps1' -Mode Build -WhatIf
# Remove SDK plus builds/caches; preserve source and profile.
& 'C:\Projects\proctal\scripts\cleanup.ps1' -Mode Toolchain -WhatIf
# Remove local keys, documents, settings and history.
& 'C:\Projects\proctal\scripts\cleanup.ps1' -Mode Data -WhatIf
# Remove SDK, packages, builds and local profile; preserve source.
& 'C:\Projects\proctal\scripts\cleanup.ps1' -Mode All -WhatIf
# Remove the complete project, including source and Git history.
& 'C:\Projects\proctal\scripts\cleanup.ps1' -RemoveProject -WhatIf
```

Remove `-WhatIf` to carry out the chosen cleanup; PowerShell asks for confirmation. The script checks the project marker and path boundaries and refuses linked folders/files. It never removes global dependencies. Rebuild after Build cleanup by running restore then package; run setup first after Toolchain/All cleanup. Copies of the portable folder outside this project must be removed separately. Secure erasure of SSD backups or cloud data is not provided.

## Troubleshooting

If a headset changes/disconnects, pause, refresh devices, select the new device and restart; automatic device recovery is deliberately not attempted. A saved device that is no longer available stays unselected and prevents listening until you choose a current device. Saving unrelated settings preserves that saved device ID. If Azure rejects transcription settings, check the deployment and the actual model behind it: a deployment name alone cannot prove compatibility. Typed questions and image attachments remain usable when audio fails. Connection errors show sanitized messages and do not print keys. This unsigned personal build may trigger Windows reputation checks; no system protections are changed.



The primary action stays visible in compact mode and becomes Stop while streaming. The input hint identifies typed text, stored transcription or the practice question. New captions do not rename the question for an answer already streaming. First text is published immediately; later UI updates are batched, with final text always flushed. End resumes following even if you previously disabled follow. Connecting audio has a Cancel connection control. Opacity/font/wrap/follow updates avoid unrelated layout rebuilds.

Speech input retains compound questions across repeat submissions, reconciles final and partial continuations, and ignores older unfinished captions once a newer question is ready. Pause retains drain-time finals. Automatic questions wait until the active answer finishes, without repeating manually answered fragments. Save & launch hides Setup without discarding attachments; F9 or double F11 answers images in the overlay.

## Focused glass overlay

The default overlay has one slim action bar, a compact expandable caption rail and a larger answer surface. Listen, Answer/F11, Capture/F10, Chat, More and Hide stay within one row. Answer becomes Stop while streaming; F11 still submits speech/text. More (Ctrl+Alt+V) groups quick preferences, follow-ups, history, export, practice and Setup; it stays inside the native window and preserves saved size/position. Advanced appearance/audio/model options stay in Setup. Attachments and Answer images appear only when there is an image or unfinished capture.

Click the caption chevron to expand/collapse. Keyboard caption navigation expands the rail automatically. Opacity affects the whole overlay. Neutral charcoal surfaces and embedded IBM Plex replace the overlay's older slate/amber chrome; Setup now shares a charcoal glass palette, with a grouped Azure form and persistent Save & launch action. Glass is translucency, with no native blur in the overlay.

Security review also blocked automatic HTTP/WebSocket redirects that could forward Azure api-key headers. Production retains normal TLS verification and Azure-host validation. Documents/history remain plain local files, while keys use Windows account encryption. Proctal has no enterprise certification, process concealment or complete invisibility guarantee. See docs/2026-11-10-security-feature-comparison.md for the cited product comparison and remaining gaps.

Capture-excluded Proctal windows suppress floating hover tooltips, including the glass shell, controls and inline Markdown. Enabling exclusion closes existing tracked tooltips. Keyboard labels and normal controls remain usable. This prevents Proctal hover help from opening a separate unprotected window; verify the actual sharing receiver.

## History, equations and deep answers

More → History or Ctrl+Alt+H opens the searchable question/answer list. Enable History → Save answer history locally for future sessions, then Save changes to retain future answers across launches; earlier unsaved sessions cannot be recovered. F7 requests one Luna xhigh answer using attached images, fresh text/speech, or the displayed history question. F8 keeps toggling speech and F11 remains fast. Equations render natively with dollar/TeX delimiters and math fences; programming code and raw Markdown exports are preserved. See docs/math-deep-history.md for limits and controls.

Luna uses the current app session's completed exchanges: a compact chronological question/answer index for earlier exchanges, four retrieved earlier excerpts and the last three pairs with richer detail. The latest pair retains up to 12,000 question and 80,000 answer characters; the other two retain up to 1200/4000. Earlier index excerpts adapt to session length. All answer routes snapshot stored transcription from both sources; F11 still submits immediately without waiting for more audio. Interrupted, cancelled, superseded, deleted and previously loaded answers are excluded from automatic session memory. Explicit Explain, Edge cases and Optimize references are included once, including selected saved history. References retain up to 60,000 characters; combined conversational input has a 160,000-character safeguard, with labelled middle omissions when necessary. Full retained answers remain available in History. Output-token limits are unchanged. Longer context increases input usage and can increase latency. An app launch defines a session; F8 pauses/restarts preserve it. See [session reliability](docs/session-reliability.md).

Context imports and edits are saved before the visible document list changes; failed saves leave the existing list intact. Unreadable answer history is preserved in `.data/answers.corrupt-<timestamp>-<id>.json`, with a notice, so new history can be saved. This preserves damaged bytes for manual recovery; it does not reconstruct lost answers. See [the November 21 audit](docs/2026-11-21-deep-audit.md) for fixes, verification and recommended next improvements.

When history saving is enabled, the first streamed text and subsequent text at five-second intervals are saved as partial checkpoints, then replaced by the final record. Saving stays off by default. Stalled audio sends time out after ten seconds; committed transcription that remains unfinished for sixty seconds raises a fault and pauses listening. WebSocket ping/pong detects unresponsive connections. Stored captions and completed answers survive these faults; use F8 to restart and check the captions for gaps. `--check-azure <report-path> --session` runs a synthetic multi-question recall/topic-scope check and transcription handshake without microphone audio or private context.

## Release pipeline

The Windows workflow restores locked dependencies, runs the test and cleanup suite, builds a fresh portable ZIP and verifies its contents and SHA256. Main pushes and pull requests produce build artifacts. Pushing a version tag such as v0.1.0 publishes the Windows ZIP and checksum under GitHub Releases. Cloud and real-audio checks use a local configured resource; CI needs no Azure credentials. Actions are pinned, and only the tag-release job has repository write permission.
