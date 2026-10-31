# Third-party components and credits

ASTRA's own code is provided under the MIT license in `LICENSE`. Third-party software, models, media, and Microsoft installers retain their respective licenses and notices. The ASTRA MIT license does not replace those terms.

Paths below refer to the extracted binary package.

| Component | Credits and license location |
| --- | --- |
| .NET / Windows Desktop Runtime | Microsoft and .NET contributors. The self-contained runtime is distributed beside the app. Upstream: [dotnet/runtime](https://github.com/dotnet/runtime) and [dotnet/windowsdesktop](https://github.com/dotnet/windowsdesktop). Additional runtime license and third-party notices are included under `licenses/dotnet-runtime`. |
| Windows App SDK / WinUI | Microsoft and contributors. [Windows App SDK](https://github.com/microsoft/WindowsAppSDK) and [WinUI](https://github.com/microsoft/microsoft-ui-xaml). Component license and notices are retained under `licenses/nuget`. |
| WebView2 and Visual C++ installers | Unmodified Microsoft runtime installers under `prerequisites`, subject to Microsoft terms. `prerequisites/sources.json` records their provenance and signature verification. |
| llama.cpp Vulkan runtime b12020 | MIT. `Ai/gpu/LICENSE.txt`; official archive and per-file hashes in `Ai/gpu/manifest.json`. [Upstream](https://github.com/ggml-org/llama.cpp/releases/tag/b12020). |
| SmolVLM-256M-Instruct vision model and projector | Hugging Face / ggml-org, Apache-2.0. `Ai/vision/LICENSE.txt` and pinned provenance/hashes in `Ai/vision/manifest.json`. [Model](https://huggingface.co/ggml-org/SmolVLM-256M-Instruct-GGUF). |
| llama.cpp CPU runtime | MIT. `Ai/components/LICENSE-llama.cpp.txt`. |
| LLamaSharp and CPU backend | SciSharp and contributors, MIT. Version 0.30.0; repository-signed NuGet packages, hashes in `Ai/embedded-runtime.json`, licenses under `licenses/nuget`. [Upstream](https://github.com/SciSharp/LLamaSharp). |
| Whisper model and whisper.cpp | OpenAI / whisper.cpp contributors, MIT. `Ai/components/LICENSE-Whisper-model.txt` and `LICENSE-whisper.cpp.txt`. |
| Whisper.net | MIT. `Ai/components/LICENSE-whisper.net.txt`. |
| NAudio | MIT. `Ai/components/LICENSE-NAudio.txt`. |
| PdfPig | Apache-2.0, with upstream notices. `Ai/components/LICENSE-PdfPig.txt`. |
| Three.js and model loaders | MIT. `Graphics3D/web/vendor/three/LICENSE`. |
| Draco decoder | Google and contributors, Apache-2.0. See `licenses/draco` for the upstream license and attribution. |
| NASA models | NASA 3D Resources. Individual source URLs and usage notices appear in `Graphics3D/web/models/CREDITS.md` and its `licenses` folder. NASA material does not imply NASA endorsement. |
| Khronos glTF Sample Assets | Individual asset creators, including Microsoft, Guido Odendahl, and Eric Chadwick. Asset-specific credits and licenses appear in `Graphics3D/web/models/CREDITS.md` and its `licenses` folder. |
| Ariane 64 reconstruction | Original ASTRA educational geometry based on public ESA/ArianeGroup dimensions. `Graphics3D/web/models/licenses/ariane64.md`. It is not official ESA CAD and does not imply ESA endorsement. |

The package includes available upstream license/notice files for the restored NuGet dependencies under `licenses/nuget`, along with a dependency inventory in `licenses/dependencies.json`. Upstream projects may contain additional component notices referenced by these files.

Windows OCR, voices, codecs, WSL, Chrome, and any applications discovered on your PC are separate components governed by their respective terms. Windows installation media is not included.

| Nomic Embed Text v1.5 Q8 | Nomic AI, Apache-2.0; `Ai/embeddings/LICENSE.txt`. Pinned official GGUF revision and SHA-256 are in `LocalModelCatalog.cs`. |
| Optional Ministral 3 8B weights | Mistral AI, Apache-2.0; downloaded separately from the revision-pinned official repository. |
| Optional Phi-4 14B weights | Microsoft, MIT; downloaded separately from the revision-pinned official GGUF repository. |
| Optional gpt-oss-20b weights | OpenAI, Apache-2.0; revision-pinned ggml-org conversion. This is an open model, not the ChatGPT hosted service. |

Optional OpenRouter free inference is a remote service governed by its provider terms and quotas. Model weights and runtime downloads do not transmit prompts.
