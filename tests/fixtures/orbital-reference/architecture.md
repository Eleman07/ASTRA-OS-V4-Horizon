# Equazioni e architettura software

## Stato ed equazioni

Lo stato cartesiano è y=(x,y,z,vx,vy,vz). La dinamica a due corpi è dr/dt=v e dv/dt=−μr/|r|³, con μ in km³/s². L'energia specifica è ε=|v|²/2−μ/|r|. Per il caso circolare, v=√(μ/r), T=2π√(r³/μ); la soluzione analitica è r(t)=r(cos(nt),sin(nt),0), v(t)=v(−sin(nt),cos(nt),0), n=√(μ/r³).

## Componenti e interfacce

| Componente | Responsabilità | Input / output |
|---|---|---|
| orbital.py | Derivata, passo RK4, propagazione, energia, riferimento analitico, CSV | Stato a sei componenti, durata e passo → campioni e metriche |
| plots.py | Grafici SVG senza librerie esterne | Campioni realmente calcolati → orbit.svg, energy.svg |
| tests/test_orbit.py | Otto test di accettazione, inclusi casi 3D e durata estesa | Funzioni deterministiche → risultati unittest |
| run_project.py | Orchestrazione, test, esportazione e documenti finali | Directory results propria, durata orbite → CSV/SVG/JSON/relazione/note |
| cubesat-manifest.json | Mappa dichiarativa degli artefatti | Percorsi relativi sicuri per il validator |

Gli errori finali confrontano lo stato integrato con quello analitico allo stesso istante, non con un campione arbitrario. I passi 30 e 15 s condividono il tempo finale; il passo finale può essere più corto. La conservazione dell'energia e la convergenza dello stato sono test separati.

Il template non esegue shell, download, pip, plugin o richieste a modelli. Non interpreta o esegue codice proveniente dai documenti del vault. ASTRA verifica gli hash del template distribuito prima dell'esecuzione Python isolata; `-I` e venv riducono interferenze ma non costituiscono un sandbox del sistema operativo.
