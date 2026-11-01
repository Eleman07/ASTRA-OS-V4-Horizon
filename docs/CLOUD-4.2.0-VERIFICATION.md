# ASTRA 4.2.0 cloud candidate verification

Checked on 2026-10-31. This is a candidate, not a completed stable-release certification. Synthetic tests used isolated local profiles; the personal Obsidian vault was not uploaded or committed.

| Check | Result and scope |
| --- | --- |
| Core agent / knowledge / routing tests | 182 agent-team checks passed, including automatic provider recovery, mocked HTTP failover, cancellation, full input and concrete UTF-8 project-file exports. These are not live model benchmarks. |
| Daily quota preflight | The saved encrypted credential returned HTTP 200. A live diagnostic reported an exhausted shared daily pool. The new guard stopped before sending a generation request; deterministic regressions verify UTC-reset scheduling and zero completion requests when the pool is already exhausted. This fixes task handling, not the provider's quota. |
| CubeSat model-generation acceptance | Requires the actual remote model to generate the complete requested software project and export its own files. A prewritten reference project is not substituted for this task. Live completion remains UNVERIFIED while the account's free request quota is exhausted. |
| Independent orbital acceptance validator | 8 validator regression checks passed against QA fixtures, including actual CSV reconstruction, tampered energy/reference data, duplicate fine-step output, truncated orbit, nonfinite SVG data and refusal to execute modified Python. The reference fixture's numerical tests do not count as model-generation success. It is not embedded in the application. |
| Long input / history | A 2.1-million-character document was extracted in full. Encrypted chat input/output beyond 100,000 characters and complete chronological reload passed. |
| Native Windows UI | 10 checks passed in an isolated profile: eight workflows, eight contracts, cloud-only defaults, provider output maximum, long objective/source fields, progress/results and graph tab. This test made no network/model calls. |
| Python source checks | 63 static tests, source validation and C# string-literal scan passed. These checks do not establish model accuracy. |
| Real Ultra call | Completed a synthetic study/research pack, preserving the supplied deterministic orbital reference at 5828.5166 s / 97.1419 min, two flashcards, an exercise, concept map and source IDs. This checks evidence preservation, not independent model arithmetic or general scientific validity. |
| Real North Mini Code call | Returned a Python orbital-period function with assertions and validation in a synthetic coding availability check. Generated code was not executed by that remote call. |
| Inkling free endpoint | Advertised in the free catalog; request returned access forbidden for this account/provider. It is not a preselected specialist. |
| Complete live multi-agent benchmark | Not passed: aerospace, creator and gaming jobs encountered HTTP 429 (free endpoint quota). Failure/checkpoint records were retained. No paid/local fallback ran. |
| Online image / audio probes | Not passed: both synthetic requests encountered HTTP 429. The media adapters compile; successful live interpretation/transcription remains unverified. |
| Generic Obsidian kit | 38 new notes/templates installed with zero overwritten files. Generic-package audit: zero read errors, unresolved wiki links or duplicate-title warnings. YAML parsing was not executed because PyYAML was unavailable. |
| Build | Windows x64 self-contained cloud candidate includes WinUI PRI/XBF, worker and supervisor. No local model weights are shipped. Restore used cached dependencies; the NuGet service index was unavailable in the sandbox, so a fresh online vulnerability audit was not completed. |

The main route is NVIDIA Nemotron 3 Ultra. The verified optional coding assignment is North Mini Code; image/audio routes use compatible NVIDIA multimodal models. Each call checks the current catalog, capability and all advertised zero-price fields; retired or repriced preferences can migrate to a compatible free family/alternative with a recorded notice. Equivalent quality is not assumed.

ASTRA preserves supplied text, selected source passages, complete saved conversation and returned output. Output requests use the provider's advertised model maximum; length-limited responses can continue without discarding previous output. Source retrieval selects relevant passages and labels them as excerpts. Unsupported files remain metadata only. ZIP expansion and external-entity protections are explicit security rejections, not silent token truncation.

Unavailable/saturated endpoints now trigger automatic compatible zero-price model substitution. Shared quota exhaustion and temporary catalog outages retain durable checkpoints and retry times across worker restarts; no Resume action is needed. This recovery cannot remove service quotas. Agent-proposed files are exported for review without execution. Only actual model-generated artifacts and their observed validation results can satisfy the CubeSat acceptance task; QA reference code is never injected as its solution.

Quota exhaustion, model/provider access, context/output ceilings, network availability and model mistakes remain external constraints. The stable release gate remains blocked pending successful live complex/multimodal validation. See [the concrete orbital acceptance scope](benchmarks/CUBESAT-400KM-ACCEPTANCE.md).
