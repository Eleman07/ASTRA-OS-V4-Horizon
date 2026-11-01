# ASTRA OS V2 RC1 — Threat model and privacy guarantees

**Scope:** ASTRA is an unprivileged Windows 11 application, not an OS security boundary. No claims of perfect security, absolute privacy or audited sandboxing.

| Attack surface | Implemented mitigation | Residual threat |
|---|---|---|
| Local note store | DPAPI CurrentUser encryption, length/count limits, atomic temp-file swap | Malware with user's privileges can decrypt data; Windows profile compromise or DPAPI key loss can cause disclosure/loss |
| Local inference | `127.0.0.1`, temporary 256-bit bearer token file, no proxy, no cloud fallback, process started on demand and stopped on exit | Local account compromise, untrusted model/runtime executable, possible localhost race; plaintext in process memory |
| AI prompt injection | Retrieved passages are labeled untrusted; system prompt forbids treating them as commands | LLMs may still obey malicious note content; **no tool execution** is supplied to the LLM |
| Plugin installation | User-provided manifests, protocol v2 strict validation, SHA-256, reparse point checks, per-run warning, limited input/output/time | SHA can be edited along with plugin manifest; plugin has user's full filesystem/network permissions; no actual sandbox |
| WebView2 3D | Offline assets with restrictive CSP, `AreHostObjectsAllowed=false`, fixed navigation allowlist, no ASTRA network code | WebView2/browser vulnerability, external resource/network attempts via unforeseen code, GPU driver bugs |
| WinGet | Curated package IDs + `ArgumentList`, no silent package agreement flags, user confirmation | Remote software supply chain, vendors' package content and prompts |
| Image Builder | Default dry-run, user media, path checks, no auto download, no overwrite, ADK only | Malicious installation media, insufficient disk, user could distribute Windows media unlawfully |
| Security Center | Read-only Windows Defender/firewall inspection | Non-Defender AV, corporate policy, false/partial status; not penetration testing |

## Hard rules for public releases

- No privileged background service, shell replacement, injected OS DLL, registry tweaks to disable security, activation bypass or unsigned drivers.
- No automatic execution of third-party plugins, package installers or AI model files without consent.
- User data stays local to ASTRA unless users deliberately invoke external WinGet packages or third-party runtimes, which have their own behavior.
- Verify WinUI/WebView2/.NET dependencies, produce SBOM, apply vulnerability fixes, sign Windows releases, check installer hash and scan with Defender.
- Do not publish the ISO Builder output as a public Windows distribution; publish the source code and setup app only.

## Testing needed before the word “secure” can be used in marketing

Threat modeling review, source audit, static code scanning, fuzz of plugin JSON and note store, malicious plugin tests inside disposable VM, dependency CVE assessment, authenticated localhost hijack test, WebView2 network trace, privilege audit, and Windows Defender false-positive assessment.
