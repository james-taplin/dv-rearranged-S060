import argparse
"""Local-only native oil cup meshes for QA; never packaged or saved in Assets."""
import UnityPy
from pathlib import Path
from UnityPy.helpers.MeshHelper import MeshHandler
import json
parser=argparse.ArgumentParser(); parser.add_argument('--output', required=True); parser.add_argument('--game', required=True); args=parser.parse_args()
p = Path(args.output);p.mkdir(parents=True, exist_ok=True)
e=UnityPy.load(str(Path(args.game) / 'DerailValley_Data' / 'resources.assets'))
meshes=[]
for obj in e.objects:
 if obj.type.name=='Mesh':
  m=obj.read()
  if 'oilcup' in m.m_Name.lower():
   print(m.m_Name,m.m_LocalAABB)
   h=MeshHandler(m);h.process();meshes.append(dict(name=m.m_Name,vertices=[dict(zip('xyz',v)) for v in h.m_Vertices],uv=[dict(zip('xy',v)) for v in h.m_UV0],triangles=[i for sub in h.get_triangles() for tri in sub for i in tri]))
p.joinpath('native-oilcups.json').write_text(json.dumps(dict(meshes=meshes)))
