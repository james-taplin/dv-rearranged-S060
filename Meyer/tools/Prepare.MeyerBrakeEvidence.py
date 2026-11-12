"""Local native S060 brake/suspension fixture; never packaged as game art."""
import argparse, json
from pathlib import Path
import UnityPy
from UnityPy.helpers.MeshHelper import MeshHandler

p=argparse.ArgumentParser();p.add_argument('--game',required=True);p.add_argument('--output',required=True);a=p.parse_args()
env=UnityPy.load(str(Path(a.game)/'DerailValley_Data/resources.assets'))
meshes={}; scripts=[]; shoe_materials=[]
def vec(v,axes='xyz'):return {k:getattr(v,k) for k in axes}
def walk(pointer,path=''):
    tr=pointer.read();go=tr.m_GameObject.read();path+='/'+go.m_Name
    node=dict(name=go.m_Name,position=vec(tr.m_LocalPosition),rotation=vec(tr.m_LocalRotation,'xyzw'),scale=vec(tr.m_LocalScale),mesh='',components=[],children=[])
    for pair in go.m_Component:
        obj=pair.component; kind=obj.type.name
        if kind=='MonoBehaviour':
            behavior=obj.deref().read(check_read=False);ref=behavior.m_Script
            kind=ref.read().m_ClassName if ref.path_id else kind
            if 'brak' in kind.lower():scripts.append(dict(path=path,type=kind,raw=obj.deref().get_raw_data().hex()))
        node['components'].append(kind)
        if kind=='MeshRenderer' and 'brake_shoes' in go.m_Name:
            for reference in obj.read().m_Materials:
                material=reference.read();shoe_materials.append(material.m_Name)
                for key,value in material.m_SavedProperties.m_TexEnvs:
                    if key=='_MainTex' and value.m_Texture.path_id:
                        target=Path(a.output).parent/'native-brake-atlas.png';value.m_Texture.read().image.save(target)
        if kind in ('MeshFilter','SkinnedMeshRenderer'):
            ref=obj.read().m_Mesh
            if not ref.path_id:continue
            mesh=ref.read();node['mesh']=mesh.m_Name
            if 'brak' in mesh.m_Name.lower() and mesh.m_Name not in meshes:
                h=MeshHandler(mesh);h.process()
                meshes[mesh.m_Name]=dict(name=mesh.m_Name,vertices=[dict(zip('xyz',v)) for v in h.m_Vertices],normals=[dict(zip('xyz',v)) for v in h.m_Normals],uv=[dict(zip('xy',v)) for v in h.m_UV0],triangles=[i for sub in h.get_triangles() for tri in sub for i in tri])
    node['children']=[walk(c,path) for c in tr.m_Children]
    return node
car=next(o.read() for o in env.objects if o.type.name=='GameObject' and o.peek_name()=='LocoS060')
root=next(c.component for c in car.m_Component if c.component.type.name=='Transform')
node=walk(root)
out=Path(a.output);out.parent.mkdir(parents=True,exist_ok=True)
out.write_text(json.dumps(dict(root=node,meshes=list(meshes.values()),scripts=scripts,shoeMaterials=shoe_materials)),encoding='utf-8')
def brief(n,path=''):
    path+='/'+n['name']
    if 'brak' in path.lower() or any('brak' in c.lower() for c in n['components']):print(path,n['mesh'],n['components'],n['position'])
    for c in n['children']:brief(c,path)
brief(node)
for s in scripts:print(s['path'],s['type'],s['raw'])
print('Meshes:',', '.join(meshes),'Materials:',shoe_materials,'Fixture:',out)
