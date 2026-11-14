using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;

// Actual candidate hooks, native reader/ports/loops and reopened audio bundle.
// Inactive host avoids a game world/ECS conversion; native one-shot pool is the
// intercepted output boundary. This does not establish audible playback.
public static class ShayExhaustNativeAudit
{
    const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static int checks, shots;
    static object Audit(string name, params object[] args) => typeof(ExhaustNativeAudit).GetMethod(name, All).Invoke(null, args);
    static object Get(object obj, string field) => obj.GetType().GetField(field, All).GetValue(obj);
    static void Set(object obj, string field, object value) => obj.GetType().GetField(field, All).SetValue(obj, value);
    static object Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, All).Invoke(obj, args);
    static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); checks++; }
    static bool SkipOneShot() { shots++; return false; }

    public static void RunBatch()
    {
        string output = Environment.GetEnvironmentVariable("S060_SHAY_EXHAUST_OUTPUT");
        string runtime = Environment.GetEnvironmentVariable("S060_EXHAUST_AUDIT_RUNTIME");
        string bundlePath = Environment.GetEnvironmentVariable("S060_SHAY_EXHAUST_BUNDLE");
        string managed = Environment.GetEnvironmentVariable("S060_AUDIT_MANAGED");
        Directory.CreateDirectory(output);
        typeof(ExhaustNativeAudit).GetField("managed", All).SetValue(null, managed);
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => (Assembly)Audit("Resolve", sender, args);
        var root = new GameObject("Inactive Shay exhaust audit"); root.SetActive(false);
        object harmony = null;
        try
        {
            var sim = Assembly.LoadFrom(Path.Combine(managed, "DV.Simulation.dll"));
            var game = Assembly.LoadFrom(Path.Combine(managed, "Assembly-CSharp.dll"));
            var mod = Assembly.LoadFrom(runtime);
            var harmonyAssembly = Assembly.LoadFrom(Path.Combine(managed, "UnityModManager/0Harmony.dll"));
            var harmonyType = harmonyAssembly.GetType("HarmonyLib.Harmony", true);
            var methodType = harmonyAssembly.GetType("HarmonyLib.HarmonyMethod", true);
            harmony = Activator.CreateInstance(harmonyType, "s060.shay.exhaust.audit");
            var presentation = mod.GetType("RearrangedS060.S060ExhaustPresentation", true);
            presentation.GetMethod("Register", All).Invoke(null, new[] { harmony });
            var prefix = Activator.CreateInstance(methodType, new object[] { typeof(ShayExhaustNativeAudit).GetMethod("SkipOneShot", All) });
            var patch = harmonyType.GetMethods().Single(m => m.Name == "Patch" && m.GetParameters().Length == 5);
            var pool = Assembly.LoadFrom(Path.Combine(managed, "DV.NAudio.dll")).GetType("NAudio", true);
            foreach (var play in pool.GetMethods(All).Where(m => m.Name == "Play" && m.GetParameters().Any(p => p.ParameterType == typeof(AudioClip[]))))
                patch.Invoke(harmony, new[] { (object)play, prefix, null, null, null });
            string stage = Path.Combine(output, "stage");
            string assets = Path.Combine(stage, "assets", "Shay"); Directory.CreateDirectory(assets);
            File.Copy(bundlePath, Path.Combine(assets, "shay_running_gear"), true);
            mod.GetType("RearrangedS060.Main", true).GetProperty("ModPath", All).GetSetMethod(true).Invoke(null, new object[] { stage });
            var bankType = mod.GetType("RearrangedS060.S060EngineBank", true);
            var bank = Activator.CreateInstance(bankType, true);
            var mode = bankType.GetField("<Mode>k__BackingField", All);
            var portType = sim.GetType("LocoSim.Implementations.Port", true);
            var definition = sim.GetType("LocoSim.Definitions.PortDefinition", true);
            var typeEnum = sim.GetType("LocoSim.Definitions.PortType", true);
            var valueEnum = sim.GetType("LocoSim.Definitions.PortValueType", true);
            var ports = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(string), portType));
            foreach (string suffix in new[] { "CHUFF_EVENT", "EXHAUST_PRESSURE", "CHUFF_FREQUENCY", "WATER_IN_CYLINDERS_NORMALIZED", "ASHES_IN_PIPES" })
                Audit("AddPort", ports, portType, definition, typeEnum, valueEnum, "steamEngine", suffix, suffix == "EXHAUST_PRESSURE" ? 1f : 0f);
            Audit("AddPort", ports, portType, definition, typeEnum, valueEnum, "cylinderCock", "EXT_IN", 0f);
            var flowType = sim.GetType("LocoSim.Implementations.SimulationFlow", true);
            var flow = FormatterServices.GetUninitializedObject(flowType);
            Set(flow, "fullPortIdToPort", Activator.CreateInstance(typeof(System.Collections.ObjectModel.ReadOnlyDictionary<,>).MakeGenericType(typeof(string), portType), ports));
            var car = FormatterServices.GetUninitializedObject(game.GetType("TrainCar", true));
            var owner = Activator.CreateInstance(presentation, All, null, new[] { car, flow, bank }, null);
            var simulationType = mod.GetType("RearrangedS060.S060ArticulatedSimulation", true);
            var simulation = FormatterServices.GetUninitializedObject(simulationType);
            Set(simulation, "<Exhaust>k__BackingField", owner);
            var owners = simulationType.GetField("Owners", All).GetValue(null);
            Call(owners, "Add", flow, simulation);
            var audioObject = new GameObject("Shay native reader"); audioObject.transform.SetParent(root.transform, false);
            var readerType = game.GetType("DV.ModularAudioCar.ChuffClipsSimReader", true);
            var reader = audioObject.AddComponent(readerType); Audit("SetupChuff", reader, readerType);
            Call(reader, "Init", car, flow);
            string[] fields = { "lowPressureClips", "mediumPressureClips", "highPressureClips", "regularChuffConfig", "pressureToVolumeCurve", "chuffLoops", "waterChuffConfig", "ashChuffConfig", "waterChuffLoops", "ashChuffLoops", "waterChuffClips", "ashChuffClips" };
            var original = fields.ToDictionary(f => f, f => Get(reader, f));
            for (int cycle = 0; cycle < 3; cycle++)
            {
                mode.SetValue(bank, Enum.Parse(mode.FieldType, "Shay")); Call(owner, "Refresh");
                Check((int)Audit("SubscriberCount", ports["steamEngine.CHUFF_EVENT"]) == 1, "one native callback after Shay selection");
                foreach (string field in fields.Take(3))
                {
                    var phases = (Array)Get(reader, field);
                    Check(phases.Length == 6, "six ordered phases in " + field);
                    foreach (var phase in phases) Check(((AudioClip[])Get(phase, "chuffVariations")).Length == 2, "two original takes per phase");
                }
                foreach (string field in fields.Skip(6)) Check(ReferenceEquals(Get(reader, field), original[field]), "native wet/ash preserved: " + field);
                var loops = (Array)Get(reader, "chuffLoops"); Check(loops.Length == 8, "eight running rates");
                var rates = new[] { 4, 8, 16, 24, 32, 48, 64, 96 };
                for (int step = 1; step <= 1920; step++)
                {
                    float frequency = step / 10f;
                    float gain = ((AnimationCurve)Get(Get(reader, "regularChuffConfig"), "chuffFrequencyToMasterVolume")).Evaluate(frequency);
                    foreach (var loop in loops) gain += ((AnimationCurve)Get(loop, "chuffFrequencyToMasterVolume")).Evaluate(frequency);
                    Check(Math.Abs(gain - 1) < .001, "complete non-amplifying frequency crossfade");
                }
                foreach (float hz in new[] { 4f, 8, 14.26f, 28.52f, 57.03f, 85.55f, 128, 192, 0 })
                {
                    Audit("SetValue", ports["steamEngine.CHUFF_FREQUENCY"], hz);
                    Call(reader, "UpdateLoops"); // Atmospheric pressure: no audio output.
                    for (int i = 0; i < loops.Length; i++)
                    {
                        var loop = loops.GetValue(i);
                        if (((AnimationCurve)Get(loop, "chuffFrequencyToMasterVolume")).Evaluate(hz) <= 0) continue;
                        foreach (var layer in (Array)Get(Get(loop, "chuffLoop"), "layers"))
                            Check(Math.Abs(((AudioSource)Get(layer, "source")).pitch * rates[i] - hz) < .001, "native loop preserves actual exhaust cadence");
                    }
                }
                // Exercise native direct bank indexing at all six phases/three
                // pressure families; only the final one-shot pool is suppressed.
                Audit("SetValue", ports["steamEngine.CHUFF_FREQUENCY"], .5f);
                int before = shots;
                foreach (float pressure in new[] { 2f, 8, 12 })
                {
                    Audit("SetValue", ports["steamEngine.EXHAUST_PRESSURE"], pressure);
                    for (int phase = 0; phase < 6; phase++) Call(reader, "OnChuff", (float)phase);
                }
                Check(shots - before == 18, "all native pressure/phase indices reach the one-shot output boundary");
                Audit("SetValue", ports["steamEngine.EXHAUST_PRESSURE"], 1f);
                Call(reader, "Deinit");
                foreach (string field in fields) Check(ReferenceEquals(Get(reader, field), original[field]), "stream-out restores exact original " + field);
                Check(audioObject.transform.childCount == 0, "stream-out destroys owned exhaust sources");
                Call(reader, "Init", car, flow);
                Check(audioObject.transform.childCount == 1, "stream-in creates one exhaust subtree");
                mode.SetValue(bank, Enum.Parse(mode.FieldType, "Simple")); Call(owner, "Refresh");
                foreach (string field in fields) Check(ReferenceEquals(Get(reader, field), original[field]), "stock selection restores " + field);
                if (Enum.IsDefined(mode.FieldType, "Dryope"))
                {
                    mode.SetValue(bank, Enum.Parse(mode.FieldType, "Dryope")); Call(owner, "Refresh");
                    foreach (string field in fields) Check(ReferenceEquals(Get(reader, field), original[field]), "Dryope unaffected: " + field);
                }
            }
            // Invalid/incomplete original data must fail before donor mutation.
            var exhaust = mod.GetType("RearrangedS060.S060ShayExhaust", true);
            var apply = exhaust.GetMethods(All).Single(m => m.Name == "Apply" && m.GetParameters().Length == 2);
            bool refused = false;
            try { apply.Invoke(null, new object[] { reader, (Func<string, AudioClip>)(_ => null) }); }
            catch (TargetInvocationException e) { refused = e.InnerException is InvalidOperationException; }
            Check(refused && fields.All(f => ReferenceEquals(Get(reader, f), original[f])) && audioObject.transform.childCount == 0,
                "incomplete data preserves donor without leaked sources");
            Call(reader, "Deinit"); Call(owners, "Remove", flow);
            string hash;
            using (var sha = System.Security.Cryptography.SHA256.Create()) using (var input = File.OpenRead(runtime)) hash = BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "");
            File.WriteAllText(Path.Combine(output, "shay-exhaust-validation.json"), "{\"passed\":true,\"checks\":" + checks + ",\"runtimeSha256\":\"" + hash + "\",\"nativeReaderAndHooks\":true,\"sixPhaseBanks\":true,\"nativeCadence\":true,\"streamingRestoration\":true,\"dryopeUnchanged\":true,\"inactiveHost\":true,\"audiblePlaybackValidated\":false}");
            Debug.Log("Shay native exhaust: " + checks + " checks passed.");
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        finally
        {
            if (harmony != null) harmony.GetType().GetMethod("UnpatchAll").Invoke(harmony, new object[] { "s060.shay.exhaust.audit" });
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
