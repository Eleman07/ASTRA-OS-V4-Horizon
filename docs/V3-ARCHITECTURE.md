# ASTRA OS V3 — Architettura tecnica

## Identità
ASTRA OS è una **piattaforma desktop open source per Windows 11**, non una distribuzione autonoma del kernel Windows. Windows resta proprietario; la sua licenza è a carico dell'utente. ASTRA gira senza privilegi elevati e non sostituisce Explorer o Defender.

```text
Windows 11 Home/Pro (x64)
└── Astra.Desktop (WinUI 3, unpackaged, per-user)
    ├── Horizon UX V3 — dock, ricerca locale, Focus Mode, profili, preferenze
    ├── ASTRA App Hub — catalogo approvato / WinGet con consenso
    ├── Performance & Security — misure on-demand e read-only
    ├── Second Brain — note DPAPI CurrentUser / BM25 / llama.cpp opt-in localhost
    ├── ASTRA Plugins — v3 declarativi data-only, no process execution in UI
    ├── Orbital Studio — WebView2 WebGL CSP no-network, FPS-cap, unload on navigate
    ├── Ubuntu Lab — WSL2 opt-in, APT solo comandi copiati
    └── Image Builder — PowerShell dry-run-first, oscdimg ADK, local-only
```

## Nuove funzioni
- `NavigationCatalog`: ricerca delle sezioni e delle applicazioni, senza eseguire comandi arbitrari.
- `AstraPreferences.FocusMode`: modalità senza barra laterale, persistente in `%LOCALAPPDATA%\ASTRA\OS`.
- Second Brain: ricerca di passaggi con identificatori citabili anche senza modello GGUF in esecuzione.
- Quattro plugin dichiarativi aggiuntivi: word count, TODO extractor, Markdown outline, line numbering.
- Orbital Studio V3: orbita procedurale, pan/zoom tramite drag, wheel/tastiera, timer cap rendering, riduzione movimento.
- PerfCenter: campionamento CPU su richiesta (stima normalizzata), nessun loop residente.
- ISO Builder: blocca input con reparse point anche in sottocartelle durante la build.

## Limitazioni intenzionali
- Nessuna esecuzione da UI di plugin nativi non sandboxati; l'SDK legacy è incluso per storia/migrazione ma non va caricato su host di produzione.
- La UI attuale è una finestra WinUI, non un desktop replacement o una shell di Windows.
- L'ISO generata non include licenze o alterazioni automatiche del kernel/driver. Non ridistribuire ISO con file Microsoft senza diritti.
- Nessun cloud AI; il runtime GGUF viene scelto dall'utente e richiede RAM/disco e un binario di terze parti attendibile.
- Firme, isolamento di minacce nell'account Windows, privacy assoluta e zero lag NON possono essere garantiti dai soli controlli statici.
