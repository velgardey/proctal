# Windows interview practice assistant: research and proposed design

Prepared 9 November 2026. Research and original design rationale. Implementation is now in `C:\Projects\proctal`; current UI tokens are in `DESIGN.md`, usage in `README.md`, and observed evidence in `docs/acceptance-matrix.md`. Statements below about future implementation are the original proposal, not claims that every acceptance check passed.

## Agreed scope

A Windows practice assistant that listens to interview audio and displays useful answers grounded in the user's resume and supporting material. F12 shows or hides the overlay. All generated answers and feedback are text; there is no synthesized voice or audio playback. Use the existing Azure resource and the confirmed `gpt-6-luna` deployment. Include both coached rehearsal and independent mock practice.

## What the comparable products actually document

| Product | Documented workflow | Useful behavior to reproduce |
| --- | --- | --- |
| Cluely | A small session widget, keyboard-triggered assistance, suggested responses, follow-up questions, recap, separate dashboard, and custom modes. | Compact movable overlay; separate setup/history; manual assistance alongside conversation context. |
| Parakeet AI | Session setup with a resume, job description, documents, instructions, live transcription, automatic or manual answers, screenshots, and saved transcripts. | Context setup before practice; answer formats; optional automatic question detection; question and answer history. |
| Interview Coder | Real-time audio transcription, technical assistance, and an overlay that can allow mouse clicks through it. | Read-only overlay mode; coding explanations and complexity; explicit interaction mode. |

Sources: [Cluely usage guide](https://support.cluely.com/en/articles/12294306-how-to-use-cluely), [Parakeet product and FAQ](https://www.parakeet-ai.com/), [Interview Coder](https://www.interviewcoder.co/). These are product disclosures, not independent performance benchmarks or evidence of their private implementation.

### Cluely: product behavior and important qualifications

Cluely separates the meeting widget from the dashboard. Its assistance modes include suggested wording, follow-up questions, recap, and typed questions. Custom modes combine prompts and uploaded PDF, DOCX, or TXT files. This supports a similar separation between a lightweight overlay and a fuller context/history screen. [Usage](https://support.cluely.com/en/articles/12294306-how-to-use-cluely), [customization](https://support.cluely.com/en/articles/12006358-how-to-customize-cluely).

Its support material qualifies the homepage's broad invisibility claim. Exclusion is not enabled by default; the support page lists Windows and capture-method restrictions, says Teams entire-screen sharing can expose the app, and explicitly excludes some installed monitoring systems. The article also contains conflicting general and platform-specific claims, so its specific limitations deserve more weight than its slogans. A screenshot test alone is insufficient to prove behavior in every meeting app. [Capture compatibility](https://support.cluely.com/en/articles/12364416-what-apps-are-supported-for-undetectability), [test instructions](https://support.cluely.com/en/articles/11127985-how-to-test-undetectability).

Its troubleshooting guide identifies permissions, wrong audio devices, Bluetooth devices, region availability, service outages, and network loss. These become first-class setup checks and recovery paths in our design. [Audio troubleshooting](https://support.cluely.com/en/articles/12005662-audio-capture-error-when-using-listen-mode).

### Parakeet: a disclosed audio and retrieval pipeline

Parakeet's privacy notice describes audio streamed to Speechmatics, specialist vocabulary extracted from session material, transcript/context passed to a selected AI provider, and document passages embedded and retrieved through an account-specific Pinecone partition. Its optional speaker voice profile is a separate feature. This is unusually concrete evidence for its pipeline; it does not establish its desktop framework, question detector, exact prompts, or capture code. We can implement the same functional stages with Azure and local storage. [Privacy notice, section 6](https://www.parakeet-ai.com/privacy-policy).

The mock product provides a spoken interviewer, suggested answers, follow-ups, and a transcript. Its FAQ describes desktop-browser-only mocks with fixed durations. For this user's app, practice questions and feedback will appear as text, and session duration will be user-controlled. [Mock interview workflow](https://www.parakeet-ai.com/mock-interview).

### What the research does not establish

There is no reliable public basis here for reproducing these companies' private models, source code, prompt templates, backend design, or claimed response times. We will build our own implementation of the useful workflows. Marketing accuracy percentages and invisibility promises will not become acceptance criteria.

## Recommended models

| Job | Choice | Reason |
| --- | --- | --- |
| Live speech-to-text | `gpt-live-transcribe`, deployed in the same Azure resource if available | Designed for low-latency transcript deltas, with context and vocabulary hints. |
| Answers, question generation, and feedback | Existing `gpt-6-luna` deployment | Preserves the requested model for all interview reasoning. |
| Transcription fallback | Existing Azure Speech resource | Use only after its region/endpoint and authentication are configured and tested. |

For this live-input workflow, `gpt-live-transcribe` is the best starting choice in the current GPT family. This is a workflow recommendation, not a claim that it wins every accent or accuracy benchmark. `gpt-transcribe` targets high-quality file transcription and committed speech turns; it starts that transcription after a turn is committed. Dedicated speech-to-speech GPT Realtime is unnecessary for the requested text-only output.

[OpenAI model description](https://developers.openai.com/api/docs/models/gpt-live-transcribe), [OpenAI transcription guide](https://developers.openai.com/api/docs/guides/realtime-transcription), [GPT-Transcribe](https://developers.openai.com/api/docs/models/gpt-transcribe). Microsoft lists `gpt-live-transcribe` in its Azure realtime model catalog; deployment and quota availability still need checking on the actual resource. [Azure support](https://learn.microsoft.com/azure/foundry/openai/how-to/realtime-audio).

## Architecture and approaches

Choose a native C# WPF desktop application on .NET 10 LTS, targeting Windows 11 x64 first. This is the recommended engineering choice, not a user-specified language. It gives direct access to Windows window behavior and audio capture, and supports a self-contained desktop package. A .NET SDK installation is needed on this machine; currently inspected locations contain desktop runtimes but no SDK.

Alternatives considered: Electron makes web-style interface work easier but requires native audio integration and a larger runtime; a browser-only app has a quick setup but cannot reliably own a native capture-excluded overlay or capture system audio with the same controls. Native WPF is the strongest fit for this Windows-specific request. Dependency versions will be pinned when implementation starts; .NET's own package tools apply to this C# app, and any Node-based tooling will use pnpm from an explicit project directory.

### End-to-end flow

1. Select the call's actual output device and optionally the microphone; run an audio-meter and transcription check.
2. Capture system audio with WASAPI loopback. Keep microphone and system streams separate so the user's answer is not mistaken for the interviewer's question.
3. Resample to headerless mono PCM16 at 24 kHz for the Azure transcription connection. Use a bounded audio queue and a worker, not network calls inside audio callbacks.
4. Display partial captions; reconcile final transcripts by item ID. Use client-side voice activity detection and commit speech turns. The current OpenAI guide says `gpt-live-transcribe` transcription sessions do not support server or semantic VAD.
5. Aggregate the completed interviewer utterance, including trailing requirements. Retrieve relevant resume/document passages and recent conversation context.
6. Stream a short answer from `gpt-6-luna`: three useful talking points first, optional expanded answer, and a relevant personal example only when supported by the user's material.
7. Preserve completed answers in history. A newer question must not silently erase an answer being read. Allow manual Answer, regenerate, pin, and pause controls.

Use the Azure v1 Responses endpoint for Luna and a dedicated transcription WebSocket session for the live model. Configure deployment names independently. Azure's transcription flow uses `/openai/v1/realtime?intent=transcription`, with the deployment selected inside session configuration. Handle WebSocket fragmentation, item ordering, disconnection, and final audio-buffer commits explicitly. [Azure WebSocket guide](https://learn.microsoft.com/azure/foundry/openai/how-to/realtime-audio-websockets), [Azure v1 API](https://learn.microsoft.com/azure/foundry/openai/api-version-lifecycle).

### Interface and practice modes

Setup contains Azure connection tests, selected audio devices, language, resume, job description, company/role, and supporting files. The overlay shows listening state, reconciled streaming captions, the current question and formatted streamed answers. It contains daily controls, Tune, practice, pin and follow-ups. History and full context management remain available in Setup. Configured startup opens the overlay with audio paused; Setup is created lazily and hides after Save & launch.

Coached rehearsal supplies text answers to questions heard from a practice call. Independent mock practice displays a Luna-generated question, records the user's microphone answer, and shows rubric-based feedback afterwards. Hints are optional. Feedback covers relevance, structure, specificity, and technical completeness; it is practice guidance rather than a prediction of hiring outcomes.

An optional screenshot/region attachment supports written coding questions. Extract readable text locally before using Luna; direct image input is enabled only if the actual Luna deployment supports it. No continuous screen-upload feature is required.

### Windows overlay behavior

Use `SetWindowDisplayAffinity(hwnd, 0x11)` (`WDA_EXCLUDEFROMCAPTURE`) on each owned top-level application window intended to be excluded. Apply it before display; check errors and reapply when window handles change. Prefer in-window controls over separate popups. File pickers and other operating-system dialogs require separate compatibility checks.

F12 requires special handling: Microsoft's `RegisterHotKey` documentation reserves it for debuggers. Use a keyboard hook restricted to the documented shortcuts. Ignore injected input, repeated action keys and AltGr typing; allow repeat only for scrolling/resizing/zoom/opacity, scope navigation and copy to the visible overlay, schedule UI actions, and return immediately. Offer a configurable fallback such as Ctrl+Shift+Space. The hook must not log other keys. Showing the read-only overlay must not activate it; mouse controls also retain nonactivation; only the explicitly requested question editor or Setup takes focus for typing. Hiding the overlay and pausing transcription are separate actions, with a clear listening indicator and an exit path.

An exclusion flag is not proof of invisibility in a particular app. The compatibility result must record the Windows build, meeting-app version, share mode, display setup, and receiver-side observation. Capture exclusion does not conceal the application's process. No complete-undetectability guarantee is technically justified. [Window capture API](https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-setwindowdisplayaffinity), [F12 restriction](https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-registerhotkey).

### Context, privacy, and recovery

Import PDF, DOCX, TXT, and Markdown. Preview extracted content and let the user correct it. Detect scanned/empty PDFs and offer text paste or a tested OCR path; do not claim a successful import when no useful text was extracted. Start with local chunking and full-text search, so no additional embedding deployment is needed. Keep source references with candidate facts and retrieved passages.

Store credentials through Windows-protected user storage and reuse the existing key from the environment when available. Never embed keys in the executable or log them. Documents and saved transcripts remain local; relevant passages and audio still leave the computer for the selected Azure services. Save transcripts only when enabled; do not persist raw audio by default. Use `store:false` for supported Responses requests, without treating it as a guarantee of zero provider retention.

Network loss, service throttling, and device changes must create visible recovery states. Use bounded retries, respect Retry-After, preserve completed transcript/answers, label capture gaps, and avoid replaying old audio silently. Typed questions remain usable when transcription is unavailable; online answers pause when the model service is unreachable. [WASAPI loopback](https://learn.microsoft.com/windows/win32/coreaudio/loopback-recording).

## Reliability acceptance and required inputs

The first build must prove: F12 behavior, actual capture exclusion in the target screen-sharing apps, audio from speakers and headphones, correct resume grounding, accurate question boundaries, and a continuous 60-minute practice session. Test device disconnect/reconnect, sleep/resume, 401/429/service failures, and cancelled or truncated model streams.

Initial measured target: first useful talking point at median <=2 seconds and 95th percentile <=5 seconds after a completed question under a documented test setup. This is a target to measure and tune, not a service promise. Benchmark at least 30 representative recorded questions plus a live rehearsal; report transcription mistakes separately from answer quality and timing. Test English first; add other languages after representative recordings are available.

Already known: existing Azure resource URL, key variable present, confirmed answer deployment `gpt-6-luna`, transcription deployments on the same resource, and an existing Azure Speech resource. None of the API connections have been tested in this planning pass.

Still needed for live acceptance: the exact deployment name serving `gpt-live-transcribe` (or permission to use an existing compatible deployment), resume and supporting context, target interview role/job description, chosen microphone/output devices, and the meeting applications/share modes to certify. The Speech fallback additionally needs its region/endpoint and credentials through app settings. Building requires a .NET 10 SDK and dependency downloads; wider distribution benefits from a code-signing certificate, while a personal portable build does not require one.

Deliverables: source, a portable Windows application, a short setup guide, and a verification report that distinguishes passed, failed, and untested checks.
