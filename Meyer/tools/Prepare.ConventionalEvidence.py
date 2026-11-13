"""Local-only native S060 mechanism hierarchy/meshes for conversion QA."""
import argparse
import json
from pathlib import Path
import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler

parser = argparse.ArgumentParser()
parser.add_argument('--output', required=True)
parser.add_argument('--game', required=True)
args = parser.parse_args()
env = UnityPy.load(str(Path(args.game) / 'DerailValley_Data' / 'resources.assets'))
meshes = {}

def vector(value, axes):
    return {k: getattr(value, k) for k in axes}

def tree(pointer):
    transform = pointer.read()
    go = transform.m_GameObject.read()
    node = dict(name=go.m_Name, position=vector(transform.m_LocalPosition, 'xyz'),
                rotation=vector(transform.m_LocalRotation, 'xyzw'), scale=vector(transform.m_LocalScale, 'xyz'), mesh='',
                children=[tree(child) for child in transform.m_Children])
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
    if obj.type.name == 'Transform':
        transform = obj.read()
        name = transform.m_GameObject.read().m_Name
        if name in ('DrivingMechanism_L', 'DrivingMechanism_R'):
            parent = transform.m_Father.read()
            if parent.m_GameObject.read().m_Name == 'LocoS060_Body':
                roots.append(tree(obj))
        elif name == 'smokebox_door':
            parent = transform.m_Father.read()
            if parent.m_GameObject.read().m_Name == 'Static' and parent.m_Father.read().m_GameObject.read().m_Name == 'LocoS060_Body':
                if any(abs(getattr(parent.m_LocalPosition,axis))>1e-6 for axis in 'xyz'):
                    raise RuntimeError('Native Static parent pose changed; door fixture must include its transform')
                roots.append(tree(obj))
if len(roots) != 3 or sorted(root['name'] for root in roots) != ['DrivingMechanism_L','DrivingMechanism_R','smokebox_door']:
    raise RuntimeError(f'Expected two mechanisms and the separate smokebox door, found {[root["name"] for root in roots]}')
output = Path(args.output) / 'conventional-native.json'
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(json.dumps(dict(roots=roots, meshes=list(meshes.values()))))
print(f'Prepared native S060 mechanisms and smokebox door: {len(roots)} roots, {len(meshes)} meshes -> {output}')
