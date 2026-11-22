# Interview assistant UI research — 10 November 2026

Public product pages, documentation and screenshot references informed Proctal's design. These observations describe visible product behavior, not independently tested performance or privacy claims. Proctal keeps its own native WPF implementation and visual identity.

| Reference | Observed pattern | Adopted in Proctal |
| --- | --- | --- |
| [Cluely screenshot](https://docs.cluely.com/images/usescreen.png), [changelog](https://docs.cluely.com/changelog) | Compact dark answer panel, screen-context action, response status and follow-up actions | Compact overlay, F10 capture, processing/reading status, Explain/Edge cases/Optimize |
| [Pluely Ask mode](https://docs.pluely.com/docs/features/ask-mode), [window controls](https://docs.pluely.com/docs/guides/resize-and-move-overlay) | Markdown and code presentation, stop/follow-ups, history search, size presets, transparency and keyboard workflow | Native selectable Markdown and syntax-colored code; per-block copy; Stop, history search, presets, saved size/placement and opacity shortcuts |
| [Parakeet AI](https://www.parakeet-ai.com/) | Dedicated session/preferences setup, audio context, coding responses, screenshot workflow and reading controls | Grouped audio setup, separate Connection/Answers/Appearance pages, code-first answers, attachments and optional streaming follow |
| [InterviewMan](https://interviewman.com/) | Technical-interview workflow with screen context and résumé-aware assistance | Screenshot input plus imported résumé/context, configurable coding language and concise complexity/edge-case explanations |

The resulting interface uses translucent dark slate surfaces, thin borders, restrained amber actions, and embedded IBM Plex Sans/Mono. The focused-glass pass uses subtle edge highlights rather than decorative backgrounds. Window opacity fades the whole overlay, including text; glass surface changes panel translucency. Dashboard typing is explicit, while overlay scroll/resize/copy/follow-up commands preserve foreground focus.

The main gaps addressed were readable streamed code, a reliable reading position, direct keyboard scrolling, configurable window geometry, separate appearance settings, searchable Markdown history, and compact controls. Marketing claims of universal invisibility or fixed response latency were not adopted as guarantees. Windows capture exclusion remains a documented best-effort feature; actual sharing behavior must be checked from the receiver's view.

The website automation executable could not run in this tool session, so first-party pages and native web screenshot references were reviewed through the web tool. Final native Proctal renders were inspected at normal/minimum sizes, with populated code/history and attachments. Static renders establish layout and legibility, not actual Windows backdrop blur or hardware activation behavior.

The later focused-glass pass uses the user's Cluely/Parakeet-style images and public OpenCluely material: a charcoal toolbar, compact captions and integrated answer, with secondary controls in More. See 2026-11-10-security-feature-comparison.md for first-party sources, verified controls and explicit feature/security gaps.
