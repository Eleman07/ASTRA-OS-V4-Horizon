# Aerospace Studio

Start a technical project with a [[Astra Command Center/90 Templates/Aerospace Project|project brief]], then connect requirements, assumptions, alternatives, tests and decisions. Change the discipline as needed: GNC, ADCS, orbital mechanics, structures, propulsion, thermal, avionics or mission analysis.

## Project register

| Project note | Selected discipline | Current decision / milestone | Model fidelity | Next verification |
| --- | --- | --- | --- | --- |
|  |  |  |  |  |

## Specialist workflow

1. Coordinator translates the objective into acceptance criteria and evidence gaps.
2. Aerospace agent states frames, epochs, units, governing equations and model fidelity.
3. Engineer proposes reproducible code and interfaces; preserve seed, dependencies and exact inputs.
4. Reviewer checks dimensional consistency, deterministic tool results and the original evidence.
5. Record accepted outputs with [[Astra Command Center/90 Templates/Verification Matrix|Verification Matrix]] and a [[Astra Command Center/90 Templates/Decision Record|Decision Record]].

Use [[Astra Command Center/04 Requirements and Verification|Requirements and Verification]] and [[Astra Command Center/05 Trade Studies and Risks|Trade Studies and Risks]] throughout the project. Operational or flight qualification is a separate evidence claim; a successful educational simulation alone does not establish it.

## Good agent request

> Design a reproducible analysis plan for the selected GNC problem. State reference frames, assumptions, equations, input provenance, comparison baselines and pass/fail tolerances. Produce code proposals and test commands. Mark every uncomputed numerical value UNVERIFIED and list the evidence required to accept the design.

Related: [[Astra Command Center/06 Research Observatory|Research Observatory]] · [[Astra Command Center/07 Evidence Ledger|Evidence Ledger]] · [[Astra Command Center/16 Verification Debt|Verification Debt]]
