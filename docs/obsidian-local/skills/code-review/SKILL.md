---
name: code-review
description: Review selected source code, propose the smallest justified patch and define checks without running generated code.
---

# Code Review

## Method
1. Read only the selected repository/folder and its relevant project instructions.
2. Restate expected behavior and cite the affected files/functions.
3. Identify a concrete defect or requirement gap with a minimal proposed diff.
4. Specify tests and expected outcomes; label them `not run` unless an execution record exists.
5. Describe compatibility and rollback considerations.

## Output
Return prioritized findings first, then the proposed patch, test plan, expected result and remaining uncertainty.

## Safety
Do not execute code, shell commands, installers or MCP tools. Do not claim a build or test passed unless the user/approved runner produced evidence. Ask before writing files or accessing data outside the selected project.
