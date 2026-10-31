# Agent Playbook

Assign a precise method and deliverable to each worker. All agents must preserve source IDs, distinguish facts from proposals and report missing evidence.

| Role | Method / skill | Reviewable output |
| --- | --- | --- |
| Coordinator | Task contract, dependency plan, acceptance criteria | Plan with handoffs, inputs, checkpoints and unresolved questions |
| Aerospace | Frames, units, assumptions, governing models, deterministic calculation check | Technical analysis and verification matrix |
| Engineer | Requirements, interface contracts, patch and reproducible checks | Proposed code/files and exact test commands |
| Researcher | Search protocol, evidence comparison, contradictions, replication | Source-linked synthesis and research gaps |
| Study tutor | Prerequisites, active recall, varied practice, rubric | Explanation, cards, map, exercises and mock exam |
| Gaming analyst | Controlled comparisons, telemetry provenance, practice feedback | Experiment record and evidence-based practice plan |
| Creator | Audience brief, narrative, evidence/rights/accessibility checks | Script/storyboard and delivery checklist |
| Curator | Inbox triage, descriptive links, duplicates and provenance | Proposed note changes and graph relationships |
| Reviewer | Check original source and actual tool/test output | Explicit pass/fail criteria, issues and verification debt |

## Skills and handoffs

ASTRA's reusable skill importer uses its own supported formats. Obsidian plugins have separate agent/skill formats; a Markdown playbook does not automatically install tools or grant shell execution. The existing local starter pack includes portable skill contracts under the repository's `docs/obsidian-local/skills` directory; import only the chosen, reviewed methods through the supported interface.

Each handoff should include the current objective, exact evidence references, completed outputs and next acceptance criterion. Long tasks should preserve the full deliverable in numbered sections rather than silently replacing it with a summary.

Related: [[Astra Command Center/90 Templates/Task Contract|Task Contract]] · [[Astra Command Center/07 Evidence Ledger|Evidence Ledger]] · [[Astra Command Center/15 Evolution and Recovery|Evolution and Recovery]]
