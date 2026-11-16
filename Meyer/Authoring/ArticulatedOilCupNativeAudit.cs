using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CCL.Types;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Bounded check of actual shipping Sites/NewMount/LateUpdate and native cup
// geometry against reopened articulated rods and Shay bearing/sill geometry.
public static class ArticulatedOilCupNativeAudit
{
    const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    static readonly Type Fixture = typeof(ConventionalNativeAudit);
    static Assembly runtime, game;
    static int checks, poses;
    static string output;
    static float maximumGap;
    static object F(string name, params object[] args) => Fixture.GetMethod(name, All).Invoke(null, args);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethod(name, All).Invoke(owner, args);
    static object Get(object owner, string name) => owner.GetType().GetField(name, All).GetValue(owner);
    static void Set(object owner, string name, object value) => owner.GetType().GetField(name, All).SetValue(owner, value);
    static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); checks++; }
    public static void RunBatch()
    {
        output = Environment.GetEnvironmentVariable("S060_ART_OIL_OUTPUT"); Directory.CreateDirectory(output);
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => (Assembly)F("Resolve", s, e);
        try
        {
            string dll = Environment.GetEnvironmentVariable("S060_EXHAUST_AUDIT_RUNTIME"); runtime = Assembly.LoadFrom(dll);
            game = Assembly.LoadFrom(Path.Combine(Environment.GetEnvironmentVariable("S060_AUDIT_MANAGED"), "Assembly-CSharp.dll"));
            Fixture.GetField("runtime", All).SetValue(null, runtime); Fixture.GetField("output", All).SetValue(null, output);
            var texture = new Texture2D(2, 2); texture.LoadImage(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("S060_NATIVE_PREVIEW")), "native-preview-atlas.png")));
            var material = new Material(Shader.Find("Standard")); material.mainTexture = texture; Fixture.GetField("material", All).SetValue(null, material);
            foreach (bool meyer in new[] { true, false }) Validate(meyer);
            // Reuse the existing real production Shay BuildModels fixture.
            Fixture.GetField("OilCupAudit", All).SetValue(null, (Action<object,GameObject>)Shay);
            Environment.SetEnvironmentVariable("S060_SHAY_PREVIEW_ONLY", "1");
            var dataType = Fixture.GetNestedType("Data", All);
            var wheels = JsonUtility.FromJson(File.ReadAllText(Environment.GetEnvironmentVariable("S060_CONVENTIONAL_NATIVE")), dataType);
            var native = JsonUtility.FromJson(File.ReadAllText(Environment.GetEnvironmentVariable("S060_NATIVE_PREVIEW")), dataType);
            var body = ((IEnumerable)native.GetType().GetField("meshes").GetValue(native)).Cast<object>().Single(e => (string)e.GetType().GetField("name").GetValue(e) == "s060_body");
            F("ValidateShay", wheels, body);
            File.WriteAllText(Path.Combine(output, "validation.json"), "{\"passed\":true,\"checks\":" + checks + ",\"poses\":" + poses + ",\"maximumMountToDrawnSurfaceGapMetres\":" + maximumGap.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ",\"runtimeSha256\":\"" + F("Hash", dll) + "\",\"meyerCups\":8,\"docksideCups\":8,\"shayCups\":6,\"productionMountMethods\":true,\"gameplayValidated\":false}");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { File.WriteAllText(Path.Combine(output, "error.txt"), e.ToString()); Debug.LogException(e); EditorApplication.Exit(1); }
    }
    static Component Owner(GameObject root)
    {
        var car = root.GetComponent(game.GetType("TrainCar", true)) ?? root.AddComponent(game.GetType("TrainCar", true));
        var oil = root.AddComponent(runtime.GetType("RearrangedS060.S060OilCups", true)); Set(oil, "car", car);
        var material = root.GetComponentsInChildren<MeshRenderer>(true).First(r => r.name == "s060_body");
        Set(oil, "stemMaterialSource", material); Set(oil, "stemMaterial", material.sharedMaterial);
        return oil;
    }
    static List<Component> Cups(GameObject root, int count)
    {
        var type = Fixture.GetNestedType("Data", All);
        var data = JsonUtility.FromJson(File.ReadAllText(Environment.GetEnvironmentVariable("S060_CONVENTIONAL_OIL_NATIVE")), type);
        var external = new GameObject("Native servicing cups QA").transform; external.SetParent(root.transform, false);
        foreach (var node in (IEnumerable)data.GetType().GetField("roots").GetValue(data)) F("Tree", external, node, data);
        foreach (var f in external.GetComponentsInChildren<MeshFilter>(true).Where(f => f.name == "OilCup_LOD0")) f.gameObject.SetActive(true);
        var consumerType = game.GetType("DV.Util.PositionSyncConsumer", true); var providerType = game.GetType("DV.Util.PositionSyncProvider", true);
        var result = new List<Component>();
        for (int i = 0; i < count; i++)
        {
            var cup = i < 6 ? external.Find("ManualOilingPoints/OilingPoint" + i) : Object.Instantiate(external.Find("ManualOilingPoints/OilingPoint2").gameObject, external).transform;
            // A cloned primitive provider is not a native circuit/controller;
            // this fixture exercises geometry binding only.
            var consumer = cup.GetComponent(consumerType) ?? cup.gameObject.AddComponent(consumerType);
            Set(consumer, "syncTag", "ART_QA_" + i);
            var provider = new GameObject("Original cup provider " + i); provider.transform.SetParent(root.transform, false);
            var component = provider.AddComponent(providerType); Set(component, "syncTag", "ART_QA_" + i);
            Call(consumer, "SetProviderTransform", component); result.Add(consumer);
        }
        Check(result.Count == count && result.Select(c => (string)Get(c, "syncTag")).Distinct().Count() == count, "Exact live cup count with no duplicates");
        return result;
    }
    static void Validate(bool meyer)
    {
        var bundle = AssetBundle.LoadFromFile(Environment.GetEnvironmentVariable(meyer ? "S060_MEYER_AUDIT_BUNDLE" : "S060_DOCKSIDE_AUDIT_BUNDLE"));
        var donor = bundle.LoadAllAssets<CustomCarPack>().Single().Cars.SelectMany(c => c.liveries).Select(l => l.prefab).First(g => g != null && g.GetComponentsInChildren<Transform>(true).Count(t => t.name == "bogie_car") == 2);
        var root = Object.Instantiate(donor); root.SetActive(false); var native = new List<Object>(); var fitted = new List<IDisposable>(); Component oil = null;
        try
        {
            Check(SteamNativePreview.Apply(root, native), "Readable native geometry required");
            foreach (var script in root.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(script);
            var frames = root.GetComponentsInChildren<Transform>(true).Where(t => t.name == "bogie_car").OrderBy(t => t.position.z).ToArray(); var rear = frames[0]; var front = frames[1];
            if (meyer)
            {
                var layout = runtime.GetType("RearrangedS060.S060MeyerLayout", true);
                front.localRotation = (Quaternion)layout.GetProperty("FrontRotation", All).GetValue(null, null);
                rear.localPosition += (Vector3)layout.GetProperty("RearVisualOffset", All).GetValue(null, null);
                fitted.Add((IDisposable)runtime.GetType("RearrangedS060.S060MeyerFrontMotion", true).GetMethod("Bind", All, null, new[] { typeof(Transform) }, null).Invoke(null, new object[] { front }));
                SteamNativePreview.RodSources(root.transform, native);
                foreach (var frame in frames) fitted.Add((IDisposable)runtime.GetType("RearrangedS060.S060MeyerGearMounts", true).GetMethod("Bind", All).Invoke(null, new object[] { frame, root.transform }));
            }
            front.name = "[S060 " + (meyer ? "Meyer" : "Dockside") + " front]"; rear.name = "[S060 " + (meyer ? "Meyer" : "Dockside") + " rear]";
            root.SetActive(true); oil = Owner(root); var cups = Cups(root, 8);
            var sites = ((IEnumerable)Call(oil, "Sites")).Cast<object>().ToArray(); Check(sites.Length == 8, "Eight articulated production cup sites");
            foreach (bool left in new[] { true, false })
            {
                var sideSites = sites.Where(s => (bool)Get(s, "Left") == left).OrderByDescending(s => (bool)Get(s, "FrontUnit")).ThenByDescending(s => (bool)Get(s, "Main")).ToArray();
                var indices = left ? new[] { 2, 0, 1, 6 } : new[] { 5, 3, 4, 7 };
                for (int i = 0; i < 4; i++) Call(oil, "FitNative", cups[indices[i]], sideSites[i], Quaternion.identity);
            }
            var clips = root.GetComponentsInChildren<Animator>(true).SelectMany(a => a.runtimeAnimatorController.animationClips).Distinct().ToArray();
            Check(clips.Length != 0, "Native donor mechanism animation clip required");
            var home = frames.Select(f => f.localRotation).ToArray();
            foreach (float yaw in new[] { -6f, 0f, 6f }) for (int phase = 0; phase < 360; phase += 30)
            {
                for (int i = 0; i < frames.Length; i++) frames[i].localRotation = home[i] * Quaternion.Euler(0, yaw, 0);
                foreach (var frame in frames) clips.First().SampleAnimation(frame.gameObject, phase / 360f * clips.First().length);
                if (meyer) Call(fitted[0], "LateUpdate");
                Call(oil, "LateUpdate"); CheckMounts(oil, frames.SelectMany(f => f.GetComponentsInChildren<MeshFilter>(true)).Where(f => f.name.IndexOf("rod", StringComparison.OrdinalIgnoreCase) >= 0).ToArray()); poses++;
            }
            F("RenderWide", root, meyer ? "meyer-cups" : "dockside-cups", new Vector3(8, 1.4f, .2f), new Vector3(0, .8f, .2f), 2.6f);
        }
        finally
        {
            if (oil != null) { Call(oil, "Clear"); Call(oil, "Clear"); }
            for (int i = fitted.Count - 1; i >= 0; i--) fitted[i].Dispose(); Object.DestroyImmediate(root);
            foreach (var item in native) if (item != null) Object.DestroyImmediate(item); bundle.Unload(true);
        }
    }
    public static void Shay(object shay, GameObject root)
    {
        var oil = Owner(root); var cups = Cups(root, 6);
        try
        {
            var seats = (IDictionary)runtime.GetType("RearrangedS060.S060ShaySeats", true).GetMethod("Build", All).Invoke(null, new[] { shay });
            Check(seats.Count == 6, "Shay has six supported native-circuit cups");
            var siteType = oil.GetType().GetNestedType("Site", All);
            foreach (DictionaryEntry entry in seats)
            {
                var seat = (Transform)entry.Value; var site = Activator.CreateInstance(siteType, true); Set(site, "Frame", seat);
                var mount = Call(oil, "NewMount", site, cups[(int)entry.Key], Get(cups[(int)entry.Key], "syncTag")); Set(mount, "Fixed", seat); Set(mount, "FixedPoint", Vector3.zero); Set(mount, "CupOrientation", Quaternion.identity);
            }
            var markers = (Transform[])shay.GetType().GetProperty("Markers", All).GetValue(shay, null);
            var trucks = markers.Where(t => t.name == "TRUCK_FRONT_PIVOT" || t.name == "TRUCK_REAR_PIVOT").ToArray(); var home = trucks.Select(t => t.localRotation).ToArray();
            foreach (float yaw in new[] { -6f, 0f, 6f })
            {
                for (int i = 0; i < trucks.Length; i++) trucks[i].localRotation = home[i] * Quaternion.Euler(0, yaw, 0);
                Call(oil, "LateUpdate");
                CheckMounts(oil, root.GetComponentsInChildren<MeshFilter>(true).Where(f => !f.name.StartsWith("OilCup") && !f.name.StartsWith("[S060 oil") && f.GetComponent<Renderer>()?.enabled == true).ToArray()); poses++;
            }
            for (int i = 0; i < trucks.Length; i++) trucks[i].localRotation = home[i];
            F("RenderWide", root, "shay-cups-geared-side", new Vector3(8, 1.4f, .2f), new Vector3(0, 1.2f, .2f), 2.5f);
            F("RenderWide", root, "shay-cups-offside", new Vector3(-8, 1.4f, .2f), new Vector3(0, 1.2f, .2f), 2.5f);
        }
        finally { Call(oil, "Clear"); Call(oil, "Clear"); }
    }
    public static int CheckMounts(Component oil, MeshFilter[] surfaces)
    {
        runtime = oil.GetType().Assembly; int before = checks;
        foreach (var mount in (IEnumerable)Get(oil, "mounts"))
        {
            var frame = (Transform)Get(mount, "Frame"); var consumer = (Component)Get(mount, "Consumer"); var stem = (Transform)Get(mount, "Stem");
            var fixedAt = (Transform)Get(mount, "Fixed"); Vector3 contact;
            if (fixedAt != null) contact = fixedAt.TransformPoint((Vector3)Get(mount, "FixedPoint"));
            else
            {
                var wheel = (Transform)Get(mount, "Wheel");
                contact = (Vector3)runtime.GetType("RearrangedS060.S060OilCupGeometry").GetMethod("Pin", All).Invoke(null, new object[] { frame, wheel, Get(mount, "X") }) + frame.up * (float)Get(mount, "Boss");
            }
            var thread = consumer.transform.position + consumer.transform.rotation * (Vector3)Get(mount, "CupFoot") + frame.up * .006f;
            if (stem.gameObject.activeSelf)
            {
                var axis = (thread - contact).normalized; var bottom = stem.TransformPoint(Vector3.down); var top = stem.TransformPoint(Vector3.up);
                float lower = Vector3.Dot(bottom - contact, axis), upper = Vector3.Dot(top - thread, axis);
                // Production retains a 10 mm minimum neck: its ends may embed
                // into the rod/thread, but may never stop short of either.
                Check(lower <= .00003f && lower >= -.005f && Vector3.Cross(bottom - contact, axis).magnitude < .00003f, "Drawn neck reaches or embeds in the rod");
                Check(upper >= -.00003f && upper <= .005f && Vector3.Cross(top - thread, axis).magnitude < .00003f, "Drawn neck reaches or embeds in the actual cup thread");
            }
            else Check(Vector3.Distance(contact, thread) < .00003f, "Fixed cup thread seats on its mounting face");
            // A fixed seat locates the engaged thread, not necessarily the
            // exposed surface. Test its physical six-millimetre engagement
            // segment so a correctly embedded thread is not called floating.
            float gap = surfaces.Where(f => f.sharedMesh != null && f.sharedMesh.isReadable).Min(f => SurfaceDistance(contact, f, fixedAt != null ? (Vector3?)(thread - frame.up * .006f) : null));
            maximumGap = Mathf.Max(maximumGap, gap); Check(gap < .002f, "Oil cup mount has no drawn support: " + consumer.name + " gap=" + gap);
        }
        return checks - before;
    }
    public static float SurfaceDistance(Vector3 point, MeshFilter filter, Vector3? threadBottom = null)
    {
        var mesh = filter.sharedMesh; var v = mesh.vertices; var t = mesh.triangles; float best = float.PositiveInfinity;
        for (int i = 0; i < v.Length; i++) v[i] = filter.transform.TransformPoint(v[i]);
        for (int i = 0; i < t.Length; i += 3)
        {
            var a = v[t[i]]; var b = v[t[i + 1]]; var c = v[t[i + 2]]; var n = Vector3.Cross(b - a, c - a);
            if (n.sqrMagnitude < 1e-16f) continue;
            if (threadBottom.HasValue)
            {
                var delta = threadBottom.Value - point; float denominator = Vector3.Dot(delta, n);
                if (Mathf.Abs(denominator) > 1e-12f)
                {
                    float along = Vector3.Dot(a - point, n) / denominator;
                    if (along >= 0 && along <= 1)
                    {
                        var hit = point + delta * along;
                        if (Vector3.Dot(Vector3.Cross(b - a, hit - a), n) >= -1e-10f && Vector3.Dot(Vector3.Cross(c - b, hit - b), n) >= -1e-10f && Vector3.Dot(Vector3.Cross(a - c, hit - c), n) >= -1e-10f) return 0;
                    }
                }
            }
            var p = point - n * (Vector3.Dot(point - a, n) / n.sqrMagnitude);
            bool inside = Vector3.Dot(Vector3.Cross(b - a, p - a), n) >= -1e-10f && Vector3.Dot(Vector3.Cross(c - b, p - b), n) >= -1e-10f && Vector3.Dot(Vector3.Cross(a - c, p - c), n) >= -1e-10f;
            if (inside) best = Mathf.Min(best, Vector3.Distance(point, p));
            foreach (var edge in new[] { new[] { a, b }, new[] { b, c }, new[] { c, a } }) { var d = edge[1] - edge[0]; var q = edge[0] + d * Mathf.Clamp01(Vector3.Dot(point - edge[0], d) / d.sqrMagnitude); best = Mathf.Min(best, Vector3.Distance(point, q)); }
        }
        return best;
    }
}
