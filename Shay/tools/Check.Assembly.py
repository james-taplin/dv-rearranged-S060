"""Read original R2 modules in place; never export DV geometry.

Checks the previously separate transmission against the finished truck frames.
The original module audits cover body, native wheels, bevels and joint forks.
This test adds the missing cross-module triangle contacts and mounting seats.
"""
from pathlib import Path
import ast, itertools, json, math, sys
import numpy as np

source = Path(sys.argv[1])
output = Path(sys.argv[2])
C = json.loads((source / 'shared/fit-contract.json').read_text())
recipe = json.loads((source / 'transmission/unity/Assets/Transmission/recipe.json').read_text())
meshes = {m['name']: m for m in recipe['meshes']}
# R2: the borrowed solver reads member length, yoke allowance and apex as globals.
K = json.loads((source / 'transmission/kinematic-markers.json').read_text())['kinematics']
MEMBER, ALLOW, APEX = K['memberLength'], K['jointEndAllowance'], C['bevelGears']['coneApexX']
for node in ast.parse((source / 'transmission/source/validate.py').read_text()).body:
    if isinstance(node, ast.FunctionDef):
        exec(compile(ast.Module(body=[node], type_ignores=[]), 'R1 solver', 'exec'))
trucks = json.loads((source / 'trucks/source/truck-recipe.json').read_text())
nodes = {n['name']: n for n in trucks['nodes']}
def position(name):
    return np.zeros(3) if name == 'ROOT' else np.array(nodes[name]['position']) + position(nodes[name]['parent'])
def under(name, parent):
    return name == parent or (name != 'ROOT' and under(nodes[name]['parent'], parent))
frames = []
for j, name in enumerate(['TRUCK_REAR', 'TRUCK_FRONT']):
    group = []
    for p in trucks['parts']:
        if not under(p['parent'], name): continue
        m = p['lods'][0]
        v = np.array(m['vertices']) + position(p['parent'])
        group.append((p['name'], v[np.array(m['triangles']).reshape(-1,3)]))
    frames.append(group)

failures, seats, checks = [], [], 0
poses = [(q,0,0,0) for q in range(-12,13)] + list(itertools.product([-12,0,12],[-1,1],[-1,1],[-.02,.02]))
for j in range(2):
    pivot = np.array(C['trucks'][j]['pivot'])
    for pose in poses:
        T = rot(pose[1], pose[0], pose[2])
        world = [(name, (tri-pivot)@T.T+pivot+[0,pose[3],0]) for name,tri in frames[j]]
        for turn in np.arange(8)/8:
            pp = [(0,0,0,0)]*2; pp[j] = pose
            for p in recipe['parts']:
                belongs = p['parent'] == ['TRUCK_REAR_PIVOT','TRUCK_FRONT_PIVOT'][j]
                moving = (p['role']=='member' and p['index']//3==j) or (p['role'] in ['outer_yoke','inner_yoke','cross'] and p['index']//2==j)
                if not (belongs or moving): continue
                tri = triangles(p['mesh']+'_LOD0', *part_world(p,pp,float(turn)))
                lo,hi = tri.min((0,1)),tri.max((0,1))
                for name,other in world:
                    candidate = other[np.all(other.max(1)>=lo,1)&np.all(other.min(1)<=hi,1)]
                    if not len(candidate): continue
                    hit = contact(tri,candidate); checks += 1
                    if hit:
                        # Fixed hangers are deliberately bolted onto truck castings.
                        item=dict(part=p['name'],frame=name,pose=pose,turn=float(turn))
                        (seats if p['role']=='fixed' else failures).append(item)
    print('Truck',j,'contacts checked',checks,'unexpected',len(failures),flush=True)
for hanger in ['bearing_hanger_'+str(i) for i in range(1,5)]:
    coverage = {(tuple(s['pose']),s['turn']) for s in seats if s['part']==hanger}
    if len(coverage) != len(poses)*8:
        failures.append(dict(type='unsupported_hanger', part=hanger, supportedStates=len(coverage), requiredStates=len(poses)*8))
output.parent.mkdir(parents=True,exist_ok=True)
output.write_text(json.dumps(dict(passed=not failures,checks=checks,failures=failures,
    fixedSeatContacts=seats,scope='49 poses x eight phases per truck; original LOD0 triangle surfaces; no continuous sweep'),indent=2))
if failures: sys.exit(1)
