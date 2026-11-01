"""Offline regression tests. These do not constitute a WinUI/Windows runtime test."""
import hashlib
import importlib.util
import json
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
loader_spec = importlib.util.spec_from_file_location('astra_release_gate', ROOT / 'scripts/release_gate.py')
gate = importlib.util.module_from_spec(loader_spec)
loader_spec.loader.exec_module(gate)


class StableGateTests(unittest.TestCase):
    def test_default_is_blocked(self):
        okay, errors = gate.evaluate(ROOT / 'docs/evidence/release-evidence.json')
        self.assertFalse(okay)
        self.assertEqual(len(errors), len(gate.REQUIRED))

    def test_passing_manifest_checks_all_hashes(self):
        with tempfile.TemporaryDirectory() as temp:
            folder = Path(temp)
            evidence = folder / 'run.txt'
            evidence.write_text('Windows QA evidence from disposable test fixture (not real QA)', encoding='utf8')
            sha = hashlib.sha256(evidence.read_bytes()).hexdigest()
            manifest = folder / 'manifest.json'
            payload = {'gates': {key: {'status':'passed','artifact':'run.txt','sha256':sha} for key in gate.REQUIRED}}
            manifest.write_text(json.dumps(payload),encoding='utf8')
            self.assertEqual(gate.evaluate(manifest), (True, []))
            evidence.write_text('modified / tampered',encoding='utf8')
            passed, errors = gate.evaluate(manifest)
            self.assertFalse(passed)
            self.assertEqual(len(errors), len(gate.REQUIRED))

    def test_path_traversal_denied(self):
        with tempfile.TemporaryDirectory() as temp:
            folder = Path(temp)
            manifest = folder / 'manifest.json'
            payload = {'gates': {key: {'status':'passed','artifact':'../outside.txt','sha256':'a'*64} for key in gate.REQUIRED}}
            manifest.write_text(json.dumps(payload),encoding='utf8')
            okay, errors = gate.evaluate(manifest)
            self.assertFalse(okay)
            self.assertEqual(len(errors), len(gate.REQUIRED))


class SourceContractsTests(unittest.TestCase):
    def test_plugin_v3_included_in_output(self):
        csproj = (ROOT / 'src/Astra.Desktop/Astra.Desktop.csproj').read_text(encoding='utf8')
        self.assertIn('sdk\\declarative',csproj)
        recipe = json.loads((ROOT / 'sdk/declarative/org.astra.textkit/recipe.json').read_text())
        self.assertEqual(recipe['schemaVersion'], 3)
        self.assertIn(recipe['operation'], ('text.upper','text.lower','text.trim','text.prefix'))

    def test_native_plugins_disabled_at_startup(self):
        xaml = (ROOT / 'src/Astra.Desktop/MainWindow.xaml').read_text(encoding='utf8')
        backend = (ROOT / 'src/Astra.Desktop/MainWindow.Extensions.cs').read_text(encoding='utf8')
        self.assertIn('x:Name="NativePluginToggle"', xaml)
        self.assertIn('IsOn="False" Toggled="NativePluginToggle_Toggled"', xaml)
        self.assertIn('if (!NativePluginToggle.IsOn) return;', backend)
        self.assertIn('esecuzione di plugin nativi non disponibile',backend)
        self.assertIn('IsEnabled="False"',xaml)

    def test_iso_builder_rejects_preexisting_partial(self):
        script=(ROOT / 'scripts/New-AstraIso.ps1').read_text(encoding='utf8')
        self.assertIn('ISO parziale già presente',script)
        self.assertIn('Media contiene un reparse point',script)
        self.assertIn('2 * $required',script)
        self.assertIn('if (-not $Build)',script)

if __name__ == '__main__':
    unittest.main()
