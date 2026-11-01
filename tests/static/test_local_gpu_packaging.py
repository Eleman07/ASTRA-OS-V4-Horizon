"""Release-policy corruption tests; no app, model or installer is executed."""
import importlib.util
from pathlib import Path
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("local_gpu_payload", ROOT/"scripts/local_gpu_payload.py")
policy = importlib.util.module_from_spec(spec)
spec.loader.exec_module(policy)


class LocalGpuPackagingPolicy(unittest.TestCase):
    def fixture(self):
        (ROOT/"dist").mkdir(exist_ok=True)
        fixture = tempfile.TemporaryDirectory(prefix="package-policy-", dir=ROOT/"dist")
        self.assertTrue(Path(fixture.name).resolve().is_relative_to((ROOT/"dist").resolve()))
        self.addCleanup(fixture.cleanup)
        return Path(fixture.name)

    def test_arbitrary_text_weights_cannot_enter_the_release(self):
        directory = self.fixture()
        (directory/"a-text-model.gguf").write_bytes(b"GGUFnot-a-real-model")
        with self.assertRaisesRegex(ValueError, "Unapproved/text model"):
            policy.inventory(directory)

    def test_allowed_filename_still_requires_pinned_model_bytes(self):
        directory = self.fixture()
        files = {}
        for relative in policy.AUXILIARY_WEIGHTS:
            path = directory/relative
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(b"corrupt fixture")
            files[relative] = path
        with self.assertRaisesRegex(ValueError, "Unapproved auxiliary model bytes"):
            policy.verify_auxiliary_weights(files)

    def test_private_vault_and_credential_patterns_are_rejected(self):
        vault = self.fixture()
        (vault/".obsidian").mkdir()
        with self.assertRaisesRegex(ValueError, "Private/cache"):
            policy.inventory(vault)
        secret = self.fixture()
        (secret/"settings.json").write_text('"'+"sk-or-v1-"+"x"*32+'"', encoding="utf8")
        with self.assertRaisesRegex(ValueError, "API credential pattern"):
            policy.inventory(secret)

    def test_obsolete_text_model_license_is_rejected(self):
        directory = self.fixture()
        (directory/"Ai/components").mkdir(parents=True)
        (directory/"Ai/components/LICENSE-model.txt").write_text("obsolete model notice", encoding="utf8")
        with self.assertRaisesRegex(ValueError, "Obsolete bundled text-model license"):
            policy.inventory(directory)

    def test_package_paths_cannot_escape_or_collide(self):
        for name in ("../outside", "/rooted", "C:/rooted", "a//b", "a/./b", "a\x00b"):
            with self.subTest(name=name), self.assertRaises(ValueError):
                policy.safe_name(name)


if __name__ == "__main__":
    unittest.main()
