# Offline Windows installer and AI startup fix

Download **ASTRA-OS-V4-HORIZON-Setup-4.0.0.1-win-x64.exe** from Assets, close the running ASTRA copy, and follow Setup. The app, local AI model, CPU/speech libraries, 3D resources, skills, SDK examples, documentation, licenses and Microsoft offline prerequisites are included.

## Installer

- Installs for the current Windows user, by default under LocalAppData/Programs/ASTRA OS Horizon.
- Creates a Start menu application entry, Prerequisites entry and uninstaller. A desktop shortcut is optional.
- Registers application removal in Windows. Uninstall preserves the separate notes, PDFs, chats and preferences store and leaves unknown user-created files in the application folder.
- Does not start AI, enable Windows login startup or run Microsoft prerequisite installers automatically. Install a missing Microsoft runtime explicitly from the included Prerequisites folder when needed.
- Requires Windows 11 x64. The setup is not Authenticode-signed; its SHA-256 and installer verification report are attached.

## Fix for the reported startup error

The previous configuration could keep a model path in the old `AstraOS-Local` folder after an update. That file was no longer present, so Start reported that a selected file did not exist.

Both included runtime and model references now migrate to the current application folder and are saved correctly. Moved integrated-runtime settings are handled too. Custom GGUF/runtime selections remain custom and report a clear error if unavailable. **Use included AI** restores both bundled file paths.

Advanced settings now describe the integrated CPU engine correctly instead of asking for a separate `llama-server.exe`. Manual Start and automatic chat startup use the same validation/loading path.

## Validation and retained features

- 382 Core checks and 47 static/Python tests passed.
- 26 native checks passed, starting from obsolete settings, clicking the actual Start button, obtaining real local model output and verifying conversation/file-approval behavior.
- The attached installer verification records installation, every installed payload hash, native checks on the installed copy, Windows registration/shortcuts and the uninstall/data-retention roundtrip.
- ASTRA Assistant, 26 skills/custom skill editor, long PDFs, web research, consent-based project tools, 43 declarative plugins, desktop/dock, STEM/3D, Creator/Gaming and Control Center remain included. See the [complete feature catalog from the preceding release](https://github.com/Eleman07/ASTRA-OS-V4-Horizon/releases/download/v4.0.0-assistant-system.20261029/FEATURE-CATALOG.md).

The **ai-startup-fix-win-x64.zip** is the portable alternative with the same application binaries. Extract the entire archive before launching its EXE.

ASTRA remains a Windows desktop application. Local AI has no ASTRA subscription or daily/message quota; hardware, storage and model context/output are finite. STEM models are educational; scripts require explicit consent and are not sandboxed. Ubuntu requires an installed WSL distribution.

Only release assets are published. The repository remains private; previous releases and main are preserved.
