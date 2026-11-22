# Audio, answering and session memory

Proctal keeps the interview state within the current app launch. Pausing/restarting audio with F8 preserves that state. Restarting the app begins a fresh automatic conversation context; saved History can still be selected for an explicit follow-up. This release does not add a separate new-session control.

## Audio and immediate answers

The selected output and enabled microphone use separate capture/transcription streams. Source labels survive identical item IDs on those two streams. Quiet audio is transmitted without a local volume cutoff. Committed items reserve transcript order, so final captions arriving out of order keep their original item order. Provisional captions are replaced by final text, and late deltas cannot overwrite a final.

F11 takes the available snapshot immediately. It does not wait for the next speaker, a final caption, audio drain or screen capture. Images, deep answers, automatic answers and feedback also include stored transcription; image/deep paths retain their explicit input priorities. Newly arriving captions do not mutate an already submitted request. Words not yet recognized cannot be sent as text. Typed questions take priority; candidate microphone wording is reference material rather than an automatic interviewer question.

Audio sends have a ten-second timeout. WebSocket ping/pong checks use a twenty-second interval and timeout. If a commit acknowledgement or committed transcription remains unfinished for sixty seconds, a fault pauses listening instead of waiting indefinitely. Network backpressure/queue overflow also reports a gap. Check the status/captions and use F8 to restart. Audio is not silently replayed after an ambiguous failure, and no raw recording is kept for reconstruction.

Stored transcripts retain up to 300 items, including pending metadata. Older items roll off. Captions are transcription hypotheses: accent, microphone level, noise, echo, overlapping voices, device routing and service quality still affect correctness. Current tests do not establish live headset word accuracy.

## What Luna remembers

Only successfully completed answers from this app session participate in automatic answer memory, up to the 250 retained History records. Deleted, failed, partial, cancelled, superseded and previously loaded records are excluded. Full retained answers remain in the local in-memory History list; saving across launches stays opt-in.

Each request includes:

- A compact chronological question/answer index of exchanges older than the latest three. Excerpt length adapts to the number of earlier exchanges.
- Up to four relevant earlier text excerpts retrieved by query terms. A simple query-term scan selects up to eight candidate exchanges for detailed retrieval, with each candidate retaining up to 12,000 question and 80,000 answer characters. Retrieval computes each query term's weight once, avoiding repeated document-frequency scans and parsing every full old answer as the session grows. Excerpts can split code and are labelled accordingly; they are not generated summaries or guaranteed exact semantic recall.
- The latest three completed pairs, oldest first. The latest question retains up to 12,000 characters and its answer up to 80,000; the other two retain up to 1200/4000 each.
- A snapshot of stored transcription from both sources and any explicit selected-answer reference. Explicit follow-ups exclude that selected answer from the automatic index/recent pairs to avoid duplicating it.

Role/job material has a 20,000-character safeguard, the combined explicit/transcript reference retains up to 60,000, and combined conversational input retains up to 160,000. Long text preserves the beginning/end with a labelled middle omission. These are character safeguards, not an exact tokenizer or a guarantee against every deployment context limit. Current question, document extracts, instructions and images also consume model context. Output has no app token cap; the model's finite limits still apply.

Instructions ask Luna to preserve relevant constraints for follow-ups, discard unrelated constraints when the topic changes, prefer current explicit constraints and identify an ambiguous reference or ask a clarification. Earlier generated responses remain untrusted reference, so previous mistakes can be corrected. This improves grounding; it cannot guarantee perfect interpretation or correct code.

## Saved history and interrupted answers

With History saving enabled, the first streamed text is checkpointed as partial. Further incoming text triggers checkpoints at five-second intervals. The final record replaces its checkpoint. Cancellation or replacement labels the interrupted record partial, even if a transport finishes after cancellation. A superseded stream cannot paint over the newer answer after a checkpoint write. Deleting an active record prevents its subsequent checkpoints/final save from restoring it.

A crash can still lose text since the last checkpoint, and failed disk writes prevent persistence; the app reports save failures. Saving disabled means closing the app loses unsaved answers. Existing corrupt-file backups preserve bytes and permit new saves; they do not reconstruct missing or never-saved sessions.

## Evidence and a repeatable cloud check

Reproduced failures and updated regression results are under `work/verification/session-reliability`. The release workflow records final native, archive and private-profile preservation evidence there. The synthetic Azure check uses seven short generated test exchanges to check recall after four unrelated topics, then independent problem scoping, plus a transcription handshake. It sends no microphone audio, personal documents, stored transcript, screenshots or saved history:

November 21 results: all 205 tests and cleanup checks passed; the staged Windows build passed all 83 native checks. The seven-answer Azure sequence passed recall and independent-topic checks plus its transcription handshake in 19.7 seconds overall. This is total sequence time, not a per-answer latency guarantee. Evidence: `work/test-results/core.trx`, `work/verification/session-reliability/native.json` and `azure-session.json`.

```powershell
& 'C:\Projects\proctal\dist\Proctal.Windows\Proctal.exe' --check-azure 'C:\Projects\proctal\work\verification\session-reliability\azure-session.json' --session
```

One successful synthetic sequence is evidence for that sequence, not a measured success rate across real interviews. Actual two-party audio accuracy, long-duration network/device behavior and screen-sharing receiver behavior still require representative hardware rehearsals. No universal reliability or invisibility guarantee is made.
