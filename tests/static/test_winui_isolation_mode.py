from pathlib import Path
import unittest
ROOT=Path(__file__).resolve().parents[2]
class TestWinUiIsolation(unittest.TestCase):
    def test_diagnostic_switch_keeps_full_default(self):
        s=(ROOT/'src/Astra.Desktop/App.xaml.cs').read_text(encoding='utf-8')
        self.assertIn('ASTRA_DIAGNOSTIC_MINIMAL',s)
        self.assertIn('_mainWindow = new MainWindow()',s)
        self.assertIn('MinimalWindow.activate.complete',s)
    def test_xaml_load_boundaries(self):
        s=(ROOT/'src/Astra.Desktop/MainWindow.xaml.cs').read_text(encoding='utf-8')
        self.assertIn('MainWindow.Xaml.InitializeComponent.begin',s)
        self.assertIn('MainWindow.Xaml.InitializeComponent.complete',s)
    def test_scripts_exist(self):
        self.assertTrue((ROOT/'scripts/diagnose-winui.ps1').is_file())
if __name__=='__main__':unittest.main()
