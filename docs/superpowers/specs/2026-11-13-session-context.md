# Recent session conversation context

Superseded by the November 21 follow-up: automatic context now includes an earlier-exchange index and relevant excerpts as well as recent pairs, with labelled input safeguards and streaming history checkpoints. See [current behavior and limits](../../session-reliability.md).

Include the last three completed question/answer pairs from this controller session in every Luna answer request, oldest first, alongside recent captions, role/job data and retrieved documents. Do not include the current empty/streaming entry, failed/partial/cancelled/superseded answers, deleted entries or entries loaded from an earlier launch. Follow-up references to a selected history answer remain explicit.

Updated November 21: include the latest completed question and answer in full, retaining code in the middle. Bound the older two questions to 1200 characters and answers to 4000, retaining the beginning and end and labelling any omitted middle. Explicit follow-up references also include the full displayed question and answer. Longer references increase input usage and can increase latency. Generated answers are untrusted conversational reference, not evidence of candidate experience or instructions. Ask Luna to resolve follow-up references and maintain relevant assumptions/language while answering the current question without repeating previous material.

Normal F11, F9/images, F7/xhigh, auto answers and practice feedback use the same context path. Keep storage opt-in, store:false, keyboard behavior, window layout and fast reasoning unchanged. No new dependencies or provider calls are needed. Verify payloads and lifecycle, run the complete suite/native checks, preserve the profile and update the installed package and release archives.
