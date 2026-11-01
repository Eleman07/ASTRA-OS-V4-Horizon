# ASTRA Plugin SDK v3 – public, data-only by default

## Safe-by-default recipe example

Copy `sdk/declarative/org.astra.textkit/recipe.json` to the same path in your build (`dist/AstraOS/recipes/org.astra.textkit/recipe.json`) or to `%LOCALAPPDATA%\ASTRA\OS\plugins\org.astra.textkit\recipe.json`.

```json
{"schemaVersion":3,"id":"org.astra.textkit","name":"Text Kit","description":"A static recipe","operation":"text.prefix","prefix":"ASTRA · "}
```

Allowed `operation`: `text.upper`, `text.lower`, `text.trim`, `text.prefix`. Id follows `[a-z0-9.-]`, matches folder name. Maximum input 4,000 chars, prefix 120 chars, bounded output. V3 is deliberately not Turing-complete and does not execute binaries, access files, or contact servers.

## Native extension protocol v2 (legacy, NOT SANDBOXED)

`sdk/Astra.SamplePlugin` shows the older JSON-over-stdio sample. Its executable is verified with a SHA-256 pin and per-run confirmation, but it runs with current-user privileges. Off by default in the V2 UI. Use **only** for trusted developer testing; do not present it as a secure public plugin marketplace or run untrusted code. An OS-enforced sandbox/low-integrity/AppContainer permission model is a future architectural requirement before a public native plugin marketplace.
