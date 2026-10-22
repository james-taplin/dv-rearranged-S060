"""Inspect native pony hierarchy/rotator references; export no game content."""
import argparse
import json
import struct
from pathlib import Path
import UnityPy

parser = argparse.ArgumentParser()
parser.add_argument("--game", default=r"B:\SteamLibrary\steamapps\common\Derail Valley")
parser.add_argument("--output", required=True)
args = parser.parse_args()
source = Path(args.game) / "DerailValley_Data" / "resources.assets"
env = UnityPy.load(str(source))

def name(pointer):
    if not pointer or not pointer.path_id:
        return None
    return pointer.read().m_Name

def path(transform):
    names = []
    for _ in range(32):
        names.append(name(transform.m_GameObject))
        if not transform.m_Father.path_id:
            break
        transform = transform.m_Father.read()
    return "/".join(reversed(names))

axles = []
rotators = []
for obj in env.objects:
    if obj.type.name == "MeshFilter":
        mesh = obj.read()
        if not mesh.m_Mesh.path_id or name(mesh.m_Mesh) != "s282_wheels_front":
            continue
        go = mesh.m_GameObject.read()
        transform = next(c.component.read() for c in go.m_Component if c.component.deref().type.name == "Transform")
        axle = transform
        while not name(axle.m_GameObject).startswith("[axle]") and axle.m_Father.path_id:
            axle = axle.m_Father.read()
        components = []
        stack = [axle]
        while stack:
            current = stack.pop()
            current_go = current.m_GameObject.read()
            for c in current_go.m_Component:
                reader = c.component.deref()
                kind = reader.type.name
                if kind == "MonoBehaviour":
                    behavior = reader.read(check_read=False)
                    kind += ":" + behavior.m_Script.read().m_ClassName
                components.append(kind)
            stack.extend(c.read() for c in current.m_Children)
        axles.append({"meshPath": path(transform), "axlePath": path(axle),
                      "axleTransformId": axle.object_reader.path_id, "components": components})
    elif obj.type.name == "MonoBehaviour":
        behavior = obj.read(check_read=False)
        if not behavior.m_Script.path_id:
            continue
        if behavior.m_Script.read().m_ClassName != "WheelRotationViaCode":
            continue
        try:
            tree = obj.read_typetree()
            rotators.append({"object": name(behavior.m_GameObject), "fields": tree})
        except Exception as exc:
            # B99 strips custom type trees. Verified managed fields in order:
            # WheelRotationBase: float radius, bool affectedByWheelSlide;
            # WheelRotationViaCode: Axis enum, Transform[] (PPtr fileId/pathId).
            # Refuse other layouts rather than guessing offsets.
            raw = obj.get_raw_data()
            header = len(raw) - 28
            if header not in (32, 36, 40):
                raise ValueError("Unexpected native wheel rotator layout") from exc
            radius, slide, axis, count, file_id, path_id = struct.unpack_from("<f?3xiii q".replace(" ", ""), raw, header)
            if count != 1 or file_id != 0:
                raise ValueError("Expected one local transform reference")
            rotators.append({"object": name(behavior.m_GameObject), "wheelRadius": radius,
                             "affectedByWheelSlide": slide, "rotationAxis": axis,
                             "transformId": path_id, "verifiedManagedFieldLayout": True})

output = Path(args.output)
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(json.dumps({"source": str(source), "exportedGameContent": False,
                              "axles": axles, "rotators": rotators}, indent=2) + "\n", encoding="utf-8")
print(json.dumps({"axles": axles, "rotators": len(rotators), "report": str(output)}, indent=2))
