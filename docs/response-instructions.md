# Answer setup and initial prompt

Open **Setup → Answers**, or choose **Answer mode & prompt** from Start. From the overlay, open More (Ctrl+Alt+V) → Setup. Answer mode, response structure and the editable Initial prompt are visible without expanding Model options. Click **Save changes** to retain them.

Initial prompt is your custom response instructions. It accepts up to 4000 characters and takes priority over the preset's format, order and detail. Leave it blank to use the preset. Preferences that only specify an aspect, such as programming language, keep the rest of the preset's guidance.

- **Code first:** working code, brief approach, complexity and edge cases.
- **Insight → solution:** problem and constraints, key insight and why it works, algorithm steps, complete code, an explanation of important variables/loops/conditions, a worked input/output example, complexity and edge cases, and a short explanation to say aloud. SQL answers explain relevant joins, filters, grouping and null handling. Conceptual answers include an explanation and a concrete example.
- **Problem → brute force → optimize:** problem/constraints, simple correct approach and code, bottleneck and intuitive improvement, optimized code, complexity and edge cases.

Example initial prompt:

> Use Python unless the question requests another language. Explain what each important variable, loop and condition does and why. Trace a small concrete example through the code. Include time and space complexity and the relevant edge cases. Finish with a short explanation I can present aloud.

Proctal no longer sends `max_output_tokens`; Azure chooses the deployment's output budget. The previous saved token limit is ignored and is removed on the next settings save. Luna's model limit still applies (currently 128,000 total output/reasoning tokens), as do context availability and deployment quotas. Longer answers can take more time and incur more usage. Streaming begins as soon as text arrives; an active stream is no longer cut off by a fixed overall deadline. A stalled text stream has a 90-second idle timeout; xhigh has up to five minutes to start returning text. Ctrl+Alt+Escape cancels.

The trusted instructions remain separate from résumé/job/transcript reference data and keep their priority on Fast-tier fallback. Settings are snapshotted for each request. Mock interviewer generation and connection checks retain their own tasks. Existing saved mode, structure, instructions, credentials and history are preserved.

Azure reference: https://learn.microsoft.com/azure/foundry/openai/how-to/reasoning
