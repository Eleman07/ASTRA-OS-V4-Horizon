# Second Brain — local personal assistant

Core and STEM keep the **Second Brain** name. Creator and Gaming combine the same tools and library into **ASTRA Assistant**, with one navigation entry. The included model, speech transcription, Windows speech output, OCR, document index and supported actions run on your computer. No AI subscription, cloud API key or paid account is required.

## Talk or type

1. Open **Second Brain** (Core/STEM) or **ASTRA Assistant** (Creator/Gaming) from the sidebar or dock.
2. Type a question or command in the question box and press the question button. The included local model starts on demand for questions that need AI.
3. Select **Open conversation**, or press **Ctrl + Alt + Space**, for voice and attachments.
4. Select **Record audio**, speak, then select **Stop and transcribe**. Recording starts only from your explicit action; there is no always-listening microphone.
5. The transcript becomes the request. With **Reply after transcription** enabled, Second Brain answers after transcription; recognized local commands use the supported action list.
6. Spoken replies can be enabled or disabled. Installed Windows voices determine which spoken languages are available. Text replies follow your Windows interface language.
7. Add files or paste text/images when needed. Use the Goal field for the question about an attachment. Recent conversational turns stay in the current window's memory; use Save note or a remember command to retain information.

For images, the assistant reads text through OCR. This does not provide general visual understanding of every photograph or diagram. Generated responses and proposed widgets are editable drafts.

## Commands that perform real actions

| Request | Result |
|---|---|
| `open Chrome` / `apri Chrome` | Requests a Chrome launch. |
| `open STEM` / `apri STEM` | Opens the ASTRA STEM workspace. Other supported routes include Home, Gaming, Creator, Files, Widgets, Plugins and Multitasking. |
| `open APP NAME` | Opens an installed application whose exact name appears in App Hub. |
| `focus 25 minutes` / `focus 25 minuti` | Creates a focus-timer widget. |
| `add task review chapter 3` / `aggiungi attività ripassare il capitolo 3` | Adds a task to your encrypted task list. |
| `remember that my project uses quaternion control` / `ricorda che il progetto usa controlli a quaternioni` | Stores the requested fact as an encrypted note. |
| `briefing` | Lists real open tasks. |
| `summarize PDF` / `riassumi PDF` | Summarizes the selected library PDF across all readable pages. |
| `cancel PDF` / `annulla PDF` | Cancels the current PDF operation. |

Typed commands can be sent from the main question field or the conversation's **Send** button (**Ctrl + Enter**). A recognized spoken command can run after you stop recording when reply-after-transcription is enabled. The parser accepts supported commands and installed-app identities. Generated text, attachments and web sources cannot authorize file changes or scripts. Project edits and scripts use their own concrete review and explicit consent. See **ASSISTANT-SYSTEM-GUIDE.md** for web research, saved chats, 26 skills and the custom skill editor.

## Long PDFs

1. Select **Import PDF** and choose a file. You can also attach a PDF in the conversation window or enter its path in the import field.
2. Wait for the page counter. Text pages are extracted directly; pages without selectable text are rendered individually and passed to local Windows OCR.
3. Select the imported document in **Document for questions and summaries**.
4. Use **Search notes and PDFs (offline)** with a concept, term or distinctive phrase. Results include original page numbers. Clear the document selection to search the entire imported library.
5. Ask a question. The model receives relevant passages and source identifiers, not the entire book in one oversized prompt. The answer displays PDF source references.
6. Select **Complete summary** to process all readable pages in bounded batches, then combine the partial summaries hierarchically. The page range currently being processed is shown. Long documents can take time on a CPU; you can continue using the interface or cancel.
7. Use **Remove selected PDF** to remove its encrypted index after confirmation. The original PDF remains in its original folder.

Limits: 512 MB, 10,000 pages and 50 million extracted characters per PDF; up to 256 indexed PDFs. Very large or malformed individual pages fail with an error rather than being silently truncated. Scans require a Windows OCR language; text quality depends on the source scan and OCR recognition. Empty/unreadable pages are reported. An unchanged file is detected by its SHA-256 hash and reuses its existing index.

Full summaries compress the document and can omit detail as any summary does. Retrieval is lexical passage search, not an assertion that every answer has read every page. Verify important claims against the displayed page references. The separate Complete summary workflow processes every readable page.

## Memory and data

Notes and extracted PDF pages are encrypted with Windows DPAPI for the current account. Original PDFs are not changed, moved or automatically scanned from other folders. PDF imports publish their index only after all pages finish; cancellation removes the incomplete staging copy.

The original note JSON export includes notes, not the separate PDF index. Keep your source PDFs so they can be reimported on another Windows account or after reinstalling Windows. JSON note exports are readable plain text. The local model is not trained on your private data.

Advanced runtime/model settings are in a collapsed disclosure. Stop the local AI model from ASTRA's existing stop action when you want to release its memory. Supported actions remain separate from generated suggestions, and imported automations remain disabled until you enable them.

If the assistant reports a missing included AI component, check that the complete package was extracted. This update uses in-process LLamaSharp CPU libraries and does not distribute or require the older `llama-server.exe`. Old saved references to that bundled server migrate to the integrated backend. Verify checksums and follow your device's security policy. Offline note and PDF search and supported deterministic commands do not require the model.
