# Third-party components in the cloud distribution

ASTRA's own code is MIT licensed in `LICENSE`. Bundled third-party components retain their own licenses. No local language, embedding, vision or speech-model weights are included in this cloud distribution.

- .NET and Windows Desktop Runtime: Microsoft and .NET contributors. Runtime license and third-party notices are under `licenses/dotnet-runtime`.
- Windows App SDK, WinUI, WebView2 SDK, PdfPig, NAudio, LLamaSharp, Whisper.net and other restored dependencies: their available license and notice files are under `licenses/nuget`; `licenses/dependencies.json` records exact package identities, license declarations and upstream links. This inventory can also include build-only dependencies. Compatibility libraries do not enable local model inference in the cloud workflow.
- Three.js and loaders: MIT, `Graphics3D/web/vendor/three/LICENSE`. Draco decoder: Google and contributors, Apache-2.0, `licenses/draco`.
- NASA and Khronos glTF assets: individual source URLs, credits and usage notices are in `Graphics3D/web/models/CREDITS.md` and the adjacent `licenses` folder. These assets imply no NASA endorsement.
- Ariane reconstruction: ASTRA educational geometry, `Graphics3D/web/models/licenses/ariane64.md`; it is not official ESA CAD and implies no ESA endorsement.
- Optional, unmodified Microsoft WebView2 and Visual C++ runtime installers: Microsoft terms apply. `prerequisites/sources.json` records provenance, hashes and signature verification. ASTRA setup does not execute these installers automatically.
- The Setup download uses NSIS. Its upstream license and compiler provenance are under `licenses/installer` in the installed package.

OpenRouter and its inference providers are separate online services governed by their own terms, access rules and quotas. Windows OCR, installed voices, codecs and other applications are separate components governed by their respective terms.
