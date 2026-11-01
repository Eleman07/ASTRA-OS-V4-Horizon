# Third-party components in the local GPU distribution

ASTRA's own code is MIT licensed in `LICENSE`. Bundled third-party components retain their own licenses.

- .NET and Windows Desktop Runtime: Microsoft and .NET contributors; licenses and notices are under `licenses/dotnet-runtime`.
- Windows App SDK, WinUI, WebView2 SDK, PdfPig, NAudio, LLamaSharp, Whisper.net and other restored dependencies: available notices are under `licenses/nuget`; `licenses/dependencies.json` records exact package identities and upstream license declarations.
- llama.cpp b12020 Vulkan: ggml-org and contributors, MIT; the full license is under `licenses/local-gpu/llama.cpp/LICENSE.txt`. `Ai/gpu/manifest.json` records the official archive and exact binary hashes. The archive also includes the LLVM OpenMP notice.
- SmolVLM 256M and its vision projector: Hugging Face/ggml-org, Apache-2.0. `Ai/vision/manifest.json` records pinned repository revision `b9e4379657e1450d04d02eec8e345667265b0a00`, source URLs and hashes.
- Whisper small and tiny: OpenAI and whisper.cpp contributors, MIT. `Ai/components/manifest.json` records the speech-weight hashes and upstream license URLs. Small is pinned to revision `5359861c739e955e79d9a303bcbc70fb988958b1` of `ggerganov/whisper.cpp`; its SHA256 is `1be3a9b2063867b937e64e2ec7483364a79917e157fa98c5d94b5c1fffea987b`. Adjacent `LICENSE-Whisper-model.txt`, `LICENSE-whisper.cpp.txt`, `LICENSE-whisper.net.txt` and `LICENSE-NAudio.txt` retain the applicable terms.
- Main text-model weights are not bundled. User-selected GPT-OSS 20B, Phi-4 and Ministral GGUF sources, pinned revisions, hashes and license identifiers are recorded in `scripts/local-models.json`. Obtain and use a selected model under its own terms.
- Three.js and loaders: MIT, `Graphics3D/web/vendor/three/LICENSE`; Draco decoder: Google and contributors, Apache-2.0, `licenses/draco`.
- NASA and Khronos glTF assets: individual credits, sources and usage terms are in `Graphics3D/web/models/CREDITS.md` and its adjacent `licenses` folder. Educational reconstructions imply no NASA or ESA endorsement.
- Optional, unmodified Microsoft WebView2 and Visual C++ runtime installers: Microsoft terms apply. `prerequisites/sources.json` records provenance, hashes and signature verification. Setup does not execute these installers automatically.
- NSIS Setup: upstream license and compiler provenance are under `licenses/installer` in the installed package.

Windows OCR, installed voices, codecs and other applications are separate components governed by their respective terms. Optional online providers are separate services with their own access rules and quotas.
