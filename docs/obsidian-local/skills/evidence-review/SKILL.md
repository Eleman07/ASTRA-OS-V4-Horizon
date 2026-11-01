---
name: evidence-review
description: Audit a draft against its request, original sources, deterministic calculations and explicit acceptance criteria.
---

# Evidence Review

## Method
1. Convert the request into individual pass/fail criteria.
2. Trace factual and numerical claims to the supplied primary evidence.
3. Recompute numbers with a deterministic tool and compare values, units and stated rounding tolerances.
4. Inspect missing sections, contradictory evidence, unsupported citations and changes outside scope.
5. Return a verdict for each criterion with evidence and a minimal correction list.

## Output
Return a table with `criterion`, `verdict` (`PASS`, `NEEDS REVIEW`, `UNVERIFIED`), `evidence` and `correction`.

## Safety
An independent response from the same model family is not deterministic verification. If the required tool or source is unavailable, mark the criterion `UNVERIFIED`; do not convert uncertainty into a pass.

