# Overlay reliability and daily workflow

User scope: visibly working opacity; audit settings/actions and persistence; initial configuration followed by overlay-first launch; live streaming captions in the overlay; rich daily-use controls without repeatedly opening the base dashboard. Retain keyboard-first nonactivation except an explicitly requested text editor/dialog.

Root cause: opacity changes only the outer brush while inner panels/code remain mostly opaque; Glass=false forces that brush alpha to 255. Use WPF AllowsTransparency and Window.Opacity for compositor-supported per-pixel transparency. Direct WS_EX_LAYERED/LWA_ALPHA was rejected by WPF, so verify rendered alpha and compositor state instead. Glass separately changes panel translucency. Text and controls fade with the window; name this Window opacity rather than claiming constant text opacity. Replace WindowChrome with native edge hit-testing; retain keyboard resizing, capture affinity and WS_EX_NOACTIVATE.

Keep WPF and the existing local runtime/dependency layout. On first launch without a saved configuration, show Setup. With saved configuration, show the overlay directly by default. Launch overlay saves pending setup, hides the setup window, and leaves the overlay running; the launcher command returns immediately. Do not start microphone/audio capture automatically. Make dashboard creation lazy and use the overlay as MainWindow.

Add persistent listening, auto-answer and caption controls to the overlay. Captions consume the existing reconciled live/final transcript; provide bounded panel size, reading-position retention and keyboard paging/end. A small explicit question editor replaces dashboard editing in daily use. Setup continues to manage resource/context and deeper preferences.

Settings must notify all open views of changes, preserve old defaults, snapshot without copying subscribers, and roundtrip all options. Selected audio devices must update all views; disable device/microphone changes during an active or connecting session. Expand compact/normal layout checks to Tune, captions and attachments.

Verify core/controller regressions, native alpha with glass on/off and at 100%, launch/configuration flow, reconciled partial/final captions, all settings bindings, listening/follow/history/code controls, layouts and private-file-free portable/source archives. Actual unlocked-desktop capture, Azure audio/vision/format quality and receiver sharing remain manual checks in this connection.
