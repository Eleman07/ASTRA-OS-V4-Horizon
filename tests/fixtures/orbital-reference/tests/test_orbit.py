"""Numerical acceptance tests for the trusted ASTRA orbital project tool."""
import math
import unittest
import orbital


class OrbitAcceptanceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.coarse = orbital.propagate(step_s=30.0)
        cls.fine = orbital.propagate(step_s=15.0)
        cls.coarse_metrics = orbital.summarize(cls.coarse, 30.0)
        cls.fine_metrics = orbital.summarize(cls.fine, 15.0)

    def test_reference_speed_and_period(self):
        self.assertAlmostEqual(orbital.SPEED_KM_S, 7.668558175407055, delta=1e-10)
        self.assertAlmostEqual(orbital.PERIOD_S, 5553.624271252228, delta=1e-7)

    def test_circular_initial_state_and_equations(self):
        state = orbital.INITIAL_STATE
        self.assertAlmostEqual(orbital.norm(state[:3]), 6778.137, delta=1e-9)
        self.assertAlmostEqual(sum(a*b for a,b in zip(state[:3], state[3:])), 0.0, delta=1e-12)
        self.assertAlmostEqual(orbital.derivative(state)[3], -orbital.MU_KM3_S2/orbital.RADIUS_KM**2, delta=1e-14)

    def test_energy_conservation(self):
        self.assertLess(self.coarse_metrics["maxRelativeEnergyDrift"], 1e-7)
        self.assertLess(self.fine_metrics["maxRelativeEnergyDrift"], 1e-8)

    def test_analytic_state_reference(self):
        self.assertLess(self.fine_metrics["finalPositionErrorKm"], 0.01)
        self.assertLess(self.fine_metrics["finalVelocityErrorKmS"], 1e-5)
        self.assertAlmostEqual(self.coarse[-1][0], self.fine[-1][0], delta=1e-9)
        self.assertGreaterEqual(self.fine[-1][0], orbital.PERIOD_S)

    def test_step_halving_convergence(self):
        ratio = self.coarse_metrics["finalPositionErrorKm"]/self.fine_metrics["finalPositionErrorKm"]
        self.assertGreater(ratio, 8.0)
        self.assertLess(ratio, 32.0)
        self.assertGreater(math.log2(ratio), 3.0)

    def test_invalid_inputs_are_rejected(self):
        for step in (0, -1, float("nan"), float("inf"), 301):
            with self.assertRaises(ValueError):
                orbital.propagate(step_s=step)

    def test_ten_orbits_inclined_energy_and_angular_momentum(self):
        inclination = math.radians(63.0)
        initial = (orbital.RADIUS_KM, 0., 0., 0., orbital.SPEED_KM_S*math.cos(inclination), orbital.SPEED_KM_S*math.sin(inclination))
        rows = orbital.propagate(10*orbital.PERIOD_S, 15., initial)
        def angular_momentum(state):
            x,y,z,vx,vy,vz = state
            return (y*vz-z*vy, z*vx-x*vz, x*vy-y*vx)
        energy0 = orbital.specific_energy(initial)
        momentum0 = angular_momentum(initial)
        energy_drift = max(abs((orbital.specific_energy(row[1:7])-energy0)/energy0) for row in rows)
        momentum_drift = max(orbital.norm([a-b for a,b in zip(angular_momentum(row[1:7]),momentum0)])/orbital.norm(momentum0) for row in rows)
        self.assertLess(energy_drift, 1e-8)
        self.assertLess(momentum_drift, 1e-8)
        self.assertLess(orbital.norm([a-b for a,b in zip(rows[-1][1:4],initial[:3])]), 0.01)

    def test_eccentric_inclined_analytic_kepler_and_convergence(self):
        eccentricity, inclination = 0.05, math.radians(51.6)
        semimajor = orbital.RADIUS_KM/(1-eccentricity)
        duration = 2*math.pi*math.sqrt(semimajor**3/orbital.MU_KM3_S2)
        perigee_speed = math.sqrt(orbital.MU_KM3_S2*(1+eccentricity)/orbital.RADIUS_KM)
        initial = (orbital.RADIUS_KM, 0., 0., 0., perigee_speed*math.cos(inclination), perigee_speed*math.sin(inclination))
        # At one Keplerian period the exact eccentric state returns to initial.
        coarse = orbital.propagate(duration, 30., initial)
        fine = orbital.propagate(duration, 15., initial)
        a = orbital.norm([x-y for x,y in zip(coarse[-1][1:4],initial[:3])])
        b = orbital.norm([x-y for x,y in zip(fine[-1][1:4],initial[:3])])
        self.assertLess(b, .01)
        self.assertGreater(a/b, 8.)
        self.assertLess(a/b, 32.)
        expected_energy = -orbital.MU_KM3_S2/(2*semimajor)
        self.assertAlmostEqual(orbital.specific_energy(initial), expected_energy, delta=1e-12)
        self.assertLess(max(abs((orbital.specific_energy(row[1:7])-expected_energy)/expected_energy) for row in fine),1e-8)


if __name__ == "__main__":
    unittest.main()
