# ASTRA OS V4 Horizon — complete feature catalog

Release: `v4.0.0-assistant-system.20261029` · Windows 11 x64 · Release candidate.

This catalog describes the implemented portable application, including earlier desktop features and the Assistant/System update. ASTRA runs on Windows. Hardware settings, device drivers, updates, security and Windows accounts remain Windows services. macOS-inspired workflows are interface equivalents, not macOS binary compatibility. Ubuntu needs WSL and an installed distribution.

## 1. One local assistant, with profile-specific names

Creator and Gaming expose one **ASTRA Assistant** entry for conversation, memory, documents, research, projects and routines. Core and STEM retain **Second Brain**. All four use the same underlying tools and encrypted document/note library.

- Open the conversation from the assistant page or with **Ctrl + Alt + Space**. The shortcut can supply the foreground app title; application content is provided explicitly by the user.
- Message bubbles, selectable replies, composer, **Send**, **Ctrl + Enter**, Stop and New conversation.
- Saved conversations encrypted with Windows DPAPI for the current account; reopen them from Chat and page through older messages. Starting a new chat preserves previous chats.
- Tool/output/voice preferences collected in a dropdown. Optional pin-on-top; the conversation is not forced above other apps.
- Window sizing follows the monitor and DPI. Motion respects Eco and reduced-motion preferences.
- Replies follow the Windows interface language; an explicit translation request can choose another language.

## 2. Inputs, outputs and local processing

Inputs include typed text and code, Markdown, text files, CSV/TSV, JSON, DOCX, PDFs, image text through OCR, clipboard content pasted explicitly, and supported audio files. File attachment is explicit; microphone capture starts only from Record. Audio decoding depends on installed Windows codecs. Recording is limited to ten minutes per capture.

Built-in response modes cover explanation, summaries, plans/checklists, translation, error analysis, structured JSON, related-note retrieval, data-to-3D and widget proposals. Results can be copied or exported as text, Markdown, JSON or WAV speech. Transcriptions with segments can export timed SRT subtitles. Results can also become an encrypted note, a validated widget proposal or a supported 3D scene.

The bundled text model is **Qwen2.5-1.5B-Instruct**, with **LLamaSharp 0.30.0 CPU inference inside ASTRA**. No default AI server executable or listening port is required. NuGet repository signatures and pinned component hashes were verified. The package includes multilingual **Whisper tiny** for transcription; Windows supplies OCR and speech output. Compatible alternative GGUF models can be selected. A user-selected external runtime is optional and requires explicit approval.

There is no ASTRA subscription, usage-credit system or daily/message quota. Hardware, model context, output length and disk space remain finite. Only recent/relevant context enters a response; saved history does not create infinite model context. The included small model produces drafts and is not equivalent in capability to frontier cloud models. Images are read with OCR, not general visual reasoning over arbitrary photographs. The assistant does not continuously watch the screen or listen to the microphone.

## 3. Memory and long PDFs

- Encrypted notes with title, content and tags; import/export, search and requested fact retention.
- Local passage search over notes and PDFs, including page references. Deterministic search works without loading the text model.
- Separate PDF library with extraction per page, encrypted storage and local OCR for scanned pages.
- **Complete summary** processes all readable pages in batches and combines their summaries. Progress and cancellation are available; unreadable pages are reported.
- Hash-based duplicate detection reuses an unchanged document's index. Original PDFs remain untouched.
- Import limits: 512 MB, 10,000 pages and 50 million extracted characters per PDF, with up to 256 library PDFs. These are processing bounds rather than paid tiers.
- Note exports are readable text/JSON and should be reviewed before sharing. DPAPI data is tied to the Windows account; retain original documents before account migration or reinstalling Windows.

## 4. Research, study and contextual help

- Public web query search with actual result titles and links. Search results are distinguished from pages already read.
- HTTPS text-page reading with original URL, title and retrieval time; add and compare session sources in chat, then clear them.
- Notes and PDFs are not automatically sent to a search engine. Public readers check addresses, DNS and redirects and reject private/local destinations.
- Dynamic pages and access restrictions may require Chrome. The reader does not execute page JavaScript or bypass access checks. Search providers can impose their own rate/availability limits.
- Study-plan and quiz commands use retrieved material when available, with source references and missing-evidence disclosure.
- STEM guidance can use supplied simulation results, units, parameters and assumptions.
- Creator help covers briefs, shot lists, asset metadata, subtitles, accessibility, delivery and licensing checklists.
- Gaming help covers setup, supplied game rules, strategy suggestions, repeatable comparisons and measured session telemetry. It does not invent live game state, FPS or temperatures.

Useful explicit commands include `open Chrome`, `open STEM`, `focus 25 minutes`, `add task review chapter 3`, `remember that my project uses quaternion control`, `briefing`, `summarize PDF`, `cancel PDF`, `search web ESA Ariane 6`, `read page https://example.com`, `study plan orbital mechanics`, `quiz orbital mechanics`, `routine creator`, `gaming coach`, `creator audit`, `context` and `control center`. Italian equivalents are supported. Unrecognized requests remain conversation; generated replies and attached content do not become tool commands.

## 5. Installable and custom skills — all 26 included packages

Skills provide reusable working instructions and suggested output modes. Listed capabilities describe intended use; they do not grant permission to edit files, run scripts or send data.

| Skill | Purpose |
|---|---|
| Code Review | Identify concrete code defects and meaningful verification. |
| Bug Triage | Turn errors into testable hypotheses and reproduction steps. |
| Refactor Planner | Plan incremental changes within the requested scope. |
| Test Designer | Design meaningful edge-case and regression checks. |
| Python Data Lab | Assist with Python analysis and reproducible data work. |
| SQL Reviewer | Review joins, queries, transactions and destructive operations. |
| Documentation Writer | Write documentation/tutorials from supplied project files. |
| Web Research | Organize research with sources, dates and evidence distinctions. |
| Source Comparison | Compare claims, agreements and conflicts between sources. |
| Study Tutor | Explain progressively and provide exercises with feedback. |
| Active Recall | Prepare source-grounded quizzes, flashcards and answer keys. |
| Math Checker | Check derivations, domains and boundary cases. |
| Units and Assumptions | Review units, assumptions and initial conditions. |
| Aerospace GNC | Support quaternion attitude, orbits and flight-dynamics study. |
| Experiment Planner | Plan repeatable experiments, controls and uncertainty. |
| Creator Brief | Turn requirements into a constrained creative plan. |
| Storyboard Studio | Prepare shot lists, storyboards and editing rhythm. |
| Delivery Audit | Review deliverable formats, credits and accessibility. |
| Subtitle Coach | Review subtitle text, timing and readability. |
| Gaming Setup | Plan repeatable setup/configuration comparisons. |
| Gaming Debrief | Review measured telemetry and supplied session observations. |
| Strategy Companion | Suggest plans from provided rules and game state. |
| Work Planner | Break goals into priorities and concrete next steps. |
| Automation Designer | Draft declarative routines for review before activation. |
| Accessibility Reviewer | Review content clarity, readability and accessibility. |
| Privacy Checklist | Identify sensitive material before export or publication. |

Select a skill, inspect its instructions and choose Use in chat. Import a JSON package or Markdown `SKILL.md`, deactivate it, export it, or remove a personal installation. Markdown imports become prompt-only skills. User skills are saved encrypted.

The custom editor supports name, description, category, instructions, declared capabilities and suggested response type. Start blank or load an existing skill as a basis, optionally draft instructions with local AI, edit them and Save custom skill. Exported JSON can be shared and reimported. The release includes an SDK example, Markdown example and JSON schema.

## 6. Projects, code proposals and explicit consent

Choose a project folder to authorize supported text/code listing and reading. Selecting that folder alone does not approve writes or processes. Discuss a selected file in chat and describe a requested edit.

The model proposes an exact replacement in the selected file. Preparing the proposal writes nothing. Review shows the full original/proposed content; **Apply the displayed changes** is the separate consent action. The current file is checked against its preview hash; stale proposals stop. Encrypted backups preserve originals, with a separately confirmed restore that refuses subsequent conflicting edits. The current automatic proposal workflow handles one selected file up to 10,000 characters; supported larger files remain readable within the reader's processing limits.

Python, PowerShell and JavaScript execution requires a separate dialog showing the actual interpreter, path, script and literal arguments. Each run requires **I authorize this execution**. The reviewed script is checked again before execution. Output and exit status are captured; Stop cancels the process started by ASTRA. The default two-minute timeout is configurable, with zero disabling it. PowerShell policy is not bypassed. Scripts run with the Windows account's permissions and **are not sandboxed**; they can access data outside the project. AI replies, skills, documents and routines cannot supply this consent.

## 7. Productivity, routines and widgets

Five included routines provide a starting sequence of navigation, a real task and a focus timer: Study Sprint, Documented Research, Creator Delivery, Gaming Session and STEM Review. Inspect the steps before Run; cancellation and an execution log are available.

Create personal routines with supported navigation, task, remember and focus actions. Routines are encrypted locally, with up to twelve steps each. Optional widget scheduling starts disabled until explicitly enabled. Routines do not authorize scripts or project edits.

Eight widget types are available: clock/time zones, system monitor, note, countdown, focus timer, tasks, workspace shortcut and automation. Customize title, text, style, width and timing; import/export JSON packs. Up to 32 widgets are supported. Interval and daily automations can show an in-app reminder, navigate, start focus or save a note/run a supported routine. ASTRA must remain open. The manual clipboard shelf holds up to twelve session entries and does not continuously capture clipboard changes.

## 8. Desktop, dock, files and window management

- Home displays actual Desktop file/folder icons and names. Open by double click or Enter; native shell icons have a visible fallback.
- Dock uses icons with hover names, ASTRA logo/quick menu and icons for real open windows. Activate, minimize and restore windows from ASTRA. The previously requested floating bar over applications is removed.
- File Explorer: browse folders, create folders, copy files/folders, move, rename, Windows file clipboard copy/cut/paste, ZIP creation/extraction, Recycle Bin through native dialogs, recursive filename search and undo of the latest move/rename.
- Quick Look previews supported text, images, folders and PDF pages. Operations preserve existing destinations and reject unsafe archive paths/reparse links. Large operations have documented bounds; native Explorer remains available.
- Multitasking arranges selected real windows into two columns, three columns or a grid, then restores the previous arrangement. Up to eight windows can be selected.
- Saved sessions retain workspace/profile, STEM path, layout and selected apps/folders, with up to six apps and six folders. Resuming opens the saved resources; choose the actual windows to arrange.
- Ctrl + K searches ASTRA pages/tools. Chrome is the configured browser integration; Windows owns system-wide file/browser associations.
- Full screen with F11, Esc to return, saved start-full-screen preference and optional launch at Windows login. The English README includes Windows 11 Automatically hide the taskbar instructions.

## 9. Personalization, 3D desktop, language and performance

Palette, wallpaper and banner can be customized, including animated GIF backgrounds. The banner adapts to screen width and larger text; module cards switch between one, two and three columns. The logo is also the application icon and dock menu entry.

Orbital 3D places real Desktop entries in orbit around an animated sphere using the selected palette. Renderers load on request and suspend/release resources when unused. Theme pointer previews include fifteen Windows pointer roles in 32/48/64 px. Applying the pointer set is opt-in for the current account, with backup/restore of the previous set.

The interface follows Windows display-language preferences. Italian and English are included; other UI translations are generated locally and cached, with English fallback. Low-resource devices can start translation manually. The assistant is instructed to answer in the Windows language; file names and personal content are not translated automatically.

Composition animations, reduced motion and Eco preferences are integrated. Lightweight defaults are selected on limited hardware while respecting saved preferences. AI thread counts are bounded; the model can be unloaded. 3D resolution/update rates and invisible widget updates are reduced in Eco. Background Gaming recording remains an explicit activity. These are software policies, not a promise of identical performance on every PC.

## 10. STEM profiles, simulations and 3D

Profiles display the Windows account and user folder. STEM domains include interdisciplinary STEM, engineering, physics, mathematics and sciences. Engineering offers 23 choices: aerospace; mechanical; electrical; electronics; computer; automation/control; robotics; civil/structural; building/architecture; environmental; chemical/process; biomedical; energy; nuclear; materials; naval/ocean; industrial/manufacturing; management; telecommunications; transport; geotechnical/mining; agricultural/food; other/interdisciplinary. Tools are filtered by field; a track choice does not imply a dedicated professional solver for every subject.

| Implemented model | Behavior and scope |
|---|---|
| Orbital mechanics | Cartesian propagation with RK4 and optional Earth J2. |
| Quaternion attitude GNC | Rigid-body Euler dynamics and saturated PD attitude control. |
| Flight mechanics | Point-mass longitudinal climb/glide model with stated atmosphere/aerodynamic assumptions. |
| Finite-wing aerodynamics | Linear lift, induced drag and efficiency/polar relationships. |
| Euler–Bernoulli beam | Elastic cantilever bending with a tip load. |
| Damped oscillator | Linear free response of a single-degree-of-freedom system. |
| Thermal diffusion | One-dimensional FTCS conduction with stability constraints. |
| Batch reactor | Isothermal first-order reaction model. |
| RLC circuit | Ideal linear series-circuit transient. |
| DFT signal analyzer | Sampled sinusoidal signals, Hann window, spectrum and aliasing. |
| M/M/1 queue | Stationary single-server queue statistics for stable arrival/service rates. |
| Lotka–Volterra | Idealized predator–prey population dynamics. |
| Lorenz system | Deterministic nonlinear dynamics and a 3D phase trajectory. |

Models expose parameters, units, assumptions and data export. Graphs stay available alongside 3D. Nine downloaded NASA/Khronos meshes plus an original educational Ariane 64 reconstruction are included with credits. Ariane geometry follows public ESA/ArianeGroup dimensions; it is not official ESA CAD or an endorsed vehicle digital twin.

Import GLB, self-contained glTF, OBJ or STL within the documented 64 MB/geometry limits. Orbit the camera, inspect sections/wireframe, scrub time, change replay speed and scale displayed deformation. Playback applies solver samples to trajectory, quaternion, displacement, field color or the corresponding representation. Mesh import does not infer physical properties or generate a CFD/FEM model. External glTF resources and OBJ MTL material files are not automatically loaded.

The Python standard-library SDK creates JSON scenes of lines, points and surfaces. Import or watch a generated scene, map CSV/JSON columns to axes, and drive a model along supplied trajectories. Importing the result does not execute the Python script automatically.

## 11. Gaming and Creator workspaces

**Gaming:** explicitly record real system CPU utilization, busiest available GPU engine and RAM use; view a Session HUD over ordinary/borderless windows; compare session averages/peaks; export CSV and discuss measured samples with ASTRA Assistant. Exclusive full-screen games may hide the HUD. The implementation does not measure FPS, frame time, temperatures or latency, and changes in utilization alone do not prove a performance cause.

**Creator:** encrypted board for up to 32 local assets; available dimensions, file size and media duration; image dominant colors; text-contrast checks; shot order, notes and storyboard timing. Export Markdown storyboards and CSS palettes. Subtitle Lab imports SRT, adjusts offset/time scale and saves a new synchronized version. Assistant briefs use supplied content/metadata, not an invented claim that the video was watched. Delivery Audit checks the available metadata and checklist.

## 12. Control Center — all 40 entries

| Group | Available entries |
|---|---|
| ASTRA workspace tools | Files/search; App Hub/launcher; multitasking/window layouts; dock/quick access; quick search; widgets/automations; assistant/memory; Ubuntu/WSL; plugins/development; performance monitor; themes/pointers/desktop. |
| Display and connections | Display/monitors; sound; Wi-Fi; Ethernet/network status; Bluetooth; VPN; printers/scanners; mouse/touchpad; keyboard/languages. |
| System and productivity | Power/sleep; storage; notifications; Windows virtual desktops/Snap settings; default apps; startup apps; Windows Update; Windows account; date/time; Windows Backup. |
| Accessibility and clipboard | Visual accessibility; Narrator; Magnifier; captions; Windows clipboard settings. |
| Gaming, appearance and protection | Game Mode; capture/Game Bar settings; per-app graphics preferences; Night Light; security tools. |

ASTRA routes open its own tools; settings entries open the original Windows pages. Availability depends on Windows/hardware. Lock, restart, shutdown and Recycle Bin access are also exposed as explicit actions, with confirmation for restart/shutdown. These controls were not used to change the PC during release QA.

Installed Windows tools are discovered/deduplicated into seven categories: productivity, utilities, creativity, entertainment, other, development and accessibility. Existing software is not silently reinstalled. Performance and security pages expose available real system status and original tools.

Ubuntu Lab checks installed WSL distributions and actual launch readiness, offers guided installation when missing and opens a separate terminal. It does not silently install a distribution. ISO Builder prepares a plan by default and can create customized Windows installation media from legitimate files supplied by the user, with Windows ADK/OSCDIMG and explicit build steps. It is not required to run ASTRA and does not clone the current PC automatically.

## 13. Plugin SDK — all 43 bundled declarative plugins

These perform the stated local text/data operations. They are distinct from assistant prompt skills and do not turn every plugin into an autonomous AI agent.

| Plugin | Operation |
|---|---|
| Core Word Count | Count words locally. |
| Creator Tasks | Extract TODO/FIXME markers. |
| Gaming Line Numbers | Number configuration-text lines. |
| STEM Outline | Extract Markdown headings. |
| Text Kit | Example local text prefix operation. |
| Contrast Inspector | Calculate contrast between two hex colors. |
| Tint Laboratory | Generate lighter/darker color variants. |
| Correlation Compass | Pearson correlation for supplied numeric pairs. |
| CSV Bridge | Convert CSV records into JSON objects. |
| Table Publisher | Convert CSV to a Markdown table. |
| Distribution Sketch | Generate a text histogram. |
| Signal Smoother | Moving average with an explicit window. |
| Outlier Detective | Flag values beyond 1.5 IQR as statistical indications. |
| Trend Fit | Least-squares linear fit of numeric pairs. |
| Measurement Notebook | Count, mean, median, deviation and standard uncertainty. |
| Z-Score Lab | Standardize supplied samples. |
| Base64 Decoder | Decode validated UTF-8 Base64. |
| Base64 Encoder | Encode UTF-8 text; this is not encryption. |
| JSON Compact | Validate and compact JSON. |
| JSON Lens | Validate and pretty-print JSON. |
| Fingerprint SHA-256 | Hash the supplied text. |
| URL Decoder | Decode a URL component as text. |
| URL Encoder | Encode a URL component without opening it. |
| UUID Forge | Generate a local UUID v4. |
| Complex Plane Kit | Basic complex-number operations and magnitudes. |
| Matrix Microscope | Determinant, trace and inverse of a 2x2 matrix. |
| Root Explorer | Solve a quadratic, including complex roots. |
| Unit Bridge | SI, temperature, angle and speed conversions. |
| Vector Workbench | Dot/cross products and norms of 3D vectors. |
| Feynman Canvas | Prepare an explanation outline to complete. |
| Lab Report Canvas | Prepare a scientific report structure without inventing results. |
| Focus Blocks | Split study time into 25-minute blocks and breaks. |
| Active Recall Cards | Convert supplied question/answer pairs to review cards. |
| Spaced Review Planner | Suggest review dates at +1/+3/+7/+14/+30 days. |
| Action Checklist | Convert lines to a Markdown checklist. |
| Text Detox | Normalize spaces, tabs and blank lines. |
| Keyword Lens | Count recurring words without an AI request. |
| Quote Builder | Prepare multiline Markdown quotations. |
| Reading Compass | Estimate reading time at 220 words/minute. |
| Slug Studio | Generate a normalized slug from a title. |
| Line Sorter | Sort text lines deterministically. |
| Duplicate Radar | Remove duplicate lines, preserving order. |
| Readable Lines | Reflow text to approximately 72 columns. |

## 14. Portable release and verification

The ZIP includes the app, self-contained .NET/WinUI runtime, local text/speech models, CPU libraries, 3D resources, language catalogs, profiles, 43 plugin recipes, 26 skills, Python/skill SDK examples, ISO script, English tutorials, licenses and official Microsoft prerequisites. Extract the whole folder before running `Astra.Desktop.exe`; the EXE alone is insufficient. Updates use a separate folder and preserve per-account data. Startup registration should be moved from the old copy if enabled.

Current artifact: 869 files, 1,610,264,047 bytes. ZIP SHA-256: `e85350648ac831a4ba601988858aae719b25f5c58b26a301c57055551b5503c3`. This catalog is an additional release attachment; the already-verified binary ZIP is unchanged.

Validation includes 376 Core checks, 47 static/Python tests, 25 PDF/memory/command regression checks, real public search/page reading, literal script arguments and process cancellation, 46 required distribution files plus CPU component hashes, and 22 final native checks with real local inference. Native QA checked the actual Send action, complete edit preview, cancellation leaving the project untouched and encrypted chat persistence. Earlier dock/3D/audio/profile validation is retained in the project evidence. No trailer was recreated for this update.
