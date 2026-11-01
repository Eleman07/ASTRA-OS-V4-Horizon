"""Regression tests for source gates; no app, model or provider is executed."""
import importlib.util
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('astra_source_validation', ROOT/'scripts/validate_source.py')
validation = importlib.util.module_from_spec(spec)
spec.loader.exec_module(validation)


class AssistantSourceContracts(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.baseline = validation.read_assistant_contract_sources(ROOT)

    def rejected(self, code, phrase):
        checks = validation.assistant_contract_checks(code)
        matching = [passed for description, passed in checks.items() if phrase in description]
        self.assertEqual(len(matching), 1, phrase)
        self.assertFalse(matching[0], phrase)

    def mutated(self, key, before, after):
        code = dict(self.baseline)
        self.assertIn(before, code[key], 'Mutation must exercise the actual source contract')
        code[key] = code[key].replace(before, after, 1)
        return code

    def test_current_source_transport_selection_and_origin_contracts(self):
        checks = validation.assistant_contract_checks(self.baseline)
        self.assertGreaterEqual(len(checks), 10)
        self.assertTrue(all(checks.values()), [description for description, passed in checks.items() if not passed])

    def test_a_reintroduced_response_byte_cap_fails_validation(self):
        code = self.mutated('session', 'return answer;', 'return answer[..Math.Min(answer.Length, 256_000)];')
        self.rejected(code, 'response parser consumes the full stream')

    def test_input_clipping_cannot_be_hidden_behind_full_output_parsing(self):
        code = self.mutated('session', 'await _taskGate.WaitAsync(ct);', 'input = input[..4000]; await _taskGate.WaitAsync(ct);')
        self.rejected(code, 'task input and complete agent handoffs')
        code = self.mutated('session', '_history.Add(("user", question))', '_history.Clear(); _history.Add(("user", question))')
        self.rejected(code, 'chat history retains complete turns')

    def test_local_handoffs_cannot_revert_to_the_old_bounded_builder(self):
        code = self.mutated('engine', '=> BuildCompleteInput(job, index)', '=> job.Goal[..1000]')
        self.rejected(code, 'task input and complete agent handoffs')
        code = self.mutated('engine', 'PromptData.Quote(e.Text)', 'PromptData.Quote(e.Text[..4000])')
        self.rejected(code, 'task input and complete agent handoffs')

    def test_cloud_continuation_cannot_drop_previous_response_parts(self):
        code = self.mutated('session', 'output.Append(part)', 'output.Clear().Append(part)')
        self.rejected(code, 'continuation accumulates every returned part')

    def test_every_settings_save_cannot_force_the_provider_online(self):
        code = self.mutated('preferences', 'settings.AiProvider = settings.AiProvider?.Trim().ToLowerInvariant() switch', 'settings.AiProvider = "openrouter"; settings.AiProvider = settings.AiProvider?.Trim().ToLowerInvariant() switch')
        self.rejected(code, 'provider and explicit selected paths survive')

    def test_missing_provider_cannot_default_to_online_or_cpu(self):
        code = self.mutated('models', 'public string AiProvider { get; set; } = "local";', 'public string AiProvider { get; set; } = "openrouter";')
        self.rejected(code, 'provider and explicit selected paths survive')
        code = self.mutated('runtime', 'public bool UseGpu { get; set; } = true;', 'public bool UseGpu { get; set; } = false;')
        self.rejected(code, 'local GPU is the default')

    def test_unselected_model_path_cannot_autofill_a_bundled_checkpoint(self):
        code = self.mutated('preferences', 'settings.AiModelPath = SanitizeLocalPath(settings.AiModelPath);', 'settings.AiModelPath = BundledAi.ModelPath;')
        self.rejected(code, 'provider and explicit selected paths survive')

    def test_unknown_or_chinese_text_weights_cannot_enter_the_curated_catalog(self):
        code = self.mutated('catalog', 'mistralai/Ministral-3-14B-Reasoning-2512-GGUF', 'qwen/Qwen-Fake-GGUF')
        self.rejected(code, 'local text model selection is curated')
        code = self.mutated('session', 'LocalModelCatalog.VerifyTextModelAsync(model,ct)', 'LocalModelCatalog.BypassVerification(model,ct)')
        self.rejected(code, 'local text model selection is curated')

    def test_optional_cloud_fallback_cannot_skip_origin_or_price_checks(self):
        code = self.mutated('router', 'ModelOriginPolicy.IsAllowedCloudModel(id)', 'true')
        self.rejected(code, 'optional cloud routes require reviewed origins')
        code = self.mutated('router', 'ZeroPrices(model)', 'true')
        self.rejected(code, 'optional cloud routes require reviewed origins')

    def test_ui_model_selection_and_compiled_persistence_tests_stay_wired(self):
        code = self.mutated('local_ui', 'SaveAssistantRuntimeSettings(c)', 'RefreshAssistantRuntimeUi()')
        # Other save calls exist, so remove all calls for this source regression.
        code['local_ui'] = code['local_ui'].replace('SaveAssistantRuntimeSettings(c)', 'RefreshAssistantRuntimeUi()')
        self.rejected(code, 'both assistant settings surfaces persist')
        code = self.mutated('core_tests', 'PreferenceProviderTests.Run(temporary, Check)', '/* persistence tests omitted */')
        self.rejected(code, 'compiled preference persistence regression tests remain wired')


if __name__ == '__main__':
    unittest.main()
