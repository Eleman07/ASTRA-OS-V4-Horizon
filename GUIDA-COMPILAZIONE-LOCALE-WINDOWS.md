# ASTRA OS V4 Horizon — Compilazione locale Windows 11 (senza GitHub)

La verifica locale del 28/10/2026 ha riprodotto e corretto il crash `0xC000027B`: il publish non includeva `Astra.Desktop.pri`, `App.xbf` e `MainWindow.xbf`. La dashboard completa è stata avviata da `dist/AstraOS-Local`.

Per i comandi e gli stati correnti, usa [installazione locale](docs/LOCAL-INSTALL-2026-10-28.md), [rapporto crash](docs/CRASH-RESOLUTION-2026-10-28.md) e [verifica finale](docs/FINAL-REPORT-2026-10-28.md). Le istruzioni sotto rimangono compatibili.

## 1. Requisiti
- Windows 11 x64 aggiornato, account utente normale.
- Visual Studio con carico di lavoro **WinUI application development** e Windows SDK.
- SDK **.NET 10 x64** (nel progetto: `net10.0-windows10.0.22621.0`).
- Connessione internet per ripristinare le dipendenze NuGet alla prima compilazione.

Scarica Visual Studio: https://visualstudio.microsoft.com/downloads/
Scarica .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0

## 2. Estrai il sorgente
Estrai l'intero ZIP in `C:\ASTRA-OS-V4-SOURCE` in modo che esistano:
- `C:\ASTRA-OS-V4-SOURCE\ASTRA-OS-V4\AstraOS.sln`
- `C:\ASTRA-OS-V4-SOURCE\ASTRA-OS-V4\src\Astra.Desktop\Astra.Desktop.csproj`

La cartella dell'artefatto *unpackaged* con solo gli EXE/DLL non è il sorgente.

## 3. Apri PowerShell nella cartella ASTRA-OS-V4
In Esplora file entra nella cartella che contiene `AstraOS.sln`,
clicca nella barra degli indirizzi, scrivi `powershell` e premi Invio.
Controlla:

```powershell
dotnet --list-sdks
Test-Path .\src\Astra.Desktop\Astra.Desktop.csproj
```

Deve comparire una riga `10.0.xxx` e `True`. Se manca SDK 10, installalo.

## 4. Compila con UN comando
Nella stessa finestra PowerShell:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-local.ps1
```

Lo script compila i test Core e pubblica l'app desktop con:
WinUI 3, `win-x64`, senza MSIX, runtime incluso, no trimming/no single-file.
L'output sarà in:

```text
ASTRA-OS-V4\dist\AstraOS-Local\Astra.Desktop.exe
ASTRA-OS-V4\dist\ASTRA-OS-V4-HORIZON-win-x64-LOCAL.zip
```

Se preferisci non eseguire subito i test Core:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-local.ps1 -SkipCoreTests
```

## 5. In alternativa: Visual Studio (debug con F5)
1. Apri `AstraOS.sln` in Visual Studio.
2. Seleziona piattaforma x64 e configurazione Debug.
3. Imposta `Astra.Desktop` come progetto di avvio.
4. Se il progetto presenta un profilo di avvio "Unpackaged", selezionalo.
5. Compila con Ctrl+Shift+B, poi avvia con F5.
6. Se fallisce, copia il **primo errore** dalla finestra Output > Build.

## 6. Se ASTRA si chiude senza aprire la finestra
Rilancia l'EXE dalla cartella `dist\AstraOS-Local`.
Controlla questo file di log locale:

```powershell
Get-Content "$env:LOCALAPPDATA\AstraOS\Logs\startup.log" -Tail 80
```

La nuova diagnostica scrive l'ultimo step completato:
`App.InitializeComponent`, `MainWindow.create`, `MainWindow.activate`.
Se si blocca dopo `MainWindow.create.begin`, il problema può trovarsi
nella costruzione di `MainWindow` o nella sua XAML.

Puoi anche consultare Event Viewer per `Application Error` (ID 1000).
Per eccezioni `0xC000027B`, esegui F5 e usa
Debug > Windows > Exception Settings / Call Stack.
Evita di sostituire a mano DLL della Windows App SDK.

## 7. Prima di distribuire
Verifica su Windows reale il funzionamento di dashboard, palette blu,
profili STEM, Second Brain, plugin, Ubuntu Lab e Orbital 3D.
Il successo della compilazione **non equivale** a una validazione in esecuzione.
Non disabilitare antivirus/SmartScreen né eseguire ASTRA come amministratore.

## Modifiche rispetto alla sorgente RC1 WinUI compilation-fixed
- Conservati tutti i moduli e le correzioni compilazione preesistenti.
- `PublishTrimmed`, `PublishSingleFile`, `PublishReadyToRun` disabilitati in csproj.
- Inseriti trace locali nelle fasi critiche del bootstrap WinUI; errori non soppressi.
- Aggiunto `scripts/build-local.ps1` per creare release localmente.
- Non sono stati eseguiti `dotnet build` / `dotnet publish` su Windows in questo ambiente.

## 8. Fix XAML — 27 ottobre 2026
Il log dell'utente conferma `MainWindow.InitializeComponent()` con
`Microsoft.UI.Xaml.Markup.XamlParseException`, prima del costruttore.
Il file App.xaml originale aveva un dizionario vuoto; il sorgente aggiornato
include `XamlControlsResources` come primo dizionario fuso.
**IMPORTANTE — fix runtime 0x8000FFFF**: non leggere `Application.Resources`
nel costruttore `App()` prima di creare la prima `Window`: tale accesso può
lanciare `COMException (0x8000FFFF)`, sebbene `InitializeComponent()` riesca.
La precedente riga `App.Resources.merged` è stata rimossa.

Per ricostruire dal nuovo ZIP (nuova cartella sorgente), esegui:

```powershell
cd C:\ASTRA-OS-V4-RESOURCES-FIX\ASTRA-OS-V4
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build-local.ps1
.\dist\AstraOS-Local\Astra.Desktop.exe
Get-Content "$env:LOCALAPPDATA\AstraOS\Logs\startup.log" -Tail 45
```

Se `MainWindow.launch.failed` con `XamlParseException` ricompare, significa
che è necessario diagnosticare un ulteriore problema in `MainWindow.xaml`.
Questa correzione elimina il difetto di lifecycle delle risorse ma non dimostra
che l'intera applicazione sia avviabile. Usa Visual Studio F5 / Output per
identificare la proprietà XAML o il controllo che fallisce.

Ricompila da sorgente **pulito** (non sovrascrivere gli output EXE/DLL precedenti).
Se WinUI continua a fallire: apri in Visual Studio con F5, controlla
Debug > Windows > Output (WinRT originate error), l'eccezione iniziale
e il suo HResult/InnerException. Non assumere che lo stesso crash
abbia una sola possibile causa. La compilazione locale non è stata
eseguita qui perché Windows/WinUI non è disponibile.
