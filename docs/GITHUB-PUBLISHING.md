# Publish the ASTRA Windows package on GitHub

This guide is for the repository maintainer. Preparing the files does not publish them automatically.

## Files to attach

After running `scripts/package-release.py`, these files are in `dist`:

| File | Purpose |
| --- | --- |
| `ASTRA-OS-V4-HORIZON-win-x64.zip` | Complete runnable Windows x64 application, tutorial, models, runtimes, and prerequisite installers. |
| `ASTRA-OS-V4-HORIZON-win-x64.zip.sha256` | SHA-256 checksum of the ZIP. |
| `ASTRA-OS-V4-HORIZON-win-x64-README.md` | Standalone English user guide, also included as `README.md` inside the ZIP. |
| `ASTRA-OS-V4-HORIZON-win-x64-RELEASE-NOTES.md` | Release description ready to paste into GitHub. |

The verification report and per-file manifest are also generated locally. A copy of `FILES.sha256` is included inside the ZIP.

## Create a GitHub pre-release

1. Create or open the GitHub repository that will host ASTRA. Put the English `README.md` and `LICENSE` at the repository root. If publishing source, commit the source tree separately; keep `dist`, build outputs, and personal/QA data out of Git history.
2. On the repository page, open **Releases → Draft a new release**.
3. Choose an existing tag, or create a tag for this build, such as `v4.0.0-horizon-rc.20261028`. Select the appropriate source commit/branch if this repository contains the source.
4. Use a title such as **ASTRA OS V4 Horizon — Windows x64 Release Candidate**.
5. Paste the contents of the prepared release-notes file. Point users to the named Windows ZIP under **Assets** for installation.
6. Attach the ZIP, its `.sha256` file, and the standalone English README. Attach the release-notes Markdown file too if desired.
7. Select **This is a pre-release**. Review the draft and its attachments, then publish when you are ready.

Upload the binary ZIP as a **Release asset**, rather than committing it as a normal repository file. GitHub currently requires each release asset to be smaller than 2 GiB; the packaging script enforces this limit. The automatically generated **Source code (zip)** does not replace the runnable binary package.

## Verify before publication

Compare the ZIP's hash with the generated `.zip.sha256` file:

```powershell
Get-FileHash .\dist\ASTRA-OS-V4-HORIZON-win-x64.zip -Algorithm SHA256
```

The packager independently reads every ZIP entry and checks its hash against the file added to the archive. It excludes browser caches, profile data, logs, and development-only artifacts. It retains the third-party model credits and component licenses. Treat the existing local test results as evidence for this release candidate, rather than a claim of testing every possible PC.

## Official references

- [GitHub: managing releases](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository)
- [GitHub: release asset size limits](https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases)
- [Microsoft: WebView2 distribution](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution)
- [Microsoft: Visual C++ runtime downloads](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist)
