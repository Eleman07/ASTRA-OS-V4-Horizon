> Guida compatta: per il tutorial aggiornato e completo vedere `docs/TUTORIAL-GITHUB-PRIVATO-V2-RC2.md`.

# GitHub Actions — repository privato

1. Crea un repository GitHub `ASTRA-OS` con visibilità **Private**.
2. Estrai lo ZIP e pubblica **il contenuto della cartella** alla radice della repository (non lo ZIP come singolo file).
3. Verifica che `.github/workflows/build.yml` sia nella branch di default `main`.
4. Da GitHub: `Settings > Actions > General` e abilita GitHub Actions.
5. Controlla `Settings > Billing & licensing > Budgets and alerts` e configura il blocco extra-spesa per Actions.
6. Avvia da `Actions > Build and validate ASTRA OS > Run workflow > main > Run workflow`.
7. Apri il run; ispeziona il primo passaggio rosso e i relativi log.
8. Solo dopo un job verde, nella pagina del run scarica l'artifact `ASTRA-OS-win-x64-unpackaged`.

NOTE: Windows runner non e` Windows 11 Home/Pro. La pipeline verifica la compilazione e test automatici, non avvio della UI, accelerazione GPU, sicurezza garantita, licenze software o compatibilita` di tutti i dispositivi.

Per effettuare un push da Ubuntu:

```bash
cd /percorso/ASTRA-OS-V2.1-UBUNTU-HORIZON
git init -b main
git add -A
git commit -m "ASTRA OS V2 Horizon + Ubuntu Lab RC2"
git remote add origin https://github.com/USERNAME/ASTRA-OS.git
git push -u origin main
```

Se HTTPS richiede credenziali, usare `gh auth login` (GitHub CLI) oppure una credenziale Git sicura. Non incollare token nei file.
