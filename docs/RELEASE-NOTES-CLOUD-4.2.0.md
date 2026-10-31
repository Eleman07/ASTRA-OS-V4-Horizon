# ASTRA OS Horizon 4.2.0 — Cloud agents and Obsidian

Windows 11 x64 · October 31, 2026 · Cloud candidate

This is the current downloadable 4.2.0 cloud build. It retains candidate status: the complete live multi-agent and online image/audio tests encountered free-provider HTTP 429 and must be repeated after quota reset. It is not a claim of unrestricted free inference or complete scientific validation.

## Downloads and installation

- **ASTRA-OS-V4-HORIZON-Setup-4.2.0-cloud-win-x64.exe**: account-scoped cloud installer. The ASTRA setup is unsigned; it does not run Microsoft prerequisites automatically.
- **ASTRA-OS-V4-HORIZON-4.2.0-cloud-win-x64.zip**: portable application. Extract the entire ZIP, then run `ASTRA-OS-V4-HORIZON/Astra.Desktop.exe`. Keep its DLLs, XAML/resources and folders together.
- Matching **.sha256** files identify both downloads; the portable package also contains a per-file SHA-256 inventory.
- **README.md**, **CLOUD-4.2.0-VERIFICATION.md** and these release notes describe configuration, actual checks and known limitations.
- GitHub's automatic **Source code** archives contain developer source rather than the runnable Windows application.

.NET and WinUI runtimes are included. Optional, unmodified Microsoft WebView2 and Visual C++ x64 offline installers are in `prerequisites`; both signatures and hashes were verified during packaging. Run them only if the corresponding runtime is missing. No prerequisite installer ran during packaging.

## Assistant and models

- OpenRouter is the assistant inference backend; NVIDIA Nemotron 3 Ultra is the preferred reasoning/coordinator model.
- North Mini Code is the verified optional coding specialist. NVIDIA Super VL and Nano Omni provide the configured image/audio routes; Windows native OCR and installed Windows voices provide distinct extraction and speech-output features.
- Local language, embedding and vision-model fallback is disabled in the cloud workflow. This package includes no local model weights.
- Every remote call checks compatible modalities and advertised zero prices in the live catalog. A removed or repriced preference can migrate to a compatible free family/alternative with an inspectable notice. Equivalent quality is not assumed, and account/provider access can still fail.
- Supplied text, stored conversation, specialist handoffs and returned output are preserved. Response requests use the provider's advertised model maximum; length-limited answers can continue without discarding previous output. Provider context/output limits, quotas and outages still apply.

## Workflows, files and Obsidian

- Redesigned agent workspace with eight deliverable-oriented workflows, a long-form composer, selected scientific area, precise specialist skills/model assignments, progress, results and an evidence ledger.
- Workflows cover technical/aerospace projects, study flashcards/maps/exercises/mock exams, scientific research, gaming, content creation, everyday tasks, knowledge synthesis and evolution/recovery proposals.
- Source-linked retrieval supports Markdown, selectable-text PDF, Office/OpenDocument files, EPUB, notebooks, code and structured text/data. Unknown formats remain explicitly metadata only. Office formulas/macros are not executed. Malicious-container/XML protections remain explicit failures.
- A selected Obsidian vault supplies relevant source passages and graph relationships. Completed deliverables can be exported as new notes; existing notes and edited exports are preserved.
- The included Command Center is a **generic 38-note/template kit**. No personal vault, `.obsidian` settings, credentials, conversations or local QA profiles are included.
- Worker checkpoints, recognized transient-failure recovery and reusable procedural lessons remain available. ASTRA does not retrain model weights or silently rewrite its executable.

## Setup

1. Save or reuse your OpenRouter API key in ASTRA settings. ASTRA stores it encrypted for the current Windows account; it does not place it in the vault.
2. Open **ASTRA Assistant → Agent team → Obsidian · sources and result publishing**, then connect your existing vault.
3. Choose a workflow, complete **What should ASTRA deliver?**, attach sources and start coordinated work.

Prompts, relevant vault passages and selected media sent online are processed under OpenRouter/provider policies. ASTRA does not require Zero Data Retention. Free endpoint pricing does not remove provider quotas.

## Actual verification

- Full Core smoke suite passed, including **77 agent/knowledge/routing checks**.
- **47 Python static tests**, source validation and C# string checks passed.
- **10 native Windows UI checks** passed, including long objective/source preservation and cloud-only configuration.
- **12 isolated setup/uninstall checks** passed: every installed payload hash, per-user registration, shortcuts, version and removal were verified; an unrelated user file was preserved. ASTRA and prerequisite installers were not launched by this packaging check.
- A **2.1-million-character document** and encrypted chat messages beyond 100,000 characters were preserved in full.
- Individual real Ultra evidence-preservation/study/research and North Mini Code coding-availability calls passed. These are narrow synthetic checks, not general model benchmarks.
- **Not passed:** the complete live aerospace/creator/gaming multi-agent benchmark and online image/audio probes were blocked by HTTP 429. Inkling was listed free but access was forbidden for this account/provider; it is not a preselected specialist.
- The generic vault kit had no unresolved wiki links or read errors; YAML parsing was not run because PyYAML was unavailable.
- The executable remains unsigned. Cached dependencies were used when the NuGet index was unavailable; a fresh online vulnerability audit was not completed.

See **CLOUD-4.2.0-VERIFICATION.md** for the exact scope. The independent stable-release certification gate remains incomplete.
