using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CCL.Types;
using RearrangedS060;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Bake the two body-fixed fittings once. Native mesh evidence is transient;
// only the newly generated sleeve mesh and existing steam-pipe material ship.
public static class DocksideFixedSleeves
{
    public static void Bake(GameObject root, string folder)
    {
        var owned = new List<Object>();
        var front = root.transform.Find("BogieF/bogie_car");
        var rear = root.transform.Find("BogieR/bogie_car");
        // Always start from the unchanged authored source. Re-running the bake
        // must not repeatedly apply the old cap's inclination correction.
        foreach (int side in new[] { -1, 1 })
        {
            string suffix = side < 0 ? "L" : "R";
            var source = AssetDatabase.LoadAssetAtPath<Mesh>(folder + "/SteamManifoldFront" + suffix + ".asset");
            if (source == null) throw new InvalidOperationException("Original Dockside manifold source is missing.");
            var part = front.Find("[S060 manifold " + suffix + "]"); part.GetComponent<MeshFilter>().sharedMesh = source;
            var normal = new Vector3(-side * .406736f, .913545f, 0);
            part.Find("[boiler datum]").localPosition = SteamPipeAuthoring.FaceCentre(source, normal);
            part.Find("[boiler datum]").localRotation = Quaternion.FromToRotation(Vector3.up, normal);
        }
        var copy = Object.Instantiate(root); copy.name = "Transient sleeve fitting";
        Transform native = null; S060BodyPipes neck = null; S060SteamPipes pipes = null;
        try
        {
            native = SteamNativePreview.CreatePipeFixture(copy.transform, owned);
            neck = S060BodyPipes.Apply(native, Vector3.zero, true, false);
            var fitFront = copy.transform.Find("BogieF/bogie_car");
            pipes = S060SteamPipes.Bind(copy.transform, fitFront, copy.transform.Find("BogieR/bogie_car"), null);
            foreach (int side in new[] { -1, 1 })
            {
                string suffix = side < 0 ? "L" : "R";
                string name = "[Dockside smokebox sleeve " + suffix + "]";
                var previous = front.Find(name); if (previous != null) Object.DestroyImmediate(previous.gameObject);
                var manifold = fitFront.Find("[S060 manifold " + suffix + "]");
                var filter = manifold.GetComponent<MeshFilter>();
                // Bake the neutral fitted manifold too. An explicit ring below
                // the cuff keeps all triangles at that joint fully body-fixed;
                // only the lower course needs the existing deformation weights.
                var fitted = SplitAtHeight(filter.sharedMesh, 1.94f);
                string manifoldPath = folder + "/DocksideFixedSteamManifold" + suffix + ".asset";
                fitted.name = "DocksideFixedSteamManifold" + suffix;
                var savedManifold = AssetDatabase.LoadAssetAtPath<Mesh>(manifoldPath);
                if (savedManifold == null) { AssetDatabase.CreateAsset(fitted, manifoldPath); savedManifold = fitted; }
                else { EditorUtility.CopySerialized(fitted, savedManifold); Object.DestroyImmediate(fitted); EditorUtility.SetDirty(savedManifold); }
                var targetManifold = front.Find("[S060 manifold " + suffix + "]");
                targetManifold.GetComponent<MeshFilter>().sharedMesh = savedManifold;
                targetManifold.Find("[boiler datum]").localPosition = manifold.InverseTransformPoint(copy.transform.TransformPoint(S060PipeGeometry.BoilerSocket(side)));
                targetManifold.Find("[boiler datum]").localRotation = Quaternion.identity;
                var matrix = copy.transform.worldToLocalMatrix * manifold.localToWorldMatrix;
                var vertices = filter.sharedMesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray();
                var triangles = filter.sharedMesh.triangles;
                var mesh = new Mesh { name = "DocksideFixedSteamSleeve" + suffix };
                S060SteamSleeve.Fit(mesh, neck.BoilerSection(side, 2.112f), neck.BoilerSection(side, 2.10f),
                    S060SteamSleeve.Section(vertices, triangles, 2.005f), S060SteamSleeve.Section(vertices, triangles, 1.990f));
                // Same dark atlas patch and live material as the adjacent pipes.
                mesh.uv = mesh.uv.Select(uv => new Vector2(.6868f, .8948f) + (uv - Vector2.one * .5f) * .012f).ToArray();
                mesh.RecalculateTangents();
                string path = folder + "/" + mesh.name + ".asset";
                var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (saved == null) { AssetDatabase.CreateAsset(mesh, path); saved = mesh; }
                else { EditorUtility.CopySerialized(mesh, saved); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(saved); }
                var part = new GameObject(name); part.transform.SetParent(front, false);
                part.transform.position = root.transform.position; part.transform.rotation = root.transform.rotation;
                part.AddComponent<MeshFilter>().sharedMesh = saved;
                var renderer = part.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = front.Find("[S060 manifold " + suffix + "]").GetComponent<MeshRenderer>().sharedMaterial;
                SteamBogieVisuals.LiveMaterial(renderer);
            }
        }
        finally
        {
            pipes?.Dispose(); neck?.Dispose(); Object.DestroyImmediate(copy);
            foreach (var item in owned) if (item != null) Object.DestroyImmediate(item);
        }
    }

    private static Mesh SplitAtHeight(Mesh source, float y)
    {
        var positions = source.vertices; var uv = source.uv; var normals = source.normals; var indices = source.triangles;
        var v = new List<Vector3>(); var u = new List<Vector2>(); var n = new List<Vector3>(); var t = new List<int>();
        var shared = new Dictionary<(int a, int b), int>();
        for (int tri = 0; tri < indices.Length; tri += 3) foreach (bool above in new[] { false, true })
        {
            var polygon = new List<(Vector3 p, Vector2 uv, Vector3 n, int a, int b)>();
            for (int e = 0; e < 3; e++)
            {
                int a = indices[tri + e], b = indices[tri + (e + 1) % 3];
                bool ai = above ? positions[a].y >= y : positions[a].y < y;
                bool bi = above ? positions[b].y >= y : positions[b].y < y;
                if (ai) polygon.Add((positions[a], uv[a], normals[a], a, a));
                if (ai != bi)
                {
                    float f = (y - positions[a].y) / (positions[b].y - positions[a].y);
                    polygon.Add((Vector3.LerpUnclamped(positions[a], positions[b], f), Vector2.LerpUnclamped(uv[a], uv[b], f), Vector3.LerpUnclamped(normals[a], normals[b], f).normalized, Math.Min(a,b), Math.Max(a,b)));
                }
            }
            var face = new List<int>();
            foreach (var p in polygon)
            {
                var key = (p.a,p.b);
                if (!shared.TryGetValue(key, out int index))
                { index=v.Count; shared.Add(key,index); v.Add(p.p); u.Add(p.uv); n.Add(p.n); }
                face.Add(index);
            }
            for (int i = 1; i + 1 < polygon.Count; i++) t.AddRange(new[] { face[0], face[i], face[i + 1] });
        }
        var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32, vertices = v.ToArray(), uv = u.ToArray(), normals = n.ToArray(), triangles = t.ToArray() };
        mesh.RecalculateTangents(); mesh.RecalculateBounds(); return mesh;
    }

    public static void RunBatch()
    {
        string output = Environment.GetEnvironmentVariable("DOCKSIDE_OUTPUT"); Directory.CreateDirectory(output);
        try
        {
            string folder = "Assets/_CCL_CARS/" + DocksidePlan.CarName;
            var pack = AssetDatabase.LoadAssetAtPath<CustomCarPack>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:CustomCarPack", new[] { folder }).Single()));
            string prefab = AssetDatabase.GetAssetPath(pack.Cars[0].liveries[0].prefab);
            var root = PrefabUtility.LoadPrefabContents(prefab);
            try { Bake(root, folder); PrefabUtility.SaveAsPrefabAsset(root, prefab); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            string destination = Path.Combine(output, DocksidePlan.CarId); Directory.CreateDirectory(destination);
            var helper = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CCL.Creator.Utility.AssetBundleHelper")).First(t => t != null);
            helper.GetMethod("CreateBundle", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).Invoke(null,
                new object[] { destination, new List<(Object, string)> { (pack, null) } });
            File.WriteAllText(Path.Combine(output, "fixed-sleeves-built.json"), "{\"passed\":true,\"fixedMeshes\":2,\"gameLaunched\":false}");
            EditorApplication.Exit(0);
        }
        catch (Exception ex) { File.WriteAllText(Path.Combine(output, "error.txt"), ex.ToString()); Debug.LogException(ex); EditorApplication.Exit(1); }
    }
}
