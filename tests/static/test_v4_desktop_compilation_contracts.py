"""Static regression checks for WinUI V4 C# compiler issues (not a substitute for MSBuild)."""
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DESKTOP = ROOT / "src" / "Astra.Desktop"

class V4DesktopCompilationContracts(unittest.TestCase):
    def test_no_unsupported_colors_reference(self):
        for filename in ("MainWindow.xaml.cs", "MainWindow.V4.cs"):
            source = (DESKTOP / filename).read_text(encoding="utf-8")
            self.assertNotIn("Colors.Transparent", source)
            self.assertIn("Color.FromArgb(0, 0, 0, 0)", source)

    def test_note_search_uses_one_scoped_button(self):
        source = (DESKTOP / "MainWindow.Extensions.cs").read_text(encoding="utf-8")
        method = source.split("private async void SearchNotes_Click", 1)[1].split(
            "private void RefreshNotes_Click", 1)[0]
        self.assertIn("var searchButton = sender as Button;", method)
        self.assertIn("finally { if (searchButton is not null) searchButton.IsEnabled = true; }", method)
        self.assertNotIn("sender is Button button", method)

    def test_banner_stop_requires_animation_object(self):
        source = (DESKTOP / "MainWindow.V4.cs").read_text(encoding="utf-8")
        self.assertIn("Vector3KeyFrameAnimation? _v4BannerAnimation", source)
        self.assertIn("BannerArtworkImage.StopAnimation(_v4BannerAnimation)", source)
        self.assertNotIn('StopAnimation("Translation")', source)

    def test_banner_start_tracks_animation(self):
        source = (DESKTOP / "MainWindow.V4.cs").read_text(encoding="utf-8")
        self.assertIn("BannerArtworkImage.StartAnimation(animation);", source)
        self.assertIn("_v4BannerAnimation = animation;", source)
        self.assertIn("_v4BannerAnimation = null;", source)

if __name__ == "__main__":
    unittest.main()
