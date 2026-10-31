# ASTRA OS V4 · Horizon — release candidate

Prepared for Windows 11 x64 on October 28, 2026.

## Included experience

- Responsive Home with real Desktop file icons, a custom File Explorer, themed Orbital 3D, an animated central sphere, and an icon dock with tooltips and quick access.
- Custom colors, wallpaper and animated banners, optional Windows cursor themes with restore, full screen, and optional startup at sign-in.
- Windows account information, Core/STEM/Gaming/Creator profiles, study-path filtering, real-window multitasking, and saved sessions.
- Thirteen educational numerical models, charts and 3D replays, a ten-model library with credits including an ASTRA Ariane 64 reconstruction, personal GLB/glTF/OBJ/STL import, and a Python scene SDK.
- Local Qwen text AI and Whisper speech transcription, Windows OCR and speech output, file input and multiple export formats, and encrypted Second Brain notes with offline passage retrieval.
- Eight widget types with in-app automation, 43 declarative plugin recipes, installed Windows tool discovery, Gaming session telemetry/HUD, and Creator boards, palettes, storyboards, and subtitle timing.
- Windows language selection, bundled English/Italian catalogs, optional locally generated translations, reduced motion, and adaptive light/Eco settings.

## Runtime package

The complete app folder includes self-contained .NET and WinUI runtimes, local models, assets, and documentation. Official Microsoft x64 offline installers for WebView2 and the Visual C++ runtime are included separately in `prerequisites`. They are not installed automatically.

Read `README.md` for the tutorial, including **Windows 11 → Personalization → Taskbar → Taskbar behaviors → Automatically hide the taskbar**, full-screen mode, and startup at sign-in.

## Validation

The application binaries are the already verified October 28 build; this packaging update changes documentation and adds prerequisite installers, without changing the application executable.

- Release and Debug builds completed without warnings or errors.
- 349 Core checks and 47 Python/static checks passed.
- 115 native checks passed in Italian, and 115 passed in English on the published distribution.
- Native checks covered 3D library loading/replay, model imports, OCR, synthetic speech/transcription, local AI, widgets, profiles, multitasking, Gaming/Creator functions, and the responsive banner at six window sizes from 390 to 3440 pixels.
- Packaging verifies the ZIP's complete file list and the SHA-256 of every payload file. Microsoft prerequisite installers are checked for a valid Microsoft Authenticode signature before packaging.

The microphone was not activated by automated tests; low-resource behavior was tested as a software policy rather than benchmarked on a second physical PC. Clean-machine testing across all Windows editions and independent review remain outside this verification.

## Practical limits

This is a **release candidate**, not a declaration of a stable or independently certified release. The ASTRA executable is unsigned. Simulation results are educational, and imported geometry does not create a complete physical model. Ariane 64 is an original educational reconstruction, not ESA CAD or endorsement. AI output needs review; image input uses OCR. Widgets need ASTRA running, and the Gaming HUD may not appear above exclusive full-screen games. WSL distributions, Windows installation media, ADK, Python, and Chrome are optional external components and are not bundled.

See `THIRD-PARTY-NOTICES.md` and the included license files for component credits.
