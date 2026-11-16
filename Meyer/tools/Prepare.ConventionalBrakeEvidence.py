"""Exact native LOD sources for the bounded conventional brake fixture; local only."""
import argparse
import json
from pathlib import Path
import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler

p = argparse.ArgumentParser()
p.add_argument('--game', required=True)
p.add_argument('--output', required=True)
a = p.parse_args()
env = UnityPy.load(str(Path(a.game) / 'DerailValley_Data/resources.assets'))
meshes = []
for obj in env.objects:
    if obj.type.name != 'Mesh' or obj.peek_name() not in ('s060_body', 's060_body_LOD1', 's060_brake_shoes'):
        continue
    mesh = obj.read()
    h = MeshHandler(mesh)
    h.process()
    meshes.append(dict(name=mesh.m_Name, readable=mesh.m_IsReadable,
                       vertices=[dict(zip('xyz', v)) for v in h.m_Vertices],
                       uv=[dict(zip('xy', v)) for v in h.m_UV0],
                       triangles=[i for sub in h.get_triangles() for tri in sub for i in tri]))
if len(meshes) != 3 or not all(m['readable'] for m in meshes):
    raise RuntimeError('Expected the three readable native S060 brake/body sources')
out = Path(a.output)
out.parent.mkdir(parents=True, exist_ok=True)
out.write_text(json.dumps(dict(meshes=meshes)), encoding='utf-8')
print('Exported exact LOD0, LOD1 and shoes:', out)
