# ASTRA 4.4.0 Conversation OS — verification scope

This edition replaces Assistant tabs with one themed page: a central knowledge graph, embedded persistent conversation, live activity terminal and side controls. Automatic routing uses the explicit user request, role contracts and assigned skills; source attachments never grant tool permissions. Users can override direct/team behavior and specialists.

Observed checks on Windows 11 x64:

- 184 existing multi-agent Core checks and 63 static regressions passed during development.
- 21 conversation/render-policy checks passed, covering routing, follow-ups, hardware constraints, reduced motion, durable full Unicode replies and idempotent delivery without duplicate messages.
- All 16 final native checks passed: one page without tabs, embedded chat, nine roles/39 skills, read-only timestamped terminal, automatic graph load, full >110,000-character node transfer into chat, working/idle animation state, graph release/recreation, automatic actual WebGL star rendering, desktop release and complete long input.
- A real request was submitted through the embedded conversation to the active Codex account in the final build. Four coordinated handoffs completed; the final flashcards returned to the same conversation, preserved calibration code 75731 and the supplied synthetic 90-second period, and cited `conversation-source`. The completed reply was persisted once through the worker delivery path. This was a synthetic software test, not a physical measurement. Private job records remain outside this repository/release.
- The earlier genuine CubeSat selected-model project passed 56 tests on unchanged generated code and independent CSV checks; see [the historical record](CODEX-4.3.0-VERIFICATION.md). That result is not a new orbital benchmark for this UI release or a guarantee for every model/task.

The graph uses an initial hardware-dependent display batch and capped pixel density/animation frequency. Graph previews are labeled and lightweight; focus-to-chat loads the full cached original node content. Source material and generated responses are not shortened by a graph display budget. Idle graph work animation stops. Reduced motion suppresses recurring work animation. The desktop nucleus is a rotating/pulsing star with a flowing two/three-color plasma-wave surface derived from the active theme, a luminous corona and animated rings within a bounded frame cap. Its actual WebGL shader compiled and rendered successfully after a shader compatibility correction.

Navigation away or window deactivation closes/removes the native WebView2 control and clears graph source references. Returning creates a fresh renderer. These are actual lifecycle checks, not a claim that all Windows/WebView2 shared caches immediately use zero RAM. Background jobs and conversation state remain available. No forced garbage collection, global browser-process termination or Windows power/security changes are used.

The terminal records timestamps and observed routing, assigned methods, queued jobs, stage/status changes, waits, completion and errors. It is not a fabricated reasoning transcript. Activity records are encrypted for the current Windows account. Generation, context, quota and upstream availability remain finite. Codex uses the user's account; OpenRouter is explicitly selected and constrained to eligible free routes. No paid fallback is enabled.

Final installer and release-asset checks are supplied with the GitHub release. The installer is unsigned; passing these checks is not independent security certification or flight validation.
