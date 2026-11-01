# ASTRA 4.3 online edition: third-party components

ASTRA is MIT licensed. Bundled libraries and assets retain their own licenses. This package includes no local text, embedding or vision language-model weights. It includes **OpenAI Whisper small and tiny speech checkpoints**, used on CPU in Codex mode. The MIT notices for Whisper, whisper.cpp, Whisper.net, NAudio and PdfPig are in `Ai/components`; the pinned speech-model manifest is beside them.

.NET and Windows Desktop Runtime notices are in `licenses/dotnet-runtime`. NuGet identities, declared licenses and upstream sources are in `licenses/dependencies.json` and `licenses/nuget`. Three.js is MIT licensed; Draco is Apache-2.0. Graphics credits and individual NASA/Khronos/educational Ariane asset notices are in `Graphics3D/web/models`. These assets imply no agency endorsement. NSIS installer licensing and compiler provenance are in `licenses/installer`.

Unmodified Microsoft WebView2 and Visual C++ offline installers are included in `prerequisites`; Microsoft terms apply. Their provenance, hashes and signature checks are in `prerequisites/sources.json`. Setup does not execute them automatically.

The official Codex CLI is installed and authenticated separately by the user. It is not redistributed with ASTRA. Codex, ChatGPT, OpenRouter, inference providers, Obsidian, Windows voices/OCR and optional third-party apps have their own terms, availability and usage limits. ASTRA is an independent project and is not endorsed by OpenAI or those providers.
