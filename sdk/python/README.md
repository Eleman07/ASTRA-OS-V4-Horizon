# ASTRA Python → 3D

Python 3.10 o successivo, nessuna dipendenza obbligatoria. Copia `astra_scene.py` accanto al tuo script (oppure aggiungi la sua cartella a PYTHONPATH).

```python
from astra_scene import Scene
import math
points = [(math.cos(t/40), math.sin(t/40), t/200) for t in range(400)]
Scene('Il mio progetto', axes=('X (m)', 'Y (m)', 'Z (m)')).line('Traiettoria', points).save('scene.json')
```

In ASTRA → STEM → Importa scena JSON scegli `scene.json`. Per dati aggiornati dal progetto attiva Aggiorna automaticamente. Il toolkit salva atomicamente: ASTRA vede soltanto file completi. L'aggiornamento si sospende quando la finestra è inattiva o si lascia STEM.

`Scene.points(name, xyz, color='#B3CFE5')`, `.line(...)`, `.surface(name, x, y, z_grid)` accettano iterabili Python, anche array NumPy. Assi e unità sono scelti dall'autore. Limiti: 24 serie, 50.000 punti totali, JSON ≤ 8 MB, coordinate finite con modulo ≤ 1e100. Una superficie richiede almeno 2×2 nodi.

ASTRA visualizza risultati esportati; il codice Python viene eseguito nel tuo editor/terminale. Gli esempi (`python examples.py --output examples-data`) sono calcoli matematici e illustrazioni, non misure sperimentali. Puoi esportare in JSON anche i risultati del Simulation Lab e mapparne le colonne del CSV sugli assi.
