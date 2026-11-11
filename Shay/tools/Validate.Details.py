"""Bounded geometry checks for the visible bogies and new rigid front equipment."""
from pathlib import Path
import ast,itertools,json,math,sys
import numpy as np
R=Path('Shay/ModelSource');C=json.loads((R/'shared/fit-contract.json').read_text())
for node in ast.parse((R/'transmission/source/validate.py').read_text()).body:
 if isinstance(node,ast.FunctionDef):exec(compile(ast.Module(body=[node],type_ignores=[]),'proven exact triangle reader','exec'))
T=json.loads((R/'trucks/source/truck-recipe.json').read_text());nodes={n['name']:n for n in T['nodes']}
def pos(n):return np.zeros(3) if n=='ROOT' else np.array(nodes[n]['position'])+pos(nodes[n]['parent'])
def under(n,p):return n==p or (n!='ROOT' and under(nodes[n]['parent'],p))
def geometry(p,lod):
 m=p['lods'][lod];return (np.array(m['vertices'])+pos(p['parent']))[np.array(m['triangles']).reshape(-1,3)]
def vectors(v):return np.array([[p[k] for k in 'xyz'] for p in v])
def candidate(a,b):
 lo,hi=a.min((0,1)),a.max((0,1));return b[np.all((body_hi if b is body else b.max(1))>=lo,1)&np.all((body_lo if b is body else b.min(1))<=hi,1)]
body=None;checks=0;failures=[]
def clear(a,b,label,**details):
 global checks
 checks+=1;b=candidate(a,b)
 if len(b):
  hit=contact(a,b)
  if hit:failures.append(dict(label=label,**details,contact=hit))
N=json.loads(Path('build/qa/s282-pony-native.json').read_text())['meshes'][0]
v=vectors(N['vertices'])*C['wheelReuse']['scale'];ix=np.array(N['triangles']).reshape(-1,3)
for lod in [0,1]:
 for phase in range(8):
  wheel=(v@rot(x=phase*45).T)[ix]
  for ai,z in enumerate(C['wheelStations']):
   w=wheel+[0,C['wheelAxisY'],z]
   truck='TRUCK_'+C['trucks'][ai//2]['name'].upper()
   for p in T['parts']:
    if not under(p['parent'],truck):continue
    clear(geometry(p,lod),w,'native wheel versus bogie',part=p['name'],axle=ai,lod=lod,phase=phase)
print('Native wheel checks',checks,'contacts',len(failures),flush=True)
B=json.loads(Path(sys.argv[1]).read_text());body=vectors(B['vertices'])[np.array(B['triangles']).reshape(-1,3)]
# Native clipping leaves collapsed faces with sub-micrometre duplicate edges.
body=body[np.linalg.norm(np.roll(body,-1,axis=1)-body,axis=2).min(1)>2e-6]
body_lo,body_hi=body.min(1),body.max(1)
rigid=[(p,geometry(p,0)) for p in T['parts'] if under(p['parent'],'SHAY_FRAME_DETAILS')]
seats=[]
def below(tri,limit):
 out=[]
 for polygon in tri:
  result=[]
  for i,a in enumerate(polygon):
   b=polygon[(i+1)%3];ia=a[1]<=limit;ib=b[1]<=limit
   if ia:result.append(a)
   if ia!=ib:result.append(a+(b-a)*(limit-a[1])/(b[1]-a[1]))
  for i in range(1,len(result)-1):out.append([result[0],result[i],result[i+1]])
 return np.array(out)
for p,tri in rigid:
 if any(s in p['name'] for s in ['BODY_pump','BODY_compressor','BODY_service_','BODY_outer_chassis','BODY_chassis_']):
  if 'mounting_knee' in p['name'] :
   checks+=1;hit=contact(tri,candidate(tri,body))
   if hit is None:failures.append(dict(label='unseated equipment mount',part=p['name']))
   else:seats.append(p['name'])
   clear(below(tri,.968),body,'equipment below its bounded mounting seat',part=p['name'])
  else:clear(tri,body,'new equipment versus native body',part=p['name'])
for p,tri in rigid:
 if 'BODY_service_support_foot_' in p['name']:
  checks+=1
  frame=next(t for q,t in rigid if q['name']=='BODY_chassis_channel_lip_(1, 0.8, 1.235)')
  if contact(tri,candidate(tri,frame)) is None:failures.append(dict(label='unseated pipe-support foot',part=p['name']))
  else:seats.append(p['name'])
for p,tri in rigid:
 if 'BODY_service_support_stay_' in p['name']:
  for q,fixed in rigid:
   if q['name'] in ['BODY_pump_air_barrel','BODY_pump_steam_barrel']:clear(tri,fixed,'pipe stay versus pump casting',part=p['name'],fixed=q['name'])
print('Rigid equipment checks' ,checks,'contacts',len(failures),flush=True)
body=body[body.min(1)[:,1]<1.7];body_lo,body_hi=body.min(1),body.max(1)
rigid_lo=np.array([t.min((0,1)) for p,t in rigid]);rigid_hi=np.array([t.max((0,1)) for p,t in rigid])
poses=[(q,0,0,0) for q in range(-12,13)]+list(itertools.product([-12,0,12],[-1,1],[-1,1],[-.02,.02]))
for j in range(2):
 pivot=np.array(C['trucks'][j]['pivot']);name='TRUCK_'+C['trucks'][j]['name'].upper()
 parts=[(p,geometry(p,0)) for p in T['parts'] if under(p['parent'],name)]
 for pose in poses:
  matrix=rot(pose[1],pose[0],pose[2])
  for p,tri in parts:
   transformed=(tri-pivot)@matrix.T+pivot+[0,pose[3],0]
   clear(transformed,body,'bogie articulation versus native body',part=p['name'],truck=j,pose=pose)
   lo,hi=transformed.min((0,1)),transformed.max((0,1))
   for ri in np.flatnonzero(np.all(rigid_hi>=lo,1)&np.all(rigid_lo<=hi,1)):
    other,fixed=rigid[ri]
    clear(transformed,fixed,'bogie articulation versus rigid equipment',part=p['name'],fixed=other['name'],truck=j,pose=pose)
 print('Truck',j,'detail checks',checks,'contacts',len(failures),flush=True)
out=Path(sys.argv[2]);out.parent.mkdir(parents=True,exist_ok=True);out.write_text(json.dumps(dict(passed=not failures,checks=checks,failures=failures,seatedMounts=seats,scope='native wheels: two LODs/eight phases; body/equipment: 49 sampled poses per truck; collapsed native clipping edges under two micrometres excluded',gameplay=False),indent=2));print('Failures',failures[:12],flush=True)
raise SystemExit(bool(failures))
