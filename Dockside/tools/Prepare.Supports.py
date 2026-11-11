"""Local-only S060 native support positions for offline frame-pose evidence."""
import argparse, json
from pathlib import Path
import UnityPy

p = argparse.ArgumentParser()
p.add_argument('--game', required=True)
p.add_argument('--output', required=True)
a = p.parse_args()
source = Path(a.game) / 'DerailValley_Data/resources.assets'
env = UnityPy.load(str(source))
car = next(o.read() for o in env.objects if o.type.name == 'GameObject' and o.peek_name() == 'LocoS060')
root = next(c.component.read() for c in car.m_Component if c.component.type.name == 'Transform')
supports = []
for pointer in root.m_Children:
    t = pointer.read()
    name = t.m_GameObject.read().m_Name
    if name not in ('Axle_F', 'Axle_R'):
        continue
    q = t.m_LocalRotation
    assert abs(q.x) + abs(q.y) + abs(q.z) < 1e-6 and abs(q.w - 1) < 1e-6
    assert abs(t.m_LocalScale.x - 1) + abs(t.m_LocalScale.y - 1) + abs(t.m_LocalScale.z - 1) < 1e-6
    supports.append(dict(name=name, position=dict(x=t.m_LocalPosition.x, y=t.m_LocalPosition.y, z=t.m_LocalPosition.z), pathId=pointer.path_id))
assert len(supports) == 2
out = Path(a.output)
out.parent.mkdir(parents=True, exist_ok=True)
out.write_text(json.dumps(dict(source=str(source), rootPathId=root.object_reader.path_id, resourceBytes=source.stat().st_size, resourceMtimeNs=source.stat().st_mtime_ns, supports=supports), indent=2), encoding='utf-8')
print('Measured native S060 supports:', supports)
