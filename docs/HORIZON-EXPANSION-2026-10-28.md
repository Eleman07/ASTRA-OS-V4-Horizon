# ASTRA OS V4 · Horizon — guida alle novità

Avvia `Astra.Desktop.exe` nella cartella `dist/AstraOS-Local`. Mantieni insieme tutti i file della distribuzione. Windows 11 x64 è richiesto. ASTRA è un ambiente desktop nativo sopra Windows: le applicazioni Windows continuano a funzionare normalmente.

## Home, tema e desktop

La Home mostra le icone reali dei file e delle cartelle del Desktop, con i loro nomi. Doppio clic o Invio apre la voce; il menu contestuale aggiorna il Desktop o apre il File Explorer completo. I programmi e i collegamenti richiedono una conferma prima dell'apertura.

Il banner segue la larghezza disponibile: immagine ritagliata senza deformazioni, titoli e spazi adattivi, testo a capo e altezza che cresce con i contenuti. La firma decorativa si nasconde quando manca spazio o il testo Windows è ingrandito. Le schede dei moduli passano da tre a due a una colonna. Il menu dal logo resta disponibile sulle finestre strette.

Personalizza permette di cambiare palette, sfondo e banner, comprese GIF animate. I nove pulsanti del dock contengono icone e mostrano il nome al passaggio del mouse. Il nuovo logo ASTRA è anche l'icona dell'eseguibile e apre il menu rapido nel dock.

Orbital 3D dispone le voci effettive del Desktop in orbita. Una sfera centrale ruota e pulsa con i colori della palette. File, cartelle e nomi non sono elementi dimostrativi. Rendering e animazioni si fermano quando non servono.

## Profili e multitasking

Profili mostra l'identità dell'account Windows e la sua cartella utente. Per STEM puoi scegliere Ingegneria, Fisica, Matematica o Scienze e, per Ingegneria, uno dei 23 indirizzi disponibili. Lo Studio filtra i modelli pertinenti.

Multitasking elenca le finestre Windows realmente aperte. Selezionane da una a otto e scegli due colonne, tre colonne o griglia sul monitor di ASTRA. Ripristina disposizione annulla gli spostamenti della sessione. Il layout usa l'area di lavoro disponibile; ASTRA esce temporaneamente dallo schermo intero per affiancare le finestre.

Le sessioni salvano nome, profilo, percorso STEM, layout, fino a sei app e sei cartelle. Riprendi sessione apre le risorse selezionate; poi scegli le finestre da affiancare. Non vengono creati desktop virtuali fittizi e non viene digitato nulla nelle altre app.

## STEM: dati, simulazioni e modelli 3D

Lo Studio include 13 modelli numerici con parametri, unità, ipotesi e risultati esportabili: orbita RK4 con J₂, assetto quaternionico PD, volo puntiforme, aerodinamica di ala finita, trave Euler–Bernoulli, oscillatore, diffusione termica FTCS, reattore batch, circuito RLC, DFT, coda M/M/1, Lotka–Volterra e Lorenz. Sono strumenti didattici, non software di certificazione o un solver CFD/FEM completo.

La libreria comprende nove mesh scaricate da NASA e glTF Sample Assets, più Ariane 64. Materiali e texture sono caricati localmente. Il grafico resta affiancato al modello; puoi ruotare la camera, sezionare la mesh, vedere il reticolo, spostare il cursore temporale, cambiare velocità e amplificare la deformazione.

Il replay applica i campioni del solutore: traiettoria, quaternione, spostamento, deformazione 1D o colore del campo. Dimensioni e proprietà fisiche non vengono ricavate automaticamente da una mesh. Le viste di chimica, code, ecologia e spettro sono rappresentazioni dei valori calcolati, non simulazioni geometriche del sistema reale.

Importa il tuo modello in GLB, glTF con risorse incorporate, STL o OBJ. Limite 64 MB e un milione di vertici/triangoli. Per OBJ non vengono caricati materiali esterni MTL. Le risorse esterne di un glTF sono escluse: esporta un GLB incorporato. Un modello personale rimane selezionato quando esegui un nuovo calcolo. Le traiettorie Python possono guidarne il movimento.

Il toolkit Python standard library crea scene JSON di linee, punti e superfici; importa il risultato senza eseguire automaticamente lo script. Puoi seguire le modifiche del file e mappare colonne CSV/JSON sugli assi. Data Lab conserva il grafico e le esportazioni CSV/JSON.

### Ariane 6 / Ariane 64

In STEM aerospaziale usa il pulsante Ariane 64 (ESA), oppure la libreria dei modelli. La mesh è una ricostruzione didattica originale ASTRA della configurazione con quattro booster P120C: non è un CAD fornito o approvato da ESA. Dimensioni e configurazione provengono da [ESA](https://www.esa.int/Enabling_Support/Space_Transportation/Ariane/Ariane_6_what_s_it_made_of) e dalla [panoramica del lanciatore](https://www.esa.int/Enabling_Support/Space_Transportation/Launch_vehicles/Ariane_6_overview). Dettagli non pubblicati sono approssimati. Nessun logo ESA viene riprodotto. Le proprietà del vero lanciatore non sono assunte dal modello.

Crediti e licenze sono accessibili dal pulsante nello Studio e da `Graphics3D/web/models/CREDITS.md`; per Ariane, `licenses/ariane64.md`. Le fonti NASA e Khronos sono fissate a revisioni e verificate con SHA256. L'assenza di sponsorizzazione è indicata nei crediti.

## Assistente locale gratuito e Second Brain

Apri Assistente ASTRA o premi Ctrl+Alt+Spazio anche mentre usi un'altra app. La finestra compatta riceve il titolo dell'app richiamata; i contenuti entrano soltanto quando scegli testo, file, appunti o registrazione. Non legge in background documenti, schermo o microfono.

Il pacchetto include llama.cpp CPU e Qwen2.5-1.5B-Instruct quantizzato, Whisper tiny multilingue, OCR Windows e sintesi vocale Windows: nessuna chiave API, account cloud o abbonamento. Il modello testuale viene caricato quando serve, e può essere arrestato per liberare memoria. Il pacchetto occupa spazio soprattutto per i modelli AI; questo non equivale a RAM usata all'avvio.

Input: testo e codice, Markdown, CSV/TSV, JSON, PDF con testo, DOCX, immagini e audio WAV/MP3/M4A/AAC/FLAC/OGG compatibile con i codec installati. Le immagini usano OCR locale con evidenziazione delle parole, non un modello di comprensione generale delle fotografie. I PDF scansionati vanno forniti come immagini. Le registrazioni partono soltanto premendo Registra audio e durano al massimo dieci minuti. I file dell'utente non vengono modificati.

Azioni: spiegazione, riepilogo, checklist, traduzione, analisi di errori, estrazione JSON, ricerca di note, grafici 3D e proposte di widget. I contesti lunghi vengono limitati per il modello locale: l'editor consente di selezionare il materiale pertinente. Le risposte generate sono bozze da verificare, soprattutto per contenuti scientifici.

Output: testo, Markdown, JSON, audio WAV, sottotitoli SRT con tempi, appunti cifrati, grafici e widget. Il widget proposto deve essere aggiunto dall'utente; le sue automazioni iniziano disattivate. L'assistente non dichiara di aver operato nelle altre app.

L'assistente e Second Brain rispondono nella lingua dell'interfaccia Windows anche con fonti in un'altra lingua. Una richiesta esplicita di traduzione può scegliere un'altra lingua. OCR e voci dipendono dai pacchetti lingua Windows; Whisper riconosce la lingua dell'audio, conservando la trascrizione originale.

Second Brain conserva note e tag cifrati per l'account Windows con DPAPI. La ricerca BM25 ordina passaggi pertinenti senza AI. La chat usa i passaggi come contesto e richiede riferimenti [NOTA: id]; non addestra il modello. Le esportazioni JSON sono leggibili in chiaro.

## Widget e automazioni

La sezione Widget offre orologio/fusi orari, sistema, nota, conto alla rovescia, focus, attività, collegamento workspace e automazione. Il creatore consente titolo, testo, stile, larghezza e scadenza; puoi importare ed esportare pacchetti JSON fino a 32 schede.

Le automazioni funzionano mentre ASTRA è aperto, anche in secondo piano: intervallo o orario giornaliero; promemoria nell'app, apertura di una sezione, timer focus o nota cifrata. Il prossimo evento viene salvato prima dell'esecuzione, per evitare ripetizioni. Le automazioni importate restano disattivate fino alla tua scelta. Non eseguono script esterni.

## Gaming e Creator

Gaming registra misure reali di CPU di sistema, motore GPU più occupato e RAM, anche mentre il gioco è in primo piano. Il Session HUD resta sopra le finestre normali e i giochi senza bordi; la modalità esclusiva di un gioco può nasconderlo. Puoi confrontare medie e picchi con la sessione precedente, esportare i campioni CSV e portarli all'assistente. Non vengono inventati FPS, frame time, temperature o latenza. Una differenza tra sessioni non prova una causa o un miglioramento del gioco.

Creator include una bacheca cifrata per 32 asset locali: dimensioni, peso, durata disponibile dei media, colori dominanti delle immagini, contrasto del testo, ordine delle inquadrature, note e durata dello storyboard. Esporta storyboard Markdown e palette CSS. L'assistente può preparare un brief dai metadati, senza fingere di aver visto il filmato. Subtitle Lab importa SRT, corregge offset e scala temporale e salva una nuova versione sincronizzata.

## Puntatori, lingua e accesso a Windows

Personalizza mostra l'anteprima dei 15 puntatori coordinati alla palette, in 32/48/64 px. Applica puntatori Windows del tema agisce soltanto sull'account corrente e salva il set precedente. Ripristina torna al backup. Finché il set ASTRA è attivo, cambia con la palette. La verifica automatica genera e carica i file, senza applicarli al sistema.

La lingua segue le preferenze UI di Windows. Italiano e inglese sono inclusi. Per altre lingue ASTRA prepara una traduzione automatica locale e la conserva nel profilo; durante la preparazione usa l'inglese. Sui dispositivi limitati la preparazione si avvia dal pulsante in Personalizza. La qualità dipende dal piccolo modello multilingue; nomi di file e contenuti personali non vengono tradotti automaticamente.

Strumenti Windows rileva le app già presenti e le ordina in sette categorie: produttività, utilità, creatività, intrattenimento, altro, sviluppo e accessibilità. Le voci sono deduplicate; non reinstalla software già disponibile. App Hub e i 43 plugin dichiarativi restano disponibili, con esempi ed esecuzione locale.

I link web e il pulsante browser usano Chrome. Le associazioni di sistema passano dalla pagina ufficiale App predefinite di Windows.

## Schermo intero, avvio e leggerezza

In Personalizza attiva Avvia ASTRA a schermo intero per coprire anche la barra Windows; F11 o Esc torna alla finestra. Avvia ASTRA all'accesso a Windows registra soltanto questa copia per il tuo account. Mantieni il pacchetto nella stessa cartella.

I dettagli tecnici sono raccolti nei pannelli espandibili. Le animazioni usano Composition e non girano in loop per i pannelli; Eco, movimento ridotto e preferenze di accessibilità le limitano. I renderer 3D sono caricati su richiesta, sospesi in secondo piano e rilasciati uscendo dalla pagina. Il replay evita di ricalcolare deformazioni e colori identici; Eco limita risoluzione e frequenza. Il monitor normale si ferma a finestra inattiva; una registrazione Gaming esplicita continua a 5 s o 15 s in Eco. I widget riducono gli aggiornamenti quando non visibili.

Al primo avvio, fino a 8 GB di RAM o quattro processori logici selezionano una configurazione leggera. Le preferenze già salvate sono rispettate. L'AI limita i thread e rimane facoltativa; puoi usare file, note, ricerca, widget, produttività e simulazioni senza caricare il modello. I criteri per 4/8 GB sono testati come politica software: non costituiscono un benchmark eseguito su un secondo PC poco potente.

Ubuntu verifica distribuzione e avvio effettivi prima di aprire il terminale. Su un PC senza distribuzione usa Installa Ubuntu e completa la prima configurazione. Questa sessione non installa una distribuzione e non sostituisce Windows. Il builder ISO conserva i controlli e il piano esplicito esistenti.

## Componenti e verifiche

Fonti dei modelli in `Graphics3D/web/models/CREDITS.md`; Three.js r180 MIT in `vendor/three/LICENSE`. AI e audio: Qwen Apache-2.0, llama.cpp MIT, Whisper/Whisper.net MIT, NAudio MIT; PdfPig Apache-2.0 con notifiche incluse. I testi delle licenze sono in `Ai/components/LICENSE-*` e nelle notifiche delle dipendenze .NET pubblicate.

Le prove usano profili QA separati. Evidenze in `docs/evidence`: test Core `new-expansion-core.log`, Python/statici `new-expansion-static.log`, servizi Windows `new-expansion-windows-services.json`, navigazione/rendering/audio/AI e profili `qa-new-expansion-final/desktop-smoke.json`, startup `new-expansion-final-startup.json`. Le prove audio usano una voce sintetica; il microfono non viene attivato. Le verifiche finali del pacchetto e dei file ZIP sono registrate separatamente.
