# ASTRA OS V4 — Plugin JSON / SmokeTests CI fix

## Issue
A smoke test was building a Windows path traversal case through `string.Replace` with a single backslash. In JSON, `\e` is an invalid escape, therefore `JsonSerializer.Deserialize<PluginManifest>` raised `JsonException` before the test reached the path validator. The smoke helper expected `InvalidDataException` and the GitHub Actions job failed.

## Fix
1. Build the malicious entryPoint with `System.Text.Json.JsonSerializer.Serialize(@"..\evil.exe")`. This keeps the JSON syntactically valid, so the test exercises the path validator.
2. `PluginProtocol.Validate` converts malformed JSON to `InvalidDataException` (keeping the original exception as InnerException), rejects empty/large input, and checks potentially-null manifest properties before applying regular expressions.
3. Regression cases cover Windows and POSIX path traversal, malformed JSON, invalid JSON escapes, null entryPoint, null plugin ID, and mismatched plugin directory.

The fix does **not** enable native plugins or weaken the existing allowlist. It changes only `src/Astra.Core/PluginProtocol.cs` and `tests/Astra.Core.SmokeTests/Program.cs`.

## Verified in this environment
- 25 Python offline regression tests: pass.
- Source/XAML/JSON structural checks: pass.
- JavaScript `node --check` (preview and 3D): pass.
- C# string literal scanner: pass (including SmokeTests source).
- Patch application against preceding `ASTRA-OS-V4-RC1-CI-FIXED-SOURCE.zip`: pass.

.NET 10 is unavailable in this Linux environment, so **the Windows/.NET Core smoke test is NOT yet verified**. GitHub Actions is the next validation step.

## Update an existing private GitHub repository
Download `ASTRA-OS-V4-FIX-PLUGIN-JSON.patch` to `~/Downloads`, then from the Git repository root:

```bash
git apply --check ~/Downloads/ASTRA-OS-V4-FIX-PLUGIN-JSON.patch
git apply ~/Downloads/ASTRA-OS-V4-FIX-PLUGIN-JSON.patch
git add src/Astra.Core/PluginProtocol.cs tests/Astra.Core.SmokeTests/Program.cs
git commit -m "Fix plugin JSON handling and path traversal smoke test"
git push
```

On GitHub: Actions → Build and validate ASTRA OS V4 → Run workflow. Check the `Run Core smoke tests` step, and then the WinUI publishing step. If your branch has diverged from the baseline the patch may fail; compare and apply the two changed files manually instead of force applying.

## Scope
This is a fix to the CI failure, not a certification of ASTRA OS V4 Stable, nor a complete application security audit.
