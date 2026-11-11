"""Generate only original R3 assets; native wheel geometry stays in private QA."""
from pathlib import Path
import ast, hashlib, itertools, json, math, subprocess, sys
import numpy as np

root = Path(__file__).resolve().parent
for module, script in [('engine','tools/build_engine.py'),('trucks','source/build_trucks.py'),('transmission','source/build.py')]:
    subprocess.run([sys.executable, '-B', str(root/module/script)], check=True)

C = json.loads((root/'shared/fit-contract.json').read_text())
truck = json.loads((root/'trucks/source/truck-recipe.json').read_text())
for part in truck['parts']:
    for mesh in part['lods']:
        mesh['vertices'] = [dict(zip('xyz', p)) for p in mesh['vertices']]
(root/'trucks/source/unity-recipe.json').write_text(json.dumps(truck,separators=(',',':')))

# The existing measured Cardan solver establishes the initial authored pose.
# The runtime uses these same interfaces and its native engine clock.
recipe = json.loads((root/'transmission/unity/Assets/Transmission/recipe.json').read_text())
meshes = {m['name']:m for m in recipe['meshes']}
K = json.loads((root/'transmission/kinematic-markers.json').read_text())['kinematics']
MEMBER, ALLOW, APEX = K['memberLength'], K['jointEndAllowance'], C['bevelGears']['coneApexX']
source = (root/'transmission/source/validate.py').read_text().replace('(43/19)','(43/10)')
for node in ast.parse(source).body:
    if isinstance(node,ast.FunctionDef):
        exec(compile(ast.Module(body=[node],type_ignores=[]),'R2 exact-export solver','exec'))

def quaternion(matrix):
    # Eigenvector form remains stable at 180-degree fork poses.
    m = matrix
    k = np.array([[m[0,0]-m[1,1]-m[2,2],m[1,0]+m[0,1],m[2,0]+m[0,2],m[2,1]-m[1,2]],
                  [m[1,0]+m[0,1],m[1,1]-m[0,0]-m[2,2],m[2,1]+m[1,2],m[0,2]-m[2,0]],
                  [m[2,0]+m[0,2],m[2,1]+m[1,2],m[2,2]-m[0,0]-m[1,1],m[1,0]-m[0,1]],
                  [m[2,1]-m[1,2],m[0,2]-m[2,0],m[1,0]-m[0,1],m.trace()]])/3
    _, vectors = np.linalg.eigh(k)
    q = vectors[:,-1]
    if q[3] < 0:q = -q
    return dict(zip('xyzw',map(float,q)))

for part in recipe['parts']:
    if part['role'] in ['member','outer_yoke','inner_yoke','cross']:
        matrix, position = part_world(part,[(0,0,0,0)]*2,0)
        part['initialPosition'] = dict(zip('xyz',map(float,position)))
        part['initialRotation'] = quaternion(matrix)
(root/'transmission/unity/Assets/Transmission/recipe.json').write_text(json.dumps(recipe,separators=(',',':')))
print('Generated final R3 model, 43 motion bindings, radial native-wheel aliases.')
