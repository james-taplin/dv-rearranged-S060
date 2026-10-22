import argparse
"""Prepare transient native S282 wheel fixture for the Dockside fit experiment."""
import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler
from pathlib import Path
import json
parser=argparse.ArgumentParser(); parser.add_argument('--output', required=True); parser.add_argument('--game', required=True); args=parser.parse_args()
p = Path(args.output);p.mkdir(parents=True, exist_ok=True)
e=UnityPy.load(str(Path(args.game) / 'DerailValley_Data' / 'resources.assets'))
meshes=[]
for o in e.objects:
 if o.type.name=='Mesh':
  m=o.read()
  if 's282' in m.m_Name.lower() and ('wheel' in m.m_Name.lower() or 'rod' in m.m_Name.lower()):
   # Local-only native mesh fixture; never written beneath Assets or package.
   pass
   if m.m_Name in ['s282_wheels_driving_1','s282_wheels_driving_2']:
    h=MeshHandler(m);h.process();meshes.append(dict(name=m.m_Name,vertices=[dict(zip('xyz',v)) for v in h.m_Vertices],uv=[dict(zip('xy',v)) for v in h.m_UV0],triangles=[i for sub in h.get_triangles() for tri in sub for i in tri]))
p.joinpath('s282-wheels-comparison.json').write_text(json.dumps(dict(meshes=meshes)))
