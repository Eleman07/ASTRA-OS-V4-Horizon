# Evolution and Recovery

ASTRA can reuse reviewed procedures and recover recognized temporary worker failures. A proposed improvement needs a reproducible failure, a concrete change, meaningful checks and a rollback plan.

## Improvement backlog

| Proposal note | Observed problem | Expected behavior | Evidence / regression check | Review status | Accepted version |
| --- | --- | --- | --- | --- | --- |
|  |  |  |  | proposed |  |

Use [[Astra Command Center/90 Templates/System Change Proposal|System Change Proposal]] for a feature or skill revision. Use [[Astra Command Center/90 Templates/Recovery Incident|Recovery Incident]] for a real failure and its recovery.

## Safe learning loop

1. Capture the failure and exact environment without secrets.
2. Propose the smallest change and state what could regress.
3. Test on a copy, fixture or isolated project with expected/actual outputs.
4. Have a reviewer inspect evidence and a person accept deployment.
5. Store the accepted procedural lesson with the test and version.

Automatic retries must be bounded, cancellation must persist and permission failures must remain visible. Procedural lessons are inspectable instructions; they do not retrain model weights or authorize an agent to rewrite its executable. Never record an untested rollback as successful.

Related: [[Astra Command Center/16 Verification Debt|Verification Debt]] · [[Astra Command Center/17 Agent Playbook|Agent Playbook]] · [[Astra Command Center/90 Templates/Decision Record|Decision Record]]
