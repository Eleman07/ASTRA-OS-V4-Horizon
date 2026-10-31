# Publish only the binary release

Repository: [Eleman07/ASTRA-OS-V4-Horizon](https://github.com/Eleman07/ASTRA-OS-V4-Horizon).

This update is a portable binary release. It does not require applying a Git patch, copying source files into the repository, committing, or pushing a branch. Keep the existing release available while preparing this one.

1. Confirm that `ASTRA-OS-V4-HORIZON-assistant-system-update-win-x64-verification.json` reports `allPayloadHashesMatch: true` and `nativeChecksPassed: true`.
2. Compare the ZIP's SHA-256 with the accompanying `.zip.sha256` file.
3. Open the repository's **Releases** page and prepare a new draft release.
4. Use a new tag, for example `v4.0.0-assistant-system.20261029`, targeting the existing main branch. Do not reuse or overwrite the previous tag.
5. Title it **Horizon — ASTRA Assistant and desktop tools** and use the contents of `RELEASE-NOTES-ASSISTANT-SYSTEM.md` as its description.
6. Attach the complete named ZIP, its `.zip.sha256`, the `-verification.json`, the `-README.md`, and optionally `ASSISTANT-SYSTEM-UPDATE.md`.
7. Mark it as a **pre-release**. Review the filenames and description, then publish when ready. Repository visibility stays private unless you explicitly change it separately.
8. Download the named Windows ZIP from Assets, extract the complete folder, close the old copy, and run `Astra.Desktop.exe`. The automatically generated source archive is not the runnable distribution.

The archive includes the free local model and CPU libraries, prerequisites, licenses, skill examples and English tutorials. User notes, chats, passwords, browser caches and QA data are excluded. No source push is necessary for this workflow.
