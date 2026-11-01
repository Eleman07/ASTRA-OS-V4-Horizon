# ASTRA OS 1.1 — Advanced Features: implementation and limits

This source preview runs **on an existing licensed Windows 11 Home/Pro**. It is not an independent operating system, and is neither affiliated with nor endorsed by Microsoft.

## Module architecture

```text
Astra.Desktop (WinUI 3, no always-on service)
  ├── Core: profiles, install catalog, preferences
  ├── Second Brain UI
  │    ├── EncryptedNoteStore (DPAPI CurrentUser -> %LOCALAPPDATA%/ASTRA/OS/brain/notes.dpapi)
  │    ├── NoteIndex (local keyword retrieval: title boosting, max 4 notes)
  │    └── LocalLlmSession (explicit llama-server.exe + user .gguf)
  │         └── 127.0.0.1 random port, temporary API key file, /v1/chat/completions
  ├── PluginHost: explicit consent -> subprocess stdin/stdout JSON API v1 (8s timeout)
  ├── OrbitView: WebView2 (lazy) -> offline procedural WebGL assets + CSP
  └── Image Builder UI: confirmed PowerShell script -> Windows ADK oscdimg
       └── local Windows media folder + ASTRA publish -> private bootable ISO
```

## Second Brain

Built in: encrypted text/Markdown notes, local note search, bounded note retrieval and context tagging, chat, structured summarization, flashcards, technical explanations and work plans. Conversation context is kept **in process memory**, up to four recent turns. Notes reside at `%LOCALAPPDATA%\ASTRA\OS\brain\notes.dpapi` encrypted using Windows DPAPI with `CurrentUser`; the data cannot be moved to a different Windows identity without suitable migration. No cloud endpoint or telemetry is coded into this feature; all inference uses the user-provided llama.cpp executable and GGUF model. **No weights, no third-party executable, and no remote service are bundled.** Users must independently obtain trusted model and runtime files and follow their licenses.

1. Obtain `llama-server.exe` from the official llama.cpp project and a compatible, properly licensed GGUF model. Review download signatures / releases and model licenses before use. For less capable computers begin with a small quantized model, ~1–3B parameters (responsiveness varies greatly with RAM and CPU).
2. Open **Second Brain**; supply absolute paths to `llama-server.exe` and `.gguf`, then confirm executing the external runtime.
3. Add notes manually or import a local `.txt` / `.md` file <=16KB. You may delete notes, clear volatile conversation history, and stop the child process manually.
4. Ask questions or select flashcard / summary modes. ASTRA retrieves up to 4 top-matching notes using **lexical keyword ranking**, not semantic embeddings, and sends excerpts only to the local authenticated model. If no overlap exists, no notes are included. Responses may hallucinate; verify claims and sources.

**Privacy guarantees are limited by OS trust:** DPAPI protects data at rest against users without access to the current Windows identity, but it does NOT prevent malware and same-account processes from reading decrypted data or calling DPAPI, and it does not prevent malicious llama.cpp/model distributions from collecting data. Local HTTP loopback traffic is not cloud traffic; however a compromised PC can still be compromised. No security audit has been performed. ASTRA does not provide a promise of absolute privacy.

## Plugin SDK

Public protocol `plugin.json` + a dedicated .exe with JSON line stdin/stdout. Plugins are found under `%LOCALAPPDATA%\ASTRA\OS\plugins\<plugin-id>\`, with validated ID, executable basename and JSON schema. Only the explicit text entered in the plugin panel is shared. Manual approval is required **for every run**. An 8-second execution deadline and 16KB output cap are imposed.

**External executables are NOT sandboxed.** They have the same user's OS permissions and can access disk and network. Out-of-process hosting is crash isolation, not a security boundary. This feature is a **developer preview**, not safe for untrusted plugin marketplaces. Strong per-capability AppContainer/WASM sandboxing and code signing are required before recommending third-party public plugins. See `sdk/README.md` for sample build.

## Optional 3D

The existing WinUI app offers an offline orbital visual rendered in an on-demand WebView2 using WebGL with a procedural sphere; it is capped at ~30 FPS, resolution/DPR capped and when leaving the panel its page is navigated to `about:blank`. Strict Content Security Policy prohibits external requests from the scene and all resources are local. No JavaScript host bridge or `eval` is used. **This is an interactive 3D workspace inside ASTRA, not a Windows Explorer shell replacement, system-wide 3D wallpaper, or VR environment.** WebView2 Runtime is required; stopping rendering does not guarantee immediate deallocation of all Edge/WebView2 processes. Monitor performance on real hardware.

## Local ISO Builder

`New-AstraIso.ps1` builds locally from **user-supplied official Windows installation media** already mounted/extracted, published ASTRA app files, and the Windows ADK `oscdimg.exe`. Default behavior is a *non-mutating prerequisite/plan check*. With explicit `-Build`, the script copies the installation files into a temporary private directory, adds a manual-run `ASTRA-OS/` folder, and constructs a UEFI/BIOS bootable ISO with Windows Boot Manager files from the user's media. Original Windows installation images and security components are not modified. It does NOT automatically install ASTRA into the WIM/ESD and is NOT a license-free Windows installer.

```powershell
# PowerShell on Windows, run in the repository directory (after scripts/build.ps1):
./scripts/New-AstraIso.ps1 -SourceMediaDirectory 'D:\' -AstraDistributionDirectory '.\dist\AstraOS' -OutputIso 'C:\ASTRA\ASTRA-local.iso'
# After reviewing the dry-run output, confirm local ISO creation:
./scripts/New-AstraIso.ps1 -SourceMediaDirectory 'D:\' -AstraDistributionDirectory '.\dist\AstraOS' -OutputIso 'C:\ASTRA\ASTRA-local.iso' -Build
```

Requires free space significantly exceeding ISO size because it copies the complete media before building; test the output in a VM before installing on physical hardware. Builds containing Microsoft binaries are **private** unless the user has redistribution rights; only distribute ASTRA code, build scripts and the application itself publicly. Do not disable Secure Boot / Defender. `oscdimg` paths follow the installed Windows ADK. Media with special boot arrangements or unsupported structures may need adjustments. The ISO builder has not been executed/tested against actual Windows media in this environment.

## Threat model / performance release gates

| Component | Risk | Mitigation | Residual / next gate |
|---|---|---|---|
| AI model runtime | Untrusted executable | Manual opt-in; loopback + temporary key; no admin | Signed verified binaries, isolation, telemetry monitoring |
| Private notes | Data exposure at rest | CurrentUser DPAPI, no cloud sync | Same-user malware and memory disclosure remain possible |
| Third-party plugins | Arbitrary code | Approval every run; validation; timeouts | No OS sandbox: **do not run untrusted plugins** |
| WebGL 3D | GPU/CPU overhead | Off by default, ~30 FPS, unload page | Benchmark startup, CPU/GPU usage, context loss recovery |
| ISO media | Boot failure/license | Only local user media, preview/dry-run | VM BIOS+UEFI boot tests, Microsoft licensing compliance |

### Before a stable public release

- Build on Windows 11 Home and Pro x64, test on different hardware (including battery-powered PCs).
- Automated unit tests, repeatable install/rollback, malware scan, dependency scanning, source audit, trusted code signing and update security.
- Measure cold/warm startup p50/p95, app private working set and background CPU when AI and 3D are off, latency of AI on CPU-only model and WebView2 GPU use when active.
- For plugin security, deploy a real OS-enforced sandbox and allow specific permissions; current plugin SDK is **experimental**.
- For privacy claims, conduct independent penetration test and verify absence of outbound calls with OS network monitoring.
- For ISO, boot validation in UEFI and BIOS VMs, and ensure no republishing of Microsoft Windows binaries.
