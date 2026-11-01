# Matrice dei requisiti verificabili

| ID | Requisito | Criterio di accettazione | Evidenza / verifica |
|---|---|---|---|
| REQ-01 | Matrice tracciabile | Otto requisiti con criterio e metodo espliciti | Questo documento e validator strutturale |
| REQ-02 | Costanti, unità, riferimento e ipotesi | h=400 km, R=6378.137 km, μ=398600.4418 km³/s²; stato ECI in km e s | assumptions.md, orbital.py, metriche e CSV iniziale |
| REQ-03 | Equazioni e architettura | dr/dt=v, dv/dt=−μr/|r|³; moduli dinamica/grafici/test/orchestrazione separati | architecture.md, derivative(), revisione dei sorgenti |
| REQ-04 | Software eseguibile e artefatti | Python completo, zero dipendenze esterne, istruzioni; due CSV numerici e due SVG non vuoti | README, requirements.txt, sorgenti AST, risultati e log |
| REQ-05 | Accuratezza e convergenza | v entro 1e-10 km/s e T entro 1e-7 s dal riferimento; drift energia <1e-7 a 30 s e <1e-8 a 15 s; errore posizione fine <0.01 km; rapporto errori tra 8 e 32 | Otto unittest, inclusi dieci orbite 3D con conservazione energia/momento angolare e caso eccentrico 3D; ricalcolo indipendente dei CSV e confronto stato circolare analitico |
| REQ-06 | Risultati, errori e limiti | Tabella dei valori realmente calcolati, durata/passi, limiti e J2 dichiarato UNVERIFIED_NOT_IMPLEMENTED | Relazione tecnica, metrics.json e test-results.json |
| REQ-07 | Documentazione italiana | README e relazione tecnica italiana completi con comandi riproducibili | Revisione documentale e presenza delle sezioni |
| REQ-08 | Note Obsidian finali collegate | Tre note con wikilink risolvibili, fonti del vault effettivamente recuperate o assenza dichiarata | obsidian/*.md, fonti_vault.md, verifica dei collegamenti |

I controlli documentali automatici sono strutturali e non sostituiscono una revisione umana delle spiegazioni. Una simulazione reale di satellite richiede criteri ulteriori.
