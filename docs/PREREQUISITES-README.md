# Optional Microsoft runtime installation

The application folder already includes .NET and WinUI. The two unmodified Microsoft installers here cover additional system runtimes if they are missing from your PC. They are not run by extracting the ZIP or starting ASTRA.

## WebView2 for 3D panels

If a 3D panel reports that WebView2 is unavailable, close ASTRA and run:

```text
MicrosoftEdgeWebView2RuntimeInstallerX64.exe
```

Follow the Microsoft installer, then reopen ASTRA. This is the full Evergreen x64 offline installer. An existing, working WebView2 Runtime generally does not need reinstalling. Chrome and the Edge browser are separate from this runtime.

## Visual C++ for native components

If Windows reports a missing Visual C++ runtime DLL, or a native AI/audio component cannot load because its runtime is missing, close ASTRA and run:

```text
vc_redist.x64.exe
```

Follow the Microsoft installer and restart Windows if it requests a restart. This is the full x64 Visual C++ v14 Redistributable installer. Microsoft may request administrator approval for the installation.

These installers remain Microsoft software under their applicable license terms. `sources.json` records the official download addresses, versions, sizes, SHA-256 hashes, and Microsoft Authenticode verification at packaging time. No runtime installer was executed to prepare this package.

Official documentation and fresh downloads:

- [WebView2 Runtime download](https://developer.microsoft.com/en-us/microsoft-edge/webview2/)
- [WebView2 deployment documentation](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution)
- [Visual C++ Redistributable downloads](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist)
