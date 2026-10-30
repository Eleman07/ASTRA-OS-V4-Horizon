# ASTRA OS V4 · Horizon

A customizable desktop workspace for Windows 11, with OpenRouter AI, an optional offline model, STEM tools, a 3D desktop, widgets, and Gaming and Creator workspaces.

**Windows 11 x64 · Portable application · Release 4.0.0.5**

ASTRA runs on your existing Windows installation. You can keep using your Windows applications and files. A valid Windows installation is required.

## Download and start

1. Open this project's **GitHub Releases** page. Download **`ASTRA-OS-V4-HORIZON-Setup-4.0.0.5-win-x64.exe`** for the account-scoped installer, or **`ASTRA-OS-V4-HORIZON-openrouter-gaming-creator-win-x64.zip`** for the portable package. The release also provides a `.sha256` checksum for each file.
2. For the installer, run Setup and follow its steps; it installs for your current Windows account and does not require administrator access. For the portable package, right-click the ZIP, choose **Extract All**, and extract it to a permanent local folder you can write to, such as `C:\Users\YourName\Apps`.
3. Start ASTRA from the Start menu, or open the extracted folder and double-click **`Astra.Desktop.exe`**.
4. Run ASTRA with your normal Windows account and keep the entire extracted folder together when using the portable package. The installer adds Start menu shortcuts and an uninstaller. Either installation method keeps your account data outside the app folder.

**Extract the ZIP before starting ASTRA.** The EXE needs its DLLs, compiled XAML, resources, and subfolders beside it. Copying the EXE alone will prevent it from working. GitHub's automatically generated **Source code (zip)** is for developers; download the named Windows binary ZIP to run the app.

### Requirements and included components

| Component | What you need |
| --- | --- |
| Operating system | Windows 11 on an x64 PC. This package is not a native ARM64 build. |
| Disk space | Allow about 4 GB for the download and extraction, plus room for your own files and caches. |
| .NET and WinUI | The .NET and Windows App SDK runtimes are included. Visual Studio and the .NET SDK are not required to run this package. |
| 3D views | Microsoft Edge WebView2 Runtime. Its official x64 offline installer is included in `prerequisites`. |
| Native AI/audio libraries | Microsoft Visual C++ v14 x64 Runtime. Its official installer is included in `prerequisites`. |
| OpenRouter AI | A free OpenRouter account and API key are required for the default cloud model route. Free-tier quotas and model availability apply. ASTRA caps model/provider prices at $0. It does not request Zero Data Retention (ZDR) or filter providers by it; providers may retain or use prompts under their own policies. |
| Optional offline AI | The bundled text model, Whisper speech model, and native runtime allow local inference without Ollama or a local server. 8 GB RAM or more is recommended for comfortable use; performance depends on the PC. |
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

### ASTRA Assistant, skills and Second Brain

1. Open the assistant from ASTRA, or press **Ctrl + Alt + Space** while using another application.
2. Type a request, choose files, paste selected text explicitly, or start an audio recording. The assistant receives the foreground application's title when called; you choose the content to provide.
3. Choose an action such as explain, summarize, make a checklist, translate, extract JSON, or analyze an error.
4. Review the result and export text, Markdown, JSON, WAV speech, SRT subtitles, a note, a scene, or a widget proposal as appropriate for the action.

#### OpenRouter AI setup (default)

1. Create a free account at [OpenRouter](https://openrouter.ai/) and create an API key in your account's **Keys** page. ASTRA never asks for OpenRouter account passwords.
2. In ASTRA, open **Second Brain / AI settings**. Paste the key into **OpenRouter API key** and select **Save key and use OpenRouter**. ASTRA encrypts the key with Windows DPAPI for your Windows account; it is not written to preferences or logs.
3. Keep **Automatic free model · zero cost only** selected for the recommended route. To inspect fixed choices, select **Refresh free models**; ASTRA filters the catalog to text models with zero prompt/completion price.
4. Start the provider or send your first request. Before the first remote inference in each ASTRA session, review the consent dialog. It names the kinds of content that may be sent. Choose **Allow for this session** only if you agree.
5. Ask a question, or use Assistant features such as PDF passages, a selected project, or a web-research task. The content used for the action is sent only after consent. To stop remote use, choose **Use included offline AI** or delete the saved key.

**ASTRA does not use Zero Data Retention (ZDR) routing.** It does not send `provider.zdr` or filter models/providers by ZDR eligibility. The only routing limits are the $0 prompt and completion price caps, including provider failovers. Prompts, attached text and selected context leave this PC. OpenRouter and the serving provider may retain or use them under their current policies; check the model/provider terms before sending sensitive information. Session consent is required before the first remote inference. Use **Use included offline AI** for local processing.

Free model access has usage limits and availability can change. OpenRouter currently documents 20 requests/minute and 50 requests/day for `:free` variants on an account without purchased credits; provider-specific limits can also apply. `openrouter/free` is a dynamic router, not a promise that one fixed model will always be selected. ASTRA never selects a paid model; check current [free models and limits](https://openrouter.ai/collections/free-models). The local option works without an OpenRouter account or internet, with finite local speed and memory use.

#### Offline AI option

The package includes **Qwen2.5-1.5B-Instruct**, an in-process CPU backend through **LLamaSharp**, and multilingual **Whisper tiny**. It does not require Ollama or `llama-server.exe`. In AI settings, choose **Use included offline AI** to keep requests on the PC. Loading the text model for the first request can take time; stop it to release memory. The optional local model has no hosted-service request quota.

For screenshots and a more detailed setup, privacy, troubleshooting and usage guide, read [`OPENROUTER-AI-GUIDE.md`](OPENROUTER-AI-GUIDE.md) in the release assets.

The interface follows the Windows display language. English and Italian are included; other interface languages are prepared locally, with English used while a translation is unavailable. On limited hardware, start translation manually from personalization. The assistant is instructed to answer in your Windows language; an explicit translation request can choose another language.

Images use local OCR to extract text, rather than general photo understanding. Second Brain imports text PDFs and uses local Windows OCR for scanned pages. Audio transcription and OCR start locally; when you use OpenRouter afterward, the transcript or extracted text included in your prompt is sent after consent. Speech output and OCR availability depend on installed Windows languages and voices. AI results are drafts to review.

**Creator and Gaming use one ASTRA Assistant**, sharing conversation, memory, documents and tools. Core and STEM retain the **Second Brain** name. Open **Open conversation**, or use **Ctrl + Alt + Space**. Send with the button or **Ctrl + Enter**. Text, documents, images with OCR and audio are supported. Microphone recording starts only when you press Record. Chats are saved encrypted; **Chat** reopens earlier conversations, and **New conversation** preserves them.

The **Skills** tab includes installable skills. Select one and choose **Use this skill in chat**. Expand **Create or edit a custom skill** to enter your own instructions, optionally draft them with the selected AI provider, and save or export the skill. Import JSON or Markdown packages to add more. A skill's capabilities do not grant permission to execute tools.

**Web research** searches the public web and adds pages as cited sources on request. **Projects and code** reads a selected project, discusses files and proposes a precise edit. Proposals write nothing: review the complete changes and explicitly approve before applying. Originals have encrypted backups and a separate restoration confirmation. Script execution has its own code/interpreter/arguments review and explicit consent each time. Scripts run with your account permissions, without a sandbox.

The OpenRouter free tier has provider and request quotas; ASTRA does not buy credits or select paid models. For a fully offline alternative with no hosted request quota, choose the included local model; its speed, context, output and available memory are finite. See [`OPENROUTER-AI-GUIDE.md`](OPENROUTER-AI-GUIDE.md) for setup, privacy, quotas and troubleshooting, and [`SECOND-BRAIN-GUIDE.md`](SECOND-BRAIN-GUIDE.md) for the notes/PDF workflow.

Useful commands include **open Chrome**, **open STEM**, **focus 25 minutes**, **add task review chapter 3**, **remember that my project uses quaternion control**, **briefing**, **summarize PDF** and **cancel PDF**. Italian equivalents work too. App launches use existing installed-app shortcuts; generated text and imported documents are not executed as commands.

For long PDFs, select **Import PDF**, then select a library document. Search notes and PDFs offline or ask a question to retrieve passages with page references. **Complete summary** processes all readable pages in bounded batches and combines their summaries. Imports support up to 512 MB, 10,000 pages and 50 million extracted characters per PDF. Progress and cancellation are available. Source PDFs remain untouched; extracted pages are encrypted individually. Reimporting an unchanged PDF reuses its index.

Notes, tags, requested memories and PDF text are encrypted for your Windows account. Search does not train the model. Export notes and retain your original PDFs before moving to another Windows account or reinstalling Windows: DPAPI files are tied to that account. Note JSON exports are readable plain text; they do not include the separately indexed PDF library. See `SECOND-BRAIN-GUIDE.md` for the complete workflow.

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
| OpenRouter returns an error | Check that the API key is saved, then verify the free-tier quota and current model availability. ASTRA does not retry through paid endpoints. |
| Offline AI fails to start | Keep all `Ai/components` files together, check available memory, and try again after closing heavy applications. |
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

The download contains the app, .NET/WinUI runtime files, UI assets, language catalogs, offline AI and speech models, 3D models and renderer, profiles, 43 plugin recipes, Python scene SDK, ISO builder script, documentation, and the Microsoft prerequisite installers.

- `LICENSE` covers ASTRA's own MIT-licensed code. Third-party components retain their own licenses.
- `THIRD-PARTY-NOTICES.md` points to bundled licenses and upstream credits, including NASA/Khronos models, the Ariane reconstruction, Three.js, Qwen, llama.cpp, Whisper, and .NET dependencies.
- `FILES.sha256` lists every payload file's SHA-256. The separately downloadable `.zip.sha256` file verifies the whole ZIP.
- `RELEASE-NOTES.md` records release 4.0.0.5's scope and validation. This package contains application files, not personal preferences, note stores, browser caches, or QA profiles.

To verify either download, run the matching command in PowerShell and compare the output with the same-named checksum asset attached to the GitHub release:

```powershell
Get-FileHash .\ASTRA-OS-V4-HORIZON-Setup-4.0.0.5-win-x64.exe -Algorithm SHA256
Get-FileHash .\ASTRA-OS-V4-HORIZON-openrouter-gaming-creator-win-x64.zip -Algorithm SHA256
```

## For developers and release maintainers

End users can run the binary package without building. A **source checkout** requires Windows, the .NET 10 SDK, Windows SDK, and WinUI build tools. Build and verify from the repository root:

Build commands require a complete source checkout with `src`, `sdk`, `scripts` and the project files. A repository used only to host binary releases may contain only a README; its automatically generated source archive is not the application source. Model weights are larger than GitHub's normal per-file limit and belong in the binary release.

NuGet restore supplies the pinned LLamaSharp CPU backend. `prepare-embedded-ai.py` verifies its repository signature and pinned package/content hashes, then records the expected DLL hashes. The resource fetch script supplies models and 3D assets; it no longer downloads a server executable. No script changes antivirus settings or restores quarantined files.

```powershell
dotnet restore .\src\Astra.Desktop\Astra.Desktop.csproj -r win-x64 -p:Platform=x64
python .\scripts\prepare-embedded-ai.py
python .\scripts\fetch-3d-ai-resources.py
dotnet publish .\src\Astra.Desktop\Astra.Desktop.csproj -c Release -r win-x64 -p:Platform=x64 --self-contained true -o .\dist\AstraOS-Local
```

To produce the GitHub binary asset from the published `dist/AstraOS-Local` folder:

```powershell
pwsh -NoProfile -File .\scripts\fetch-release-prerequisites.ps1
python .\scripts\package-release.py
```

The download script verifies Microsoft Authenticode signatures without executing installers. Packaging checks all payload bytes and creates the ZIP, per-file manifest, archive checksum, and verification report. This specific assistant update uses `scripts/package-assistant-update.py` after its native QA report is recorded; it refuses unverified binaries. `GITHUB-UPDATE.md` describes publishing a binary release without pushing sources.
