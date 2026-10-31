# ASTRA OS V4 — Second Brain Context Fix (RC1 hotfix)

## Problema riprodotto dai log GitHub Actions
Il controllo C# `Contesto marcato come dato` falliva perché `NoteIndex.BuildContext()` generava i delimitatori `[NOTA: ...]` / `[/NOTA]` e successivamente richiamava `Sanitize()` sull'intero contesto, sostituendo anche quelli affidabili. Il difetto interessava le citazioni del Second Brain.

## Correzione
- I delimitatori vengono costruiti dall'applicazione e non vengono mai ripassati nella funzione che neutralizza le etichette inserite dall'utente.
- ID, titoli e testi importati sono trattati come contenuto non attendibile, con neutralizzazione case-insensitive dei delimitatori forgiati e rimozione delle nuove righe nelle intestazioni.
- I blocchi sono sempre chiusi e il contesto è limitato a 8.000 caratteri senza tagliare il marcatore di chiusura.
- I test C# includono blocchi falsificati e note molto lunghe; i test Python aggiuntivi verificano il contratto del sorgente.

**Privacy/sicurezza:** mantenere le note come dati con marcatori affidabili aiuta a ridurre l'ambiguità, ma NON costituisce una protezione completa contro la prompt injection dei modelli linguistici. Nessun contenuto di note deve autorizzare azioni esterne o l'esecuzione di comandi.

## Installazione rapida su repository privato (Ubuntu)
Scaricare `ASTRA-OS-V4-FIX-SECOND-BRAIN-CONTEXT.patch` e aprire il terminale nella cartella del repository ASTRA OS già aggiornato con la correzione precedente Plugin JSON.

```bash
git status --short
git switch -c fix/v4-second-brain-context
git apply --check ~/Downloads/ASTRA-OS-V4-FIX-SECOND-BRAIN-CONTEXT.patch
git apply ~/Downloads/ASTRA-OS-V4-FIX-SECOND-BRAIN-CONTEXT.patch
python3 -S -m unittest discover -s tests/static -v
git add src/Astra.Core/SecondBrain/NoteIndex.cs tests/Astra.Core.SmokeTests/Program.cs tests/static/test_v4_note_context_fix.py docs/V4-SECOND-BRAIN-CONTEXT-FIX.md
git commit -m "Fix Second Brain trusted note delimiters and bounded context"
git push -u origin fix/v4-second-brain-context
```

Poi aprire una pull request verso `main` e, dopo l'unione, eseguire **Actions -> Build and validate ASTRA OS V4 -> Run workflow** sul branch `main`. Se il workflow è manuale e la PR non attiva Actions, selezionare direttamente il branch di correzione nel selettore di esecuzione, se disponibile.

In alternativa, sostituire con i file omonimi presenti nello ZIP completo, senza sovrascrivere altre modifiche del repository.

## Verifiche disponibili
- 27 test Python offline: **PASS**.
- `scripts/validate_source.py`: **PASS**.
- `scripts/lint_cs_strings.py`: **PASS**.
- JS syntax: **PASS**.
- Release gate: correttamente **BLOCKED** in assenza dei collaudi Windows.
- .NET smoke test / build WinUI su Windows: **NON ESEGUITI qui**; occorre l'esito GitHub Actions.

La patch è mirata a questo errore: se il runner segnala altri problemi, non sono automaticamente risolti da questa modifica.
