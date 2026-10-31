---
type: recovery-incident
tags: [astra, recovery]
status: open
date: "{{date}}"
---
# {{title}}

## Incident evidence

- Time / affected job:
- Component and version:
- Failure message without secrets:
- Last completed checkpoint:
- What work was preserved:
- User-visible effect:

## Recovery attempts

| Attempt / time | Recognized failure class | Action | Expected result | Actual result | Checkpoint resumed? |
| --- | --- | --- | --- | --- | --- |
| 1 |  |  |  | not run |  |

## Completion and review

- Retry limit and cancellation state:
- Root cause supported by evidence:
- Successful recovery evidence or remaining blocker:
- Data integrity check:
- Prevention / proposed skill change:
- Rollback artifact and actual test:
- Reviewer and closure:

Do not retry permission failures or persistent configuration errors indefinitely. Record the blocker and required action. A claimed recovery needs a completed check and preserved output.

Related: [[Astra Command Center/15 Evolution and Recovery|Evolution and Recovery]] · [[Astra Command Center/90 Templates/System Change Proposal|System Change Proposal]] · [[Astra Command Center/16 Verification Debt|Verification Debt]]
