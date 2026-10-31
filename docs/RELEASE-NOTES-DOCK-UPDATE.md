# ASTRA OS V4 Horizon — Desktop and Second Brain Update

Windows x64 portable pre-release, including the local AI and speech components. Final native integration: 31 checks passed, including dock switching and a real local AI answer from page 121 of an indexed PDF. The complete package verifies every included AI component against its pinned manifest.

Antivirus compatibility: Norton previously flagged `llama-server.exe` as `IDP.Generic` on the development PC. The owner resolved the local alert and authorized testing and distribution; the final inference test passed. The file matches the pinned upstream manifest. This does not establish universal antivirus compatibility or prove that a warning on another PC can be ignored. Verify the release checksums and follow your device's security policy.

Open applications now appear directly in ASTRA's dock, so switching windows no longer requires opening the Windows Start menu.

Second Brain keeps its name and gains an integrated local personal-assistant workflow: voice conversation, encrypted document memory, long-PDF retrieval and complete summaries, plus supported productivity and application commands. The included local model and tools require no AI subscription or cloud API account.

- One icon per open application window, with its original window title on hover and a running indicator.
- Click to bring a window forward and restore it when minimized. Maximized windows retain their size.
- Right-click an application icon to activate or minimize that specific window.
- App buttons remain inside the ASTRA window; other applications keep the full screen area available.
- Desktop files, shortcuts and dock applications use native Windows icons, with transparent pixel handling and a visible fallback.
- Automatic refresh, a scrollable layout, keyboard-accessible controls and Windows-language interface labels.
- Windows shell surfaces, ASTRA helper windows, owned tool palettes and non-activating overlays are filtered out. Window titles are kept only in memory.
- Long PDFs are indexed page by page, including local OCR for scans. Search can find evidence on the final pages; complete summaries process all readable pages rather than only an opening excerpt.
- PDF progress, cancellation, duplicate detection, page references and encrypted storage are included. Limits are 512 MB, 10,000 pages and 50 million extracted characters per document.
- Second Brain supports microphone transcription on request, spoken answers, recent conversational context, facts saved to encrypted notes, focus timers, task creation, installed-app launches and a task briefing.
- Voice and typed commands include English and Italian forms. Source documents and generated replies cannot run arbitrary commands.

This release changes the application code and compiled XAML. Extract the entire updated Windows x64 package into a new folder; do not replace only the EXE. Close the old ASTRA instance before using the new one. Existing per-account preferences and personal data remain in their existing location. If login startup is enabled, disable it in the old copy and enable it in the new copy.

The app uses normal Windows foreground-window rules. Protected/elevated applications may restrict switching. Some protected/packaged apps use a generic icon, while their window title remains available.

Validation includes a Release build, dock eligibility and icon tests, Core regressions, and a dedicated native test window for switching, minimize/restore, maximization, icon rendering and absence of extra overlay windows. See the included verification report for actual results.

PDF regression tests cover retrieval at page 1201, a real 121-page PDF, scanned-page OCR, encrypted persistence, cancelled imports and full-document batch coverage. Local AI responses use retrieved evidence; generated results still require normal user review.
