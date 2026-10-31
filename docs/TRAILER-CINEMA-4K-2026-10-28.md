# ASTRA Horizon — nuovo trailer cinematografico 4K

Consegna: `dist/trailer/2026-10-28/cinema-4k`. Master `ASTRA-HORIZON-CINEMA-40s-4K60.mp4`: 73.602.436 byte, SHA-256 `907a6af45807c813b1f18d45ec367874a8212e504d50e21f7d46d267a35db722`.

Verifica ffprobe con decodifica e conteggio: H.264 High, 3840×2160 progressivo, yuv420p, 60/1 fps, 40.000000 secondi, 2.400 frame decodificati, un solo stream video e zero stream audio. Foglio delle scene e poster estratti dall'MP4 con FFmpeg `-xerror`, senza errori.

Il film è stato ricostruito in HTML/Canvas/WebGL e GSAP, con fotogrammi disegnati alla risoluzione 4K ed encoder WebCodecs. Non è l'ingrandimento del vecchio trailer registrato. Orbite, sfera, traiettorie, assi XYZ, widget, stelle e transizioni sono procedurali; le interfacce sono mockup inglesi sostituibili, dichiarati in video e nella documentazione. Non sono state inventate risposte dell'assistente o prestazioni misurate.

Sorgenti in `design/trailer-horizon`: CONFIG, studio responsive, replay, modalità fullscreen senza controlli, esportazione MP4, server locale e renderer senza dipendenze npm. Font OFL e librerie locali con crediti. README inglese passo passo, storyboard, analisi del riferimento, cue sheet a 120 BPM e script voice-over inglese. Preset sorgente 45s, 9:16 e voice-over; solo il master landscape 40s è stato esportato come video finito.

Controllati 11 fotogrammi chiave per ognuno dei preset master, vertical e social45: nessun errore browser o media, font caricati, reverse seek coerente. Verificata anche la modalità recording nativa 4K, compreso l'URL per OBS. Il controllo pixel ammette arrotondamenti GPU di due livelli 8-bit in massimo lo 0,01% dei canali e richiede stato GSAP identico. I report includono hash esatti, differenze misurate e tolleranza, in `verification.json`.

ZIP completo `ASTRA-HORIZON-CINEMA-4K-PACK.zip` e ZIP dei sorgenti `ASTRA-HORIZON-SOURCE.zip`: inventario esplicito, hash e CRC verificati. Esclusi cache Chrome, copie del sito di riferimento e vecchie catture desktop. `delivery-manifest.json` contiene dimensioni e hash finali.

Nessuna pubblicazione, messaggio, modifica alle impostazioni Windows o modifica ai binari ASTRA. Il master rimane silenzioso come richiesto: direzione musicale e voice-over sono forniti come indicazioni e testo.
