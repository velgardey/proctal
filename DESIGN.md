# Proctal visual system

Dashboard and overlay share a neutral charcoal glass palette. The dashboard supports explicit editing; the overlay supports reading and keyboard operation while the foreground editor keeps focus. Start, Context, Setup and History remain separate pages. Setup divides Connection, Answers and Appearance.

Embedded IBM Plex Sans Regular/Medium/SemiBold/Italic provides interface text. IBM Plex Mono provides code, shortcuts and measured values. No fonts are installed globally or fetched at runtime. Prose defaults to 16 px with generous line height; answer size is configurable from 12–24 px.

| Token | Dashboard | Overlay |
| --- | --- | --- |
| Background | #DC1B1D21 | #101B2C with configurable alpha |
| Surface | #B5282B31 | #CF1D2D43 |
| Text | #F1F2F4 | #EEF3FB |
| Secondary | #B9BDC6 | #AFBDD0 |
| Border | #554F545F | #67849EBE |
| Field | #E0191C21 | #BF142135 |
| Primary | #ECEEF1 | #EABB72 |
| Primary text | #1C1F24 | #29231A |
| Selection | #4C5869 | #4C647F |

The overlay uses WPF per-pixel transparency with independently translucent charcoal panels. Window opacity fades text and controls together; Glass surface controls panel translucency. The setup dashboard retains Windows 11 Desktop Acrylic where supported. Thin borders define depth; there are no decorative gradients, glowing effects or animations. Actual system blur depends on Windows composition/transparency and is not visible in static RenderTargetBitmap previews.

Spacing follows 4/8/12/16/24/32 px. Sidebar is 188 px; content padding 28 px. Corners: controls 8 px, groups 12 px, shells 14 px. Checkboxes, fields, selectors, sliders, audio meters and lists share the palette. Off-white marks primary actions; soft green marks meaningful status.

Markdown uses native selectable FlowDocuments, language-labeled syntax-colored code panels, per-block copy, lists, quotes and tables. WPF measures code height from rendered layout, including tabs and wide glyphs. Streaming renders are throttled and reading position retained. Remote images and raw HTML do not become active content.

The overlay retains WS_EX_NOACTIVATE in keyboard and mouse modes. Navigation, resize/move, opacity, zoom, code copy, history and follow-ups work without activation. Typing happens in a small explicitly requested question editor. Initial configuration uses Setup; configured startup opens the overlay, with audio paused. Captions and daily controls live in the overlay. Physical placement is saved independently of startup display scaling; mixed-DPI hardware rehearsal remains required.

Native renders cover normal/minimum windows, setup subpages, populated code/history and attachments. Fresh scoped visual review found no layout blockers. Taste and Impeccable guidance was adapted to native WPF; website-specific validators do not certify this application.

## Focused overlay — November 2026

Overlay palette: charcoal #202124; primary text #F1F2F4; muted text #AEB0B6; primary action #ECEDEF with #202124 text; live audio #9DD5A4. Main glass alpha 204/255, toolbar/captions 220/255, with independently adjustable window opacity. Fine neutral strokes and 7–12px radii. IBM Plex Sans and Mono stay embedded. Three aligned surfaces separate actions, captions and answer; advanced controls appear only in More/Setup. Native WPF translucency is deliberate; native blur is not claimed. Setup follows the charcoal palette with local WPF resources.
