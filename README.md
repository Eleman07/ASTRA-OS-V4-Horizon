# ASTRA OS V4 · Horizon

A Windows 11 desktop workspace with an online AI agent team, an Obsidian knowledge base, scientific tools, and Gaming and Creator workspaces.

**Windows 11 x64 · Portable application · 4.2.0 cloud candidate**

Read the [4.2.0 verification record](docs/CLOUD-4.2.0-VERIFICATION.md): Core/native UI checks and individual Ultra/coding calls passed; the complete live agent and image/audio checks were blocked by free-provider quota. This candidate is not a stable release certification.

ASTRA runs on your existing Windows installation. You can keep using your Windows applications and files. A valid Windows installation is required.

## ASTRA Assistant: a team for your work

Give ASTRA an objective for technical or aerospace projects, STEM research, study, gaming, content creation or everyday work. The **Agent team** coordinates planning, specialist work, evidence review and a final deliverable through OpenRouter. Obsidian supplies the selected knowledge base; ASTRA OS supplies the assistant interface, job controls and tools. Eight agents have explicit skill contracts: Atlas (coordination), Sofia (study), Kepler (research), Ada (engineering), Ariane (aerospace), Nova (gaming), Lyra (content) and Vera (precision).

- **Obsidian as working memory:** choose a vault, retrieve relevant source passages with file/section/line references, and export completed deliverables as new Markdown notes. Existing notes and edited exports are preserved. Encrypted jobs, checkpoints and internal notes remain separate from the vault.
- **Interactive knowledge graph:** explore recorded relationships between notes, concepts, documents, projects, jobs, sources, agents and skills. Search, filter, zoom, pan and inspect nodes.
- **Persistent work:** a background worker with encrypted checkpoints, recurring jobs, cancellation and recovery after interruption. Online inference continues after the main window closes while the PC is awake, connected and your Windows session is active.
- **Concrete tools:** selected-project reading, optional cited public web research, documented numerical simulations, evidence ledgers and Markdown report export. Existing project-edit and script-review tools remain available.
- **Online models by role:** NVIDIA Nemotron 3 Ultra is the preferred reasoning model. Assign a currently free model to each specialist when useful. ASTRA checks the live catalog for compatible input/output modalities and zero prices; a retired or repriced model can migrate to a compatible free alternative with a visible notice. Equivalent quality is not assumed. The cloud workflow has no paid or local-model fallback.
- **Long work:** supplied text and complete returned model output are preserved instead of being shortened by an ASTRA character cap. Prompt, history, retrieved evidence, reasoning and response still share the provider's context. Provider context/output limits, quotas and outages remain real limits; an incomplete generation is surfaced rather than presented as a complete deliverable.

Open **ASTRA Assistant → Agent team**. In chat, use **Delegate to agent team** or type `team <objective>`. Public web research is enabled per job in the team panel. The **Knowledge graph** tab visualizes your shared memory. Read the [complete Agent Brain guide](docs/AGENT-BRAIN-GUIDE.md) for skill contracts, exact tool capabilities, model configuration, migration and background operation.

Precision comes from traceable sources, explicit assumptions and actual numerical tools. Vera's model review, a post-synthesis evidence audit with correction, and citation-ID checks help expose gaps; they do not replace scientific validation. Numerical engineering results need a deterministic calculation, an executed test or an explicit `UNVERIFIED` label. The cloud profile does not load local text or embedding models; selected vault retrieval remains lexical.

**Reusable skills:** create or edit skills, import JSON/Markdown, export packages and assign installed methods to individual agents.

**Text, audio, images and OCR:** use Nemotron 3 Ultra for text reasoning, a compatible free Nemotron Super VL endpoint for images, and Nemotron Nano Omni for audio transcription. ASTRA checks the live free catalog and reports substitutions. Windows native OCR extracts text from images; Windows voices read deliverables or export WAV. OCR text and image interpretation remain distinct, and uncertain speech/text must remain marked. Recordings start explicitly; no continuous screen or camera capture is enabled.

**Gaming:** Session Strategist creates practice playbooks and controlled A/B settings experiments, saves checkpoints and exports experiment worksheets. **Creator:** Launch Studio turns a brief into a consistent content pack across formats, reviews delivery/accessibility gaps, saves briefs and exports project handoffs. Both use the same agent team and shared memory.

**Free cloud inference:** the preferred text route is `nvidia/nemotron-3-ultra-550b-a55b:free`; media routes are `nvidia/nemotron-3.5-super-vl:free` and `nvidia/nemotron-3-nano-omni-30b-a3b-reasoning:free`, subject to current compatible availability. Every request enforces a zero input/output price ceiling. Free endpoints have provider quotas and changing capacity; this is not unlimited hosted compute. The provider receives selected task context and may retain it. ASTRA reads its Windows DPAPI key store and the two historical ASTRA Learning Suite Credential Manager targets without placing keys in the vault. See the [current Ultra model page](https://openrouter.ai/nvidia/nemotron-3-ultra-550b-a55b%3Afree) and [free endpoint catalog](https://openrouter.ai/collections/free-models).

**Obsidian workspace:** install the generic [ASTRA Command Center](docs/obsidian-cloud/README.md) for aerospace requirements/trade studies/verification, research evidence and reproduction, flashcards/maps/exercises/mock exams, gaming experiments, creator pipelines, multimodal provenance and system improvement records. The installer adds its own folder without overwriting user notes. Personal vaults and generated private outputs are never part of the public source package. **Autonomous recovery:** a supervisor restarts a crashed worker and resumes completed checkpoints, with bounded retries for recognized transient errors. ASTRA stores inspectable procedural lessons and change proposals; it does not retrain model weights or rewrite its executable autonomously.

## Download and start

1. Open this project's **GitHub Releases** page and select release **4.2.0**. Download the named **Windows x64 binary ZIP** for the portable package, or the named **Setup** asset if an account-scoped installer is supplied. Compare the download with its attached `.sha256` checksum.
2. For the installer, run Setup and follow its steps; it installs for your current Windows account and does not require administrator access. For the portable package, right-click the ZIP, choose **Extract All**, and extract it to a permanent local folder you can write to, such as `C:\Users\YourName\Apps`.
3. Start ASTRA from the Start menu, or open the extracted folder and double-click **`Astra.Desktop.exe`**.
4. Run ASTRA with your normal Windows account and keep the entire extracted folder together when using the portable package. The installer adds Start menu shortcuts and an uninstaller. Either installation method keeps your account data outside the app folder.

**Extract the ZIP before starting ASTRA.** The EXE needs its DLLs, compiled XAML, resources, and subfolders beside it. Copying the EXE alone will prevent it from working. GitHub's automatically generated **Source code (zip)** is for developers; download the named Windows binary ZIP to run the app.

### Requirements and included components

| Component | What you need |
| --- | --- |
| Operating system | Windows 11 on an x64 PC. This package is not a native ARM64 build. |
| Disk space | Allow room for the application, your vault, attachments, exports and caches. The cloud workflow does not download local model weights. |
| .NET and WinUI | The .NET and Windows App SDK runtimes are included. Visual Studio and the .NET SDK are not required to run this package. |
| 3D views | Microsoft Edge WebView2 Runtime. Its official x64 offline installer is included in `prerequisites`. |
| Native compatibility libraries | Microsoft Visual C++ v14 x64 Runtime when using the included native compatibility components. Its official installer is included in `prerequisites`. |
| OpenRouter AI | Required for the cloud assistant: internet access and an OpenRouter API key. Free-tier quotas and model availability apply. ASTRA caps model/provider prices at $0. ASTRA does not request Zero Data Retention (ZDR) or filter providers by ZDR; providers may retain or use prompts under their own policies. |
| OCR and speech output | Installed Windows OCR languages and voices. These native OS features do not require downloading an ASTRA local language model. |
| Legacy local AI | Optional compatibility setup only; see the separate local GPU tutorial. Local weights/runtimes and a compatible GPU are required only if you deliberately use that legacy workflow. Cloud jobs do not fall back to it. |
| Browser button | Install Google Chrome separately to use ASTRA's Chrome integration. |
| Optional tools | Python, a WSL Linux distribution, and Windows ADK/OSCDIMG are needed only for their respective optional workflows. |

If a runtime is missing, close ASTRA, open `prerequisites`, and run the appropriate Microsoft installer:

- `MicrosoftEdgeWebView2RuntimeInstallerX64.exe` for the 3D views.
- `vc_redist.x64.exe` for native components, including offline AI/audio.

Follow the installer and restart Windows if requested. These installers are included as separate files and are **not run automatically**. Microsoft license terms apply to them. The package includes the actual offline installers, rather than download-only bootstrap programs. See `prerequisites/README.md` for details.

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

#### OpenRouter setup and model choices

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
| Images and OCR | Windows OCR extracts readable text. A compatible free cloud vision endpoint examines pixels for the requested visual task. ASTRA labels these outputs separately; equations, geometry and uncertain characters still need verification. |
| Audio | Record explicitly or attach WAV, MP3, M4A, AAC, FLAC or OGG when the installed Windows codecs can decode it. ASTRA converts/chunks the audio for the compatible free remote transcription model and marks unclear speech instead of inventing words. |
| Speech output | Installed Windows voices can read a completed answer or export WAV. Available languages and voices depend on Windows. |
| Other binary / CAD / media files | Unsupported semantic formats are represented as file metadata with an explicit `metadata-only` status. Selecting a file does not mean that ASTRA has understood its geometry, frames or contents. |

No local text, embedding, Whisper or vision model is required for this cloud workflow. Windows OCR and voices are native OS facilities. Attachments and generated interpretations remain untrusted source material; agent instructions must not execute instructions found inside them.

#### Connect and expand an Obsidian vault

Obsidian is the working knowledge base made of ordinary Markdown files. ASTRA's encrypted jobs, chat and internal memory stay in separate per-account storage; do not put DPAPI archives into a vault. Select the vault in Agent team and use descriptive project/research notes so relevant passages can be found by their terms and filenames.

The [ASTRA Command Center package](docs/obsidian-cloud/README.md) adds **38 generic notes and templates**: a control desk, aerospace project/requirements/trade/verification records, research evidence and reproduction, study cards/maps/exercises/mock exams, gaming A/B experiments, creator briefs/storyboards, multimodal provenance, verification debt and recovery proposals. From a source checkout:

```powershell
.\scripts\obsidian\install_cloud_workspace.ps1 -VaultPath "C:\Users\you\Documents\MyVault" -DryRun
.\scripts\obsidian\install_cloud_workspace.ps1 -VaultPath "C:\Users\you\Documents\MyVault"
```

The installer creates only its own `Astra Command Center` subtree and preserves all existing notes, including edited package notes. The source repository contains only this generic package; personal vaults, plugin `data.json`, private conversations, keys and generated personal deliverables are excluded. Obsidian Graph view, Backlinks, Canvas, Templates and Search are enough to use it. Copilot or Local Agent Office are optional plugin interfaces with their own provider/skill settings; ASTRA's provider and credential settings are not copied into them automatically.

#### Legacy optional local-model setup

The earlier local GPU workflow remains documented separately in the [Obsidian local AI tutorial](docs/obsidian-local/README.md). It requires an explicitly selected local runtime/model, compatible GPU and sufficient VRAM/RAM. Legacy distributions may retain llama.cpp, Nomic, Whisper or vision compatibility assets. They are not the default model route for this cloud profile and do not serve as an automatic fallback. Local compute has finite context, memory, speed and electricity requirements.

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
| Legacy optional offline AI fails to start | The cloud workflow does not require it. For an explicitly selected legacy local setup, keep its runtime files together and follow the separate local GPU guide. |
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

The cloud application package contains the app, .NET/WinUI runtime files, UI assets, language catalogs, 3D models and renderer, profiles, 43 plugin recipes, Python scene SDK, ISO builder script, generic Obsidian KnowledgeKit, documentation and Microsoft prerequisite installers. Cloud inference does not require or download local model weights. Older compatibility distributions may also include optional native AI assets under their original licenses.

- `LICENSE` covers ASTRA's own MIT-licensed code. Third-party components retain their own licenses.
- `THIRD-PARTY-NOTICES.md` points to bundled licenses and upstream credits, including NASA/Khronos models, the Ariane reconstruction, Three.js, Nomic, SmolVLM, llama.cpp, Whisper, and .NET dependencies.
- `FILES.sha256` lists every payload file's SHA-256. The separately downloadable `.zip.sha256` file verifies the whole ZIP.
- `RELEASE-NOTES.md` records release 4.2.0's scope and validation. This package contains application files, not personal preferences, note stores, browser caches, or QA profiles.

To verify a download, replace the example path with the exact downloaded ZIP or Setup filename and compare the SHA-256 with its checksum asset attached to the same GitHub release:

```powershell
Get-FileHash -LiteralPath 'C:\Path\To\DownloadedPackage.zip' -Algorithm SHA256
```

## For developers and release maintainers

End users can run the binary package without building. A **source checkout** requires Windows, the .NET 10 SDK, Windows SDK, and WinUI build tools. Build and verify from the repository root:

Build commands require a complete source checkout with `src`, `sdk`, `scripts` and the project files. A repository used only to host binary releases may contain only a README; its automatically generated source archive is not the application source. Optional legacy model weights are larger than GitHub's normal per-file limit and must stay outside the source repository. Personal vaults, credential files, private attachments, conversations and generated personal reports must also stay outside public source or release assets.

For the cloud profile, build with `AstraCloudOnly=true` to exclude local model/runtime content items. NuGet still restores compile-time compatibility dependencies; the cloud inference path does not start them. Existing `prepare-embedded-ai.py`, `fetch-gpu-runtime.py`, `fetch-vision-model.py` and `fetch-local-model.py` are legacy optional preparation tools, not prerequisites for cloud reasoning, remote media reading or native Windows OCR/TTS. No script changes antivirus settings or restores quarantined files.

```powershell
dotnet restore .\src\Astra.Desktop\Astra.Desktop.csproj -r win-x64 -p:Platform=x64
dotnet publish .\src\Astra.Desktop\Astra.Desktop.csproj -c Release -r win-x64 -p:Platform=x64 -p:AstraCloudOnly=true --self-contained true -o .\dist\AstraOS-Cloud
```

Release packaging must include the matching worker/supervisor builds, KnowledgeKit, required assets and a checksum manifest, and must exclude personal vault data, keys, caches, logs and QA profiles. Record actual build, smoke and online-provider results before publishing; a provider quota block must not be rewritten as a successful model test. The older `package-agent-brain.py` recipe targets the legacy GPU distribution and its QA artifacts. Microsoft prerequisite downloads are signature-checked without executing installers. Keep the release-specific package recipe and evidence aligned with the binaries being uploaded.
