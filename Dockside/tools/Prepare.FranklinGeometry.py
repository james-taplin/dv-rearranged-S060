"""Extract only the original custom Franklin meshes; local authoring input, never shipped."""
import argparse
import hashlib
import json
from pathlib import Path
import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler

p = argparse.ArgumentParser()
p.add_argument('--bundle', required=True)
p.add_argument('--output', required=True)
a = p.parse_args()
source = Path(a.bundle)
env = UnityPy.load(str(source))
root = next(o.read() for o in env.objects if o.type.name == 'GameObject' and o.peek_name() == 'franklin_type_b')
transform = next(c.component.read() for c in root.m_Component if c.component.type.name == 'Transform')
meshes = []

def walk(t, path):
    go = t.m_GameObject.read()
    for component in go.m_Component:
        if component.component.type.name != 'MeshFilter':
            continue
        mesh = component.component.read().m_Mesh.read()
        assert mesh.m_Name == 'default', 'Unexpected non-custom mesh'
        h = MeshHandler(mesh)
        h.process()
        vertices = [dict(zip('xyz', v)) for v in h.m_Vertices]
        assert len(vertices) > 0 and any(abs(v['x']) + abs(v['y']) + abs(v['z']) > .001 for v in vertices)
        meshes.append(dict(path=path, vertices=vertices, normals=[dict(zip('xyz', v)) for v in h.m_Normals],
                           uv=[dict(zip('xy', v)) for v in h.m_UV0],
                           triangles=[i for sub in h.get_triangles() for tri in sub for i in tri]))
    for child in t.m_Children:
        child = child.read()
        walk(child, (path + '/' if path else '') + child.m_GameObject.read().m_Name)

walk(transform, '')
assert len(meshes) >= 15
output = Path(a.output)
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(json.dumps(dict(sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(), meshes=meshes)), encoding='utf-8')
print(f'Extracted {len(meshes)} intact custom Franklin meshes to {output}')
