# ASTRA Assistant and desktop tools

ASTRA is a Windows 11 desktop application. It combines its own file, workspace and productivity tools with links to the original Windows settings. It does not replace the Windows kernel or drivers, emulate macOS applications, or install Linux automatically. Ubuntu tools require an installed WSL distribution.

## One assistant in Creator and Gaming

Creator and Gaming use **ASTRA Assistant** for conversation, documents, memory, web sources, project work and routines. The duplicate Second Brain navigation entry is hidden in those profiles. Core and STEM retain the **Second Brain** name and use the same tools and encrypted library.

Open the conversation from the assistant page or with **Ctrl + Alt + Space**. Write a message and select **Send**, or press **Ctrl + Enter**. Attach files, explicitly paste clipboard content, or record audio. Recording starts only from the microphone button. Spoken responses are optional. **Tools, outputs and preferences** contains output modes, export, voice settings, source text and the optional keep-on-top switch. Conversation windows fit the monitor; animations respect Eco and reduced-motion preferences.

Chats are saved with Windows DPAPI encryption for the current account. Use **Chat** to reopen conversations and page through older messages. New conversation starts another chat without deleting earlier conversations. There is no daily or message quota imposed by ASTRA. Only a recent portion is sent to the local model for each response; stored history is not the same as an unlimited model context window.

## Free local processing

The bundled text model, transcription, Windows OCR and speech output run on your computer. No AI subscription, paid API, account or usage-credit system is required. The default CPU model runs inside ASTRA through version-pinned LLamaSharp libraries; it needs neither a separate server executable nor a listening port. Packages are repository-signed by NuGet, with component hashes included in `Ai/embedded-runtime.json`. You can choose another compatible local GGUF model in advanced settings. An optional, explicitly selected external runtime executable still requires separate approval before launch.

“No usage quota” does not mean infinite computing resources: speed, model capability, context, output length and storage depend on your hardware and model. File and request limits protect memory and data integrity; they are not paid tiers. Web search providers can impose their own availability or rate limits. No external AI provider is contacted as a fallback.

## Research with actual sources

1. Open **Web research** and enter a query, or type `search web ESA Ariane 6` in chat.
2. Search results identify links; they are not presented as pages already read.
3. Choose **Read and add as a source**, or send `read page https://example.com`.
4. Compare the read sources in chat. Sources include their original URL and retrieval time.
5. Remove session sources when finished. Notes and PDFs are not automatically sent to a search engine.

Public HTTPS text pages are supported. Private-network addresses, credentials in URLs and custom ports are rejected; DNS and redirects are checked. Dynamic pages, access restrictions and verification pages may require the browser. ASTRA does not bypass those restrictions. Web pages, attached documents and skill instructions cannot themselves execute tools.

## Installable and custom skills

The **Skills** tab includes 26 packages for coding, diagnostics, testing, Python, SQL, documentation, research, source comparison, tutoring, active recall, mathematics, units, GNC, experiments, creative briefs, storyboards, delivery, subtitles, gaming, planning, automations, accessibility and privacy.

Select a skill and choose **Use this skill in chat**. The selected skill appears beside the conversation context. It supplies reusable guidance; its listed capabilities do not grant permissions.

To create a custom skill:

1. Expand **Create or edit a custom skill**.
2. Enter its name, description, category and instructions. Choose the suggested response type.
3. Optionally generate an editable draft using the local AI.
4. Review the declared capabilities and select **Save custom skill**.
5. Use it in chat, edit it again, export its JSON package, or remove the installed personal skill.

To install an external skill, choose a JSON package or `SKILL.md` and review the installation dialog. Markdown imports become prompt-only skills. Installation does not execute scripts. User-installed skills are encrypted locally. Exported JSON is readable text: review it before sharing. The SDK includes a schema and examples under `sdk/assistant-skills` in the source project.

## Project work and explicit consent

In **Projects and code**, choose a project folder. This authorizes listing and reading supported project text files; it does not authorize automatic writes or process execution. Select a file to discuss it in chat.

For code changes, describe the goal and generate a proposal. The local model proposes one exact replacement in the selected file. Preparing a proposal writes nothing. **Review and approve** shows the concrete changes; only **Apply the displayed changes** writes them. Changed files are checked against the preview snapshot. Encrypted backups preserve originals, and restoration has its own confirmation and refuses to overwrite subsequent changes.

For Python, PowerShell or JavaScript scripts, select the script, enter literal arguments as a JSON array, and review its interpreter, path, code and arguments. Each execution requires **I authorize this execution**. Scripts run with your Windows account permissions and **are not sandboxed**; they can access data outside the project. PowerShell execution policy is not bypassed. Output and exit status are captured; Stop terminates the process started by ASTRA. The timeout is configurable; zero disables the timeout. Closing ASTRA cancels its running job.

## Routines and automations

Five built-in routines cover study, research, Creator delivery, Gaming and STEM review. Review the steps before pressing Run. Personal routines use explicit `navigate`, `task`, `remember` and `focus` steps, and are saved encrypted. Scheduling creates a disabled widget automation; enable it in Widgets when desired. Automation requires ASTRA to remain open. These routine steps do not authorize project edits or scripts.

## Desktop functions

The Control Center organizes system settings and existing ASTRA tools. Windows owns hardware settings, users, networking, updates and security. The application opens those original panels; it does not claim to implement another operating system's internals.

File Explorer now supports folder creation, file/folder copying, moving, renaming, Windows file clipboard operations, ZIP creation/extraction, native Recycle Bin dialogs, bounded recursive search and undo of the most recent move/rename. Quick Look previews supported text, images, folders and PDF pages. Copy and extraction preserve existing destinations; archive traversal, symbolic links and unsafe Windows names are rejected. Folder copy is bounded at 10,000 items/8 GB; ZIP operations at 5,000 files/2 GB. Use native Explorer for larger operations. Cross-volume folder moves may require copying through native Explorer.

The session clipboard shelf is manual and memory-only, with no continuous monitoring. Lock, restart and shutdown are explicit user actions; restart and shutdown show a confirmation. Existing window tiling/session restoration, dock, widgets, themes, STEM tools and Creator/Gaming labs remain available.

## Validation and practical limits

Automated tests cover file roundtrips, cancellation, archive traversal, encrypted routines, skills, chat pagination, proposal preparation without writes, stale-change rejection, approved application and backup restoration. Live public HTTPS reading and actual search results were checked. Native integration checks are recorded separately in the included package verification.

The local model can make mistakes. Skills improve workflow guidance rather than converting a small model into a frontier model. Creator review uses available metadata and supplied content; Gaming advice uses measured telemetry and does not invent FPS or live game state. Native settings availability varies by Windows version and hardware.
