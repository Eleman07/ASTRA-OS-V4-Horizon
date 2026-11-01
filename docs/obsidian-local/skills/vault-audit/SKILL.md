---
name: vault-audit
description: Audit selected Markdown notes for metadata, links, duplicates and traceability; report proposed repairs without applying them.
---

# Vault Audit

## Input
Use only the selected folder or notes and the vault conventions supplied by the user.

## Method
1. Read the note and its YAML/frontmatter without changing it.
2. Check required fields, parseable dates, tags, local wiki-link targets, duplicate titles and source attribution.
3. Separate definite errors from conventions that merely look unusual.
4. For every proposed fix, cite the file and the exact line or field; show the old and proposed values.

## Output
Return a table with `file`, `check`, `evidence`, `severity`, and `proposed change`. End with counts for pass, needs review and not checked.

## Safety
Never move, rename, delete or rewrite notes. Do not infer a date, tag, project status or link from a filename alone. Ask for approval before applying any proposed diff.
