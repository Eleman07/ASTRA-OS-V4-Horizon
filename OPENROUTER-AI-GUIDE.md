# ASTRA Assistant and OpenRouter — setup guide

This guide covers the default cloud AI setup in ASTRA OS V4 Horizon. The offline model remains available as an alternative. This guide was updated on October 30, 2026; model lists, provider policies, and free-tier limits can change.

## What you need

- ASTRA OS V4 Horizon on Windows 11 x64.
- An OpenRouter account and an API key. ASTRA does not ask for your account password.
- An internet connection for OpenRouter. The offline model does not require OpenRouter or internet access.

OpenRouter lists its currently available free models at [openrouter.ai/collections/free-models](https://openrouter.ai/collections/free-models). A free model can still be rate-limited or temporarily unavailable.

## First-time setup

1. Sign in or create an account at [OpenRouter](https://openrouter.ai/).
2. Open the account's **Keys** page, create a key, and copy it. Keep the key private; do not put it in a chat, screenshot, source file, or public issue.
3. Start ASTRA and open **Second Brain / AI settings**.
4. Paste the key into **OpenRouter API key (encrypted for this Windows account)**.
5. Select **Save key and use OpenRouter**. ASTRA saves it encrypted with Windows DPAPI under `%LOCALAPPDATA%\ASTRA\OS\ai\openrouter-key.dpapi`. The plaintext is not saved in preferences or written to the app's diagnostic log.
6. Leave **Automatic free model · zero cost only** selected to use OpenRouter's dynamic `openrouter/free` router. Or select **Refresh free models** and choose a listed fixed model ending in `:free`.
7. Select **Start OpenRouter**, or open a conversation and send a request.
8. Before the first remote inference in each ASTRA session, read the consent dialog. Choose **Allow for this ASTRA session** only if you agree to send that request's content. Cancel to keep the content on your PC; you can still use offline note and PDF search.

Saving the key, refreshing the public model list, and starting the OpenRouter provider do not send a prompt or document. ASTRA sends inference content only after the consent prompt is accepted.

## What can be sent

For an accepted request, ASTRA can send the prompt, recent conversation turns, and the content used for that task. Depending on what you ask it to do, that may include text extracted from selected files, relevant Second Brain notes or PDF passages, selected project/workspace context, or web research sources. Review and deselect sensitive material before asking.

Image OCR and speech transcription run locally first. If you then ask OpenRouter to analyze their extracted text or transcript, that text is part of the request and leaves the PC after consent. ASTRA does not send microphone audio or image files directly through this text-model integration. ASTRA's assistant has no direct access to your filesystem or open apps; it receives only context provided by the application for the requested task.

## Privacy and cost behavior

ASTRA attaches these routing requirements to every OpenRouter inference. It does not request Zero Data Retention (ZDR) or filter models/providers by ZDR eligibility:

```json
{
  "provider": {
    "allow_fallbacks": true,
    "max_price": { "prompt": 0, "completion": 0 }
  }
}
```

ASTRA does **not** request Zero Data Retention (ZDR), deny provider data collection, or filter providers by ZDR eligibility. A serving provider may retain prompts or use them under its own policy. OpenRouter may fail over to another provider. A $0 prompt/completion price ceiling remains in force for every attempt, so ASTRA does not route to paid endpoints. Fixed model choices are filtered by zero prompt/completion price and a `:free` model ID; automatic routing chooses dynamically from free models.

**Cloud inference is not private/local processing.** The prompt and selected context are transmitted to OpenRouter and the serving provider. ASTRA does not require ZDR, so provider retention and training policies may apply. Review the model/provider policy before sending sensitive material. Use the included offline model for content that must stay on this PC.

ASTRA submits no prompt automatically on launch or during key setup. It does not enable OpenRouter's web, file, or other plugins in its inference request. Web research, when explicitly requested in ASTRA, is performed by ASTRA's separate search feature; review the sources and consent dialog before sending the selected sources to a model.

## Free use and limits

ASTRA selects only models priced at zero and never selects a paid model. OpenRouter's documented free-model quota currently includes **20 requests per minute and 50 requests per day** for `:free` models on an account without purchased credits. Providers can set additional limits, and free capacity, model quality, context size, and availability can change. Failed requests may also count against a free quota. The `openrouter/free` router is a dynamic choice, not a guarantee of unlimited calls or a specific model. Check the current [free-model collection and quota](https://openrouter.ai/collections/free-models).

ASTRA does not buy credits or silently switch to a billed route. A quota error means to check the account's [Activity page](https://openrouter.ai/activity), wait for the account or provider limit to reset, or switch to the offline model. OpenRouter currently documents up to 20 requests/minute and 50 requests/day for free variants on an account without purchased credits; provider limits may also apply, and failed attempts count toward the daily cap. Adding credits is not needed to use models ending in `:free` and does not cause ASTRA to select paid models.

## Use the offline model instead

1. Open **Second Brain / AI settings**.
2. Select **Use included offline AI**.
3. Select **Start local AI** or send a message to load the bundled model on demand.

The included Qwen model runs in-process using LLamaSharp. It does not use Ollama, `llama-server.exe`, a localhost server, or a cloud provider. It keeps inference content on the PC and has no hosted request quota; local speed, memory, and output capacity are finite. Stop the model when you want to release its memory. The local speech model, OCR, and note/PDF search are also local.

You can switch back to OpenRouter from the settings using the saved key. Selecting **Forget OpenRouter key** deletes the encrypted key and switches to the offline AI option.

## Troubleshooting

| Message or symptom | Suggested action |
| --- | --- |
| Invalid API key / unauthorized | Confirm the copied key starts with `sk-or-v1-`, then replace it in AI settings and save it again. |
| HTTP 429 / rate limit reached | Check [OpenRouter Activity](https://openrouter.ai/activity). Wait for the account/provider limit to reset or choose **Use included offline AI**. Failed requests may count toward the daily quota; ASTRA does not retry against a paid endpoint. |
| No free endpoint available | Try **Automatic free model** or refresh the model list. Availability changes; use offline AI if no free endpoint is available. ASTRA does not route to a paid endpoint. |
| Model temporarily unavailable | Refresh the current catalog, choose another `:free` model, or use the automatic free router. |
| OpenRouter key cannot be decrypted | Keys are tied to the Windows account that saved them. Sign in to that account and save the key again. Reinstalling Windows or using a different account may require a new save. |
| Local model missing or won't load | Re-extract the complete release package, keep the `Ai/components` files together, check available memory, and review the antivirus event. Do not disable antivirus protection. |

For privacy, the key is protected by the current Windows account. Export important Second Brain notes and keep original PDFs before moving user accounts; encrypted notes and AI credentials are tied to Windows account protection.

## Sources

- [OpenRouter free models and current limits](https://openrouter.ai/collections/free-models)
- [OpenRouter API reference](https://openrouter.ai/docs/api_reference/overview)
