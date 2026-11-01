# Performance + Security engineering gates

## Targets (NOT claimed results)

Reference test machines (select at least two):
- Mainstream: Windows 11 Home x64, 6-core CPU, 16 GB RAM, integrated GPU, NVMe.
- High-end: Windows 11 Pro x64, modern CPU, dedicated GPU, 32 GB RAM, NVMe.

| Indicator | Proposed gate | Measurement |
|---|---|---|
| Warm startup to meaningful interactivity | <= 2.0 s median | ETW/WPA on 10 warmed launches; user input probe |
| Cold startup to meaningful interactivity | <= 4.0 s median | ETW/WPA after restart or cache-cleared test, 10 runs |
| Idle RAM of ASTRA desktop | <= 200 MB median | Process Working Set after 30 seconds, 10 runs |
| Idle CPU overhead | <= 1% of all logical processors | 30 s idle, 10 runs, subtract baseline |
| UI frame budget | <= 16.7 ms for >= 95% of animation frames | Windows Performance Recorder/Analyzer or profiler trace |
| UI long hangs | No UI thread stalls >= 200 ms during routine navigation | Profile trace and automatic UI test |
| Startup network requirement | None | Run offline (firewall isolation), should launch normally |
| Extra resident services | 0 required | Services and startup entry audit |

Deployment tradeoff: this preview uses self-contained Windows App SDK for easy installation, even though Microsoft documents **more memory use and potentially slower load times** than framework-dependent deployment. Before release, benchmark both self-contained and framework-dependent packaging, and switch if shared-runtime deployment wins on target machines.

These numerical limits are **project acceptance criteria**, not hardware-independent promises. Some accessibility settings, disk loads, GPU conditions and Windows background processes may affect measured performance. RAM target is especially subject to WinUI and Windows App SDK runtime version/diagnostic state.

### Benchmarking workflow

1. Build a **Release** desktop binary, record exact OS build, drivers and power settings.
2. Run `./scripts/benchmark.ps1 -ExePath ./dist/AstraOS/Astra.Desktop.exe -Runs 10 -IdleSeconds 30` for basic window-appearance, idle CPU and working-set numbers. It writes `astra-benchmark.csv` and does **not** measure true user-interactivity or frame times.
3. Record ETW with Windows Performance Recorder; inspect UI/XAML activities, cold/warm startup and expensive frames with Windows Performance Analyzer.
4. Disable ASTRA startup apps and compare with clean Windows to quantify **added** overhead, not OS overhead.
5. Repeat on Home and Pro machines and post anonymized benchmark conditions/results.
6. Gate releases on regression thresholds, not on subjective "non lagga" claims.

### Hardening checklist

- Never disable Defender, Firewall, SmartScreen, Secure Boot, BitLocker or Windows Update.
- No UAC bypass, unsigned kernel drivers, injected Explorer hooks or undocumented patches.
- App processes should run unelevated; insist on confirmation for all external downloads/installs.
- Do not persist credentials or private telemetry. Store profile selection only in user-local JSON.
- Curated WinGet package list; sanitize all IDs; use `ProcessStartInfo.ArgumentList` for process launch; no arbitrary manifest executable commands.
- Let WinGet display package agreements and consent prompts. WinGet/package vendors retain responsibility for installed software; independently review packages before adding them to a trusted manifest.
- Windows App SDK versions pinned. Before public release: SBOM, dependency audit, vulnerability scan, lockfile, signed installer with timestamped certificate, SHA-256 checksums, provenance and reproducible Windows CI.
- No runtime downloading of untrusted plugins, no remote code execution features, no optional startup AI engine.
- Security Center merely offers links to native Windows settings; its text is NOT a machine security attestation.
- Test installer/uninstaller in a VM (Windows Home/Pro), including standard non-admin user, Windows Updates, repair, uninstall/reinstall and failure rollback.

### Realistic security interpretation

No consumer application can guarantee zero exploits. The target is small attack surface, predictable behavior, least privileges, explicit permissions, reviewable code, patch management and independent testing.
