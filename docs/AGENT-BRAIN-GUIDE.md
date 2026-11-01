# ASTRA Assistant 4.3 — Agent Team and Second Brain

ASTRA combines an online specialist team, persistent encrypted jobs, shared memory and an interactive knowledge graph. It supports study, scientific research, engineering and aerospace work, gaming and content creation.

## Start a job

Open **ASTRA Assistant / Second Brain → Agent team**. Describe an objective and its acceptance criteria. Select specialists, or leave the selection empty for keyword-based routing. Atlas always coordinates the work, Vera reviews the drafts, and Atlas produces the final deliverable.

Optionally choose a project folder, allow public web research, select a numerical simulation and supply its parameter JSON. The simulation selector uses ASTRA's existing documented educational models. Unknown parameters are rejected. The full parameter set, assumptions and actual numeric results are recorded as evidence.

Press **Start coordinated work**. The job progresses through planning, specialists, review and synthesis. Read all handoffs and the evidence ledger in the report. Export the report as Markdown. Failed jobs can resume without regenerating completed steps. Cancellation stops the selected recurring series. ASTRA adds no commercial job-count quota; the selected Codex plan or OpenRouter service still imposes usage limits.

In the conversation window, **Delegate to agent team** submits the current objective and attachment text. Commands `team <objective>`, `agents <objective>`, `agenti <objective>` and `delega <objective>` also enqueue work. Public web research is off for chat delegation; use the team panel to enable it per job. In-app task lists and ordinary chat retain their existing interfaces.

## Specialist skill contracts

| Agent | Precise skills | Expected output |
| --- | --- | --- |
| Atlas — Coordinator | Decomposition, dependencies, acceptance criteria, synthesis | Plan and final deliverable, unresolved questions and next actions |
| Sofia — Study | Prerequisites, worked examples, active recall, spaced repetition | Explanations, examples, questions and answer key |
| Kepler — Research | Literature comparison, provenance, hypotheses, reproducibility | Evidence-based comparison, gaps and experiment plan |
| Ada — Engineering | Requirements, architecture, numerical methods, code review, tests | Concrete design/code proposals and validation cases |
| Ariane — Aerospace | Orbital mechanics, GNC, flight dynamics, propulsion trade studies, units | Equations, reference frames, assumptions, trade studies and tool-based analysis |
| Nova — Gaming | Strategy, settings, telemetry interpretation, session review | Version-specific recommendations and repeatable comparisons |
| Lyra — Creator | Briefs, scripts, storyboards, editorial planning, accessibility, delivery | Usable content drafts, storyboard and handoff checklist |
| Orion — Core | Everyday documents, organization, comparisons and real system diagnostics | Concrete documents, plans and observed system snapshots |
| Vera — Precision | Evidence and citation audit, dimensional checks, contradictions, acceptance | PASS / NEEDS WORK / UNVERIFIED criteria with supporting source IDs |

The catalog is implemented in `src/Astra.Core/Agents/AgentCatalog.cs`. Tool access is enforced by the worker. A skill is a method, not an additional operating-system permission. Project reading, memory retrieval, public web research and selected simulations are implemented tools. Generated code remains a proposal; use the existing **Projects and code** review/execution workflow when you want to apply or run it. The team does not silently execute generated shell commands.

In **Skills**, create/edit a custom method, draft it with the selected model, import JSON or Markdown, and export reusable packages. In **Agent team → Create/import skills and assign them to agents**, bind any installed skill to a specific agent or remove that binding. Each agent retains its baseline contract. Assigned methods are loaded from the shared installed-skill catalog for new inference calls; declared skill capabilities never expand the worker's actual tool permissions.

## Shared memory and graph

Internal notes, jobs, images and checkpoints use Windows DPAPI for the current account. Vault Markdown remains ordinary files under your own backup policy. Export internal notes before moving Windows accounts. DPAPI is not a sandbox against other software running as the same user.

This online edition retrieves relevant vault passages lexically, with source locations; no local embedding model is required. Completed exports are marked as reviewed AI drafts and preserve existing notes. Retrieval and graph links support traceability, not truth by themselves.

Open **Knowledge graph**, refresh, search/filter node types, zoom/pan and inspect notes, documents, concepts, projects, tasks, sources, agents and skills. The initial display batch can be expanded and is not a storage quota. Relationships reflect recorded tags, assignments and source use.

## Choose the provider

New profiles select Codex online. Existing explicit provider settings are preserved. The recommended route requires the official Codex CLI and your own ChatGPT sign-in with Codex access. Expand the Codex panel to synchronize its native account, select an available model/effort and activate it. ASTRA reads native sign-in before requests and stores no fixed account or OAuth token. A desktop account switch was not simulated. All agents share your plan's usage allowance; model names do not grant unavailable access.

Alternatively save your own OpenRouter key in AI settings, refresh the current free catalog and select **Use OpenRouter free models for assistant and agents**. Main/per-specialist choices are recorded. Only eligible zero-price routes are used, without implicit paid or local fallback. Account quotas cannot be reset by choosing another model. Recognized provider waits preserve checkpoints and retry after the provider delay. Provider availability and quality vary.

## Media and complete files

In Codex mode, pinned Whisper small/tiny transcribes speech **on CPU**. Windows voices speak/export WAV; Windows OCR extracts text and Codex can interpret pixels. OpenRouter media needs a compatible free route. Review transcription numbers and uncertain visual interpretations. Unsupported binary files are identified; selecting every extension does not make every format readable. No GPU text/vision weights are bundled in this edition.

Supplied inputs and returned outputs are preserved without an application character cap. Provider context/output, quota, storage and runtime remain finite. Complete `file:relative/path` proposals export into a fresh directory for review. They are not automatically executed or scientifically certified. Use the separate reviewed project/script workflow and actual tests to establish measured results.

## Background work, recovery and improvement

The worker can continue after the window closes while the PC is awake, connected and the Windows session remains available. A file lease avoids duplicate processing. Encrypted checkpoints survive interruptions; recurring jobs follow their interval. Cancel stops the selected series. Windows sign-in startup is an explicit option.

A supervisor restarts crashed workers. Recognized temporary failures have recovery logic; procedural learning stores inspectable validation/recovery lessons. This does not train model weights or autonomously deploy a rewritten executable. Do not report a proposed repair or model review as an executed test.

The team performs coordinated, normally sequential handoffs, not unrestricted parallel desktop control. See [4.3 verification](CODEX-4.3.0-VERIFICATION.md) for actual checks, including unchanged Codex-generated CubeSat code passing 56 tests. Those checks do not certify every task, a flight system or alternative models' performance.
