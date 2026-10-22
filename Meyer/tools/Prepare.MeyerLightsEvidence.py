import argparse
"""Native lamp/stem geometry for transient local QA only; never packaged."""
import json
from pathlib import Path
import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler

parser=argparse.ArgumentParser(); parser.add_argument('--output', required=True); parser.add_argument('--game', required=True); args=parser.parse_args()
output = Path(args.output) / 'meyer-lights-native.json'
output.parent.mkdir(parents=True, exist_ok=True)
env = UnityPy.load(str(Path(args.game) / 'DerailValley_Data' / 'resources.assets'))
meshes = []
stem_mesh_id = None
for obj in env.objects:
    if obj.type.name != 'Mesh':
        continue
    source = obj.read()
    if source.m_Name not in ('s060_buffer_stems', 's060_headlight_glass_R_LO', 'Buffer04'):
        continue
    data = MeshHandler(source)
    data.process()
    meshes.append(dict(name=source.m_Name, readable=source.m_IsReadable,
                       vertices=[dict(zip('xyz', p)) for p in data.m_Vertices],
                       triangles=[i for sub in data.get_triangles() for tri in sub for i in tri]))
    if source.m_Name == 's060_buffer_stems':
        stem_mesh_id = obj.path_id
if len(meshes) != 3:
    raise RuntimeError('Expected native S060 rear glass, buffer stems and Buffer04 head geometry')
stem_renderers = set()
for obj in env.objects:
    if obj.type.name != 'MeshFilter':
        continue
    mesh_filter = obj.read()
    if mesh_filter.m_Mesh.path_id != stem_mesh_id:
        continue
    for component in mesh_filter.m_GameObject.read().m_Component:
        renderer = obj.assets_file.objects[component.component.path_id]
        if renderer.type.name == 'MeshRenderer':
            stem_renderers.add(renderer.path_id)
groups = []
for obj in env.objects:
    if obj.type.name != 'LODGroup':
        continue
    group = obj.read_typetree()
    levels = [dict(screenRelativeHeight=level['screenRelativeHeight'], fadeTransitionWidth=level['fadeTransitionWidth'],
                   containsStem=any(renderer['renderer']['m_PathID'] in stem_renderers for renderer in level['renderers']),
                   originalRendererCount=len(level['renderers'])) for level in group['m_LODs']]
    if any(level['containsStem'] for level in levels):
        groups.append(dict(nativePathId=obj.path_id, size=group['m_Size'], referencePoint=group['m_LocalReferencePoint'], levels=levels))
if len(stem_renderers) != 1 or len(groups) != 1:
    raise RuntimeError('Expected one native stem renderer and one owning LOD group')
output.write_text(json.dumps(dict(meshes=meshes, nativeStemRendererCount=len(stem_renderers), stemLodGroup=groups[0])))
print(output)
