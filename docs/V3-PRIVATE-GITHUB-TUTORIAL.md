# Tutorial — ASTRA OS V3 RC1 su GitHub privato

## 1. Crea la repository privata
Apri https://github.com/new, nome `ASTRA-OS`, seleziona **Private** e crea il repository vuoto senza README iniziale. Le Actions sui repository privati possono utilizzare i minuti inclusi nel piano, soggetti a regole di billing e limiti; consulta https://docs.github.com/en/billing e imposta un budget con stop all'extra.

## 2. Carica il sorgente da Ubuntu (nuovo repository vuoto)

```bash
cd ~/Downloads
unzip ASTRA-OS-V3-RC1-SOURCE.zip
cd ASTRA-OS-V3-RELEASE-CANDIDATE
python3 scripts/validate_source.py
python3 -m unittest discover -s tests/static -v
python3 scripts/lint_cs_strings.py

git init -b main
git add -A
git commit -m "ASTRA OS V3 release candidate"
git remote add origin https://github.com/TUO_USERNAME/ASTRA-OS.git
git push -u origin main
```

Se la repository **non è vuota**, clona la repository anziché inizializzarne una nuova: copia in una nuova branch i file del sorgente, esegui i test e apri una pull request. Non fare `git push --force`. Prima di poter lanciare `workflow_dispatch` dal sito, il workflow deve esistere sul branch di default.

## 3. Avvia build Windows gratuita entro quota
In GitHub: `ASTRA-OS` → **Actions** → **Build and validate ASTRA OS** → **Run workflow** → `main` → **Run workflow**. Il job usa il runner Windows di GitHub, .NET 10 SDK, controlli statici, test Core, build WinUI x64, inventario SHA-256 e upload artefatto `ASTRA-OS-win-x64-unpackaged`.

Non si tratta di una macchina Windows 11 Home/Pro. La build automatizzata non certifica prestazioni, GPU, UAC, installazione o accessibilità dell'interfaccia.

## 4. Se la CI fallisce
Apri la run → `windows-build` → lo step rosso. Copia solo errori e stack trace, eliminando eventuali token/percorsi sensibili. Correggi i file interessati, fai commit e rilancia manualmente. Non condividere token GitHub, note private, password o modelli protetti da copyright.

## 5. Artefatto e prova Windows 11
Se la CI è verde, nella pagina della run scarica **Artifacts → ASTRA-OS-win-x64-unpackaged**. Estrai tutti i file in una cartella Windows: **non** spostare soltanto `Astra.Desktop.exe`, perché la build è self-contained multi-file. Verifica gli hash in `SHA256SUMS.txt` e fai una prova in una VM Windows 11 personale o macchina fisica di test, senza installare sul sistema principale.

Controlla avvio, ricerca, Focus Mode, riapertura preferenze, 4 profili, WinGet, WSL opzionale, note DPAPI, inferenza GGUF, desktop WebGL, chiusura processi, disinstallazione e rollback. Nessuna funzione deve disabilitare le protezioni di Windows.

## 6. Stable gate
Solo dopo evidenze firmate e verificabili per tutti i controlli `docs/evidence/release-evidence.json`, un audit dipendenze, benchmark replicati e firma dell'installer si potrà proporre `V3 Stable`. Esegui `python scripts/release_gate.py`: senza le prove, l'uscita !=0 è **corretta**.
