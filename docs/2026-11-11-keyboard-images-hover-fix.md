# Keyboard image answers and hover popup fix

F9 answers attached images directly. F10 captures. Single F11 answers typed text or current interviewer speech immediately. Two distinct unmodified F11 presses within 500 ms switch the second action to Answer images. F12 shows or hides the overlay. Ctrl+Alt+S remains available.

F9 avoids starting an initial speech request. Double F11 preserves immediate single-press behavior: its first press can start a speech request, and its second press supersedes that request with images. The timing uses a monotonic clock. Holding F11 never triggers images or extends the double-press window. Intervening physical keys, modifiers and completed pairs reset the sequence; injected input is ignored.

Only image submissions wait for unfinished capture. Single F11 remains independent of screenshots. Both image shortcuts work when the overlay is hidden and preserve its nonactivation behavior.

The hover rectangle is consistent with WPF's floating tooltips. The entire glass shell had a tooltip, in addition to individual controls. Tooltips use a separate native window; excluding the main overlay does not apply its display affinity to that popup. A native test reproduced the separate window, and the regression test showed that tooltip opening was not blocked.

Capture-excluded Proctal windows now cancel automatic tooltip opening for both FrameworkElement controls and FrameworkContentElement text. The event is direct, so prevention is registered on those classes and scoped to the owning window rather than only attached to a parent window. Enabling exclusion closes existing tracked tooltips. Controls retain their labels, shortcut text and help metadata; normal hover help is available when exclusion is explicitly disabled. The policy also applies to Setup and the question editor.

This suppresses Proctal's floating hover help. It does not certify invisibility in every sharing/capture tool, hide system dialogs or provide protection against cameras or software that disregards Windows display affinity. Verify the receiving screen in the actual sharing tool.

Microsoft references:

- [WPF Popup: separate native window and tooltip usage](https://learn.microsoft.com/dotnet/desktop/wpf/controls/popup)
- [ToolTipOpening: handling prevents display; event routing is direct](https://learn.microsoft.com/dotnet/api/system.windows.frameworkelement.tooltipopening?view=windowsdesktop-10.0)

No dependencies, Azure deployment choices, saved credentials or profile formats changed.
