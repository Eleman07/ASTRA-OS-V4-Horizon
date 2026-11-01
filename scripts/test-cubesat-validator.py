"""Positive and corruption tests for the orbital artifact validator; stdlib only."""
import csv
import importlib.util
import json
from pathlib import Path
import shutil
import subprocess
import sys
import unittest
import uuid

ROOT=Path(__file__).resolve().parents[1]
spec=importlib.util.spec_from_file_location("cubesat_validator",ROOT/"scripts/verify-cubesat-deliverable.py")
validator=importlib.util.module_from_spec(spec)
spec.loader.exec_module(validator)


class ValidatorAcceptanceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.stage=ROOT/"dist/cubesat-validator-tests"/uuid.uuid4().hex
        cls.baseline=cls.stage/"baseline"
        shutil.copytree(validator.TEMPLATE,cls.baseline)
        (cls.baseline/"fonti_vault.md").write_text("# Fonti del vault\n\nUNVERIFIED: nessuna fonte recuperata; fixture sintetica del validator.\n",encoding="utf8")
        result=subprocess.run([sys.executable,"-I","-S","-B",str(cls.baseline/"run_project.py"),"--output",str(cls.baseline/"results"),"--duration-orbits","1"],capture_output=True,text=True,timeout=120)
        if result.returncode:
            raise RuntimeError(result.stdout+result.stderr)

    def fixture(self):
        target=self.stage/uuid.uuid4().hex
        shutil.copytree(self.baseline,target)
        return target

    def assert_check_rejected(self,project,phrase):
        report=validator.validate(project)
        self.assertFalse(report["passed"])
        found=[item for item in report["checks"] if phrase in item["name"]]
        self.assertTrue(found,phrase)
        self.assertFalse(found[0]["passed"],found[0])

    def test_real_template_outputs_pass_without_reexecution(self):
        report=validator.validate(self.baseline)
        self.assertTrue(report["passed"],[item for item in report["checks"] if not item["passed"]])
        self.assertFalse(report["projectCodeExecutedByValidator"])
        self.assertFalse(report["executionReceiptIndependentlyAuthenticated"])

    def test_energy_corruption_rejected_even_with_claimed_pass(self):
        project=self.fixture()
        path=project/"results/trajectory.csv"
        with path.open(newline="",encoding="utf8") as stream:
            rows=list(csv.reader(stream))
        rows[40][4]=str(float(rows[40][4])*1.1)
        rows[40][5]=str(float(rows[40][5])*1.1)
        with path.open("w",newline="",encoding="utf8") as stream:
            csv.writer(stream).writerows(rows)
        self.assert_check_rejected(project,"energy conservation")

    def test_duplicated_fine_trajectory_is_not_convergence(self):
        project=self.fixture()
        shutil.copyfile(project/"results/trajectory.csv",project/"results/trajectory-half-step.csv")
        self.assert_check_rejected(project,"actual nominal step is halved")
        self.assert_check_rejected(project,"observed step-halving convergence")

    def test_invented_period_rejected(self):
        project=self.fixture()
        path=project/"results/metrics.json"
        metrics=json.loads(path.read_text())
        metrics["reference"]["periodSeconds"]=6000.0
        path.write_text(json.dumps(metrics))
        self.assert_check_rejected(project,"reported speed/period")

    def test_nonfinite_svg_coordinates_rejected(self):
        project=self.fixture()
        path=project/"results/orbit.svg"
        path.write_text(path.read_text(encoding="utf8").replace('points="','points="nan,2 ',1),encoding="utf8")
        self.assert_check_rejected(project,"finite multi-sample")

    def test_truncated_trajectory_is_not_one_full_orbit(self):
        project=self.fixture()
        for name in ("trajectory.csv","trajectory-half-step.csv"):
            path=project/"results"/name
            path.write_text("\n".join(path.read_text().splitlines()[:20])+"\n")
        self.assert_check_rejected(project,"span at least one full orbit")

    def test_changed_source_invalidates_execution_receipt_and_cannot_run(self):
        project=self.fixture()
        marker=self.stage/"MUST-NOT-EXIST.txt"
        path=project/"orbital.py"
        path.write_text(path.read_text()+"\nfrom pathlib import Path\nPath("+repr(str(marker))+").write_text('executed')\n")
        self.assert_check_rejected(project,"source hashes match")
        self.assertFalse(marker.exists())
        with self.assertRaisesRegex(ValueError,"Execution refused"):
            validator.execute_reviewed(project,"0"*64)
        self.assertFalse(marker.exists())

    def test_independent_reference_has_expected_fourth_order_convergence(self):
        result=validator.independent_reference()
        self.assertEqual(result["kind"],"independent-validator-tool-calculation")
        self.assertAlmostEqual(result["circular"]["speedKmS"],7.668558175407055,places=12)
        self.assertGreater(result["observedOrder"],3.5)
        self.assertLess(result["observedOrder"],4.5)

    def test_arbitrary_csv_names_si_conversion_and_producer_are_supported(self):
        project=self.fixture()
        manifest={"artifacts":{},"producer":"actual-model-output-with-reviewer-mapping","csvColumns":{name:"actual_"+str(index) for index,name in enumerate(validator.FIELDS)},"csvScale":{name:(1 if name=="t_s" else .001) for name in validator.FIELDS}}
        for key,original,target in (("trajectory","trajectory.csv","coarse_model.csv"),("trajectoryHalfStep","trajectory-half-step.csv","fine_model.csv")):
            with (project/"results"/original).open(newline="",encoding="utf8") as stream:
                rows=list(csv.DictReader(stream))
            with (project/target).open("w",newline="",encoding="utf8") as stream:
                writer=csv.writer(stream)
                writer.writerow(manifest["csvColumns"].values())
                writer.writerows([float(row[name])*(1 if name=="t_s" else 1000) for name in validator.FIELDS] for row in rows)
            manifest["artifacts"][key]=target
        (project/"cubesat-manifest.json").write_text(json.dumps(manifest))
        report=validator.validate_numerics(project)
        self.assertTrue(report["passed"],report)
        self.assertFalse(report["fullDeliverableValidated"])
        self.assertIn("UNVERIFIED",report["remoteModelProjectAcceptance"])

    def test_exact_review_fingerprint_changes_with_any_source_edit(self):
        project=self.fixture()
        reviewed=validator.fingerprint(project)
        path=project/"orbital.py"
        path.write_text(path.read_text()+"\n# Changed after review\n")
        self.assertNotEqual(validator.fingerprint(project),reviewed)
        with self.assertRaisesRegex(ValueError,"Execution refused"):
            validator.execute_reviewed(project,reviewed)


if __name__=="__main__":
    unittest.main(verbosity=2)
