"""Original procedural mesh primitives; no external geometry or game assets."""
from pathlib import Path
import numpy as np
import math,json,hashlib
ROOT=Path(__file__).resolve().parents[1]
ASSET=ROOT/'unity/Assets/EngineStudy'
SLOTS=['S060_PAINT','S060_DARK_STEEL','S060_MACHINED','S060_PIPE']
COLORS=[[.15,.30,.24],[.14,.17,.19],[.62,.68,.70],[.32,.30,.25]]
def dump(path,data):
 path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(data,separators=(',',':')),encoding='utf8')
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
class Mesh:
 def __init__(self):self.v=[];self.n=[];self.uv=[];self.t=[]
 def face(self,p,uv=None,n=None):
  p=np.array(p,float);norm=np.cross(p[1]-p[0],p[2]-p[0]);norm/=np.linalg.norm(norm)
  i=len(self.v);self.v.extend(p.tolist());self.n.extend(([norm.tolist()]*len(p)) if n is None else np.asarray(n).tolist())
  self.uv.extend(uv or [[0,0],[1,0],[1,1],[0,1]][:len(p)])
  self.t.extend([[i,i+j,i+j+1] for j in range(1,len(p)-1)])
 def box(self,lo,hi):
  x,y,z=lo;X,Y,Z=hi
  for p in [[(x,y,z),(x,y,Z),(x,Y,Z),(x,Y,z)],[(X,y,Z),(X,y,z),(X,Y,z),(X,Y,Z)],[(x,y,Z),(x,y,z),(X,y,z),(X,y,Z)],[(x,Y,z),(x,Y,Z),(X,Y,Z),(X,Y,z)],[(X,y,z),(x,y,z),(x,Y,z),(X,Y,z)],[(x,y,Z),(X,y,Z),(X,Y,Z),(x,Y,Z)]]:self.face(p)
 def tube(self,a,b,ro,ri=0,n=16):
  # Preserve a real clear bore between polygon vertices, including the far LOD.
  if ri:ri/=math.cos(math.pi/n)
  a=np.array(a,float);b=np.array(b,float);d=b-a;d/=np.linalg.norm(d)
  ref=np.array([0,0,1.]) if abs(d[2])<.9 else np.array([1.,0,0])
  u=np.cross(ref,d);u/=np.linalg.norm(u);v=np.cross(d,u)
  radial=[math.cos(i*math.tau/n)*u+math.sin(i*math.tau/n)*v for i in range(n+1)]
  for i in range(n):
   q,w=radial[i:i+2]
   self.face([a+ro*q,a+ro*w,b+ro*w,b+ro*q],[[i/n,0],[(i+1)/n,0],[(i+1)/n,1],[i/n,1]],[-q,-w,-w,-q])
   # Correct face winding below, normals are separately exact cylinder normals.
   self.n[-4:]=[q.tolist(),w.tolist(),w.tolist(),q.tolist()]
   if ri:
    self.face([a+ri*w,a+ri*q,b+ri*q,b+ri*w],[[i/n,0],[(i+1)/n,0],[(i+1)/n,1],[i/n,1]],[-w,-q,-q,-w])
    self.face([a+ri*q,a+ri*w,a+ro*w,a+ro*q],n=[-d]*4)
    self.face([b+ro*q,b+ro*w,b+ri*w,b+ri*q],n=[d]*4)
   else:
    self.face([a,a+ro*w,a+ro*q],[[.5,.5],[.5+.5*math.cos((i+1)*math.tau/n),.5+.5*math.sin((i+1)*math.tau/n)],[.5+.5*math.cos(i*math.tau/n),.5+.5*math.sin(i*math.tau/n)]],[-d]*3)
    self.face([b,b+ro*q,b+ro*w],n=[d]*3)
 def data(self):return dict(vertices=np.array(self.v).flatten().tolist(),normals=np.array(self.n).flatten().tolist(),uv=np.array(self.uv).flatten().tolist(),triangles=np.array(self.t).flatten().tolist())
