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
    private static float maximumPocketError, maximumBufferError, maximumCoupledBufferError, minimumCockProjection = float.PositiveInfinity;
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
                ",\"maximumCoupledBufferErrorMetres\":" + maximumCoupledBufferError.ToString("R", CultureInfo.InvariantCulture) +
                ",\"minimumCockProjectionMetres\":" + minimumCockProjection.ToString("R", CultureInfo.InvariantCulture) +
                ",\"nativeGeometry\":true,\"gameplayValidated\":false,\"failures\":[" +
                string.Join(",", failures.Distinct().Select(s => "\"" + s + "\"")) + "]}");
            bool baseline = Environment.GetEnvironmentVariable("S060_COUPLING_BASELINE") == "1";
            bool reproduced = failures.Contains("Hook pocket alignment") && failures.Contains("Angle cock exposed behind sill");
            bool compressionBaseline = Environment.GetEnvironmentVariable("S060_BUFFER_COMPRESSION_BASELINE") == "1";
            bool coupledBaseline = Environment.GetEnvironmentVariable("S060_COUPLED_SPACING_BASELINE") == "1";
            EditorApplication.Exit(coupledBaseline ? (failures.Contains("Coupled rear buffer spacing matches stock native geometry") ? 0 : 1) :
                compressionBaseline ? (failures.Contains("Native rear buffer compression matches its visible datum") ? 0 : 1) :
                baseline ? (reproduced ? 0 : 1) : (failures.Count == 0 ? 0 : 1));
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
            // The native chain owns a cached local pose independently of the
            // visual rig. Its real enable-entry method must survive relocation.
            var interactionType = game.GetType("ChainCouplerInteraction", true);
            var interaction = chain.gameObject.AddComponent(interactionType);
            interactionType.GetMethod("Awake", Instance).Invoke(interaction, null);
            var cachedPose = interactionType.GetField("originalLocalPosition", Instance);
            var originalCachedPose = (Vector3)cachedPose.GetValue(interaction);
            PrepareEnableEntry(game, interaction, chain, root.transform);
            foreach (string side in new[] { "L", "R" }) buffers.Find("Buffer_R" + side).SetParent(rig.Find(side == "R" ? "buffer anchor left" : "buffer anchor right"), true);
            var bufferController = NativeBuffer(game, rig, rig.Find("buffer anchor left"), rig.Find("buffer anchor right"));
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
            var originalRigToAnchor = Local(rig) - Local(rearAnchor);
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
                Check(Vector3.Distance(Local(hook) - Local(rearAnchor), originalHookToAnchor + new Vector3(0, 0, -.295f)) < .0001f,
                    "Visual hook correction is independent of the physical anchor");
                var points = lever.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v => root.transform.InverseTransformPoint(lever.TransformPoint(v))).ToArray();
                float cockProjection = -7.98f - points.Max(p=>p.z);
                minimumCockProjection = Mathf.Min(minimumCockProjection, cockProjection);
                Check(cockProjection > .02f, "Angle cock exposed behind sill");
                Check(Vector3.Distance(Local(rig) - Local(rearAnchor), originalRigToAnchor) < .0001f,
                    "Physical coupling anchor retains stock buffer offset");
                float bufferError = Mathf.Max(Vector3.Distance(Local(bufferHeads[2]), bufferPositions[2] + new Vector3(0,0,-3.70f)),
                    Vector3.Distance(Local(bufferHeads[3]), bufferPositions[3] + new Vector3(0,0,-3.70f)));
                maximumBufferError = Mathf.Max(maximumBufferError, bufferError);
                Check(bufferError < .0001f, "Existing rear buffers retained");
                Check(Vector3.Distance(Local(bufferHeads[0]), bufferPositions[0]) < .0001f && Vector3.Distance(Local(bufferHeads[1]), bufferPositions[1]) < .0001f, "Front buffers unchanged");
                Check(frontRig.localPosition == originals.Single(x=>x.t==frontRig).p && frontAnchor.localPosition == originals.Single(x=>x.t==frontAnchor).p, "Front coupling unchanged");
                ValidateCompression(game, root.transform, bufferController);
                ValidateCoupledSpacing(game, car, rearCoupler, bufferController, originalRigToAnchor);
                for (int visibilityCycle = 0; visibilityCycle < 3; visibilityCycle++)
                {
                    interactionType.GetMethod("Entry_Enabled", Instance).Invoke(interaction, null);
                    Check(Vector3.Distance(Local(hook) - Local(plate), originalHookToPlate) < .0001f,
                        "Native visibility enable retains relocated hook");
                    foreach (string name in new[] { "knob", "knobFollower" })
                    {
                        var item = (Object)interactionType.GetField(name, Instance).GetValue(interaction);
                        if (item is Component component) Object.DestroyImmediate(component.gameObject);
                        else if (item != null) Object.DestroyImmediate(item);
                    }
                }
                lightsType.GetMethod("Dispose").Invoke(binding, null);
                supportType.GetMethod("Restore", Instance).Invoke(support, null);
                Check((Vector3)cachedPose.GetValue(interaction) == originalCachedPose, "Native cached chain pose restored");
                Check(originals.Where(x=>x.t!=root.transform).All(x=>x.t.parent==x.parent && Vector3.Distance(x.t.localPosition,x.p)<.0001f && Quaternion.Angle(x.t.localRotation,x.r)<.01f && x.t.localScale==x.s), "Exact stock transform restoration");
            }
        }
        finally { Object.DestroyImmediate(root); foreach (var item in owned) if(item!=null) Object.DestroyImmediate(item); }
    }
    private static Transform Group(Transform parent, string name) { var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform; }
    private static Component NativeBuffer(Assembly game, Transform rig, Transform left, Transform right)
    {
        var scanner=rig.gameObject.AddComponent(game.GetType("CouplingScanner",true));
        var controller=rig.gameObject.AddComponent(game.GetType("TrainBufferController",true));
        var type=controller.GetType();
        type.GetField("couplingScanner").SetValue(controller,scanner);
        type.GetField("bufferAnchorLeft").SetValue(controller,left);
        type.GetField("bufferAnchorRight").SetValue(controller,right);
        type.GetField("bufferModelLeft").SetValue(controller,left.childCount>0?left.GetChild(0):Group(left,"buffer model"));
        type.GetField("bufferModelRight").SetValue(controller,right.childCount>0?right.GetChild(0):Group(right,"buffer model"));
        type.GetMethod("Awake",Instance).Invoke(controller,null);
        return controller;
    }
    private static void ValidateCompression(Assembly game, Transform root, Component actual)
    {
        var type=actual.GetType();var left=(Transform)type.GetField("bufferAnchorLeft").GetValue(actual);
        var right=(Transform)type.GetField("bufferAnchorRight").GetValue(actual);var basis=left.parent;
        var reference=Group(root,"Native buffer reference"); reference.SetPositionAndRotation(basis.position,basis.rotation);
        var opponent=Group(root,"Native opposing buffers");
        Component Make(Transform rig)
        {
            var l=Group(rig,"left");l.localPosition=new Vector3(-.864f,0,.3f);
            var r=Group(rig,"right");r.localPosition=new Vector3(.864f,0,.3f);
            return NativeBuffer(game,rig,l,r);
        }
        var expected=Make(reference);var other=Make(opponent);
        try
        {
            foreach(float yaw in new[]{-5f,0f,5f})foreach(float distance in new[]{.8f,.6f,.45f,.3f,.1f,0f})
            {
                opponent.SetPositionAndRotation(basis.TransformPoint(new Vector3(0,0,distance)),basis.rotation*Quaternion.Euler(0,180+yaw,0));
                type.GetMethod("SetOtherBuffer").Invoke(actual,new object[]{other});
                type.GetMethod("SetOtherBuffer").Invoke(expected,new object[]{other});
                type.GetMethod("UpdateBuffers").Invoke(actual,null);type.GetMethod("UpdateBuffers").Invoke(expected,null);
                var expectedLeft=(Transform)type.GetField("bufferAnchorLeft").GetValue(expected);
                var expectedRight=(Transform)type.GetField("bufferAnchorRight").GetValue(expected);
                Check(Mathf.Abs(left.localPosition.z-expectedLeft.localPosition.z)<.0001f&&Mathf.Abs(right.localPosition.z-expectedRight.localPosition.z)<.0001f,
                    "Native rear buffer compression matches its visible datum");
            }
            type.GetMethod("SetOtherBuffer").Invoke(actual,new object[]{null});
            Check(Mathf.Abs(left.localPosition.z-.3f)<.0001f&&Mathf.Abs(right.localPosition.z-.3f)<.0001f,"Uncoupling restores both native rear buffers");
        }
        finally {Object.DestroyImmediate(reference.gameObject);Object.DestroyImmediate(opponent.gameObject);}
    }
    private static void ValidateCoupledSpacing(Assembly game, Component car, Component rearCoupler,
        Component actual, Vector3 stockRigToAnchor)
    {
        // The previous fixture imposed buffer-rig separations directly. Here
        // car poses use the native tight/loose target separations and actual
        // joint anchors are read back. Chain adaptation, spring settling and
        // rail dynamics are not simulated.
        var carType = car.GetType(); var couplerType = rearCoupler.GetType(); var type = actual.GetType();
        var left = (Transform)type.GetField("bufferAnchorLeft").GetValue(actual);
        var right = (Transform)type.GetField("bufferAnchorRight").GetValue(actual);
        var reference = Group(car.transform, "Stock rear buffer datum at coupled anchor");
        reference.SetPositionAndRotation(rearCoupler.transform.position + car.transform.TransformVector(stockRigToAnchor), actual.transform.rotation);
        var opponent = Group(car.transform, "Stock opposing coupled car");
        var rb = car.gameObject.GetComponent<Rigidbody>();
        if (rb == null) rb = car.gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        var otherCar = opponent.gameObject.AddComponent(carType);
        opponent.gameObject.AddComponent<Rigidbody>().isKinematic = true;
        var otherCoupler = Group(opponent, "front coupler").gameObject.AddComponent(couplerType);
        couplerType.GetField("train").SetValue(rearCoupler, car);
        couplerType.GetField("train").SetValue(otherCoupler, otherCar);
        couplerType.GetField("coupledTo").SetValue(rearCoupler, otherCoupler);
        couplerType.GetField("coupledTo").SetValue(otherCoupler, rearCoupler);
        Component Make(Transform rig)
        {
            var l = Group(rig, "left"); l.localPosition = new Vector3(-.864f, 0, .3f);
            var r = Group(rig, "right"); r.localPosition = new Vector3(.864f, 0, .3f);
            return NativeBuffer(game, rig, l, r);
        }
        var expected = Make(reference);
        var otherRig = Group(opponent, "front buffer rig"); otherRig.localPosition = -stockRigToAnchor;
        var other = Make(otherRig);
        try
        {
            foreach (float limit in new[] { .2f, .3f })
            {
                opponent.SetPositionAndRotation(rearCoupler.transform.position + rearCoupler.transform.forward * (limit - .2f),
                    rearCoupler.transform.rotation * Quaternion.Euler(0, 180, 0));
                couplerType.GetMethod("CreateRigidJoint", Instance).Invoke(rearCoupler, null);
                var joint = (ConfigurableJoint)couplerType.GetField("rigidCJ").GetValue(rearCoupler);
                Check(Mathf.Abs(Vector3.Distance(car.transform.TransformPoint(joint.anchor),
                    opponent.TransformPoint(joint.connectedAnchor)) - limit) < .0001f,
                    "Native coupled anchors exercise tight and loose spacing");
                type.GetMethod("SetOtherBuffer").Invoke(actual, new object[] { other });
                type.GetMethod("SetOtherBuffer").Invoke(expected, new object[] { other });
                type.GetMethod("UpdateBuffers").Invoke(actual, null); type.GetMethod("UpdateBuffers").Invoke(expected, null);
                float error = Mathf.Max(Vector3.Distance(left.position, ((Transform)type.GetField("bufferAnchorLeft").GetValue(expected)).position),
                    Vector3.Distance(right.position, ((Transform)type.GetField("bufferAnchorRight").GetValue(expected)).position));
                maximumCoupledBufferError = Mathf.Max(maximumCoupledBufferError, error);
                Check(error < .0001f, "Coupled rear buffer spacing matches stock native geometry");
                Object.DestroyImmediate(joint);
            }
        }
        finally
        {
            type.GetMethod("SetOtherBuffer").Invoke(actual, new object[] { null });
            couplerType.GetField("coupledTo").SetValue(rearCoupler, null);
            Object.DestroyImmediate(reference.gameObject); Object.DestroyImmediate(opponent.gameObject);
        }
    }
    private static void PrepareEnableEntry(Assembly game, Component interaction, Transform chain, Transform root)
    {
        var type = interaction.GetType();
        Type Native(string name)
        {
            var found = game.GetType(name) ?? AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).FirstOrDefault(t=>t!=null);
            if (found != null) return found;
            foreach (var reference in game.GetReferencedAssemblies())
            {
                var assembly = Assembly.Load(reference);
                found = assembly.GetType(name); if (found != null) return found;
            }
            throw new TypeLoadException(name);
        }
        var prefab = Group(root, "Inactive native knob fixture").gameObject; prefab.SetActive(false);
        var highlight = prefab.AddComponent(Native("HighlightTag"));
        var list = highlight.GetType().GetField("renderers");
        if (list.GetValue(highlight) == null) list.SetValue(highlight, Activator.CreateInstance(list.FieldType));
        prefab.AddComponent(Native("DV.CabControls.NonVR.GizmoNonVR"));
        type.GetField("knobPrefab").SetValue(interaction, prefab);
        type.GetField("parkedAnchor").SetValue(interaction, Group(chain, "Native parked anchor fixture"));
        foreach (string name in new[] { "draggingIK", "attachedIK", "otherAttachedIK", "ownAttachPoint" })
        {
            var field = type.GetField(name);
            field.SetValue(interaction, Group(root, "Inactive " + name).gameObject.AddComponent(field.FieldType));
        }
    }
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
