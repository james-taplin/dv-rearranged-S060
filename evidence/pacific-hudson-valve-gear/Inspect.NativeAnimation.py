import UnityPy,struct,json,zlib
from pathlib import Path
E=UnityPy.load(r'B:\SteamLibrary\steamapps\common\Derail Valley\DerailValley_Data\resources.assets')
def go(t): return t.m_GameObject.deref().read().m_Name
def path(t):
 p=t.m_Father
 return (path(p.deref().read())+'/' if p.m_PathID else '')+go(t)
trans=[]
for o in E.objects:
 if o.type.name!='Transform': continue
 t=o.read(); p=path(t)
 if '/LocoS282A_Drivetrain R/' in p or p.endswith('/LocoS282A_Drivetrain R'): trans.append((o,t,p))
paths={zlib.crc32(p.split('/LocoS282A_Drivetrain R/',1)[-1].encode()):p.split('/LocoS282A_Drivetrain R/',1)[-1] for o,t,p in trans if '/LocoS282A_Drivetrain R/' in p}
out={'transforms':[],'clips':[],'pipes':[]}
for o,t,p in trans:
 if '(Clone)' in p: continue
 out['transforms'].append({'path':p,'id':o.path_id,'position':vars(t.m_LocalPosition) if hasattr(t.m_LocalPosition,'__dict__') else [t.m_LocalPosition.x,t.m_LocalPosition.y,t.m_LocalPosition.z], 'rotation':[t.m_LocalRotation.x,t.m_LocalRotation.y,t.m_LocalRotation.z,t.m_LocalRotation.w], 'scale':[t.m_LocalScale.x,t.m_LocalScale.y,t.m_LocalScale.z]})
for o in E.objects:
 if o.type.name!='AnimationClip': continue
 d=o.read_typetree()
 if not d['m_Name'].startswith('LocoS282A_Drivetrain'): continue
 clip=d['m_MuscleClip']['m_Clip']['data']; raw=struct.pack('<'+'I'*len(clip['m_StreamedClip']['data']),*clip['m_StreamedClip']['data']); off=0; frames=[]
 while off<len(raw):
  time,count=struct.unpack_from('<fi',raw,off);off+=8; vals={}
  for j in range(count):
   index,*co=struct.unpack_from('<iffff',raw,off);off+=20;vals[index]=co[-1]
  frames.append((time,vals))
 binds=[]; i=0; streamed=clip['m_StreamedClip']['curveCount']; const=clip['m_ConstantClip']['data']
 for b in d['m_ClipBindingConstant']['genericBindings']:
  n={1:3,2:4,3:3}.get(b['attribute'],1)
  binds.append({'path':paths.get(b['path'],str(b['path'])),'attribute':b['attribute'],'indices':list(range(i,i+n)),'first':[frames[0][1].get(k) if k<streamed else const[k-streamed] for k in range(i,i+n)]});i+=n
 out['clips'].append({'name':d['m_Name'],'sampleRate':d['m_SampleRate'],'bindings':binds,'frames':[{'time':t,'values':v} for t,v in frames if t>=0 and t<1000], 'dense':clip['m_DenseClip'], 'constant':clip['m_ConstantClip']})
from UnityPy.helpers.MeshHelper import MeshHandler
for o in E.objects:
 if o.type.name=='AnimatorController' and o.read().m_Name=='LocoS282A_Drivetrain':
  d=o.read_typetree(); out['controller']={'name':d['m_Name'],'strings':d['m_TOS'],'clips':d['m_AnimationClips']}
 if o.type.name!='Mesh' or o.read().m_Name not in ['s282_locomotive_body','s282_locomotive_body_LOD1']: continue
 mesh=o.read(); handler=MeshHandler(mesh); handler.process()
 for side in [-1,1]:
  verts=[list(v) for v in handler.m_Vertices if .6<=v[0]*side<=1.2 and 1.5<=v[1]<=2.5 and 4.45<=v[2]<=4.78]
  out['pipes'].append({'name':mesh.m_Name+('_r' if side<0 else '_l'),'vertices':verts,'bodyOffset':4.885 if 'LOD1' in mesh.m_Name else 4.88})
Path(r'B:\GitHub repo\rearranged-s282-b99\evidence\pacific-hudson-valve-gear\native-animation.json').write_text(json.dumps(out,indent=2))
print('Native transforms',len(out['transforms']),'clips',[(c['name'],len(c['frames'])) for c in out['clips']],'pipe samples',[(p['name'],len(p['vertices'])) for p in out['pipes']])
