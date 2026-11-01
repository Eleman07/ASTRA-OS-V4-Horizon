# Online providers for ASTRA

Reviewed against official documentation on 2026-10-31. Free access is not unlimited access. No provider below guarantees unlimited powerful inference, continuous availability or error-free scientific results.

## Current integration: OpenRouter

ASTRA supports the saved Windows-encrypted OpenRouter credential, live zero-price catalog checks, reviewed model origins, explicit specialist assignments, complete-output continuation and durable provider waits. NVIDIA Nemotron 3 Ultra is the preferred text route. Changing `:free` models does not reset the account's shared free-request pool.

The quota guard checks `GET /api/v1/key` before generation. If its daily counter reports no remaining requests, no completion request is sent. Team jobs preserve completed checkpoints and retry after the UTC daily reset, respecting a supplied server retry time. This removes unnecessary failed attempts and a manual Resume requirement; it does not remove provider limits. The live complex CubeSat acceptance remains UNVERIFIED until the model actually generates the project and its files pass executed checks.

[OpenRouter limits](https://openrouter.ai/docs/api_reference/limits), [free-model terms](https://openrouter.ai/collections/free-models/).

## Alternatives to evaluate

These are proposals, not installed or successfully benchmarked ASTRA adapters. Each needs its own account and API credential. Do not paste credentials into chat or put them in the vault. Keep the accounts on their free access plans and disable any paid fallback.

| Provider | Relevant model/path | Free access and material constraint |
| --- | --- | --- |
| NVIDIA NIM hosted API | NVIDIA Nemotron 3 Ultra, also available directly from NVIDIA | Free Developer Program trial access; model/account rate limits and trial terms apply. Direct access uses a separate credential and quota system from OpenRouter. No unlimited production entitlement is established. |
| Google Gemini Developer API / AI Studio | Gemini 3.8 Flash for agentic and multimodal workflows | The published pricing lists free input/output for the free tier. Project/model request and token quotas still apply; check the actual account's AI Studio limits. Flagship Gemini 4 Argon is not on the free tier. |
| GroqCloud | `openai/gpt-oss-120b` for reasoning/code | Published free-plan baseline: 30 requests/minute, 1,000/day, 8,000 tokens/minute and 200,000/day; the organization console is authoritative. Long ASTRA prompts may exceed the token rate allowance and need explicit stages. |
| Mistral Studio | Mistral-developed models available to the account | Free mode includes monthly usage within the account's Limits page. Model availability, tokens/minute, requests/second and monthly allowances must be checked before adopting it. |

Sources: [NVIDIA Developer access](https://docs.api.nvidia.com/nim/docs/product), [NVIDIA Ultra model card and trial terms](https://build.nvidia.com/nvidia/nemotron-3-ultra-550b-a55b/modelcard), [NVIDIA trial rate limits](https://forums.developer.nvidia.com/t/request-more-4-000-credits-option-on-build-nvidia-com/344567/2), [Gemini pricing](https://ai.google.dev/gemini-api/docs/pricing), [Gemini quotas](https://ai.google.dev/gemini-api/docs/rate-limits), [Groq free-plan limits](https://console.groq.com/docs/rate-limits), [Mistral usage and limits](https://docs.mistral.ai/admin/billing-usage/usage-limits).

Cerebras is not a permanently free fallback: its current documentation describes a $5, 30-day trial that requires a verified payment method, then paid usage. [Cerebras trial terms](https://inference-docs.cerebras.ai/support/rate-limits).

For ASTRA's stated goal, direct NVIDIA access is the first alternative to test because it offers the same requested Ultra model. Gemini Flash and Groq GPT-OSS are separate options to benchmark; equal quality is not assumed. A future multi-provider scheduler can use legitimately separate free allowances and persist pending work when all are exhausted. Combining finite allowances does not make them unlimited.
