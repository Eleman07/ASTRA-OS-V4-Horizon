"""V3 offline source contracts; never represented as a Windows integration test."""
import json
import subprocess
import sys
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

class V3Contracts(unittest.TestCase):
    def read(self, rel):
        return (ROOT / rel).read_text(encoding='utf8')

    def test_original_wsl_multiline_bug_fixed(self):
        code = self.read('src/Astra.Desktop/WslInspector.cs')
        self.assertIn('return "Distribuzioni WSL rilevate:\\n"', code)
        result = subprocess.run([sys.executable, str(ROOT/'scripts/lint_cs_strings.py')],capture_output=True,text=True,timeout=20)
        self.assertEqual(result.returncode,0,result.stdout+result.stderr)

    def test_v3_search_navigation_and_focus_persist(self):
        nav=self.read('src/Astra.Core/NavigationCatalog.cs')
        model=self.read('src/Astra.Core/Models.cs')
        ui=self.read('src/Astra.Desktop/MainWindow.xaml')
        handlers=self.read('src/Astra.Desktop/MainWindow.HorizonUbuntu.cs')
        self.assertIn('public static string? Resolve', nav)
        self.assertIn('focusMode', model)
        self.assertIn('FocusModeButton', ui)
        self.assertIn('ApplyFocusMode', handlers)
        self.assertIn('SearchSuggestions', ui)

    def test_brain_privacy_and_search_offline(self):
        src=self.read('src/Astra.Desktop/MainWindow.Extensions.cs')
        index=self.read('src/Astra.Core/SecondBrain/NoteIndex.cs')
        encrypt=self.read('src/Astra.Core/SecondBrain/EncryptedNoteStore.cs')
        ai=self.read('src/Astra.Desktop/Ai/LocalLlmSession.cs')
        self.assertIn('SearchNotes_Click',src)
        self.assertIn('RetrievePassages',src)
        self.assertIn('BuildContext',index)
        self.assertIn('DataProtectionScope.CurrentUser',encrypt)
        self.assertIn('127.0.0.1',ai)
        self.assertIn('FileAttributes.ReparsePoint',ai)
        self.assertNotIn('api.openai.com',ai)

    def test_no_native_plugin_execution_from_ui(self):
        xaml=self.read('src/Astra.Desktop/MainWindow.xaml')
        logic=self.read('src/Astra.Desktop/MainWindow.Extensions.cs')
        self.assertIn('x:Name="NativePluginToggle" IsEnabled="False"',xaml)
        self.assertIn('esecuzione di plugin nativi non disponibile',logic)
        self.assertNotIn('_plugins.ExecuteAsync(',logic)
        ops=self.read('src/Astra.Core/DeclarativePlugins.cs')
        for op in ('text.wordcount','text.todos','text.outline','text.numberlines'):
            self.assertIn(op,ops)
        for module in ('stem.outline','creator.todos','core.words','gaming.lines'):
            name = 'org.astra.' + module
            file=ROOT/'sdk/declarative'/name/'recipe.json'
            recipe=json.loads(file.read_text())
            self.assertEqual(recipe['schemaVersion'],3)
            self.assertEqual(recipe['id'],name)

    def test_3d_csp_eco_render_and_iso_builder(self):
        html=self.read('src/Astra.Desktop/Graphics3D/web/index.html')
        js=self.read('src/Astra.Desktop/Graphics3D/web/scene.js')+self.read('src/Astra.Desktop/Graphics3D/web/renderer.js')
        builder=self.read('scripts/New-AstraIso.ps1')
        self.assertIn("connect-src 'none'",html)
        self.assertIn('requestAnimationFrame',js)
        self.assertIn('clearTimeout(this.timer)',js)
        self.assertIn('webglcontextlost',js)
        self.assertNotIn('fetch(',js)
        self.assertIn('Collegamento/symlink non autorizzato nella sorgente',builder)
        self.assertIn('if (-not $Build)',builder)

    def test_release_stays_blocked_without_windows_evidence(self):
        r = subprocess.run([sys.executable,str(ROOT/'scripts/release_gate.py'),'--expect-blocked'],capture_output=True,text=True,timeout=10)
        self.assertEqual(r.returncode,0,r.stdout+r.stderr)
        self.assertIn('BLOCKED',r.stdout)

if __name__=='__main__':unittest.main()
