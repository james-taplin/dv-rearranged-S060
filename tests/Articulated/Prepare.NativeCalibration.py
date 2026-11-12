"""Extract the installed stock S060 calibration; local test evidence, never shipped."""
import argparse
import hashlib
import json
import struct
from pathlib import Path
import UnityPy

p = argparse.ArgumentParser()
p.add_argument('--game', required=True)
p.add_argument('--output', required=True)
a = p.parse_args()
data = Path(a.game) / 'DerailValley_Data'
source = data / 'resources.assets'
env = UnityPy.load(str(source))

def hierarchy(go):
    transform = next(c.component.read() for c in go.m_Component
                     if c.component.deref().type.name == 'Transform')
    names = [go.m_Name]
    while transform.m_Father.path_id:
        transform = transform.m_Father.read()
        names.append(transform.m_GameObject.read().m_Name)
    return '/'.join(reversed(names))

def string(raw, at):
    n, = struct.unpack_from('<i', raw, at)
    if n < 0 or n > len(raw) - at - 4:
        raise ValueError('Invalid native serialized string')
    value = raw[at + 4:at + 4 + n].decode('utf-8')
    return value, (at + 4 + n + 3) & ~3

def sha(path):
    with path.open('rb') as f:
        return hashlib.file_digest(f, 'sha256').hexdigest().upper()

matches = []
for obj in env.objects:
    if obj.type.name != 'MonoBehaviour':
        continue
    behavior = obj.read(check_read=False)
    if not behavior.m_Script.path_id or behavior.m_Script.read().m_ClassName != 'ReciprocatingSteamEngineDefinition':
        continue
    path = hierarchy(behavior.m_GameObject.read())
    if path != 'LocoS060/[sim]/steamEngine':
        continue
    raw = obj.get_raw_data()
    # Native MonoBehaviour: GO PPtr12, enabled/alignment4, script PPtr12,
    # aligned name, then SimComponentDefinition.ID. Definition has i17f in
    # the installed managed declaration order; readonly ports are not serialized.
    name, at = string(raw, 28)
    identity, at = string(raw, at)
    if name != behavior.m_Name or identity != 'steamEngine' or len(raw) - at != 72:
        raise ValueError('Native engine serialized layout changed; do not guess fields')
    names = ['numCylinders', 'cylinderBore', 'pistonStroke', 'minCutoff', 'maxCutoff',
             'throttleMaxFlow', 'steamChestVolume', 'cylinderHeatRate', 'maxCondensationRate',
             'primingRate', 'maxWaterExpulsionRate', 'waterDrainPercentagePerSecond',
             'waterDamagePercentagePerChuff', 'waterDamageRestorePerSecond',
             'cylinderCrackDamage', 'passiveDamagePerRev', 'noOilDamagePerRev', 'ashChuffDamage']
    fields = dict(zip(names, struct.unpack_from('<i17f', raw, at)))
    if fields['numCylinders'] != 2 or not .40 < fields['cylinderBore'] < .44 or not .59 < fields['pistonStroke'] < .63:
        raise ValueError('Unexpected stock S060 baseline; review native version')
    matches.append(dict(MonoBehaviour=dict(ID=identity, **fields), source=dict(
        prefabPath=path, pathId=obj.path_id, serializedByteCount=len(raw),
        resourcesPath=str(source), resourcesSha256=sha(source),
        simulationAssemblySha256=sha(data / 'Managed' / 'DV.Simulation.dll'),
        nativeDefinition=True)))
if len(matches) != 1:
    raise ValueError(f'Expected one exact native S060 definition, found {len(matches)}')
out = Path(a.output)
out.parent.mkdir(parents=True, exist_ok=True)
out.write_text(json.dumps(matches[0], indent=2) + '\n')
print('Extracted stock S060 engine from', matches[0]['source']['prefabPath'], '->', out)
