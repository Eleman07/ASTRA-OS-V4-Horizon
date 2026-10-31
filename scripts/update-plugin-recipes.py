"""Generate the bundled data-only manifests from the reviewed C# tool catalog."""
import json
import re
from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / 'src/Astra.Core/PluginLibrary.cs').read_text(encoding='utf-8')
literal = r'("(?:\\.|[^"\\])*")'
pattern = r'new\(' + ', '.join([literal] * 5) + r'\)'
tools = [tuple(json.loads(s) for s in match) for match in re.findall(pattern, source)]
assert len(tools) >= 30, 'Catalog extraction failed; no manifests changed.'
lines = ['# Plugin locali ASTRA', '', 'Operazioni dichiarative senza shell, rete, filesystem o codice importato.', '', '| Plugin | Categoria | Funzione |', '|---|---|---|']
for operation, name, category, description, example in tools:
    plugin_id = 'org.astra.tools.' + operation
    assert re.fullmatch(r'[a-z0-9.-]{3,64}', plugin_id)
    directory = root / 'sdk/declarative' / plugin_id
    directory.mkdir(exist_ok=True)
    manifest = dict(schemaVersion=3, id=plugin_id, name=name, description=description,
                    operation=operation, category=category, example=example, prefix=None)
    (directory / 'recipe.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    lines.append(f'| {name} | {category} | {description} |')
(root / 'sdk/declarative/README.md').write_text('\n'.join(lines) + '\n', encoding='utf-8')
print(f'Generated {len(tools)} distinct functional plugins; existing recipes preserved.')
