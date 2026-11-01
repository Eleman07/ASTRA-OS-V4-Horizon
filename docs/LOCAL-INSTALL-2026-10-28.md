# ASTRA OS V4 Horizon — installazione locale

ASTRA è un'applicazione desktop per Windows 11 x64. Avviala con un account normale; non sostituisce Explorer, il kernel o il bootloader.

## Avvio della distribuzione

1. Copia o estrai **tutta** la cartella `AstraOS-Local` in una posizione locale scrivibile.
2. Avvia `Astra.Desktop.exe`. Non è necessario aprire Visual Studio.
3. Conserva accanto all'eseguibile `Astra.Desktop.pri`, `App.xbf`, `MainWindow.xbf`, le DLL e tutte le sottocartelle. Copiare soltanto l'EXE impedisce l'avvio.

Il runtime .NET e Windows App SDK è distribuito con l'applicazione. Orbital richiede il runtime Microsoft Edge WebView2; se non è disponibile, il modulo mostra l'errore e la dashboard resta accessibile.

## Compilazione senza GitHub Actions

Prerequisiti: Windows 11 x64, SDK .NET 10, dipendenze WinUI/Windows SDK ripristinabili da NuGet. La versione usata per questa verifica è .NET SDK 10.0.403.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-local.ps1 -RunWindowsTests
```

Il bypass vale per il solo processo dello script. La policy permanente di Windows non viene modificata. Il publish avviene in una cartella nuova, verifica le risorse richieste e conserva la distribuzione precedente se compilazione o validazione falliscono.

Output: `dist\AstraOS-Local\Astra.Desktop.exe` e `dist\ASTRA-OS-V4-HORIZON-win-x64-LOCAL.zip`.

## Uso

- **Home:** metriche di sistema, attività cifrate e produttività calcolata dalle attività completate.
- **Core / STEM / Gaming / Creator:** launcher delle app rilevate, collegamenti a cartelle progetto e strumenti specifici. Profili permette di combinare i cataloghi consigliati.
- **STEM:** Engineering Studio calcola un'orbita circolare a due corpi; Mission Data Lab importa CSV/JSON reali. CSV: intestazione, virgole come separatore e punto decimale. Non è un simulatore di volo.
- **App Hub:** rileva collegamenti del menu Start, filtra categorie, salva preferiti e recenti. Le installazioni WinGet richiedono conferma; l'esito segue il codice di uscita del processo.
- **Second Brain:** note e tag cifrati con DPAPI, ricerca BM25 offline, visualizzazione, importazione ed esportazione. L'esportazione JSON è in chiaro e la UI lo segnala prima della scelta del file.
- **AI locale:** scegli un runtime `llama-server.exe` fidato e un modello GGUF. Nulla viene scaricato o avviato automaticamente.
- **Orbital:** attiva la scena solo quando serve; trascina per ruotare, usa frecce/rotella per camera e zoom. Qualità e limite FPS sono configurabili; lasciare il modulo rilascia WebView2.
- **Ubuntu Lab:** rileva WSL. L'installazione Ubuntu richiede una conferma distinta e l'eventuale UAC di Windows.
- **Security Center:** legge gli stati disponibili. Uno stato non disponibile non viene sostituito con una rassicurazione.
- **ISO Builder:** verifica media e strumenti, propone un piano e richiede una conferma distinta per la costruzione. ADK/OSCDIMG e media Windows legittimi sono necessari; l'avvio dell'ISO va verificato separatamente in VM.

## Dati e diagnosi

I dati ordinari rimangono in `%LOCALAPPDATA%\ASTRA\OS`: `preferences.json`, `tasks\tasks.dpapi`, `brain\notes.dpapi`, `plugins` e cache `WebView2`. DPAPI lega i dati cifrati all'account Windows; conserva una copia esportata prima di migrare account o reinstallare il sistema.

Log locale di avvio: `%LOCALAPPDATA%\AstraOS\Logs\startup.log`.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\diagnose-winui.ps1 -Mode Minimal -CloseAfterCheck
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\diagnose-winui.ps1 -Mode Full -CloseAfterCheck
```

La suite UI è separata dal profilo normale:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\diagnose-winui.ps1 -Mode Full -RunSmoke -DataDirectory "$PWD\docs\evidence\qa-profile" -ObserveSeconds 45 -CloseAfterCheck
```

`ASTRA_DATA_DIRECTORY` è riservato ai test e non serve per l'avvio ordinario. Non eliminare i dati del profilo per risolvere un problema di avvio. Lo stato della verifica e le limitazioni sono nel rapporto `docs/FINAL-REPORT-2026-10-28.md` del repository.
