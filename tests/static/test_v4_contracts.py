"""V4 implementation contracts. These are source/asset checks, not Windows runtime QA."""
import re
import unittest
import subprocess
import sys
from pathlib import Path
from xml.etree import ElementTree
import struct
import zlib
import tempfile

R = Path(__file__).resolve().parents[2]


def validate_png_without_external_packages(path: Path) -> tuple[int, int]:
    """Check PNG structure, per-chunk CRC and compressed pixel stream.

    Designed for committed UI assets; no dependency on Pillow in the CI runner.
    """
    data = path.read_bytes()
    if not data.startswith(b"\x89PNG\r\n\x1a\n"):
        raise ValueError(f"{path.name}: PNG signature not found")
    offset, dimensions, image_data = 8, None, bytearray()
    saw_ihdr = saw_iend = False
    while offset < len(data):
        if offset + 12 > len(data):
            raise ValueError(f"{path.name}: truncated PNG chunk header")
        length = struct.unpack_from(">I", data, offset)[0]
        chunk_type = data[offset + 4:offset + 8]
        end = offset + 12 + length
        if end > len(data):
            raise ValueError(f"{path.name}: truncated {chunk_type!r} chunk")
        body = data[offset + 8:offset + 8 + length]
        expected_crc = struct.unpack_from(">I", data, end - 4)[0]
        actual_crc = zlib.crc32(chunk_type + body) & 0xffffffff
        if actual_crc != expected_crc:
            raise ValueError(f"{path.name}: CRC failure in {chunk_type!r}")
        if chunk_type == b"IHDR":
            if saw_ihdr or offset != 8 or length != 13:
                raise ValueError(f"{path.name}: invalid IHDR")
            width, height, depth, color, compression, filtering, interlace = struct.unpack(">IIBBBBB", body)
            if width <= 0 or height <= 0 or (depth, color, compression, filtering, interlace) != (8, 2, 0, 0, 0):
                raise ValueError(f"{path.name}: unexpected PNG image format")
            # All ASTRA V4 assets are un-interlaced RGB8.
            if width * height * 3 > 64 * 1024 * 1024:
                raise ValueError(f"{path.name}: unexpected asset dimensions")
            dimensions = (width, height)
            saw_ihdr = True
        elif chunk_type == b"IDAT":
            if not saw_ihdr:
                raise ValueError(f"{path.name}: IDAT precedes IHDR")
            image_data.extend(body)
        elif chunk_type == b"IEND":
            if length != 0 or end != len(data):
                raise ValueError(f"{path.name}: invalid IEND or trailing data")
            saw_iend = True
        offset = end
    if not (saw_ihdr and saw_iend and image_data):
        raise ValueError(f"{path.name}: incomplete PNG")
    decomp = zlib.decompressobj()
    expected_bytes = dimensions[1] * (1 + 3 * dimensions[0])
    decoded = decomp.decompress(bytes(image_data), expected_bytes + 1)
    if len(decoded) != expected_bytes or not decomp.eof or decomp.unused_data or decomp.unconsumed_tail:
        raise ValueError(f"{path.name}: incomplete or inconsistent pixel stream")
    return dimensions

UI = R / 'src/Astra.Desktop/MainWindow.xaml'
APP = R / 'src/Astra.Desktop/MainWindow.V4.cs'
CORE = R / 'src/Astra.Core'

class V4Contracts(unittest.TestCase):
    def test_visual_tokens_match_uploaded_reference(self):
        palette = (CORE/'AstraPalette.cs').read_text()
        ui = UI.read_text()
        for value in ('#0A1931','#B3CFE5','#4A7FA7','#1A3D63','#F6FAFD'):
            self.assertIn(value, palette)
            self.assertIn(value, ui)
        self.assertIn('V4ResetTheme_Click', ui)
        self.assertIn('V4ApplyTheme_Click', ui)
        self.assertIn('ApplyV4Palette', APP.read_text())
        self.assertIn('StaticResource AstraIce', ui)

    def test_layout_matches_semantics_of_reference_design(self):
        x = UI.read_text()
        for word in ('HomePanel','Security Center','Second Brain','Orbital 3D','Ubuntu Lab','Gaming','STEM','Creator','System Performance','Productivity','Tasks','V4Clock','HorizonDock','HorizonSearchBox','BannerArtworkImage'):
            self.assertIn(word,x)
        tree=ElementTree.parse(UI)
        key='{http://schemas.microsoft.com/winfx/2006/xaml}Name'
        names=[e.get(key) for e in tree.iter() if e.get(key) is not None]
        self.assertEqual(len(names),len(set(names)),set(n for n in names if names.count(n)>1))
        self.assertIn('Ctrl + K', x)

    def test_banner_native_compositor_and_ecological_controls(self):
        code=APP.read_text()
        for expected in ('CreateVector3KeyFrameAnimation','BannerArtworkImage.StartAnimation',
            'BannerArtworkImage.StopAnimation','_preferences.ReducedMotion',
            '_preferences.EcoMode','WindowActivationState.Deactivated',
            '_v4LastPage != "home"','V4StopBanner'):
            self.assertIn(expected,code)
        self.assertNotIn('Thread.Sleep(',code)

    def test_tasks_dpapi_instead_of_plaintext(self):
        model=(CORE/'Models.cs').read_text()
        code=(CORE/'EncryptedTaskStore.cs').read_text()
        ui=APP.read_text()
        self.assertNotIn('public List<AstraTaskItem> Tasks',model)
        self.assertIn('ProtectedData.Protect',code)
        self.assertIn('ProtectedData.Unprotect',code)
        self.assertIn('DataProtectionScope.CurrentUser',code)
        self.assertIn('_v4TaskStore.Save(_v4Tasks)',ui)
        self.assertIn('V4RemoveTask_Click',ui)
        self.assertIn('V4RenderActivityChart',ui)

    def test_no_fake_performance_or_false_security(self):
        x=UI.read_text()
        cs=APP.read_text()
        self.assertIn('Non disponibile',x)
        monitor=(R/'src/Astra.Desktop/SystemMonitor.cs').read_text()
        ui_monitor=(R/'src/Astra.Desktop/MainWindow.Productivity.cs').read_text()
        self.assertIn('GetSystemTimes',monitor)
        self.assertIn('GlobalMemoryStatusEx',monitor)
        self.assertIn('RefreshLiveMetricsAsync(force: true)',cs)
        self.assertIn('sample.CpuPercent',ui_monitor)
        self.assertNotIn('Set-MpPreference',cs)

    def test_assets_and_offline_integrity(self):
        for name in ('banner_art_v4.png','wallpaper_v4.png'):
            path=R/'src/Astra.Desktop/Assets'/name
            self.assertTrue(path.is_file())
            width, height = validate_png_without_external_packages(path)
            self.assertGreater(width, 0)
            self.assertGreater(height, 0)
        proj=(R/'src/Astra.Desktop/Astra.Desktop.csproj').read_text()
        self.assertIn('Assets\\*.png',proj)
        self.assertIn('Version="2.5.1"',proj)

    def test_png_integrity_rejects_corrupted_asset(self):
        original = bytearray((R/'src/Astra.Desktop/Assets/banner_art_v4.png').read_bytes())
        original[45] ^= 0x01  # corrupt an IDAT byte without adjusting the CRC
        with tempfile.TemporaryDirectory() as tmp:
            corrupted = Path(tmp) / 'corrupted.png'
            corrupted.write_bytes(original)
            with self.assertRaisesRegex(ValueError, 'CRC failure'):
                validate_png_without_external_packages(corrupted)

    def test_previous_modules_maintained(self):
        x=UI.read_text()
        for name in ('BrainPanel','PluginsPanel','OrbitPanel','UbuntuPanel','IsoPanel','ProfilesPanel','CatalogPanel','DiagnosticPanel','SecurityPanel'):
            self.assertIn(f'x:Name="{name}"',x)
        self.assertIn('IsEnabled="False"',x)  # native plugins still disabled

    def test_ci_stable_gate_still_fails_closed(self):
        result=subprocess.run([sys.executable,str(R/'scripts/release_gate.py'),'--expect-blocked'],capture_output=True,text=True,timeout=20)
        self.assertEqual(result.returncode,0,result.stdout+result.stderr)
        self.assertIn('BLOCKED',result.stdout)

if __name__ == '__main__':unittest.main()
