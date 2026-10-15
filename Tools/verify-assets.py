"""Verify portable project references and unchanged source media (stdlib only)."""
import hashlib
import json
from pathlib import Path

project = Path(__file__).resolve().parent.parent
source = project.parent / 'browser-neon-shooter'
manifest = json.loads((project / 'docs/asset-manifest.json').read_text())
for item in manifest:
    copied = project / item['destination']
    assert hashlib.sha256(copied.read_bytes()).hexdigest() == item['sha256'], copied
    if source.exists():
        original = source / item['source']
        assert hashlib.sha256(original.read_bytes()).hexdigest() == item['sha256'], original
print(f'PASS {len(manifest)} original media assets and checksums')
menu = json.loads((project / 'docs/menu-soundtrack.json').read_text())
assert hashlib.sha256((project / menu['destination']).read_bytes()).hexdigest() == menu['sha256']
print('PASS user-supplied menu soundtrack checksum')

for name in ('NeonShooter.Core', 'NeonShooter.Runtime', 'NeonShooter.Editor',
             'NeonShooter.EditMode', 'NeonShooter.PlayMode'):
    definition = next((project / 'Assets').rglob(name + '.asmdef'))
    assert json.loads(definition.read_text())['name'] == name
print('PASS five assembly definitions')

scene = project / 'Assets/Scenes/NeonShooter.unity'
script = project / 'Assets/NeonShooter/Runtime/NeonGame.cs'
scene_guid = next(line.split(': ', 1)[1] for line in Path(str(scene) + '.meta').read_text().splitlines() if line.startswith('guid:'))
script_guid = next(line.split(': ', 1)[1] for line in Path(str(script) + '.meta').read_text().splitlines() if line.startswith('guid:'))
assert script_guid in scene.read_text()
assert scene_guid in (project / 'ProjectSettings/EditorBuildSettings.asset').read_text()
print('PASS scene, bootstrap script and build-scene GUID references')

conversion = json.loads((project / 'docs/browser-font-conversion.json').read_text())
assert hashlib.sha256((project / conversion['destination']).read_bytes()).hexdigest() == conversion['destination_sha256']
if source.exists():
    assert hashlib.sha256((project / conversion['source']).read_bytes()).hexdigest() == conversion['source_sha256']
print('PASS browser Neue Power font source and converted TTF checksums')
