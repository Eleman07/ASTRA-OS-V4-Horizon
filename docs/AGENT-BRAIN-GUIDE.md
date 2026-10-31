# ASTRA Assistant 4.1 — Agent Team and Second Brain

ASTRA combines an on-device specialist team, persistent encrypted jobs, shared memory and an interactive knowledge graph. It supports study, scientific research, engineering and aerospace work, gaming and content creation.

## Start a job

Open **ASTRA Assistant / Second Brain → Agent team**. Describe an objective and its acceptance criteria. Select specialists, or leave the selection empty for keyword-based routing. Atlas always coordinates the work, Vera reviews the drafts, and Atlas produces the final deliverable.

Optionally choose a project folder, allow public web research, select a numerical simulation and supply its parameter JSON. The simulation selector uses ASTRA's existing documented educational models. Unknown parameters are rejected. The full parameter set, assumptions and actual numeric results are recorded as evidence.

Press **Start coordinated work**. The job progresses through planning, specialists, review and synthesis. Read all handoffs and the evidence ledger in the report. Export the report as Markdown. Failed jobs can resume without regenerating completed steps. Cancellation stops the selected recurring series. There is no commercial job-count or daily-request quota.

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
| Vera — Precision | Evidence and citation audit, dimensional checks, contradictions, acceptance | PASS / NEEDS WORK / UNVERIFIED criteria with supporting source IDs |

The catalog is implemented in `src/Astra.Core/Agents/AgentCatalog.cs`. Tool access is enforced by the worker. A skill is a method, not an additional operating-system permission. Project reading, memory retrieval, public web research and selected simulations are implemented tools. Generated code remains a proposal; use the existing **Projects and code** review/execution workflow when you want to apply or run it. The team does not silently execute generated shell commands.

In **Skills**, create/edit a custom method, draft it with the local model, import JSON or Markdown, and export reusable packages. In **Agent team → Create/import skills and assign them to agents**, bind any installed skill to a specific agent or remove that binding. Each agent retains its baseline contract. Assigned methods are loaded from the shared installed-skill catalog for new inference calls; declared skill capabilities never expand the worker's actual tool permissions.

## Shared memory and graph

Notes migrate automatically to per-note Windows DPAPI encryption. Migration retains the original encrypted archive for recovery and preserves note identities. New storage has no 1,000-note cap and no aggregate 16 MB notebook cap. Existing PDF source files are untouched. Records remain tied to the Windows account; use exports when moving accounts.

The worker combines lexical retrieval with local model embeddings for semantic note search. Vectors are content-addressed, cached encrypted and reused. The first semantic search indexes existing note passages and can take time; disable **Semantic note search** to use lexical retrieval alone. A dedicated Nomic Embed Text v1.5 Q8 model runs on the GPU and uses search_query/search_document role prefixes. Retrieval is supporting evidence, not a truth check. PDF passage search remains lexical. Finished deliverables are saved to shared memory as explicitly labeled reviewed AI drafts, with source references.

Open **Knowledge graph → Open / refresh graph**. Search, filter node types, zoom, pan and inspect notes, documents, concepts, projects, sources, tasks, agents and their skills. The renderer displays 300 matching nodes initially; **Show more** expands the view. This is a display batch, not an archive quota. Links show recorded tags, assignments, skills and source use; the graph does not invent conceptual relationships. Refresh after new work completes. The canvas has keyboard navigation and a selectable node list.

## Background operation and compute

`Astra.AgentWorker.exe` is a separate on-device process. It continues after the main ASTRA window closes. A per-profile file lease prevents duplicate workers from processing the same queue. Jobs and step checkpoints are encrypted and survive restarts. A repeat interval schedules a new job after completion; 0 runs once. A supervisor restarts crashed workers and retries recognized transient failures up to three times with increasing delay. Other failed jobs remain available for explicit retry. No busy loop generates tasks without an objective.

Use **Local compute and background operation** to choose a local GGUF model, context size, output tokens and inference threads. Local generation requires the included Vulkan GPU backend and a compatible GPU driver. CPU fallback is disabled. Optionally enable the free online model in Agent team: task context is sent to OpenRouter, with a zero input/output price ceiling and GPU fallback on provider errors or quotas. Larger models and contexts require more RAM and processing time. One generation runs at a time in the worker; specialist roles share the loaded weights. This avoids loading eight copies of the model. A second model may be loaded by ordinary interactive chat. The worker unloads idle model weights after ten minutes. Settings apply to new inference calls.

Enable **Start worker when I sign in to Windows** if desired. Disable the worker toggle to stop processing and release the worker. The PC must be powered on, awake and signed into the Windows account; this is not a service that operates while the computer is off. No paid API subscription is required, but hardware, electricity, storage, context and model capability are finite. Optional free online inference retains provider quotas and requires a saved key. Windows Credential Manager targets openrouter@ASTRA Learning Suite and ASTRA Learning Suite are supported alongside the DPAPI key file.

## Precision and verification

Every report retains specialist outputs and actual source/tool records. Unknown bracketed source IDs are flagged. Vera is a second model pass, not an independent scientist or experimental validation. Numerical precision comes from deterministic tools and checking assumptions, units and results. Scientific or aerospace conclusions still require appropriate domain validation.

The release includes core regression results, focused agent tests, a local-model worker test and native UI/graph checks. See `AGENT-BRAIN-VERIFICATION.json` in the release assets for the checks actually executed.

## Build

Build the existing WinUI desktop project and publish the worker to the same distribution folder:

```powershell
dotnet run --project tests/Astra.Core.SmokeTests/Astra.Core.SmokeTests.csproj -c Release
dotnet publish src/Astra.Desktop/Astra.Desktop.csproj -c Release -p:Platform=x64 -r win-x64 --self-contained true -o dist/AstraOS-Agent-Brain
dotnet publish src/Astra.AgentWorker/Astra.AgentWorker.csproj -c Release -r win-x64 --self-contained true -o dist/agent-worker-publish
Copy-Item dist/agent-worker-publish/Astra.AgentWorker.* dist/AstraOS-Agent-Brain/
python scripts/package-agent-brain.py
```

Model weights, native backend libraries and offline prerequisites are included in the end-user binary distribution, not in Git source archives. Existing resource preparation instructions in the main README apply.

## One assistant, voice and vision

ASTRA Assistant is the single assistant in Core, STEM, Gaming and Creator. Second Brain is its shared memory, not a separate product. Chat automatically retrieves relevant shared notes and PDF passages.

In the team panel, record a voice objective or attach an audio file. Whisper transcribes locally into editable text; review it before starting work. Read a selected final deliverable aloud, stop playback or export WAV. The conversation window also supports recording, voice responses and audio export. Recording starts only after pressing Record.

Attach images for actual local pixel analysis through the included **SmolVLM-256M-Instruct Q8** model and its matching f16 projector, distributed under Apache 2.0 from the pinned ggml-org conversion. The worker stores attachments encrypted, interprets the image locally and shares labeled visual interpretations with the specialists. Ordinary conversation attachments and pasted images also receive local vision analysis alongside OCR. The lightweight model can make visual mistakes; choose a compatible larger GGUF/projector pair in compute settings for stronger image understanding. The app does not continuously capture the screen or camera. Vision processes still images; it does not claim to watch video.

## Gaming and Creator additions

Gaming includes a **Session Strategist** for personalized practice plans, accessibility preferences, saved session checkpoints, a controlled A/B settings experiment and an exportable experiment worksheet. Plans refer to available telemetry and never invent FPS or latency.

Creator includes a **Launch Studio** for turning one brief into scripts, carousels, posts and newsletters, with consistent claims, storyboard, captions, alt text and delivery checks. Save briefs to shared memory, request a prioritized accessibility/delivery review and export a project handoff snapshot with the actual asset audit and storyboard. These workflows queue specialist work in the same assistant.

GPU inference uses a pinned official llama.cpp runtime bound to authenticated `127.0.0.1`, with a temporary session key. A GPU is required for local generation and semantic embeddings; failure is reported rather than silently using CPU. Numerical simulation reports include deterministic facts formatted directly from executed tool records.

## Open model choices and GPU execution

The default text model is Ministral 3 8B Q4_K_M (5.20 GB, Apache-2.0). Download buttons also offer Microsoft Phi-4 14B Q4_K (9.05 GB, MIT) and OpenAI gpt-oss-20b MXFP4 (12.11 GB, Apache-2.0). Each download is pinned by repository revision, exact size and SHA-256, resumes byte ranges after interruption and verifies the complete file before activation. A custom compatible GGUF remains selectable. Weights are downloaded once into the account data directory. Model size is not a guarantee of accuracy.

Local generation, vision and Nomic embeddings use the GPU. Whisper and Windows voices/OCR are auxiliary local services and may use CPU. The included llama.cpp server is authenticated on 127.0.0.1 and tied to its owner process. The first Vulkan GPU is selected; on the verified RTX 5080 Laptop system this is the dedicated NVIDIA GPU. GPU memory and context are finite. The worker runs specialists sequentially with shared weights.

The tested hybrid configuration uses Nomic locally and NVIDIA Nemotron 3 Ultra 550B on OpenRouter's free endpoint. Refresh the live free catalog and select a specific model; availability and quotas can change. No paid fallback is allowed. Local GPU inference can continue after cloud failures. Cloud model reviews still require original sources and real tool evidence.

## Obsidian and procedural learning

Import an existing Markdown vault without changing source files. Export notes into the ASTRA Second Brain subfolder with YAML metadata, tags, aliases and wiki links. A manifest preserves existing edited files and writes conflicts separately; repeated import is idempotent. No Obsidian subscription is required.

Auto-learn stores a small catalog of inspectable validation methods based on recognized citation/numerical/recovery events. Show learned methods to inspect them. These are prompt methods, not trained weights or executable self-modification. Auto-repair restarts processes and resumes checkpoints; missing drivers or incompatible models require attention after bounded retries.

A final evidence audit checks the synthesized deliverable against original records, with one correction pass and re-audit. Malformed verdicts or unresolved findings keep an explicit EVIDENCE REVIEW REQUIRED label. This is an additional model review, not a mathematical proof or a guarantee of factual accuracy. Numeric simulation facts are also displayed directly from executed tool outputs.
