using System;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using CCL.Types.Components;
using RearrangedS060;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SteamPipeAuthoring
{
    [Serializable] private class Sources { public string sourceSha256 = ""; public SourceMesh[] meshes = Array.Empty<SourceMesh>(); }
    [Serializable] private class SourceMesh { public string name = ""; public Vector3[] vertices = Array.Empty<Vector3>(); public Vector2[] uv = Array.Empty<Vector2>(); public int[] triangles = Array.Empty<int>(); }
    private static Sources readable;
    public static void Apply(Transform front, Transform rear, string folder, float axisY, float cylinderZ, float frontRadius, float rearRadius, bool nativeFront = false)
    {
        string path = Environment.GetEnvironmentVariable("DOCKSIDE_FRANKLIN_BUNDLE");
        if (string.IsNullOrEmpty(path)) path = @"B:\SteamLibrary\steamapps\common\Derail Valley\Mods\RearrangedS282\assets\s282_drivetrain";
        readable = JsonUtility.FromJson<Sources>(File.ReadAllText(Environment.GetEnvironmentVariable("S060_PIPE_SOURCE")));
        using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path))
            if (BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "") != readable.sourceSha256) throw new InvalidOperationException("Readable pipe sources do not match the S282 source bundle.");
        foreach (var unit in new[] { front, rear })
            if (unit.Find("[S060 fitted gear 0.6]") == null)
            { var marker = new GameObject("[S060 fitted gear 0.6]"); marker.transform.SetParent(unit, false); }
        var bundle = AssetBundle.LoadFromFile(path);
        if (bundle == null) throw new InvalidOperationException("Existing S282 branch-pipe source bundle is missing.");
        try
        {
            var source = bundle.LoadAsset<GameObject>("branch_pipe");
            if (source == null) throw new InvalidOperationException("S282 branch_pipe prefab is missing.");
            foreach (int side in new[] { -1, 1 })
            {
                string suffix = side < 0 ? "L" : "R";
                foreach (bool isFront in new[] { false, true })
                {
                    var unit = isFront ? front : rear; float radius = isFront ? frontRadius : rearRadius;
                    if (!(nativeFront && isFront))
                        foreach (var chest in unit.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Valve chest"))
                        {
                            // The two existing manifold feet must sit on the
                            // actual casting, including their complete flanges.
                            var size = chest.localScale; size.y = .36f; chest.localScale = size;
                            var cover = chest.parent.Find("Valve chest front cover");
                            if (cover != null) { var position = cover.localPosition; position.z = cylinderZ + .375f; cover.localPosition = position; }
                        }
                    float valve = Mathf.Min(.12f, radius * .48f);
                    float lower = axisY + radius + valve * 1.66f;
                    if (nativeFront && isFront) lower = S060PipeGeometry.NativeCylinderLowerY;
                    var original = source.transform.Find(isFront ? "intake_manifold_f" : "intake_manifold_r");
                    var filter = original.GetComponentInChildren<MeshFilter>();
                    if (filter.sharedMesh.name != "default") throw new InvalidOperationException("Unexpected non-custom manifold source.");
                    var mesh = ReadableCopy(filter.sharedMesh, isFront ? "intake_manifold_f" : "intake_manifold_r");
                    var v = mesh.vertices;
                    for (int i = 0; i < v.Length; i++) v[i] = S060PipeGeometry.Manifold(source.transform.InverseTransformPoint(filter.transform.TransformPoint(v[i])), isFront, side, lower, cylinderZ);
                    mesh.vertices = v; if (side < 0) ReverseWinding(mesh); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                    string name = "SteamManifold" + (isFront ? "Front" : "Rear") + suffix;
                    var part = Create(unit, "[S060 manifold " + suffix + "]", Save(mesh, folder, name), folder);
                    Marker(part, "[lower datum]", new Vector3(side * 1.03f, lower, cylinderZ));
                    var savedMesh = part.GetComponent<MeshFilter>().sharedMesh;
                    Marker(part, "[transfer socket]", FaceCentre(savedMesh, isFront ? Vector3.back : Vector3.forward));
                    if (isFront)
                    {
                        Vector3 normal = new Vector3(-side * .406736f, .913545f, 0);
                        var cap = Marker(part, "[boiler datum]", FaceCentre(savedMesh, normal));
                        cap.localRotation = Quaternion.FromToRotation(Vector3.up, normal);
                    }
                }
                var pipeSource = source.transform.Find("dry_pipe_new").GetComponentInChildren<MeshFilter>().sharedMesh;
                if (pipeSource.name != "default") throw new InvalidOperationException("Unexpected non-custom dry-pipe source.");
                var pipe = ReadableCopy(pipeSource, "dry_pipe_new"); var points = pipe.vertices;
                for (int i = 0; i < points.Length; i++) points[i] = new Vector3(side * points[i].x * S060PipeGeometry.Radial, points[i].y * S060PipeGeometry.Radial, points[i].z - S060PipeGeometry.PipeMinZ);
                pipe.vertices = points; if (side < 0) ReverseWinding(pipe); pipe.RecalculateNormals(); pipe.RecalculateBounds();
                var span = Create(front, "[S060 transfer pipe " + suffix + "]", Save(pipe, folder, "SteamTransfer" + suffix), folder);
                var a = rear.Find("[S060 manifold " + suffix + "]/[transfer socket]").position;
                var b = front.Find("[S060 manifold " + suffix + "]/[transfer socket]").position;
                span.position = a; span.rotation = Quaternion.LookRotation(b - a, front.up);
                // Preview the exact production collar-preserving fit. Runtime
                // starts from the saved source mesh, never this fitted copy.
                span.localScale = new Vector3(1, 1, Vector3.Distance(a, b) / S060PipeGeometry.PipeLength);
            }
        }
        finally { bundle.Unload(true); }
    }
    private static Mesh Save(Mesh source, string folder, string name)
    {
        string path = folder + "/" + name + ".asset";
        var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (saved == null) { source.name = name; AssetDatabase.CreateAsset(source, path); saved = source; }
        else { EditorUtility.CopySerialized(source, saved); saved.name = name; Object.DestroyImmediate(source); }
        // Use a dark native S060 atlas patch, rather than spreading the entire
        // boiler/cab atlas across each small custom casting.
        var uv = saved.uv;
        for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(.6868f, .8948f) + (uv[i] - Vector2.one * .5f) * .012f;
        saved.uv = uv; saved.RecalculateTangents(); EditorUtility.SetDirty(saved); return saved;
    }
    private static Mesh ReadableCopy(Mesh source, string key)
    {
        // S282's static bundle discards CPU access. Use its hash-verified custom
        // source data (exported by Prepare.BranchPipeSources.py), not a runtime
        // GPU readback, extra DLL or hidden copy of a native game asset.
        var data = readable.meshes.Single(m => m.name == key);
        if (data.vertices.Length != source.vertexCount || data.triangles.Length == 0) throw new InvalidOperationException("Custom pipe source data is incomplete.");
        var copy = new Mesh { name = source.name, indexFormat = source.indexFormat };
        copy.vertices = data.vertices; copy.uv = data.uv; copy.triangles = data.triangles;
        copy.RecalculateNormals(); copy.RecalculateBounds(); return copy;
    }
    private static Transform Create(Transform parent, string name, Mesh mesh, string folder)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = SteamBogieVisuals.Metal(folder, "SteamPipePreview", new Color(.14f, .15f, .14f), .55f, .35f);
        SteamBogieVisuals.LiveMaterial(renderer); return go.transform;
    }
    private static Transform Marker(Transform parent, string name, Vector3 position)
    { var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = position; return go.transform; }
    public static Vector3 FaceCentre(Mesh mesh, Vector3 normal, Vector3? socketPlane = null)
    {
        var vertices = mesh.vertices; var triangles = mesh.triangles; Vector3 centre = Vector3.zero; float area = 0;
        // A fitted boiler course can pass behind the transfer socket. The
        // authored socket plane identifies the cap without a global-extreme guess.
        float plane = socketPlane.HasValue ? Vector3.Dot(socketPlane.Value, normal) : vertices.Max(p => Vector3.Dot(p, normal));
        for (int i = 0; i < triangles.Length; i += 3)
        {
            var a = vertices[triangles[i]]; var b = vertices[triangles[i + 1]]; var c = vertices[triangles[i + 2]];
            var cross = Vector3.Cross(b - a, c - a); float size = cross.magnitude;
            if (size < 1e-8f || Vector3.Dot(cross / size, normal) < .9999f ||
                Mathf.Abs(Vector3.Dot(a, normal) - plane) > .0002f || Mathf.Abs(Vector3.Dot(b, normal) - plane) > .0002f || Mathf.Abs(Vector3.Dot(c, normal) - plane) > .0002f) continue;
            centre += (a + b + c) * (size / 3); area += size;
        }
        if (area < .001f) throw new InvalidOperationException("Actual manifold socket cap was not found.");
        return centre / area;
    }
    private static void ReverseWinding(Mesh mesh)
    {
        var triangles = mesh.triangles;
        for (int i = 0; i < triangles.Length; i += 3) { int saved = triangles[i + 1]; triangles[i + 1] = triangles[i + 2]; triangles[i + 2] = saved; }
        mesh.triangles = triangles;
    }
    public static void Validate(GameObject root)
    {
        var units = root.GetComponentsInChildren<Transform>(true).Where(t => t.name == "bogie_car").ToArray();
        foreach (var unit in units) foreach (string side in new[] { "L", "R" })
        {
            var m = unit.Find("[S060 manifold " + side + "]");
            if (m == null || m.Find("[transfer socket]") == null || m.GetComponent<MaterialGrabberRenderer>() == null)
                throw new InvalidOperationException("Live-material S282 manifold/socket missing.");
            if (!m.GetComponent<MeshFilter>().sharedMesh.isReadable) throw new InvalidOperationException("Reopened steam manifold must retain CPU-readable geometry.");
        }
        if (root.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("[S060 transfer pipe ")) != 2)
            throw new InvalidOperationException("Both custom S282 transfer pipes are required.");
        foreach (var part in root.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("[S060 transfer pipe ")))
            if (!part.GetComponent<MeshFilter>().sharedMesh.isReadable) throw new InvalidOperationException("Reopened transfer pipe must retain CPU-readable geometry.");
    }
}
