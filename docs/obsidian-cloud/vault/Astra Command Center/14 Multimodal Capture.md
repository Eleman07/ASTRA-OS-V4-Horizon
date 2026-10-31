# Multimodal Capture

Capture a source once and keep each extraction tied to it. Use [[Astra Command Center/90 Templates/Multimodal Source|Multimodal Source]] for an audio recording, screenshot, scanned page, photograph or diagram.

| Source note | Modality | Extraction method | Language | Uncertain sections | Reviewed transcript / OCR | Downstream task |
| --- | --- | --- | --- | --- | --- | --- |
|  | audio / image / document |  |  |  |  |  |

## Audio

Preserve timestamps, speaker labels as supplied, unclear words and the original file link. Distinguish a verbatim transcript from a corrected transcript and a summary. Do not infer speaker identity, emotion or agreement from a transcript alone. Derive actions and decisions only when the source supports them.

## Images and OCR

Keep the image reference, crop/page coordinates, extracted text and uncertain characters. Equations, minus signs, decimal separators, indices and units need manual or deterministic checks. OCR text does not describe the geometry of a circuit, free-body diagram, plot or game scene; request remote vision analysis explicitly when that is needed and keep the interpretation separate.

## Text for downstream agents

Provide the exact question, extracted source, source IDs and uncertainty table. For a long recording or document, partition by timestamp/section and produce an index before synthesis. Keep the full extraction so the summary can be checked.

Related: [[Astra Command Center/07 Evidence Ledger|Evidence Ledger]] · [[Astra Command Center/08 Study Lab|Study Lab]] · [[Astra Command Center/12 Creator Studio|Creator Studio]]
