"""Offline ASTRA source validation. Not a Windows/.NET compiler or runtime QA."""
from __future__ import annotations
import json, re, xml.etree.ElementTree as ET
from pathlib import Path

repo = Path(__file__).resolve().parents[1]
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
sources = '\n'.join(p.read_text(encoding='utf8') for p in (repo/'src/Astra.Desktop').rglob('*.cs'))
xaml = (repo/'src/Astra.Desktop/MainWindow.xaml').read_text(encoding='utf8')
for handler in re.findall(r'\b(?:Click|Toggled|SelectionChanged)="(\w+)"',xaml):
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
assert '256_000' in (repo/'src/Astra.Desktop/Ai/LocalLlmSession.cs').read_text()
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
print('Static tests: PASS. No claims about runtime compilation or Windows QA.')
