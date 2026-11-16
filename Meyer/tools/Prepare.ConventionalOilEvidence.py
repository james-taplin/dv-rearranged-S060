"""Local native S060 cup hierarchy/meshes for the conventional rod fixture only."""
import argparse
import json
from pathlib import Path
import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler

parser = argparse.ArgumentParser()
parser.add_argument('--game', required=True)
parser.add_argument('--output', required=True)
args = parser.parse_args()
env = UnityPy.load(str(Path(args.game) / 'DerailValley_Data/resources.assets'))
meshes = {}

def vector(value, axes):
    return {k: getattr(value, k) for k in axes}

def tree(pointer):
    transform = pointer.read()
    go = transform.m_GameObject.read()
    node = dict(name=go.m_Name, position=vector(transform.m_LocalPosition, 'xyz'),
                rotation=vector(transform.m_LocalRotation, 'xyzw'), scale=vector(transform.m_LocalScale, 'xyz'),
                mesh='', children=[tree(child) for child in transform.m_Children])
    for pair in go.m_Component:
        component = pair.component
        if component.type.name != 'MeshFilter':
            continue
        reference = component.read().m_Mesh
        if not reference.path_id:
            continue
        source = reference.read()
        node['mesh'] = source.m_Name
        if source.m_Name not in meshes:
            handler = MeshHandler(source)
            handler.process()
            meshes[source.m_Name] = dict(name=source.m_Name,
                vertices=[dict(zip('xyz', p)) for p in handler.m_Vertices],
                uv=[dict(zip('xy', p)) for p in handler.m_UV0],
                triangles=[i for sub in handler.get_triangles() for tri in sub for i in tri])
    return node

roots = []
for obj in env.objects:
    if obj.type.name != 'Transform':
        continue
    transform = obj.read()
    if transform.m_GameObject.read().m_Name == 'ManualOilingPoints' and transform.m_Father.read().m_GameObject.read().m_Name == 'LocoS060_ExternalInteractables':
        roots.append(tree(obj))
if len(roots) != 1 or len([c for c in roots[0]['children'] if c['name'].startswith('OilingPoint')]) != 6:
    raise RuntimeError('Expected six native cups: ' + str([(r['name'], [c['name'] for c in r['children']]) for r in roots]))
output = Path(args.output)
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(json.dumps(dict(roots=roots, meshes=list(meshes.values()))))
print('Exported six native cup transforms and', len(meshes), 'meshes to', output)
