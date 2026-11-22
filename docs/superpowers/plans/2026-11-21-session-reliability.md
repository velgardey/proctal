# Audio and session reliability

Build on the existing native WPF app and Azure deployments. Keep F11 independent of new audio/finals/capture, history opt-in and the keyboard/window behavior unchanged. The app launch defines the current session, including listening pauses/restarts. Do not capture live microphone audio without a fresh user-requested rehearsal.

1. Reproduce older-than-three answer exclusion, duplicate explicit references, missing streaming checkpoints, superseded-answer labelling and stalled audio/transcription connections with existing synthetic controller/socket infrastructure.
2. Include a compact chronological index of all retained completed current-session exchanges, relevant earlier excerpts and recent pairs. Exclude loaded/deleted/incomplete turns; bound request context transparently and keep full answers in History. Snapshot all sources before requesting an answer, including image/automatic requests.
3. Save partial-answer checkpoints only when history is enabled, then replace them with the final record. Mark cancelled/superseded streams partial. Avoid replaying requests or audio after ambiguous connection failures.
4. Bound audio send waits, enable WebSocket ping/pong failure detection and flag committed transcription that stalls. Preserve stored captions and completed answers on faults. Verify existing ordering, both-source isolation, quiet audio and drain behavior.
5. Run the complete suite and native checks; update user docs, prepare verified source/portable archives and install when Proctal has exited. Compare saved-data and installed-binary hashes. Report actual test evidence and remaining hardware/cloud limitations.

No new dependencies, models, frontend redesign, recording mode or settings are needed. Context budgets are app safeguards for inputs, not output-token limits. Compact excerpts are labelled; they are not generated summaries or guaranteed perfect recall. A service fault pauses listening for an explicit F8 restart rather than silently skipping/replaying audio.
