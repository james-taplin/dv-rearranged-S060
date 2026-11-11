"""Original procedural mesh source. Run with Python + numpy; writes only transmission/.
No native mesh or texture is imported or exported. Metres, positive local scales.
Bevel flanks are tapered trapezoids, NOT manufacturing involutes (see handover).
"""
from pathlib import Path
import json, math, hashlib
import numpy as np
ROOT=Path(__file__).resolve().parents[1]
for folder in ['unity/Assets/Transmission','unity/ProjectSettings','unity/Packages']:(ROOT/folder).mkdir(parents=True,exist_ok=True)
C=json.loads((ROOT.parent/'shared/fit-contract.json').read_text())
HASH=hashlib.sha256((ROOT.parent/'shared/fit-contract.json').read_bytes()).hexdigest()
assert C['revision']=='S060-SHAY-FIT-R3-20261111'
assert C['bevelGears']['teeth']==[86,20]
G=C['bevelGears'];RING=G['ringPitchRadius'];PIN=G['pinionPitchRadius'];APEX=G['coneApexX'];OFF=abs(G['pinionPitchPlaneZOffsetByTruck']['rear'])
B=C['bulk'];SHAFT=B['lineShaftDiameter']/2;SQ=B['slidingSquareSection']/2
# R2 full-size Shay: universal joints 160 mm across (R1 60 mm) -> yoke/cross scale.
UJ=B['universalJointAcross']/.060;ALLOW=C['transmissionR3']['jointEndAllowance']
# Pony wheel disc face at the ring-adapter radius (private fixture, |x| at r .25-.31).
DISC_X=.7570
def vec(p): return dict(zip('xyz',map(float,p)))
def quat(p): return dict(zip('xyzw',map(float,p)))
meshes=[];parts=[]
LENGTHS=C['transmissionR3']['jointLengthsByTruck']  # R2 joint-centre lengths (fit search, engine shifted -0.15)
MEMBER=C['transmissionR3']['memberLength']
class Mesh:
 def __init__(self):self.v=[];self.t=[]
 def quad(self,a,b,c,d):
  i=len(self.v);self.v.extend([a,b,c,d]);self.t.extend([[i,i+1,i+2],[i,i+2,i+3]])
 def solid(self,pts):
  # eight corners, two consistently ordered rectangular rings
  a,b,c,d,e,f,g,h=pts
  for q in [(a,d,c,b),(e,f,g,h),(a,b,f,e),(b,c,g,f),(c,d,h,g),(d,a,e,h)]:self.quad(*q)
 def box(self,lo,hi):
  x,y,z=lo;X,Y,Z=hi;self.solid([(x,y,z),(X,y,z),(X,Y,z),(x,Y,z),(x,y,Z),(X,y,Z),(X,Y,Z),(x,Y,Z)])
 def tube(self,ro,ri,z0,z1,n=16):
  if ri:ri/=math.cos(math.pi/n)
  rings=[[(r*math.cos(a),r*math.sin(a),z) for a in np.linspace(0,math.tau,n,endpoint=False)] for r,z in [(ro,z0),(ro,z1),(ri,z0),(ri,z1)]]
  for i in range(n):
   j=(i+1)%n;a,b,c,d=rings
   for q in [(a[i],a[j],b[j],b[i]),(c[j],c[i],d[i],d[j]),(a[j],a[i],c[i],c[j]),(b[i],b[j],d[j],d[i])]:self.quad(*q)
 def square(self,outer,inner,length):
  # square tubing, not overlapping solid rods
  self.box([-outer,-outer,0],[-inner,outer,length]);self.box([inner,-outer,0],[outer,outer,length])
  self.box([-inner,-outer,0],[inner,-inner,length]);self.box([-inner,inner,0],[inner,outer,length])
 def finish(self,name):
  v=np.array(self.v);t=np.array(self.t);ns=[];uv=[]
  # hard edges per quad, no UV overlaps between quad islands
  cols=math.ceil(math.sqrt(len(v)/4));rows=math.ceil(len(v)/4/cols)
  for i in range(0,len(v),4):
   n=np.cross(v[i+1]-v[i],v[i+2]-v[i]);n/=max(np.linalg.norm(n),1e-20);ns.extend([vec(n)]*4)
   q=i//4;u=q%cols;w=q//cols
   uv.extend([dict(x=(u+a)/cols,y=(w+b)/rows) for a,b in [(.05,.05),(.95,.05),(.95,.95),(.05,.95)]])
  entry=dict(name=name,vertices=[vec(x) for x in v],normals=ns,uv=uv,triangles=t.flatten().tolist());meshes.append(entry)
  out=ROOT/'meshes';out.mkdir(exist_ok=True)
  lines=['# Original S060 Shay transmission; metres; local +Z axis','o '+name]
  lines+=['v %.9f %.9f %.9f'%tuple(x) for x in v]
  lines+=['vt %.8f %.8f'%(x['x'],x['y']) for x in uv]
  lines+=['vn %.8f %.8f %.8f'%tuple(x.values()) for x in ns]
  lines+=['f '+' '.join(f'{j+1}/{j+1}/{j+1}' for j in tri) for tri in t]
  (out/(name+'.obj')).write_text('\n'.join(lines)+'\n')
  return name

def bevel(name,r,axial,teeth,lod,ring=False):
 m=Mesh();R=math.hypot(r,axial);sd=r/R;cd=axial/R
 # Cone points toward local -Z; h is normal displacement toward mating gear.
 def p(scale,h,a):return ((r*scale+h*cd)*math.cos(a),(r*scale+h*cd)*math.sin(a),-axial*scale+h*sd)
 n=max(24,teeth//2) if lod==0 else max(12,teeth//3)
 # Conical root band. Ring retains open bore; pinion has separate hub/spider.
 for i in range(n):
  a=i*math.tau/n;b=(i+1)*math.tau/n
  corners=[p(s,h,t) for s in [.96,1.04] for h,t in [(-.013,a),(-.013,b),(-.009,b),(-.009,a)]]
  A,B,D,E,F,G,H,I=corners
  for q in [(A,E,D,B),(F,G,H,I),(A,B,G,F),(D,E,I,H)]:m.quad(*q)
 if lod<2:
  for i in range(teeth):
   a=i*math.tau/teeth
   # tooth width .40 circular pitch at root, .22 at top: generous visual backlash
   m.solid([p(s,h,a+side*math.tau/teeth*width/2) for s in [.96,1.04] for h,side,width in [(-.009,-1,.40),(-.009,1,.40),(.004,1,.20),(.004,-1,.20)]])
 # Backing annulus / pinion web is kept behind the tooth cone.
 if ring:m.tube(r+.02,r-.04,-axial-.032,-axial-.008, 24 if lod==0 else 12)
 else:
  m.tube(r+.002,SHAFT+.006,-axial-.040,-axial-.0145,24 if lod==0 else 12)
  m.tube(SHAFT+.009,SHAFT+.006,-axial-.075,-axial-.008,16)
 return m.finish(name)

def add(name,parent,mesh,pos=(0,0,0),rot=(0,0,0),slot='shaft_steel',role='fixed',index=0):
 parts.append(dict(name=name,parent=parent,mesh=mesh,position=vec(pos),euler=vec(rot),slot=slot,role=role,index=index))
for lod in range(3):
 for kind,r,d,t,ring in [('ring',RING,PIN,86,True),('pinion',PIN,RING,20,False)]:bevel(f'{kind}_LOD{lod}',r,d,t,lod,ring)
 for kind in ['flange','bearing','yoke','cross','shaft_rear','shaft_front','adapter']:
  m=Mesh();n=16 if lod==0 else 8
  if kind=='flange':m.tube(B['crankshaftDiameter']/2+.025,SHAFT,-.03,.03,n)
  if kind=='bearing':
   m.tube(SHAFT+.035,SHAFT+.002,-.04,.04,n);m.box([-.045,SHAFT+.03,-.04],[.045,SHAFT+.08,.04])
  if kind=='yoke':
   # open fork, arms capture cross trunnions along local X; body lies on -Z
   m.tube(.075,.065,-.120,-.072,n)
   for sign in [-1,1]:
    lo,hi=sorted([sign*.062,sign*.083]);m.box([lo,-.015,-.083],[hi,.015,.0187])
  if kind=='cross':
   m.box([-.073,-.012,-.012],[.073,.012,.012]);m.box([-.012,-.073,-.012],[.012,.073,.012])
  if kind.startswith('shaft_'):
   j=0 if kind.endswith('rear') else 1;s=C['transmissionStudy'][j];zz=[C['wheelStations'][j*2+i]+(-OFF if j==0 else OFF) for i in range(2)]+[s['truckEndNeutral'][2]]
   m.tube(SHAFT,SHAFT*.6,min(zz)-(.10 if j==0 else 0),max(zz)+(.10 if j==1 else 0),n)
  if kind=='adapter':
   # Native spoke-face triangle plane X=.829361439; 0.0001 m visual seating gap.
   # Original deep annular drum bridges to ring without changing wheel/gauge.
   # R2: cast spacer from the pony disc face to the ring web, bolted on a .257 m circle.
   m.tube(RING-.02,RING-.065,DISC_X+.0001-APEX,-PIN-.032,24 if lod==0 else 12)
   if lod==0:
    for angle in np.linspace(0,math.tau,8,endpoint=False):
     first=len(m.v);m.tube(.008,.003,-PIN-.037,-PIN-.027,8)
     for ii in range(first,len(m.v)):
      x,y,z=m.v[ii];m.v[ii]=(x+(RING-.030)*math.cos(angle),y+(RING-.030)*math.sin(angle),z)
  m.finish(f'{kind}_LOD{lod}')
 for j in range(2):
  m=Mesh();s=-1 if j==0 else 1
  # Folded bearing hanger to truck-frame top. It clears the wheel radially above it.
  # R2: post from the bearing to an arm on the truck side-frame top flange (y .8055).
  top=C['trucks'][0]['frameRailY'][1]-C['wheelAxisY'];inb=APEX-.52
  m.box([-.025,SHAFT+.075,-.025],[.025,top,.025])
  m.box([-inb-.04,top-.03,-.03],[.025,top,.03])
  # Outer bearings sit beyond the truck frame end: run the arm back onto the frame flange.
  z0,z1=sorted([0,-s*.32]);m.box([-inb-.04,top-.03,z0-.03],[-inb+.04,top,z1+.03])
  m.finish(f'hanger{j}_LOD{lod}')
 for j,L in enumerate([MEMBER,MEMBER]):
  for k,(outer,inner) in enumerate([(SQ*.70,0),(SQ*.85,SQ*.70+.001),(SQ,SQ*.85+.001)]):
   m=Mesh()
   if inner:m.square(outer,inner,L)
   else:m.box([-outer,-outer,0],[outer,outer,L])
   m.finish(f'link{j}_member{k}_LOD{lod}')

for name in ['ENGINE_CRANK_REAR','ENGINE_CRANK_FRONT','FRONT_DRIVE_OUTPUT']:
 add(name+'_collar',name,'flange',role='outlet',index=1 if name=='FRONT_DRIVE_OUTPUT' else 0)
for j,truck in enumerate(C['trucks']):
 parent='TRUCK_'+truck['name'].upper()+'_PIVOT';py=truck['pivot'][2];ay=C['wheelAxisY']
 add(truck['name']+'_line_shaft',parent,'shaft_'+truck['name'],[APEX,ay,-py],role='line',index=j)
 for i in truck['axleIndices']:
  z=C['wheelStations'][i]-py
  # Local mesh -Z cone maps toward -X for wheel and toward +/-Z for pinion.
  add('wheel_ring_'+str(i+1),parent,'ring',[APEX,ay,z],[0,90,0],'gear_steel','wheel',i)
  add('wheel_adapter_'+str(i+1),parent,'adapter',[APEX,ay,z],[0,90,0],'gear_steel','wheel',i)
  add('pinion_'+str(i+1),parent,'pinion',[APEX,ay,z],[0,0 if j==0 else 180,180/20],'gear_steel','pinion',i)
  # Support is beyond pinion, on its back side, no support through teeth.
  s=-1 if j==0 else 1
  add('bearing_'+str(i+1),parent,'bearing',[APEX,ay,z+s*(OFF+.10)],slot='frame_painted')
  add('bearing_hanger_'+str(i+1),parent,'hanger'+str(j),[APEX,ay,z+s*(OFF+.10)],slot='frame_painted')
for j in range(2):
 for k in range(3):add(f'link{j}_member{k}','BODY_ROOT',f'link{j}_member{k}',role='member',index=j*3+k)
 for end in range(2):
  for kind in ['outer_yoke','inner_yoke','cross']:
   add(f'link{j}_joint{end}_{kind}','BODY_ROOT','cross' if kind=='cross' else 'yoke',role=kind,index=j*2+end)
markers=[dict(name=m['name'],parent=m['parent'],position=vec(m['position']),rotation=quat(m['rotationQuaternionXYZW'])) for m in C['markers'] if m['name'] in ['BODY_ROOT','ENGINE_CRANK_REAR','ENGINE_CRANK_FRONT','FRONT_DRIVE_OUTPUT'] or m['name'].startswith(('TRUCK_','AXLE_','BEVEL_','REAR_LINESHAFT','FRONT_LINESHAFT'))]
recipe=dict(meshes=meshes,parts=parts,markers=markers,revision=C['revision'],sha256=HASH)
(ROOT/'unity/Assets/Transmission/recipe.json').write_text(json.dumps(recipe,separators=(',',':')))
lodCounts=[sum(len(next(m for m in meshes if m['name']==p['mesh']+f'_LOD{l}')['triangles'])//3 for p in parts) for l in range(3)]
metadata=dict(status='ENGINEERING_STUDY_PENDING_FIT_OWNER',contractRevision=C['revision'],contractSha256=HASH,units='metres',axisConvention='Unity +X axle, +Z shaft. All scales +1. OBJ local spin axis +Z.',gear=C['bevelGears'],parts=parts,markers=markers,triangleCounts=lodCounts,frontReverser='owned by engine session; output clock -engineTurns at FRONT_DRIVE_OUTPUT',kinematics=dict(memberLength=MEMBER,jointEndAllowance=ALLOW,ideal='wheelTurns = -10/43 * engineTurns about +X',rear='lineTurns = engineTurns + Cardan residual',front='lineTurns = -engineTurns + Cardan residual',rearWheel='-10/43 * rearLineTurns',frontWheel='10/43 * frontLineTurns',loopEngineTurns=43,loopWheelTurns=-10,warning='Exact constant ratio and exact Hooke joints are incompatible at unequal compound bend angles; preview follows the Hooke joints, wheels follow their own truck gears.'),squareLinks=dict(memberLengths=[MEMBER,MEMBER],halfWidths=[SQ*.70,SQ*.85,SQ],sleeveInnerHalfWidths=[SQ*.70+.001,SQ*.85+.001],perFaceClearance=.001,minimumEngagement=.08,shoulderAllowance=.015,jointEndAllowance=ALLOW,engagementRange=[(3*MEMBER-(hi-2*ALLOW))/2 for _,hi in LENGTHS]+[(3*MEMBER-(lo-2*ALLOW))/2 for lo,_ in LENGTHS]),simplification='Straight tapered trapezoidal bevel teeth on common-apex cones; not conjugate spherical involutes, not certified manufacturing or loaded-contact geometry. R2 face scale .905..1.095, tooth normal heights -.010..+.014 m (cast, visible). Adapter is an annular spacer, fastening details unresolved pending native face audit.',nativeGeometryIncluded=False)
(ROOT/'kinematic-markers.json').write_text(json.dumps(metadata,indent=2))
(ROOT/'unity/ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 2019.4.40f1\n')
(ROOT/'unity/Packages/manifest.json').write_text(json.dumps({'dependencies':{'com.unity.modules.imageconversion':'1.0.0','com.unity.modules.jsonserialize':'1.0.0'}}))
print(json.dumps(dict(parts=len(parts),uniqueMeshes=len(meshes),triangleCounts=lodCounts,sha256=HASH)))
assert lodCounts[0]<=22000
