#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Exact native coupling transforms/meshes, candidate relocation, buffer binding
// and restoration. No rail physics, live brake simulation or gameplay is run.
public static class MeyerRearCouplingNativeAudit
{
    [Serializable] private class Data { public Node[] nodes; public Entry[] meshes; }
    [Serializable] private class Node { public string name, mesh; public Vector3 position, scale; public Quaternion rotation; public Node[] children; }
    [Serializable] private class Entry { public string name; public Vector3[] vertices; public int[] triangles; }
    private const BindingFlags Instance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private const BindingFlags Static = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    private static string managed;
    private static int checks;
    private static float maximumPocketError, maximumBufferError, minimumCockProjection = float.PositiveInfinity;
    private static readonly List<string> failures = new List<string>();
    public static void RunBatch()
    {
        string output = Environment.GetEnvironmentVariable("S060_COUPLING_OUTPUT");
        string runtime = Environment.GetEnvironmentVariable("S060_EXHAUST_AUDIT_RUNTIME");
        managed = Environment.GetEnvironmentVariable("S060_AUDIT_MANAGED");
        Directory.CreateDirectory(output);
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        try
        {
            var game = Assembly.LoadFrom(Path.Combine(managed, "Assembly-CSharp.dll"));
            var mod = Assembly.LoadFrom(runtime);
            var fixture = JsonUtility.FromJson<Data>(File.ReadAllText(Environment.GetEnvironmentVariable("S060_COUPLING_FIXTURE")));
            var lamps = JsonUtility.FromJson<Data>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("S060_NATIVE_PREVIEW")), "meyer-lights-native.json")));
            foreach (bool reparented in new[] { false, true }) foreach (float yaw in new[] { 0f, 73f })
                Validate(game, mod, fixture, lamps, reparented, yaw);
            if (Environment.GetEnvironmentVariable("S060_COUPLING_BASELINE") != "1") MeyerLightsAudit.Validate(output);
            string hash = Hash(runtime);
            File.WriteAllText(Path.Combine(output, "rear-coupling-validation.json"),
                "{\"passed\":" + (failures.Count == 0 ? "true" : "false") + ",\"checks\":" + checks +
                ",\"runtimeSha256\":\"" + hash + "\",\"fixtureSha256\":\"" + Hash(Environment.GetEnvironmentVariable("S060_COUPLING_FIXTURE")) +
                "\",\"maximumPocketErrorMetres\":" + maximumPocketError.ToString("R", CultureInfo.InvariantCulture) +
                ",\"maximumBufferErrorMetres\":" + maximumBufferError.ToString("R", CultureInfo.InvariantCulture) +
                ",\"minimumCockProjectionMetres\":" + minimumCockProjection.ToString("R", CultureInfo.InvariantCulture) +
                ",\"nativeGeometry\":true,\"gameplayValidated\":false,\"failures\":[" +
                string.Join(",", failures.Distinct().Select(s => "\"" + s + "\"")) + "]}");
            bool baseline = Environment.GetEnvironmentVariable("S060_COUPLING_BASELINE") == "1";
            bool reproduced = failures.Contains("Hook pocket alignment") && failures.Contains("Angle cock exposed behind sill");
            EditorApplication.Exit(baseline ? (reproduced ? 0 : 1) : (failures.Count == 0 ? 0 : 1));
        }
        catch (Exception ex) { File.WriteAllText(Path.Combine(output, "error.txt"), ex.ToString()); Debug.LogException(ex); EditorApplication.Exit(1); }
        finally { AppDomain.CurrentDomain.AssemblyResolve -= Resolve; }
    }
    private static void Validate(Assembly game, Assembly mod, Data fixture, Data lamps, bool reparented, float yaw)
    {
        var owned = new List<Object>();
        var root = new GameObject("Inactive native Meyer coupling audit"); root.SetActive(false);
        try
        {
            var carType = game.GetType("TrainCar", true);
            var car = root.AddComponent(carType);
            var meshes = fixture.meshes.Concat(lamps.meshes).GroupBy(m => m.name).ToDictionary(g => g.Key, g => Mesh(g.First(), owned));
            foreach (var node in fixture.nodes) Tree(node, root.transform, meshes);
            var buffers = root.transform.Find("[buffers]");
            var rig = buffers.Cast<Transform>().Single(t => t.name == "BuffersAndChainRig" && t.localPosition.z < 0);
            var frontRig = buffers.Cast<Transform>().Single(t => t.name == "BuffersAndChainRig" && t.localPosition.z > 0);
            var rearAnchor = root.transform.Find("[coupler rear]");
            var frontAnchor = root.transform.Find("[coupler front]");
            var couplerType = game.GetType("Coupler", true);
            var rearCoupler = rearAnchor.gameObject.AddComponent(couplerType);
            var couplers = Array.CreateInstance(couplerType, 2);
            couplers.SetValue(frontAnchor.gameObject.AddComponent(couplerType), 0); couplers.SetValue(rearCoupler, 1);
            carType.GetField("couplers").SetValue(car, couplers);
            var visual = rig.gameObject.AddComponent(game.GetType("VisualCouplerInit", true));
            var chain = rig.Find("ChainCoupler"); var hoses = rig.Find("hoses");
            visual.GetType().GetField("chain").SetValue(visual, chain); visual.GetType().GetField("hoses").SetValue(visual, hoses);
            couplerType.GetField("visualCoupler").SetValue(rearCoupler, visual);
            var interior = Group(root.transform, "Native interior");
            if (reparented) { chain.SetParent(interior, true); hoses.SetParent(interior, true); }
            foreach (string side in new[] { "L", "R" }) buffers.Find("Buffer_R" + side).SetParent(rig.Find(side == "R" ? "buffer anchor left" : "buffer anchor right"), true);
            var body = Group(root.transform, "Body");
            foreach (string side in new[] { "left", "right" }) Group(body, "s060_taillight_lower_" + side);
            var upper = Group(body, "s060_taillight_upper"); upper.gameObject.AddComponent<MeshFilter>().sharedMesh = meshes["Buffer04"];
            var stem = Group(body, "s060_buffer_stems"); stem.gameObject.AddComponent<MeshFilter>().sharedMesh = meshes["s060_buffer_stems"];
            stem.gameObject.AddComponent<MeshRenderer>();
            var rearLights = Group(Group(root.transform, "LocoS060_Headlights"), "RearSide");
            foreach (string name in new[] { "s060_headlight_glass_R_LO", "HeadlightsLowLeft", "HeadlightsLowRight", "s060_headlight_glass_R_HI", "HeadlightHigh" }) Group(rearLights, name);
            var datum = Group(root.transform, "Fixed Meyer rear datum"); datum.localPosition = new Vector3(0, 0, -6.10f);
            var bogieType = game.GetType("Bogie", true); var bogies = Array.CreateInstance(bogieType, 1);
            bogies.SetValue(Group(root.transform, "Inert native bogie").gameObject.AddComponent(bogieType), 0);
            carType.GetField("_bogies", Instance).SetValue(car, bogies);
            var supportType = mod.GetType("RearrangedS060.S060MeyerSupport", true);
            var support = root.AddComponent(supportType);
            supportType.GetField("car", Instance).SetValue(support, car);
            supportType.GetField("rearAnchor", Instance).SetValue(support, rearAnchor.localPosition);
            supportType.GetField("logicLength", Instance).SetValue(support, 9.14f);
            var hook = chain.Find("drawgear pivot/CarHook"); var plate = buffers.Find("HookPlate_R");
            var lever = hoses.Find("CouplingHoseRig/cock valve/CableBaseLever model");
            Vector3 Local(Transform part) => root.transform.InverseTransformPoint(part.position);
            var originalHookToPlate = Local(hook) - Local(plate); var originalHookToAnchor = Local(hook) - Local(rearAnchor);
            var bufferHeads = new[] { buffers.Find("Buffer_FR"), buffers.Find("Buffer_FL"), rig.Find("buffer anchor left").GetChild(0), rig.Find("buffer anchor right").GetChild(0) };
            var bufferPositions = bufferHeads.Select(Local).ToArray();
            var originals = root.GetComponentsInChildren<Transform>(true).Select(t => new { t, parent=t.parent, p=t.localPosition, r=t.localRotation, s=t.localScale }).ToArray();
            root.transform.SetPositionAndRotation(new Vector3(120, 15, -83), Quaternion.Euler(0, yaw, 0));
            for (int cycle = 0; cycle < 2; cycle++)
            {
                supportType.GetField("<Active>k__BackingField", Instance).SetValue(support, true);
                var relocate = supportType.GetMethod("MoveRearCoupling", Instance);
                if (relocate != null) relocate.Invoke(support, null);
                else
                {
                    // Original Apply's pre-fix call sequence; track relocation is outside this fixture.
                    var old = rearAnchor.localPosition; var target = old; target.z = -8.27f; rearAnchor.localPosition = target;
                    supportType.GetMethod("MoveCouplingVisuals", Instance).Invoke(support, new object[] { root.transform.TransformVector(target - old) });
                }
                var lightsType = mod.GetType("RearrangedS060.S060MeyerLights", true);
                var binding = lightsType.GetMethod("Bind", Static).Invoke(null, new object[] { root.transform, body, datum, Local(rearAnchor) - originals.Single(x=>x.t==rearAnchor).p, true });
                float pocketError = Vector3.Distance(Local(hook) - Local(plate), originalHookToPlate);
                maximumPocketError = Mathf.Max(maximumPocketError, pocketError);
                Check(pocketError < .0001f, "Hook pocket alignment");
                Check(Vector3.Distance(Local(hook) - Local(rearAnchor), originalHookToAnchor) < .0001f, "Native hook to physical anchor relationship");
                var points = lever.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v => root.transform.InverseTransformPoint(lever.TransformPoint(v))).ToArray();
                float cockProjection = -7.98f - points.Max(p=>p.z);
                minimumCockProjection = Mathf.Min(minimumCockProjection, cockProjection);
                Check(cockProjection > .02f, "Angle cock exposed behind sill");
                Check(Mathf.Abs(Local(rearAnchor).z + 8.565f) < .0001f, "Aligned physical coupling anchor");
                float bufferError = Mathf.Max(Vector3.Distance(Local(bufferHeads[2]), bufferPositions[2] + new Vector3(0,0,-3.70f)),
                    Vector3.Distance(Local(bufferHeads[3]), bufferPositions[3] + new Vector3(0,0,-3.70f)));
                maximumBufferError = Mathf.Max(maximumBufferError, bufferError);
                Check(bufferError < .0001f, "Existing rear buffers retained");
                Check(Vector3.Distance(Local(bufferHeads[0]), bufferPositions[0]) < .0001f && Vector3.Distance(Local(bufferHeads[1]), bufferPositions[1]) < .0001f, "Front buffers unchanged");
                Check(frontRig.localPosition == originals.Single(x=>x.t==frontRig).p && frontAnchor.localPosition == originals.Single(x=>x.t==frontAnchor).p, "Front coupling unchanged");
                lightsType.GetMethod("Dispose").Invoke(binding, null);
                supportType.GetMethod("Restore", Instance).Invoke(support, null);
                Check(originals.Where(x=>x.t!=root.transform).All(x=>x.t.parent==x.parent && Vector3.Distance(x.t.localPosition,x.p)<.0001f && Quaternion.Angle(x.t.localRotation,x.r)<.01f && x.t.localScale==x.s), "Exact stock transform restoration");
            }
        }
        finally { Object.DestroyImmediate(root); foreach (var item in owned) if(item!=null) Object.DestroyImmediate(item); }
    }
    private static Transform Group(Transform parent, string name) { var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform; }
    private static Transform Tree(Node node, Transform parent, Dictionary<string,Mesh> meshes)
    {
        var part=Group(parent,node.name); part.localPosition=node.position; part.localRotation=node.rotation; part.localScale=node.scale;
        if(!string.IsNullOrEmpty(node.mesh)) part.gameObject.AddComponent<MeshFilter>().sharedMesh=meshes[node.mesh];
        foreach(var child in node.children) Tree(child,part,meshes); return part;
    }
    private static Mesh Mesh(Entry entry, List<Object> owned) { var mesh=new Mesh {name=entry.name}; mesh.vertices=entry.vertices; mesh.triangles=entry.triangles; mesh.RecalculateBounds(); owned.Add(mesh); return mesh; }
    private static void Check(bool value,string message) { checks++; if(!value) failures.Add(message); }
    private static string Hash(string path) { using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", ""); }
    private static Assembly Resolve(object sender,ResolveEventArgs args)
    {
        string name=new AssemblyName(args.Name).Name; var loaded=AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a=>a.GetName().Name==name); if(loaded!=null)return loaded;
        foreach(string directory in new[]{managed,Path.Combine(managed,"UnityModManager"),Path.Combine(Environment.GetEnvironmentVariable("S060_AUDIT_MODS"),"LocoMeshSplitter")})
        {string path=Path.Combine(directory,name+".dll"); if(File.Exists(path))return Assembly.LoadFrom(path);} return null;
    }
}
#endif
