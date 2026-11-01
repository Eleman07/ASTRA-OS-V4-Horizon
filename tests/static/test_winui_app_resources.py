"""Check the critical WinUI application resource dictionary contract."""
from pathlib import Path
import unittest
from xml.etree import ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
APP = ROOT / "src" / "Astra.Desktop" / "App.xaml"
CSPROJ = ROOT / "src" / "Astra.Desktop" / "Astra.Desktop.csproj"

class WinUIAppResourceTests(unittest.TestCase):
    def test_default_control_resources_loaded(self):
        root = ET.parse(APP).getroot()
        ns = {"p": "http://schemas.microsoft.com/winfx/2006/xaml/presentation"}
        merged = root.find("p:Application.Resources/p:ResourceDictionary/p:ResourceDictionary.MergedDictionaries", ns)
        self.assertIsNotNone(merged, "Default resource dictionary is missing")
        self.assertTrue(len(merged) > 0)
        self.assertEqual(merged[0].tag, "{using:Microsoft.UI.Xaml.Controls}XamlControlsResources")

    def test_main_xaml_is_well_formed(self):
        ET.parse(ROOT / "src" / "Astra.Desktop" / "MainWindow.xaml")

    def test_sdk_and_unpacked_options_preserved(self):
        p = CSPROJ.read_text(encoding="utf-8")
        self.assertIn('<WindowsPackageType>None</WindowsPackageType>', p)
        self.assertIn('<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>', p)
        self.assertIn('<PublishTrimmed>false</PublishTrimmed>', p)

    def test_no_early_application_resources_access(self):
        s = (ROOT / "src" / "Astra.Desktop" / "App.xaml.cs").read_text(encoding="utf-8")
        ctor = s.split("public App()", 1)[1].split("protected override void OnLaunched", 1)[0]
        self.assertNotIn("Resources.MergedDictionaries", ctor)
        self.assertNotIn("Resources[", ctor)
        self.assertIn('StartupDiagnostics.Record("App.InitializeComponent.complete")', ctor)

if __name__ == "__main__":
    unittest.main()
