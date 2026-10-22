#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;

// Exercise the production resource-only destruction path after filters have
// already vanished, and while filters remain live. No native car world needed.
public static class CleanupNativeAudit
{
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    public static void RunBatch()
    {
        string output = Environment.GetEnvironmentVariable("S060_CLEANUP_AUDIT_OUTPUT");
        string runtime = Environment.GetEnvironmentVariable("S060_EXHAUST_AUDIT_RUNTIME");
        Directory.CreateDirectory(output);
        string managed = Environment.GetEnvironmentVariable("S060_AUDIT_MANAGED");
        if (string.IsNullOrEmpty(managed)) managed = @"B:\SteamLibrary\steamapps\common\Derail Valley\DerailValley_Data\Managed";
        string mods = Environment.GetEnvironmentVariable("S060_AUDIT_MODS");
        if (string.IsNullOrEmpty(mods)) mods = Path.GetFullPath(Path.Combine(managed, "..", "..", "Mods"));
        ResolveEventHandler resolver = (sender, args) =>
        {
            string name = new AssemblyName(args.Name).Name;
            var loaded = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name);
            if (loaded != null) return loaded;
            foreach (string directory in new[] { managed, Path.Combine(managed, "UnityModManager"), Path.Combine(mods, "LocoMeshSplitter") })
            { string path = Path.Combine(directory, name + ".dll"); if (File.Exists(path)) return Assembly.LoadFrom(path); }
            return null;
        };
        AppDomain.CurrentDomain.AssemblyResolve += resolver;
        int checks = 0;
        try
        {
            var game = Assembly.LoadFrom(Path.Combine(managed, "Assembly-CSharp.dll"));
            var assembly = Assembly.LoadFrom(runtime);
            Type conversionType = assembly.GetType("RearrangedS060.S060Conversion", true);
            var active = (IDictionary)conversionType.GetField("active", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            var forget = conversionType.GetMethod("ForgetDestroyed", BindingFlags.Static | BindingFlags.NonPublic);
            for (int cycle = 0; cycle < 24; cycle++)
            {
                var car = FormatterServices.GetUninitializedObject(game.GetType("TrainCar", true));
                var conversion = Activator.CreateInstance(assembly.GetType(cycle % 2 == 0 ? "RearrangedS060.S060FourCoupled" : "RearrangedS060.S060EightCoupled"), true);
                conversionType.GetField("requestedScale", Instance).SetValue(conversion, cycle % 2 == 0 ? 1f : .8f);
                conversionType.GetField("<CleanupPending>k__BackingField", Instance).SetValue(conversion, cycle % 3 == 0);
                var root = new GameObject("cleanup audit");
                var filter = new GameObject("rod").AddComponent<MeshFilter>(); filter.transform.SetParent(root.transform);
                var original = Triangle(); filter.sharedMesh = original;
                var swapType = assembly.GetType("RearrangedS060.S060RodMeshSwap", true);
                var swap = Activator.CreateInstance(swapType, filter, new[] { new Vector2(0, -1), new Vector2(0, 1) });
                var rodCopy = filter.sharedMesh;
                var rodsType = assembly.GetType("RearrangedS060.S060CouplingRods", true);
                var rods = Activator.CreateInstance(rodsType, true);
                ((IList)rodsType.GetField("swaps", Instance).GetValue(rods)).Add(swap);
                conversionType.GetField("fittedCouplingRods", Instance).SetValue(conversion, rods);
                var body = new GameObject("body"); body.transform.SetParent(root.transform);
                var bodyFilter = new GameObject("s060_body").AddComponent<MeshFilter>(); bodyFilter.transform.SetParent(body.transform);
                var bodySource = Triangle(); bodyFilter.sharedMesh = bodySource;
                var pipeType = assembly.GetType("RearrangedS060.S060BodyPipes", true);
                var pipes = pipeType.GetMethod("Apply", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { body.transform, new Vector3(0, 0, .2f), false });
                var pipeCopy = bodyFilter.sharedMesh;
                var fittingsType = assembly.GetType("RearrangedS060.S060CylinderFittings", true);
                var fittings = Activator.CreateInstance(fittingsType, true); fittingsType.GetField("pipes", Instance).SetValue(fittings, pipes);
                var scaleType = assembly.GetType("RearrangedS060.S060DriverScale", true);
                var scale = FormatterServices.GetUninitializedObject(scaleType); scaleType.GetField("fittings", Instance).SetValue(scale, fittings);
                conversionType.GetField("<Scale>k__BackingField", Instance).SetValue(conversion, scale);
                active.Add(car, conversion);
                if (cycle % 2 == 1) UnityEngine.Object.DestroyImmediate(root);
                forget.Invoke(null, new[] { car });
                Check(!active.Contains(car), "conversion registry released", ref checks);
                Check(rodCopy == null && pipeCopy == null, "both generated meshes destroyed", ref checks);
                Check(original != null && bodySource != null, "native/shared sources retained", ref checks);
                if (filter != null) Check(filter.sharedMesh == original && bodyFilter.sharedMesh == bodySource, "live filters restored", ref checks);
                forget.Invoke(null, new[] { car });
                Check(original != null && bodySource != null, "repeated cleanup idempotent", ref checks);
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(original); UnityEngine.Object.DestroyImmediate(bodySource);
            }
            // Native destruction must hit the registered pre-destruction hook,
            // while OnDestroy still has the resource-only fallback tested above.
            var hooks = assembly.GetType("RearrangedS060.S060Lifecycle", true).GetNestedTypes(BindingFlags.NonPublic);
            Check(hooks.Any(t => t.Name == "PreparingForDestroy"), "native pre-destruction hook present", ref checks);
            TransactionsNativeAudit.Validate(game, assembly, managed, ref checks);
            File.WriteAllText(Path.Combine(output, "native-cleanup-validation.json"), "{\"passed\":true,\"checks\":" + checks + ",\"directDeletionCycles\":24,\"nativeMeshLifetime\":true,\"articulatedDisposalFailureRetry\":true,\"nativeAttachmentRestoredBeforeFrameDeletion\":true,\"transactionFixturePhysicsSkipped\":true,\"gameScene\":false}");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(output, "native-cleanup-validation.json"), "{\"passed\":false,\"checks\":" + checks + "}");
            Debug.LogException(ex); EditorApplication.Exit(1);
        }
        finally { AppDomain.CurrentDomain.AssemblyResolve -= resolver; }
    }
    private static Mesh Triangle()
    {
        var mesh = new Mesh(); mesh.vertices = new[] { new Vector3(.8f, 1.7f, 3.1f), new Vector3(.9f, 1.8f, 3.1f), new Vector3(1f, 1.7f, 3.2f) };
        mesh.triangles = new[] { 0, 1, 2 }; mesh.RecalculateNormals(); return mesh;
    }
    private static void Check(bool result, string message, ref int checks)
    { if (!result) throw new InvalidOperationException(message); checks++; }
}
#endif
