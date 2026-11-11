"""Independent numeric evidence. Reads native fixtures privately; exports no native geometry."""
from pathlib import Path
import json,math,itertools
import numpy as np
R=Path(__file__).resolve().parents[1];C=json.loads((R.parent/'shared/fit-contract.json').read_text())
recipe=json.loads((R/'unity/Assets/Transmission/recipe.json').read_text());meshes={m['name']:m for m in recipe['meshes']}
# R2: member length and yoke allowance come from this module's own kinematic output.
K=json.loads((R/'kinematic-markers.json').read_text())['kinematics'];MEMBER=K['memberLength'];ALLOW=K['jointEndAllowance'];APEX=C['bevelGears']['coneApexX']
def vv(m):return np.array([[v[k] for k in 'xyz'] for v in m['vertices']])
def rot(x=0,y=0,z=0):
 x,y,z=np.radians([x,y,z]);cx,sx,cy,sy,cz,sz=math.cos(x),math.sin(x),math.cos(y),math.sin(y),math.cos(z),math.sin(z)
 return np.array([[cy,0,sy],[0,1,0],[-sy,0,cy]])@np.array([[1,0,0],[0,cx,-sx],[0,sx,cx]])@np.array([[cz,-sz,0],[sz,cz,0],[0,0,1]])
def unit(v):return v/np.linalg.norm(v)
def wrap(t):return (t+.5)%1-.5
def solve(j,pose,turn):
 yaw,pitch,roll,dy=pose;M=rot(pitch,yaw,roll);s=C['transmissionStudy'][j];pivot=np.array(C['trucks'][j]['pivot']);a=np.array(s['bodyEnd']);b=M@(np.array(s['truckEndNeutral'])-pivot)+pivot+[0,dy,0]
 d=unit(b-a)*(1 if j else -1);theta=(turn if j==0 else -turn)*math.tau;u=np.array([math.cos(theta),math.sin(theta),0]);v=unit(np.cross(d,u));w=unit(np.cross(v,M[:,2]));inp=turn if j==0 else -turn
 principal=math.atan2(w@M[:,1],w@M[:,0])/math.tau;out=inp+wrap(principal-inp)
 bend=max(math.degrees(math.acos(np.clip(d@[0,0,1],-1,1))),math.degrees(math.acos(np.clip(d@M[:,2],-1,1))))
 overlap=(3*MEMBER-(np.linalg.norm(b-a)-2*ALLOW))/2
 return dict(output=out,error=(out-inp)*360,bend=bend,engagement=overlap,length=float(np.linalg.norm(b-a)),crossOrthogonality=max(abs(u@v),abs(w@v)),u=u,v=v,w=w,a=a,b=b,d=d)
poses=[(q,0,0,0) for q in range(-12,13)]+list(itertools.product([-12,0,12],[-1,1],[-1,1],[-.02,.02]))
records=[];summaries=[]
for j in range(2):
 values=[];maxError=0;worst=None;seam=0;orth=0
 for pose in poses:
  for turn in np.linspace(-1,1,721):
   s=solve(j,pose,float(turn));orth=max(orth,s['crossOrthogonality'])
   if abs(s['error'])>maxError:maxError=abs(s['error']);worst=dict(pose=pose,engineTurns=float(turn),phaseErrorDegrees=s['error'])
  s=solve(j,pose,0);t=solve(j,pose,43);seam=max(seam,float(np.linalg.norm(s['u']-t['u'])),float(np.linalg.norm(s['v']-t['v'])),abs(wrap((t['output']-s['output'])/(43/10))))
  rec=dict(truck=C['trucks'][j]['name'],pose=list(pose),engagement=s['engagement'],jointAngle=s['bend'],length=s['length']);records.append(rec);values.append(rec)
 summaries.append(dict(truck=C['trucks'][j]['name'],minEngagement=min(s['engagement'] for s in values),maxEngagement=max(s['engagement'] for s in values),maxJointAngle=max(s['jointAngle'] for s in values),maxPhaseErrorDegrees=maxError,worstPhase=worst,maxCrossDot=orth,seam43EngineTurnsError=seam,phaseSamples=49*721))
print(json.dumps(summaries,indent=2),flush=True)

def intersects(a,bs):
 ea=np.roll(a,-1,axis=0)-a;eb=np.roll(bs,-1,axis=1)-bs;na=np.cross(ea[0],ea[1]);nb=np.cross(eb[:,0],eb[:,1]);axes=[np.broadcast_to(na,nb.shape),nb]+[np.cross(v,eb[:,j]) for v in ea for j in range(3)]+[np.broadcast_to(np.cross(na,v),nb.shape) for v in ea]+[np.cross(nb,eb[:,j]) for j in range(3)]
 ok=np.ones(len(bs),bool)
 for axis in axes:
  pa=a@axis.T;pb=np.einsum('nvc,nc->nv',bs,axis);ok&=(pa.max(0)>=pb.min(1)-1e-10)&(pb.max(1)>=pa.min(0)-1e-10)
 return np.flatnonzero(ok)
def contact(a,b):
 lo=b.min(1);hi=b.max(1)
 for idx,t in enumerate(a):
  ii=np.flatnonzero(np.all(hi>=t.min(0),1)&np.all(lo<=t.max(0),1))
  if len(ii):
   hit=intersects(t,b[ii])
   if len(hit):return [idx,int(ii[hit[0]])]
 return None
def triangles(name,M=np.eye(3),offset=np.zeros(3)):
 m=meshes[name];return (vv(m)@M.T+offset)[np.array(m['triangles']).reshape(-1,3)]
def frame(x,z):return np.column_stack([x,unit(np.cross(z,x)),z])
def part_world(p,poses2,turn):
 pos=np.array(list(p['position'].values()));Q=rot(*p['euler'].values());j=p['index'];role=p['role']
 marker=next(m for m in recipe['markers'] if m['name']==p['parent']);parentpos=np.array(list(marker['position'].values()))
 if p['parent'].startswith('TRUCK_'):
  j0=0 if 'REAR' in p['parent'] else 1;pose=poses2[j0];P=rot(pose[1],pose[0],pose[2]);parentpos=parentpos+[0,pose[3],0]
 else:P=np.eye(3)
 if role=='outlet':Q=rot(z=turn*360*(1 if j==0 else -1))
 if role in ['line','wheel','pinion']:
  j=j if role=='line' else j//2;s=solve(j,poses2[j],turn);out=s['output'];ang=out*360
  Q=(rot(x=ang*(-1 if j==0 else 1)/(43/10)) if role=='wheel' else rot(z=ang))@Q
 if role=='member':
  j,k=divmod(p['index'],3);s=solve(j,poses2[j],turn);direction=unit(s['b']-s['a']);length=MEMBER;return frame(s['v'],direction),s['a']+direction*(ALLOW+k*(length-s['engagement']))
 if role in ['outer_yoke','inner_yoke','cross']:
  j,end=divmod(p['index'],2);s=solve(j,poses2[j],turn);pose=poses2[j];T=rot(pose[1],pose[0],pose[2]);tr=s['u'] if end==0 else s['w'];direction=unit(s['b']-s['a'])
  if role=='cross':Q=np.column_stack([tr,s['v'],np.cross(tr,s['v'])])
  elif role=='inner_yoke':Q=frame(s['v'],direction*(-1 if end==0 else 1))
  else:Q=frame(tr,(np.array([0,0,1]) if end==0 else T[:,2])*(-1 if j==0 else 1)*(1 if end==0 else -1))
  return Q,s['a'] if end==0 else s['b']
 return P@Q,P@pos+parentpos
gearTests=[]
for j in range(2):
 for k in range(39):
  t=k/38/38 # one tooth-pitch cycle with endpoint
  shaft=t;wheel=shaft*(-1 if j==0 else 1)/(43/10)
  ring=triangles('ring_LOD0',rot(x=wheel*360)@rot(y=90))
  pinion=triangles('pinion_LOD0',rot(z=shaft*360)@rot(y=0 if j==0 else 180,z=180/38))
  hit=contact(ring,pinion);gearTests.append(dict(truck=j,shaftTurns=t,contact=hit))
print('gear interpenetrating samples',sum(x['contact'] is not None for x in gearTests),'/',len(gearTests),flush=True)
# Check native face/boss using actual private fixture, no files copied.
native=json.loads(Path('B:/GitHub repo/rearranged-s060/build/qa/conventional-native.json').read_text());nm={m['name']:m for m in native['meshes']}
nativeTests=[]
for i,alias in enumerate(C['wheelReuse']['nativeMeshAliases']):
 m=nm[alias];v=vv(m)*[1,.8,.8];t=v[np.array(m['triangles']).reshape(-1,3)]
 for kind in ['ring','adapter']:
  authored=triangles(kind+'_LOD0',rot(y=90),[APEX,0,0]);hit=contact(authored,t)
  # Whole author AABB vs triangle AABBs: conservative lower bound, possibly 0.
  gap=np.maximum(np.maximum(t.min(1)-authored.max((0,1)),authored.min((0,1))-t.max(1)),0)
  nativeTests.append(dict(axle=i+1,alias=alias,part=kind,intersection=hit,aabbDistanceLowerBound=float(np.linalg.norm(gap,axis=1).min())))
# Frozen radial/axial envelope validation for authored gear meshes, independent of visual rendering.
bounds={}
for kind in ['ring','pinion','adapter']:
 v=vv(meshes[kind+'_LOD0']);bounds[kind]=dict(axialMin=float(v[:,2].min()),axialMax=float(v[:,2].max()),radialMin=float(np.linalg.norm(v[:,:2],axis=1).min()),radialMax=float(np.linalg.norm(v[:,:2],axis=1).max()))
report=dict(status='STUDY_NOT_RELEASED',kinematics=summaries,poses=records,gearMeshContactSamples=gearTests,nativeWheelContact=nativeTests,meshBounds=bounds,perFaceSquareClearanceMetres=.0007,shaftToBearingRadialClearanceMetres=.0015,pinionBoreToShaftClearanceMetres=.001,notes=['Contact means triangle surface intersection, not a certified loaded contact or penetration depth.','Native mounting test uses common wheel/gear co-rotation, so relative shape does not vary with wheel phase.','No exact whole-kit continuous minimum clearance claimed.','Unequal Cardan angles break exact ideal crank/wheel phase relation.'])
(R/'evidence/engineering-validation.json').write_text(json.dumps(report,indent=2))
print(json.dumps(dict(bounds=bounds,native=nativeTests),indent=2),flush=True)
