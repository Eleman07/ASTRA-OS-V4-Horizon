"""Static checks for local Windows build additions (not a substitute for a Windows compile)."""
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DESKTOP = ROOT / 'src' / 'Astra.Desktop'

class LocalWindowsBuildContracts(unittest.TestCase):
    def test_project_publish_options_are_safe(self):
        csproj = (DESKTOP / 'Astra.Desktop.csproj').read_text(encoding='utf-8')
        for property_text in (
            '<TargetFramework>net10.0-windows10.0.22621.0</TargetFramework>',
            '<WindowsPackageType>None</WindowsPackageType>',
            '<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>',
            '<PublishTrimmed>false</PublishTrimmed>',
            '<PublishSingleFile>false</PublishSingleFile>',
            '<PublishReadyToRun>false</PublishReadyToRun>',
        ):
            self.assertIn(property_text, csproj)

    def test_diagnostics_preserve_original_exception(self):
        app = (DESKTOP / 'App.xaml.cs').read_text(encoding='utf-8')
        self.assertIn('StartupDiagnostics.Record("MainWindow.create.begin")', app)
        self.assertIn('StartupDiagnostics.Record("MainWindow.launch.failed", ex.ToString())', app)
        self.assertIn('throw;', app)
        self.assertTrue((DESKTOP / 'StartupDiagnostics.cs').exists())

    def test_local_build_and_guide_exist(self):
        ps1 = (ROOT / 'scripts' / 'build-local.ps1').read_text(encoding='utf-8')
        self.assertIn('dotnet publish', ps1)
        self.assertIn('PublishTrimmed=false', ps1)
        self.assertIn('AstraOS-Local', ps1)
        self.assertTrue((ROOT / 'GUIDA-COMPILAZIONE-LOCALE-WINDOWS.md').exists())

if __name__ == '__main__':
    unittest.main()
