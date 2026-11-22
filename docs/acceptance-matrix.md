# Acceptance evidence — 10 November 2026

| Check | Result | Evidence / limit |
| --- | --- | --- |
| Release build | Passed | Zero warnings/errors |
| Core/controller/Markdown tests | 106 passed | Includes custom instructions/presets, Fast retry and snapshot, settings roundtrip and mock/connection-check isolation, plus prior keyboard/Markdown/audio/screenshot/history coverage |
| Cleanup | Passed | Preview, scoped removal, source preservation and marker guard |
| Current native check | 54 of 55 passed | Capture affinity, mouse/keyboard nonactivation, visibility, image attachment, hook, Markdown, keyboard resize/opacity/reading and physical placement passed |
| Current screenshot check | Unavailable in tool session | GDI error 6; same restricted-session limitation as prior runs |
| User's ordinary Windows native check | Previously 9 of 9 passed | Capture affinity 0x11, actual capture, restoration, interaction flags and hooks |
| Azure connection | User previously reported working | Luna and live-transcription connection; not re-run in this tool session |
| UI | Passed scoped static review | Normal/minimum dashboard, all settings subpages, code/history, normal/compact overlays and image attachments; no concrete layout blockers |
| Portable runtime | Passed | App launched with deliberately invalid external DOTNET_ROOT and used bundled .NET runtime |
| Glass / opacity | Native and static checks passed | Layered overlay compositor and rendered alpha verified; glass changes panel translucency. Actual desktop appearance remains a manual check; native blur applies only to Setup. |
| Mixed-DPI monitors | Hardware check remains | Physical origin/native placement no longer uses startup scale; saved-origin test and native authority check pass, multi-monitor hardware not available |
| Live audio / vision quality | Manual checks remain | Needs chosen output device and representative questions/screenshots |
| Résumé-grounded quality | Manual check | Needs user's imported résumé and known/unknown questions |
| Receiver-side sharing | Manual check | Test exact app/version/share mode/monitor/scaling; capture affinity alone is insufficient |
| Physical shortcut rehearsal / long soak | Manual checks | Keyboard policy and native styles verified, not actual international hardware or a 60-minute call |

Current report: work/ui-upgrade/native-focused-glass.json. UI renders: work/ui-upgrade/. Unit result: work/test-results/core.trx.

Deferred minor formatting limitations from review: ordered Markdown lists start at 1; copying code removes trailing blank lines. They do not affect ordinary coding answers or scrolling.

This chat's process cannot access the unlocked desktop's screen capture, normal user's DPAPI profile, or outbound Azure connection. User keys remain protected; no security setting or TLS verification was bypassed. Windows capture exclusion cannot guarantee complete undetectability.

The earlier reliability review found four Important defects. Each was reproduced before its fix and now passes: cramped Tune/caption layout, rolling/reconciled caption anchors, hidden-answer catch-up, and leaving history when starting practice. The actual XAML/theme was also tested at 600, 650, 700 and 800 logical pixels of height with maximum captions and More open. The answer viewport remains at least 140 pixels.

Implementation decisions: WPF per-pixel transparency replaces direct layered-window alpha rejected by WPF; text fades with window opacity and overlay glass has translucency rather than native blur. Setup is created lazily and hides in the shared overlay process; the command launcher exits immediately. Source is exported instead of committed because this connection cannot update the Git index.

The review set aside real hardware/compositor/receiver capture, live Azure and soak results, and final archives. Hardware/cloud checks remain manual. Portable/source archive checks are performed separately for runtime inclusion and private-data exclusion; a successful archive check does not establish live interview quality.
Quality-of-life checks: independent live-text submission during pending capture, explicit image sequencing, duplicate prevention, stable answer question, candidate exclusion/continuation, manual-final suppression, paused question retention, stale-practice input, clearing unfinished captures, connection controls and batched burst streaming. Native checks cover actual F11 / image command routing, hiding until capture completes, restoration, compact primary Answer and lightweight appearance updates. Native fixtures use synthetic local HTTP and image data; no new live Azure test.

The QoL review found four Important defects: stale partial speech overriding a newer question, lost drain-time finals, automatic questions consumed during streaming, and a missing image action in Setup. Regression tests and the actual Setup image-button diagnostic failed before fixes and pass afterward. Extra regressions cover compound repeat submission, partial/final continuation updates and excluding manually consumed fragments from the next automatic question. Final native result is 54/55; screenshot capture remains unavailable in this tool session.

Focused-glass pass: 106 Release tests and cleanup passed. Default 480x520 viewport and one-row/hidden-options regressions failed before the redesign and pass. In-window More preserves physical geometry; conditional image actions and explicit caption expansion retain keyboard focus. Neutral-backdrop previews demonstrate composited alpha, not actual desktop blur. Native 54/55 after the final review fix pass, actual screenshot unavailable in this connection. HTTP redirect guard and WebSocket handshake guard pass against synthetic local server requests after prior redirect-following failures. New transport still needs ordinary-session Azure connection verification.

Final focused-glass review: operational failures hidden during reading/history were reproduced by rejected capture operations, then passed with an independent visible status label. Primary hover contrast failed before a light primary-only hover brush, then passed. 106/106 Release and cleanup green; native 54/55, only actual screenshot unavailable. All native view bindings clean. No fresh live Azure run is claimed after transport changes; test Check connection in your ordinary Windows session. Review decisions and set-aside checks are recorded in 2026-11-10-focused-glass-review.md.

## Current math/deep/history release — 13 November 2026

154/154 Release tests and cleanup passed. All 71 native Windows checks passed, including repeated F7 during capture, retained typed prompt, F7 xhigh request isolation/nonactivation, History shortcut, rendered equations plus code and math hover prevention. A synthetic Azure request to the saved gpt-6-luna deployment succeeded with xhigh and 32768 total output budget; no audio, documents, transcript or screenshots were sent. The xhigh check is stored in work/verification/luna-xhigh.json. Two P2 review findings were reproduced and fixed with three red-to-green regressions. The saved history preference remains off. Receiver-side sharing and a real audio rehearsal are separate checks; no universal invisibility guarantee is made.
