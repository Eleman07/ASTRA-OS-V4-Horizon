# Obsidian + Local GPU AI

This is an optional, separate Obsidian workspace for people who want a Markdown vault and local GPU inference. ASTRA can import/export Markdown, but its encrypted Second Brain database is not an Obsidian vault and its DPAPI files must not be copied into one.

## Recommended free local stack

- **Obsidian** stores notes as files in a vault. Enable its built-in Graph view, Canvas, Bases, Backlinks, Search, Templates and Daily notes as needed.
- **Ollama** downloads and serves open-weight models on the PC. Local API requests do not need an API key and use `http://localhost:11434`.
- **Local Agent Office** is the recommended Obsidian team plugin for this guide. Its agent roster is made from Markdown notes, it supports Ollama and other OpenAI-compatible endpoints, squads and multi-agent brainstorming, and asks before vault writes. It has a primary provider and an optional Light provider for routing/squads/brainstorms, so you can use a larger model for direct specialist work and a smaller model for high-frequency team steps. Each agent note below gives its role a narrow, testable skill contract.
- **Copilot for Obsidian** is a convenient single-agent alternative. Its BYOK route can connect to Ollama and its custom Skills/Commands work without a Copilot license; its multi-agent feature requires Copilot Plus.
- **Agentic Chat** is an optional experimental alternative with local Ollama, `SKILL.md` agent-plugin packages and delegated subagents. Its built-in subagent is a read-only Explorer; it does not currently provide a user-configurable roster of specialist subagent roles.

No cloud model, paid subscription, API token or cloud search is needed for the recommended Ollama + Local Agent Office route. Hardware, power, model quality, context size and generation speed remain finite. Several local agents make several model calls; they do not create extra GPUs or run continuously while the PC is asleep. The PC used for the AstroX setup does not have Ollama installed, so install it before following the Ollama commands below, or configure a local OpenAI-compatible endpoint that is already running.

## 1. Install a model and confirm GPU use

Install the [Ollama Windows application](https://ollama.com/download/windows), then open a new PowerShell window. A practical two-model setup for the 16 GB RTX 5080 Laptop is:

```powershell
# Primary model for technical work, code and complex agent tasks.
# Ollama lists this package at about 14 GB, before context/KV cache.
ollama pull gpt-oss:20b
ollama run gpt-oss:20b

# Light provider for routing, brainstorm/squad steps, drafting and study notes.
ollama pull llama3.1:8b
ollama run llama3.1:8b
```

In Local Agent Office, set `gpt-oss:20b` as the primary provider/model and `llama3.1:8b` as the Light provider/model. The plugin documents that the Light provider is used for brainstorm, squads and routing while 1:1 chat can keep the primary model. It may call both models during one workflow. On this 16 GB GPU, do not assume both remain loaded at once; check the `PROCESSOR` column in `ollama ps`, and use a smaller context or let Ollama swap/unload models if VRAM is tight. For research PDFs or very long text, you can try `mistral-nemo:12b` instead of Llama 3.1; its published context is an upper limit, not a promise that it fits in available VRAM. Do not use `gpt-oss:20b` and another model concurrently unless `ollama ps` and Task Manager show that there is enough VRAM headroom. Qwen is omitted from the AstroX profile as previously requested.

Treat model benchmarks as narrow comparisons: a coding or math score does not establish that a model is more accurate than GPT-4/Claude across aerospace, research, writing and every other task. For the aerospace agents, require units, assumptions and a deterministic calculation or label numerical output `UNVERIFIED`; a second LLM agreeing with the first is not an independent proof.

Exit the chat with `/bye`. Start one model at a time, then run:

```powershell
ollama ps
```

Check the `PROCESSOR` column. `100% GPU` means Ollama placed the loaded model on the GPU. A mixed CPU/GPU value means some layers were offloaded; CPU fallback can be much slower. Also watch VRAM in Task Manager or `nvidia-smi`. The advertised model context is a maximum, not a sensible default for every laptop: a larger context consumes more memory. Begin with Ollama's default context, raise it only while memory stays available, and lower it if the GPU offloads or becomes unstable. A tool-capable model is needed for agent tool calls.

### Use ASTRA's bundled GPT-OSS server without Ollama

The ASTRA workspace also contains a GPU-enabled `llama.cpp` server and the GPT-OSS 20B GGUF. To expose that local model to Local Agent Office or Copilot, run this from the repository root:

```powershell
.\scripts\obsidian\start_gpt_oss_local_endpoint.ps1 -DryRun
.\scripts\obsidian\start_gpt_oss_local_endpoint.ps1
```

The launcher first checks whether the expected GPT-OSS model is already running on loopback. If so, it reuses that process and verifies its context instead of asking for more VRAM or starting a duplicate. If no matching server exists, it requires at least 13,800 MiB free, binds only to `127.0.0.1:11435`, keeps the model on the GPU, uses a 32,768-token context, and waits for the model endpoint to become ready. Prompt, retrieved notes, reasoning and answer all share this context; the Obsidian agent output cap is left at provider default (`0`). A shorter existing context is reported clearly and must be restarted once. Logs go under `logs/obsidian-gpt-oss`. If the bundled server or model is elsewhere, pass `-ServerPath` and `-ModelPath`. In Local Agent Office, add an **OpenAI-compatible** provider with model ID `gpt-oss:20b`, a blank API key, and base URL `http://127.0.0.1:11435/v1`; select it as the active provider. In Copilot BYOK, use the same base URL and model ID, leaving the optional API key blank. Stop the background server using the PID printed by the script (`Stop-Process -Id <PID>`). This direct endpoint serves GPT-OSS only; use the Ollama setup above if you want Ollama to switch between GPT-OSS and a smaller Light model.

Official references: [Ollama local/OpenAI-compatible endpoints](https://github.com/ollama/ollama/blob/main/docs/api/introduction.mdx), [GPU support](https://github.com/ollama/ollama/blob/main/docs/gpu.mdx), [context and GPU-offload checks](https://github.com/ollama/ollama/blob/main/docs/context-length.mdx), [GPT-OSS](https://ollama.com/library/gpt-oss), [Llama 3.1](https://ollama.com/library/llama3.1), [Mistral Nemo](https://ollama.com/library/mistral-nemo), and [Local Agent Office](https://community.obsidian.md/plugins/local-agent-office).

### Precision check performed on local GPT-OSS

On an RTX 5080 Laptop (16 GB), the bundled llama.cpp server loaded the local GPT-OSS 20B GGUF onto the GPU and exposed an OpenAI-compatible endpoint on loopback. A synthetic circular-orbit check showed why a role prompt alone is not an accuracy guarantee: the aerospace specialist answered **5,815.0 s** instead of **5,828.5 s**, and the reviewer incorrectly marked a different value as a pass. When the model called a deterministic calculator tool with the same inputs, it returned **5,828.5 s / 97.1 min**, matching the calculation. This is a narrow smoke test, not a general model benchmark.

Local Agent Office supports specialist notes, squads/brainstorms and approval-gated vault writes, but its documented feature set does not provide a general-purpose calculator execution tool. The included aerospace/reviewer skills therefore require `UNVERIFIED` for numeric claims unless a deterministic calculation result is supplied. Use a separate calculator/MCP/tool integration for verified mathematics; do not infer precision from using multiple agents.

## 2. Install the multi-agent plugin and starter agents

1. Create a new Obsidian vault, for example `SecondBrain`. Keep the vault in a normal user-writable folder.
2. Create folders such as `00_Inbox`, `10_Projects`, `20_Research`, `30_Creator`, `40_Gaming`, `50_Agents`, `60_Conversations`, `70_Templates`, and `90_Archive`.
3. Copy the Markdown files in [`starter-vault/`](starter-vault/) into the vault root. They provide a linked home page, profile hubs, evidence conventions and ready-to-fill templates; replace the sample folder guidance with your own structure if needed.
4. Open **Settings → Community plugins → Browse**, install **Local Agent Office**, then enable it.
5. In **Settings → Local Agent Office**, add an **Ollama** provider, set the server to `http://localhost:11434`, select the exact model tag shown by `ollama list`, and leave the API key blank for local inference. Choose `gpt-oss:20b` for the primary provider and `llama3.1:8b` for the Light provider if you use the two-model profile above.
6. Point **Agents folder** at `50_Agents`, **Conversations folder** at `60_Conversations`, and **Agency output folder** at a review area such as `10_Projects/_AI-Proposals`. Add only the note folders that are relevant as context folders.
7. Copy the `.md` files in [`agents/`](agents/) to `50_Agents`. Each file becomes a separate agent with its own skills, rules, memory and note connections. Use **Validate agents** from the plugin before starting.
8. Run a read-only question first, then **Multi-agent brainstorm** or **Run squad** on sample notes. Keep the default write approval enabled and inspect each diff before applying it.

### Existing vault layouts

For an existing vault, select its real agents, conversation, review and knowledge folders in the plugin settings. Copy the generic agents and skills from this starter pack into new folders without replacing personal notes. The local-only route requires a running loopback model server; the current ASTRA cloud workflow is documented separately in [the cloud guide](../obsidian-cloud/README.md).

To see the knowledge graph immediately, enable Obsidian's core **Graph view** and open the `Second Brain Home` note. The starter notes use profile tags such as `#area/research`, `#area/aerospace`, `#area/creator` and `#area/gaming`; use the Graph filters to focus on a profile. The graph visualizes explicit note links and does not claim that every suggested relationship is meaningful.

Local Agent Office describes itself as local-first, with no telemetry; note contents are sent only to the provider configured by the user. Keep the provider set to a local loopback endpoint, do not add cloud providers, and leave optional web/search services off for a local-only workflow. The plugin stores provider settings in its vault `data.json`; keep that file out of public backups and Git repositories.

## 3. Connect Copilot or use reusable SKILL.md files

### Copilot for Obsidian

For a lightweight local chat, install **Copilot** from Community Plugins, then open **Settings → Copilot → BYOK → Add a provider**. Choose Ollama or a custom OpenAI-compatible provider, use base URL `http://localhost:11434/v1`, add the exact model ID (for example `gemma4:12b`) and save. A local API key is not required. Select the model for the available agent and add **Skills** or **Commands** in Copilot's own settings. If Quick Chat reports a browser CORS error, Copilot offers a CORS compatibility switch; its documentation says this disables streaming for that route.

Copilot is useful for vault Q&A and a single local agent. Its local BYOK setup is free, but its multi-agent answer feature is behind Copilot Plus. Use Local Agent Office for a custom free team.

### Portable skill files

The [`skills/`](skills/) directory contains standalone `SKILL.md` files for coordination, study retrieval, research synthesis, aerospace and engineering reviews, creator packs, gaming experiments, vault curation and evidence review. Copilot V4 discovers skills placed under `.agents/skills/<skill-name>/SKILL.md` without a license; it also scans `copilot/skills`, `.opencode/skills` and `.claude/skills`. The `SKILL.md` files are not automatically injected into Local Agent Office, so keep each worker's method in its own agent note as well. The skills only provide instructions: they do not grant shell access or make unverified calculations correct. See the [current Copilot Skills settings](https://docs.obsidiancopilot.com/settings/) and the [Agentic Chat Agent Plugins specification](https://tardigrde.github.io/obsidian-agentic-chat/) for their own supported formats.

Agent cards for Local Agent Office are in `agents/`; reusable skills are in `skills/`. Keep these formats distinct: Local Agent Office reads an agent note as a worker, while plugins that implement `SKILL.md` load a skill as reusable instructions.

## 4. Profile-specific Obsidian plugins

Start with Obsidian's built-in Graph view, Canvas, Bases, Backlinks, Search, Templates and Daily notes. Set the core Templates plugin folder to `70_Templates` to use the included templates, including the evidence-aware `Flashcard.md` template. Choose the local team plugin (Local Agent Office) or the convenient one-agent Copilot route. Add only the plugins useful for the current profile; they are community code running inside Obsidian, so review the publisher, license and permissions before installing.

| Profile | Useful community plugins | What they add |
| --- | --- | --- |
| **Core / Second Brain** | [Local Agent Office](https://community.obsidian.md/plugins/local-agent-office) **or** [Copilot](https://community.obsidian.md/plugins/copilot); [Dataview](https://community.obsidian.md/plugins/dataview); [QuickAdd](https://community.obsidian.md/plugins/quickadd); [Templater](https://community.obsidian.md/plugins/templater-obsidian); [Tasks](https://community.obsidian.md/plugins/obsidian-tasks-plugin); [Omnisearch](https://community.obsidian.md/plugins/omnisearch) | Local agents or vault chat, metadata dashboards, capture macros, repeatable note templates, task queries and full-text search. |
| **STEM / Research / Aerospace** | Core set plus [Zotero Integration](https://community.obsidian.md/plugins/obsidian-zotero-desktop-connector) (requires Zotero and Better BibTeX), [Latex Suite](https://community.obsidian.md/plugins/obsidian-latex-suite), [Excalidraw](https://community.obsidian.md/plugins/obsidian-excalidraw-plugin), [Dataview](https://community.obsidian.md/plugins/dataview) | Bibliography/citation capture, faster equations, system diagrams and structured research/project tables. Record units, assumptions, source links and validation status in every technical note. |
| **Creator** | Core set plus [QuickAdd](https://community.obsidian.md/plugins/quickadd), [Templater](https://community.obsidian.md/plugins/templater-obsidian), [Excalidraw](https://community.obsidian.md/plugins/obsidian-excalidraw-plugin), [Tasks](https://community.obsidian.md/plugins/obsidian-tasks-plugin) | Capture briefs quickly, create consistent scripts/storyboards, map ideas visually and track review, caption, credit and delivery tasks. Obsidian's built-in Slides plugin can present Markdown notes. |
| **Gaming** | Core set plus [Dataview](https://community.obsidian.md/plugins/dataview), [Templater](https://community.obsidian.md/plugins/templater-obsidian), [Tasks](https://community.obsidian.md/plugins/obsidian-tasks-plugin), [Tracker](https://community.obsidian.md/plugins/obsidian-tracker), [Periodic Notes](https://community.obsidian.md/plugins/periodic-notes), and optionally [CardBoard](https://community.obsidian.md/plugins/card-board) | Session logs, build/strategy databases, patch and practice checklists, trend charts and campaign/project boards. Use actual telemetry; do not infer FPS or latency from utilization. |

For tabletop role-playing games, the community [Initiative Tracker](https://community.obsidian.md/plugins/initiative-tracker) is a separate optional profile plugin. It is not needed for PC/console gaming.

## 5. Safe self-repair and improvement loop

Use the provided skills as a proposal-review cycle:

1. Audit the selected notes and generate a report of missing frontmatter, broken links, duplicate concepts and uncertain metadata.
2. Ask the Curator for exact proposed edits and links, not blanket auto-edits. Keep original wording and source text.
3. Ask the Reviewer to compare every proposal with the original note and linked evidence.
4. Inspect the plugin's diff/approval card and apply only the changes you accept.
5. Store a useful, reviewed lesson as a **proposed** skill revision. Test it on a copy or sample note, then update the active skill yourself and keep a backup/version history.

For code, ask the Engineering agent to produce a patch and a reproducible test command. Review code before running it; use a disposable project or a proper OS sandbox for untrusted scripts. Do not give an LLM an unrestricted shell/MCP server or let it execute newly generated code in an unlimited retry loop. Local Agent Office approval gates vault mutations; an agent's local connection does not make arbitrary code safe.

ASTRA has bounded recovery for recognized temporary worker failures, resumes completed checkpoints, and records inspectable procedural lessons. It does not train model weights, modify its executable or silently replace installed skills. Models and agents can still make mistakes, so review technical, scientific and destructive changes.

## 6. Read-only vault utilities

The repository includes two local Python helpers. Both read Markdown notes and print results; neither edits the vault.

```powershell
# Validate links and frontmatter; require these fields for this vault's convention.
python scripts/obsidian/validate_vault.py "C:\Users\you\Documents\SecondBrain" `
  --required-field tags --json

# Find candidate evidence passages with file and line references.
python scripts/obsidian/search_vault.py "C:\Users\you\Documents\SecondBrain" orbital period --limit 12
```

`validate_vault.py` uses PyYAML when it is already available to the Python environment. Without it, the report clearly marks YAML syntax and field checks as unverified instead of pretending they passed. Review the report, then ask an agent to propose a diff and approve changes through the plugin. `search_vault.py` is a simple lexical finder, not semantic search; the selected agent may still misread or miscite a passage, so open the source note before reusing a claim.

The local agents are on-demand desktop workflows. They do not run continuously 24/7 or keep generating while the PC is asleep. The plugin UI itself was not exercised in the ASTRA build environment; verify your installed Obsidian/plugin versions before relying on the setup.
