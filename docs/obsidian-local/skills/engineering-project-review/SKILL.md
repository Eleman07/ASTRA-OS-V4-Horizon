---
name: engineering-project-review
description: Turn an engineering project request into explicit requirements, interfaces, risks and a reproducible verification plan.
---

# Engineering Project Review

## Method
1. Identify the user, intended operating conditions, deliverable and design revision.
2. Convert requirements into measurable acceptance criteria and note the source of each.
3. Map interfaces, inputs/outputs, units, tolerances, assumptions and dependencies.
4. Review the proposed design or code for failure modes, edge cases and compatibility.
5. Define tests, instruments/tools, expected results, rollback and evidence to retain.

## Output
Return `requirements`, `interfaces`, `assumptions`, `design review`, `risks`, `verification matrix` and `rollback`.

## Safety
Do not claim implementation, execution or certification without evidence. Never run generated code or change the project; provide a reviewable patch or plan and ask before writes.

