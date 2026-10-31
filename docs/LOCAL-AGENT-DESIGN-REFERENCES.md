# Developer-project references behind ASTRA's local agent design

The following open-source developer projects informed design choices. ASTRA's C#/WinUI implementation is its own code; these projects are not runtime dependencies or required cloud subscriptions.

| Developer project | Useful pattern | ASTRA implementation |
| --- | --- | --- |
| [Khoj](https://github.com/khoj-ai/khoj) | Personal assistant combining documents, agents and scheduled work | One assistant identity, shared encrypted memory, recurring jobs and document-backed tasks |
| [OpenClaw skills](https://github.com/openclaw/openclaw/blob/main/docs/tools/skills.md) | Reusable `SKILL.md` methods with metadata, scoped to agents | Markdown/JSON creation and import, simple `name`/`description` frontmatter, explicit per-agent assignments |
| [Letta memory and skills](https://docs.letta.com/configuration/skills) | Persistent agent methods and memory across sessions | Shared local notes and source records, role contracts, step checkpoints and stored handoffs |
| [Cognee](https://github.com/topoteretes/cognee) | Combining graph relationships with vector retrieval | Encrypted cached note embeddings plus an inspectable provenance graph; ASTRA graph edges record actual tags, assignments and source use |
| [AnythingLLM tool selection](https://docs.anythingllm.com/agent/intelligent-tool-selection) | Supply relevant methods and tools instead of loading every tool into every prompt | Each specialist receives a precise contract and only its assigned tools/skills |
| [Open WebUI retrieval](https://docs.openwebui.com/features/chat-conversations/rag/) | Preserve source boundaries, metadata and citations | Source IDs, PDF page references and a complete evidence ledger |
| [LangGraph persistence](https://github.com/langchain-ai/langgraphjs/blob/main/docs/docs/concepts/persistence.md) | Checkpoint long-running work and resume interrupted steps | Atomic DPAPI records and a single-worker lease per profile |

Short social-media demos are useful inspiration. Release claims are based on actual local acceptance tasks and their saved reports, rather than the appearance of a demo. ASTRA distinguishes original user/tool evidence from previous AI drafts; earlier AI deliverables are recalled automatically only when the new objective explicitly asks to continue or revisit previous work.

The user-supplied creator reference is [metakaihos / Neomas](https://neomas.app/): connected source notes, recall and review inspired the capture/recall/audit/deliver workflows and Markdown vault interoperability. No proprietary application code, assets or paid knowledge vault is included.

Model/runtime references: [llama.cpp](https://github.com/ggml-org/llama.cpp), [Ministral official GGUF](https://huggingface.co/mistralai/Ministral-3-8B-Instruct-2512-GGUF), [Microsoft Phi-4 GGUF](https://huggingface.co/microsoft/phi-4-gguf), [OpenAI gpt-oss](https://developers.openai.com/api/docs/models/gpt-oss-20b), [Nomic embeddings](https://huggingface.co/nomic-ai/nomic-embed-text-v1.5-GGUF).

Free online alternatives were checked against primary documentation: [OpenRouter limits](https://openrouter.ai/docs/api_reference/limits), [Gemini rate limits](https://ai.google.dev/gemini-api/docs/rate-limits), and [Puter user-pays model](https://docs.puter.com/user-pays-model/). Free allowances are not unlimited hosting. Puter shifts metered costs to the user. ASTRA does not bypass service limits or manufacture accounts to evade quotas.
