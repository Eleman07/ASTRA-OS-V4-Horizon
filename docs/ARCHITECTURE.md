# ASTRA OS 1.0 — Concrete architecture

## Boundary

The existing Windows 11 installation is *not* modified as part of the preview. ASTRA OS is a user-level desktop app, not a Windows fork or a kernel/Explorer replacement. The project uses WinUI 3 for presentation, .NET 10 for its business logic, and a non-elevated installer. This preserves native Windows application, game, driver, DirectX and professional software compatibility.

```text
                         WINDOWS 11 HOME / PRO
  ┌───────────────────────────────────────────────────────────────────┐
  │  ASTRA Desktop (WinUI 3, no permanent monitor/daemon)            │
  │  Home | Profiles | App Hub | Performance | Security              │
  ├───────────────────────────────────────────────────────────────────┤
  │  ASTRA Core (.NET)                                               │
  │  ProfileCatalog / validation / CuratedCatalog / PreferenceStore  │
  ├─────────────────────┬──────────────────────┬──────────────────────┤
  │ Manifest JSON       │ Local settings JSON  │ On-demand Win32 stats│
  │ core/stem/gaming/   │ %LOCALAPPDATA%       │ no background polling│
  │ creator             │                      │                      │
  ├─────────────────────┴──────────────────────┴──────────────────────┤
  │ User-initiated external services: WinGet | Windows Settings       │
  ├───────────────────────────────────────────────────────────────────┤
  │ Windows 11 Explorer / Security / Update / UAC / Driver model      │
  └───────────────────────────────────────────────────────────────────┘
```

## Architectural decisions

1. **Separation of concerns**. `Astra.Core` contains pure .NET data/persistence logic; GUI cannot directly manufacture unknown package commands.
2. **Four composable profiles**. Core is always active. STEM, Gaming and Creator select curated optional software but do not silently install it. A future version may add shortcuts, workspace templates and project environments.
3. **Whitelisted package installation**. Profile manifests accept *only* IDs in `CuratedCatalog`, with full-string exact matches. The app opens WinGet only in response to a user click and after a confirm dialog; no shell commands or PowerShell scripts execute from manifests.
4. **One-click security links**. Native Windows Security and Windows Update remain the source of truth. ASTRA's UI does not claim they are active without system queries and does not change their settings.
5. **No elevated default**. `asInvoker`; per-user installation via `{localappdata}`. Drivers and administrator-level OS tweaks are intentionally absent.
6. **No permanent measurement**. System statistics are acquired on demand asynchronously with `Task.Run`. The UI avoids blocking on network calls or scanning installed applications during startup.
7. **Atomic settings save**. Preferences are written to a temporary JSON file and atomically moved into place; invalid selections are discarded on load.
8. **Deployment**. Unpackaged/self-contained WinUI 3 folder, optionally wrapped in per-user Inno Setup. Release binaries must eventually be signed and their integrity published.

## Scope / dependencies

- Windows App SDK **2.4.0 pinned** (official NuGet), C#/.NET **10**, Windows SDK >= **22621**.
- Target `win-x64` only in MVP, Windows 11 minimum OS build **22000**.
- Does not distribute Microsoft Windows or proprietary commercial applications.
- Flat JSON manifests reduce startup I/O and dependency complexity. Upgrade to SQLite only when queries and version migrations justify it.
- No embedded browser/Electron, GPU-heavy background rendering or local AI at startup.

## Proposed future modules (not yet coded)

- `Astra.Dock`: app shortcuts / taskbar companion; no undocumented Explorer hooks.
- `Astra.Workspace`: STEM/Creator/Gaming workspace presets and layout persistence.
- `Astra.Updater`: signed update metadata, release hashing, staged rollback.
- `Astra.PluginSdk`: sandboxing/permissions; no untrusted arbitrary C# DLL execution by default.
- `Astra.Diagnostics`: Event Tracing for Windows (ETW) opt-in and app performance profiles.
- `Astra.Setup`: signed setup with backup/restore for every ASTRA-managed setting.

Avoid replacement of `explorer.exe`, third-party system optimizer scripts, and optional kernel drivers in the 1.0 track.
