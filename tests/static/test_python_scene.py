import importlib.util
import json
import tempfile
import unittest
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location('astra_scene',ROOT/'sdk/python/astra_scene.py')
sdk=importlib.util.module_from_spec(spec);spec.loader.exec_module(sdk)

class PythonBridge(unittest.TestCase):
    def test_roundtrip_and_atomic_replace(self):
        with tempfile.TemporaryDirectory(prefix='astra-python-') as folder:
            path=Path(folder)/'scene.json'
            sdk.Scene('Test',axes=('m','s','K')).line('Trajectory',((i,i*i,0) for i in range(4))).save(path)
            data=json.loads(path.read_text());self.assertEqual(data['axes']['z'],'K');self.assertEqual(len(data['series'][0]['points']),4)
            sdk.Scene('Replacement').points('P',[(0,0,0)]).save(path)
            self.assertEqual(json.loads(path.read_text())['title'],'Replacement');self.assertEqual(len(list(Path(folder).iterdir())),1)
    def test_reject_bad_geometry(self):
        for points in ([(0,0,float('nan'))],[(0,0,float('inf'))],[(0,0,1e101)],[(0,1)]):
            with self.assertRaises(ValueError):sdk.Scene().points('bad',points)
        with self.assertRaises(ValueError):sdk.Scene().line('short',[(0,0,0)])
        with self.assertRaises(ValueError):sdk.Scene().surface('bad',[0,1],[0,1],[[1,2]])
    def test_point_budget_across_series(self):
        scene=sdk.Scene().points('Full',((i,0,0) for i in range(50000)))
        with self.assertRaises(ValueError):scene.points('Overflow',[(0,0,0)])
    def test_examples_are_valid_schema(self):
        for file in (ROOT/'sdk/python/examples-data').glob('*.json'):
            data=json.loads(file.read_text());self.assertEqual(data['schemaVersion'],1)
            for series in data['series']:
                if series['kind']=='surface':self.assertEqual(series['rows']*series['columns'],len(series['points']))
