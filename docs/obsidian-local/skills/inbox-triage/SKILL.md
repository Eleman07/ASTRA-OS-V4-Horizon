---
name: inbox-triage
description: Turn selected Inbox notes into structured, linked proposals while preserving the original note until the user approves changes.
---

# Inbox Triage

## Method
1. Read only the selected note and directly linked/source notes.
2. Summarize the central idea in one or two sentences and extract explicit entities, dates, actions and open questions.
3. Suggest a title, tags and destination folder based on the vault's written conventions.
4. Suggest at most five links. For each, explain the shared idea and quote or point to the supporting passage.
5. Present a proposed frontmatter block and exact diff.

## Output
Return `summary`, `suggested title`, `suggested destination`, `tags`, `candidate links`, `open questions`, and `proposed diff`.

## Safety
Do not edit, move, retag or rename the source note. Do not invent metadata or backlinks. Wait for approval before applying the proposal.
