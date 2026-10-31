"""Regression contract for Second Brain delimiters (actual behavior tested in C# smoke tests)."""
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]


class NoteContextContracts(unittest.TestCase):
    def test_trusted_note_markers_survive_sanitization(self):
        code = (ROOT / "src/Astra.Core/SecondBrain/NoteIndex.cs").read_text(encoding="utf-8")
        self.assertIn('var header = $"[NOTA:', code)
        self.assertIn('const string footer = "\\n[/NOTA]\\n";', code)
        self.assertIn('return result.ToString();', code)
        self.assertNotIn('return Sanitize(result.ToString()', code)
        self.assertIn('StringComparison.OrdinalIgnoreCase', code)
        self.assertIn('maxContextChars - result.Length - header.Length - footer.Length', code)

    def test_compiled_smoke_suite_has_adversarial_cases(self):
        code = (ROOT / "tests/Astra.Core.SmokeTests/Program.cs").read_text(encoding="utf-8")
        for contract in (
            'Contesto marcato come dato',
            'I delimitatori del contesto possono essere creati solo dal programma',
            'I delimitatori e gli header non attendibili sono neutralizzati',
            'Il contesto rimane limitato senza tagliare i delimitatori',
        ):
            self.assertIn(contract, code)


if __name__ == "__main__":
    unittest.main()
