"""Copy and adapt the proven R2 authoring recipe; never modify LLW originals."""
from pathlib import Path
import hashlib,json,math,shutil,subprocess,sys
repo=Path(__file__).resolve().parents[2]
original=Path('B:/LLW CONVERT/locos/s060-shay')
root=repo/'build/physics-20261111/regear-source'
root.mkdir(parents=True,exist_ok=True)
for relative in ['shared/fit-contract.json','transmission/source/build.py','transmission/source/validate.py','trucks/source/truck-recipe.json']:
    dst=root/relative;dst.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(original/relative,dst)
provenance={p:hashlib.sha256((original/p).read_bytes()).hexdigest() for p in ['shared/fit-contract.json','transmission/source/build.py','transmission/source/validate.py','trucks/source/truck-recipe.json']}
contract=json.loads((root/'shared/fit-contract.json').read_text())
old=contract['bevelGears']['coneApexX'];pin=.3*20/86;apex=.9+pin
def move(value):
    if isinstance(value,dict):return {k:move(v) for k,v in value.items()}
    if isinstance(value,list):return [move(v) for v in value]
    return apex if isinstance(value,(int,float)) and abs(value-old)<1e-10 else value
contract=move(contract);contract['revision']='S060-SHAY-FIT-R3-20261111-86-20'
g=contract['bevelGears'];g.update(teeth=[86,20],pinionPitchRadius=pin,reduction=4.3,module=.6/86,
    coneAnglesDegrees=[math.degrees(math.atan(4.3)),math.degrees(math.atan(1/4.3))],
    note='86:20 physical bevel gears; same native wheels and ring pitch plane; private R3 authoring copy')
(root/'shared/fit-contract.json').write_text(json.dumps(contract,indent=2))
p=root/'transmission/source/build.py';s=p.read_text()
replacements={
 "assert C['revision']=='S060-SHAY-FIT-R2-20261027'":"assert C['revision']=='S060-SHAY-FIT-R3-20261111-86-20'",
 "assert C['bevelGears']['teeth']==[43,19]":"assert C['bevelGears']['teeth']==[86,20]",
 "[.905,1.095]":"[.94,1.06]",
 "[(-.022,a),(-.022,b),(-.010,b),(-.010,a)]":"[(-.008,a),(-.008,b),(-.004,b),(-.004,a)]",
 "[(-.010,-1,.46),(-.010,1,.46),(.014,1,.24),(.014,-1,.24)]":"[(-.004,-1,.46),(-.004,1,.46),(.006,1,.24),(.006,-1,.24)]",
 "if ring:m.tube(r+.02,r-.06,-axial-.045,-axial-.018":"if ring:m.tube(r+.02,r-.06,-axial-.032,-axial-.008",
 "m.tube(r-.03,SHAFT+.002,-axial-.040,-axial-.018":"m.tube(r+.006,SHAFT+.002,-axial-.040,-axial-.018",
 "m.tube(SHAFT+.03,SHAFT+.002,-axial-.075,-axial+.02,16)":"m.tube(SHAFT+.008,SHAFT+.002,-axial-.075,-axial+.02,12)",
 "[('ring',RING,PIN,43,True),('pinion',PIN,RING,19,False)]":"[('ring',RING,PIN,86,True),('pinion',PIN,RING,20,False)]",
 "DISC_X+.0001-APEX,-PIN-.045":"DISC_X+.0001-APEX,-PIN-.032",
 "-PIN-.050,-PIN-.040":"-PIN-.037,-PIN-.027",
 "180/19":"180/20",
 "-19/43":"-10/43",
 "19/43 * frontLineTurns":"10/43 * frontLineTurns",
 "loopWheelTurns=-19":"loopWheelTurns=-10",
 "R2 face scale .905..1.095, tooth normal heights -.010..+.014":"R3 face scale .94..1.06, tooth normal heights -.004..+.006"
}
for a,b in replacements.items():
    if a not in s:raise RuntimeError('Expected proven source not found: '+a)
    s=s.replace(a,b)
p.write_text(s)
p=root/'transmission/source/validate.py';s=p.read_text().replace('(43/19)','(43/10)').replace('180/38','180/20').replace('k/38/38','k/20/40');p.write_text(s)
for d in ['transmission/unity/Assets/Transmission','transmission/unity/ProjectSettings','transmission/unity/Packages']:(root/d).mkdir(parents=True,exist_ok=True)
subprocess.run([sys.executable,str(root/'transmission/source/build.py')],check=True)
for relative,before in provenance.items():
    if hashlib.sha256((original/relative).read_bytes()).hexdigest()!=before:raise RuntimeError('Original source changed: '+relative)
(root/'source-provenance.json').write_text(json.dumps(dict(originalSources=provenance,originalsUnchanged=True,
    copiedContractSha256=hashlib.sha256((root/'shared/fit-contract.json').read_bytes()).hexdigest()),indent=2))
print(root)
