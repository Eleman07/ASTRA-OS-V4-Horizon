"""Offline ASTRA source validation. Not a Windows/.NET compiler or runtime QA."""
from __future__ import annotations
import json, re, xml.etree.ElementTree as ET
from pathlib import Path

repo = Path(__file__).resolve().parents[1]

ASSISTANT_CONTRACT_FILES = {
    'session': 'src/Astra.Desktop/Ai/LocalLlmSession.cs',
    'engine': 'src/Astra.Core/Agents/AgentEngine.cs',
    'preferences': 'src/Astra.Core/PreferenceStore.cs',
    'models': 'src/Astra.Core/Models.cs',
    'runtime': 'src/Astra.Core/Agents/AgentStore.cs',
    'catalog': 'src/Astra.Core/Agents/LocalModelCatalog.cs',
    'local_ui': 'src/Astra.Desktop/MainWindow.LocalModels.cs',
    'legacy_ui': 'src/Astra.Desktop/MainWindow.Extensions.cs',
    'router': 'src/Astra.Core/Agents/FreeModelRouter.cs',
    'origin': 'src/Astra.Core/Agents/ModelOriginPolicy.cs',
    'cloud': 'src/Astra.Core/Agents/FreeCloudCompletion.cs',
    'preference_tests': 'tests/Astra.Core.SmokeTests/PreferenceProviderTests.cs',
    'core_tests': 'tests/Astra.Core.SmokeTests/Program.cs',
}


def read_assistant_contract_sources(root: Path = repo) -> dict[str, str]:
    return {key: (root/path).read_text(encoding='utf8') for key, path in ASSISTANT_CONTRACT_FILES.items()}


def assistant_contract_checks(code: dict[str, str]) -> dict[str, bool]:
    """Source regression checks, complemented by compiled Core behavior tests.

    These inspect transport/configuration paths, not a model's accuracy or a
    runtime sandbox. Negative tests inject the regressions this gate must catch.
    """
    def region(source: str, start: str, end: str) -> str:
        if start not in source or end not in source.split(start, 1)[1]:
            return ''
        return source.split(start, 1)[1].split(end, 1)[0]
    def compact(source: str) -> str:
        return re.sub(r'\s+', '', source)
    def clipped(source: str) -> bool:
        return bool(re.search(r'(?:\b(?:input|question|answer|payload|part|candidate)|\bjob\.(?:Goal|Context)|\bs\.Output|\be\.Text)\s*(?:\[\s*\.\.|\.Substring\s*\(|\.Take\s*\()', source)
            or re.search(r'\b(?:MaxResponseBytes|MaxResponseChars|maxResponseBytes|maxResponseChars|responseByteLimit|responseCharLimit)\b', source)
            or re.search(r'\b(?:buffer|answer|response|payload)\.(?:Length|Count)\s*>\s*[\d_]+', source))
    session = code['session']
    local = region(session, 'private async Task<string> SendLocalAsync(', 'public async Task<string> AskAsync(')
    task = region(session, 'public async Task<string> RunTaskAsync(', 'internal async Task<string> AnalyzeImageAsync(')
    ask = region(session, 'public async Task<string> AskAsync(', 'public async Task<string> RunTaskAsync(')
    complete = region(session, 'private async Task<string> SendCompleteOpenRouterAsync(', 'private async Task<string> SendOpenRouterAsync(')
    remembered = region(session, 'private void Remember(', 'public void Stop(')
    engine = code['engine']
    handoff = region(engine, 'public static string BuildCompleteInput(', 'public static string AppendToolFacts(')
    defaults = code['runtime']
    pref = code['preferences']
    ui, legacy = compact(code['local_ui']), compact(code['legacy_ui'])
    catalog = code['catalog']
    specs = re.findall(r'LocalModelSpec\s+(\w+)\s*=\s*new\("([^"]+)","([^"]+)","([^"]+)",(\d+),"([a-f0-9]{64})","([^"]+)","([a-f0-9]{40})","([^"]+)"\)', catalog)
    text_models = re.search(r'TextModels\s*=\s*\[([^\]]+)\]', catalog)
    selected = [value.strip() for value in text_models[1].split(',')] if text_models else []
    spec_map = {record[0]: record for record in specs}
    curated = bool(selected) and all(name in spec_map and (
        spec_map[name][6].startswith(('mistralai/', 'microsoft/'))
        or spec_map[name][1] == 'gpt-oss' and spec_map[name][6] == 'ggml-org/gpt-oss-20b-GGUF')
        and not re.search(r'qwen|deepseek|glm|kimi', ' '.join(spec_map[name][1:]), re.I) for name in selected)
    return {
        'local response parser consumes the full stream without a fixed response byte cap': bool(local) and 'ReadAsStreamAsync(ct)' in local and 'JsonDocument.ParseAsync(stream' in local and not clipped(local),
        'task input and complete agent handoffs are preserved without source clipping': bool(task) and not clipped(task) and bool(handoff) and 'job.Goal' in handoff and 'job.Context' in handoff and 's.Output' in handoff and 'e.Text' in handoff and '=> BuildCompleteInput(job, index)' in handoff and not clipped(handoff),
        'chat history retains complete turns rather than a fixed tail': bool(ask) and 'foreach (var turn in _history)' in ask and bool(remembered) and '_history.Add(("user", question))' in remembered and '_history.Add(("assistant", answer))' in remembered and not re.search(r'_history\.(?:RemoveRange|RemoveAt|Clear|Take|Skip)', ask+remembered),
        'provider-length continuation accumulates every returned part': bool(complete) and 'output.Append(part)' in complete and 'while(LastTaskTruncated)' in complete and 'return output.ToString()' in complete and not clipped(complete),
        'local generation requests model termination and reports context overflow': 'maxTokens > 0 ? maxTokens : -1' in local and 'maxTokens=0' in compact(task[:task.find('{')]) and '--no-context-shift' in session and 'Input was not shortened' in local and 'contextSize>verifiedModel.NativeContext' in compact(session),
        'local provider and explicit selected paths survive settings persistence': 'public string AiProvider { get; set; } = "local";' in code['models'] and 'settings.AiProvider?.Trim().ToLowerInvariant() switch' in pref and '_ => "local"' in pref and '"openrouter" => "openrouter"' in pref and not re.search(r'settings\.AiProvider\s*=\s*"openrouter"\s*;', pref) and not re.search(r'settings\.Ai(?:Model|Executable)Path\s*=.*(?:BundledAi|Qwen|LocalModelCatalog)', pref),
        'local GPU is the default with no implicit weights or cloud fallback': 'public bool UseGpu { get; set; } = true;' in defaults and 'public bool PreferFreeCloud { get; set; }' in defaults and not re.search(r'PreferFreeCloud\s*\{[^}]+\}\s*=\s*true', defaults) and 'public string ModelPath { get; set; } = "";' in defaults and 'public string RuntimePath { get; set; } = "";' in defaults and 'CPU language-model fallback is disabled' in session and 'offloaded\\s+' in session,
        'local text model selection is curated and SHA256 verified before startup': curated and 'SHA256.HashDataAsync' in catalog and 'VerifyTextModelAsync' in session and session.find('VerifyTextModelAsync') < session.find('Process.Start') and 'c.ModelPath=file;' in ui,
        'both assistant settings surfaces persist the explicit provider and model choice': 'SaveAssistantRuntimeSettings(c)' in ui and 'runtime.UseGpu=!runtime.PreferFreeCloud;' in legacy and 'runtime.FallbackToGpu=false;' in legacy and '_preferences.AiProvider=runtime.UseCodex?"codex":runtime.PreferFreeCloud?"openrouter":"local";' in legacy and '_preferences.AiModelPath=runtime.ModelPath;' in legacy and '_preferenceStore.Save(_preferences);' in legacy,
        'optional cloud routes require reviewed origins and live zero-price compatibility': 'ModelOriginPolicy.IsAllowedCloudModel(model)' in session and 'ModelOriginPolicy.IsAllowedCloudModel(id)' in code['router'] and 'ZeroPrices(model)' in code['router'] and 'Approved.TryGetValue' in code['origin'] and 'ExcludedLineageMarkers' in code['origin'] and 'key.Contains' in code['origin'] and 'FreeModelRouter.Resolve' in code['cloud'] and 'max_price' in code['cloud'] and 'route.MaxOutputTokens' in code['cloud'],
        'compiled preference persistence regression tests remain wired into Core smoke': 'PreferenceProviderTests.Run(temporary, Check)' in code['core_tests'] and 'new PreferenceStore(store.DirectoryPath).Load()' in code['preference_tests'] and 'loaded.AiModelPath == selectedModel' in code['preference_tests'] and 'loaded.OpenRouterModel == selectedCloud' in code['preference_tests'],
    }


def main() -> None:
    run_original_checks()
    for description, passed in assistant_contract_checks(read_assistant_contract_sources()).items():
        assert passed, 'Assistant source contract failed: ' + description
        print('PASS assistant contract', description)
    print('Static source checks: PASS. No claims about runtime compilation, model accuracy or Windows QA.')


def run_original_checks() -> None:
    profiles = repo / 'src/Astra.Desktop/profiles'
    known = ('core','stem','gaming','creator')
    expected = {
        'core': [],
        'stem': ['Git.Git', 'Microsoft.VisualStudioCode', 'Python.Python.3.13', 'Kitware.CMake'],
        'gaming': ['Valve.Steam', 'EpicGames.EpicGamesLauncher', 'Discord.Discord'],
        'creator': ['BlenderFoundation.Blender', 'OBSProject.OBSStudio', 'GIMP.GIMP'],
    }
    for name in known:
        m = json.loads((profiles / f'{name}.json').read_text(encoding='utf8'))
        assert m['schemaVersion'] == 1 and m['id'] == name and m['packages'] == expected[name]
        assert len(set(m['packages'])) == len(m['packages'])
        print('PASS profile',name)
    manifest = json.loads((repo/'sdk/Astra.SamplePlugin/plugin.json').read_text(encoding='utf8'))
    assert manifest['apiVersion'] == 2
    assert manifest['id'] == 'org.astra.sample' and manifest['entryPoint'] == 'Astra.SamplePlugin.exe'
    assert manifest['capabilities'] == ['text.transform']
    assert len(manifest['sha256']) == 64
    print('PASS plugin manifest')
    for f in list(repo.rglob('*.csproj')) + list(repo.rglob('*.xaml')) + list(repo.rglob('*.manifest')):
        if not f.is_file() or any(part.lower() in ('bin','obj','dist') for part in f.relative_to(repo).parts):
            continue
        ET.parse(f)
        print('PASS XML', f.relative_to(repo))
    sources = '\n'.join(p.read_text(encoding='utf8') for p in (repo/'src/Astra.Desktop').rglob('*.cs')
        if not any(part.lower() in ('bin','obj') for part in p.relative_to(repo).parts))
    xaml = (repo/'src/Astra.Desktop/MainWindow.xaml').read_text(encoding='utf8')
    for handler in re.findall(r'\b(?:Click|Toggled|SelectionChanged|TextChanged|Loaded|Unloaded|KeyDown|SizeChanged|Checked|Unchecked|ValueChanged|DoubleTapped|ItemClick|Tapped)="(\w+)"',xaml):
        assert re.search(r'\b'+re.escape(handler)+r'\s*\(',sources), f'Missing C# handler {handler}'
        print('PASS UI event handler', handler)
    assert 'requestedExecutionLevel level="asInvoker"' in (repo/'src/Astra.Desktop/app.manifest').read_text()
    assert '--accept-package-agreements' not in sources
    assert 'UseShellExecute = false' in sources
    assert '127.0.0.1' in (repo/'src/Astra.Desktop/Ai/LocalLlmSession.cs').read_text()
    assert '--api-key-file' in (repo/'src/Astra.Desktop/Ai/LocalLlmSession.cs').read_text()
    assert 'ProtectedData.Protect' in (repo/'src/Astra.Core/SecondBrain/EncryptedNoteStore.cs').read_text()
    assert 'DataProtectionScope.CurrentUser' in (repo/'src/Astra.Core/SecondBrain/EncryptedNoteStore.cs').read_text()
    assert 'ReadToEndAsync' in sources
    assert 'IsRunning' in sources
    assert 'CancelAfter(TimeSpan.FromSeconds(8))' in (repo/'src/Astra.Desktop/Plugins/PluginHost.cs').read_text()
    assert 'SHA256.HashData' in (repo/'src/Astra.Desktop/Plugins/PluginHost.cs').read_text()
    assert 'EffectiveOrbitFps' in sources
    assert 'OperationCanceledException' in sources
    html=(repo/'src/Astra.Desktop/Graphics3D/web/index.html').read_text()
    assert "connect-src 'none'" in html and "default-src 'none'" in html
    js=(repo/'src/Astra.Desktop/Graphics3D/web/scene.js').read_text() + (repo/'src/Astra.Desktop/Graphics3D/web/renderer.js').read_text()
    assert 'requestAnimationFrame' in js and 'getContext(\'webgl\'' in js
    assert not any(x in js for x in ('fetch(', 'XMLHttpRequest', 'WebSocket(', 'https://', 'http://'))
    script = (repo/'scripts/New-AstraIso.ps1').read_text()
    assert 'if (-not $Build)' in script and 'oscdimg' in script
    assert 'ASTRA-OS' in script and 'Astra.Desktop.exe' in script
    assert 'System.Net.WebClient' not in script and 'Invoke-WebRequest' not in script
    # Stable-default security checks for source evolution. Still not runtime security proof.
    recipe=json.loads((repo/'sdk/declarative/org.astra.textkit/recipe.json').read_text(encoding='utf8'))
    assert recipe['schemaVersion']==3 and recipe['operation']=='text.prefix'
    assert (repo/'src/Astra.Core/DeclarativePlugins.cs').exists()
    assert 'NativePluginToggle' in xaml and 'IsOn="False"' in xaml
    assert 'if (!NativePluginToggle.IsOn)' in sources
    assert 'ExecuteDeclarative' in sources and 'DiscoverDeclarative' in sources
    assert 'DeleteAllNotes_Click' in sources
    assert 'FileAttributes]::ReparsePoint' in script
    assert "if ([IO.File]::Exists($partial))" in script
    print('PASS V2 RC default plugin isolation / ISO safety checks')


if __name__ == "__main__":
    main()
