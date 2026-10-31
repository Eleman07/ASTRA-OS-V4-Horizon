# Risoluzione del crash WinUI — 28 ottobre 2026

## Causa verificata

Il publish unpackaged ometteva le risorse **dell'applicazione** `Astra.Desktop.pri`, `App.xbf` e `MainWindow.xbf`. Le risorse WinUI del framework e `Microsoft.UI.Xaml.dll` erano presenti. Il caricamento della dashboard con `Application.LoadComponent` falliva con `XamlParseException`, propagata come eccezione stowed `0xC000027B` in `Microsoft.UI.Xaml.dll`.

Non è stata attribuita la colpa alla DLL per il solo nome riportato dal Visualizzatore eventi. Non sono state sostituite manualmente DLL del framework.

## Prova e isolamento

- Avvio originale da `dist/AstraOS-Local`: crash riprodotto alle **09:48 del 28/10/2026**, eventi Application Error 1000 e Windows Error Reporting 1001, eccezione `0xC000027B`.
- Modalità Minimal: finestra visibile e marcatori `MinimalWindow.create.complete` / `activate.complete`.
- Conservato lo XAML originale in `evidence/startup/MainWindow.original.xaml`.
- Caricamento isolato con XamlReader: l'albero senza gestori viene caricato; il probe non è stato usato come sostituto della UI finale.
- Build `bin` con le risorse generate presenti: dashboard completa, `MainWindow.create.complete`, `MainWindow.activate.complete`, processo responsivo e chiusura normale. Nessuna modifica ampia al layout era stata effettuata a questo punto.
- Confronto delle directory: la cartella pubblicata era priva dei tre file applicativi; la cartella della build li conteneva.
- Publish corretto: avvio e chiusura normali anche da `dist/AstraOS-Local`. Rapporti per processo in `evidence/startup/`.

Il comportamento è coerente con il difetto del percorso MRT Core unpackaged descritto nell'[issue Microsoft Windows App SDK #6720](https://github.com/microsoft/WindowsAppSDK/issues/6720). La diagnosi locale è basata sui controlli e sugli avvii sopra riportati.

## Correzione

`PublishAstraXamlResources` in `Astra.Desktop.csproj` aggiunge PRI e XBF all'elenco `ResolvedFileToPublish`. La compilazione fallisce esplicitamente se le risorse applicative richieste non esistono. Il publish conserva i percorsi relativi degli XBF.

`Test-AstraDistribution.ps1` verifica i file richiesti e impedisce di consegnare un output incompleto. `build-local.ps1` pubblica in staging e promuove l'output soltanto dopo questa verifica. `diagnose-winui.ps1` controlla la sessione corrente, la risposta della finestra, i marcatori e i nuovi eventi di crash; restituisce exit code non zero quando un controllo fallisce.

Rimangono disabilitati trimming, single-file e ReadyToRun. Rimangono `WindowsPackageType=None` e `WindowsAppSDKSelfContained=true`. Non viene effettuato accesso prematuro a `Application.Resources`.

## Altre correzioni runtime verificate

La query del Security Center ora passa un comando PowerShell costante tramite `ArgumentList`, raccoglie stdout e stderr e usa il percorso assoluto di Windows PowerShell. L'invocazione precedente con `-EncodedCommand` non restituiva dati in questo ambiente; l'invocazione diretta restituisce JSON verificato. I singoli servizi non disponibili conservano uno stato sconosciuto.

WSL emetteva output UTF-16 che veniva letto come UTF-8, mostrando caratteri NUL e accenti errati. La lettura ora riconosce UTF-16/UTF-8 e limita la dimensione dell'output. La condizione WSL non installato è stata verificata senza installare componenti.
