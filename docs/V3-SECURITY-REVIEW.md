# V3 — Security review e minacce residue

| Superficie | Protezione attuale | Limite residuo |
|---|---|---|
| Integrità applicazioni | Catalogo WinGet statico e conferma dell'utente | La sicurezza del package manager e la supply chain dei vendor vanno verificate |
| Second Brain | DPAPI CurrentUser, file locale, ricerca locale, GGUF localhost, chiave temporanea | Processi maligni nello stesso account possono leggere file in uso/dati decrittati; llama.cpp e modello sono software di terzi |
| AI runtime | Verifica presenza GGUF e intestazione, blocco symlink espliciti, processi su richiesta | Non è una verifica crittografica dell'origine del modello o del binario; potenziali porte TCP locali già occupate |
| Plugin | Ricette dichiarative limitate in input e operazioni | Nessuna sandbox per i plugin eseguibili SDK legacy: UI V3 **non** li esegue |
| 3D WebView2 | CSP `default-src 'none'`, `connect-src 'none'`, allowlist navigazione locale, rendering su richiesta | Driver GPU e runtime WebView2 restano dipendenze terze; test GPU reali mancanti |
| Ubuntu Lab | WSL ufficiale, installazione con conferma/UAC, APT manuale | Le VM WSL non trasferiscono la sicurezza del kernel Linux a Windows; WSL deve ricevere aggiornamenti |
| ISO Builder | Dry-run predefinito, check path, reparse check, mai patch del kernel Windows | Verifica supporto boot UEFI reale mancante; licenza media Microsoft resta dell'utente |
| Release | Gate fail-closed, workflow Actions, SHA-256 artifact inventory | Build/test Windows Home/Pro, audit dipendenze, firme e analisi malware ancora necessari |

## Policy operativa
1. Non disabilitare Defender, Firewall, Windows Update, Secure Boot, SmartScreen e virtualizzazione basata sulla sicurezza.
2. Processi esterni solo su input esplicito; mai eseguire script non attendibili in shell.
3. Distribuire esclusivamente binari compilati da CI fidata e firmati prima della release stabile.
4. Verificare con `dotnet list package --vulnerable --include-transitive` e revisionare i falsi positivi.
5. Non raccogliere telemetria o note in server ASTRA senza consenso separato, informato e documentato.
6. Non attribuire proprietà di sicurezza a Ubuntu che non possono applicarsi alle app Windows.

V3 RC1 non è attestata come Stable: il gate di release rimane intenzionalmente BLOCCATO.
