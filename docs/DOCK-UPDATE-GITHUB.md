# Primo caricamento e aggiornamenti GitHub

Repository: [Eleman07/ASTRA-OS-V4-Horizon](https://github.com/Eleman07/ASTRA-OS-V4-Horizon).

## Perché la patch dava errore

Il clone verificato contiene soltanto `README.md`. La patch era incrementale e richiedeva file già presenti, come `src/Astra.Desktop/MainWindow.xaml`. Per il primo caricamento occorre copiare il progetto completo. I comandi `git apply` falliti non hanno caricato nulla su GitHub.

## 1. Preparare il clone esistente

La cartella `C:\ASTRA-OS\ASTRA-OS-V4-Horizon-github` è già un clone Git e il ramo `feature/horizon-second-brain-update` è già stato creato. Non ripetere clone, creazione del ramo o applicazione della patch.

Il progetto completo viene preparato dallo script locale seguente, che accetta solo un clone iniziale pulito, conserva `.git`, salva una copia del README iniziale e non esegue stage, commit o push:

```powershell
python C:\ASTRA-OS\ASTRA-OS-V4\scripts\prepare-github-source.py --install C:\ASTRA-OS\ASTRA-OS-V4-Horizon-github
```

Se i sorgenti sono già stati copiati, non ripetere il comando: passa al punto 2. Come alternativa manuale, estrai `dist/ASTRA-OS-V4-HORIZON-complete-source.zip` direttamente nella radice del clone, dove si trova `.git`. Non creare un'ulteriore cartella ASTRA dentro al repository. Il vecchio ZIP `dock-source-update` contiene solo le modifiche incrementali e non basta per inizializzare il progetto.

## 2. Controllare i file

Apri PowerShell e usa la cartella del clone:

```powershell
cd C:\ASTRA-OS\ASTRA-OS-V4-Horizon-github
git branch --show-current
git status --short
Test-Path .\src\Astra.Desktop\MainWindow.xaml
Test-Path .\.github\workflows\build.yml
```

Il ramo deve essere `feature/horizon-second-brain-update` e i due controlli devono dare `True`. È normale vedere molte righe `??`: sono i nuovi sorgenti pronti per il primo commit.

Il progetto include sorgenti, risorse grafiche, modelli 3D, SDK, plugin, test e documentazione. `.gitignore` esclude `bin`, `obj`, `dist`, download, modelli/runtime AI e dati QA personali. Solo il template pubblico con verifiche ancora pending rimane in `docs/evidence/release-evidence.json`, perché è richiesto dai test. Non sono inclusi video o nuove registrazioni.

## 3. Creare il commit locale

Su questo clone appena preparato:

```powershell
git add .
git diff --cached --stat
git commit -m "Import complete ASTRA Horizon source with Second Brain updates"
```

Controlla l'elenco prima del commit. Non devono esserci file `.gguf`, runtime AI `.exe`/`.dll`, cartelle `dist`, `bin` o `obj`, né dati personali.

Se Git chiede l'identità, configura un nome e un indirizzo verificato nel tuo account GitHub o il tuo indirizzo noreply. Esempio da personalizzare, solo per questo repository:

```powershell
git config user.name "YOUR NAME"
git config user.email "YOUR VERIFIED OR NOREPLY EMAIL"
```

Poi ripeti il comando `git commit`. Questi valori vengono pubblicati nei metadati del commit.

## 4. Inviare il ramo su GitHub

```powershell
git push -u origin feature/horizon-second-brain-update
```

Completa l'accesso con il tuo account se richiesto. Non incollare token nel codice o nell'URL del repository. Non servono force push o un nuovo `git init`.

## 5. Unire il progetto nel ramo principale

1. Apri il repository su GitHub.
2. Premi **Compare & pull request** per il ramo appena inviato.
3. Usa il titolo **Import complete ASTRA Horizon source and Second Brain updates**.
4. Controlla i file, crea la pull request e seleziona **Merge pull request → Confirm merge** quando hai completato la revisione.

Il nuovo ramo conterrà l'intero progetto e il README, non soltanto una patch. Questo lavoro locale non esegue automaticamente il push o la pubblicazione.

## 6. Compilare da GitHub Actions

Dopo il merge, apri **Actions → Build and validate ASTRA OS V4 → Run workflow** e scegli il ramo principale. I run sono manuali. Il workflow esegue test e build, poi produce **ASTRA-OS-V4-win-x64-source-build**, conservato per tre giorni.

Questo artefatto è una build dai sorgenti: non contiene i modelli/runtime AI scaricati né gli installer offline Microsoft. Non è il pacchetto completo destinato all'utente finale. I crediti e i manifest AI sono nel repository; il ripristino opzionale delle risorse upstream è descritto nel README.

## 7. Pubblicare un eseguibile completo

Su richiesta dell'utente viene pubblicata soltanto la release, senza caricare i sorgenti del progetto. Il proprietario ha risolto l'avviso locale Norton sul runtime AI e ha autorizzato test e caricamento. La verifica nativa finale ha superato tutti i 31 controlli, inclusa una vera risposta AI da pagina 121 di un PDF. Il nuovo pacchetto si chiama `ASTRA-OS-V4-HORIZON-desktop-second-brain-update-win-x64.zip`; il vecchio ZIP non rappresenta questo aggiornamento. Verifica gli hash e segui le regole di sicurezza del tuo dispositivo.

Quando il pacchetto completo sarà verificato, allega lo ZIP e il suo SHA-256 a **Releases → Draft a new release**. Non aggiungere lo ZIP a un commit Git. GitHub blocca i file oltre 100 MiB nei normali repository; il modello AI locale supera 1 GB.

Riferimenti ufficiali: [limiti dei file](https://docs.github.com/en/repositories/working-with-files/managing-large-files/about-large-files-on-github), [git push](https://docs.github.com/en/get-started/using-git/pushing-commits-to-a-remote-repository), [gestione delle release](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository).
