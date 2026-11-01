# ASTRA 2.0 – Security & release policy

The stable default enables **declarative plugins** (v3) only. These manifests provide a finite set of text transforms, validated before use. No executable is launched, no script engine is embedded, and no plugin-provided file or URL is accessed. This is not a general-purpose third-party sandbox; the manifest is *data*, not code.

Native plugins (legacy v2) remain for development compatibility, **disabled in the UI by default**. Enabling them for a single running session explicitly warns that native `.exe` code inherits the user's Windows privileges; its SHA-256 verification is an integrity check, **not** a sandbox or Authenticode verification. A stable public release may ship the SDK sources without enabling arbitrary native plugins.

Local AI: opt-in `llama-server.exe` from a trusted publisher and separately downloaded GGUF. ASTRA does not bundle models. The server runs on loopback with temporary token, but the machine/user account must be trusted. Encrypted notes use DPAPI CurrentUser for data at rest, not an isolation boundary from other software running as the same user. No default telemetry or cloud inference.

3D: local assets only, CSP prevents outbound connections, WebView2 is unloaded when the user leaves the tab. Always validate navigation and keep WebView2 and Windows up to date.

ISO: only repack legitimate input media locally. Never distribute Windows-containing media or product keys with ASTRA without the applicable Microsoft rights. Builder plan mode is the default. No automatic OS deployment or security bypass.

## Release gate

**DO NOT call this project Stable until all are independently evidenced:**

1. Windows 11 Home x64: clean VM app build, launch, close, install, uninstall; no loss of user data.
2. Windows 11 Pro x64: same validation, including GPU/no-GPU and WebView2 missing behavior.
3. Local inference with actual llama-server + GGUF, cancellation, offline traffic verification, and encrypted notes recovery.
4. Native-plugin feature disabled by default, manifest negative tests, executable integrity test, test of consent.
5. ISO local build + boot in both UEFI and supported BIOS firmware, no Windows redistribution.
6. Measured cold/warm startup, idle CPU/RAM, 3D FPS and UI frame-time on declared hardware; published test traces.
7. Dependency/license review, SCA, source provenance, Authenticode signing, installer malware scan, independent security review.

On Linux, static tests can be run but cannot substitute for these Windows, hardware and legal release gates.
