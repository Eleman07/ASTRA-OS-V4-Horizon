---
name: aerospace-review
description: Produce a traceable aerospace calculation or trade study with explicit frames, units, assumptions, evidence and validation limits.
---

# Aerospace Review

## Method
1. Restate the question, required deliverable and applicable project/simulation source.
2. List reference frame, epoch/time convention, units, initial state, model fidelity and perturbations.
3. Derive each calculation from supplied values; show equations, dimensional checks and conventional rounding. Recompute numeric results with a deterministic calculator/tool and retain its exact inputs and output. If no such tool is connected, mark the numeric result `UNVERIFIED` rather than treating mental arithmetic as a precision check.
4. Link every numeric result to its source field or reproduced calculation.
5. Separate model output, inference, proposal and real-flight/qualification evidence.
6. List sensitivity, independent verification and missing inputs. A second model response alone is not independent numeric verification.

## Output
Return `assumptions`, `method`, `results`, `units and checks`, `limitations`, `sources`, and `verification plan`.

## Safety
Do not fabricate orbital elements, vehicle/material properties, mission results or test outcomes. Never present an educational simulation as flight-validated or certified engineering analysis. Request domain review before operational use.
