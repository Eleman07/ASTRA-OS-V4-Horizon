# ASTRA Assistant and desktop tools

Creator and Gaming now combine conversation, memory, PDFs, research, projects and routines in one **ASTRA Assistant**. Core and STEM retain **Second Brain** and share the same tools and encrypted library.

## What changed

- Modern conversation window with messages, attachments, audio, Ctrl + Enter, optional spoken replies and encrypted saved chat history.
- **26 included skills**, JSON/Markdown import and an in-app editor for **custom skills**, including optional local-AI instruction drafting and JSON export.
- Actual public web search and HTTPS source reading with URLs and retrieval context.
- Project code reading and precise AI edit proposals. **No write on generation**: a full preview and explicit approval are required. Encrypted backups and separately approved restoration are included.
- Python/PowerShell/JavaScript review and explicit approval **for every execution**, literal arguments, captured output, cancellation and configurable timeouts. Scripts are not sandboxed.
- Five built-in routines, encrypted personal routines and optional widget schedules that start disabled.
- **40 Control Center entries**, manual clipboard shelf, Quick Look, file/folder operations, Windows file clipboard, ZIP creation/extraction, recursive search and move/rename undo.
- Default CPU inference now uses **LLamaSharp 0.30.0 inside ASTRA**, with repository-signed NuGet packages and pinned component hashes. **No `llama-server.exe` dependency or default AI server port.**

Earlier Home/dock icons, window switching, responsive themes, animated Orbital desktop, STEM/3D, Creator/Gaming labs, widgets, 43 declarative plugins, WSL integration and optional ISO tools are retained.

## Complete feature list

The attached [FEATURE-CATALOG.md](https://github.com/Eleman07/ASTRA-OS-V4-Horizon/releases/download/v4.0.0-assistant-system.20261029/FEATURE-CATALOG.md) documents every included assistant skill and plugin, all simulation models, desktop functions, workflows and practical limits.

The [assistant tutorial](https://github.com/Eleman07/ASTRA-OS-V4-Horizon/releases/download/v4.0.0-assistant-system.20261029/ASSISTANT-SYSTEM-UPDATE.md) explains saved chats, PDF work, research, custom skills, project consent and routines. The English README covers startup, full screen, login startup and Windows 11 **Automatically hide the taskbar**.

## Download and update

Download **ASTRA-OS-V4-HORIZON-assistant-system-update-win-x64.zip** from Assets. Close the old ASTRA copy, extract the entire ZIP into a separate permanent folder and run **Astra.Desktop.exe**. Keep all files together. The automatically generated source archive is not the runnable Windows distribution. Per-account data stays in its existing location; move login startup registration to the new copy if enabled.

The package includes the local model, CPU and speech libraries/models, 3D resources, skills, SDK examples, English tutorials, licenses and official Microsoft prerequisites. It excludes personal notes, chats, browser caches and QA profiles.

## Verification

- **376 Core checks**, **47 static/Python tests**, **25 PDF/memory/command regression checks**.
- **22 final native checks with real local inference**, actual Send/cancel actions, complete change preview and encrypted history persistence.
- Live public search/page reading; literal process arguments and cancellation checked.
- **869 payload files** verified individually; **1,610,264,047-byte ZIP**.
- SHA-256: `e85350648ac831a4ba601988858aae719b25f5c58b26a301c57055551b5503c3`.

## Scope and practical limits

ASTRA is a **Windows 11 x64 desktop application**. Windows owns device/system settings; Ubuntu requires WSL with a distribution. macOS-inspired workflows do not provide macOS application compatibility. STEM solvers are educational models with stated assumptions, rather than complete CFD/FEM or certified engineering tools. Gaming records actual utilization, not FPS or live game state.

Local AI is free, with no ASTRA subscription, usage credits or daily/message quota. Hardware, disk, context and output remain finite. The small included model produces drafts and does not match frontier cloud model capability. Search providers can impose their own limits. Scripts require explicit consent and run with account permissions without a sandbox.

This is a **pre-release**. Previous releases are preserved. Only binary release assets and documentation are updated; application source is not pushed to main.
