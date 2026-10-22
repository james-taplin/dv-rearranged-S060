using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Author-owned castings and frame parts. Native wheels/body keep their aliases.
public static class SteamBogieVisuals
{
    public static void LiveMaterial(MeshRenderer renderer)
    {
        var alias = renderer.GetComponent<CCL.Types.Components.MaterialGrabberRenderer>();
        if (alias == null) alias = renderer.gameObject.AddComponent<CCL.Types.Components.MaterialGrabberRenderer>();
        alias.RenderersToAffect = new Renderer[] { renderer };
        alias.Replacements = new[] { new CCL.Types.Components.MaterialGrabberRenderer.IndexToName { RendererIndex = 0, ReplacementName = MeyerPlan.BodyMaterial } };
    }
    public static bool IsCylinderHousing(Renderer renderer, Transform cylinder)
    {
        for (var part = renderer.transform; part != null && part != cylinder; part = part.parent)
            if (part.name == "driveshaft_f" || part.name == "u_joint_mapped") return false;
        return renderer.transform.IsChildOf(cylinder);
    }
    public static void HideFranklinHousing(Transform cylinder)
    {
        foreach (var renderer in cylinder.GetComponentsInChildren<Renderer>(true).Where(r => IsCylinderHousing(r, cylinder)).ToArray())
        {
            // Retain the animation datums, but do not export the old casting.
            var alias = renderer.GetComponent<CCL.Types.Components.MaterialGrabberRenderer>();
            if (alias != null) Object.DestroyImmediate(alias);
            var filter = renderer.GetComponent<MeshFilter>();
            if (filter != null) Object.DestroyImmediate(filter);
            Object.DestroyImmediate(renderer);
        }
    }
    public static Material Metal(string folder, string name, Color color, float metallic, float gloss)
    {
        string path = folder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(material, path); }
        material.color = color; material.SetFloat("_Metallic", metallic); material.SetFloat("_Glossiness", gloss); material.enableInstancing = true;
        EditorUtility.SetDirty(material); return material;
    }
    public static Transform Part(Transform parent, string name, PrimitiveType kind, Vector3 at, Vector3 size, Material material)
    {
        var go = GameObject.CreatePrimitive(kind); go.name = name; go.transform.SetParent(parent, false);
        go.transform.localPosition = at; go.transform.localScale = size; go.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(go.GetComponent<Collider>()); return go.transform;
    }
    public static Transform Group(Transform parent, string name)
    { var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform; }
    private static Transform Cylinder(Transform parent, string name, Vector3 at, float radius, float length, Material material)
    {
        var part = Part(parent, name, PrimitiveType.Cylinder, at, new Vector3(2 * radius, length / 2, 2 * radius), material);
        part.localRotation = Quaternion.Euler(90, 0, 0); return part;
    }
    public static void Cylinders(Transform frame, float radius, float axisY, float z, string folder, float bore)
    {
        var iron = Metal(folder, "CastIron", new Color(.095f, .105f, .11f), .55f, .28f);
        var steel = Metal(folder, "MachinedSteel", new Color(.32f, .34f, .35f), .8f, .55f);
        var brass = Metal(folder, "CockBrass", new Color(.38f, .27f, .10f), .7f, .45f);
        foreach (int side in new[] { -1, 1 })
        {
            string suffix = side < 0 ? "L" : "R";
            var root = Group(frame, "[Round cylinders " + suffix + "]"); float x = side * 1.075f;
            Cylinder(root, "Steam cylinder barrel", new Vector3(x, axisY, z), radius, .60f, iron);
            foreach (int end in new[] { -1, 1 })
            {
                Cylinder(root, end < 0 ? "Rear cylinder flange" : "Front cylinder flange", new Vector3(x, axisY, z + end * .30f), radius + .025f, .055f, iron);
                Cylinder(root, end < 0 ? "Rear cylinder cover" : "Front cylinder cover", new Vector3(x, axisY, z + end * .337f), radius - .012f, .025f, steel);
                for (int bolt = 0; bolt < 10; bolt++)
                {
                    float a = bolt * 2 * Mathf.PI / 10;
                    Cylinder(root, "Cover bolt " + end + " " + bolt, new Vector3(x + Mathf.Cos(a) * radius * .85f, axisY + Mathf.Sin(a) * radius * .85f, z + end * .357f), .014f, .018f, iron);
                }
                // Cock outlet is directly below each end of this cylinder.
                var cock = Part(root, "Cylinder cock " + end, PrimitiveType.Cylinder,
                    new Vector3(x, axisY - radius - .06f, z + end * .20f), new Vector3(.045f, .06f, .045f), brass);
                cock.localRotation = Quaternion.identity;
            }
            Cylinder(root, "Piston rod gland", new Vector3(x, axisY, z - .39f), .065f, .10f, steel);
            float valveRadius = Mathf.Min(.12f, radius * .48f);
            Cylinder(root, "Valve chest", new Vector3(x - side * .045f, axisY + radius + valveRadius * .66f, z), valveRadius, .49f, iron);
            Cylinder(root, "Valve chest front cover", new Vector3(x - side * .045f, axisY + radius + valveRadius * .66f, z + .26f), valveRadius + .015f, .035f, steel);
            Part(root, "Cylinder saddle", PrimitiveType.Cube, new Vector3(side * .73f, axisY + radius * .5f, z), new Vector3(.36f, .16f, .48f), iron);
        }
    }
    public static void Frame(Transform unit, float pitch, float radius, string folder)
    {
        var metal = Metal(folder, "BogieFrame", new Color(.075f, .085f, .09f), .45f, .24f);
        var spring = Metal(folder, "SpringSteel", new Color(.18f, .19f, .20f), .65f, .35f);
        var root = Group(unit, "[Powered bogie frame]");
        foreach (int side in new[] { -1, 1 })
        {
            Part(root, "Main frame rail " + side, PrimitiveType.Cube, new Vector3(side * .64f, radius + .20f, 0), new Vector3(.12f, .23f, pitch + .74f), metal);
            Part(root, "Visible upper frame edge " + side, PrimitiveType.Cube, new Vector3(side * .93f, radius + .33f, 0), new Vector3(.07f, .12f, pitch + .32f), metal);
            foreach (int axle in new[] { -1, 1 })
            {
                float z = axle * pitch / 2;
                Part(root, "Axle horn " + side + " " + axle, PrimitiveType.Cube, new Vector3(side * .64f, radius, z), new Vector3(.18f, .35f, .24f), metal);
                for (int leaf = 0; leaf < 4; leaf++)
                    Part(root, "Leaf spring " + side + " " + axle + " " + leaf, PrimitiveType.Cube,
                        new Vector3(side * .91f, radius + .41f + leaf * .018f, z), new Vector3(.10f, .016f, .65f - leaf * .09f), spring);
            }
        }
        foreach (float z in new[] { -pitch / 2 - .28f, 0, pitch / 2 + .28f })
            Part(root, "Frame stretcher " + z, PrimitiveType.Cube, new Vector3(0, radius + .25f, z), new Vector3(1.37f, .16f, .12f), metal);
        Part(root, "Bogie pivot bolster", PrimitiveType.Cylinder, new Vector3(0, radius + .49f, 0), new Vector3(.58f, .12f, .58f), metal);
    }
    public static void PositionEffects(Transform frame, float axisY, float cylinderZ, float radius)
    {
        foreach (var part in frame.GetComponentsInChildren<Transform>(true))
        {
            float x = Mathf.Sign(part.localPosition.x) * 1.075f;
            if (part.name.StartsWith("Cock front ")) part.localPosition = new Vector3(x, axisY - radius - .10f, cylinderZ + .20f);
            if (part.name.StartsWith("Cock rear ")) part.localPosition = new Vector3(x, axisY - radius - .10f, cylinderZ - .20f);
            if (part.name.StartsWith("Water drip ") || part.name.StartsWith("Cock audio ")) part.localPosition = new Vector3(x, axisY - radius - .11f, cylinderZ);
        }
    }
}
