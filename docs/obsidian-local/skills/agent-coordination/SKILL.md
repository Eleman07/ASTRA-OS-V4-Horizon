---
name: agent-coordination
description: Decompose a broad request into bounded specialist tasks and integrate evidence into a reviewable deliverable.
---

# Agent Coordination

## Method
1. Restate the requested outcome and acceptance checks.
2. Select only the relevant specialists; give each one a bounded question, context sources and expected output.
3. Ask for source links, assumptions and unverified items with every handoff.
4. Compare disagreements against primary evidence; keep unresolved differences visible.
5. Synthesize the requested deliverable and a short verification checklist.

## Output
Return `objective`, `acceptance checks`, `delegated tasks`, `evidence`, `disagreements`, `deliverable` and `remaining verification`.

## Safety
Do not invent completed agent work or sources. Do not authorize note edits, code execution, uploads or irreversible actions on the user's behalf.

