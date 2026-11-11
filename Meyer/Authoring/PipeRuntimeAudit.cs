#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// The candidate DLL runs on real Unity meshes/transforms; no car, game world,
// simulation or save file is created. Exercise the same owner used in LateUpdate.
public static class PipeRuntimeAudit
{
    private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static int checks;
    public static void RunBatch()
    {
        string output = Environment.GetEnvironmentVariable("S060_PIPE_RUNTIME_AUDIT_OUTPUT");
        string runtime = Environment.GetEnvironmentVariable("S060_EXHAUST_AUDIT_RUNTIME");
        Directory.CreateDirectory(output);
        string managed = Environment.GetEnvironmentVariable("S060_AUDIT_MANAGED");
        if (string.IsNullOrEmpty(managed)) managed = @"B:\SteamLibrary\steamapps\common\Derail Valley\DerailValley_Data\Managed";
        ResolveEventHandler resolver = (sender, args) =>
        {
            string name = new AssemblyName(args.Name).Name;
            var loaded = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name);
            if (loaded != null) return loaded;
            string path = Path.Combine(managed, name + ".dll");
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        };
        AppDomain.CurrentDomain.AssemblyResolve += resolver;
        var root = new GameObject("Candidate steam-pipe pose audit");
        var originals = new List<Mesh>(); object owner = null; Type pipe = null;
        int errors = 0;
        Application.LogCallback logger = (message, stack, kind) => { if (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert) errors++; };
        Application.logMessageReceived += logger;
        try
        {
            var assembly = Assembly.LoadFrom(runtime);
            pipe = assembly.GetType("RearrangedS060.S060SteamPipes", true);
            var pose = assembly.GetType("RearrangedS060.S060PipePose", true);
            float tolerance = (float)pose.GetField("MaxError", Static).GetRawConstantValue();
            Check(tolerance > 0 && 2 * tolerance <= .00025f, "Two independent fit errors share a .25 mm joint budget below the authored .3 mm tolerance.");
            ValidatePoseBounds(pose, tolerance);
            var front = Child(root.transform, "front"); var rear = Child(root.transform, "rear"); rear.localPosition = Vector3.back * 4;
            foreach (string side in new[] { "L", "R" })
            {
                foreach (var unit in new[] { front, rear })
                {
                    var manifold = Child(unit, "[S060 manifold " + side + "]");
                    manifold.localPosition = new Vector3(side == "L" ? -.7f : .7f, 0, 0);
                    Child(manifold, "[lower datum]"); Child(manifold, "[boiler datum]").localPosition = Vector3.up;
                    Child(manifold, "[transfer socket]").localPosition = new Vector3(0, .3f, 0);
                    var mesh = Mesh(new[] { Vector3.up, new Vector3(.05f, 1, 0), new Vector3(0, 1, .05f), Vector3.zero, new Vector3(.05f, 0, 0), new Vector3(0, 0, .05f) });
                    manifold.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh; originals.Add(mesh);
                }
                var span = Child(front, "[S060 transfer pipe " + side + "]");
                var source = Mesh(new[] { Vector3.zero, new Vector3(.02f, 0, 0), new Vector3(0, .02f, 0), new Vector3(0, 0, 3.0988f), new Vector3(.02f, 0, 3.0988f), new Vector3(0, .02f, 3.0988f) });
                span.gameObject.AddComponent<MeshFilter>().sharedMesh = source; originals.Add(source);
            }
            var filters = root.GetComponentsInChildren<MeshFilter>().Select(f => (filter: f, mesh: f.sharedMesh)).ToArray();
            owner = pipe.GetMethod("Bind", Static).Invoke(null, new object[] { root.transform, front, rear, null });
            var update = pipe.GetMethod("UpdateGeometry", Instance);
            var socket = assembly.GetType("RearrangedS060.S060PipeGeometry", true).GetMethod("BoilerSocket", Static);
            var fitted = front.Find("[S060 manifold L]").GetComponent<MeshFilter>().sharedMesh;
            var baseline = fitted.vertices;
            // Compare against the last uploaded pose, not the last sampled one:
            // repeated individually small movements must eventually update.
            for (int i = 1; i <= 8; i++) { front.localPosition = Vector3.right * (tolerance * i / 10); update.Invoke(owner, null); }
            Check(Equal(baseline, fitted.vertices), "Sub-budget suspension translation does not upload/recalculate a manifold.");
            var leftManifold = front.Find("[S060 manifold L]");
            var leftTarget = root.transform.TransformPoint((Vector3)socket.Invoke(null, new object[] { -1 }));
            Check(Vector3.Distance(leftManifold.TransformPoint(fitted.vertices[0]), leftTarget) <= tolerance + .000001f,
                "An actual skipped mesh cap stays within its component fit budget.");
            rear.localPosition = Vector3.back * (4 + tolerance * .8f); update.Invoke(owner, null);
            var tinySpan = front.Find("[S060 transfer pipe L]");
            float spanEnd = tinySpan.GetComponent<MeshFilter>().sharedMesh.vertices.Max(vertex => vertex.z);
            float upperGap = Vector3.Distance(leftManifold.TransformPoint(fitted.vertices[0]), leftTarget);
            float transferGap = Vector3.Distance(tinySpan.TransformPoint(Vector3.forward * spanEnd), leftManifold.Find("[transfer socket]").position);
            Check(upperGap > tolerance * .5f && transferGap > tolerance * .5f && upperGap + transferGap <= 2 * tolerance + .000001f,
                "Concurrent skipped manifold/span fits compose within the shared .25 mm budget.");
            rear.localPosition = Vector3.back * 4; update.Invoke(owner, null);
            front.localPosition = Vector3.right * (tolerance * 1.2f); update.Invoke(owner, null);
            Check(!Equal(baseline, fitted.vertices), "Accumulated motion beyond the budget updates the manifold.");
            baseline = fitted.vertices;
            front.localRotation = Quaternion.AngleAxis(.0001f, Vector3.up); update.Invoke(owner, null);
            Check(Equal(baseline, fitted.vertices), "Sub-budget bogie rotation leaves the uploaded manifold untouched.");
            front.localRotation = Quaternion.AngleAxis(.01f, Vector3.up); update.Invoke(owner, null);
            Check(!Equal(baseline, fitted.vertices), "Bogie rotation beyond the displacement budget refits the manifold.");
            foreach (float yaw in new[] { -15f, 0, 15f })
            {
                root.transform.SetPositionAndRotation(new Vector3(120, 20, -73), Quaternion.Euler(0, 73, 0));
                front.localRotation = Quaternion.AngleAxis(yaw, Vector3.up); update.Invoke(owner, null);
                foreach (string side in new[] { "L", "R" })
                {
                    var manifold = front.Find("[S060 manifold " + side + "]");
                    var target = root.transform.TransformPoint((Vector3)socket.Invoke(null, new object[] { side == "L" ? -1 : 1 }));
                    Check(Vector3.Distance(manifold.TransformPoint(manifold.GetComponent<MeshFilter>().sharedMesh.vertices[0]), target) < .0003f,
                        "Finite articulated rotations still mate the upper cap to the boiler in world space.");
                }
            }
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); front.localRotation = Quaternion.identity; update.Invoke(owner, null);
            var spanPart = front.Find("[S060 transfer pipe L]"); var spanMesh = spanPart.GetComponent<MeshFilter>().sharedMesh;
            var spanBefore = spanMesh.vertices; var positionBefore = spanPart.position; var rotationBefore = spanPart.rotation;
            rear.localPosition = front.localPosition;
            for (int i = 0; i < 256; i++) update.Invoke(owner, null);
            Check(Equal(spanBefore, spanMesh.vertices) && spanPart.position == positionBefore && spanPart.rotation == rotationBefore,
                "Repeated overlapping sockets preserve the last good visual without exceptions.");
            rear.localPosition = Vector3.back * 5; update.Invoke(owner, null);
            Check(!Equal(spanBefore, spanMesh.vertices), "A valid pose after repeated invalid frames refits the span.");
            foreach (string side in new[] { "L", "R" })
            {
                var part = front.Find("[S060 transfer pipe " + side + "]"); var vertices = part.GetComponent<MeshFilter>().sharedMesh.vertices;
                var from = rear.Find("[S060 manifold " + side + "]/[transfer socket]").position;
                var to = front.Find("[S060 manifold " + side + "]/[transfer socket]").position;
                Check(Vector3.Distance(part.TransformPoint(Vector3.zero), from) < .00001f &&
                    Vector3.Distance(part.TransformPoint(Vector3.forward * vertices.Max(v => v.z)), to) < .00001f,
                    "Recovered span ends reconnect to both sockets.");
                Check(vertices.All(Finite) && Finite(part.position), "Recovery leaves finite geometry and transforms.");
            }
            ((IDisposable)owner).Dispose(); owner = null;
            Check(filters.All(pair => pair.filter.sharedMesh == pair.mesh), "Owner disposal restores all source meshes.");
            owner = pipe.GetMethod("Bind", Static).Invoke(null, new object[] { root.transform, front, rear, null });
            update.Invoke(owner, null);
            Check(filters.Where(pair => pair.filter.transform.IsChildOf(front)).All(pair => pair.filter.sharedMesh != pair.mesh), "Rebinding after disposal owns fresh front pipe meshes.");
            Object.DestroyImmediate(front.Find("[S060 manifold R]/[boiler datum]").gameObject);
            Object.DestroyImmediate(rear.Find("[S060 manifold L]/[transfer socket]").gameObject);
            for (int i = 0; i < 8; i++) update.Invoke(owner, null);
            ((IDisposable)owner).Dispose(); owner = null;
            Check(filters.All(pair => pair.filter.sharedMesh == pair.mesh), "Destroyed optional pose nodes do not prevent source-mesh restoration.");
            Check(errors == 0, "Invalid runtime poses do not generate Unity errors or exception logs.");
            string hash;
            using (var sha = SHA256.Create()) using (var file = File.OpenRead(runtime)) hash = BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "");
            File.WriteAllText(Path.Combine(output, "pipe-runtime-validation.json"), "{\"passed\":true,\"runtimeSha256\":\"" + hash + "\",\"checks\":" + checks +
                ",\"invalidRuntimeFrames\":256,\"runtimeRecovery\":true,\"candidateDll\":true,\"maxSkippedComponentErrorMetres\":" + tolerance.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ",\"maxCombinedFitErrorMetres\":" + (2 * tolerance).ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ",\"gameScene\":false}");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(output, "pipe-runtime-error.txt"), ex.ToString());
            Debug.LogException(ex); EditorApplication.Exit(1);
        }
        finally
        {
            if (owner != null) ((IDisposable)owner).Dispose();
            Object.DestroyImmediate(root); foreach (var mesh in originals) Object.DestroyImmediate(mesh);
            Application.logMessageReceived -= logger; AppDomain.CurrentDomain.AssemblyResolve -= resolver;
        }
    }
    private static void ValidatePoseBounds(Type pose, float tolerance)
    {
        var refit = pose.GetMethod("Refit", Static); var span = pose.GetMethod("Span", Static);
        bool Refit(Vector3 delta, Quaternion rotation, float radius = 1) => (bool)refit.Invoke(null, new object[] { true, Vector3.zero, Quaternion.identity, delta, rotation, 1f, radius });
        Check(!Refit(Vector3.right * (tolerance * .8f), Quaternion.identity) && Refit(Vector3.right * (tolerance * 1.2f), Quaternion.identity), "Translation gate respects its stated error budget.");
        Check(!Refit(Vector3.zero, Quaternion.AngleAxis(tolerance * .4f * Mathf.Rad2Deg, Vector3.up)) &&
            Refit(Vector3.zero, Quaternion.AngleAxis(tolerance * 1.2f * Mathf.Rad2Deg, Vector3.up)), "Small rotations are resolved without Quaternion.Angle rounding to zero.");
        Check(!Refit(Vector3.zero, new Quaternion(0, 0, 0, -1)), "Equivalent quaternion signs do not cause a mesh rebuild.");
        foreach (float value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            for (int i = 0; i < 64; i++)
            {
                Check(!Refit(new Vector3(value, 0, 0), Quaternion.identity) && !Refit(Vector3.zero, new Quaternion(value, 0, 0, 1)), "Nonfinite manifold candidates are rejected repeatedly without changing the last good pose.");
                foreach (int endpoint in new[] { 0, 1 })
                {
                    var args = new object[] { endpoint == 0 ? new Vector3(value, 0, 0) : Vector3.zero, endpoint == 1 ? new Vector3(value, 0, 1) : Vector3.forward, Vector3.up, Vector3.zero, 0f };
                    Check(!(bool)span.Invoke(null, args), "Nonfinite endpoint is rejected before Unity transform/mesh writes.");
                }
            }
        foreach (var pair in new[] { (end: Vector3.zero, up: Vector3.up), (end: Vector3.forward, up: Vector3.forward), (end: Vector3.forward, up: Vector3.zero) })
            Check(!(bool)span.Invoke(null, new object[] { Vector3.zero, pair.end, pair.up, Vector3.zero, 0f }), "Overlap and unusable look frames are rejected.");
        Check(Refit(Vector3.right * tolerance * 2, Quaternion.identity), "A good manifold candidate remains eligible after invalid samples.");
        // Validate the bound against actual weighted vertices, mixed rotations
        // and translations at many small phases, not just threshold constants.
        var random = new System.Random(6013);
        for (int sample = 0; sample < 512; sample++)
        {
            var axis = new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()).normalized;
            var point = axis * 1.7f; float weight = .25f + .75f * (float)random.NextDouble();
            var rotation = Quaternion.AngleAxis((float)(random.NextDouble() * .04), Vector3.up);
            var delta = new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()) * tolerance;
            bool changes = (bool)refit.Invoke(null, new object[] { true, Vector3.zero, Quaternion.identity, delta, rotation, weight, point.magnitude * weight });
            float actual = (weight * (rotation * point - point + delta)).magnitude;
            Check(changes || actual <= tolerance + 1e-6f, "Skipping a mixed weighted deformation respects the vertex error bound.");
        }
    }
    private static bool Finite(Vector3 v) => !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) && !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);
    private static Transform Child(Transform parent, string name) { var child = new GameObject(name).transform; child.SetParent(parent, false); return child; }
    private static bool Equal(Vector3[] a, Vector3[] b) => a.Length == b.Length && a.Zip(b, (x, y) => x.Equals(y)).All(equal => equal);
    private static Mesh Mesh(Vector3[] vertices)
    {
        var mesh = new Mesh { name = "Pipe runtime fixture", vertices = vertices, triangles = new[] { 0, 1, 2, 3, 5, 4, 0, 3, 1, 1, 3, 4 } };
        mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); checks++; }
}
#endif
