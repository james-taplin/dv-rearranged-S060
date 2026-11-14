#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using CCL.Types;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Actual B99 registry and spawner list builder, with a seeded registry and
// destination-display reset suppressed. No game world or CCL world import.
public static class DonorVisibilityNativeAudit
{
    const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    const string PatchId = "s060.donor.visibility.offline";
    static readonly List<Object> owned = new List<Object>();
    static Assembly game, things, runtime;
    static Type liveryType, carType, modelType, spawnerType;
    static object model, kind;
    static int checks;

    public static void RunBatch()
    {
        string output = Environment.GetEnvironmentVariable("S060_DONOR_OUTPUT");
        string dll = Environment.GetEnvironmentVariable("S060_DONOR_RUNTIME");
        string assets = Environment.GetEnvironmentVariable("S060_DONOR_ASSETS");
        bool baseline = Environment.GetEnvironmentVariable("S060_DONOR_BASELINE") == "1";
        Directory.CreateDirectory(output);
        string managed = Environment.GetEnvironmentVariable("S060_AUDIT_MANAGED");
        typeof(ExhaustNativeAudit).GetField("managed", All).SetValue(null, managed);
        var resolver = (ResolveEventHandler)Delegate.CreateDelegate(typeof(ResolveEventHandler), typeof(ExhaustNativeAudit).GetMethod("Resolve", All));
        AppDomain.CurrentDomain.AssemblyResolve += resolver;
        object harmony = null, previousGlobals = null;
        FieldInfo globalsField = null;
        try
        {
            things = Assembly.LoadFrom(Path.Combine(managed, "DV.ThingTypes.dll"));
            game = Assembly.LoadFrom(Path.Combine(managed, "Assembly-CSharp.dll"));
            runtime = Assembly.LoadFrom(dll);
            liveryType = things.GetType("DV.ThingTypes.TrainCarLivery", true);
            carType = things.GetType("DV.ThingTypes.TrainCarType_v2", true);
            modelType = things.GetType("DV.ThingTypes.DVObjectModel", true);
            spawnerType = game.GetType("DV.CommsRadioCarSpawner", true);
            kind = Asset(things.GetType("DV.ThingTypes.TrainCarKind", true)); Set(kind, "id", "Loco");
            model = Asset(modelType);
            var globalsType = game.GetType("DV.Globals", true);
            var globals = Asset(globalsType); Set(globals, "types", model);
            globalsField = globalsType.GetField("_current", All);
            previousGlobals = globalsField.GetValue(null); globalsField.SetValue(null, globals);

            var harmonyType = Assembly.LoadFrom(Path.Combine(managed, "UnityModManager/0Harmony.dll")).GetType("HarmonyLib.Harmony", true);
            harmony = Activator.CreateInstance(harmonyType, PatchId);
            var patchType = runtime.GetType("RearrangedS060.S060DonorVisibility");
            Check(baseline ? patchType == null : patchType != null, "Expected baseline/candidate patch identity.");
            if (!baseline)
            {
                var processor = harmonyType.GetMethod("CreateClassProcessor").Invoke(harmony, new object[] { patchType });
                processor.GetType().GetMethod("Patch").Invoke(processor, null);
            }
            // Exclude only the native destination UI, which needs a game world.
            var hm = harmonyType.Assembly.GetType("HarmonyLib.HarmonyMethod", true);
            var hook = Activator.CreateInstance(hm, GetMethod(typeof(DonorVisibilityNativeAudit), "SkipDestinationUi"));
            var patch = harmonyType.GetMethod("Patch"); var args = new object[patch.GetParameters().Length];
            args[0] = GetMethod(spawnerType, "ClearFlags"); args[1] = hook; patch.Invoke(harmony, args);

            var donors = new[] { Donor(assets, "Meyer"), Donor(assets, "Dockside") };
            foreach (var donor in donors)
            {
                Registry(donor);
                Check(Visible().Contains(donor) == baseline, "Native spawner must show baseline donor and hide repaired donor.");
                Check(Lookup((string)Get(donor, "id")) == donor, "Hidden donor must remain addressable by its original ID.");
            }
            if (!baseline)
            {
                var stock = Livery("LocoS060", new GameObject("stock placeholder")); owned.Add((Object)Get(stock, "prefab"));
                var other = Livery("RS060_Meyer_0440_unrelated", null);
                var alreadyHidden = Livery("unrelated_hidden", null); Set(alreadyHidden, "isHidden", true);
                for (int present = 0; present < 4; present++)
                {
                    foreach (var donor in donors) Set(donor, "isHidden", false);
                    var entries = new List<object> { stock, alreadyHidden };
                    if ((present & 1) != 0) entries.Add(donors[0]);
                    if ((present & 2) != 0) entries.Add(donors[1]);
                    Registry(entries.ToArray());
                    Check(Visible().Cast<object>().SequenceEqual(new[] { stock }), "Only stock S060 should be listed, including absent/late donor cases.");
                    Check(!(bool)Get(stock, "isHidden") && (bool)Get(alreadyHidden, "isHidden"), "Unrelated visibility must be preserved.");
                }
                Registry(donors[0], donors[1], other, alreadyHidden);
                Check(Visible().Cast<object>().SequenceEqual(new[] { other }), "Exact donor IDs only; unrelated locomotive remains visible.");
                Check(Visible().Cast<object>().SequenceEqual(new[] { other }), "Repeated list construction must keep donor flags stable.");

                Registry(donors[0], donors[1], stock);
                Check(Visible().Cast<object>().SequenceEqual(new[] { stock }), "Stock S060 remains selectable alongside hidden donors.");
                var preflight = runtime.GetType("RearrangedS060.S060Articulated", true).GetMethod("Preflight", All);
                foreach (bool meyer in new[] { true, false })
                {
                    var donor = donors[meyer ? 0 : 1]; var prefab = Get(donor, "prefab");
                    var frames = (Transform[])preflight.Invoke(null, new object[] { meyer });
                    Check(frames.Length == 2, "Actual radio arrangement preflight must resolve both hidden powered frames.");
                    Check(Lookup((string)Get(donor, "id")) == donor && Get(donor, "prefab") == prefab, "Hiding must retain the exact donor and prefab.");
                }
                var shayFrames = (Transform[])preflight.Invoke(null, new object[] { false });
                Check(shayFrames[0].Find("[S060 fitted effects 0.7]") != null, "Shay's Dockside effects donor remains available.");
                var radioType = runtime.GetType("RearrangedS060.S060Radio", true);
                foreach (var choice in new[] { new { index=5, name="Meyer" }, new { index=6, name="Dockside" }, new { index=8, name="Shay" } })
                {
                    var radio = FormatterServices.GetUninitializedObject(radioType); Set(radio, "choice", choice.index);
                    var selection = GetMethod(radioType, "Selection").Invoke(radio, null);
                    Check(selection.GetType().GetProperty("Name", All).GetValue(selection, null).ToString().Contains(choice.name), "Production radio choice must retain " + choice.name);
                }
                runtime.GetType("RearrangedS060.Main", true).GetField("<ModPath>k__BackingField", All).SetValue(null, assets);
                var shayAssets = runtime.GetType("RearrangedS060.S060ShayAssets", true);
                Check(GetMethod(shayAssets, "Resolve").Invoke(null, null) != null, "Shay prefab and transmission specification remain valid.");
                foreach (string clip in new[] { "RS060_Shay_low_0_0", "RS060_Shay_high_loop_96" })
                {
                    var audio = (AudioClip)GetMethod(shayAssets, "Audio").Invoke(null, new object[] { clip });
                    Check(audio != null && audio.channels == 1 && audio.frequency == 24000, "Shay audio remains addressable.");
                }
                Set(globals, "types", null);
                GetMethod(patchType, "Prefix").Invoke(null, null);
                Set(globals, "types", model);
                Check(true, "Unavailable registry must not cause a visibility-patch exception.");
            }
            using (var sha = SHA256.Create()) using (var stream = File.OpenRead(dll))
                File.WriteAllText(Path.Combine(output, "validation.json"), JsonUtility.ToJson(new Report { passed=true, checks=checks,
                    baseline=baseline, runtimeSha256=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", ""),
                    nativeSpawner=true, hiddenDonorsRemainRegistered=!baseline, radioPreflight=!baseline, shayAssetsAndEffectsSource=!baseline }, true));
            EditorApplication.Exit(0);
        }
        catch (Exception error) { File.WriteAllText(Path.Combine(output, "error.txt"), error.ToString()); Debug.LogException(error); EditorApplication.Exit(1); }
        finally
        {
            if (harmony != null) harmony.GetType().GetMethod("UnpatchAll").Invoke(harmony, new object[] { PatchId });
            if (globalsField != null) globalsField.SetValue(null, previousGlobals);
            foreach (var obj in owned) if (obj != null) Object.DestroyImmediate(obj);
            AppDomain.CurrentDomain.AssemblyResolve -= resolver;
        }
    }
    static object Donor(string root, string name)
    {
        var bundle = AssetBundle.LoadFromFile(Path.Combine(root, "assets", name, "ccl_bundle"));
        if (bundle == null) throw new InvalidOperationException("Donor bundle unavailable: " + name);
        try
        {
            var car = bundle.LoadAllAssets<CustomCarPack>().Single().Cars.Single().liveries.Single();
            return Livery(car.id, car.prefab);
        }
        finally { bundle.Unload(false); } // Match CCL's sequential import and retained prefabs.
    }
    static object Livery(string id, GameObject prefab)
    {
        var car = Asset(carType); Set(car, "id", id); Set(car, "kind", kind);
        var livery = Asset(liveryType); Set(livery, "id", id); Set(livery, "parentType", car); Set(livery, "prefab", prefab);
        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(liveryType)); list.Add(livery); Set(car, "liveries", list);
        return livery;
    }
    static void Registry(params object[] entries)
    {
        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(liveryType));
        var map = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(string), liveryType));
        foreach (var item in entries) { list.Add(item); map.Add(Get(item, "id"), item); }
        Set(model, "_liveries", list); Set(model, "_liveriesByID", map);
    }
    static object Lookup(string id)
    { var args = new object[] { id, null }; Check((bool)GetMethod(modelType, "TryGetLivery").Invoke(model, args), "Native livery lookup must succeed."); return args[1]; }
    static IList Visible()
    {
        var spawner = FormatterServices.GetUninitializedObject(spawnerType);
        foreach (string field in new[] { "locoLiveries", "carLiveriesToSpawn" })
        { var info = spawnerType.GetField(field, All); info.SetValue(spawner, Activator.CreateInstance(info.FieldType)); }
        GetMethod(spawnerType, "UpdateCarLiveriesToSpawn").Invoke(spawner, null);
        return (IList)Get(spawner, "locoLiveries");
    }
    static bool SkipDestinationUi() => false;
    static ScriptableObject Asset(Type type) { var item = ScriptableObject.CreateInstance(type); owned.Add(item); return item; }
    static FieldInfo Field(Type type, string name) { for (; type != null; type=type.BaseType) { var f=type.GetField(name, All); if(f!=null)return f; } throw new MissingFieldException(name); }
    static void Set(object value, string field, object data) => Field(value.GetType(), field).SetValue(value, data);
    static object Get(object value, string field) => Field(value.GetType(), field).GetValue(value);
    static MethodInfo GetMethod(Type type, string name) => type.GetMethod(name, All);
    static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); checks++; }
    [Serializable] sealed class Report
    {
        public bool passed, baseline, nativeSpawner, hiddenDonorsRemainRegistered, radioPreflight, shayAssetsAndEffectsSource;
        public bool gameWorld=false, cclWorldImport=false, gameplayValidated=false;
        public int checks; public string runtimeSha256;
    }
}
#endif
