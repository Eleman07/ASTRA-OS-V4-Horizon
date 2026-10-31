# ASTRA OS V4 RC1 — Build Windows gratuita con GitHub Actions (repository privato)

## Requisiti

Un repository GitHub privato creato dall'utente, Actions abilitato e quota disponibile. Il workflow è attivabile manualmente (`workflow_dispatch`) e non utilizza segreti per la compilazione. Assicurati di avere un budget di utilizzo GitHub Actions appropriato.

## Da Ubuntu

1. Estrai lo ZIP, entra nella cartella `ASTRA-OS-V4`.
2. Facoltativo: controlli sorgenti `python3 scripts/validate_source.py && python3 -m unittest discover -s tests/static -v`.
3. Crea una nuova repository **Private** in `https://github.com/new` (es. `ASTRA-OS`).
4. Solo se la repository è nuova e vuota:

```bash
git init -b main
git add -A
git commit -m "ASTRA OS V4 RC1 - WinUI Horizon"
git remote add origin https://github.com/TUO_USERNAME/ASTRA-OS.git
git push -u origin main
```

Per una repository preesistente, usa un branch nuovo invece di reinizializzare la cronologia. Non usare `git push --force`.

## Build

1. GitHub → repository → **Actions** → **Build and validate ASTRA OS V4**.
2. **Run workflow** → branch `main` (o il branch selezionato) → **Run workflow**.
3. Leggi in ordine l'esito di: static tests, core smoke tests Windows, publish WinUI, inventory checksum.
4. Se il workflow è verde, scarica l'artefatto `ASTRA-OS-V4-win-x64-unpackaged` e prova l'app su Windows 11 Home/Pro.
5. In caso di errore, apri il job `windows-build` e conserva l'output del primo passaggio fallito per la correzione.

## Esecuzione Windows

Da PowerShell nella cartella estratta dall'artefatto:

```powershell
.\Astra.Desktop.exe
```

Verifica manualmente `Personalizza` → modifica dei 5 token → applicazione immediata → riavvio → persistenza, `Ripristina originale`, Dashboard → animazione del banner, Eco/Movimento ridotto, click dei moduli, Task crittografati, misura Performance. Non avviare processi come amministratore senza ragione. Esegui test ISO Builder esclusivamente con file Microsoft legittimamente ottenuti e usa prima l'anteprima/dry-run.

**Importante:** le macchine GitHub Actions Windows Server non sostituiscono una verifica di UX e driver su Windows 11 con monitor e GPU reali.
