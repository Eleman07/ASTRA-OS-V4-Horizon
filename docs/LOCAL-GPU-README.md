# ASTRA 4.2.0: choose your local GPU model

ASTRA can run agents against a GGUF language model on your own GPU, with no inference API subscription or provider request quota. Your GPU memory, RAM, model context, execution time and electricity remain real constraints. A local model is not guaranteed to match a frontier hosted model on every task.

## What the download includes

- The verified official llama.cpp b12020 Vulkan runtime under `Ai/gpu`.
- Whisper small for precision speech transcription, Whisper tiny as an explicit fast option, plus SmolVLM 256M and its projector for the compact local vision path. These auxiliary models are pinned and hash checked.
- The assistant, agents, skill management, knowledge graph and generic Obsidian workspace kit. Your personal vault and credentials are not included.
- Optional Microsoft prerequisites under `prerequisites`. Setup does not run them automatically.

The main text language model is **your choice and a separate download**. A large language-model GGUF is not included in the ZIP or Setup. The curated choices below are from OpenAI, Microsoft and Mistral; ASTRA does not require a Chinese model.

Speech transcription runs on the CPU separately from GPU text inference. Transcripts remain editable: review them before using technical values or names in a task.

## Choose and download a model

Use ASTRA's model selector to choose a curated model and download it, or browse to an existing GGUF. The complete model catalogue and pinned sources are in [LOCAL-GPU-MODELS.md](LOCAL-GPU-MODELS.md) and `local-gpu-model-pins.json`. The table and commands below are additional examples.

Install Python 3 if you want to use the optional command-line downloader, or download the GGUF manually from the pinned source in `scripts/local-models.json`. Python is needed for that optional downloader, not for ordinary ASTRA chat.

From the extracted or installed ASTRA folder, run exactly one choice:

```powershell
python .\scripts\fetch-local-model.py gpt-oss
# Or:
python .\scripts\fetch-local-model.py phi4
# Or:
python .\scripts\fetch-local-model.py ministral
# Or:
python .\scripts\fetch-local-model.py ministral-reasoning-14
# Or:
python .\scripts\fetch-local-model.py ministral-reasoning-8
```

| Choice | Separate GGUF download | Native context declared in this GGUF | License |
|---|---:|---:|---|
| OpenAI GPT-OSS 20B MXFP4 | 12.11 GB | 131,072 tokens | Apache-2.0 |
| Microsoft Phi-4 Q4_K | 9.05 GB | 16,384 tokens | MIT |
| Mistral Ministral 3 8B Q4_K_M | 5.20 GB | 262,144 tokens | Apache-2.0 |
| Mistral Ministral 3 14B Reasoning Q4_K_M | 8.24 GB | 262,144 tokens | Apache-2.0 |
| Mistral Ministral 3 8B Reasoning Q4_K_M | 5.20 GB | 262,144 tokens | Apache-2.0 |

These context figures come from the actual GGUF metadata; they do not promise that the full native context fits your hardware. The downloader resumes chunks and verifies the complete file's pinned SHA256 before using it. Keep the GGUF in a location you control; ASTRA's file picker also accepts a model outside the installation folder.

## Select it in ASTRA

1. Open Assistant settings and select the **Local GPU** provider.
2. Choose the downloaded `.gguf` file. For the downloader above, it is under `downloads/models` in the ASTRA folder. Select the actual file; a similarly named path under AppData will not work unless you placed it there.
3. Use the bundled runtime at `Ai/gpu/llama-server.exe` when ASTRA asks for an executable.
4. Select an explicit Obsidian vault and create a task. Each output's verification state and citations matter more than a confident model response.

Local inference needs no OpenRouter key. The optional online provider is a separate choice and retains its provider's quotas and terms. After model download, local text inference can work without internet; web research and external services still need a connection.

## GPU and long inputs/outputs

The reference machine is an RTX 5080 Laptop GPU with 16,303 MiB reported VRAM. Startup checks loaded GPT-OSS 20B with all 25 model layers offloaded to `Vulkan0` at both 32,768 and 65,536 tokens. Flash Attention, `q8_0` K/V caches, batch 256, microbatch 128 and one generation slot were used. Startup success is not a completed long-context accuracy benchmark; see `LOCAL-GPU-4.2.0-VERIFICATION.md` for the actual task results.

Run one generation server at a time. Close GPU-heavy games or rendering workloads before using a large model. A smaller model can leave more GPU memory for a longer context. ASTRA's GPU setting uses your GPU; some auxiliary operations or tensors can still execute on the CPU according to the runtime.

Input and generated output share the model's context window. `--no-context-shift` prevents silently discarding earlier context when the server runs out of space. Requests that cannot fit must report the problem or be broken into explicit, traceable stages. Model EOS, available context and hardware remain limits even when no provider quota exists.

For Obsidian Copilot or another OpenAI-compatible plugin, use the loopback endpoint you explicitly start/configure and its exact model alias. Keep the server bound to `127.0.0.1`; do not expose it to a network. ASTRA-managed endpoints use temporary authentication and lifecycle management, so another application should not assume its private port or key is stable.

## Rebuild and package

For source builds, use Python 3.12 or newer for the packaging tools. Restore/build the documented prerequisites and fetch the pinned GPU/vision components if they are not present. Then:

```powershell
.\scripts\build-local-gpu.ps1
python .\scripts\package-local-gpu-release.py
python .\scripts\build-cloud-installer.py --flavour local-gpu
python .\scripts\test-cloud-installer.py --flavour local-gpu
```

The build creates a new staging folder and preserves a previous Final as a separate backup. Packaging checks every file against `local-gpu-build-manifest.json`, allows only the four exact auxiliary model hashes, compares the GPU runtime with its pinned official archive, and rejects private vault/cache/credential files and unexplained additions. It does not launch models or execute prerequisite installers. The Setup is unsigned; the separate installer QA report records what was actually tested.
