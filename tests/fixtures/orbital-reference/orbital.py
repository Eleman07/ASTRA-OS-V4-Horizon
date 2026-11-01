"""Deterministic educational two-body orbit, kilometres and seconds, ECI axes."""
import csv
import math
from pathlib import Path

MU_KM3_S2 = 398600.4418
EARTH_RADIUS_KM = 6378.137
ALTITUDE_KM = 400.0
RADIUS_KM = EARTH_RADIUS_KM + ALTITUDE_KM
SPEED_KM_S = math.sqrt(MU_KM3_S2 / RADIUS_KM)
PERIOD_S = 2.0 * math.pi * math.sqrt(RADIUS_KM**3 / MU_KM3_S2)
INITIAL_STATE = (RADIUS_KM, 0.0, 0.0, 0.0, SPEED_KM_S, 0.0)
COLUMNS = ("t_s", "x_km", "y_km", "z_km", "vx_km_s", "vy_km_s", "vz_km_s", "specific_energy_km2_s2")


def norm(values):
    return math.sqrt(sum(value * value for value in values))


def derivative(state):
    radius = norm(state[:3])
    if not math.isfinite(radius) or radius <= EARTH_RADIUS_KM:
        raise ValueError("State intersects Earth or is not finite")
    scale = -MU_KM3_S2 / radius**3
    return tuple(state[3:]) + tuple(scale * value for value in state[:3])


def rk4_step(state, step_s):
    if not math.isfinite(step_s) or step_s <= 0:
        raise ValueError("Step must be finite and positive")
    def shifted(slope, fraction):
        return tuple(value + step_s * fraction * delta for value, delta in zip(state, slope))
    k1 = derivative(state)
    k2 = derivative(shifted(k1, 0.5))
    k3 = derivative(shifted(k2, 0.5))
    k4 = derivative(shifted(k3, 1.0))
    return tuple(value + step_s / 6 * (a + 2*b + 2*c + d) for value, a, b, c, d in zip(state, k1, k2, k3, k4))


def specific_energy(state):
    return sum(value * value for value in state[3:]) / 2 - MU_KM3_S2 / norm(state[:3])


def analytic_state(time_s):
    phase = math.sqrt(MU_KM3_S2 / RADIUS_KM**3) * time_s
    cosine, sine = math.cos(phase), math.sin(phase)
    return (RADIUS_KM*cosine, RADIUS_KM*sine, 0.0, -SPEED_KM_S*sine, SPEED_KM_S*cosine, 0.0)


def propagate(duration_s=PERIOD_S, step_s=30.0, initial_state=INITIAL_STATE):
    if not math.isfinite(duration_s) or not 0 < duration_s <= 10 * PERIOD_S:
        raise ValueError("Educational run requires 0 < duration <= 10 periods")
    if not math.isfinite(step_s) or not 0 < step_s <= 300:
        raise ValueError("Step must be in (0, 300] seconds")
    if len(initial_state) != 6 or not all(math.isfinite(value) for value in initial_state):
        raise ValueError("Initial state must contain six finite components")
    derivative(initial_state)
    rows = [(0.0, *initial_state, specific_energy(initial_state))]
    time_s, state = 0.0, tuple(initial_state)
    while time_s < duration_s:
        next_time = min(duration_s, time_s + step_s)
        if next_time <= time_s:
            raise ValueError("Time step is below floating point resolution")
        state = rk4_step(state, next_time - time_s)
        time_s = next_time
        rows.append((time_s, *state, specific_energy(state)))
    return rows


def summarize(rows, step_s):
    initial_energy = specific_energy(rows[0][1:7])
    exact = analytic_state(rows[-1][0])
    return {"nominalStepSeconds": step_s, "durationSeconds": rows[-1][0], "samples": len(rows),
            "maxRelativeEnergyDrift": max(abs((specific_energy(row[1:7])-initial_energy)/initial_energy) for row in rows),
            "finalPositionErrorKm": norm([rows[-1][index+1]-exact[index] for index in range(3)]),
            "finalVelocityErrorKmS": norm([rows[-1][index+4]-exact[index+3] for index in range(3)])}


def write_csv(path, rows):
    with Path(path).open("w", newline="", encoding="utf8") as stream:
        writer = csv.writer(stream)
        writer.writerow(COLUMNS)
        writer.writerows(rows)
