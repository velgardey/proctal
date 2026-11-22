# Proctal: reference features, security and reliability

Reviewed 10 November 2026. Public first-party pages and the user's two screenshot references informed this pass. This is a source/code and native-control review, not an independent security certification or a penetration test of other products. Product names refer to the sources linked below; similarly named third-party sites were excluded.

## Reference patterns

| Source | Publicly described / visible pattern | Proctal's response |
| --- | --- | --- |
| [Cluely guide](https://docs.cluely.com/) and [changelog](https://docs.cluely.com/changelog) | Simplified desktop widget, audio setup/test, live answers, contextual knowledge and instant follow-ups; detailed transcripts moved into the dashboard to reduce widget clutter. The guide lists enterprise compliance claims. | One action bar, short expandable captions, generous answer surface; full configuration/context/history in Setup. Follow-ups are in More. Proctal does not inherit enterprise certifications. |
| [Pluely](https://pluely.com/), [Ask mode](https://docs.pluely.com/docs/features/ask-mode), [window controls](https://docs.pluely.com/docs/guides/resize-and-move-overlay) | Translucent overlay, typed/screen/audio input, streamed answers, stop/follow-ups, dashboard/library, resize and opacity controls. Its public interface separates immediate asking from listening and advanced setup. | Immediate speech/text Answer, explicit image answer, keyboard sizing/opacity, small Chat editor, hidden quick controls, Markdown/code and searchable history. |
| [OpenCluely repository](https://github.com/TechyCSR/OpenCluely) | Floating technical-answer workflow, voice or screenshot input, Gemini visual reasoning, optional local Whisper or Azure Speech, shortcuts and platform-dependent capture exclusion. | Reference hierarchy adopted with an independent native WPF implementation. Existing Luna/transcription deployments retained; local/offline Whisper and Gemini are not added. |
| [ParakeetAI](https://www.parakeet-ai.com/) and [privacy policy](https://www.parakeet-ai.com/privacy-policy) | The supplied screenshot emphasizes Answer, Screenshot and Chat, a transcript strip and a large answer panel. Public pages market capture/tab/cursor/process concealment; the policy describes cloud processing and optional biometric voice profiles. | Similar action hierarchy and fewer persistent controls. Proctal has no biometric voice profile, no claimed hidden process or undetectable keystrokes, and no universal invisibility claim. |

The new overlay uses three aligned charcoal surfaces with thin edge highlights, restrained white actions and embedded IBM Plex fonts. Checkbox rows, persistent follow-ups, presets, configuration buttons and duplicate status labels are removed from the default view. More is rendered inside the same native window, so it does not introduce another popup window or change saved geometry. Caption paging expands the rail; image actions appear only with pending/attached images. Glass means per-pixel translucency here; native desktop backdrop blur is not implemented in the overlay.

## Controls actually present in Proctal

| Area | Implementation and evidence | Practical limit |
| --- | --- | --- |
| Azure credentials | Key protected with Windows DPAPI CurrentUser. Errors show sanitized messages; archives exclude the key/profile. | A process running as that same account may be able to decrypt it. No claim of protection from a compromised Windows account. |
| Resource validation | Azure resource hosts only, HTTPS, default port, no URL user-info/query/fragment; transcription uses WSS. | Uses configured Azure resource access and retention policies. It is not offline. |
| Redirect protection | HTTP answer transport and WebSocket handshake explicitly refuse automatic redirects. Local server tests reproduced redirect following before fixes, then verified a single request without reaching the redirect destination. | Local tests use synthetic keys and loopback HTTP/WS to isolate redirect behavior; they do not prove real Azure availability or certificate-chain correctness. Production keeps normal TLS verification. |
| Stored context | Résumé extracts, settings and optional history stored within .data. History saving defaults off; scoped cleanup exists. | Documents/settings/history are plain local JSON. Use Windows account/file permissions and device encryption as appropriate; app-level document/history encryption is not implemented. |
| Audio/images | Bounded queues, explicit listening controls, source labels, capture-gap reporting and drain-on-pause. Audio/images are not persisted by ordinary app operation. Screenshots go to Luna only through Answer images. | While listening, audio goes to Azure; selected context, recent captions and answers go to Azure. Debug verification intentionally writes synthetic UI renders. No long live-session audio-quality claim. |
| Responses | Trusted response instructions separated from reference content; raw HTML disabled; image markup does not fetch remote images; links limited to explicit HTTP/HTTPS clicks. | Models can produce incorrect answers or follow malicious reference content despite instruction separation. Model output is never executed as code by the overlay. |
| Keyboard/focus | Ordinary shortcut hook, unrecognized keys passed through, injected events ignored, no general keystroke log. Nonactivating overlay and physical-position/native-style checks pass. | Hooks, process and UI remain observable to Windows and privileged monitoring. Explicit Chat/Setup/dialogs take focus; microphone capture remains explicit. |
| Capture exclusion | WDA_EXCLUDEFROMCAPTURE on owned windows, read-back/status, capture-time hiding/restoration. Native affinity passes; user previously passed ordinary-session capture. | This session's actual screen capture is unavailable. Check the receiver's exact meeting/share mode. A camera or unsupported capture method can still see the display. |
| Reliability | Existing cancellation, rate-limit progress, Fast fallback, stable question/answer pairing, partial/final reconciliation, history/reading anchors and image retry. Small-window and native-menu/focus checks included. | No universal latency, mixed-DPI hardware, 60-minute soak or live résumé-grounding guarantee. Fast processing is requested and only confirmed when returned by Azure. |
| Distribution/removal | Local toolchain/cache, project marker/path guards, source-preserving cleanup, bundled runtime, readable archive/checksum checks. | Personal build is unsigned; no managed updater, signing certificate or supply-chain attestation. Cleanup does not erase backups or Azure-held data. |

## What is not at feature parity

Public products cover additional areas: region selection for screenshots, push-to-talk, local/offline transcription, automatic meeting detection, configurable response pacing, rich recaps/session search, model switching, mobile interfaces and enterprise administration. Proctal does not implement all of those. It retains the requested Windows/Luna/text-output focus and explicit screenshot submission. No competitor performance or concealment claim was independently reproduced here.

Enterprise SSO/RBAC, managed deployments, independent penetration testing, documented compliance certification, signed auto-updates and encrypted document/history storage are separate gaps. The current private tool should not be represented as an enterprise-certified product.

## Windows visibility is a limited control

[Microsoft's SetWindowDisplayAffinity documentation](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowdisplayaffinity) explicitly says that capture affinity works through a specific set of operating-system features and does not guarantee strict content protection. It requires a composing DWM; WDA_EXCLUDEFROMCAPTURE support begins with Windows 10 version 2004. It does not hide processes or keystrokes.

Use Proctal for mock practice and sessions where assistance is allowed. Capture exclusion helps keep private practice controls out of supported capture; it is not a complete-undetectability feature. Security and consent obligations still apply to the audio/context sent to Azure.

Redirect implementation reference: [SocketsHttpHandler.AllowAutoRedirect](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.socketshttphandler.allowautoredirect?view=net-10.0) states that custom headers are retained during automatic redirects; [WebSockets in .NET](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/websockets) documents the explicit HttpMessageInvoker handshake overload used for the fix.

Current execution evidence and remaining manual checks are recorded in acceptance-matrix.md and work/ui-upgrade/native-focused-glass.json. The archive's checksums establish the delivered files, not a security certification.
