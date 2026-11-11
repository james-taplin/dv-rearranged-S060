"""Private QA textures only; native wheel assets remain live game references."""
import argparse, hashlib, json
from pathlib import Path
import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler

parser = argparse.ArgumentParser()
parser.add_argument('--game', required=True)
parser.add_argument('--output', required=True)
args = parser.parse_args()
out = Path(args.output)
out.mkdir(parents=True, exist_ok=True)
env = UnityPy.load(str(Path(args.game) / 'DerailValley_Data/resources.assets'))
for obj in env.objects:
    if obj.type.name != 'MeshFilter':
        continue
    f = obj.read()
    if f.m_Mesh.path_id == 0 or f.m_Mesh.read().m_Name != 's282_wheels_front':
        continue
    go = f.m_GameObject.read()
    renderer = next(c.component.read() for c in go.m_Component
                    if c.component.type.name == 'MeshRenderer')
    material = renderer.m_Materials[0].read()
    assert material.m_Name == 'LocoS282A_Body', material.m_Name
    mesh = f.m_Mesh.read()
    handler = MeshHandler(mesh)
    handler.process()
    fixture = dict(name=mesh.m_Name, vertices=[dict(zip('xyz', p)) for p in handler.m_Vertices],
                   uv=[dict(zip('xy', p)) for p in handler.m_UV0],
                   triangles=[i for sub in handler.get_triangles() for tri in sub for i in tri])
    fixture_path = out / 's282-pony-native.json'
    if fixture_path.exists():
        previous = json.loads(fixture_path.read_text())['meshes'][0]
        assert previous['vertices'] == fixture['vertices'] and previous['triangles'] == fixture['triangles'], 'Native geometry changed; revalidate geometry.'
    fixture_path.write_text(json.dumps(dict(meshes=[fixture])))
    record = dict(material=material.m_Name, mesh='s282_wheels_front', textures={},
                  nativeMeshPathId=mesh.object_reader.path_id,
                  fixtureSha256=hashlib.sha256(fixture_path.read_bytes()).hexdigest())
    for name, slot in material.m_SavedProperties.m_TexEnvs:
        if name not in ('_MainTex', '_MetallicGlossMap'):
            continue
        texture = slot.m_Texture.read()
        path = out / ('shay-native-wheel-' + ('albedo' if name == '_MainTex' else 'metallic') + '.png')
        texture.image.save(path)
        record['textures'][name] = dict(name=texture.m_Name, file=str(path),
            scale=[slot.m_Scale.x, slot.m_Scale.y], offset=[slot.m_Offset.x, slot.m_Offset.y],
            sha256=hashlib.sha256(path.read_bytes()).hexdigest())
    assert len(record['textures']) == 2
    (out / 'shay-wheel-material.json').write_text(json.dumps(record, indent=2))
    print('Prepared private native wheel material:', material.m_Name)
    break
else:
    raise RuntimeError('Native S282 pony wheel material not found.')
