# ASTRA OS 2.0 – Release Candidate gates

The source release is **RC1** and does not claim that Windows executables or a bootable ISO were built or tested.

| Required gate | Status in this authoring environment | How to verify |
|---|---|---|
| Source JSON/XAML contracts | To run offline | `python scripts/validate_source.py` |
| JS syntax | To run offline | `node --check src/Astra.Desktop/Graphics3D/web/scene.js` |
| Windows Core smoke tests | Not available on Linux | `dotnet run --project tests/Astra.Core.SmokeTests/... -c Release` on Windows |
| Windows WinUI 3 compile and startup | Not available on Linux | `scripts/build.ps1`, clean Windows 11 Home+Pro x64 VMs |
| AI GGUF inference and cancellation | Not verified | Trusted llama-server, test on CPU and GPU, localhost inspection |
| Native plugins disabled by default | Source implemented | UI state, integrity tests + adversarial testing on Windows |
| 3D WebView2 and driver compatibility | Not verified | 3D on/off, renderer crash recovery, navigation tests |
| ISO boot | Not verified | Local source media, Windows ADK and a disposable UEFI VM |
| Installer and uninstall | Not verified | Install/uninstall without data loss on Home and Pro |
| CPU/RAM/startup/frame-time objectives | Not verified | `scripts/benchmark.ps1` and ETW/WPA measurements |
| Dependencies, code signing, pentest, license review | Pending | SBOM, external audit and a signed build |

To qualify as stable, use `scripts/release_gate.py` with an evidence manifest containing **each** required test artifact and SHA-256. The default manifest deliberately fails the stable gate. Consult `docs/SECURITY-RELEASE-POLICY.md`.
