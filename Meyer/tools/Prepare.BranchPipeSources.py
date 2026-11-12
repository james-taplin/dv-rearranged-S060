import UnityPy,json,hashlib
from UnityPy.helpers.MeshHelper import MeshHandler
from pathlib import Path
p=Path(r'B:\SteamLibrary\steamapps\common\Derail Valley\Mods\RearrangedS282\assets\s282_drivetrain')
e=UnityPy.load(str(p));data={'sourceSha256':hashlib.sha256(p.read_bytes()).hexdigest().upper(),'meshes':[]}
for o in e.objects:
 if o.type.name=='GameObject':
  go=o.read()
  if go.m_Name=='branch_pipe':
   root=next(c.component.read() for c in go.m_Component if c.component.type.name=='Transform')
   for ch in root.m_Children:
    t=ch.read();key=t.m_GameObject.read().m_Name
    print(key,'rotation',t.m_LocalRotation)
    child=t.m_Children[0].read(); g=child.m_GameObject.read()
    m=next(c.component.read().m_Mesh.read() for c in g.m_Component if c.component.type.name=='MeshFilter')
    h=MeshHandler(m);h.process()
    data['meshes'].append({'name':key,'vertices':[dict(zip('xyz',v)) for v in h.m_Vertices], 'uv':[dict(zip('xy',v)) for v in h.m_UV0], 'triangles':[i for sub in h.get_triangles() for tri in sub for i in tri]})
out=Path('Meyer/AuthoringData');out.mkdir(exist_ok=True)
out.joinpath('S282BranchPipes.json').write_text(json.dumps(data,separators=(',',':')))
print('Exported custom mesh source:',[(m['name'],len(m['vertices'])) for m in data['meshes']],data['sourceSha256'])
