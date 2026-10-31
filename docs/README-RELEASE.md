# ASTRA OS V4 · Horizon

A customizable desktop workspace for Windows 11, with local AI, STEM tools, a 3D desktop, widgets, and Gaming and Creator workspaces.

**Windows 11 x64 · Portable application · Release candidate**

ASTRA runs on your existing Windows installation. You can keep using your Windows applications and files. A valid Windows installation is required.

## Download and start

1. Open this project's **GitHub Releases** page and download `ASTRA-OS-V4-HORIZON-win-x64.zip` from the release's **Assets** section.
2. Right-click the ZIP in File Explorer and choose **Extract All**. Extract it to a permanent local folder you can write to, such as `C:\Users\YourName\Apps`.
3. Open the extracted `ASTRA-OS-V4-HORIZON` folder.
4. Double-click **`Astra.Desktop.exe`**. Run ASTRA with your normal Windows account.
5. Keep the entire extracted folder together. You can create a shortcut to the EXE on your desktop.

**Extract the ZIP before starting ASTRA.** The EXE needs its DLLs, compiled XAML, resources, and subfolders beside it. Copying the EXE alone will prevent it from working. GitHub's automatically generated **Source code (zip)** is for developers; download the named Windows binary ZIP to run the app.

### Requirements and included components

| Component | What you need |
| --- | --- |
| Operating system | Windows 11 on an x64 PC. This package is not a native ARM64 build. |
| Disk space | Allow about 4 GB for the download and extraction, plus room for your own files and caches. |
| .NET and WinUI | The .NET and Windows App SDK runtimes are included. Visual Studio and the .NET SDK are not required to run this package. |
| 3D views | Microsoft Edge WebView2 Runtime. Its official x64 offline installer is included in `prerequisites`. |
| Native AI/audio libraries | Microsoft Visual C++ v14 x64 Runtime. Its official installer is included in `prerequisites`. |
| Local AI | Text and speech models are included. 8 GB RAM or more is recommended for comfortable AI use; performance depends on the PC. AI can stay unloaded while you use other tools. |
| Browser button | Install Google Chrome separately to use ASTRA's Chrome integration. |
| Optional tools | Python, a WSL Linux distribution, and Windows ADK/OSCDIMG are needed only for their respective optional workflows. |

If a runtime is missing, close ASTRA, open `prerequisites`, and run the appropriate Microsoft installer:

- `MicrosoftEdgeWebView2RuntimeInstallerX64.exe` for the 3D views.
- `vc_redist.x64.exe` for native components, including local AI/audio.

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

### Local assistant and Second Brain

1. Open the assistant from ASTRA, or press **Ctrl + Alt + Space** while using another application.
2. Type a request, choose files, paste selected text explicitly, or start an audio recording. The assistant receives the foreground application's title when called; you choose the content to provide.
3. Choose an action such as explain, summarize, make a checklist, translate, extract JSON, or analyze an error.
4. Review the result and export text, Markdown, JSON, WAV speech, SRT subtitles, a note, a scene, or a widget proposal as appropriate for the action.

The package includes **Qwen2.5-1.5B-Instruct**, a CPU llama.cpp runtime, and multilingual **Whisper tiny**. No cloud API key or paid subscription is required for these local functions. Loading the text model for the first request can take time. You can stop it to release memory.

The interface follows the Windows display language. English and Italian are included; other interface languages are prepared locally, with English used while a translation is unavailable. On limited hardware, start translation manually from personalization. The assistant is instructed to answer in your Windows language; an explicit translation request can choose another language.

Images use local OCR to extract text, rather than general photo understanding. PDF import extracts existing text; provide page images for scanned documents. Speech output and OCR language availability depend on Windows language components and installed voices. AI results are drafts to review.

**Second Brain** stores notes and tags encrypted for your Windows account. Its offline search retrieves relevant passages; optional local AI uses those passages to answer questions. It does not train the model on your notes. Export notes before moving to another Windows account or reinstalling Windows: DPAPI-encrypted files are tied to the original account. JSON exports are readable plain text, so store them appropriately.

### Widgets and automation

Open **Widget and automations** to add a clock, system monitor, note, countdown, focus timer, tasks, workspace shortcut, or automation. Use the editor to customize a widget, or import/export a JSON widget pack. Up to 32 widgets are supported.

Automations can run on an interval or a daily schedule and perform supported in-app actions, such as reminders, navigation, focus timers, or notes. **ASTRA must remain open**, including in the background. Imported automations start disabled until you enable them. Declarative widgets do not execute arbitrary external scripts.

The **Plugin SDK** also includes 43 declarative plugins. Open that section to browse and enable them; examples are included under `recipes`.

### Gaming and Creator

- **Gaming:** start a session to record actual system CPU, GPU engine utilization where available, and RAM. Show the Session HUD, compare sessions, and export CSV samples. The HUD may be hidden by an exclusive full-screen game. It does not measure FPS, frame time, temperature, or network latency.
- **Creator:** add local assets to the board, inspect media metadata and image colors, arrange a storyboard, and export Markdown or CSS. Subtitle Lab imports SRT, adjusts timing offset and scale, and exports a new file. Your original media files stay intact.
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
| Local AI fails to start | Keep all `Ai/components` files together, check available memory, and try again after closing heavy applications. |
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

To update, close ASTRA and extract the new release into a separate folder. Your per-account data remains in its existing location. If you enabled login startup, disable it in the old copy and enable it in the new one.

To remove ASTRA, disable its login startup option and restore the previous Windows cursor scheme if you applied an ASTRA scheme. Close ASTRA, then delete its extracted app folder and shortcuts. Your separate user data is retained unless you choose to remove it; export important notes first.

## Package contents, credits, and verification

The download contains the app, .NET/WinUI runtime files, UI assets, language catalogs, local AI and speech models, 3D models and renderer, profiles, 43 plugin recipes, Python scene SDK, ISO builder script, documentation, and the Microsoft prerequisite installers.

- `LICENSE` covers ASTRA's own MIT-licensed code. Third-party components retain their own licenses.
- `THIRD-PARTY-NOTICES.md` points to bundled licenses and upstream credits, including NASA/Khronos models, the Ariane reconstruction, Three.js, Qwen, llama.cpp, Whisper, and .NET dependencies.
- `FILES.sha256` lists every payload file's SHA-256. The separately downloadable `.zip.sha256` file verifies the whole ZIP.
- `RELEASE-NOTES.md` records this release candidate's scope and validation. This package contains application files, not personal preferences, note stores, browser caches, or QA profiles.

To compare the ZIP checksum in PowerShell, run this in the folder containing the download and compare the hash to the release's `.zip.sha256` file:

```powershell
Get-FileHash .\ASTRA-OS-V4-HORIZON-win-x64.zip -Algorithm SHA256
```

## For developers and release maintainers

End users can run the binary package without building. A **source checkout** requires Windows, the .NET 10 SDK, Windows SDK, and WinUI build tools. Build and verify from the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-local.ps1 -RunWindowsTests
```

To produce the GitHub binary asset from the published `dist/AstraOS-Local` folder:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\fetch-release-prerequisites.ps1
python .\scripts\package-release.py
```

The download script verifies Microsoft Authenticode signatures without executing installers. Packaging checks all payload bytes and creates the ZIP, per-file manifest, archive checksum, and verification report. `GITHUB-PUBLISHING.md` describes how to attach these assets to a GitHub pre-release.
