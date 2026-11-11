#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using CCL.Types;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Focused candidate-DLL check of unchanged donor meshes with the new runtime
// orientation. Uses the existing local native preview reader; no asset edits.
public static class InwardPipeRuntimeAudit
{
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static int checks, poses, highlightedTriangles;
    public static void RunBatch()
    {
        string output = Environment.GetEnvironmentVariable("S060_INWARD_PIPE_AUDIT_OUTPUT"); Directory.CreateDirectory(output);
        ResolveEventHandler resolver = (s, a) => {
            string name = new AssemblyName(a.Name).Name;
            var loaded = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(v => v.GetName().Name == name); if (loaded != null) return loaded;
            string path = Path.Combine(Environment.GetEnvironmentVariable("S060_AUDIT_MANAGED"), name + ".dll"); return File.Exists(path) ? Assembly.LoadFrom(path) : null; };
        AppDomain.CurrentDomain.AssemblyResolve += resolver;
        try
        {
            string candidate = Environment.GetEnvironmentVariable("S060_EXHAUST_AUDIT_RUNTIME"); var runtime = Assembly.LoadFrom(candidate);
            Validate(runtime, Environment.GetEnvironmentVariable("S060_MEYER_AUDIT_BUNDLE"), true);
            Validate(runtime, Environment.GetEnvironmentVariable("S060_DOCKSIDE_AUDIT_BUNDLE"), false);
            File.WriteAllText(Path.Combine(output, "inward-pipe-runtime-validation.json"), JsonUtility.ToJson(new Report {
                passed = true, checks = checks, poses = poses, runtimeSha256 = Hash(candidate),
                meyerBundleSha256 = Hash(Environment.GetEnvironmentVariable("S060_MEYER_AUDIT_BUNDLE")),
                docksideBundleSha256 = Hash(Environment.GetEnvironmentVariable("S060_DOCKSIDE_AUDIT_BUNDLE")),
                nativeFixtureSha256 = Hash(Environment.GetEnvironmentVariable("S060_NATIVE_PREVIEW")),
                inwardMeyerConnected = true, docksideLegacyUnchanged = true, cabEntryClear = true, liveMaterialsAndCleanup = true,
                sampledRunningGearClearance = true, returnRouteSeparate = true, redundantUpperCourseRemoved = true,
                assembledSidePreview = true, forwardStockDropHighlightTriangles = highlightedTriangles
            }, true));
            EditorApplication.Exit(0);
        }
        catch (Exception ex) { File.WriteAllText(Path.Combine(output, "inward-pipe-error.txt"), ex.ToString()); Debug.LogException(ex); EditorApplication.Exit(1); }
        finally { AppDomain.CurrentDomain.AssemblyResolve -= resolver; }
    }
    private static void Validate(Assembly runtime, string bundlePath, bool meyer)
    {
        var bundle = AssetBundle.LoadFromFile(bundlePath); GameObject root = null;
        IDisposable owner = null, clearance = null, frontMotion = null, equipment = null, bodywork = null, frontMounts = null, rearMounts = null; var native = new List<Object>();
        try
        {
            Check(bundle != null, "Verified donor bundle must load.");
            var pack = bundle.LoadAllAssets<CustomCarPack>().Single();
            var donor = pack.Cars.SelectMany(c => c.liveries).Select(l => l.prefab).First(g => g != null && g.GetComponentsInChildren<Transform>(true).Count(t => t.name == "bogie_car") == 2);
            root = Object.Instantiate(donor); root.SetActive(false);
            Check(SteamNativePreview.Apply(root, native), "Existing readable native geometry fixtures are required.");
            foreach (var script in root.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(script);
            var frames = root.GetComponentsInChildren<Transform>(true).Where(t => t.name == "bogie_car").OrderBy(t => root.transform.InverseTransformPoint(t.position).z).ToArray();
            var rear = frames[0]; var front = frames[1];
            if (meyer)
            {
                var layout = runtime.GetType("RearrangedS060.S060MeyerLayout", true);
                front.localRotation = (Quaternion)layout.GetProperty("FrontRotation", All).GetValue(null, null);
                rear.localPosition += (Vector3)layout.GetProperty("RearVisualOffset", All).GetValue(null, null);
            }
            root.SetActive(true);
            var beforeAssembly = root.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).ToDictionary(f => f, f => f.sharedMesh);
            var stockBody = beforeAssembly.First(p => p.Key.name == "s060_body");
            if (meyer)
            {
                frontMotion = (IDisposable)runtime.GetType("RearrangedS060.S060MeyerFrontMotion", true).GetMethod("Bind", All, null, new[] { typeof(Transform) }, null).Invoke(null, new object[] { front });
                SteamNativePreview.RodSources(root.transform, native);
                var mountBind = runtime.GetType("RearrangedS060.S060MeyerGearMounts", true).GetMethod("Bind", All);
                frontMounts = (IDisposable)mountBind.Invoke(null, new object[] { front, root.transform });
                rearMounts = (IDisposable)mountBind.Invoke(null, new object[] { rear, root.transform });
                foreach (var frame in new[] { front, rear })
                    foreach (var wheel in frame.Find("[axle] rear").GetComponentsInChildren<MeshFilter>(true))
                        checks += MeyerInwardPhaseAudit.CheckMainJournal(beforeAssembly[wheel], wheel.sharedMesh);
                // Existing native preview imports the old complete castings;
                // apply the same compiled per-car placement datum as Bind.
                float shift = (float)runtime.GetType("RearrangedS060.S060MeyerLayout").GetField("FrontCylinderShift", All).GetRawConstantValue();
                foreach (var f in front.GetComponentsInChildren<MeshFilter>(true).Where(f => f.name.StartsWith("Native S060 full-size cylinder QA ", StringComparison.Ordinal))) f.transform.localPosition += Vector3.forward * shift;
                foreach (string side in new[] { "L", "R" })
                    foreach (var renderer in front.Find("[Round cylinders " + side + "]").GetComponentsInChildren<Renderer>(true))
                        renderer.enabled = renderer.name == "Cylinder saddle";
                equipment = (IDisposable)runtime.GetType("RearrangedS060.S060MeyerFrontEquipment", true).GetMethod("Bind", All, null, new[] { typeof(Transform), typeof(Transform) }, null).Invoke(null, new object[] { root.transform, root.transform });
                bodywork = (IDisposable)runtime.GetType("RearrangedS060.S060MeyerBodywork", true).GetMethod("Build", All, null, new[] { typeof(Transform), typeof(Transform), typeof(Transform) }, null).Invoke(null, new object[] { root.transform, root.transform, rear });
            }
            // Removed proxy readers do not evaluate the donor Animator. Pose
            // both real clips explicitly before any gear contact or preview.
            // Native p=.125 maps to inward-front q=-p+.75=.625 (phase audit).
            foreach (var pose in new[] { (frame: front, phase: meyer ? .625f : .125f), (frame: rear, phase: .125f) })
            {
                var animator = pose.frame.GetComponent<Animator>();
                var clip = animator.runtimeAnimatorController.animationClips.Distinct().Single();
                clip.SampleAnimation(pose.frame.gameObject, pose.phase * clip.length); animator.enabled = false;
            }
            if (meyer)
            {
                frontMotion.GetType().GetMethod("ApplyPose", All).Invoke(frontMotion, null);
                foreach (var frame in new[] { front, rear }) checks += MeyerInwardPhaseAudit.CheckGearMounts(frame, true);
            }
            var beforeClearance = root.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).ToDictionary(f => f, f => f.sharedMesh);
            if (meyer) clearance = (IDisposable)runtime.GetType("RearrangedS060.S060MeyerClearance", true).GetMethod("Apply", All, null, new[] { typeof(Transform) }, null).Invoke(null, new object[] { root.transform });
            var source = root.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).ToDictionary(f => f, f => f.sharedMesh);
            var homes = root.GetComponentsInChildren<Transform>(true).Select(t => (t, p: t.localPosition, r: t.localRotation)).ToArray();
            var pipeType = runtime.GetType("RearrangedS060.S060SteamPipes", true);
            owner = (IDisposable)pipeType.GetMethod("Bind", All).Invoke(null, new object[] { root.transform, front, rear, root.transform });
            object inward = pipeType.GetProperty("InwardPipes", All).GetValue(owner, null);
            object exhaust = pipeType.GetProperty("ExhaustReturn", All).GetValue(owner, null);
            Check(meyer ? inward != null && exhaust != null : inward == null && exhaust == null, "Only inward Meyer selects the new routing; Dockside remains on its existing fitter.");
            Transform Group(object value, string name) => (Transform)value.GetType().GetProperty(name, All).GetValue(value, null);
            var generated = meyer ? new[] { Group(inward, "BodyRoot"), Group(inward, "FrontRoot"), Group(inward, "RearRoot") }.SelectMany(t => t.GetComponentsInChildren<MeshFilter>()).ToArray() : Array.Empty<MeshFilter>();
            var returnParts = meyer ? new[] { Group(exhaust, "BodyRoot"), Group(exhaust, "RearRoot") }.SelectMany(t => t.GetComponentsInChildren<MeshFilter>()).ToArray() : Array.Empty<MeshFilter>();
            var ownedMeshes = generated.Concat(returnParts).Select(f => f.sharedMesh).Concat(source.Where(p => p.Key.sharedMesh != p.Value).Select(p => p.Key.sharedMesh)).ToArray();
            var gear = source.Keys.Where(f => f.name.Contains("driver ") || f.name == "[Meyer motion brackets and spring mounts]" || f.name.StartsWith("Leaf spring ", StringComparison.Ordinal) || f.GetComponentsInParent<Transform>().Any(t => t.name.StartsWith("[Meyer rods ", StringComparison.Ordinal))).ToArray();
            if (meyer) Check(gear.Length >= 16, "Actual native wheel/rod fixtures are required for the pipe clearance sample.");
            var rearHome = rear.parent.localPosition; var frontHome = front.parent.localPosition;
            var nativeBody = root.GetComponentsInChildren<MeshFilter>(true).First(f => f.name == "s060_body" && f.sharedMesh != null);
            if (meyer) ValidateVisualPolish(root, stockBody.Value, nativeBody.sharedMesh);
            var covers = new List<MeshFilter>();
            if (meyer)
                foreach (object subset in (IEnumerable)clearance.GetType().GetProperty("CoverTriangles", All).GetValue(clearance, null))
                {
                    var f = (MeshFilter)subset.GetType().GetField("Item1").GetValue(subset); var triangles = (int[])subset.GetType().GetField("Item2").GetValue(subset);
                    var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32, vertices = f.sharedMesh.vertices, triangles = triangles }; native.Add(mesh);
                    var go = new GameObject("Actual compiled Meyer cover QA subset"); go.transform.SetParent(f.transform, false); native.Add(go);
                    var copy = go.AddComponent<MeshFilter>(); copy.sharedMesh = mesh; covers.Add(copy);
                }
            if (meyer) Check(covers.Count != 0, "Actual compiled clearance cover triangles are required.");
            var update = pipeType.GetMethod("UpdateGeometry", All);
            var equipmentParts = meyer ? Group(equipment, "Root").GetComponentsInChildren<MeshFilter>() : Array.Empty<MeshFilter>();
            var equipmentTargets = equipmentParts;
            if (meyer)
            {
                var neck = NativeNeck(nativeBody, root.transform, -1);
                Check(equipmentParts.Length == 1, "Actual combined front equipment mesh is required.");
                Near(CurrentCap(equipmentParts[0], Vector3.up), neck, "Auxiliary pump steam tee shares the native boiler cut");
                equipmentTargets = OutsideEquipmentTee(equipmentParts, root.transform, root.transform.InverseTransformPoint(neck), native);
            }
            var entries = CreateEntries(root.transform, native);
            if (meyer)
            {
                frontMotion.GetType().GetMethod("ApplyPose", All).Invoke(frontMotion, null); update.Invoke(owner, null);
                RenderAssembly(root, runtime, stockBody.Key, stockBody.Value, native);
                MeasureStemSweep(root, front, rear, inward, () => update.Invoke(owner, null));
            }
            foreach (float fy in new[] { -6f, 0, 6f }) foreach (float ry in new[] { -6f, 0, 6f }) foreach (int heave in new[] { -1, 0, 1 })
            {
                root.transform.SetPositionAndRotation(new Vector3(120, 20, -73), Quaternion.Euler(0, 73, 0));
                front.parent.localPosition = frontHome + Vector3.up * (-heave * .02f); rear.parent.localPosition = rearHome + Vector3.up * (heave * .02f);
                front.parent.localRotation = Quaternion.Euler(heave, fy, -heave); rear.parent.localRotation = Quaternion.Euler(-heave, ry, heave);
                update.Invoke(owner, null); poses++;
                foreach (int side in new[] { -1, 1 })
                {
                    string suffix = side < 0 ? "L" : "R", fs = meyer ? (side < 0 ? "R" : "L") : suffix;
                    var f = front.Find("[S060 manifold " + fs + "]"); var r = rear.Find("[S060 manifold " + suffix + "]");
                    var span = front.Find("[S060 transfer pipe " + fs + "]").GetComponent<MeshFilter>();
                    var from = meyer ? (Transform)inward.GetType().GetMethod("RearTransfer", All).Invoke(inward, new object[] { side }) : r.Find("[transfer socket]");
                    var to = meyer ? (Transform)inward.GetType().GetMethod("FrontTransfer", All).Invoke(inward, new object[] { side }) : f.Find("[transfer socket]");
                    Near(OriginalCap(span, source[span], Vector3.back), from.position); Near(OriginalCap(span, source[span], Vector3.forward), to.position);
                    var expected = meyer ? (Vector3)inward.GetType().GetMethod("AdmissionSocket", All).Invoke(inward, new object[] { side }) :
                        (Vector3)runtime.GetType("RearrangedS060.S060PipeGeometry").GetMethod("BoilerSocket", All).Invoke(null, new object[] { side });
                    var mf = f.GetComponent<MeshFilter>();
                    if (!meyer)
                    {
                        var normal = f.Find("[boiler datum]").localRotation * Vector3.up;
                        Near(OriginalCap(mf, source[mf], normal), root.transform.TransformPoint(expected)); continue;
                    }
                    float cut = f.Find("[lower datum]").localPosition.y + .33f;
                    var rendered = mf.sharedMesh.triangles.Select(j => mf.sharedMesh.vertices[j]).ToArray();
                    Check(rendered.All(p => p.y <= cut + .00001f), "No referenced upper donor branch/flange remains on inward Meyer.");
                    Near(CurrentCap(mf, Vector3.back), OriginalCap(mf, source[mf], Vector3.back, true), "Retained complete HP transfer face");
                    var stem = (Transform)inward.GetType().GetMethod("FrontAdmission", All).Invoke(inward, new object[] { side });
                    Near(CutAxis(mf, cut), stem.position, "Measured retained HP stem axis");
                    var admission = Group(inward, "BodyRoot").Find("HP boiler admission " + side).GetComponent<MeshFilter>();
                    Near(DrawnCap(admission, false), NativeNeck(nativeBody, root.transform, side), "HP admission native-neck start " + side);
                    Near(DrawnCap(admission, true), root.transform.TransformPoint(expected), "HP admission swivel end " + side);
                    var flex = Group(inward, "BodyRoot").Find("HP admission flex " + side).GetComponent<MeshFilter>();
                    Near(DrawnCap(flex, false), root.transform.TransformPoint(expected), "Single lower admission feed start");
                    Near(DrawnCap(flex, true), stem.position, "Single lower admission feed to HP stem");
                    var low = admission.sharedMesh.triangles.Select(j => root.transform.InverseTransformPoint(admission.transform.TransformPoint(admission.sharedMesh.vertices[j]))).ToArray();
                    Check(!low.Any(p => p.z > -.20f && p.z < 2.80f && p.y > 1.72f), "The redundant high course across the tank side is absent.");
                    foreach (var tuple in new[] { (group: Group(inward, "FrontRoot"), name: "HP interstage elbow ", datum: f.Find("[transfer socket]"), joint: to),
                        (group: Group(inward, "RearRoot"), name: "LP interstage elbow ", datum: r.Find("[transfer socket]"), joint: from) })
                    {
                        var elbow = tuple.group.Find(tuple.name + side).GetComponent<MeshFilter>();
                        Near(DrawnCap(elbow, false), tuple.datum.position); Near(DrawnCap(elbow, true), tuple.joint.position);
                    }
                }
                foreach (var filter in generated.Concat(returnParts)) Check(filter.sharedMesh.vertices.All(Finite), "Generated pipe geometry stays finite.");
                foreach (var filter in generated.Concat(front.GetComponentsInChildren<MeshFilter>().Where(f => f.name.StartsWith("[S060 transfer pipe "))))
                    Check(ClearEntry(filter, root.transform, entries, out var entryHit), "New admission/interstage pipe must leave the existing cab-entry envelope clear: " +
                        filter.name + "; frontYaw=" + fy + ",rearYaw=" + ry + ",heave=" + heave + "; " + entryHit);
                if (meyer)
                {
                    var supply = generated.Concat(front.GetComponentsInChildren<MeshFilter>().Where(f => f.name.StartsWith("[S060 transfer pipe "))).Select(f => new SteamMeshContact.Shell(f, root.transform)).ToArray();
                    var coverTargets = supply.Concat(front.GetComponentsInChildren<MeshFilter>().Where(f => f.name.StartsWith("[S060 manifold ")).Select(f => new SteamMeshContact.Shell(f, root.transform))).ToArray();
                    foreach (var filter in covers)
                    { var shell = new SteamMeshContact.Shell(filter, root.transform); foreach (var target in coverTargets)
                        Check(!shell.Intersects(target, out var hit), "Inward steam route intersects actual compiled clearance cover: " + hit); }
                    foreach (var filter in returnParts)
                    { var shell = new SteamMeshContact.Shell(filter, root.transform); foreach (var target in supply)
                        Check(!shell.Intersects(target, out var hit), "LP return intersects inward admission/interstage route: " + hit); }
                    if (fy == 0 && ry == 0 && heave == 0)
                    {
                        foreach (var filter in gear)
                        { var shell = new SteamMeshContact.Shell(filter, root.transform); foreach (var target in supply)
                            Check(!shell.Intersects(target, out var hit), "Inward pipe intersects actual running-gear mesh in straight sample: " + hit); }
                        foreach (var filter in equipmentTargets)
                        { var shell = new SteamMeshContact.Shell(filter, root.transform); foreach (var target in supply)
                            Check(!shell.Intersects(target, out var hit), "Lower admission intersects actual compiled front equipment: " + hit); }
                    }
                    var a = Group(exhaust, "RearJoint"); var b = Group(exhaust, "BodyJoint");
                    Near(DrawnCap(Group(exhaust, "InnerSleeve").GetComponent<MeshFilter>(), false), a.position);
                    Near(DrawnCap(Group(exhaust, "OuterSleeve").GetComponent<MeshFilter>(), false), b.position);
                    float length = (float)exhaust.GetType().GetField("SleeveLength", All).GetRawConstantValue();
                    Check(2 * length - Vector3.Distance(a.position, b.position) > .10f, "Shifted LP return retains positive sleeve overlap.");
                }
            }
            if (meyer)
            {
                var renderer = nativeBody.GetComponent<MeshRenderer>(); var original = renderer.sharedMaterial; var paint = new Material(original); native.Add(paint);
                renderer.sharedMaterial = paint; update.Invoke(owner, null);
                Check(generated.All(f => f.GetComponent<MeshRenderer>().sharedMaterial == paint), "Generated inward routes follow the current live native material."); renderer.sharedMaterial = original;
            }
            foreach (var h in homes) { h.t.localPosition = h.p; h.t.localRotation = h.r; }
            if (meyer)
            {
                frontMotion.GetType().GetMethod("ApplyPose", All).Invoke(frontMotion, null); update.Invoke(owner, null);
            }
            owner.Dispose(); owner.Dispose(); owner = null;
            Check(source.All(p => p.Key.sharedMesh == p.Value), "Both layouts restore their exact source meshes.");
            Check(ownedMeshes.All(m => m == null), "New inward and return pipe meshes are released idempotently.");
            clearance?.Dispose(); clearance = null;
            Check(beforeClearance.All(p => p.Key.sharedMesh == p.Value), "Combined pipe/clearance cleanup restores original native aliases.");
            equipment?.Dispose(); equipment = null; bodywork?.Dispose(); bodywork = null;
            frontMounts?.Dispose(); frontMounts = null; rearMounts?.Dispose(); rearMounts = null;
            frontMotion?.Dispose(); frontMotion = null;
            Check(beforeAssembly.All(p => p.Key.sharedMesh == p.Value), "Assembled front fitting and equipment restore original donor/native meshes.");
        }
        finally { owner?.Dispose(); clearance?.Dispose(); equipment?.Dispose(); bodywork?.Dispose(); frontMounts?.Dispose(); rearMounts?.Dispose(); frontMotion?.Dispose(); if (root != null) Object.DestroyImmediate(root); foreach (var item in native) if (item != null) Object.DestroyImmediate(item); if (bundle != null) bundle.Unload(true); }
    }
    private static Vector3 OriginalCap(MeshFilter filter, Mesh original, Vector3 normal, bool sourceOnly = false)
    {
        var source = original.vertices; float plane = source.Max(v => Vector3.Dot(v, normal));
        var current = sourceOnly ? source : filter.sharedMesh.vertices; var triangles = original.triangles; var centre = Vector3.zero; float area = 0;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            var ids = new[] { triangles[i], triangles[i + 1], triangles[i + 2] }; if (ids.Any(j => Mathf.Abs(Vector3.Dot(source[j], normal) - plane) > .0001f)) continue;
            var a = filter.transform.TransformPoint(current[ids[0]]); var b = filter.transform.TransformPoint(current[ids[1]]); var c = filter.transform.TransformPoint(current[ids[2]]);
            float size = Vector3.Cross(b - a, c - a).magnitude; centre += (a + b + c) * (size / 3); area += size;
        }
        Check(area > .00001f, "Actual original cap triangles required."); return centre / area;
    }
    private static Vector3 CurrentCap(MeshFilter filter, Vector3 normal) => OriginalCap(filter, filter.sharedMesh, normal);
    private static Vector3 CutAxis(MeshFilter f, float y)
    {
        var v = f.sharedMesh.vertices; var t = f.sharedMesh.triangles; var points = new List<Vector3>();
        for (int i = 0; i < t.Length; i += 3)
            if (Enumerable.Range(i, 3).All(j => Mathf.Abs(v[t[j]].y - y) < .00001f))
                points.AddRange(Enumerable.Range(i, 3).Select(j => v[t[j]]));
        Check(points.Count >= 6, "Actual capped lower admission stem faces are required.");
        return f.transform.TransformPoint(new Vector3((points.Min(p => p.x) + points.Max(p => p.x)) / 2, y, (points.Min(p => p.z) + points.Max(p => p.z)) / 2));
    }
    private static void RenderAssembly(GameObject root, Assembly runtime, MeshFilter nativeFilter, Mesh nativeSource, List<Object> owned)
    {
        // Reuse the existing camera/lighting utility for both images from this
        // exact assembled candidate fixture. Only the second adds an overlay.
        var output = typeof(ConventionalNativeAudit).GetField("output", All);
        var previous = output.GetValue(null); output.SetValue(null, Environment.GetEnvironmentVariable("S060_INWARD_PIPE_AUDIT_OUTPUT"));
        var render = typeof(ConventionalNativeAudit).GetMethod("RenderWide", All, null, new[] { typeof(GameObject), typeof(string), typeof(Vector3), typeof(Vector3), typeof(float) }, null);
        void Draw(string name) => render.Invoke(null, new object[] { root, name, new Vector3(-13, 2.35f, -1.2f), new Vector3(0, 1.85f, -1.7f), 3.8f });
        try
        {
            foreach (var animator in root.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            Draw("meyer-compact-pipes-side");
            void Detail(string name, Vector3 eye, Vector3 target, float size)
            {
                var fill = new GameObject("QA detail camera fill"); fill.transform.SetParent(root.transform, false);
                fill.transform.rotation = Quaternion.LookRotation(target - eye);
                var light = fill.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = .7f;
                try { render.Invoke(null, new object[] { root, name, eye, target, size }); }
                finally { Object.DestroyImmediate(fill); }
            }
            Detail("meyer-rear-beam-detail", new Vector3(-3, 2.1f, -11), new Vector3(0, 1.3f, -7.9f), 1.35f);
            Detail("meyer-bunker-detail", new Vector3(-6, 3.4f, -6), new Vector3(-1.25f, 2.6f, -4.95f), 1.25f);
            Detail("meyer-front-gear-detail", new Vector3(-6, 1.6f, 1.3f), new Vector3(-.9f, .92f, 1.3f), 1.5f);
            Detail("meyer-rear-gear-detail", new Vector3(-6, 1.6f, -5.5f), new Vector3(-.9f, .92f, -5.5f), 1.5f);
            var matrix = root.transform.worldToLocalMatrix * nativeFilter.transform.localToWorldMatrix;
            var vertices = nativeSource.vertices.Select(matrix.MultiplyPoint3x4).ToArray();
            var triangles = Enumerable.Range(0, nativeSource.subMeshCount).Select(nativeSource.GetTriangles).ToArray();
            var args = new object[] { vertices, triangles, null };
            var ids = (int[])runtime.GetType("RearrangedS060.S060MeyerClearance", true).GetMethod("Islands", All, null, new[] { typeof(Vector3[]), typeof(int[][]), typeof(Bounds[]).MakeByRefType() }, null).Invoke(null, args);
            var bounds = (Bounds[])args[2];
            var candidates = new HashSet<int>(Enumerable.Range(0, bounds.Length).Where(i => {
                var b = bounds[i]; return Mathf.Abs(b.center.x) > .19f && Mathf.Abs(b.center.x) < .36f && b.min.y > .50f && b.min.y < .52f &&
                    b.max.y > 1.62f && b.max.y < 1.64f && b.min.z > .25f && b.min.z < .27f && b.max.z > 2.40f && b.max.z < 2.43f; }));
            var selected = new List<Vector3>();
            foreach (var sub in triangles) for (int i = 0; i < sub.Length; i += 3)
            {
                if (!candidates.Contains(ids[sub[i]])) continue;
                var p = Clip(new[] { vertices[sub[i]], vertices[sub[i + 1]], vertices[sub[i + 2]] }.ToList(), 2, 2.05f, false);
                p = Clip(p, 1, 1.50f, true);
                for (int j = 1; j + 1 < p.Count; j++) selected.AddRange(new[] { p[0], p[j], p[j + 1] });
            }
            Check(selected.Count != 0, "The measured stock forward-drop island must be identified before considering a crop.");
            highlightedTriangles = selected.Count / 3;
            var mesh = new Mesh { name = "Measured stock front drop QA highlight", vertices = selected.ToArray(), triangles = Enumerable.Range(0, selected.Count).ToArray() };
            mesh.colors = Enumerable.Repeat(new Color(1, .05f, .8f), selected.Count).ToArray(); owned.Add(mesh);
            var material = new Material(Shader.Find("Hidden/Internal-Colored")) { color = Color.white, renderQueue = 5000 };
            material.SetInt("_ZTest", 8); material.SetInt("_ZWrite", 0); material.SetInt("_Cull", 0); owned.Add(material);
            var overlay = new GameObject("Measured unsupported native forward drop QA only"); owned.Add(overlay); overlay.transform.SetParent(root.transform, false);
            overlay.AddComponent<MeshFilter>().sharedMesh = mesh; overlay.AddComponent<MeshRenderer>().sharedMaterial = material;
            Draw("meyer-compact-pipes-stock-drop-highlight"); overlay.SetActive(false);
        }
        finally { output.SetValue(null, previous); }
    }
    private static void ValidateVisualPolish(GameObject root, Mesh originalBody, Mesh fittedBody)
    {
        var original = originalBody.vertices; var originalTriangles = originalBody.triangles;
        var islands = RearrangedS060.S060MeyerClearance.Islands(original, new[] { originalTriangles }, out var bounds);
        var counts = originalTriangles.Distinct().GroupBy(j => islands[j]).ToDictionary(g => g.Key, g => g.Count());
        var orphanIslands = new HashSet<int>(Enumerable.Range(0, bounds.Length).Where(j => counts.ContainsKey(j) &&
            ((counts[j] == 24 && Mathf.Abs(bounds[j].center.x) > 1.02f && Mathf.Abs(bounds[j].center.x) < 1.04f && bounds[j].min.z > 1.11f && bounds[j].max.z < 1.18f) ||
             (counts[j] == 42 && Mathf.Abs(bounds[j].center.x) > 1.02f && Mathf.Abs(bounds[j].center.x) < 1.04f && bounds[j].min.z > 1.12f && bounds[j].max.z < 1.17f && bounds[j].max.y < 1.12f) ||
             (counts[j] == 33 && Mathf.Abs(bounds[j].center.x) > 1.14f && Mathf.Abs(bounds[j].center.x) < 1.15f && bounds[j].min.z > .75f && bounds[j].max.z < .82f))));
        Check(orphanIslands.Count == 8, "Exact eight native orphan islands are present in the source fixture.");
        Vector3 Key(Vector3 p) => new Vector3(Mathf.Round(p.x * 100000), Mathf.Round(p.y * 100000), Mathf.Round(p.z * 100000));
        var orphanPoints = new HashSet<Vector3>(originalTriangles.Distinct().Where(j => orphanIslands.Contains(islands[j])).Select(j => Key(original[j])));
        int Orphans(Mesh mesh)
        {
            var v = mesh.vertices; var t = mesh.triangles; int count = 0;
            for (int i = 0; i < t.Length; i += 3) if (orphanPoints.Contains(Key(v[t[i]])) && orphanPoints.Contains(Key(v[t[i + 1]])) && orphanPoints.Contains(Key(v[t[i + 2]]))) count++;
            return count;
        }
        int before = Orphans(originalBody), after = Orphans(fittedBody);
        Check(before > 50 && after == 0, "Measured obsolete static guide cheek/bolts/pivot triangles disappear: " + before + " -> " + after);
        var bodywork = root.transform.Find("[Meyer R2 bodywork]");
        var iron = bodywork.Find("Meyer R2 ironwork").GetComponent<MeshFilter>().sharedMesh;
        var vertices = iron.vertices; var triangles = iron.triangles; int sharedFaces = 0;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            var p = new[] { vertices[triangles[i]], vertices[triangles[i + 1]], vertices[triangles[i + 2]] };
            if (p.Max(a => a.x) <= -1.25f || p.Min(a => a.x) >= 1.25f || p.Max(a => a.y) <= .90f || p.Min(a => a.y) >= 1.25f) continue;
            if (p.All(a => Mathf.Abs(a.z + 7.98f) < .00001f) || p.All(a => Mathf.Abs(a.z + 7.82f) < .00001f)) sharedFaces++;
        }
        Check(sharedFaces == 0, "Iron supports do not share either exposed red buffer-beam face.");
        var paint = bodywork.Find("Meyer R2 paintwork").GetComponent<MeshFilter>().sharedMesh;
        var points = paint.vertices; var uv = paint.uv; float maxError = 0;
        foreach (int side in new[] { -1, 1 })
        {
            var ids = paint.triangles.Distinct().Where(j => paint.normals[j].x * side > .9f && Mathf.Abs(points[j].x - side * 1.28f) < .00001f && points[j].z >= -5.55001f && points[j].z <= -4.37999f && points[j].y >= 1.65999f).ToArray();
            int At(float z, float y) => ids.OrderBy(j => Mathf.Abs(points[j].z - z) + Mathf.Abs(points[j].y - y)).First();
            var basis = new[] { At(-4.38f, 1.66f), At(-5.55f, 1.66f), At(-4.38f, 3.45f) };
            var matrix = Matrix4x4.identity;
            for (int j = 0; j < 3; j++) matrix.SetRow(j, new Vector4(points[basis[j]].z, points[basis[j]].y, 1, 0));
            var u = matrix.inverse.MultiplyVector(new Vector3(uv[basis[0]].x, uv[basis[1]].x, uv[basis[2]].x));
            var v = matrix.inverse.MultiplyVector(new Vector3(uv[basis[0]].y, uv[basis[1]].y, uv[basis[2]].y));
            foreach (int j in ids)
            {
                var p = new Vector3(points[j].z, points[j].y, 1);
                maxError = Mathf.Max(maxError, Vector2.Distance(uv[j], new Vector2(Vector3.Dot(u, p), Vector3.Dot(v, p))));
            }
            Check(ids.Length >= 5 && maxError < .00001f, "Whole hopper side uses one continuous affine UV map, including the sloped corner: " + maxError);
        }
        File.WriteAllText(Path.Combine(Environment.GetEnvironmentVariable("S060_INWARD_PIPE_AUDIT_OUTPUT"), "visual-polish-validation.json"),
            "{\"passed\":true,\"originalOrphanTriangles\":" + before + ",\"remainingOrphanTriangles\":" + after + ",\"beamSharedFaces\":" + sharedFaces + ",\"maximumHopperUvError\":" + maxError.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ",\"gameScene\":false}");
    }
    private static List<Vector3> Clip(List<Vector3> polygon, int axis, float edge, bool less)
    {
        var result = new List<Vector3>();
        for (int i = 0; i < polygon.Count; i++)
        { var a = polygon[i]; var b = polygon[(i + 1) % polygon.Count]; bool ia = less ? a[axis] <= edge : a[axis] >= edge, ib = less ? b[axis] <= edge : b[axis] >= edge;
            if (ia) result.Add(a); if (ia != ib) result.Add(Vector3.LerpUnclamped(a, b, (edge - a[axis]) / (b[axis] - a[axis]))); }
        return result;
    }
    private static void MeasureStemSweep(GameObject root, Transform front, Transform rear, object inward, Action update)
    {
        var homes = new[] { root.transform, front.parent, rear.parent }.Distinct().Select(t => (t, p: t.localPosition, r: t.localRotation)).ToArray();
        var report = new StemSweep { sides = new[] { new StemSide { side = -1 }, new StemSide { side = 1 } } };
        var frontHome = front.parent.localPosition; var rearHome = rear.parent.localPosition;
        try
        {
            foreach (float fy in new[] { -6f, 0, 6f }) foreach (float ry in new[] { -6f, 0, 6f }) foreach (int heave in new[] { -1, 0, 1 })
            {
                root.transform.SetPositionAndRotation(new Vector3(120, 20, -73), Quaternion.Euler(0, 73, 0));
                front.parent.localPosition = frontHome + Vector3.up * (-heave * .02f); rear.parent.localPosition = rearHome + Vector3.up * (heave * .02f);
                front.parent.localRotation = Quaternion.Euler(heave, fy, -heave); rear.parent.localRotation = Quaternion.Euler(-heave, ry, heave); update(); report.poses++;
                foreach (var side in report.sides)
                {
                    var stem = (Transform)inward.GetType().GetMethod("FrontAdmission", All).Invoke(inward, new object[] { side.side });
                    var filter = stem.GetComponent<MeshFilter>(); var matrix = root.transform.worldToLocalMatrix * stem.localToWorldMatrix;
                    var v = filter.sharedMesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray(); var triangles = filter.sharedMesh.triangles;
                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        var tri = new[] { v[triangles[i]], v[triangles[i + 1]], v[triangles[i + 2]] }.ToList();
                        foreach (var band in new[] { (range: side.exactCoverBand, low: 1.675f, high: 1.690f), (range: side.serviceBand, low: 1.54f, high: 1.72f) })
                        {
                            var polygon = Clip(Clip(tri, 1, band.low, false), 1, band.high, true);
                            foreach (var p in polygon) band.range.Add(p, fy, ry, heave);
                        }
                    }
                }
            }
            File.WriteAllText(Path.Combine(Environment.GetEnvironmentVariable("S060_INWARD_PIPE_AUDIT_OUTPUT"), "meyer-admission-stem-cover-sweep.json"), JsonUtility.ToJson(report, true));
        }
        finally { foreach (var h in homes) { h.t.localPosition = h.p; h.t.localRotation = h.r; } update(); }
    }
    private static Vector3 DrawnCap(MeshFilter f, bool end)
    {
        var v = f.sharedMesh.vertices; int cap = v.Length - (end ? 1 : 2); var t = f.sharedMesh.triangles; var centre = Vector3.zero; float area = 0;
        for (int i = 0; i < t.Length; i += 3) if (t[i] == cap || t[i + 1] == cap || t[i + 2] == cap)
        { var a = f.transform.TransformPoint(v[t[i]]); var b = f.transform.TransformPoint(v[t[i + 1]]); var c = f.transform.TransformPoint(v[t[i + 2]]); float s = Vector3.Cross(b - a, c - a).magnitude; centre += (a + b + c) * (s / 3); area += s; }
        Check(area > .00001f, "Actual generated cap triangles required."); return centre / area;
    }
    private static Vector3 NativeNeck(MeshFilter f, Transform body, int side)
    {
        // Compose local hierarchy matrices without a 120 m world roundtrip:
        // sub-centimetre plane selection must not depend on world-origin loss.
        var matrix = Matrix4x4.identity;
        for (var part = f.transform; part != body; part = part.parent)
        { if (part == null) throw new InvalidOperationException("Native neck filter is outside the body fixture."); matrix = Matrix4x4.TRS(part.localPosition, part.localRotation, part.localScale) * matrix; }
        var v = f.sharedMesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray(); var t = f.sharedMesh.triangles;
        var section = new List<Vector3>(); float area = 0;
        for (int i = 0; i < t.Length; i += 3)
        {
            var ids = new[] { t[i], t[i + 1], t[i + 2] };
            if (ids.Any(j => Mathf.Abs(v[j].y - 2.10f) > .00001f || v[j].x * side < .52f || v[j].x * side > 1.50f || v[j].z < 2.98f || v[j].z > 3.35f)) continue;
            area += Vector3.Cross(v[ids[1]] - v[ids[0]], v[ids[2]] - v[ids[0]]).magnitude;
            section.AddRange(ids.Select(j => v[j]));
        }
        Check(area > .00001f && section.Count >= 6, "Actual native boiler neck cut triangles required.");
        // The genuine asymmetric opening's axis is the centre of its measured
        // section, not the area centroid of its triangulated cap fan. Those
        // differ by 20.75 mm in the unchanged native source.
        var centre = new Vector3((section.Min(p => p.x) + section.Max(p => p.x)) / 2, 2.10f,
            (section.Min(p => p.z) + section.Max(p => p.z)) / 2);
        return body.TransformPoint(centre);
    }
    private static MeshFilter[] CreateEntries(Transform body, List<Object> owned)
    {
        var result = new List<MeshFilter>();
        foreach (int side in new[] { -1, 1 })
        {
            var b = new Bounds(new Vector3(side * 1.63f, 2.55f, -2.7f), new Vector3(.5f, 1.8f, 3.2f));
            var a = b.min; var z = b.max;
            var mesh = new Mesh { name = "Unchanged cab entry QA volume", vertices = new[] {
                new Vector3(a.x,a.y,a.z), new Vector3(z.x,a.y,a.z), new Vector3(z.x,z.y,a.z), new Vector3(a.x,z.y,a.z),
                new Vector3(a.x,a.y,z.z), new Vector3(z.x,a.y,z.z), new Vector3(z.x,z.y,z.z), new Vector3(a.x,z.y,z.z) },
                triangles = new[] { 0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,3,7,6,3,6,2,0,4,7,0,7,3,1,2,6,1,6,5 } };
            owned.Add(mesh); var go = new GameObject("Unchanged cab entry QA " + side); owned.Add(go); go.transform.SetParent(body, false);
            var filter = go.AddComponent<MeshFilter>(); filter.sharedMesh = mesh; result.Add(filter);
        }
        return result.ToArray();
    }
    private static MeshFilter[] OutsideEquipmentTee(MeshFilter[] equipment, Transform body, Vector3 neck, List<Object> owned)
    {
        // The pump and HP feed share this one native boiler tee. Preserve all
        // actual equipment faces outside the tightly bounded junction volume;
        // splitting crossing triangles avoids exempting a whole long service.
        var lo = neck + new Vector3(-.12f, -.27f, -.12f); var hi = neck + new Vector3(.12f, .03f, .12f);
        var result = new List<MeshFilter>();
        foreach (var filter in equipment)
        {
            var matrix = body.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            var v = filter.sharedMesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray(); var t = filter.sharedMesh.triangles;
            var points = new List<Vector3>();
            for (int i = 0; i < t.Length; i += 3)
            {
                var remainder = new[] { v[t[i]], v[t[i + 1]], v[t[i + 2]] }.ToList();
                foreach (var plane in new[] { (axis: 0, edge: lo.x, less: false), (axis: 0, edge: hi.x, less: true),
                    (axis: 1, edge: lo.y, less: false), (axis: 1, edge: hi.y, less: true),
                    (axis: 2, edge: lo.z, less: false), (axis: 2, edge: hi.z, less: true) })
                {
                    var outside = Clip(remainder, plane.axis, plane.edge, !plane.less);
                    for (int j = 1; j + 1 < outside.Count; j++) points.AddRange(new[] { outside[0], outside[j], outside[j + 1] });
                    remainder = Clip(remainder, plane.axis, plane.edge, plane.less);
                    if (remainder.Count == 0) break;
                }
            }
            Check(points.Count > 0, "Equipment contact test retains actual faces outside the shared boiler tee.");
            var mesh = new Mesh { name = "Actual equipment outside native boiler tee QA", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
                vertices = points.ToArray(), triangles = Enumerable.Range(0, points.Count).ToArray() }; owned.Add(mesh);
            var go = new GameObject("Actual equipment outside bounded boiler tee QA"); owned.Add(go); go.transform.SetParent(body, false);
            var target = go.AddComponent<MeshFilter>(); target.sharedMesh = mesh; result.Add(target);
        }
        return result.ToArray();
    }
    private static bool ClearEntry(MeshFilter f, Transform body, MeshFilter[] entries, out string hit)
    {
        hit = null;
        var v = f.sharedMesh.vertices.Select(p => body.InverseTransformPoint(f.transform.TransformPoint(p))).ToArray();
        var shell = new SteamMeshContact.Shell(f, body);
        foreach (var entry in entries)
        {
            var target = new SteamMeshContact.Shell(entry, body);
            // An entirely contained small pipe may not cross a box face.
            foreach (int j in f.sharedMesh.triangles)
                if (target.Bounds.Contains(v[j])) { hit = entry.name + " contains actual pipe vertex " + v[j].ToString("F7"); return false; }
            if (shell.Intersects(target, out hit)) return false;
        }
        return true;
    }
    private static bool Finite(Vector3 v) => !float.IsNaN(v.x + v.y + v.z) && !float.IsInfinity(v.x + v.y + v.z);
    private static void Near(Vector3 a, Vector3 b, string name = "Steam-pipe cap") => Check(Vector3.Distance(a, b) < .0003f,
        name + " detached by " + Vector3.Distance(a, b).ToString("R") + "; actual " + a.ToString("F7") + ", expected " + b.ToString("F7"));
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); checks++; }
    private static string Hash(string path) { using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", ""); }
    [Serializable] private sealed class Report
    { public bool passed, inwardMeyerConnected, docksideLegacyUnchanged, cabEntryClear, liveMaterialsAndCleanup, sampledRunningGearClearance, returnRouteSeparate, redundantUpperCourseRemoved, assembledSidePreview; public int checks, poses, forwardStockDropHighlightTriangles; public string runtimeSha256, meyerBundleSha256, docksideBundleSha256, nativeFixtureSha256; public bool gameScene = false; }
    [Serializable] private sealed class StemSweep { public int poses; public StemSide[] sides; public bool actualRenderedTriangleBandClipping = true; }
    [Serializable] private sealed class StemSide { public int side; public StemRange exactCoverBand = new StemRange(), serviceBand = new StemRange(); }
    [Serializable] private sealed class StemRange
    {
        public int points; public Vector3 min, max, minAbsXPoint, minAbsXPose; public float minAbsX = float.MaxValue, maxAbsX;
        public void Add(Vector3 p, float frontYaw, float rearYaw, int heave)
        {
            if (points++ == 0) { min = p; max = p; } else { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
            float x = Mathf.Abs(p.x); maxAbsX = Mathf.Max(maxAbsX, x);
            if (x < minAbsX) { minAbsX = x; minAbsXPoint = p; minAbsXPose = new Vector3(frontYaw, rearYaw, heave); }
        }
    }
}
#endif
