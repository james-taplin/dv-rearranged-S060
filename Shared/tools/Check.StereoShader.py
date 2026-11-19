"""Fail unless every Standard shader in the given bundle has single-pass stereo variants."""
import json, sys
import UnityPy

def standard_shaders(path):
    found = []
    for obj in UnityPy.load(path).objects:
        if obj.type.name != "Shader":
            continue
        tree = obj.read_typetree()
        name = tree.get("m_ParsedForm", {}).get("m_Name") or tree.get("m_Name")
        text = repr(tree)
        found.append({"name": name, "singlePassStereo": "UNITY_SINGLE_PASS_STEREO" in text})
    return found

if __name__ == "__main__":
    bundle, report = sys.argv[1], sys.argv[2]
    shaders = standard_shaders(bundle)
    passed = len(shaders) == 1 and shaders[0]["name"] == "Standard" and shaders[0]["singlePassStereo"]
    with open(report, "w", encoding="utf-8") as f:
        json.dump({"bundle": bundle, "shaders": shaders, "passed": passed}, f, indent=1)
    print(json.dumps(shaders))
    sys.exit(0 if passed else 1)
