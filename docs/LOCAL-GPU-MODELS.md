# Choose a local GPU model for ASTRA

Verified against primary publisher cards and file pointers on **31 October 2026**. The user chooses the model. No model in this guide is claimed to equal a hosted frontier model, and local CubeSat generation remains **UNVERIFIED until the selected model actually generates a project that passes independent checks**.

Local inference has no hosted API token bill or provider daily quota. It still consumes your hardware, electricity, disk space and time, and has finite model/context and memory capacity. **ASTRA uses the GPU for local model inference.** Keep one model resident and run specialist agents sequentially when VRAM is constrained; several named agents do not require several simultaneously loaded models.

## Three practical choices for a 16 GB GPU

The reference laptop GPU has 16,303 MiB total and approximately 2,526 MiB already used by Windows/apps at inspection: about **13.45 GiB free**. This is a snapshot, not a guaranteed budget. File sizes below are exact; inference footprints are planning estimates rather than measurements on this machine.

| Choice | Developer / license | Exact selected weights | Published model context | Why choose it / tradeoff |
|---|---|---:|---:|---|
| **Ministral 3 14B Reasoning, Q4_K_M** | Mistral AI, France; Apache-2.0 | 8,239,591,488 bytes = **7.674 GiB** | 256k | First candidate to evaluate for STEM, code and multi-step work. More room for KV/cache than gpt-oss-20b. Estimate roughly **10–13 GiB** for one short/moderate-context text session, depending on runtime/cache settings. Its actual project accuracy must be tested. [Official model card](https://huggingface.co/mistralai/Ministral-3-14B-Reasoning-2512-GGUF). |
| **gpt-oss-20b, native MXFP4** | OpenAI, US; Apache-2.0 | 12,109,566,624 bytes = **11.278 GiB** | 131,072 | OpenAI reasoning/agent candidate, with low/medium/high reasoning settings. This leaves only about 2.18 GiB of the observed free GPU memory before cache and work buffers: start with a modest live context and verify actual offload. OpenAI documents approximately 16 GB for native MXFP4; a busy 16 GB laptop GPU is a tighter case. [Official specification](https://developers.openai.com/api/docs/models/gpt-oss-20b), [official memory guidance](https://developers.openai.com/cookbook/articles/gpt-oss/run-transformers). |
| **Ministral 3 8B Reasoning, Q4_K_M** | Mistral AI, France; Apache-2.0 | 5,198,910,368 bytes = **4.842 GiB** | 256k | More headroom for long inputs, other applications and experimentation. Estimate roughly **6.5–9 GiB** for one short/moderate-context text session. A smaller model can make more reasoning errors; test the real task before relying on it. [Official GGUF card](https://huggingface.co/mistralai/Ministral-3-8B-Reasoning-2512-GGUF), [publisher model card](https://huggingface.co/mistralai/Ministral-3-8B-Reasoning-2512). |

These are selection candidates, not a measured ranking. **Reasoning and Instruct are different checkpoints.** An already installed `Ministral-3-8B-Instruct` file is not the Reasoning file; a filename change cannot turn one into the other. The Instruct checkpoint remains a useful alternative for faster ordinary chat/content workflows. [Official Instruct checkpoint](https://huggingface.co/mistralai/Ministral-3-8B-Instruct-2512-GGUF).

Weight-file size is not total VRAM usage. Add KV/recurrent state, activations, compute buffers, context length, parallel requests, optional vision projectors and desktop allocations. The ranges above assume one text inference session and are deliberately not promises of a particular token count or throughput.

## Exact downloads and integrity

The table pins the file upload revision, not the moving `main` branch. The OpenAI GGUF is an upstream **ggml-org conversion of OpenAI weights**, rather than a file published by OpenAI's account. The Mistral files are published by Mistral itself. Exact metadata is also available in [local-gpu-model-pins.json](local-gpu-model-pins.json).

| File | Immutable download / SHA-256 |
|---|---|
| `Ministral-3-14B-Reasoning-2512-Q4_K_M.gguf` | [Mistral pinned download](https://huggingface.co/mistralai/Ministral-3-14B-Reasoning-2512-GGUF/resolve/650da2e5ed9a23b6d760d998bd8232ba6787b1bb/Ministral-3-14B-Reasoning-2512-Q4_K_M.gguf) · `fe08ca2158cd7438211ec6a4e5256d31bc980f016e3f5b635fe91fe6848d461c` |
| `gpt-oss-20b-MXFP4.gguf` | [ggml-org pinned conversion](https://huggingface.co/ggml-org/gpt-oss-20b-GGUF/resolve/b97cbb20d1995efd41dce8c4dd1ddf86e8db375b/gpt-oss-20b-MXFP4.gguf) · `27cd6c432c7672cb812a92f611cf3ba7bbc35928262bb1e1253ff4ee6ae35901` |
| `Ministral-3-8B-Reasoning-2512-Q4_K_M.gguf` | [Mistral pinned download](https://huggingface.co/mistralai/Ministral-3-8B-Reasoning-2512-GGUF/resolve/ab77f02a3bd3d21d31448e6a63a92e9c1e481673/Ministral-3-8B-Reasoning-2512-Q4_K_M.gguf) · `894aa3645ef8708a81dbe201c26105ce37c4c741252c89c5a78f81b49ac438c6` |

Download only the model you choose into your own model folder, then compare its exact byte count and SHA-256 before selecting it in ASTRA. In PowerShell:

    Get-Item -LiteralPath 'C:\Models\your-selected-model.gguf' | Select-Object Length
    Get-FileHash -LiteralPath 'C:\Models\your-selected-model.gguf' -Algorithm SHA256

The full weights are separate large downloads and do not belong in the application Git repository or the personal vault. Keep the corresponding model license and attribution with the downloaded weights.

## Other verified non-Chinese developer candidates

- **Microsoft Phi-4-reasoning-plus 14B**: Microsoft Research, US; MIT; text only, published 32k context. It is a distinct reasoning checkpoint, not the existing `microsoft/phi-4-gguf` model. Microsoft reports longer average generations and primarily English/math-oriented evaluation. BF16 weights alone are about 28 GB; a 4-bit conversion is needed for this GPU, with roughly 8–10 GiB of weights as a planning range before cache. No Microsoft-owned GGUF for this specific Plus checkpoint was verified in this review, so do not label a community conversion as Microsoft-published. [Official card](https://huggingface.co/microsoft/Phi-4-reasoning-plus), [different official Phi-4 GGUF](https://huggingface.co/microsoft/phi-4-gguf).
- **NVIDIA Nemotron Nano 9B v2**: NVIDIA, US; NVIDIA Open Model License, text, up to 128k context. The publisher describes a from-scratch Nemotron-Hybrid base; check hybrid recurrent-state and parser support in the selected runtime. A 4-bit conversion should be much smaller than BF16, but no exact NVIDIA-owned 9B GGUF file was verified here. This is an alternative to test, not a pinned built-in download in this guide. [Official card](https://huggingface.co/nvidia/NVIDIA-Nemotron-Nano-9B-v2).
- **NVIDIA Nemotron 3.5 Bolt E4B 4GB**: new NVIDIA post-training of Google's Gemma-4-E4B, both US developers; OpenMDW-1.1, text only, 64k context. Intended for responsive local assistants/tool use. Its actual official Q4_K_M file is **6,180,446,208 bytes / 5.756 GiB**, despite the product name. It is an experimental choice until runtime compatibility and the requested STEM project are tested; the official example uses llama.cpp CUDA13 build b11459. [Official card and recipe](https://huggingface.co/nvidia/NVIDIA-Nemotron-3.5-Bolt-E4B-4GB), [pinned file](https://huggingface.co/nvidia/NVIDIA-Nemotron-3.5-Bolt-E4B-4GB/resolve/1db54d5b12533ff1bcaa8e42cf4c667988a1f1be/Nemotron-3.5-Bolt-E4B-Q4_K_M.gguf).

The origin preference concerns verified model developers and base-model lineage; it is not inferred from a filename or a model's spoken language. It does **not** establish that every training example or synthetic teacher is from the same country. NVIDIA's cards explicitly list synthetic data from Qwen, DeepSeek, GLM and other teachers. If excluding those teacher sources is also required, do not treat the NVIDIA alternatives as verified against that stricter requirement. [NVIDIA training disclosure](https://huggingface.co/nvidia/NVIDIA-Nemotron-3.5-Bolt-E4B-4GB#training-testing-and-evaluation-datasets).

Optional cloud routing uses an explicit reviewed model-identity list with publisher/base evidence. An unfamiliar release does not inherit approval from its vendor prefix or free price. Automatic fallback can select another already reviewed compatible free identity; a newly published identity needs its origin metadata reviewed first. The generic `openrouter/free` broker route is excluded because it could select an unreviewed model. This policy is separate from the user's selected local, hash-verified GGUF.

## Larger models and long inputs

**Nemotron 3 Nano 30B-A3B** has about 30B total parameters; the roughly 3B active parameters reduce compute per token, not the storage of all expert weights. Even an ideal 4-bit 30B payload is about 14 GiB before mixed-precision overhead and cache, already above the observed 13.45 GiB free VRAM. Its advertised 1M context is not a 16 GB hardware promise. It requires more GPU memory, aggressive quantization or partial CPU/RAM offload; benchmark the speed and quality tradeoff. [Official architecture/context card](https://huggingface.co/nvidia/NVIDIA-Nemotron-3-Nano-30B-A3B-BF16).

**gpt-oss-120b** is not a full-GPU 16 GB choice. OpenAI's native reference targets a large-memory GPU; CPU/RAM offload is a separate, much slower configuration and needs substantial system RAM. The 20B checkpoint is the practical OpenAI candidate here. [OpenAI hardware guidance](https://developers.openai.com/cookbook/articles/gpt-oss/run-transformers).

Published context is the architectural ceiling, not a guarantee that its entire cache fits this laptop. Input, output and reasoning compete for context and memory. To process larger work, preserve the entire source/output on disk and use retrieval, explicit chunking and continued generation with visible provenance. Do not silently drop text or claim that an unlimited file history fits in one model call. Check actual GPU-layer residency and peak VRAM in runtime logs and `nvidia-smi` before increasing context or concurrency.

gpt-oss requires the Harmony chat format. Use the checkpoint's chat template and a runtime that supports the architecture; merely loading its tensor file does not prove correct reasoning or tool behavior. [OpenAI model card](https://huggingface.co/openai/gpt-oss-20b).

## Judge accuracy with the actual generated project

Have the selected model generate the CubeSat software and all requested artifacts from the objective and real source records. Record its model/hash, prompt, unmodified response and extracted file hashes. Review code before execution, then run real tests and independently recompute speed, period, energy drift and step-halving convergence from its own output CSV. A fixed QA reference passing tests is not the model passing. See [the eight-deliverable acceptance benchmark](benchmarks/CUBESAT-400KM-ACCEPTANCE.md).

Local text generation does not automatically make a checkpoint audio/vision-capable. Native Windows OCR/voices and separately configured transcription/vision models are separate routes; Mistral vision additionally needs its matching projector and runtime support. Keep the active modality and any online route visible. [Mistral multimodal architecture](https://huggingface.co/mistralai/Ministral-3-8B-Reasoning-2512).
