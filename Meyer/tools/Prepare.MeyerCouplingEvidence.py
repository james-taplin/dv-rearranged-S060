"""Local-only native S060 coupling fixture. Never packaged or installed."""
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

def tree(pointer):
    transform = pointer.read()
    go = transform.m_GameObject.read()
    vector = lambda value, axes: {key: getattr(value, key) for key in axes}
    node = dict(name=go.m_Name, position=vector(transform.m_LocalPosition, 'xyz'),
                rotation=vector(transform.m_LocalRotation, 'xyzw'),
                scale=vector(transform.m_LocalScale, 'xyz'), mesh='',
                children=[tree(child) for child in transform.m_Children])
    for pair in go.m_Component:
        if pair.component.type.name != 'MeshFilter':
            continue
        reference = pair.component.read().m_Mesh
        if not reference.path_id:
            continue
        source = reference.read()
        node['mesh'] = source.m_Name
        if source.m_Name not in meshes:
            handler = MeshHandler(source)
            handler.process()
            meshes[source.m_Name] = dict(name=source.m_Name,
                vertices=[dict(zip('xyz', point)) for point in handler.m_Vertices],
                triangles=[index for sub in handler.get_triangles() for tri in sub for index in tri])
    return node

car = next(obj.read() for obj in env.objects
           if obj.type.name == 'GameObject' and obj.peek_name() == 'LocoS060')
root = next(pair.component.read() for pair in car.m_Component if pair.component.type.name == 'Transform')
nodes = []
for child in root.m_Children:
    name = child.read().m_GameObject.read().m_Name
    if name in ('[buffers]', '[coupler front]', '[coupler rear]'):
        nodes.append(tree(child))
assert len(nodes) == 3 and 'CarHook' in meshes and 'HookPlate' in meshes
output = Path(args.output)
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(json.dumps(dict(nodes=nodes, meshes=list(meshes.values()))), encoding='utf-8')
print(f'Native coupling fixture: {len(nodes)} roots, {len(meshes)} meshes -> {output}')
