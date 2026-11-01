# AI startup fix and Windows installer

This release fixes saved AI paths after moving, extracting or updating ASTRA. Both the included engine and bundled GGUF model now resolve relative to the current application folder. Old `AstraOS-Local` references no longer prevent startup. Custom model/runtime selections are preserved, including a missing custom path that needs reselection.

Advanced AI settings now describe the integrated engine correctly. **Use included AI** restores both included paths. **Start local model** and automatic chat startup share the same validation and loading path. The integrated CPU engine does not need `llama-server.exe` or GPU activation.

## Recommended: Setup EXE

1. Download **ASTRA-OS-V4-HORIZON-Setup-4.0.0.1-win-x64.exe** from the private GitHub release's Assets.
2. Close the running ASTRA copy before installing or updating.
3. Open the setup, review its license/components and confirm the destination. It installs for the current Windows user; the default folder is under LocalAppData/Programs.
4. Start **ASTRA OS Horizon** from the Start menu. A desktop shortcut is optional.
5. If a Microsoft runtime is missing, use the included **Prerequisites** Start menu entry and run the appropriate official Microsoft installer. These prerequisite installers can require administrator approval.
6. Open the assistant and send a message, or expand advanced settings and select **Start local model**. Use **Use included AI** to reset a custom configuration when desired.

The setup includes the app, models, libraries, tutorials, SDK examples, licenses and offline Microsoft prerequisite installers. It does not download a model during installation. ASTRA's data stays in its existing per-account location; application removal preserves notes, PDFs, chats and preferences. Removing the application does not uninstall shared Microsoft runtimes.

Windows may show an unknown-publisher notice because the ASTRA setup has no Authenticode signing certificate. The release provides its SHA-256. Do not disable security protection to install it.

## Portable alternative

Download **ASTRA-OS-V4-HORIZON-ai-startup-fix-win-x64.zip**, extract the complete folder and run `Astra.Desktop.exe`. Do not copy only the EXE. Both installer and portable editions use the same tested application binaries.

For a full desktop experience, follow README.md to enable Windows 11 **Automatically hide the taskbar**, full-screen startup and optional launch at login. These settings are user choices.

## Verified regression

The native test starts with obsolete runtime/model settings matching the reported problem, confirms their migration and persistence, clicks the real Start button and checks real model output. All 26 native checks pass, along with 382 Core checks and 47 static/Python tests. Installer verification is recorded separately after a full install/uninstall roundtrip and byte-for-byte payload checks.
