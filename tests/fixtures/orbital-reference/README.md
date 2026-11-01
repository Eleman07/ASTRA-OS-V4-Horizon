# CubeSat 400 km · progetto software orbitale ASTRA

Questo progetto completo usa **solo la libreria standard Python 3.12 o successivo**. Non richiede account, pacchetti pip, rete o GPU. È un tool numerico deterministico fornito da ASTRA: non dimostra che un modello remoto abbia generato codice corretto o superato un benchmark multiagente.

## Obiettivo e output

Propagare un corpo puntiforme in orbita circolare iniziale a 400 km dalla superficie terrestre nel modello a due corpi; verificare il risultato con soluzione analitica, energia e dimezzamento del passo. Sono inclusi codice completo, test eseguibili, CSV, grafici SVG, relazione tecnica italiana e note Obsidian collegate. Non è un simulatore completo di missione o un progetto hardware di CubeSat.

## Esecuzione riproducibile

Da questa cartella, con Python già installato:

    python -m venv .venv
    .venv\Scripts\python.exe -I -S -B run_project.py --output results --duration-orbits 1

Su Linux/macOS sostituire l'eseguibile con `.venv/bin/python`. Nessuna installazione pip è necessaria: [requirements.txt](requirements.txt) dichiara zero dipendenze esterne. `-I` abilita la modalità isolata di Python, `-S` esclude inizializzazione e hook site-packages e `-B` evita cache bytecode. Una venv non costituisce un sandbox del sistema operativo; eseguire solo il template fidato o codice che si è revisionato.

L'entrypoint accetta esclusivamente la propria directory `results`; una durata tra 1 e 10 orbite. I test di accettazione verificano una singola orbita; le metriche descrivono la durata selezionata. Il processo restituisce 0 soltanto se i test e i controlli numerici della durata richiesta passano. Un errore restituisce un codice diverso da 0.

## File prodotti dopo l'esecuzione reale

- `results/trajectory.csv` e `trajectory-half-step.csv`: tutti i campioni, passi nominali 30 e 15 s; ultimo passo eventualmente ridotto allo stesso istante finale.
- `results/orbit.svg` e `energy.svg`: grafici prodotti dai campioni effettivi, assi e unità espliciti.
- `results/metrics.json`: costanti, riferimenti analitici, errori, ordine osservato, timestamp e hash dei sorgenti eseguiti.
- `results/test-results.json`: esito e log degli otto test unittest realmente eseguiti, inclusi dieci orbite inclinate e un'orbita eccentrica inclinata confrontata col ritorno analitico kepleriano.
- `relazione_tecnica.md` e `obsidian/Risultati e verifiche.md`: rigenerate con i valori calcolati e lo stato PASS/FAIL.

Prima dell'esecuzione i risultati rimangono **UNVERIFIED**; non confondere codice proposto, calcoli realmente eseguiti e verifica scientifica indipendente. Il PASS riguarda solo i requisiti e le tolleranze di questo benchmark didattico.

## Tracciabilità e Obsidian

Leggere [matrice requisiti](requirements.md), [ipotesi](assumptions.md), [architettura](architecture.md), [relazione](relazione_tecnica.md) e [fonti del vault](fonti_vault.md). ASTRA compila `fonti_vault.md` separatamente con gli estratti e gli identificativi realmente recuperati, oppure dichiara che nessuna fonte pertinente è disponibile. I documenti del vault non modificano automaticamente il template fidato.

Le tre note in `obsidian` usano wikilink tra loro: [[Progetto CubeSat 400 km]], [[Metodo e ipotesi]], [[Risultati e verifiche]]. Per preservare i collegamenti ai risultati copiare l'intero progetto in una nuova cartella del vault, oppure usare la pubblicazione ASTRA che conserva la struttura. Le note precedenti dell'utente vanno preservate.

## Verifica indipendente e limiti

Il validator di repository `scripts/verify-cubesat-deliverable.py --project <cartella-progetto>` legge i file senza eseguire il codice del progetto, ricalcola energia ed errori dai CSV e confronta gli hash. La sola presenza di un JSON con scritto PASS non basta. Il validator usa anche un riferimento RK4 indipendente: il suo risultato è un calcolo del validator, non un risultato generato da un agente.

J2 è un'estensione **UNVERIFIED_NOT_IMPLEMENTED**; formula proposta e limiti in [assumptions.md](assumptions.md). Nessuna atmosfera, manovra o validazione di volo viene simulata. Per uno studio scientifico servono fonti appropriate, modelli perturbati, verifica indipendente, incertezze e casi ulteriori.
