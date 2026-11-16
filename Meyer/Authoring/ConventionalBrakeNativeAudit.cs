using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Reuses the proven native conventional hierarchy and installed LMS split methods.
// Exercises production geometry, native heat property application and cleanup.
// Native simulation/temperature integration and gameplay are outside this fixture.
public static class ConventionalBrakeNativeAudit
{
    const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static readonly Type Fixture = typeof(ConventionalNativeAudit);
    static int checks, comparisons;
    static string output;
    static Assembly runtime;
    static readonly List<string> contacts = new List<string>();
    static readonly List<string> nativeContacts = new List<string>();
    static object F(string name, params object[] args) => Fixture.GetMethod(name, All).Invoke(null, args);
    static void Field(string name, object value) => Fixture.GetField(name, All).SetValue(null, value);
    static void Set(object owner, string name, object value) => F("Set", owner, name, value);
    static object Call(object owner, string name, params object[] args) => owner.GetType().GetMethod(name, All).Invoke(owner, args);
    static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); checks++; }
    static object Entry(object data, string name) => ((IEnumerable)data.GetType().GetField("meshes").GetValue(data)).Cast<object>().Single(e => (string)e.GetType().GetField("name").GetValue(e) == name);
    static Mesh Mesh(object data, string name) => (Mesh)F("Mesh", Entry(data, name), null);
    public static void RunBatch()
    {
        output = Environment.GetEnvironmentVariable("S060_BRAKE_OUTPUT"); Directory.CreateDirectory(output);
        AppDomain.CurrentDomain.AssemblyResolve += (s, e) => (Assembly)F("Resolve", s, e);
        try
        {
            string dll = Environment.GetEnvironmentVariable("S060_EXHAUST_AUDIT_RUNTIME");
            runtime = Assembly.LoadFrom(dll); Field("runtime", runtime);
            var type = Fixture.GetNestedType("Data", All);
            object Read(string env) => JsonUtility.FromJson(File.ReadAllText(Environment.GetEnvironmentVariable(env)), type);
            var native = Read("S060_NATIVE_PREVIEW"); var wheels = Read("S060_CONVENTIONAL_NATIVE"); var brakes = Read("S060_BRAKE_NATIVE");
            var texture = new Texture2D(2, 2); texture.LoadImage(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("S060_NATIVE_PREVIEW")), "native-preview-atlas.png")));
            var material = new Material(Shader.Find("Standard")); material.mainTexture = texture; Field("material", material);
            bool baseline = Environment.GetEnvironmentVariable("S060_BRAKE_BASELINE") == "1";
            foreach (int count in new[] { 3, 4, 5 }) Validate(native, wheels, brakes, count, baseline);
            File.WriteAllLines(Path.Combine(output, "contacts.txt"), contacts);
            File.WriteAllLines(Path.Combine(output, "native-profile-contacts.txt"), nativeContacts);
            Check(contacts.Count == 0, "Brake contacts found; see contacts.txt");
            File.WriteAllText(Path.Combine(output, "validation.json"), "{\"passed\":true,\"checks\":" + checks + ",\"triangleComparisons\":" + comparisons + ",\"inheritedNativeContactPairs\":" + nativeContacts.Count + ",\"newContactPairs\":" + contacts.Count + ",\"baseline\":" + baseline.ToString().ToLowerInvariant() + ",\"runtimeSha256\":\"" + F("Hash", dll) + "\",\"nativeLod0AndLod1\":true,\"gameplayValidated\":false}");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { File.WriteAllText(Path.Combine(output, "error.txt"), e.ToString()); File.WriteAllLines(Path.Combine(output, "contacts.txt"), contacts); Debug.LogException(e); EditorApplication.Exit(1); }
    }
    static void Validate(object native, object wheels, object brakes, int count, bool baseline)
    {
        string name = count == 3 ? "060-short" : count == 4 ? "080" : "0100";
        var root = new GameObject("S060 native brake fixture " + name); root.SetActive(false);
        object scale = null, owner = null; IDisposable rods = null;
        var game = Assembly.LoadFrom(Path.Combine(Environment.GetEnvironmentVariable("S060_AUDIT_MANAGED"), "Assembly-CSharp.dll"));
        var car = root.AddComponent(game.GetType("TrainCar", true));
        var body = new GameObject("LocoS060_Body").transform; body.SetParent(root.transform, false);
        foreach (var node in (IEnumerable)wheels.GetType().GetField("roots").GetValue(wheels)) F("Tree", body, node, wheels);
        var left = body.Find("DrivingMechanism_L"); var right = body.Find("DrivingMechanism_R");
        var brakeFilters = new List<MeshFilter>();
        var splitter = Assembly.LoadFrom(Path.Combine(Environment.GetEnvironmentVariable("S060_AUDIT_MODS"), "LocoMeshSplitter/LocoMeshSplitter.dll"));
        for (int lod = 0; lod < 2; lod++)
        {
            var group = new GameObject("Native static LOD" + lod).transform; group.SetParent(body, false);
            var mesh = Mesh(brakes, lod == 0 ? "s060_body" : "s060_body_LOD1");
            var splitterType = splitter.GetType("LocoMeshSplitter.MeshSplitters.S060.S060LOD" + lod + "MeshSplitter", true);
            foreach (string side in new[] { "L", "R" }) foreach (int index in new[] { 1, 2, 3 })
            {
                var split = (Mesh)splitterType.GetMethod("GetCaliper" + side + index + "Mesh", All).Invoke(null, new object[] { mesh });
                Check(split.isReadable && split.triangles.Length != 0, "Installed LMS produces readable nonempty calipers at LOD" + lod);
                brakeFilters.Add(((Transform)F("Draw", group, "s060_caliper_" + side.ToLowerInvariant() + "_" + index, split)).GetComponent<MeshFilter>());
            }
            if (lod == 1) group.gameObject.SetActive(false);
        }
        var shoe = ((Transform)F("Draw", body, "s060_brake_shoes", Mesh(brakes, "s060_brake_shoes"))).GetComponent<MeshFilter>(); brakeFilters.Add(shoe);
        var heatMaterial = new Material(Shader.Find("Standard")); heatMaterial.mainTexture = ((Material)Fixture.GetField("material", All).GetValue(null)).mainTexture; heatMaterial.EnableKeyword("_EMISSION");
        shoe.GetComponent<Renderer>().sharedMaterial = heatMaterial;
        var heat = root.AddComponent(game.GetType("DV.Simulation.Brake.BrakesOverheatingController", true));
        var heatField = heat.GetType().GetField("brakeRenderers"); var originalHeat = new[] { shoe.GetComponent<Renderer>() }; heatField.SetValue(heat, originalHeat);
        bool Brake(Vector3 p) => Mathf.Abs(p.x) >= .41f && Mathf.Abs(p.x) <= 1 && p.y >= 0 && p.y <= .8215f && ((p.z >= 2.16f && p.z <= 2.59f) || (p.z >= .6f && p.z <= 1.31f) || (p.z >= -.91f && p.z <= -.57f));
        Func<Vector3, bool> shell = p => !Brake(p) && !SteamNativePreview.NativeCylinder(p, -1) && !SteamNativePreview.NativeCylinder(p, 1);
        F("Draw", body, "s060_body", F("Mesh", Entry(native, "s060_body"), shell));
        foreach (int side in new[] { -1, 1 })
        {
            Func<Vector3, bool> cylinder = p => SteamNativePreview.NativeCylinder(p, side) && !Brake(p);
            F("Draw", body, side < 0 ? "s060_cylinder_left" : "s060_cylinder_right", F("Mesh", Entry(native, "s060_body"), cylinder));
        }
        var parts = F("New", "S060Parts"); Set(parts, "Car", car); Set(parts, "Body", body); Set(parts, "MechanismLeft", left); Set(parts, "MechanismRight", right);
        var front = new GameObject("Axle_F"); front.transform.SetParent(root.transform, false); var rear = new GameObject("Axle_R"); rear.transform.SetParent(root.transform, false);
        Set(parts, "FrontBogie", front.AddComponent(game.GetType("Bogie", true))); Set(parts, "RearBogie", rear.AddComponent(game.GetType("Bogie", true)));
        var wheelMap = (IDictionary)parts.GetType().GetField("wheels", All).GetValue(parts); var axleMap = (IDictionary)parts.GetType().GetField("axleForWheel", All).GetValue(parts); var poweredMap = (IDictionary)parts.GetType().GetField("poweredForWheel", All).GetValue(parts);
        var poweredType = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("LocoSim.Implementations.Wheels.PoweredWheel")).First(t => t != null);
        for (int i = 1; i <= 3; i++)
        {
            wheelMap.Add(i, new List<Transform> { left.Find("s060_Wheels_0" + i), right.Find("s060_Wheels_0" + i) });
            var axle = new GameObject("[axle]"); axle.transform.SetParent(i == 1 ? rear.transform : front.transform, false);
            var powered = axle.AddComponent(poweredType); poweredType.GetField("wheelTransform", All).SetValue(powered, right.Find("s060_Wheels_0" + i)); axleMap.Add(i, axle.transform); poweredMap.Add(i, powered);
        }
        var originalMeshes = brakeFilters.Select(f => f.sharedMesh).ToArray();
        var sourceCenters = Enumerable.Range(1, 3).Select(i => right.Find("s060_Wheels_0" + i).localPosition).ToArray();
        var renderers = brakeFilters.Select(f => f.GetComponent<Renderer>()).ToArray(); var materials = renderers.Select(r => r.sharedMaterial).ToArray();
        // Keep one normally hidden LOD1 renderer disabled to exercise state
        // preservation without removing a visible caliper from the renders.
        var disabled = renderers[6]; disabled.enabled = false;
        float factor = count == 5 ? 1f / 1.424256f : .8f;
        scale = Activator.CreateInstance(runtime.GetType("RearrangedS060.S060DriverScale", true), All, null, new[] { parts }, null);
        try
        {
            Call(scale, "Apply", factor);
            Check(renderers.All(r => !r.enabled), "Baseline scaling hides every native shoe and caliper");
            root.SetActive(true);
            if (count > 3)
            {
                owner = F("New", count == 5 ? "S060TenCoupled" : "S060EightCoupled"); Set(owner, "Parts", parts); Set(owner, "Scale", scale); Call(owner, "BuildGeometry");
                rods = (IDisposable)runtime.GetType("RearrangedS060.S060CouplingRods", true).GetMethod("Apply", All).Invoke(null, new[] { parts });
            }
            Call(scale, "PositionFittings", owner == null ? 0f : owner.GetType().GetProperty("CylinderShift", All).GetValue(owner, null));
            var targets = Enumerable.Range(1, count).Select(i => right.Cast<Transform>().Single(w => w.name.StartsWith("s060_Wheels_0" + i) && !w.name.Contains("LOD"))).ToList();
            if (!baseline) Call(scale, "PositionBrakes", targets);
            Render(root, name + "-cold");
            if (!baseline)
            {
                Check(!disabled.enabled && renderers.Where(r => r != disabled).All(r => r.enabled), "Preserve originally disabled renderer and restore fitted visible brakes");
                Check(ReferenceEquals(heatField.GetValue(heat), originalHeat), "Exact native overheating renderer array retained");
                for (int i = 0; i < brakeFilters.Count; i++)
                {
                    Check(brakeFilters[i].sharedMesh != originalMeshes[i], "Fitted mesh is instance owned at both native LODs");
                    Check(renderers[i].sharedMaterial == materials[i], "Native material binding retained");
                    Check(brakeFilters[i].sharedMesh.normals.Length == brakeFilters[i].sharedMesh.vertexCount && brakeFilters[i].sharedMesh.uv.Length == brakeFilters[i].sharedMesh.vertexCount, "Native shading and UV data retained");
                    int copies = brakeFilters[i].name.StartsWith("s060_caliper_") && brakeFilters[i].name.EndsWith("_3") ? count - 2 : 1;
                    if (brakeFilters[i] != shoe) Check(brakeFilters[i].sharedMesh.triangles.Length == originalMeshes[i].triangles.Length * copies && brakeFilters[i].sharedMesh.subMeshCount == originalMeshes[i].subMeshCount, "Exact caliper section count and material slots at both LODs");
                }
                Check(shoe.sharedMesh.triangles.Length == originalMeshes.Last().triangles.Length / 3 * count, "Exactly one pair of native shoes per driven axle");
                var positions = shoe.sharedMesh.vertices; var centers = targets.Select(t => body.InverseTransformPoint(t.position)).ToArray();
                foreach (var center in centers) foreach (int side in new[] { -1, 1 }) Check(positions.Any(p => p.x * side > .4f && p.z > center.z + .45f * factor && p.z < center.z + .80f * factor), "A fitted shoe exists on both sides of every axle");
                var block = new MaterialPropertyBlock(); var glow = new Color(4, .3f, .02f); block.SetColor("_EmissionColor", glow);
                var set = heat.GetType().GetMethod("SetOverheatPropertyBlockToAllRenderers", All); set.Invoke(heat, new object[] { block });
                var actual = new MaterialPropertyBlock(); shoe.GetComponent<Renderer>().GetPropertyBlock(actual); Check(actual.GetColor("_EmissionColor") == glow, "Native controller applies the brake heat property");
                Render(root, name + "-native-heat"); set.Invoke(heat, new object[] { null }); shoe.GetComponent<Renderer>().GetPropertyBlock(actual); Check(actual.isEmpty, "Native controller clears brake glow");
                foreach (var brake in brakeFilters.Where(f => f.transform.parent.gameObject.activeSelf)) foreach (var wheel in new[] { left, right }.SelectMany(m => m.Cast<Transform>()).Where(w => w.name.StartsWith("s060_Wheels_0") && !w.name.Contains("LOD")).Select(w => w.GetComponent<MeshFilter>()))
                {
                    comparisons++; var a = new SteamMeshContact.Shell(brake, body); var b = new SteamMeshContact.Shell(wheel, body, a.Bounds);
                    if (!a.Intersects(b, out var hit)) continue;
                    // A native caliper is allowed the same own-wheel contact
                    // only if the exact unmodified source meshes reproduce it.
                    // Adjacent-wheel contacts and shoe contacts still fail.
                    int nativeIndex = brake.name.StartsWith("s060_caliper_") ? 3 - int.Parse(brake.name.Substring(brake.name.LastIndexOf('_') + 1)) : -1;
                    int wheelIndex = int.Parse(wheel.name.Substring("s060_Wheels_0".Length, 1)) - 1;
                    bool sameAxle = nativeIndex == (wheelIndex < 3 ? wheelIndex : 0);
                    var reference = new GameObject("Unmodified native caliper reference").transform; reference.SetParent(body, false);
                    reference.gameObject.AddComponent<MeshFilter>().sharedMesh = originalMeshes[brakeFilters.IndexOf(brake)];
                    var stockWheel = new GameObject("Unmodified native wheel reference").transform; stockWheel.SetParent(body, false);
                    stockWheel.localPosition = sourceCenters[Mathf.Max(0, nativeIndex)]; stockWheel.localRotation = wheel.transform.localRotation;
                    stockWheel.localScale = new Vector3(wheel.transform.parent == left ? -1 : 1, 1, 1);
                    stockWheel.gameObject.AddComponent<MeshFilter>().sharedMesh = wheel.sharedMesh;
                    bool inherited = sameAxle && new SteamMeshContact.Shell(reference.GetComponent<MeshFilter>(), body).Intersects(new SteamMeshContact.Shell(stockWheel.GetComponent<MeshFilter>(), body));
                    Check(inherited, "Fitted brake contact must reproduce with the exact stock profile: " + name + ": " + hit);
                    if (inherited) nativeContacts.Add(name + ": " + hit); else contacts.Add(name + ": " + hit);
                    Object.DestroyImmediate(reference.gameObject); Object.DestroyImmediate(stockWheel.gameObject);
                }
            }
        }
        finally
        {
            var fitted = brakeFilters.Select(f => f.sharedMesh).ToArray();
            rods?.Dispose(); if (owner != null) Call(owner, "RestoreGeometry"); Call(scale, "Restore"); Call(scale, "Restore");
            for (int i = 0; i < brakeFilters.Count; i++) Check(brakeFilters[i].sharedMesh == originalMeshes[i] && originalMeshes[i] != null, "Restore exact original native meshes");
            Check(!disabled.enabled && renderers.Where(r => r != disabled).All(r => r.enabled), "Restore original renderer visibility");
            if (!baseline) Check(fitted.All(m => m == null), "Release all owned fitted brake meshes");
            Check(ReferenceEquals(heatField.GetValue(heat), originalHeat), "Restore exact native overheating binding");
            if (!baseline && count == 3)
            {
                Call(scale, "Apply", factor);
                Call(scale, "PositionBrakes", Enumerable.Range(1, 3).Select(i => right.Find("s060_Wheels_0" + i)).ToList());
                var destructionCopies = brakeFilters.Select(f => f.sharedMesh).ToArray();
                foreach (var filter in brakeFilters) Object.DestroyImmediate(filter);
                Call(scale, "ReleaseResources"); Call(scale, "ReleaseResources");
                Check(destructionCopies.All(m => m == null) && originalMeshes.All(m => m != null), "Destruction fallback releases owned brake meshes after native filters vanish");
                Check(ReferenceEquals(heatField.GetValue(heat), originalHeat), "Destruction fallback leaves native heat binding intact");
                Call(scale, "Restore");
            }
            Object.DestroyImmediate(root); Object.DestroyImmediate(heatMaterial);
        }
    }
    static void Render(GameObject root, string name)
    {
        // Render the actual transient fixture so the native property block is
        // preserved; cloning renderers does not copy their property blocks.
        var scene = EditorSceneManager.NewPreviewScene(); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
        var cameraGo = new GameObject("Brake preview camera"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGo, scene);
        var camera = cameraGo.AddComponent<Camera>(); camera.scene = scene; camera.orthographic = true; camera.orthographicSize = 1.19f;
        camera.backgroundColor = new Color(.75f, .80f, .84f); camera.clearFlags = CameraClearFlags.SolidColor; camera.transform.position = new Vector3(10, 1.3f, .3f); camera.transform.LookAt(new Vector3(0, .72f, .3f));
        var lamp = new GameObject("Preview light"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lamp, scene); var light = lamp.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.4f; light.transform.rotation = Quaternion.Euler(45, -30, 0);
        var target = new RenderTexture(1800, 650, 24); camera.targetTexture = target; camera.Render(); var previous = RenderTexture.active; RenderTexture.active = target;
        var image = new Texture2D(1800, 650, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 1800, 650), 0, 0); image.Apply(); File.WriteAllBytes(Path.Combine(output, name + ".png"), image.EncodeToPNG());
        RenderTexture.active = previous; camera.targetTexture = null; Object.DestroyImmediate(image); Object.DestroyImmediate(target);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, UnityEngine.SceneManagement.SceneManager.GetActiveScene()); EditorSceneManager.ClosePreviewScene(scene);
    }
}
