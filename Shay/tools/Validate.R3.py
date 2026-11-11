"""Bounded offline checks on exact original exports, never source AABBs as contact proof."""
from pathlib import Path
import argparse, ast, hashlib, itertools, json, math
import numpy as np

parser=argparse.ArgumentParser()
parser.add_argument('--source',required=True)
parser.add_argument('--output',required=True)
args=parser.parse_args()
R=Path(args.source)
C=json.loads((R/'shared/fit-contract.json').read_text())
recipe=json.loads((R/'transmission/unity/Assets/Transmission/recipe.json').read_text())
meshes={m['name']:m for m in recipe['meshes']}
K=json.loads((R/'transmission/kinematic-markers.json').read_text())['kinematics']
MEMBER,ALLOW,APEX=K['memberLength'],K['jointEndAllowance'],C['bevelGears']['coneApexX']
source=(R/'transmission/source/validate.py').read_text().replace('(43/19)','(43/10)')
for node in ast.parse(source).body:
    if isinstance(node,ast.FunctionDef):exec(compile(ast.Module(body=[node],type_ignores=[]),'R2 exact-export reader','exec'))
mesh_vertices={name:vv(m) for name,m in meshes.items()}
mesh_indices={name:np.array(m['triangles']).reshape(-1,3) for name,m in meshes.items()}
def triangles(name,M=np.eye(3),offset=np.zeros(3)):
    return (mesh_vertices[name]@M.T+offset)[mesh_indices[name]]

checks=0
failures=[]
def check(value,label,**evidence):
    global checks
    checks+=1
    if not value:failures.append(dict(check=label,**evidence))

check(len(recipe['parts'])==43,'43 transmission bindings')
gear_samples=[]
for lod in [0,1]:
    for j in range(2):
        for k in range(41):
            turn=k/40/20
            ring=triangles('ring_LOD'+str(lod),rot(x=turn*(-1 if j==0 else 1)/4.3*360)@rot(y=90))
            pinion=triangles('pinion_LOD'+str(lod),rot(z=turn*360)@rot(y=0 if j==0 else 180,z=9))
            hit=contact(ring,pinion)
            gear_samples.append(dict(lod=lod,truck=j,sample=k,contact=hit))
            check(hit is None,'gear pair surface clearance',lod=lod,truck=j,sample=k,contact=hit)

poses=[(q,0,0,0) for q in range(-12,13)]+list(itertools.product([-12,0,12],[-1,1],[-1,1],[-.02,.02]))
kinematics=[]
for j in range(2):
    for pose in poses:
        a=solve(j,pose,0);b=solve(j,pose,43)
        check(a['engagement']>=.08,'minimum telescopic engagement',truck=j,pose=pose,engagement=a['engagement'])
        check(a['engagement']<=MEMBER-.015,'telescopic retraction stop',truck=j,pose=pose,engagement=a['engagement'])
        check(abs((b['output']-a['output'])/4.3-(10 if j==0 else -10))<1e-9,'43 crank turns / 10 wheel turns',truck=j,pose=pose)
        kinematics.append(dict(truck=j,pose=pose,length=a['length'],engagement=a['engagement'],jointAngle=a['bend']))

# Read complete native pony geometry without changing production Read/Write.
native_path=Path(__file__).resolve().parents[2]/'build/qa/s282-pony-native.json'
native=json.loads(native_path.read_text())
native_mesh=next(m for m in native['meshes'] if m['name']=='s282_wheels_front')
v=vv(native_mesh)*C['wheelReuse']['scale'];nt=v[np.array(native_mesh['triangles']).reshape(-1,3)]
native_contacts=[]
for lod in [0,1]:
    for kind in ['ring','adapter']:
        authored=triangles(kind+'_LOD'+str(lod),rot(y=90),[APEX,0,0]);hit=contact(authored,nt)
        check(hit is None,'native wheel / gear surface clearance',lod=lod,part=kind,contact=hit)
        native_contacts.append(dict(lod=lod,part=kind,contact=hit))
pinion=vv(meshes['pinion_LOD0'])
shaft_margin=float(np.linalg.norm(pinion[:,:2],axis=1).min())-.05
check(shaft_margin>=.002,'pinion / 100 mm shaft clearance',margin=shaft_margin)

# Four universal joints: the two forks must clear each other and the gears.
# Their trunnion contacts with the cross itself are deliberate bearing seats.
for lod in [0,1]:
    for j in range(2):
        joint_parts=[p for p in recipe['parts'] if p['role'] in ['outer_yoke','inner_yoke'] and p['index']//2==j]
        gear_parts=[p for p in recipe['parts'] if p['role'] in ['wheel','pinion'] and p['index']//2==j]
        for pose in poses:
            pp=[(0,0,0,0)]*2;pp[j]=pose
            for turn in np.arange(8)/8:
                joint_meshes=[(p,triangles(p['mesh']+'_LOD'+str(lod),*part_world(p,pp,float(turn)))) for p in joint_parts]
                gear_meshes=[(p,triangles(p['mesh']+'_LOD'+str(lod),*part_world(p,pp,float(turn)))) for p in gear_parts]
                for end in [0,1]:
                    outer=next(t for p,t in joint_meshes if p['role']=='outer_yoke' and p['index']%2==end)
                    inner=next(t for p,t in joint_meshes if p['role']=='inner_yoke' and p['index']%2==end)
                    hit=contact(outer,inner)
                    check(hit is None,'joint fork clearance',lod=lod,truck=j,end=end,pose=pose,turn=float(turn),contact=hit)
                for part,jt in joint_meshes:
                    for gear,gt in gear_meshes:
                        if not(np.all(jt.max((0,1))>=gt.min((0,1))) and np.all(gt.max((0,1))>=jt.min((0,1)))):continue
                        hit=contact(jt,gt)
                        check(hit is None,'joint / gear clearance',lod=lod,truck=j,part=part['name'],gear=gear['name'],pose=pose,turn=float(turn),contact=hit)

# Actual engine triangles at the authored pivots, on both detail levels.
# Contacts at the front/back guide bearing faces are intentional engagements.
engine=json.loads((R/'engine/unity/Assets/EngineStudy/engine-model.json').read_text())
for lod in engine['lods']:
    groups={g['name']:g for g in lod['groups']}
    def engine_triangles(part,turn):
        def transform(name):
            if name=='ROOT':return np.eye(3),np.zeros(3)
            g=groups[name];position=np.array(g['position']);angle=g['rotationZ']
            if name=='CRANK':angle=-360*turn
            elif name=='FRONT_REVERSED':angle=360*turn+9
            elif name.startswith(('ROD_','CROSSHEAD_','VALVE_VISUAL_ONLY_')):
                i=g['cylinder'];a=math.tau*(turn+i/3);x=.15*math.sin(a);y=.15*math.cos(a);dx=-.065-x;dy=math.sqrt(.9*.9-dx*dx);z=(i-1)*.35
                if name.startswith('ROD_'):position=np.array([x,y,z-.035]);angle=math.degrees(math.atan2(-dx,dy))
                elif name.startswith('CROSSHEAD_'):position=np.array([-.065,y+dy,z]);angle=0
                else:position=np.array([C['engine']['valveAxisX'],1.22+.015*math.sin(a+math.pi/2),z]);angle=0
            parent,offset=transform(g['parent']);return parent@rot(z=angle),parent@position+offset
        matrix,offset=transform(part['group']);vertices=np.array(part['vertices']).reshape(-1,3)@matrix.T+offset
        return vertices[np.array(part['triangles']).reshape(-1,3)]
    stationary=[(p,engine_triangles(p,0)) for p in lod['parts'] if p['group']=='FIXED']
    moving=[p for p in lod['parts'] if p['group']!='FIXED']
    for turn in np.arange(24)/24:
        for p in moving:
            tri=engine_triangles(p,float(turn));lo=tri.min((0,1));hi=tri.max((0,1))
            for other,bt in stationary:
                if p['name'].startswith('Crosshead_cheek_') and other['name'].startswith('Guide_('):continue
                if not (np.all(bt.max((0,1))>=lo) and np.all(bt.min((0,1))<=hi)):continue
                hit=contact(tri,bt)
                check(hit is None,'engine moving / fixed surface clearance',lod=lod['lod'],turn=float(turn),moving=p['name'],fixed=other['name'],contact=hit)
out=Path(args.output);out.parent.mkdir(parents=True,exist_ok=True)
report=dict(passed=not failures,checks=checks,failures=failures,contractSha256=hashlib.sha256((R/'shared/fit-contract.json').read_bytes()).hexdigest(),gearSamples=gear_samples,kinematics=kinematics,nativeWheelContacts=native_contacts,pinionShaftMargin=shaft_margin,gameplay=False)
out.write_text(json.dumps(report,indent=2))
print(json.dumps(dict(passed=report['passed'],checks=checks,failures=failures[:8],failureCount=len(failures),pinionShaftMargin=shaft_margin)))
raise SystemExit(0 if report['passed'] else 1)
