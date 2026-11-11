using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CCL.Types.Components;
using RearrangedS060;
using UnityEngine;

// Transient preview objects only, created after bundle export/audit. Native
// geometry/textures are read from local QA evidence and never saved as assets.
public static class SteamNativePreview
{
    [Serializable] private class Data { public Entry[] meshes = Array.Empty<Entry>(); }
    [Serializable] private class Entry { public string name = ""; public Vector3[] vertices = Array.Empty<Vector3>(), normals = Array.Empty<Vector3>(); public Vector2[] uv = Array.Empty<Vector2>(); public int[] triangles = Array.Empty<int>(); }
    public static void RodSources(Transform body, List<UnityEngine.Object> owned)
    {
        var data = JsonUtility.FromJson<Data>(File.ReadAllText(Environment.GetEnvironmentVariable("S060_NATIVE_PREVIEW")));
        foreach (var entry in data.meshes.Where(m => m.name == "s060_mech_wheels_connect" || m.name == "s060_mech_push_rod_to_connect"))
        {
            var mesh = new Mesh { name = entry.name, vertices = entry.vertices, uv = entry.uv, triangles = entry.triangles };
            if (entry.normals.Length == entry.vertices.Length) mesh.normals = entry.normals; else mesh.RecalculateNormals();
            mesh.RecalculateBounds(); owned.Add(mesh);
            var go = new GameObject(entry.name + " (hidden native source QA)"); go.transform.SetParent(body, false); go.SetActive(false); owned.Add(go);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
        }
    }
    public static bool Apply(GameObject clone, List<UnityEngine.Object> owned)
    {
        string path = Environment.GetEnvironmentVariable("S060_NATIVE_PREVIEW");
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
        var data = JsonUtility.FromJson<Data>(File.ReadAllText(path));
        var texture = new Texture2D(2, 2); texture.LoadImage(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(path), "native-preview-atlas.png"))); owned.Add(texture);
        var material = new Material(Shader.Find("Standard")); material.mainTexture = texture; material.SetFloat("_Metallic", .35f); material.SetFloat("_Glossiness", .26f); owned.Add(material);
        bool attachedRear = clone.transform.Find("BogieR/bogie_car/[Attached rear platform]") != null;
        foreach (var entry in data.meshes)
        {
            var mesh = new Mesh { name = entry.name + " (local QA only)", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.vertices = entry.vertices; mesh.uv = entry.uv;
            bool Removed(Vector3 p) => (attachedRear && NativeRearLamp(p)) || (Mathf.Abs(p.x) >= .77f && p.y <= 1.57f && p.z >= 2.55f && p.z <= 3.7f) ||
                (Mathf.Abs(p.x) >= .41f && Mathf.Abs(p.x) <= 1f && p.y <= .8215f && ((p.z >= 2.16f && p.z <= 2.59f) || (p.z >= .6f && p.z <= 1.31f) || (p.z >= -.91f && p.z <= -.57f)));
            var triangles = new List<int>();
            for (int i = 0; i < entry.triangles.Length; i += 3)
                if (entry.name != "s060_body" || !(Removed(entry.vertices[entry.triangles[i]]) && Removed(entry.vertices[entry.triangles[i + 1]]) && Removed(entry.vertices[entry.triangles[i + 2]])))
                { triangles.Add(entry.triangles[i]); triangles.Add(entry.triangles[i + 1]); triangles.Add(entry.triangles[i + 2]); }
            mesh.triangles = triangles.ToArray(); mesh.RecalculateNormals(); mesh.RecalculateBounds(); owned.Add(mesh);
            foreach (var alias in clone.GetComponentsInChildren<MeshGrabberFilter>(true).Where(a => a.ReplacementName == entry.name))
            { alias.Filter.sharedMesh = mesh; if (entry.name == "s060_body") alias.Filter.gameObject.name = "s060_body"; }
        }
        foreach (var alias in clone.GetComponentsInChildren<MaterialGrabberRenderer>(true))
            foreach (var renderer in alias.RenderersToAffect) if (renderer != null) renderer.sharedMaterial = material;
        // SDK plate-anchor Cubes are editor placeholders, not locomotive
        // panels. The radio copies only bogie frames onto the existing S060,
        // whose real identity plates stay native. Showing these Cubes produced
        // a false green "beam" at X=1.345/Y=1.01/Z=-3.231 in the QA image.
        foreach (var renderer in clone.GetComponentsInChildren<Renderer>(true).Where(r =>
            r.name == "DummyPlate" && r.transform.parent != null && r.transform.parent.name.StartsWith("[car plate anchor", StringComparison.Ordinal)))
            renderer.enabled = false;
        var nativeFront = clone.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "[Native S060 cylinders]");
        if (nativeFront != null)
        {
            var body = data.meshes.Single(e => e.name == "s060_body");
            foreach (int side in new[] { -1, 1 })
            {
                var selected = body.triangles.Where((j, i) =>
                {
                    int first = i - i % 3;
                    return Enumerable.Range(first, 3).All(k => NativeCylinder(body.vertices[body.triangles[k]], side));
                }).ToArray();
                var unique = selected.Distinct().ToArray(); var map = unique.Select((j, i) => new { j, i }).ToDictionary(p => p.j, p => p.i);
                var mesh = new Mesh { name = "Native S060 cylinder (local QA only)" };
                mesh.vertices = unique.Select(j => body.vertices[j]).ToArray(); mesh.uv = unique.Select(j => body.uv[j]).ToArray();
                mesh.triangles = selected.Select(j => map[j]).ToArray(); mesh.RecalculateNormals(); mesh.RecalculateBounds(); owned.Add(mesh);
                var go = new GameObject("Native S060 full-size cylinder QA " + side); go.transform.SetParent(nativeFront, false);
                go.transform.localPosition = new Vector3(0, 0, MeyerPlan.CylinderZ - S060PipeGeometry.NativeCylinderCentreZ);
                go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = material;
            }
            foreach (string side in new[] { "L", "R" })
                foreach (var renderer in nativeFront.parent.Find("[Round cylinders " + side + "]").GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        }
        if (attachedRear) RearEndPreview(clone, data, material, owned, path);
        return true;
    }
    private static bool NativeRearLamp(Vector3 p) => Mathf.Abs(p.x) >= .8f && Mathf.Abs(p.x) <= 1.25f && p.y >= 1.25f && p.y <= 1.79f && p.z < -3.9f;
    private static void RearEndPreview(GameObject clone, Data data, Material material, List<UnityEngine.Object> owned, string path)
    {
        var rear = clone.transform.Find("BogieR/bogie_car");
        var native = JsonUtility.FromJson<Data>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(path), "meyer-lights-native.json")));
        Mesh Part(Entry entry, Func<Vector3, bool> selected, Vector3 offset)
        {
            var triangles = new List<int>();
            for (int i = 0; i < entry.triangles.Length; i += 3)
                if (selected(entry.vertices[entry.triangles[i]]) && selected(entry.vertices[entry.triangles[i + 1]]) && selected(entry.vertices[entry.triangles[i + 2]]))
                { triangles.Add(entry.triangles[i]); triangles.Add(entry.triangles[i + 1]); triangles.Add(entry.triangles[i + 2]); }
            var unique = triangles.Distinct().ToArray(); var map = unique.Select((j, i) => new { j, i }).ToDictionary(p => p.j, p => p.i);
            var mesh = new Mesh { name = entry.name + " rear-end local QA" };
            mesh.vertices = unique.Select(j => entry.vertices[j] + offset).ToArray();
            if (entry.uv.Length == entry.vertices.Length) mesh.uv = unique.Select(j => entry.uv[j]).ToArray();
            mesh.triangles = triangles.Select(j => map[j]).ToArray(); mesh.RecalculateNormals(); mesh.RecalculateBounds(); owned.Add(mesh); return mesh;
        }
        Transform Draw(string name, Mesh mesh, Material surface, Transform parent)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = surface; return go.transform;
        }
        Draw("Native rear lamp casings QA", Part(data.meshes.Single(m => m.name == "s060_body"), NativeRearLamp,
            new Vector3(0, 0, MeyerPlan.RearLampGlassZ - MeyerPlan.NativeRearLampGlassZ)), material, rear);
        var iron = new Material(Shader.Find("Standard")); iron.color = new Color(.07f, .08f, .075f); iron.SetFloat("_Metallic", .6f); owned.Add(iron);
        Draw("Native rear lamp glass QA", Part(native.meshes.Single(m => m.name == "s060_headlight_glass_R_LO"), p => true,
            new Vector3(0, 0, MeyerPlan.RearLampGlassZ - MeyerPlan.NativeRearLampGlassZ)), iron, rear);
        var stems = native.meshes.Single(m => m.name == "s060_buffer_stems");
        float flange = stems.vertices.Where(p => p.z < 0).Max(p => p.z);
        Draw("Native rear buffer stems QA", Part(stems, p => p.z < 0,
            new Vector3(0, 0, MeyerPlan.RearBeamCentreZ + MeyerPlan.RearBeamDepth / 2 - flange)), iron, rear);
        var headMesh = Part(native.meshes.Single(m => m.name == "Buffer04"), p => true, Vector3.zero);
        var rig = new GameObject("Native rear buffer rig QA").transform; rig.SetParent(rear, false);
        rig.localPosition = new Vector3(0, 1.05f, -1.9517f); rig.localRotation = Quaternion.Euler(0, 180, 0);
        foreach (int side in new[] { -1, 1 })
        {
            var head = Draw("Native rear buffer head QA " + side, headMesh, iron, rig);
            head.localPosition = new Vector3(side * (.864f - .0109f), -.0356f, .3f - .07175f);
        }
    }
    public static bool NativeCylinder(Vector3 p, int side) => p.x * side >= .77f && p.x * side <= 4 && p.y >= 0 && p.y <= 1.57f && p.z >= 2.55f && p.z <= 3.7f;
    public static Transform CreatePipeFixture(Transform parent, List<UnityEngine.Object> owned)
    {
        string path = Environment.GetEnvironmentVariable("S060_NATIVE_PREVIEW");
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) throw new InvalidOperationException("Actual native S060 mesh fixture required for pipe validation.");
        var entry = JsonUtility.FromJson<Data>(File.ReadAllText(path)).meshes.Single(m => m.name == "s060_body");
        var mesh = new Mesh { name = "Native boiler pipe fixture (local QA only)", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.vertices = entry.vertices; mesh.uv = entry.uv; mesh.triangles = entry.triangles; mesh.RecalculateBounds(); owned.Add(mesh);
        var go = new GameObject("s060_body"); go.transform.SetParent(parent, false); go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var surface = new Material(Shader.Find("Standard")); owned.Add(surface);
        go.AddComponent<MeshRenderer>().sharedMaterial = surface;
        return go.transform;
    }
}
