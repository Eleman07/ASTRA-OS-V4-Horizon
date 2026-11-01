# ASTRA OS V4 — Horizon Design, Privacy & Security

## UI reference

The user-approved UI mockup is preserved in `docs/V4-REFERENCE-UI.png`. The native implementation is `src/Astra.Desktop/MainWindow.xaml` and `MainWindow.V4.cs`. The design-preview browser page is a visual testing aid, **not the Windows application**.

### Default design tokens (exact)

| Role | Value | Used for |
|---|---|---|
| Navy | `#0A1931` | shell, navigation, dark hero |
| Ice | `#B3CFE5` | supporting surfaces |
| Blue | `#4A7FA7` | accents, selection and chart |
| Deep | `#1A3D63` | dark widgets and dock |
| Snow | `#F6FAFD` | light cards |

Users can edit all five tokens in **Personalizza → Applica e salva**, or restore the default. Values must be exactly `#RRGGBB` and are normalized and validated in the UI-independent core. Changes persist under the current Windows account in `%LOCALAPPDATA%\ASTRA\OS\preferences.json`, with atomic save. Shared SolidColorBrush resources update live, so a reboot is not needed. Text colors are computed for contrast. Decorative blue wallpaper/banner imagery are suppressed when a non-default palette is selected, avoiding clashes.

### The banner animation

The banner image is rendered natively and animated using the WinUI Composition `Translation` property, with a 20-second subtle keyframe loop. It is **not a continuously playing video** or a JavaScript interval. Its animation stops when the user leaves Home, the window is inactive, Eco is enabled, or reduced motion is enabled. Custom wallpaper is produced as a static PNG. The V4 preview uses a CSS analogy; it does not prove WinUI frame rate.

### Actual data versus illustrative reference

The design mockup contains illustrative figures such as CPU 42%, GPU 68% and user tasks. ASTRA V4 does **not** hardcode any of those. The native dashboard shows `—` until the user presses Refresh. It measures only ASTRA process CPU/RAM and actual system disk utilization; GPU is `Non disponibile` until a reliable per-adapter counter is implemented. The productivity chart counts completed tasks from the user's local encrypted task store. It does not impersonate Windows Defender or claim a machine is secure without inspection.

## Privacy and threat model

- All app profiles, colors and optional modules are locally stored; the OS base remains Windows 11.
- Task data is encrypted with Windows DPAPI `CurrentUser` and saved to `%LOCALAPPDATA%\ASTRA\OS\tasks\tasks.dpapi`. The Second Brain retains its own DPAPI-encrypted notes.
- Tasks are not uploaded. The separate HTML **preview** deliberately stores sample tasks in browser localStorage; it is labeled as a preview and should never be used for sensitive tasks.
- The new dashboard performs no polling for CPU/GPU. There is only a minute-resolution wall clock timer and one user-triggered diagnostic sample.
- An arbitrary local AI runtime has normal-user privileges. Do not run binaries from untrusted sources. The server remains loopback-bound with an ephemeral credential; this is not an OS-grade sandbox.
- Native plugin execution remains disabled in the public UI. Declarative plugins can only execute approved text operations, not arbitrary shell commands.
- WSL 2 and Ubuntu remain optional and require user action. Ubuntu security mechanisms do not extend the Windows kernel security boundary.
- ISO Builder only processes sources obtained by the user; no proprietary Windows files are distributed in this repository.
- The install manifest requests `asInvoker`. System protection settings are never silently disabled.

## Release blockers (must pass before using *Stable*)

1. `dotnet run --project tests/Astra.Core.SmokeTests` on Windows 11 (includes DPAPI and custom theme roundtrip).
2. `dotnet publish src/Astra.Desktop/Astra.Desktop.csproj -c Release -p:Platform=x64 -r win-x64 --self-contained true -o dist/AstraOS` and native launch.
3. Visual comparison to the approved reference at a common window size, plus 125%, 150% and 200% scaling.
4. Verify palette editing, reset, restart persistence and contrast for radically different themes.
5. Verify banner movement at normal speed; pauses on background, Eco and Windows reduced motion.
6. Verify modules: Core, STEM, Gaming, Creator, AI model start/stop + private notes, Plugin SDK safety, Orbit 3D, Ubuntu WSL, ISO Builder dry-run.
7. Run CPU/RAM GPU/battery/standby benchmarks on low and high-end hardware; include a 30-min idle soak and crash logs.
8. Audit dependencies, signed release artifacts, user opt-in, uninstall/rollback, accessibility, DPI and keyboard navigation.

Passing static XML/source tests is **not** evidence of a pixel-perfect UI, native compilation or security certification.
