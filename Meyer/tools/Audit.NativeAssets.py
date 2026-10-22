"""Read stock asset names locally; export no mesh, texture or audio content."""
import argparse
import json
from pathlib import Path
import UnityPy

parser = argparse.ArgumentParser()
parser.add_argument("--game", default=r"B:\SteamLibrary\steamapps\common\Derail Valley")
parser.add_argument("--output", required=True)
args = parser.parse_args()
source = Path(args.game) / "DerailValley_Data" / "resources.assets"
env = UnityPy.load(str(source))
found = {"Mesh": [], "Material": [], "AnimationClip": []}
for obj in env.objects:
    kind = obj.type.name
    if kind not in found:
        continue
    name = obj.peek_name() if hasattr(obj, "peek_name") else obj.read().m_Name
    if name and ("s060" in name.lower() or name == "s282_wheels_front"):
        found[kind].append({"name": name, "pathId": obj.path_id})
renderer_materials = []
for obj in env.objects:
    if obj.type.name != "MeshRenderer":
        continue
    renderer = obj.read()
    try:
        name = renderer.m_GameObject.read().m_Name
        if name not in {"s060_body", "s060_Wheels_02", "s060_cab"}:
            continue
        renderer_materials.append({"object": name, "materials": [p.read().m_Name for p in renderer.m_Materials]})
    except (AttributeError, KeyError):
        continue
report = {"source": str(source), "exportedGameContent": False, "assets": found, "rendererMaterials": renderer_materials}
output = Path(args.output)
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
for kind, values in found.items():
    print(kind + ": " + str(len(values)) + " named assets")
print("Renderer materials: " + json.dumps(renderer_materials))
