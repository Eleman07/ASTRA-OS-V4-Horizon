# ASTRA OS V2 Horizon + Ubuntu Lab — tutorial completo (repository GitHub privato)

## 0. Cosa hai in mano

Una **app Windows 11** non una ISO Windows o un kernel Linux modificato. La compatibilità con programmi Windows è ereditata dall'installazione Windows 11 di partenza. Ubuntu è un ambiente WSL 2 **opzionale**, non trasferisce le funzionalità di sicurezza del kernel Linux al kernel Windows.

La UI Horizon è una dashboard WinUI 3 con sidebar, ricerca locale, dock opzionale, controlli energetici e accessibilità; non sostituisce Explorer né cambia la taskbar di Windows.

**Stato:** sorgenti RC2, verifiche statiche eseguite offline. Richiede build Windows + test UI e sicurezza prima del rilascio Stable.

## 1. Da Ubuntu: estrai e controlla i sorgenti

1. Scarica `ASTRA-OS-V2-HORIZON-UBUNTU-RC2-SOURCE.zip`.
2. Apri Terminale:

```bash
cd ~/Downloads
unzip ASTRA-OS-V2-HORIZON-UBUNTU-RC2-SOURCE.zip
cd ASTRA-OS-V2.1-UBUNTU-HORIZON
python3 scripts/validate_source.py
python3 -m unittest discover -s tests/static -v
node --check src/Astra.Desktop/Graphics3D/web/scene.js
```

`node` è facoltativo se non è installato. Il controllo di build reale verrà svolto nel runner Windows GitHub. Non eseguire `wsl --install` in Ubuntu Linux: è un comando **Windows**.

## 2. Crea una repository privata GitHub

1. Vai a `https://github.com/new`.
2. Repository name: `ASTRA-OS`.
3. Seleziona **Private**.
4. Se il repository è nuovo e vuoto, non aggiungere README o .gitignore automatici.
5. Se possiedi già una repo ASTRA-OS privata, lavora in un branch di feature invece di sovrascrivere file a caso. Crea prima un backup o un nuovo branch.

Nel terminale Ubuntu nella cartella estratta:

```bash
git init -b main
git status
git add -A
git commit -m "ASTRA OS V2 Horizon + Ubuntu Lab RC2"
git remote add origin https://github.com/TUO_USERNAME/ASTRA-OS.git
git push -u origin main
```

Sostituisci `TUO_USERNAME`. Se GitHub chiede autenticazione, usa GitHub CLI: `gh auth login` e `gh auth setup-git`. **Mai inserire token, password, modelli privati, note personali o file `.env` nel repository.**

Se il repository non è vuoto: evita `git push --force`; clona la repo, crea un branch e copia i sorgenti aggiornati dopo aver confrontato modifiche locali.

## 3. Abilita e avvia il runner Windows gratuito (nei limiti del tuo piano)

1. Repo GitHub > **Settings** > **Actions** > **General**.
2. Consenti Actions ufficiali richieste dal file `.github/workflows/build.yml`.
3. Imposta le workflow permissions su **Read repository contents** (il workflow ha `permissions: contents: read`).
4. Verifica **Billing & licensing** e imposta budget/limiti di Actions per repo privata. Il costo/consumo dipende dal piano e dal tipo di runner.
5. Repo > **Actions** > **Build and validate ASTRA OS** > **Run workflow** > `main` > **Run workflow**.
6. Apri il job `windows-build` e attendi i risultati. In caso di errore apri il **primo** step rosso e copia il relativo log.
7. Se tutti gli step sono verdi, scorri ad **Artifacts** e scarica `ASTRA-OS-win-x64-unpackaged`.

Questo artifact contiene l'app Windows compilata **se** il job termina correttamente. GitHub Windows runners non equivalgono a un test completo su Windows 11 Home/Pro né garantiscono accesso alla GPU.

## 4. Se preferisci compilare su PC Windows 11

Installa **Visual Studio con tool WinUI**, Windows SDK, .NET 10 SDK (e internet per ripristinare NuGet). Apri PowerShell nella cartella del progetto:

```powershell
.\scripts\build.ps1
.\dist\AstraOS\Astra.Desktop.exe
```

Non usare l'app con diritti di amministratore. L'interfaccia deve avviarsi da un account standard.

## 5. Prova la nuova interfaccia Horizon

- **Dock in basso:** Home, STEM/profili, App Hub, Second Brain, Ubuntu Lab, Security, Esplora file.
- **Ricerca rapida nella barra superiore:** digita `ubuntu`, `stem`, `gaming`, `creator`, `brain`, `security`, `3d`, `iso`, poi premi Invio. Non esegue comandi di shell.
- **ASTRA Eco:** lascia i moduli AI e 3D spenti quando non servono.
- **Mostra il dock:** cambia preferenza nella pagina *Panoramica*, nella scheda prestazioni. Se nascondi il dock, il menu laterale resta sempre disponibile.
- **Movimento ridotto:** limiti la fluidità degli effetti 3D; non altera Windows.

Questa è un'**esperienza in-app ispirata ai principi macOS** (dock, ricerca, semplicità), non replica icone/marchi Apple e non sostituisce Explorer o Finder.

## 6. Usa Ubuntu Lab / WSL 2 in ASTRA OS

1. Apri *Ubuntu Lab* dal menu o dock.
2. Premi **Verifica WSL/Ubuntu**. ASTRA esegue soltanto `wsl.exe --list --verbose`.
3. Se Ubuntu non è installato, premi **Installa Ubuntu (UAC)…** e leggi il dialogo. Dopo il consenso, Windows mostrerà il prompt UAC ed eseguirà `wsl.exe --install -d Ubuntu`. Potrebbero servire privilegi admin, connessione Internet e riavvio.
4. Dopo l'installazione, completa la configurazione iniziale dell'utente Linux nel terminale Ubuntu.
5. Premi **Apri terminale Ubuntu** per richiamare `wsl.exe --distribution Ubuntu`.
6. Clicca **Copia comando aggiornamenti APT**; incollalo nel terminale Linux e conferma le operazioni di APT che desideri eseguire.
7. Facoltativo: **Copia toolchain STEM** e verifica i pacchetti prima di installarli.

Se WSL non parte, controlla virtualizzazione BIOS/UEFI, `Virtual Machine Platform`, versioni WSL e riavvio. Documentazione: https://learn.microsoft.com/it-it/windows/wsl/install e https://ubuntu.com/wsl.

**Privacy:** WSL è locale e non equivale ad assenza assoluta di traffico; APT e lo Store possono contattare servizi esterni per aggiornamenti. Installazione e gestione Linux sono **opt-in**.

## 7. Verifica la sicurezza Windows

In *Sicurezza* premi **Controlla protezioni Windows**. Visualizza dati di Defender, firewall, Secure Boot e cifratura, quando il tuo account può leggerli. `non disponibile` significa dato non verificabile, **non** protezione assente. Tramite i pulsanti puoi aprire la sicurezza Windows, Windows Update, crittografia e isolamento core.

**Non** disabilitare Defender, firewall, Secure Boot, Memory Integrity o Windows Update per cercare più FPS. Non eseguire plugin nativi di terzi in modalità sviluppatore se non li ritieni attendibili; le impronte hash non creano sandbox.

## 8. Crea l'installer (solo dopo una build riuscita)

Su Windows installa Inno Setup 6 e compila `installer/AstraOS.iss`. L'output è una versione **non firmata** di ASTRA Setup: non pubblicarlo come trusted/stable senza firma e audit.

## 9. Checklist QA prima di chiamarlo Stable

- [ ] Workflow Windows verde e artefatto scaricabile
- [ ] Installazione ed esecuzione su Windows 11 Home e Pro
- [ ] Test visuali a 100% / 150% / 200% DPI e tastiera
- [ ] Dock visibile/nascosto e preferenze persistenti
- [ ] WSL assente: messaggio leggibile, senza crash
- [ ] WSL installato: rilevamento distro e avvio Ubuntu
- [ ] UAC rifiutato: nessuna installazione involontaria
- [ ] Security Center: risultati corretti e casi non disponibili
- [ ] Second Brain / plugin / 3D / ISO Builder testati separatamente
- [ ] Benchmark CPU, RAM, avvio, frame pacing e audit dipendenze
- [ ] Firma del binario e prova di disinstallazione/ripristino

La GitHub Action **non esegue automaticamente tutti questi test**. La release gate continuerà a negare lo status Stable fino a evidenza verificata.
