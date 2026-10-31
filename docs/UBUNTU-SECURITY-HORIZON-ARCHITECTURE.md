# Security, Linux interoperability and UI decisions — V2 Horizon / RC2

## Boundaries

- The **Windows host** always retains the NT kernel and native Windows executable/driver compatibility. Windows Defender, Windows Update, Firewall, Secure Boot and Windows device encryption remain Windows-managed.
- Optional **Ubuntu WSL 2** provides GNU/Linux userspace, packages and CLI toolchains in a virtualized Linux environment. WSL does **not** replace Windows system security nor protect hostile code executed on Windows. Installing WSL may alter Windows optional features and may need reboot/admin approval.
- Ubuntu apt commands are copied as static text and never invoked by ASTRA automatically; WSL install uses static argument lists, explicit confirmation and UAC. Audits are read-only.
- The WinUI application is an app running under the current user; no drivers, shell replacement, always-on background monitoring or developer mode is enabled.

## macOS-inspired design without copying macOS

Horizon adds round-corner surfaces, three colored window-style indicators (decorative only), visual hierarchy, a compact dock, local-only Spotlight-inspired section search and reduced-motion preferences. The dock is an in-app navigation tool and does NOT replace the taskbar / Finder / Explorer. Unknown search strings cannot execute processes. Dock visibility persists in a JSON settings file under LocalAppData.

## Threats and controls

| Threat | Current defense | Limit |
|---|---|---|
| Malicious text in search | exact allowlist of section names | only navigates ASTRA panels |
| Privilege escalation | Windows UAC when user confirms WSL install | approved admin process may change system |
| Plugin code execution | declarative SDK v3 by default | legacy developer-mode native plugins aren't sandboxed |
| Disabled host protection | security dashboard read-only; no Defender/Firewall disabling | reporting is partial and Windows/third-party status can vary |
| Unbounded background load | no mandatory WSL/AI/3D background work | user-started Linux and AI services may consume resources |
| Personal data exfiltration | no ASTRA network telemetry, local DPAPI notes | third-party apps / models / plugins can access user resources |
| Untrusted package repositories | static Ubuntu APT instructions only | installing software still brings supply-chain risk |

## Performance targets, not claims

Desktop idle process RAM <=200 MB; CPU idle <=1%; hot start <=2 seconds on agreed reference machine; 60 FPS for transitions on reference GPU. These are targets, not measured results; WSL startup, Ubuntu package managers, installed applications and VM activities are excluded from ASTRA idle measurements and should be profiled separately.
