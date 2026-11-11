using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using RearrangedS060;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Bounded native shell test for reversing the front engine. Uses the existing
// exported source reader and triangle intersection implementation, never changes
// production mesh readability or claims a DV suspension/gameplay validation.
public static class MeyerInwardBodyAudit
{
    [Serializable] private sealed class Data { public Entry[] meshes = Array.Empty<Entry>(); }
    [Serializable] private sealed class Entry { public string name = ""; public Vector3[] vertices = Array.Empty<Vector3>(); public Vector2[] uv = Array.Empty<Vector2>(); public int[] triangles = Array.Empty<int>(); }
    [Serializable] private sealed class Result
    {
        public bool passed, actualNativeTriangles = true, gameExecuted = false;
        public int checks, poses, cylinderTriangles, bodyTriangles;
        public bool obsoleteStockSlideBarsRemoved;
        public int legacySlideTrianglesBefore, legacySlideTrianglesAfter;
        public string nativeFixtureSha256, clearanceSourceSha256, runtimeSha256;
        public float yawDegrees = 6, pitchRollDegrees = 1, verticalTravel = .02f, longitudinalTravel = .02f;
        public string[] contacts = Array.Empty<string>();
    }
    public static void RunBatch()
    {
        var output = Environment.GetEnvironmentVariable("S060_MEYER_INWARD_OUTPUT");
        try { Validate(output); EditorApplication.Exit(0); }
        catch (Exception ex)
        {
            if (!string.IsNullOrEmpty(output)) { Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, "error.txt"), ex.ToString()); }
            Debug.LogException(ex); EditorApplication.Exit(1);
        }
    }
    public static void Validate(string output)
    {
        if (string.IsNullOrEmpty(output)) throw new InvalidOperationException("S060_MEYER_INWARD_OUTPUT required.");
        Directory.CreateDirectory(output);
        SteamMeshContact.ValidateAlgorithm();
        var fixture = Environment.GetEnvironmentVariable("S060_NATIVE_PREVIEW");
        var entry = JsonUtility.FromJson<Data>(File.ReadAllText(fixture)).meshes.Single(m => m.name == "s060_body");
        var owned = new List<Object>(); S060MeyerClearance activeClearance = null;
        var car = new GameObject("Native inward Meyer body audit"); owned.Add(car);
        MeshFilter Filter(string name, Transform parent, Func<Vector3, bool> select)
        {
            var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; owned.Add(mesh);
            mesh.vertices = entry.vertices; mesh.uv = entry.uv;
            mesh.triangles = Enumerable.Range(0, entry.triangles.Length / 3)
                .Where(i => Enumerable.Range(0, 3).All(k => select(entry.vertices[entry.triangles[i * 3 + k]])))
                .SelectMany(i => new[] { entry.triangles[3 * i], entry.triangles[3 * i + 1], entry.triangles[3 * i + 2] }).ToArray();
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var filter = go.AddComponent<MeshFilter>(); filter.sharedMesh = mesh; return filter;
        }
        try
        {
            // Runtime disables the split stock castings, calipers and brake-shoe
            // renderers. Use the established native preview's measured masks.
            bool Brake(Vector3 p) => Mathf.Abs(p.x) >= .41f && Mathf.Abs(p.x) <= 1f && p.y <= .8215f &&
                ((p.z >= 2.16f && p.z <= 2.59f) || (p.z >= .6f && p.z <= 1.31f) || (p.z >= -.91f && p.z <= -.57f));
            bool Cylinder(Vector3 p) => SteamNativePreview.NativeCylinder(p, -1) || SteamNativePreview.NativeCylinder(p, 1);
            // Keep a complete triangle unless all its vertices belong to a
            // removed native piece, matching SteamNativePreview.Apply.
            var body = Filter("s060_body", car.transform, p => true);
            body.sharedMesh.triangles = Enumerable.Range(0, entry.triangles.Length / 3)
                .Where(i => !Enumerable.Range(0, 3).All(k => { var p = entry.vertices[entry.triangles[3 * i + k]]; return Cylinder(p) || Brake(p); }))
                .SelectMany(i => new[] { entry.triangles[3 * i], entry.triangles[3 * i + 1], entry.triangles[3 * i + 2] }).ToArray();
            // Applicable LMS ownership: the floor and low tank rails are not
            // part of the remaining s060_body renderer. Partition those exact
            // regions before applying the runtime owner, then test every shell.
            var shells = new List<MeshFilter> { body };
            void Split(string name, Func<Vector3, bool> select)
            {
                var indices = body.sharedMesh.triangles; var chosen = new List<int>(); var kept = new List<int>();
                for (int i = 0; i < indices.Length; i += 3)
                {
                    var target = Enumerable.Range(0, 3).All(k => select(entry.vertices[indices[i + k]])) ? chosen : kept;
                    target.AddRange(new[] { indices[i], indices[i + 1], indices[i + 2] });
                }
                var part = Filter(name, car.transform, p => true); part.sharedMesh.triangles = chosen.ToArray(); body.sharedMesh.triangles = kept.ToArray(); shells.Add(part);
            }
            Split("s060_tank_floor", p => Mathf.Abs(p.x) <= 3 && p.y >= 1.619f && p.y <= 1.661f && p.z >= -1.31f && p.z <= 2.81f);
            foreach (int side in new[] { -1, 1 }) Split(side > 0 ? "s060_tank_l" : "s060_tank_r", p => p.x * side >= 1.23f && p.x * side <= 1.27f && p.y >= 1.55f && p.y <= 1.631f && p.z >= -1.311f && p.z <= 2.79f);
            var pivot = new GameObject("Forward native bogie pivot").transform; pivot.SetParent(car.transform, false);
            var front = new GameObject("Inward visual engine").transform; front.SetParent(pivot, false); front.localRotation = S060MeyerLayout.FrontRotation;
            var castings = new[] { -1, 1 }.Select(side => Filter("Full native inward front cylinder " + side, front, p => SteamNativePreview.NativeCylinder(p, side))).ToArray();
            foreach (var casting in castings)
            {
                casting.transform.localPosition = new Vector3(0, 0, S060MeyerLayout.FrontCylinderZ - S060PipeGeometry.NativeCylinderCentreZ);
                Compact(casting.sharedMesh);
            }
            var originals = shells.Select(f => f.sharedMesh).ToArray();
            int originalSlides = shells.Sum(f => CountOldSlides(f, car.transform));
            if (originalSlides == 0) throw new InvalidOperationException("Native legacy slide-bar fixture missing.");
            var clearance = activeClearance = S060MeyerClearance.Apply(car.transform);
            int remainingSlides = shells.Sum(f => CountOldSlides(f, car.transform));
            if (remainingSlides != 0) throw new InvalidOperationException("Obsolete forward stock slide bars still render in Meyer.");
            var result = new Result { checks = 7, obsoleteStockSlideBarsRemoved = true, legacySlideTrianglesBefore = originalSlides, legacySlideTrianglesAfter = remainingSlides,
                cylinderTriangles = castings.Sum(c => c.sharedMesh.triangles.Length / 3), bodyTriangles = shells.Sum(s => s.sharedMesh.triangles.Length / 3),
                nativeFixtureSha256 = Hash(fixture), clearanceSourceSha256 = Hash(Path.GetFullPath(Path.Combine(Application.dataPath, "../../../RearrangedS060/S060MeyerClearance.cs"))),
                runtimeSha256 = Hash(Environment.GetEnvironmentVariable("S060_CONTACT_AUDIT_RUNTIME")) };
            if (result.cylinderTriangles < 100) throw new InvalidOperationException("Full native cylinder fixture required.");
            // Fixture-only broad phase: only used casting vertices contribute
            // to each side's actual swept AABB. Compact the stationary candidate
            // meshes once, then refine to the actual casting AABB at every pose.
            // SteamMeshContact's triangle algorithm remains unchanged.
            var poses = new List<(Vector3 position, Quaternion rotation, string label)>();
            foreach (float yaw in new[] { -6f, 0f, 6f }) foreach (float pitch in new[] { -1f, 0f, 1f })
                foreach (float roll in new[] { -1f, 0f, 1f }) foreach (float dy in new[] { -.02f, .02f }) foreach (float dz in new[] { -.02f, .02f })
                    poses.Add((new Vector3(0, dy, MeyerPlan.FrontCentre + dz), Quaternion.Euler(pitch, yaw, roll), "yaw=" + yaw + " pitch=" + pitch + " roll=" + roll + " dy=" + dy + " dz=" + dz));
            Bounds Region(MeshFilter casting)
            {
                var points = casting.sharedMesh.vertices.Select(p => car.transform.InverseTransformPoint(casting.transform.TransformPoint(p))).ToArray();
                var region = new Bounds(points[0], Vector3.zero); foreach (var point in points) region.Encapsulate(point); region.Expand(.00001f); return region;
            }
            var candidates = new Dictionary<MeshFilter, MeshFilter[]>();
            foreach (var casting in castings)
            {
                Bounds? swept = null;
                foreach (var pose in poses)
                {
                    pivot.localPosition = pose.position; pivot.localRotation = pose.rotation; var bounds = Region(casting);
                    var all = swept ?? bounds; all.Encapsulate(bounds.min); all.Encapsulate(bounds.max); swept = all;
                }
                candidates[casting] = shells.Select(source => Candidate(source, car.transform, swept.Value, owned)).Where(f => f != null).ToArray();
            }
            var contacts = new List<string>();
            foreach (var pose in poses)
            {
                pivot.localPosition = pose.position; pivot.localRotation = pose.rotation;
                foreach (var casting in castings)
                {
                    var region = Region(casting); var moving = new SteamMeshContact.Shell(casting, car.transform, region);
                    foreach (var filter in candidates[casting])
                    {
                        var shell = new SteamMeshContact.Shell(filter, car.transform, region);
                        if (shell.Intersects(moving, out var contact)) contacts.Add(pose.label + ": " + contact);
                    }
                }
                result.poses++; result.checks++;
            }
            pivot.localPosition = new Vector3(0, 0, MeyerPlan.FrontCentre); pivot.localRotation = Quaternion.identity;
            Render(car, fixture, output, owned);
            clearance.Dispose(); activeClearance = null;
            for (int i = 0; i < shells.Count; i++)
            { if (shells[i].sharedMesh != originals[i]) throw new InvalidOperationException("Clearance mesh did not restore its exact split native source."); result.checks++; }
            result.contacts = contacts.ToArray(); result.passed = contacts.Count == 0;
            File.WriteAllText(Path.Combine(output, "inward-native-cylinder-body-validation.json"), JsonUtility.ToJson(result, true));
            if (!result.passed) throw new InvalidOperationException("Native inward cylinders contact stationary shell in " + contacts.Count + " poses; see report.");
        }
        finally { activeClearance?.Dispose(); foreach (var item in owned) if (item != null) Object.DestroyImmediate(item); }
    }
    private static string Hash(string path)
    { if (string.IsNullOrEmpty(path) || !File.Exists(path)) return "unavailable"; using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", ""); }
    private static int CountOldSlides(MeshFilter filter, Transform car)
    {
        var v = filter.sharedMesh.vertices.Select(p => car.InverseTransformPoint(filter.transform.TransformPoint(p))).ToArray();
        var t = filter.sharedMesh.triangles;
        bool Slide(Vector3 p) => Mathf.Abs(p.x) >= 1.035f && Mathf.Abs(p.x) <= 1.071f && p.z >= 1.109f && p.z <= 2.735f &&
            ((p.y >= .864f && p.y <= .949f) || (p.y >= 1.019f && p.y <= 1.104f));
        return Enumerable.Range(0, t.Length / 3).Count(i => Slide(v[t[i * 3]]) && Slide(v[t[i * 3 + 1]]) && Slide(v[t[i * 3 + 2]]));
    }
    private static void Compact(Mesh mesh)
    {
        var indices = mesh.triangles; var used = indices.Distinct().ToArray(); var map = used.Select((index, local) => new { index, local }).ToDictionary(p => p.index, p => p.local);
        var vertices = mesh.vertices; var uv = mesh.uv;
        mesh.Clear(); mesh.vertices = used.Select(i => vertices[i]).ToArray();
        if (uv.Length == vertices.Length) mesh.uv = used.Select(i => uv[i]).ToArray();
        mesh.triangles = indices.Select(i => map[i]).ToArray(); mesh.RecalculateNormals(); mesh.RecalculateBounds();
    }
    private static MeshFilter Candidate(MeshFilter source, Transform space, Bounds region, List<Object> owned)
    {
        var vertices = source.sharedMesh.vertices.Select(p => space.InverseTransformPoint(source.transform.TransformPoint(p))).ToArray();
        var indices = source.sharedMesh.triangles; var kept = new List<int>();
        for (int i = 0; i < indices.Length; i += 3)
        {
            var bounds = new Bounds(vertices[indices[i]], Vector3.zero); bounds.Encapsulate(vertices[indices[i + 1]]); bounds.Encapsulate(vertices[indices[i + 2]]); bounds.Expand(.00001f);
            if (bounds.Intersects(region)) kept.AddRange(new[] { indices[i], indices[i + 1], indices[i + 2] });
        }
        if (kept.Count == 0) return null;
        var mesh = new Mesh { name = source.name + " actual swept candidates", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; owned.Add(mesh);
        mesh.vertices = vertices; mesh.triangles = kept.ToArray(); Compact(mesh);
        var go = new GameObject(source.name + " compact contact fixture"); owned.Add(go); go.transform.SetParent(space, false); go.SetActive(false);
        var filter = go.AddComponent<MeshFilter>(); filter.sharedMesh = mesh; return filter;
    }
    private static void Render(GameObject car, string fixture, string output, List<Object> owned)
    {
        var texture = new Texture2D(2, 2); texture.LoadImage(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(fixture), "native-preview-atlas.png"))); owned.Add(texture);
        var material = new Material(Shader.Find("Standard")); material.mainTexture = texture; owned.Add(material);
        foreach (var filter in car.GetComponentsInChildren<MeshFilter>()) filter.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
        var equipment = S060MeyerFrontEquipment.Bind(car.transform, car.transform);
        var scene = EditorSceneManager.NewPreviewScene(); var clone = Object.Instantiate(car); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(clone, scene);
        var cameraGo = new GameObject("QA camera"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGo, scene); var camera = cameraGo.AddComponent<Camera>(); camera.scene = scene;
        camera.orthographic = true; camera.orthographicSize = 1.55f; camera.backgroundColor = new Color(.84f, .88f, .92f); camera.clearFlags = CameraClearFlags.SolidColor;
        camera.transform.position = new Vector3(8, 3.7f, -4); camera.transform.LookAt(new Vector3(0, 1.5f, -.3f));
        var lightGo = new GameObject("QA light"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightGo, scene); var light = lightGo.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.4f; light.transform.rotation = Quaternion.Euler(45, -30, 0);
        var target = new RenderTexture(1600, 900, 24); var previous = RenderTexture.active;
        try
        {
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            try { image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply(); File.WriteAllBytes(Path.Combine(output, "inward-front-clearance-preview.png"), image.EncodeToPNG()); }
            finally { Object.DestroyImmediate(image); }
        }
        finally { RenderTexture.active = previous; Object.DestroyImmediate(target); EditorSceneManager.ClosePreviewScene(scene); equipment.Dispose(); }
    }
}
