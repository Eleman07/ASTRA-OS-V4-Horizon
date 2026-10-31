# Horizon — Assistant and desktop tools update

Creator and Gaming now have one **ASTRA Assistant** for chat, memory, PDFs, research, projects and routines. Core and STEM retain **Second Brain** and share the same local tools and encrypted library.

- Redesigned conversation window with message bubbles, attachments, audio, Ctrl + Enter, optional spoken answers, encrypted saved conversations and history paging.
- 26 included skills for coding, research, study, STEM, Creator, Gaming and productivity. Import JSON/Markdown, create or edit custom skills in the app, optionally draft instructions with local AI, and export packages.
- Public web search and HTTPS page reading with source URLs. Sources and skill instructions cannot run tools.
- Selected-project code reading and AI proposals. File writes require an explicit review of actual changes; encrypted backups support separately approved restoration. Python, PowerShell and JavaScript execution requires an additional explicit code/interpreter/arguments approval each time.
- Five built-in routines and encrypted personal routine editing; optional widget schedules remain disabled until activated.
- Control Center with 40 tools/settings, manual clipboard shelf, Quick Look, folder/file operations, ZIP extraction/creation, file clipboard, recursive search and move/rename undo.
- Free local inference, speech and OCR; no ASTRA subscription, usage credits or daily message quota. Compatible GGUF model selection is supported.
- Default inference now uses version-pinned, repository-signed LLamaSharp CPU libraries inside ASTRA. The old server executable is no longer required or distributed; no AI server port is opened.

ASTRA remains a Windows 11 application. Settings and device management use Windows; Ubuntu requires WSL with a distribution. macOS-style workflows are integrated equivalents, not macOS application compatibility. Hardware, storage and model context remain finite. The bundled small model produces drafts requiring review; project scripts are not sandboxed.

## Start or update

Extract the complete new ZIP into a separate permanent folder. Close your previous ASTRA copy, then run **Astra.Desktop.exe** from the new folder. Account data remains in the existing location. If enabled, move login startup from the old copy to the new copy. Follow **README.md** for full screen and Windows 11's **Automatically hide the taskbar** option. See **ASSISTANT-SYSTEM-GUIDE.md** for step-by-step assistant and custom skill instructions.

The package includes verification reports, component hashes, third-party notices and official Microsoft prerequisites. It does not install prerequisites, alter Windows security settings or execute project scripts automatically.
