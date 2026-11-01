# ASTRA + Obsidian Cloud Workspace

This package adds a practical command center to an existing Obsidian vault. ASTRA OS supplies the assistant interface and agent orchestration; Markdown notes remain the portable knowledge base. Every shipped note is generic. Personal vault notes, `.obsidian` settings, credentials, conversations and model responses are not part of this package.

## Install without overwriting notes

Run from a complete ASTRA source checkout:

```powershell
.\scripts\obsidian\install_cloud_workspace.ps1 -VaultPath "C:\Users\you\Documents\MyVault" -DryRun
.\scripts\obsidian\install_cloud_workspace.ps1 -VaultPath "C:\Users\you\Documents\MyVault"
```

The installer requires an existing vault with an `.obsidian` directory. It creates only `Astra Command Center` and files inside it. Existing files are preserved byte for byte, including edited templates. Re-running installs missing package files and reports preserved files; it never replaces, deletes or renames a user note. Each new note is written to a temporary file inside that subtree and moved atomically without replacing an existing destination; its own temporary file is then cleaned up. Reparse points and symbolic links in the install path are rejected. Open `Astra Command Center/00 Home.md` afterward. Set Obsidian's built-in Templates folder to `Astra Command Center/90 Templates` if you want these templates as your default; an existing Templates setup can also insert them manually.

## Model and modalities

The requested primary online model is `nvidia/nemotron-3-ultra-550b-a55b:free` through OpenRouter. Check the [current OpenRouter model page](https://openrouter.ai/nvidia/nemotron-3-ultra-550b-a55b%3Afree) and [NVIDIA model card](https://build.nvidia.com/nvidia/nemotron-3-ultra-550b-a55b/modelcard) before deployment. Free endpoints have provider quotas, availability limits and changing capacity. Free pricing does not mean unlimited hosted compute or guaranteed correctness. ASTRA should keep a zero input/output price ceiling and surface quota errors rather than choose a paid model silently.

Nemotron 3 Ultra is a text input/output model. ASTRA's preferred compatible media routes are `nvidia/nemotron-3.5-super-vl:free` for image interpretation and `nvidia/nemotron-3-nano-omni-30b-a3b-reasoning:free` for audio transcription, subject to the live catalog. Windows native OCR extracts image text and installed Windows voices produce speech. Keep the transcript, OCR or vision interpretation linked to its original attachment and distinguish exact source text from uncertain extraction. The workspace's multimodal notes never claim that OCR can interpret a diagram or that a transcript proves speaker identity. A main text-model request can then use these extracted, source-linked artifacts. The workspace itself runs no local language model.

Specialists can have separate preferred free models. When an endpoint disappears, becomes priced or no longer advertises the needed modality, ASTRA's live-catalog router can substitute another compatible zero-price endpoint and record the change. A substitution does not prove equal quality. Full user text and returned output are preserved; provider context/output limits still apply, and selected vault retrieval/document extraction report their own scope and resource status.

Selected vault excerpts sent to OpenRouter leave the PC. Keep API keys in ASTRA's Windows credential store, outside the vault. Do not add a personal vault, plugin `data.json`, attachment folder, API key, or generated private report to the GitHub source repository.

## What the package adds

| Area | Useful outputs |
| --- | --- |
| Technical / aerospace | Requirements matrix, model assumptions, trade study, verification plan, risk register, decision record, reproducible calculation |
| Study | Source-grounded explanation, flashcards, linked mind map, exercises with solution separation, scored mock exam, mastery and review plan |
| STEM research | Narrow research question, evidence matrix, hypothesis and falsifier, reproducibility package, uncertainty and contradiction ledger |
| Gaming | Repeatable A/B experiment, telemetry log, strategy evidence, accessible practice progression |
| Creator | Audience brief, claims and credits review, storyboard, delivery pipeline, post-publication measurement |
| Core | Inbox triage, next actions, task acceptance criteria, decision replay, weekly synthesis |
| System evolution | Proposed skill revisions, recovery incident records, test evidence, rollback plan and verification debt |
| Multimodal | Timestamped transcript, OCR uncertainty table, source-linked image observations and diagram review questions |

These templates define output contracts; they do not execute simulations or make a model's arithmetic correct. STEM results need a deterministic calculation, a real test or an `UNVERIFIED` label. The agent may propose and review a change; changing model weights, rewriting ASTRA's executable or approving its own deployment is not implied by procedural learning or recovery.

## Start a useful task

1. Open `00 Home`, choose an area, and duplicate its template into your project folder.
2. Fill the objective, input sources, constraints and acceptance criteria before dispatching agents.
3. In ASTRA select the vault, relevant project or research area and the specialist skills. Attach only the source material needed for that task.
4. Ask for a complete deliverable with source references, assumptions, unresolved questions and a verification method. For long work, ask for numbered sections and preserve the full source references between steps.
5. Have the Reviewer assess the original evidence and actual tool/test outputs. A second model agreeing is not independent numerical verification.
6. Review the proposed file changes. Save accepted results as notes and link them to the originating work item.

Built-in Graph view, Backlinks, Canvas, Search and Templates are enough to use the pack. The `08 Study Lab` note includes a Mermaid mind map example; Canvas can arrange the same concepts visually. Community plugins such as Dataview, Tasks, Templater, Copilot or Local Agent Office are optional and use their own settings. ASTRA's encrypted credential store and UI remain separate from an Obsidian plugin's provider configuration; this package does not copy credentials into plugins.

## Validate the shipped notes

```powershell
python scripts/obsidian/validate_vault.py docs/obsidian-cloud/vault --json
```

The audit reports unresolved wiki links and duplicate filenames. YAML parsing is marked unverified if PyYAML is not installed. The installer also supports a preview without modifying the vault. Keep the installed folder under your own vault backup/version history.

See [the package validation record](VALIDATION.md) for the checks performed on the generic notes and installer. Those checks do not establish live model accuracy or hosted API availability.
