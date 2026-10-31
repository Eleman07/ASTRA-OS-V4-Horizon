"""Source regression checks for WSL and new WinUI navigation (not runtime tests)."""
import re
import unittest
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]

class UbuntuIntegrationTests(unittest.TestCase):
    def test_wsl_explicit_opt_in(self):
        source=(ROOT/'src/Astra.Desktop/MainWindow.HorizonUbuntu.cs').read_text(encoding='utf-8')
        source += (ROOT/'src/Astra.Desktop/WslInspector.cs').read_text(encoding='utf-8')
        self.assertRegex(source,r'Title = (?:AstraOS\.Core\.UiText\.T\()?"Installa Ubuntu tramite WSL 2\?"')
        self.assertIn('if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;',source)
        self.assertIn('new[] { "--install", "-d", "Ubuntu" }',source)
        self.assertIn('if (admin) psi.Verb = "runas";',source)
        self.assertIn('UseShellExecute = false', source)
        self.assertIn('CancelAfter(TimeSpan.FromSeconds(10))',source)
        self.assertNotIn('cmd.exe', source)
        self.assertNotIn('ExecutionPolicy Bypass', source)

    def test_ubuntu_no_automated_apt(self):
        source=(ROOT/'src/Astra.Desktop/MainWindow.HorizonUbuntu.cs').read_text(encoding='utf-8')
        self.assertIn('CopyLiteral("sudo apt update',source)
        self.assertNotIn('wsl", "sudo',source)
        self.assertNotIn('curl -s',source)

    def test_mac_style_nav_and_preferences(self):
        xaml=(ROOT/'src/Astra.Desktop/MainWindow.xaml').read_text(encoding='utf-8')
        self.assertIn('x:Name="HorizonDock"',xaml)
        self.assertIn('x:Name="HorizonSearchBox"',xaml)
        self.assertIn('x:Name="UbuntuPanel"',xaml)
        self.assertIn('x:Name="DockVisibilityToggle"',xaml)
        self.assertIn('UbuntuNav_Click',xaml)
        self.assertIn('KeyDown="HorizonSearchBox_KeyDown"',xaml)
        self.assertIn('Nessun kernel Windows viene sostituito'.lower(), xaml.lower())
        self.assertNotIn('Explorer.exe /kill',xaml)

    def test_security_not_disabled(self):
        cs=(ROOT/'src/Astra.Desktop/SecurityInspector.cs').read_text(encoding='utf-8')
        self.assertIn('Get-MpComputerStatus',cs)
        self.assertIn('Get-NetFirewallProfile',cs)
        self.assertIn('Confirm-SecureBootUEFI',cs)
        self.assertIn('Get-BitLockerVolume',cs)
        self.assertNotIn('Set-MpPreference',cs)
        self.assertNotIn('Set-NetFirewallProfile',cs)

if __name__=='__main__': unittest.main()
