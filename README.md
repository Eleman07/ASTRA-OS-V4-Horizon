# ASTRA OS V4 · Horizon

A Windows 11 workspace with **ASTRA Assistant: one Second Brain, nine specialist agents, 39 reusable skills, Obsidian memory and an interactive knowledge graph**.

**4.3.0 · Windows 11 x64 · Per-user EXE installer and portable ZIP**

## Choose your AI provider

**The recommended, tested Codex workflow requires your own ChatGPT account with Codex access and the official Codex CLI installed and signed in.** ASTRA does not include an account or a subscription. The account enables the chosen models; accuracy comes from relevant sources, explicit assumptions, deterministic tools and executed tests, not from signing in alone. All agents share your plan's usage allowance. Model availability and context limits still apply.

**Alternatively, use OpenRouter's free models with your own OpenRouter account and API key.** Select this provider explicitly in ASTRA. Requests enforce zero input/output token prices and a zero-price provider ceiling; incompatible/unavailable routes fail or try another eligible free route. There is no automatic paid fallback. Free endpoints have rate/daily limits, changing availability and model-dependent quality. Switching models cannot reset an account-wide quota. The Codex CubeSat result below does not certify equivalent OpenRouter performance.

ASTRA's application code is free and MIT licensed. Hosted providers are not unlimited compute, and a ChatGPT subscription may cost money. Refer to [Codex authentication](https://learn.chatgpt.com/docs/auth), [Codex usage/pricing](https://learn.chatgpt.com/docs/pricing), [app-server integration](https://learn.chatgpt.com/docs/app-server) and [OpenRouter limits](https://openrouter.ai/docs/api_reference/limits) for current terms. This is an independent local/open-source integration, not a hosted Codex service.

## Download and start

1. Open [the latest GitHub release](https://github.com/Eleman07/ASTRA-OS-V4-Horizon/releases/latest). Download **ASTRA-OS-V4-HORIZON-Setup-4.3.0-codex-win-x64.exe** and its `.sha256` file. The installer is unsigned; compare its SHA-256 before running it. It installs for your current Windows account, with Start menu entries and an uninstaller.
2. Close ASTRA and its background worker before updating the same installation directory. Existing account data stays outside the application folder. Setup does not bundle any personal vault, login, API key or fixed account.
3. Install the [official Codex CLI](https://learn.chatgpt.com/docs/cli) separately. Sign in using **ChatGPT**, then check `codex login status`. ASTRA discovers the CLI on PATH or in the official desktop installation. The CLI and its sign-in are not redistributed by ASTRA.
4. Open **ASTRA Assistant → Agent team → Codex · account, modello e disponibilità**. Synchronize the account, select a model available in your own plan and its reasoning effort, then choose **Use Codex for assistant and agents**. The default model identifier is editable; it does not grant access to a model your account lacks.
5. Select your Obsidian vault, choose a workflow, supply your objective and acceptance criteria, attach source material and start coordinated work.

For OpenRouter instead, save your own API key in ASTRA's AI settings, expand **Choose your model → OpenRouter · online models and credentials**, refresh the current free catalog, select a model and choose **Use OpenRouter free models for assistant and agents**. Advanced provider settings also let you explicitly switch providers. Keys use Windows account-bound encrypted storage; they are never placed in the vault. ASTRA reads Codex's native sign-in before requests; it does not store a fixed email, password or OAuth token. Switching desktop accounts was not simulated in testing.

The portable ZIP is an alternative: **extract the whole ZIP**, then launch `Astra.Desktop.exe`. Copying the EXE alone will not work. GitHub's automatic source ZIP is for developers.

Requirements: Windows 11 x64, internet for online reasoning, enough disk space for the app, CPU Whisper checkpoints, prerequisites and your own files. .NET/Windows App SDK are included. Microsoft WebView2 and Visual C++ offline installers are in `prerequisites`, for separate user installation if needed; Setup does not run them automatically. No local text/vision model or GPU inference is required for Codex/OpenRouter. **Whisper small/tiny uses CPU** in Codex mode. Optional local GPU configuration is separate and depends on compatible weights, runtime and finite VRAM.

## What ASTRA Assistant can do

| Area | Agents and concrete outputs |
| --- | --- |
| Study | Sofia: explanations, source-linked summaries, flashcards, mind maps, exercises, worked solutions and mock exams |
| Technical / aerospace | Ada + Ariane: requirements matrices, assumptions/units/frames, architecture, equations, complete proposed source files, numerical simulations, verification cases, trade studies and technical reports |
| STEM research | Kepler: source comparisons, evidence matrices, reproducibility procedures, hypotheses, uncertainty and research gaps; optional public-web research |
| Gaming | Nova: practice playbooks, session reviews and controlled A/B experiments; actual CPU/GPU-engine/RAM measurements where available, without invented FPS or temperatures |
| Creator | Lyra: briefs, scripts, storyboards, editorial plans, platform variants, subtitles, asset/handoff and accessibility checks |
| Core / everyday | Orion: documents, organization, comparisons, planning and real system snapshots within implemented tool access |
| Coordination / precision | Atlas integrates the work; Vera audits claims, sources, units and acceptance criteria, including unresolved or UNVERIFIED results |

The agents run coordinated handoffs through the selected provider. They are not nine unrestricted OS agents. Their precise installed methods are reusable: **create, import JSON/Markdown, edit, export and assign skills** in the Skills tab. A skill does not grant extra tool permissions.

The Second Brain unifies conversations, encrypted internal notes/jobs, source-linked Obsidian retrieval, project context, reusable methods and a visual graph. Search/filter/inspect related notes, documents, concepts, sources, projects, agents and skills. Completed deliverables can become new notes while preserving existing notes. It is an external knowledge and work system, not a human brain, perfect memory or a guarantee of correct reasoning. Obsidian retrieval is lexical; no embedding model is required.

**Text, audio and images:** attach text/source/code, supported documents and tables, PDFs, audio and images. Windows OCR extracts text, Codex interprets pixels, CPU Whisper transcribes speech, and Windows voices read answers/export WAV. Review transcripts, especially scientific numbers and names. OpenRouter media uses compatible free routes with their quotas. Unsupported/proprietary binary files are reported with available metadata; support for selecting a file does not guarantee semantic interpretation of every format.

**Long tasks:** ASTRA preserves supplied text and full returned output without an app character cap. Provider context, output, quota, storage and runtime limits remain. Generated file proposals export into fresh project directories for review; they are not silently executed. Use reviewed execution and actual tests to verify them.

**Background work and recovery:** the worker can continue after the main window closes while the PC is awake, signed in and connected. Encrypted checkpoints preserve progress. Recognized provider waits retry automatically; a supervisor can restart a crashed worker. Enable sign-in startup explicitly if desired. Inspectable procedural lessons can improve future methods; ASTRA does not train model weights or autonomously deploy a rewritten executable. It cannot work while the PC is off or a provider has no available quota.

## Observed verification

A genuine CubeSat software project was generated by the selected Codex model through five handoffs, without receiving a reference implementation. **All 56 generated-project tests passed** after source review; the generated sources were unchanged. An independent CSV calculation confirmed energy conservation and RK4 convergence. Nominal sampled position error was about **1.72 cm against the ideal analytical two-body orbit**, not against a real spacecraft. J2 remains a documented extension. This was a functional test, not a personal project bundled with the release.

Core checks, native long-input checks, actual Codex vision/OCR, CPU speech, TTS and crashed-worker recovery were also exercised. Read the [verification scope and limitations](docs/CODEX-4.3.0-VERIFICATION.md). No model is guaranteed to solve every complex task accurately. Precision requires checking the original evidence and measured results.

The [Agent Brain guide](docs/AGENT-BRAIN-GUIDE.md) and [Second Brain guide](docs/SECOND-BRAIN-GUIDE.md) describe memory, skills and tools. The generic [Obsidian Command Center kit](docs/obsidian-cloud/README.md) includes profile templates; personal vaults and generated private outputs are excluded from source and binary releases.

## Tutorial: full-screen desktop experience

### 1. Enable automatic taskbar hiding in Windows 11

**Recommended: turn on “Automatically hide the taskbar” for the desktop experience.**

1. Press **Windows + I** to open Windows Settings.
2. Select **Personalization → Taskbar**.
3. Expand **Taskbar behaviors**.
4. Enable **Automatically hide the taskbar**.
5. Move your pointer away from the taskbar. To reveal it again, move the pointer to the taskbar edge, normally the bottom of the screen.

You can turn this off at any time in the same menu. This is a Windows setting you choose yourself; extracting or starting ASTRA does not change it. Microsoft documents the option in [Customize the taskbar in Windows](https://support.microsoft.com/en-us/windows/experience/personalization/customize-the-taskbar-in-windows).

### 2. Make ASTRA open full screen

1. In ASTRA, select the settings icon in the bottom dock, or open the **ASTRA logo → Personalizza** menu. The page heading is **Personalize ASTRA** in English; some navigation labels retain their original names.
2. In **FULL SCREEN AND START**, enable **Start ASTRA on full screen**.
3. Use **F11** to switch between full screen and a window. **Esc** leaves full screen.

The preference is saved for your Windows account. You can also start this copy once in a specific mode from PowerShell, while inside its extracted folder:

```powershell
.\Astra.Desktop.exe --fullscreen
.\Astra.Desktop.exe --windowed
```

### 3. Start ASTRA when you sign in

1. Put the extracted ASTRA folder in its permanent location first.
2. On the same personalization page, enable **Start ASTRA on Windows login**.
3. Open **Windows Settings → Apps → Startup**, and check that ASTRA is enabled if it appears there.
4. Leave **Start ASTRA on full screen** enabled if you want the desktop experience at each sign-in.

ASTRA starts after you sign in to your account. If you move the app folder, turn the login option off and back on from the new copy. To stop automatic startup, turn the option off before deleting or relocating the app.

## Tutorial: set up your workspace

### Home, files, and appearance

- **Home** displays your real Windows Desktop files and folders. Double-click an item, or select it and press Enter. The context menu lets you refresh or open the complete File Explorer view.
- Hover over the dock's icons to see their names. Select the **ASTRA logo** for quick access to files, profiles, widgets, multitasking, the assistant, and settings. This menu is also useful in a narrow window.
- Open application windows appear after the dock divider, with their app icons and a running indicator. Each window has its own button; hover to read its original title, click to bring it forward, or right-click to minimize it. Minimized windows remain available, and activating a maximized window keeps it maximized.
- The running-app buttons stay inside ASTRA's dock. When another app is in front, it has the full screen area available. The list refreshes approximately every two seconds, or four seconds in Eco mode. Window titles stay in memory and are not saved or sent anywhere. Desktop files and app buttons use native Windows Shell/window icons, including shortcut icons and a visible fallback when an icon is unavailable.
- On the personalization page, choose your five-color palette, wallpaper, and banner. Animated GIF images are supported. The banner and module cards adapt to the window size.
- Theme-colored Windows cursors are optional. Preview them, apply them explicitly, and use **Restore** on the same page to return to the saved previous set.
- **Orbital 3D** places your Desktop entries around an animated central sphere that follows your theme. Activate the scene when you want to use it.

### Profiles and multitasking

1. Open **Profiles** from the logo menu or sidebar. Your Windows identity and user-folder path are shown there.
2. Choose **Core**, **STEM**, **Gaming**, or **Creator**. For STEM, choose Engineering, Physics, Mathematics, or Science, then an engineering specialization if applicable.
3. Open **Multitasking and sessions**. Select the real windows you want to arrange and choose two columns, three columns, or a grid. Use the restore action to undo that arrangement.
4. Save a session with a profile, study path, layout, applications, and folders. Resume it later, then select the windows to arrange after the applications have opened.

### STEM: simulations, 3D models, and Python projects

1. Set your study path in Profiles, then open **STEM Studio**. The simulation list is filtered for that path.
2. Choose a numerical model, inspect its assumptions and units, adjust its parameters, and run it. Results remain available as a chart and exportable data.
3. Select a library model or import a **GLB**, a **glTF with embedded resources**, **OBJ**, or **STL** file. Imports are limited to 64 MB and one million vertices/triangles. External glTF resources and OBJ MTL files are not loaded; a self-contained GLB is the simplest choice.
4. Rotate the model, inspect wireframe or section views, and replay the calculated samples with the timeline. A mesh does not automatically supply mass, material properties, or a complete physics model.
5. For aerospace, select **Ariane 64 (ESA)**. This is an original ASTRA educational reconstruction based on public dimensions, with credits; it is not official ESA CAD or an ESA-endorsed product.

The 13 numerical models cover orbital dynamics, attitude control, flight mechanics, aerodynamics, beams, oscillation, heat diffusion, chemistry, circuits, spectra, queues, ecology, and nonlinear dynamics. They are educational tools; they are not a complete CFD/FEM solver or certified engineering analysis.

To visualize your own Python results, install Python separately and open the included `python-sdk/README.md`. Its standard-library toolkit exports points, lines, and surfaces as scene JSON. You can try the included examples from the extracted app folder:

```powershell
cd python-sdk
python examples.py
```

Import a generated scene into STEM Studio. You can also map CSV/JSON columns to 3D axes or follow changes to a scene file. ASTRA does not automatically execute an imported Python program.

Model credits and source links are in `Graphics3D/web/models/CREDITS.md` and are accessible from the Studio.

### ASTRA Assistant, skills and Obsidian

1. Open **ASTRA Assistant → Agent team**, or use **Ctrl + Alt + Space** for the conversation window.
2. Choose a task workspace: technical/aerospace project, study, STEM research, gaming, creator, core work or system improvement. Select the research/study area and describe the required output, source material and acceptance criteria.
3. Select your Obsidian vault. Retrieval brings relevant excerpts into the evidence ledger with source locations; it excludes plugin settings, credentials, virtual environments and generated dependencies. Enabling vault export creates completed results under `ASTRA/Deliverables` as model-reviewed drafts while preserving existing notes and edited exports.
4. Attach text, documents, audio or images, then choose the specialist skills and model preferences. Review extraction status: an OCR result, a vision interpretation and file metadata are different kinds of evidence.
5. Start the task and follow the plan, specialist handoffs, review, evidence audit and final deliverable. Review the original sources and actual tool outputs before accepting an engineering or scientific result.

#### Optional advanced local GPU configuration

1. Open **ASTRA Assistant → Agent team → Choose your model** and select **Local GPU**. The user chooses the model; ASTRA does not replace it silently.
2. Select an approved model and download its pinned weights, or choose an existing GGUF. SHA-256 and publisher/base provenance are checked before inference. OpenAI and Microsoft are US publishers; Mistral AI is French. Chinese publisher/base models and unknown origins are excluded from the curated catalog.
3. Set a context that fits your GPU and the model's native window. The observed RTX 5080 Laptop 16 GB startup test loaded gpt-oss-20b at 65,536 tokens with all 25 layers offloaded; this is a loading check, not a completed scientific-project benchmark. Phi-4's listed checkpoint has a smaller native window.
4. Connect your Obsidian vault, assign agent skills and submit the actual project objective. Files proposed by the model are exported into a new directory for review. Only an observed execution can mark their tests as passed.

There is no ASTRA daily-request quota or paid token meter in local mode. You provide the GPU, storage and electricity. Hardware capacity, model context and accuracy remain finite. Read the [model guide](docs/LOCAL-GPU-MODELS.md) before choosing weights.

#### Optional OpenRouter setup

1. Create an account at [OpenRouter](https://openrouter.ai/) and an API key on its **Keys** page. In ASTRA's AI settings save the key for OpenRouter. It is encrypted with Windows DPAPI for your account, outside the vault and preferences. Existing historical ASTRA Learning Suite Credential Manager entries are also supported.
2. Use **NVIDIA Nemotron 3 Ultra** as the preferred reasoning model, and select a currently free model for a specialist role when appropriate. The preferred ID is `nvidia/nemotron-3-ultra-550b-a55b:free`; the live catalog is the source of current availability and capabilities.
3. The media readers prefer **Nemotron 3.5 Super VL** for images and **Nemotron 3 Nano Omni** for audio. Ultra itself accepts text; ASTRA sends an image or recording through the appropriate compatible route and passes the extracted, source-linked result to the working agents.
4. Refresh free models to inspect current choices. If a preferred endpoint disappears, becomes priced or lacks the required modality, the router selects a compatible zero-price alternative from the current catalog and records the substitution. It does not assume that a replacement has identical quality.
5. Keep the cloud workflow selected for this setup. Cloud inference enforces a $0 prompt/completion price ceiling, has no paid fallback and does not start a local model when a provider is unavailable.

Requests send selected task context and attached material to OpenRouter and its serving provider. ASTRA does not request Zero Data Retention (ZDR) or filter providers by ZDR eligibility; current provider retention/use terms apply. API keys remain in Windows credential storage, outside exported reports and the Obsidian vault. Free endpoints retain request, provider, context and availability limits; ASTRA cannot turn a hosted free tier into unlimited compute. Check the [current free endpoints and limits](https://openrouter.ai/collections/free-models). Quota and authentication failures remain visible.

#### Long inputs and outputs

ASTRA preserves supplied user text and complete returned model output instead of silently cutting them to an application character budget. The provider's context and completion metadata determine the available capacity. Input, system instructions, retrieved evidence, conversation history, reasoning and final output share the context; a large advertised context is not an independent allowance for each. Output `0` uses the provider-advertised completion allowance instead of an arbitrary ASTRA response cap. A truncated response is treated as incomplete and the task can continue from its checkpoint.

Vault retrieval selects relevant passages rather than uploading every file. Document adapters also have explicit resource guards and return `partial`, `resource-limit` or `metadata-only` status when appropriate. For a large research corpus, retain original files, index by question/section and preserve references between task stages. Ask for complete numbered deliverable sections and review their coverage against the task contract.

#### Text, audio, images and file adapters

| Input / output | Processing and evidence |
| --- | --- |
| Text and code | User-supplied text is retained. Plain text, Markdown, common source/config/data formats and notebook cells can be extracted without executing code. |
| PDF | The document adapter extracts selectable page text with page labels. Scans and image equations require OCR/vision; figures are not inferred from extracted text alone. The separate PDF library supports page indexing, progress and cancellation. |
| Office / OpenDocument / EPUB | DOCX, PPTX, XLSX, ODT, ODP, ODS and EPUB adapters extract supported text/cells with section references. They do not execute macros, recalculate spreadsheet formulas or interpret every embedded object/layout. |
| Images and OCR | Windows OCR extracts readable text. The verified local SmolVLM reads pixels on GPU; explicit online mode can instead use its reviewed compatible free route. ASTRA labels these outputs separately; equations, geometry and uncertain characters still need verification. |
| Audio | Record explicitly or attach WAV, MP3, M4A, AAC, FLAC or OGG when the installed Windows codecs can decode it. ASTRA converts/chunks audio for local OpenAI Whisper, retaining chunk/segment timing and language. Transcripts are editable and are not guaranteed word-perfect. Explicit online mode uses its separate provider. |
| Speech output | Installed Windows voices can read a completed answer or export WAV. Available languages and voices depend on Windows. |
| Other binary / CAD / media files | Unsupported semantic formats are represented as file metadata with an explicit `metadata-only` status. Selecting a file does not mean that ASTRA has understood its geometry, frames or contents. |

Local mode uses selected text weights and the verified speech/vision assets. No embedding model is required for lexical vault retrieval. Windows OCR and voices are native OS facilities. Attachments and generated interpretations remain untrusted source material; agent instructions must not execute instructions found inside them.

#### Connect and expand an Obsidian vault

Obsidian is the working knowledge base made of ordinary Markdown files. ASTRA's encrypted jobs, chat and internal memory stay in separate per-account storage; do not put DPAPI archives into a vault. Select the vault in Agent team and use descriptive project/research notes so relevant passages can be found by their terms and filenames.

The [ASTRA Command Center package](docs/obsidian-cloud/README.md) adds **38 generic notes and templates**: a control desk, aerospace project/requirements/trade/verification records, research evidence and reproduction, study cards/maps/exercises/mock exams, gaming A/B experiments, creator briefs/storyboards, multimodal provenance, verification debt and recovery proposals. From a source checkout:

```powershell
.\scripts\obsidian\install_cloud_workspace.ps1 -VaultPath "C:\Users\you\Documents\MyVault" -DryRun
.\scripts\obsidian\install_cloud_workspace.ps1 -VaultPath "C:\Users\you\Documents\MyVault"
```

The installer creates only its own `Astra Command Center` subtree and preserves all existing notes, including edited package notes. The source repository contains only this generic package; personal vaults, plugin `data.json`, private conversations, keys and generated personal deliverables are excluded. Obsidian Graph view, Backlinks, Canvas, Templates and Search are enough to use it. Copilot or Local Agent Office are optional plugin interfaces with their own provider/skill settings; ASTRA's provider and credential settings are not copied into them automatically.

#### Obsidian plugins with local models

Use the [Obsidian local AI tutorial](docs/obsidian-local/README.md) for Copilot, Local Agent Office and profile-specific plugins with local Ollama/OpenAI-compatible endpoints. You choose the model and endpoint in each plugin; ASTRA does not copy credentials or provider settings into a vault. The [current GPU model guide](docs/LOCAL-GPU-MODELS.md) lists verified non-Chinese choices and explains VRAM/context planning. Local compute has finite context, memory, speed and electricity requirements; it removes hosted request quotas, not physical limits.

#### Skills, memory and reviewed changes

The **Skills** tab supports installing, importing JSON/Markdown, editing, exporting and assigning methods to individual agents. A skill supplies instructions and an output contract; it does not grant unrestricted tool execution. System improvement uses a proposal, reproducible checks, review, rollback evidence and an inspectable procedural lesson. Checkpoint recovery and learned procedures do not change model weights or silently deploy a replacement executable.

**Web research** searches the public web and adds cited sources on request. **Projects and code** reads a selected project and proposes precise edits. Review changes before applying them; originals have encrypted backups. Script execution has its own interpreter/arguments review and consent. Scripts run with your account permissions, without an OS sandbox.

Core, STEM, Creator and Gaming use one **ASTRA Assistant** identity, shared conversations and the selected knowledge sources. Chats and internal notes are encrypted for the current Windows account; Markdown vault notes remain ordinary files under the vault's own backup policy. Export important internal notes before changing Windows accounts or reinstalling Windows because DPAPI data is account-bound. The interface includes English and Italian and follows the Windows display language; explicit requests can choose the response language.

### Widgets and automation

Open **Widget and automations** to add a clock, system monitor, note, countdown, focus timer, tasks, workspace shortcut, or automation. Use the editor to customize a widget, or import/export a JSON widget pack. Up to 32 widgets are supported.

Automations can run on an interval or a daily schedule and perform supported in-app actions, such as reminders, navigation, focus timers, or notes. **ASTRA must remain open**, including in the background. Imported automations start disabled until you enable them. Declarative widgets do not execute arbitrary external scripts.

The **Plugin SDK** also includes 43 declarative plugins. Open that section to browse and enable them; examples are included under `recipes`.

### Gaming and Creator

- **Gaming:** label each run with a game, repeatable scene, resolution, and preset. Record actual system CPU, GPU engine utilization where available, and RAM, then compare it only with the previous matching setup. The encrypted history retains up to 50 sessions; export raw CSV and open the Session HUD for live status and measured-resource warnings. It does not measure FPS, frame time, temperature, or network latency, and cannot guarantee visibility over exclusive full-screen games.
- **Creator:** build a local media board with metadata and image-color palettes; order shots into a timed storyboard, add alt text and credits, and check aspect ratio, suggested resolution, subtitle presence, and handoff notes before export. Export Markdown, CSS, or a delivery preflight report. Subtitle Lab imports SRT, adjusts timing offset and scale, and writes a new file. ASTRA checks metadata only, does not inspect video frames or certify rights, and leaves original media intact.
- **Windows Tools:** browse already installed applications and system shortcuts across seven categories. Chrome integration uses your installed Chrome; changing Windows default associations is done through Windows Settings.

### Optional Ubuntu and ISO tools

**Ubuntu Lab** requires WSL and an installed Linux distribution. If it reports that no distribution exists, use its guided install action, complete any Windows restart and Ubuntu first-run account setup, then check again. The terminal opens separately. See Microsoft's [WSL installation guide](https://learn.microsoft.com/en-us/windows/wsl/install).

**ISO Builder** is an optional workflow for creating customized Windows installation media from legitimate Windows files you provide. It requires Windows ADK/OSCDIMG and explicit build steps. You do not need an ISO to run ASTRA, and the builder does not automatically clone your current PC. Windows installation media and a Windows license are not included.

## Performance and shortcuts

On a new profile, ASTRA selects lighter settings for devices with up to 8 GB RAM or four logical processors. Existing saved preferences are respected. Use the light/Eco settings and reduced motion to lower animation and 3D work. 3D scenes load on demand and suspend or release resources when unused; the AI model is optional. The policy has been tested, but this is not a performance guarantee for every PC.

| Shortcut | Action |
| --- | --- |
| F11 | Toggle ASTRA full screen |
| Esc | Leave full screen |
| Ctrl + K | Open ASTRA search |
| Ctrl + Alt + Space | Open the assistant, when the shortcut is available |
| Alt + F4 | Close the active window |

## Troubleshooting, updates, and removal

| Problem | What to do |
| --- | --- |
| The EXE does not start or resources are missing | Extract the complete ZIP again into a new folder. Check that `Astra.Desktop.pri`, `App.xbf`, `MainWindow.xbf`, DLLs, and subfolders remain beside the EXE. |
| A native runtime DLL is reported missing | Run the included `prerequisites/vc_redist.x64.exe`, then restart ASTRA. |
| A 3D panel cannot initialize | Run the included WebView2 installer if the runtime is missing, restart ASTRA, then activate the scene again. Installing Chrome alone does not provide WebView2. |
| OpenRouter returns HTTP 429 | Check [OpenRouter Activity](https://openrouter.ai/activity) and the current account/provider quotas. Wait for capacity or the quota reset. Model migration is for compatible catalog availability, not a way to bypass provider limits; the cloud workflow does not start paid or local inference. |
| A preferred free model was removed or repriced | Refresh the live catalog and inspect the replacement notice and effective model. Only compatible zero-price endpoints are eligible; if none exist, the request stays blocked. |
| A file says partial, resource-limit or metadata-only | Open the extraction status and original source. Split oversized source material or choose an appropriate OCR/vision reader; unsupported binary geometry or embedded objects have not been interpreted. |
| Local model fails to start | Check the selected GGUF checksum, GPU driver, runtime files, model context and free VRAM. ASTRA reports missing GPU offload instead of silently using CPU or cloud. |
| WSL says no distribution is installed | Complete Ubuntu installation and its first-run setup; WSL itself and a Linux distribution are separate requirements. |
| The taskbar remains visible | Check the Windows auto-hide option described above, then use F11 in ASTRA. Move your pointer away from the taskbar edge. |
| Login startup points to an old folder | Disable and re-enable the login option from the new ASTRA copy, then check Windows Startup settings. |
| Windows displays a download/reputation prompt | Verify the download source, compare its SHA-256 with the release checksum, and follow your device's security policy. This release's ASTRA executable is not code-signed. |

Your preferences, notes, widgets, and caches are stored separately under:

```text
%LOCALAPPDATA%\ASTRA\OS
```

Startup diagnostics are written to:

```text
%LOCALAPPDATA%\AstraOS\Logs\startup.log
```

To update the portable version, close ASTRA and extract the new release into a separate folder. For the installed version, close ASTRA, download and run the new Setup, and select the same installation folder. Your per-account data remains in its existing location. If you enabled login startup, disable it in the old portable copy and enable it in the new one.

To remove the installed version, use **Settings → Apps → Installed apps → ASTRA OS V4 Horizon → Uninstall**, or its Start menu uninstaller. For the portable version, disable login startup and delete its extracted app folder and shortcuts. If you applied an ASTRA cursor scheme, restore the previous scheme first. Your separate user data is retained unless you remove it yourself; export important notes first.

## Package contents, credits, and verification

The release includes the matching Desktop, Worker and Supervisor, assets, 39 skill definitions, graph, generic KnowledgeKit, Python scene SDK, CPU Whisper small/tiny checkpoints, Microsoft prerequisites and license notices. `cloud-build-manifest.json` retains its historical filename and now identifies the 4.3 Codex/optional-OpenRouter package. It records payload sizes and SHA-256 hashes. Setup also includes `INSTALLER-FILES.sha256`. The `.exe.sha256` and `.zip.sha256` assets verify complete downloads.

Personal vaults, credentials, account settings, browser caches, QA profiles and private generated results are excluded. Graphics and library attribution are under `licenses`, `Ai/components` and `Graphics3D/web/models`. No agency or provider endorsement is implied. The installer is unsigned and this tested candidate is not an independent security or engineering certification.

```powershell
Get-FileHash -LiteralPath 'C:\Path\To\DownloadedSetup.exe' -Algorithm SHA256
```

## For developers and release maintainers

Use a complete source checkout on Windows with .NET 10 SDK, Windows SDK and WinUI build tools. Codex CLI and provider accounts remain separate user installations. Source archives exclude large checkpoints; prepare speech and Microsoft prerequisites before packaging:

```powershell
.\scripts\fetch-whisper.ps1
.\scripts\fetch-release-prerequisites.ps1
.\scripts\build-online-release.ps1
python .\scripts\package-codex-release.py
python .\scripts\build-cloud-installer.py --flavour codex --version 4.3.0 --payload dist/AstraOS-Online-4.3.0 --output dist/ASTRA-OS-V4-HORIZON-Setup-4.3.0-codex-win-x64.exe
```

Installer compilation also requires the verified NSIS 3.13 portable compiler and archive under `downloads/installer-tools`; the builder checks its pinned official archive hash. Run `scripts/test-cloud-installer.py --flavour codex --build <installer-build.json>` for isolated install/uninstall checks. A virtual environment is not an OS sandbox. Never include your vault, native Codex login, API key, private job records or personal output in release files. All published claims must correspond to observed tests; a quota block is not a passed model test.

For source development without model weights, publish Desktop/Worker/Supervisor with `-p:AstraCloudOnly=true`. The advanced local GPU recipes are separate optional builds, not a requirement or the default of this online release.
