using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// S060 mounting of the S282 Franklin presentation. All motion is baked into
// DocksideDrive; no independent clock or runtime mechanism component is added.
public static class DocksideDriveDetail
{
    public const float CrankInside = 1.123f, CrankOutside = 1.128f;
    public static Mesh Save(string folder, Mesh mesh)
    {
        string path = folder + "/" + mesh.name + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) { AssetDatabase.CreateAsset(mesh, path); existing = mesh; }
        else { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); }
        EditorUtility.SetDirty(existing); return existing;
    }
    public static Transform Part(Transform parent, string name, Mesh mesh, Material material, bool nativeMetal = true)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
        if (nativeMetal) SteamBogieVisuals.LiveMaterial(renderer);
        return go.transform;
    }
    public static float GearboxFace(Transform unit, Transform gear, int side)
    {
        var box = gear.Find("transmission_box/default");
        var bounds = box.GetComponent<MeshFilter>().sharedMesh.bounds;
        float face = float.PositiveInfinity;
        foreach (float x in new[] { bounds.min.x, bounds.max.x })
            foreach (float y in new[] { bounds.min.y, bounds.max.y })
                foreach (float z in new[] { bounds.min.z, bounds.max.z })
                    face = Mathf.Min(face, side * unit.InverseTransformPoint(box.TransformPoint(new Vector3(x, y, z))).x);
        return face;
    }
    public static void Fit(Transform unit, Transform supports, Transform gear, string suffix, int side, string folder, Material material)
    {
        float face = GearboxFace(unit, gear, side);
        if (face - CrankOutside < .0008f) throw new InvalidOperationException("Franklin input plate has no measured gearbox clearance.");
        // Native S060 wheel01 retains its original shoulder/pin. The 5 mm
        // plate fits the measured gap outside that shoulder and inside the box;
        // changing gauge, native wheel meshes or shaft-joint datums is unnecessary.
        var axle = new Vector3(0, DocksidePlan.DriverRadius, -DocksidePlan.AxlePitch / 2);
        var crankRoot = new GameObject("Franklin return crank " + suffix).transform;
        crankRoot.SetParent(unit, false); crankRoot.localPosition = axle; crankRoot.localScale = new Vector3(side, 1, 1);
        var crankMesh = DocksideDetailMesh.Link("DocksideDetailCrank", DocksidePlan.CrankRadius, .052f, .025f, .067f, .042f, .0025f, .0025f);
        var points = crankMesh.vertices; for (int i = 0; i < points.Length; i++) points[i].x += (CrankInside + CrankOutside) / 2;
        crankMesh.vertices = points; crankMesh.RecalculateBounds();
        Part(crankRoot, "Wheel crank", Save(folder, crankMesh), material);
        Part(crankRoot, "Hub sleeve", Save(folder, DocksideDetailMesh.Tube("DocksideDetailHubSleeve", .028f, .017f, CrankInside, CrankOutside + .0005f)), material);
        Part(crankRoot, "Input shaft", Save(folder, DocksideDetailMesh.Tube("DocksideDetailInputShaft", .020f, .017f, CrankOutside, face + .003f)), material);
        var collar = Part(supports, "Gearbox input bearing collar", Save(folder, DocksideDetailMesh.Tube("DocksideDetailInputCollar", .039f, .021f, face - .0004f, face + .003f)), material);
        collar.localPosition = axle; collar.localScale = new Vector3(side, 1, 1);
    }
}
