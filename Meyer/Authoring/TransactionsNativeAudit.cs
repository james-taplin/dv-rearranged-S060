#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;

// Runs the candidate DLL's actual articulated Undo with a native Transform
// attachment and a transient disposal failure. Physics is explicitly skipped:
// this fixture establishes cleanup ordering, not native world restoration.
public static class TransactionsNativeAudit
{
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static int failures, physicsAttempts;
    public static void Validate(Assembly game, Assembly runtime, string managed, ref int checks)
    {
        var harmonyAssembly = Assembly.LoadFrom(System.IO.Path.Combine(managed, "UnityModManager/0Harmony.dll"));
        var harmonyType = harmonyAssembly.GetType("HarmonyLib.Harmony", true);
        var harmonyMethodType = harmonyAssembly.GetType("HarmonyLib.HarmonyMethod", true);
        const string id = "s060.transaction.offline.audit";
        var harmony = Activator.CreateInstance(harmonyType, id);
        var ownerType = runtime.GetType("RearrangedS060.S060Articulated", true);
        var conversionType = ownerType.BaseType;
        var lightsType = runtime.GetType("RearrangedS060.S060MeyerLights", true);
        var physicsType = runtime.GetType("RearrangedS060.B99PhysicsState", true);
        void Prefix(MethodInfo method, string prefix)
        {
            var hook = Activator.CreateInstance(harmonyMethodType, typeof(TransactionsNativeAudit).GetMethod(prefix, BindingFlags.Static | BindingFlags.NonPublic));
            harmonyType.GetMethod("Patch").Invoke(harmony, new object[] { method, hook, null, null, null });
        }
        Prefix(lightsType.GetMethod("Dispose"), nameof(InjectDisposalFailure));
        Prefix(physicsType.GetMethod("Restore"), nameof(SkipPhysics));
        var root = new GameObject("Native articulated cleanup ordering audit"); root.SetActive(false);
        try
        {
            var front = new GameObject("inactive native front bogie"); front.transform.SetParent(root.transform, false);
            var rear = new GameObject("inactive native rear bogie"); rear.transform.SetParent(root.transform, false);
            var bogieType = game.GetType("Bogie", true);
            var partsType = runtime.GetType("RearrangedS060.S060Parts", true);
            var parts = Activator.CreateInstance(partsType, true);
            partsType.GetField("<FrontBogie>k__BackingField", Instance).SetValue(parts, front.AddComponent(bogieType));
            partsType.GetField("<RearBogie>k__BackingField", Instance).SetValue(parts, rear.AddComponent(bogieType));
            var owner = Activator.CreateInstance(runtime.GetType("RearrangedS060.S060Meyer", true), true);
            conversionType.GetField("<Parts>k__BackingField", Instance).SetValue(owner, parts);
            conversionType.GetField("<Physics>k__BackingField", Instance).SetValue(owner, FormatterServices.GetUninitializedObject(physicsType));
            var nativeParent = new GameObject("Native cab housing parent").transform; nativeParent.SetParent(root.transform, false);
            var frame = new GameObject("Owned rear frame"); frame.transform.SetParent(root.transform, false);
            var lamp = new GameObject("Native lower lamp").transform; lamp.SetParent(frame.transform, false);
            var homePosition = new Vector3(1.01f, 1.5f, -4.2f);
            var lights = Activator.CreateInstance(lightsType, true);
            var homeType = lightsType.GetNestedType("Home", BindingFlags.NonPublic);
            var home = Activator.CreateInstance(homeType, true);
            void Set(string field, object value) => homeType.GetField(field, Instance).SetValue(home, value);
            Set("Part", lamp); Set("Parent", nativeParent); Set("Position", homePosition);
            Set("Scale", Vector3.one); Set("Rotation", Quaternion.identity); Set("Sibling", 0);
            ((IList)lightsType.GetField("homes", Instance).GetValue(lights)).Add(home);
            ownerType.GetField("meyerLights", Instance).SetValue(owner, lights);
            var added = (IList)ownerType.GetField("added", Instance).GetValue(owner); added.Add(frame);
            var undo = ownerType.GetMethod("Undo", Instance);
            failures = 1; physicsAttempts = 0;
            bool rejected = false;
            try { undo.Invoke(owner, null); } catch (TargetInvocationException ex) { rejected = ex.InnerException is AggregateException; }
            Check(rejected, "Injected native-owner disposal failure must reject Undo.", ref checks);
            Check(ReferenceEquals(ownerType.GetField("meyerLights", Instance).GetValue(owner), lights), "Failed native owner must remain reachable.", ref checks);
            Check(frame != null && added.Count == 1 && lamp != null && lamp.parent == frame.transform,
                "Failed native restoration must retain its parent frame and native lamp.", ref checks);
            Check(physicsAttempts == 1, "Independent physics cleanup must still be attempted after native-owner failure.", ref checks);
            undo.Invoke(owner, null);
            Check(ownerType.GetField("meyerLights", Instance).GetValue(owner) == null && added.Count == 0 && frame == null,
                "Successful retry releases the owner and owned frame.", ref checks);
            Check(lamp != null && lamp.parent == nativeParent && lamp.localPosition == homePosition,
                "Actual native Transform returns home before its former frame is destroyed.", ref checks);
            Check(physicsAttempts == 2, "Retry independently attempts physics restoration again.", ref checks);
            undo.Invoke(owner, null);
            Check(lamp != null && lamp.parent == nativeParent, "Repeated clean Undo preserves the restored native attachment.", ref checks);
        }
        finally
        {
            failures = 0;
            harmonyType.GetMethod("UnpatchAll").Invoke(harmony, new object[] { id });
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
    private static void InjectDisposalFailure() { if (failures > 0) { failures--; throw new InvalidOperationException("Injected native attachment disposal failure"); } }
    private static bool SkipPhysics() { physicsAttempts++; return false; }
    private static void Check(bool condition, string message, ref int checks)
    { if (!condition) throw new InvalidOperationException(message); checks++; }
}
#endif
