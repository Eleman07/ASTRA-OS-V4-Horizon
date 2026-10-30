# ASTRA OS V4 Horizon — OpenRouter, Gaming and Creator update

Windows 11 x64 · Release 4.0.0.5 · October 30, 2026

## Assistant and Second Brain

- OpenRouter is the default assistant provider, with dynamic `openrouter/free` routing and a live picker for fixed free models.
- Model discovery filters to zero-cost text models; it does not filter for ZDR endpoints.
- ASTRA does not request ZDR or filter providers for it. Each inference caps prompt and completion prices at $0, including provider failovers. ASTRA never selects a paid model.
- The API key is stored encrypted for the current Windows account with DPAPI. First remote inference in each ASTRA session requires explicit content-sharing consent.
- Local OCR, audio transcription, and Second Brain retrieval remain available. The included LLamaSharp model is an optional offline provider; Ollama and a localhost model server are not required.
- The README and `OPENROUTER-AI-GUIDE.md` describe setup, consent, the absence of ZDR routing, provider data policies, free-tier quotas, offline mode, and recovery.

## Gaming profile

- Label a performance run by game, repeatable scene, resolution, and graphics preset.
- Compare against the last matching run only; retain up to 50 same-account DPAPI-encrypted session summaries.
- Show CPU, GPU-engine, and RAM measurements with scoped warnings, a live Session HUD, and CSV export. The app does not claim to measure FPS, frame time, temperature, latency, or network quality.

## Creator profile

- Organize local creative assets into a timed storyboard and CSS palette; inspect supported file metadata and image colors.
- Record alt text and source/licensing credits for assets.
- Run a delivery preflight for target aspect ratio, suggested resolution, storyboard notes, SRT presence, audio notes, and missing credits/accessibility descriptions.
- Adjust SRT offset and timing scale, then export a new subtitle file. Original media remains untouched.
- Preflight uses metadata and user notes. It does not inspect video frames or certify rights, accessibility, or platform compliance.

## Practical limits

OpenRouter's free service is subject to changing account/provider quotas and model availability; it is not unlimited. ASTRA does not request ZDR or filter providers by it. Cloud prompts and selected context are sent to OpenRouter and the serving provider, which may retain or use them under its own policies. Use offline AI for content that must remain on the PC.

The ASTRA executable remains unsigned. Full-screen Gaming HUD visibility depends on the game and Windows compositor. Creator checks are advisory metadata checks. All AI output and STEM simulation results need review.
