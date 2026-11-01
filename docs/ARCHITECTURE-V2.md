# ASTRA OS V2 — Technical architecture

## Modules and trust boundaries

```text
ASTRA Desktop WinUI 3 (no admin; user-initiated)
  ├─ Astra.Core: strict profile catalog + preference JSON + performance policy
  ├─ Astra App Hub: allowlisted WinGet arguments; confirmation and vendor terms
  ├─ Performance Center: manual read of own process memory/system data
  ├─ SecurityInspector: read-only PowerShell Defender/firewall queries (optional)
  ├─ Second Brain: DPAPI notes -> BM25 passages -> authenticated localhost llama-server.exe child
  ├─ PluginHost: SHA-256 verified binary -> warning/consent -> out-of-process SDK v2
  ├─ Orbital 3D: WebView2 offline mapped local assets; FPS/motion policy
  └─ ISO Builder: official Windows media supplied by user -> explicit plan/build
```

### Runtime

- **No permanent Windows service or mandatory startup entry.** Functions are loaded or launched on demand. Windows is still responsible for kernel, drivers, updates and security.
- **No background inference.** GPU/CPU inference runs only when the user starts the process; stopping clears ephemeral ASTRA chat history.
- **Profiles:** Core always enabled, STEM, Gaming and Creator independently toggled. Profiles change suggestions/workspaces, not kernel tuning. Package IDs curated in `CuratedCatalog.cs`.
- **Performance policy:** Eco cap <=30 FPS, reduced motion suppresses automatic orbit rotation and caps at 15 FPS; without Eco the user may select 15, 30 or 60 FPS. The 3D view is unloaded on leaving the page.
- **Local notes:** 1,000 note and 16 KB/note limits, ciphertext at rest with Windows CurrentUser DPAPI. Search computes bounded BM25-like passage scores with no external service. Citations are note IDs, not scientific literature verification.
- **Plugin SDK v2:** only `text.transform` operation is supported. Plugin `plugin.json` holds a filename, SHA-256, ID and description; hash is recalculated before execution. This is *integrity pinning* and explicit opt-in, **not an OS-enforced sandbox**. Plugins run under the interactive user account and could access other files/network; users must trust them.
- **Image Builder:** local media verified by required setup files; no modifications to Windows installation internals, no unattended keys, no license bypass, no redistribution.

## Development backlog to stable V2

1. CI compilation and integration tests on fresh Windows 11 Home/Pro VMs, clean install/update/uninstall and rollback.
2. Reproducible build, signed installer and artifacts, CycloneDX SBOM, dependency and malware scanning, vulnerability monitoring.
3. Actual restricted execution for third-party plugins (Windows AppContainer/restricted token or Execution Containers with proper policy verification), separate consent for capabilities; until then no plugin marketplace.
4. AI model metadata/verification (download provenance, license and SHA-256), private importers with file picker and explicit consent, optional hybrid semantic index, resilience tests under low RAM.
5. Accessibility testing: keyboard-only navigation, Windows high-contrast support, screen readers, localization, scalable layouts, motion preference.
6. Real performance profiling: ETW/WPA, cold/warm boot, GPU frame timing, idle overhead and background tasks on reference devices.
7. 3D scene refinements after measured GPU power/thermal budget; avoid shell replacement on Home/Pro.


## V2 Release Candidate security changes

- v3 declarative plugin SDK runs a finite validated transform API in ASTRA itself; it cannot invoke plugin-supplied executables, scripts, files or URLs.
- v2 native plugins are legacy development features, disabled by default in the UI and activated per session at the user's risk. SHA-256 does not imply sandboxing.
- User privacy now includes an explicit delete-all-notes action with a confirmation dialog (does not delete external backups or cloud-synced copies).
- ISO Builder denies reparse points anywhere within user-selected trees, denies pre-existing `.partial` ISO paths and checks simultaneous disk-space consumption.
- Stable releases require external evidence under the fail-closed `scripts/release_gate.py` policy.
