import argparse
"""Local-only native geometry/texture fixtures for QA. Never packaged."""
import json
from pathlib import Path
import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler

parser=argparse.ArgumentParser(); parser.add_argument('--output', required=True); parser.add_argument('--game', required=True); args=parser.parse_args()
out = Path(args.output)
out.mkdir(parents=True, exist_ok=True)
env = UnityPy.load(str(Path(args.game) / 'DerailValley_Data' / 'resources.assets'))
meshes = []
for obj in env.objects:
    if obj.type.name == 'Mesh':
        mesh = obj.read()
        if mesh.m_Name not in ('s060_body', 's060_Wheels_01', 's060_Wheels_02'):
            continue
        handler = MeshHandler(mesh)
        handler.process()
        vectors = lambda values: [dict(zip('xyz', p)) for p in values]
        meshes.append(dict(name=mesh.m_Name, vertices=vectors(handler.m_Vertices),
                           uv=[dict(zip('xy', p)) for p in handler.m_UV0],
                           triangles=[i for sub in handler.get_triangles() for tri in sub for i in tri]))
    elif obj.type.name == 'Material':
        material = obj.read()
        if material.m_Name == 'LocoS060Green_Body':
            for name, value in material.m_SavedProperties.m_TexEnvs:
                if name == '_MainTex':
                    value.m_Texture.read().image.save(out / 'native-preview-atlas.png')
out.joinpath('native-preview.json').write_text(json.dumps(dict(meshes=meshes)))
print('Prepared local-only preview/geometry fixtures:', ', '.join(m['name'] for m in meshes))
