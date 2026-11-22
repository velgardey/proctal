# Overlay upgrade — 10 November 2026

User-authorized scope: keyboard-operated nonactivating overlay; configurable glass opacity, text size, code wrapping, streaming follow and window layout; proper resizing; fast code-first Markdown answers; useful interaction ideas from InterviewMan, Cluely, Pluely and Parakeet AI.

Keep native WPF and Azure deployments. Use Markdig 1.6.0 for CommonMark/pipe-table parsing, render native FlowDocuments with selectable text, labeled code panels, syntax color and copy actions. Throttle streaming renders to about 10 per second and preserve reading position. Disable raw HTML and remote images; links open only by an explicit user action. The native approach avoids adding a browser runtime and works with current window controls.

Keep the overlay nonactivating, including mouse controls. Global keyboard shortcuts handle scroll/page/top/end, resize/move, opacity/zoom, latest/history, code copy and three follow-ups. The dashboard handles typing/configuration. Persist layout with bounds checking when monitors change; retain default F10/F11/F12. Add a compact composer, follow indicator and quick tuning controls.

Use documented Windows 11 Desktop Acrylic where supported and a translucent tinted fallback. Preserve foreground contrast; opacity changes the glass surface rather than fading answer text. No decorative animation. System transparency/composition can suppress acrylic, so actual backdrop appearance needs normal-session confirmation.

Implementation sequence: (1) parser and code-first payload with regression tests; (2) normalized preferences, layout and keyboard policy with tests; (3) native renderer and glass/resizable nonactivating shell; (4) follow-ups/history/export and dashboard configuration; (5) full suite, populated/minimum UI renders, native checks, focused review and portable package.

Verification focus: incomplete streamed fences, nested inline/list/table Markdown, code copy fidelity, retained reading position, shortcut repeats without typing interception, invalid/saved off-screen geometry, source-preserving cleanup, and kept window-focus/capture behavior. Actual live Azure/audio/receiver sharing remain manual acceptance checks.
