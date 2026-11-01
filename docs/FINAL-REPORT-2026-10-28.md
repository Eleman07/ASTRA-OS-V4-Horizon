# ASTRA OS V4 Horizon — verifica locale del 28 ottobre 2026

## Esito

Il crash iniziale è stato riprodotto e corretto. La distribuzione unpackaged apre la dashboard completa su Windows 11 Pro x64, registra `MainWindow.create.complete` e `MainWindow.activate.complete`, resta responsiva e si chiude normalmente. È una **release candidate locale**, non una certificazione di produzione.

Il problema era il publish privo delle risorse applicative PRI/XBF, non una DLL del framework da sostituire. Vedi [rapporto della correzione](CRASH-RESOLUTION-2026-10-28.md).

## File consegnati

- Sorgenti aggiornati nello stesso repository, senza riscrittura dell'applicazione o modifica dei dati personali preesistenti.
- `dist/AstraOS-Local/Astra.Desktop.exe`, tutte le dipendenze, PRI/XBF, asset, profili, ricette, builder, licenza e istruzioni.
- `dist/ASTRA-OS-V4-HORIZON-win-x64-LOCAL.zip`: distribuire l'intera cartella contenuta.
- [Istruzioni di installazione e build](LOCAL-INSTALL-2026-10-28.md).
- [Confronto fra riferimento e screenshot reali](SCREENSHOT-COMPARISON-2026-10-28.md).
- Evidenze locali in `docs/evidence/`; profilo sintetico separato in `docs/evidence/qa-profile/`.

## Ambiente

Windows 11 Pro x64, build 26300, .NET SDK 10.0.403. Target desktop `net10.0-windows10.0.22621.0`. Windows App SDK 2.5.1, secondo il progetto esistente. Non sono stati usati GitHub Actions, sostituzioni manuali delle DLL o cambiamenti a partizioni/bootloader.

## Modifiche e funzioni disponibili

| Area | Stato e comportamento |
|---|---|
| Avvio e distribuzione | **Verificato.** Target MSBuild pubblica PRI/XBF; validatore controlla la struttura. Publish in staging con conservazione dell'output precedente in caso di errore. |
| Dashboard | **Funzionante.** Sidebar, ricerca locale, dock, artwork, attività DPAPI e produttività calcolata da date reali di completamento. Tutte le attività salvate sono accessibili tramite elenco scorrevole. |
| Temi | **Verificato.** Cinque colori, applicazione dal vivo, persistenza, contrasto delle card e reset. I pennelli vengono mutati, senza sostituire risorse WinUI in uso. |
| Core / STEM / Gaming / Creator | **Verificato per navigazione e funzionalità interne.** Launcher distinti per categoria, cartelle progetto e preferenza workspace. I profili possono coesistere. |
| Monitoraggio | **Verificato sulla macchina locale.** CPU di sistema, RAM totale/disponibile, GPU tramite contatori PDH, spazio disco, batteria, rete e numero processi. Storico fino a 60 campioni. Refresh 5 s, 15 s in Eco, sospeso con finestra inattiva. |
| App Hub | **Rilevamento e preferiti verificati:** 68 collegamenti effettivi del menu Start. Ricerca, categorie, recenti e avvio tramite collegamenti. WinGet attende il processo e distingue esito positivo/errore. |
| STEM | **Calcolo e parsing verificati.** Orbita circolare a due corpi con unità esplicite, importazione CSV/JSON limitata, statistiche e grafico numerico, anteprima tabellare. Workflow Python/Jupyter copiabile e avvio di simulatori selezionati con conferma. |
| Gaming | **Funzionante con limiti.** Launcher da menu Start, sessione manuale, metriche e profili bilanciato/Eco reversibili. Nessuna disattivazione della sicurezza Windows. |
| Creator | **Funzionante con limiti.** Launcher creativi, collegamenti a cartelle progetto/asset, monitoraggio e preset Eco. |
| Second Brain | **Storage e ricerca verificati.** Note/tag DPAPI, BM25 con tag, visualizzazione, importazione testo/Markdown e archivi JSON, esportazione JSON esplicita in chiaro, eliminazioni con conferma. Importazioni non valide non sovrascrivono l'archivio. |
| AI locale | **Architettura preservata; inferenza NOT TESTED.** Nessun GGUF/runtime fornito. Stato inattivo corretto finché l'utente non inizializza un modello. Nessun download o avvio automatico. |
| Orbital | **Rendering reale verificato.** WebGL offline lazy, due cicli di caricamento/rilascio, camera/zoom, qualità, cap FPS, palette, riduzione movimento ed Eco. La prontezza viene confermata dal renderer; la cache è nel profilo locale e non nella distribuzione. |
| Ubuntu Lab | **Assenza WSL verificata e gestita.** Output Unicode leggibile, timeout e rilevamento distribuzioni. Installazione e uso effettivo di Ubuntu: **NOT TESTED**, WSL non installato. |
| Security Center | **Query locale verificata.** Defender, firewall, integrità memoria, ultimo hotfix; Secure Boot e cifratura restano non disponibili quando i privilegi non consentono la lettura. Disponibilità di nuovi aggiornamenti rimanda a Windows Update. Nessuna protezione viene cambiata. |
| Plugin | **Verificato.** Ricette dichiarative, validazione, trasformazioni permesse, abilitazione/disabilitazione persistente. Il percorso UI dei plugin nativi resta disabilitato. |
| ISO Builder | **Controlli di sicurezza verificati.** Percorsi sovrapposti respinti, sorgente preservata, nessun output nel caso negativo. Rilevamento DISM/ADK; metadata WIM/ESD verificati in sola lettura tramite DISM quando i prerequisiti sono presenti. Build e boot ISO: **NOT TESTED**, ADK/OSCDIMG e media Windows non disponibili. |

## Verifiche eseguite

- **72 controlli Core superati**: profili, preferenze, comandi, DPAPI, input avversari dei plugin, contesto note, BM25/tag, CSV/JSON e formule orbitali.
- **43 test statici superati**, validazione sorgente e scanner delle stringhe C# superati.
- **36 controlli UI superati** nel profilo QA: 12 rotte, tema dal vivo/reset/persistenza, preferiti, dock, Eco reversibile, task, note, dataset, plugin, metriche e due cicli WebGL.
- Test Windows in sola lettura: campioni reali CPU/RAM/GPU, contatori processi, rilevamento app, disposizione delle risorse, Security Center e condizione WSL assente.
- Test di sicurezza del Deployment Lab: piano con directory identiche rifiutato prima di copiare, file sorgente invariato e nessun ISO creato.
- Build desktop **Debug e Release**, zero avvisi e zero errori.
- Avvio Full e Minimal dalla directory pubblicata; creazione/attivazione, risposta, chiusura e riapertura. Nessun nuovo evento di crash corrispondente negli intervalli controllati.
- Screenshot della dashboard e di Orbital acquisiti dalla finestra reale. DPI osservato dalla suite: `RasterizationScale=1` (**100%**).

I JSON e i log sono la fonte dei risultati dettagliati. I precedenti fallimenti della suite hanno individuato una sostituzione di risorse durante il cambio tema e una mancata attesa del campione di sistema; entrambi sono stati corretti e la suite ripetuta.

## Prestazioni misurate

Tre avvii locali della build finale in modalità normale hanno mostrato una finestra visibile/responsiva in **473–500 ms**, working set **180,7–185,2 MB** e CPU media del processo nei brevi campioni **0,504–1,611%** della capacità totale. Dati grezzi: `evidence/astra-benchmark.csv`.

Sono campioni locali brevi, dopo caricamento delle dipendenze: il tempo della finestra è un proxy, non una misura del primo frame interattivo o di un avvio a cache fredda. Non sono benchmark comparativi di hardware, frame time, gaming o rendering. Il codice del benchmark chiude le finestre normalmente.

Lo ZIP contiene **540 file**, tutti confrontati tramite SHA-256 con la directory pubblicata. Risultati e checksum: `evidence/final-artifact-hashes.json`. La cache WebView2 viene creata nel profilo locale; non viene inserita nel pacchetto.

## Limiti espliciti e verifiche mancanti

- La replica del riferimento **non è pixel-identica**. Le differenze sono documentate nel confronto visivo; non viene dichiarata una corrispondenza perfetta.
- **150% e 200% DPI: NOT TESTED.** Non sono state cambiate le impostazioni Windows della macchina.
- **Inferenza GGUF, installazione WinGet, avvio effettivo di MATLAB/Jupyter/simulatori e installazione WSL: NOT TESTED.** Non sono stati installati o eseguiti programmi esterni soltanto per ottenere un esito positivo.
- La discovery copre i collegamenti del menu Start: librerie Steam/Epic complete, tutte le app UWP e programmi portabili non sono censiti automaticamente. Le categorie sono euristiche.
- Il sistema progetti collega cartelle esistenti; non sostituisce un editor, un asset manager o un render manager. La sessione Gaming viene avviata manualmente. I profili agiscono sulle attività di ASTRA, non su driver o impostazioni globali Windows.
- Engineering Studio implementa un calcolo circolare a due corpi, non un sistema completo di simulazione aerospace. Le note supportano tag/ricerca; non è stato aggiunto un sistema di versionamento collaborativo.
- Secure Boot e cifratura non sono stati attestati con privilegi amministrativi. Il controllo hotfix non equivale a una scansione completa degli aggiornamenti disponibili.
- **Costruzione e boot UEFI/BIOS dell'ISO: NOT TESTED.** Nessuna ISO viene dichiarata avviabile.
- Test prolungati, leak sotto carico, frame time con WPA/ETW, più configurazioni GPU e firma/certificazione dell'installer: **NOT TESTED**.

L'avvio critico è risolto e i flussi interni sopra indicati sono utilizzabili. Le condizioni dipendenti da componenti esterni, la fedeltà visiva esatta e la verifica estesa impediscono di dichiarare soddisfatti tutti i criteri di una release commerciale finale.
