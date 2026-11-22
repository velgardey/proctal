# Proctal focused glass overlay implementation plan

> Execute inline with executing-plans, under existing implementation authorization.

Goal: three glass surfaces, less overlay clutter, and a cited security/reliability comparison.
Stack: native WPF/.NET10, existing Markdown/IBM Plex; no new dependency.
Spec: docs/superpowers/specs/2026-11-10-focused-glass-design.md

- [x] Task 1: Add failing real-XAML regressions for 480x520 viewport and one-row default controls with hidden secondary options. Add native More-preserves-geometry check. Replace OverlayWindow.xaml hierarchy; update code-behind for caption expansion, conditional images, compact toolbar, More and streaming state. Preserve meaningful existing selectors and native lifecycle.
- [x] Task 2: Audit transport/key/storage/render/shortcut/capture controls; write docs/2026-11-10-security-feature-comparison.md with first-party citations and implemented/claim/manual statuses. Reproduce any credential-redirect gap before shared HTTP/WebSocket transport fixes. Local fake endpoints only; do not bypass production TLS or use real credentials.
- [x] Task 3: Full test/cleanup and native self-check to work/ui-upgrade/native-focused-glass.json. Inspect normal/minimum/expanded/More/images/compact/code renders over neutral light/dark backgrounds. One fresh final review/fix pass. Update README/quickstart/acceptance, freeze app/source archives and verify readability/runtime/privacy/matching app SHA256.

Review focus: click/keyboard parity under nonactivation; More/images preserve geometry; caption reading and hidden catch-up; short windows/long labels/font sizes; HTTPS/WSS credentials/redirect/TLS. Document plaintext profile, unsigned build, certification and sharing limits.
