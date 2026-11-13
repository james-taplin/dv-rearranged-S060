using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using CCL.Types;
using RearrangedS060;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Executes the shipping collision owner against the authored rear shell,
// including automatic mass-frame invariants and native collision readiness.
// Actual contact delivery/joint collision policy run in the separate PlayMode
// contact audit. This static-pose audit is not a gameplay damage test.
public static class MeyerCollisionAudit
{
    public static void RunBatch()
    {
        string output = Environment.GetEnvironmentVariable("S060_COLLISION_AUDIT_OUTPUT");
        if (string.IsNullOrEmpty(output)) throw new InvalidOperationException("S060_COLLISION_AUDIT_OUTPUT is required.");
        Directory.CreateDirectory(output);
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        try { Validate(output); EditorApplication.Exit(0); }
        catch (Exception ex) { File.WriteAllText(Path.Combine(output, "collision-error.txt"), ex.ToString()); Debug.LogException(ex); EditorApplication.Exit(1); }
        finally { AppDomain.CurrentDomain.AssemblyResolve -= Resolve; }
    }

    public static void Validate(string output)
    {
        // A MonoBehaviour copied under Assets/Editor cannot be attached to a
        // GameObject. Load the candidate DLL instead, exactly as the native
        // exhaust/cleanup audits do, so this executes the shipping component.
        string runtime = Environment.GetEnvironmentVariable("S060_EXHAUST_AUDIT_RUNTIME");
        Require(!string.IsNullOrEmpty(runtime) && File.Exists(runtime), "S060_EXHAUST_AUDIT_RUNTIME must identify the candidate runtime DLL.");
        var runtimeAssembly = Assembly.LoadFrom(runtime);
        var ownerType = runtimeAssembly.GetType("RearrangedS060.S060MeyerCollision", true);
        var create = ownerType.GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic);
        var bindFrame = ownerType.GetMethod("BindFrame", BindingFlags.Instance | BindingFlags.NonPublic);
        var syncPose = ownerType.GetMethod("SyncPose", BindingFlags.Instance | BindingFlags.NonPublic);
        var bindNative = ownerType.GetMethod("BindNativeBogie", BindingFlags.Instance | BindingFlags.NonPublic);
        var encapsulate = ownerType.GetMethod("Encapsulate", BindingFlags.Instance | BindingFlags.NonPublic);
        AssetBundle bundle = null;
        GameObject prefab;
        string releasedBundle = Environment.GetEnvironmentVariable("S060_MEYER_AUDIT_BUNDLE");
        if (!string.IsNullOrEmpty(releasedBundle))
        {
            // A fresh audit project need not contain editable donor assets.
            // Use the exact released bundle already selected by Test.Runtime.
            bundle = AssetBundle.LoadFromFile(releasedBundle);
            Require(bundle != null, "Released Meyer bundle is required.");
            prefab = bundle.LoadAllAssets<CustomCarPack>().Single().Cars.SelectMany(c=>c.liveries).Single().prefab;
        }
        else
        {
            string folder = "Assets/_CCL_CARS/" + MeyerPlan.CarName;
            var ids = AssetDatabase.FindAssets("t:CustomCarVariant", new[] { folder });
            Require(ids.Length == 1, "One authored Meyer variant is required.");
            prefab = AssetDatabase.LoadAssetAtPath<CustomCarVariant>(AssetDatabase.GUIDToAssetPath(ids[0])).prefab;
        }
        var prefabFrame = prefab.GetComponentsInChildren<Transform>(true).Where(t => t.name == "bogie_car")
            .OrderBy(t => prefab.transform.InverseTransformPoint(t.position).z).First();

        var car = new GameObject("Meyer collision audit native car root");
        var bogie = new GameObject("Meyer collision audit native rear bogie");
        var material = new PhysicMaterial("Native material QA stand-in");
        int checks = 0; float maximumGap = 0;
        try
        {
            var body = car.AddComponent<Rigidbody>(); body.isKinematic = true;
            var collisionRoot = new GameObject("Native collision root").transform; collisionRoot.SetParent(car.transform, false);
            // A translated/rotated collider root detects accidental car-space
            // assumptions. Both native and authored rear-frame scales are one.
            collisionRoot.localPosition = new Vector3(.2f, .1f, .35f); collisionRoot.localRotation = Quaternion.Euler(0, 7, 0);
            var stockCollider = collisionRoot.gameObject.AddComponent<BoxCollider>(); stockCollider.size = new Vector3(2, 3, 8);
            stockCollider.sharedMaterial = material; stockCollider.gameObject.layer = 8;
            var stockCentre = stockCollider.center; var stockSize = stockCollider.size;
            UnityEngine.Physics.SyncTransforms();
            var nativeMassCentre = body.centerOfMass; var nativeInertia = body.inertiaTensor; var nativeInertiaRotation = body.inertiaTensorRotation;
            float nativeMass = body.mass;
            body.mass = nativeMass * .9f;
            var nativeReducedInertia = body.inertiaTensor; var nativeReducedInertiaRotation = body.inertiaTensorRotation;
            body.mass = nativeMass;
            var bogieBody = bogie.AddComponent<Rigidbody>(); bogieBody.isKinematic = true;
            var initialFrame = new GameObject("Native bogie_car before authored frame binding").transform; initialFrame.SetParent(bogie.transform, false);
            var visual = Object.Instantiate(prefabFrame.gameObject, initialFrame).transform;
            visual.localPosition = Vector3.zero;
            var deck = visual.GetComponentsInChildren<MeshFilter>(true).Single(f => f.name == "Pannier deck");
            var beam = visual.GetComponentsInChildren<MeshFilter>(true).Single(f => f.name == "Rear buffer beam");
            var geometries = new[] { (name: "Rear deck", bounds: GeometryBounds(visual, new[] { deck })),
                (name: "Rear buffer beam", bounds: GeometryBounds(visual, new[] { beam })) }
                .Concat(new[] { -1, 1 }.Select(side => {
                    var tank = visual.Find(side < 0 ? "[Meyer rear pannier L]" : "[Meyer rear pannier R]");
                    Require(tank != null, "Authored rear pannier must exist.");
                    return (name: "Rear pannier " + side, bounds: GeometryBounds(visual, tank.GetComponentsInChildren<MeshFilter>(true)
                        .Where(f => f.name == "Tank shell" || f.name == "Rounded tank end").ToArray()));
                })).ToArray();

            // Repeat removal/rebuild too: pooling must not accumulate active
            // boxes, and the old source must cease controlling a rebound shell.
            for (int cycle = 0; cycle < 3; cycle++)
            {
                var binding = (MonoBehaviour)create.Invoke(null, new object[] { collisionRoot, initialFrame, stockCollider });
                var cleanup = new DisposeCounter();
                ((IList)ownerType.GetField("contactBindings", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(binding)).Add(cleanup);
                var ownedGroup = binding.gameObject;
                try
                {
                    Require(binding.transform.parent == initialFrame && binding.GetComponent<Rigidbody>() == null,
                        "The owned shell belongs to the existing rear frame, without a new Rigidbody."); checks++;
                    Require(binding.GetType().Assembly == runtimeAssembly, "The loaded candidate DLL must own the executing collision component."); checks++;
                    bindFrame.Invoke(binding, new object[] { visual });
                    Require(binding.transform.parent == visual && binding.transform.localPosition == Vector3.zero &&
                        binding.transform.localRotation == Quaternion.identity && binding.transform.localScale == Vector3.one,
                        "Rebinding attaches once with an identity local pose; no per-frame body-space copy is needed."); checks++;
                    // Separate the old source after rebinding. Following that
                    // stale source must no longer influence the owned shell.
                    visual.SetParent(bogie.transform, true);
                    initialFrame.localPosition = new Vector3(5, 0, 0);
                    foreach (float bodyYaw in new[] { 0f, 73f, 179f })
                        foreach (float yaw in new[] { -15f, -12f, 0f, 12f, 15f })
                            foreach (bool rerailed in new[] { false, true })
                            {
                                car.transform.SetPositionAndRotation(new Vector3(120, 15, -83), Quaternion.Euler(0, bodyYaw, 0));
                                // Native bogie Rigidbodies can live outside the
                                // car transform hierarchy. Copy an independent
                                // world pose, including a rerail pitch/roll.
                                bogie.transform.SetPositionAndRotation(car.transform.TransformPoint(new Vector3(0, rerailed ? .12f : 0, MeyerPlan.RearCentre)),
                                    car.transform.rotation * Quaternion.Euler(rerailed ? 3 : 0, yaw, rerailed ? -2 : 0));
                                syncPose.Invoke(binding, null); UnityEngine.Physics.SyncTransforms();
                                foreach (var geometry in geometries)
                                {
                                    var box = binding.transform.Find(geometry.name).GetComponent<BoxCollider>();
                                    Require(box.attachedRigidbody == bogieBody && box.attachedRigidbody != body,
                                        "Every rear shell box belongs to the existing native REAR BOGIE Rigidbody."); checks++;
                                    Require(box.sharedMaterial == material && box.gameObject.layer == stockCollider.gameObject.layer && !box.isTrigger,
                                        "Rear shell boxes retain native collision material/layer semantics."); checks++;
                                    Require(Vector3.Distance(box.center, geometry.bounds.center) < .00001f &&
                                        Vector3.Distance(box.size, geometry.bounds.size) < .00001f,
                                        "Rear shell box dimensions must match actual authored shell geometry: " + geometry.name); checks++;
                                    for (int corner = 0; corner < 8; corner++)
                                    {
                                        var signs = new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1);
                                        var local = geometry.bounds.center + Vector3.Scale(signs, geometry.bounds.size) / 2;
                                        var expected = visual.TransformPoint(local);
                                        float gap = Vector3.Distance(box.transform.TransformPoint(box.center + Vector3.Scale(signs, box.size) / 2), expected);
                                        maximumGap = Mathf.Max(maximumGap, gap);
                                        Require(gap < .0001f, "Collision corners must follow actual rendered rear-frame pose, including body yaw and rerail."); checks++;
                                        var enclosure = (Bounds)encapsulate.Invoke(binding, new object[] { new Bounds(Vector3.zero, Vector3.one), car.transform });
                                        enclosure.Expand(.0002f);
                                        Require(enclosure.Contains(car.transform.InverseTransformPoint(expected)), "Car-local bounds include translated/rotated collision corners."); checks++;
                                    }
                                }
                                Require(stockCollider.center == stockCentre && stockCollider.size == stockSize && stockCollider.enabled,
                                    "Native stock collider must never be moved or resized by the rear-shell follower."); checks++;
                                Require(Vector3.Distance(body.centerOfMass, nativeMassCentre) < .00001f &&
                                    Vector3.Distance(body.inertiaTensor, nativeInertia) < .001f && Quaternion.Angle(body.inertiaTensorRotation, nativeInertiaRotation) < .001f,
                                    "Rear articulation must not feed moving colliders into the native CAR automatic centre of mass or inertia."); checks++;
                            }
                    var boxes = binding.GetComponentsInChildren<BoxCollider>();
                    stockCollider.isTrigger = true; syncPose.Invoke(binding, null);
                    Require(boxes.All(b => b.isTrigger), "Rear shell mirrors native temporary trigger/rerail policy."); checks++;
                    stockCollider.enabled = false; syncPose.Invoke(binding, null);
                    Require(boxes.All(b => !b.enabled), "Rear shell mirrors disabled native collision."); checks++;
                    stockCollider.enabled = true; stockCollider.isTrigger = false; collisionRoot.gameObject.SetActive(false); syncPose.Invoke(binding, null);
                    Require(boxes.All(b => !b.enabled), "Rear shell mirrors inactive native collision root."); checks++;
                    collisionRoot.gameObject.SetActive(true); syncPose.Invoke(binding, null);
                    Require(boxes.All(b => b.enabled && !b.isTrigger), "Rear shell resumes only the current native collision policy."); checks++;
                    var dummy = new GameObject("Inactive native Bogie readiness fixture"); dummy.SetActive(false);
                    try
                    {
                        var nativeBogie = dummy.AddComponent(bindNative.GetParameters()[0].ParameterType);
                        var rbField = nativeBogie.GetType().GetField("rb", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        var derailed = nativeBogie.GetType().GetField("<HasDerailed>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
                        bindNative.Invoke(binding, new object[] { nativeBogie });
                        Require(boxes.All(b => !b.enabled), "Absent native Bogie.rb disables shell even while its soon-to-be-destroyed Rigidbody component still exists."); checks++;
                        rbField.SetValue(nativeBogie, bogieBody); derailed.SetValue(nativeBogie, true); syncPose.Invoke(binding, null);
                        Require(boxes.All(b => !b.enabled), "Derailed native bogie never activates the added shell on the car fallback body."); checks++;
                        derailed.SetValue(nativeBogie, false); syncPose.Invoke(binding, null);
                        Require(boxes.All(b => b.enabled && b.attachedRigidbody == bogieBody), "Rerailed native rear-body ownership re-enables shell."); checks++;
                    }
                    finally { bindNative.Invoke(binding, new object[] { null }); Object.DestroyImmediate(dummy); }
                    float mass = body.mass;
                    body.mass = mass * .9f;
                    Require(Mathf.Abs(body.mass - mass * .9f) < .00001f && Vector3.Distance(body.centerOfMass, nativeMassCentre) < .00001f &&
                        Vector3.Distance(body.inertiaTensor, nativeReducedInertia) < .001f && Quaternion.Angle(body.inertiaTensorRotation, nativeReducedInertiaRotation) < .001f,
                        "Mass assignment retains the unmodified native body's observed inertia response; the shell must not pin car mass properties."); checks++;
                    body.mass = mass;
                }
                finally { ((IDisposable)binding).Dispose(); }
                ((IDisposable)binding).Dispose();
                Require(cleanup.calls == 1, "Normal collision release disposes contact bindings exactly once."); checks++;
                Require((ownedGroup == null || !ownedGroup.activeSelf) && collisionRoot.GetComponentsInChildren<BoxCollider>().Length == 1,
                    "Returning to stock immediately disables all added rear collision boxes."); checks++;
                Require(stockCollider.sharedMaterial == material && stockCollider.center == stockCentre && stockCollider.size == stockSize,
                    "Stock collider dimensions and material remain exact across repeated restoration."); checks++;
                if (ownedGroup != null) Object.DestroyImmediate(ownedGroup);
                initialFrame.localPosition = Vector3.zero;
            }
            string runtimeHash;
            using (var sha = SHA256.Create()) using (var stream = File.OpenRead(runtime))
                runtimeHash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
            File.WriteAllText(Path.Combine(output, "meyer-collision-validation.json"), JsonUtility.ToJson(new Report {
                passed = true, checks = checks, maximumCornerGapMetres = maximumGap,
                runtimeSha256 = runtimeHash,
                productionCollisionOwner = true, actualAuthoredShellDimensions = true, rearBogieRigidbodyOwnership = true,
                nativeCarAutomaticMassFramePreserved = true, nativeCollisionPolicyMirrored = true,
                nativeBogieReadinessChecked = true,
                rearBogieIndependentWorldHierarchy = true, bodyYawAndRerailPose = true, repeatedStockRestoration = true,
                gameRuntimeValidated = false
            }, true));
        }
        finally { Object.DestroyImmediate(bogie); Object.DestroyImmediate(car); Object.DestroyImmediate(material); if(bundle!=null)bundle.Unload(true); }
    }


    private static Bounds GeometryBounds(Transform frame, MeshFilter[] filters)
    {
        var vertices = filters.SelectMany(f => f.sharedMesh.vertices.Select(v => frame.InverseTransformPoint(f.transform.TransformPoint(v)))).ToArray();
        Require(vertices.Length != 0, "Authored collision shell geometry is required.");
        var bounds = new Bounds(vertices[0], Vector3.zero); foreach (var vertex in vertices) bounds.Encapsulate(vertex); return bounds;
    }
    private sealed class DisposeCounter : IDisposable { internal int calls; public void Dispose() { calls++; } }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        string name = new AssemblyName(args.Name).Name;
        var loaded = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name);
        if (loaded != null) return loaded;
        string managed = Environment.GetEnvironmentVariable("S060_AUDIT_MANAGED");
        if (string.IsNullOrEmpty(managed)) managed = @"B:\SteamLibrary\steamapps\common\Derail Valley\DerailValley_Data\Managed";
        string mods = Environment.GetEnvironmentVariable("S060_AUDIT_MODS");
        if (string.IsNullOrEmpty(mods)) mods = Path.GetFullPath(Path.Combine(managed, "..", "..", "Mods"));
        foreach (string directory in new[] { managed, Path.Combine(managed, "UnityModManager"), Path.Combine(mods, "LocoMeshSplitter") })
        { var path = Path.Combine(directory, name + ".dll"); if (File.Exists(path)) return Assembly.LoadFrom(path); }
        return null;
    }
    [Serializable] private sealed class Report
    {
        public bool passed, productionCollisionOwner, actualAuthoredShellDimensions, rearBogieRigidbodyOwnership,
            nativeCarAutomaticMassFramePreserved, nativeCollisionPolicyMirrored, nativeBogieReadinessChecked,
            rearBogieIndependentWorldHierarchy, bodyYawAndRerailPose, repeatedStockRestoration, gameRuntimeValidated;
        public int checks;
        public float maximumCornerGapMetres;
        public string runtimeSha256;
    }
}
