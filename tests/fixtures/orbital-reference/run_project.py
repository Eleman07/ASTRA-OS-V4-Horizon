"""Execute ASTRA's trusted, stdlib-only orbital software acceptance project."""
import argparse
from datetime import datetime, timezone
import hashlib
import io
import json
import math
from pathlib import Path
import sys
import unittest

if sys.version_info < (3, 12):
    print("Python 3.12 or newer is required. Outputs remain UNVERIFIED.", file=sys.stderr)
    raise SystemExit(78)

ROOT = Path(__file__).resolve().parent
# -I deliberately removes the current directory. Only this reviewed template's
# own directory is added; the ASTRA exporter verifies embedded source hashes.
sys.path.insert(0, str(ROOT))
import orbital
import plots


def json_write(path, value):
    path.write_text(json.dumps(value, indent=2, ensure_ascii=False)+"\n", encoding="utf8")


def report_text(metrics):
    reference, coarse, fine = metrics["reference"], metrics["runs"]["coarse"], metrics["runs"]["fine"]
    return f'''# Relazione tecnica · CubeSat in orbita circolare a 400 km

Stato: **{metrics["status"]}** per il modello numerico didattico a due corpi.
Esecuzione reale: {metrics["executedAtUtc"]}. Provenienza: strumento deterministico ASTRA, non codice generato o validato da un modello remoto. Il superamento di questi test non certifica un satellite reale.

## Ipotesi, unità e riferimento

Terra sferica, corpo puntiforme di massa trascurabile, gravità centrale costante, assenza di atmosfera e perturbazioni. Coordinate cartesiane ECI con orbita iniziale nel piano XY; km, s, km/s. Costanti adottate: μ={orbital.MU_KM3_S2} km³/s², R={orbital.EARTH_RADIUS_KM} km, h={orbital.ALTITUDE_KM} km. Sono parametri espliciti del benchmark, non misure di volo. Vedi [ipotesi](assumptions.md) e [fonti del vault](fonti_vault.md).

## Equazioni e architettura

dr/dt=v; dv/dt=−μ r/|r|³. Stato iniziale r=(R+h,0,0) km, v=(0,√(μ/(R+h)),0) km/s. RK4 a passo fisso nominale, con ultimo passo ridotto per raggiungere lo stesso istante finale. L'architettura separa dinamica, grafici, test e orchestrazione; vedi [architettura](architecture.md) e [matrice requisiti](requirements.md).

## Risultati realmente calcolati

| Grandezza | Valore | Metodo |
|---|---:|---|
| Raggio iniziale | {reference["radiusKm"]:.9f} km | R+h |
| Velocità circolare di riferimento | {reference["speedKmS"]:.12f} km/s | √(μ/r) |
| Periodo circolare di riferimento | {reference["periodSeconds"]:.9f} s | 2π√(r³/μ) |
| Energia specifica di riferimento | {reference["specificEnergyKm2S2"]:.12f} km²/s² | −μ/(2r) |
| Passo nominale grossolano / fine | 30 / 15 s | dimezzamento del passo |
| Errore finale posizione grossolano | {coarse["finalPositionErrorKm"]:.12g} km | confronto con stato circolare analitico allo stesso t |
| Errore finale posizione fine | {fine["finalPositionErrorKm"]:.12g} km | confronto con stato circolare analitico allo stesso t |
| Massima deriva relativa energia grossolana | {coarse["maxRelativeEnergyDrift"]:.12g} | max su tutti i campioni |
| Massima deriva relativa energia fine | {fine["maxRelativeEnergyDrift"]:.12g} | max su tutti i campioni |
| Rapporto errori posizione | {metrics["convergence"]["positionErrorRatio"]:.9f} | errore30 / errore15 |
| Ordine osservato | {metrics["convergence"]["observedOrder"]:.9f} | log₂(rapporto), atteso circa 4 per RK4 |
| Test eseguiti | {metrics["tests"]["run"]}, fallimenti {metrics["tests"]["failures"]}, errori {metrics["tests"]["errors"]} | unittest reale; [registro](results/test-results.json) |

I dati completi sono [CSV grossolano](results/trajectory.csv), [CSV fine](results/trajectory-half-step.csv), [metriche JSON](results/metrics.json), [orbita SVG](results/orbit.svg) e [energia SVG](results/energy.svg). I test numerici sono calcolati da Python, non dedotti da un testo del modello.

## Errori, limiti e J2

Le tolleranze verificano questa singola orbita e questi passi. La conservazione dell'energia da sola non prova l'accuratezza della fase: il confronto con la soluzione analitica e il dimezzamento del passo sono controlli distinti. RK4 non è simplettico; una simulazione lunga richiede ulteriori studi di stabilità. Non sono implementati attrito atmosferico, rotazione terrestre, Sole/Luna, pressione di radiazione, manovre, determinazione orbitale o incertezze. Nessuna previsione di vita orbitale, prestazione hardware o validazione di volo è ottenuta.

**J2: UNVERIFIED_NOT_IMPLEMENTED**, estensione futura. Con z asse polare, f=(3/2) J2 μ R²/|r|⁵ e q=z²/|r|², l'accelerazione perturbativa proposta è (f x (5q−1), f y (5q−1), f z (5q−3)). Richiede test indipendenti del segno, potenziale coerente, precessione e riferimento polare; non usare l'energia a due corpi per giudicare da sola una traiettoria perturbata. Non è stata eseguita una simulazione J2.

## Stato delle evidenze

Calcoli e test sopra: eseguiti dal tool deterministico. Fonti del vault: consultare [fonti_vault.md](fonti_vault.md), compilato separatamente da ASTRA con i soli estratti realmente recuperati; la loro presenza non cambia il codice. Affermazioni non supportate restano UNVERIFIED. Note finali: [[Progetto CubeSat 400 km]], [[Metodo e ipotesi]], [[Risultati e verifiche]].
'''


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=ROOT/"results")
    parser.add_argument("--duration-orbits", type=float, default=1.0)
    args = parser.parse_args()
    output = args.output.resolve()
    if output != ROOT/"results" or (output.exists() and (output.is_symlink() or output.is_junction())):
        raise ValueError("This trusted project writes only its own ordinary results directory")
    if not math.isfinite(args.duration_orbits) or not 1 <= args.duration_orbits <= 10:
        raise ValueError("Duration must be between one and ten orbits")
    output.mkdir(exist_ok=True)
    for file in output.iterdir():
        if file.is_symlink() or file.is_junction():
            raise ValueError("Results cannot contain links")
    duration = args.duration_orbits * orbital.PERIOD_S
    coarse, fine = orbital.propagate(duration, 30), orbital.propagate(duration, 15)
    a, b = orbital.summarize(coarse, 30), orbital.summarize(fine, 15)
    log = io.StringIO()
    suite = unittest.defaultTestLoader.discover(str(ROOT/"tests"), pattern="test_orbit.py")
    outcome = unittest.TextTestRunner(stream=log, verbosity=2).run(suite)
    tests = {"passed": outcome.wasSuccessful(), "run": outcome.testsRun, "failures": len(outcome.failures), "errors": len(outcome.errors), "log": log.getvalue()}
    json_write(output/"test-results.json", tests)
    orbital.write_csv(output/"trajectory.csv", coarse)
    orbital.write_csv(output/"trajectory-half-step.csv", fine)
    plots.create_plots(output, coarse)
    ratio = a["finalPositionErrorKm"]/b["finalPositionErrorKm"]
    valid = outcome.wasSuccessful() and a["maxRelativeEnergyDrift"] < 1e-7 and b["maxRelativeEnergyDrift"] < 1e-8 and b["finalPositionErrorKm"] < 0.01 and 8 < ratio < 32
    hashes = {file.relative_to(ROOT).as_posix(): hashlib.sha256(file.read_bytes()).hexdigest() for file in (ROOT/"run_project.py", ROOT/"orbital.py", ROOT/"plots.py", ROOT/"tests/test_orbit.py")}
    metrics = {"kind": "astra-trusted-orbital-tool", "status": "PASS" if valid else "FAIL", "executed": True, "executedAtUtc": datetime.now(timezone.utc).isoformat(), "codeSha256": hashes,
               "constants": {"muKm3S2": orbital.MU_KM3_S2, "earthRadiusKm": orbital.EARTH_RADIUS_KM, "altitudeKm": orbital.ALTITUDE_KM, "frame": "ECI", "units": "km,s,km/s"},
               "reference": {"radiusKm": orbital.RADIUS_KM, "speedKmS": orbital.SPEED_KM_S, "periodSeconds": orbital.PERIOD_S, "specificEnergyKm2S2": -orbital.MU_KM3_S2/(2*orbital.RADIUS_KM)},
               "runs": {"coarse": a, "fine": b}, "convergence": {"positionErrorRatio": ratio, "observedOrder": math.log2(ratio)}, "tests": {key:value for key,value in tests.items() if key != "log"}, "j2Status": "UNVERIFIED_NOT_IMPLEMENTED"}
    json_write(output/"metrics.json", metrics)
    (ROOT/"relazione_tecnica.md").write_text(report_text(metrics), encoding="utf8")
    (ROOT/"obsidian/Risultati e verifiche.md").write_text("# Risultati e verifiche\n\n[[Progetto CubeSat 400 km]] · [[Metodo e ipotesi]]\n\nStato del tool deterministico: **"+metrics["status"]+"**. Esecuzione: "+metrics["executedAtUtc"]+".\n\n"+f"Velocità di riferimento {orbital.SPEED_KM_S:.12f} km/s; periodo {orbital.PERIOD_S:.9f} s. Test reali: {tests['run']}; errori e fallimenti: {tests['errors']+tests['failures']}.\n\n"+"[Relazione tecnica](../relazione_tecnica.md) · [metriche](../results/metrics.json) · [fonti del vault](../fonti_vault.md). J2 UNVERIFIED_NOT_IMPLEMENTED; nessuna validazione di volo.\n", encoding="utf8")
    print(json.dumps({"status": metrics["status"], "tests": metrics["tests"], "metrics": "results/metrics.json", "provenance": metrics["kind"]}, ensure_ascii=False))
    return 0 if valid else 1


if __name__ == "__main__":
    raise SystemExit(main())
