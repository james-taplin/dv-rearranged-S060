#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;

// Failure injection runs the installed B99 Init/Deinit and production Harmony
// finalizers. It does not substitute a mock reader, event source or Port.
public static class ExhaustFailureNativeAudit
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static int checks;
    private static string injection;
    private static object capturedExtra;
    private static object throwOnStop;
    public static void RunBatch()
    {
        string output = Environment.GetEnvironmentVariable("S060_EXHAUST_FAILURE_AUDIT_OUTPUT");
        string runtime = Environment.GetEnvironmentVariable("S060_EXHAUST_FAILURE_AUDIT_RUNTIME");
        string managed = Environment.GetEnvironmentVariable("S060_AUDIT_MANAGED");
        if (string.IsNullOrEmpty(managed)) managed = @"B:\SteamLibrary\steamapps\common\Derail Valley\DerailValley_Data\Managed";
        Directory.CreateDirectory(output);
        ResolveEventHandler resolver = (sender, args) => (Assembly)Audit("Resolve", sender, args);
        // Reuse the existing fixture's assembly resolution and authored fixture
        // builders; all lifecycle assertions below are independent.
        typeof(ExhaustNativeAudit).GetField("managed", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, managed);
        AppDomain.CurrentDomain.AssemblyResolve += resolver;
        var root = new GameObject("Exhaust failure audit");
        object harmony = null;
        try
        {
            var sim = Assembly.LoadFrom(Path.Combine(managed, "DV.Simulation.dll"));
            var game = Assembly.LoadFrom(Path.Combine(managed, "Assembly-CSharp.dll"));
            var mod = Assembly.LoadFrom(runtime);
            var harmonyType = Assembly.LoadFrom(Path.Combine(managed, "UnityModManager/0Harmony.dll")).GetType("HarmonyLib.Harmony", true);
            harmony = Activator.CreateInstance(harmonyType, "s060.exhaust.failure.audit");
            var presentation = mod.GetType("RearrangedS060.S060ExhaustPresentation", true);
            presentation.GetMethod("Register", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new[] { harmony });
            var chuffType = game.GetType("DV.ModularAudioCar.ChuffClipsSimReader", true);
            var smokeType = game.GetType("DV.Simulation.Controllers.SteamSmokeParticlePortReader", true);
            Patch(harmony, chuffType.GetMethod("Init"), "AudioInitInjection", true);
            Patch(harmony, chuffType.GetMethod("Init"), "AudioCompletedInjection", false);
            Patch(harmony, chuffType.GetMethod("Deinit"), "DeinitInjection", true);
            Patch(harmony, smokeType.GetMethod("Init"), "SmokeCompletedInjection", false);
            Patch(harmony, smokeType.GetMethod("Deinit"), "DeinitInjection", true);
            var portType = sim.GetType("LocoSim.Implementations.Port", true);
            var definition = sim.GetType("LocoSim.Definitions.PortDefinition", true);
            var typeEnum = sim.GetType("LocoSim.Definitions.PortType", true);
            var valueEnum = sim.GetType("LocoSim.Definitions.PortValueType", true);
            var ports = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(string), portType));
            foreach (var engine in new[] { "steamEngine", "rs060FrontEngine" })
                foreach (var suffix in new[] { "CHUFF_EVENT", "EXHAUST_PRESSURE", "CHUFF_FREQUENCY", "WATER_IN_CYLINDERS_NORMALIZED", "ASHES_IN_PIPES" })
                    Audit("AddPort", ports, portType, definition, typeEnum, valueEnum, engine, suffix, suffix == "EXHAUST_PRESSURE" ? 1f : 0f);
            foreach (var pair in new[] { new[] { "cylinderCock", "EXT_IN" }, new[] { "firebox", "FIRE_ON" }, new[] { "boiler", "IS_BROKEN" } })
                Audit("AddPort", ports, portType, definition, typeEnum, valueEnum, pair[0], pair[1], 0f);
            var flowType = sim.GetType("LocoSim.Implementations.SimulationFlow", true);
            Patch(harmony, flowType.GetMethods().Single(m => m.Name == "TryGetPort" && m.GetParameters().Length == 3), "PortLookupInjection", true);
            var flow = FormatterServices.GetUninitializedObject(flowType);
            // TryGetPort formats a native missing-port diagnostic through this
            // host before returning false; real SimulationFlow always has one.
            Set(flow, "simGO", root);
            Set(flow, "fullPortIdToPort", Activator.CreateInstance(typeof(System.Collections.ObjectModel.ReadOnlyDictionary<,>).MakeGenericType(typeof(string), portType), ports));
            var carType = game.GetType("TrainCar", true);
            var car = FormatterServices.GetUninitializedObject(carType);
            var adhesion = FormatterServices.GetUninitializedObject(game.GetType("DV.Wheels.AdhesionController", true));
            Set(car, "adhesionController", adhesion);
            var slipObject = new GameObject("inactive native slip source"); slipObject.SetActive(false); slipObject.transform.SetParent(root.transform);
            var slip = slipObject.AddComponent(game.GetType("DV.Wheels.WheelslipController", true));
            var optionType = adhesion.GetType().GetField("wheelslipController", Fields).FieldType;
            Set(adhesion, "wheelslipController", optionType.GetMethod("Some").Invoke(null, new object[] { slip }));
            var templateObject = new GameObject("authored native chuffs"); templateObject.SetActive(false); templateObject.transform.SetParent(root.transform);
            var template = templateObject.AddComponent(chuffType); Audit("SetupChuff", template, chuffType);
            var livery = ScriptableObject.CreateInstance(carType.GetField("carLivery").FieldType);
            var carDefinition = ScriptableObject.CreateInstance(livery.GetType().GetField("parentType").FieldType);
            Set(livery, "parentType", carDefinition); Set(carDefinition, "audioPrefab", templateObject); Set(car, "carLivery", livery);
            var bankType = mod.GetType("RearrangedS060.S060EngineBank", true);
            var bank = Activator.CreateInstance(bankType, true);
            var modeField = bankType.GetField("<Mode>k__BackingField", Fields);
            var owner = Activator.CreateInstance(presentation, Fields, null, new[] { car, flow, bank }, null);
            var simulationType = mod.GetType("RearrangedS060.S060ArticulatedSimulation", true);
            var simulation = FormatterServices.GetUninitializedObject(simulationType); Set(simulation, "<Exhaust>k__BackingField", owner);
            var owners = simulationType.GetField("Owners", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            owners.GetType().GetMethod("Add").Invoke(owners, new[] { flow, simulation });
            var liveObject = new GameObject("live native chuffs"); liveObject.transform.SetParent(root.transform);
            var audio = liveObject.AddComponent(chuffType); Audit("SetupChuff", audio, chuffType);
            var smoke = root.AddComponent(smokeType); SetupSmoke(smoke, root);
            var audioInit = chuffType.GetMethod("Init"); var audioEnd = chuffType.GetMethod("Deinit");
            var smokeInit = smokeType.GetMethod("Init"); var smokeEnd = smokeType.GetMethod("Deinit");
            Action clean = () => {
                Check(ports.Values.Cast<object>().All(p => Count(p, "ValueUpdatedInternally") == 0), "all native and routed port callbacks removed");
                Check(Count(car, "MovementStateChanged") == 0 && Count(adhesion, "WheelSlideStateChanged") == 0 && Count(slip, "WheelslipStateChanged") == 0, "car, slide and slip callbacks removed");
                Check(((IList)Get(owner, "audio")).Count == 0 && ((IList)Get(owner, "smoke")).Count == 0, "owner enrollment removed");
                Check((string)Get(audio, "chuffEventPortId") == "steamEngine.CHUFF_EVENT" && (float)Get(audio, "mediumPressureThreshold") == 7 && (float)Get(audio, "highPressureThreshold") == 11, "audio authored IDs and pressure bands restored");
                Check((string)Get(smoke, "chuffEventPortId") == "steamEngine.CHUFF_EVENT" && (string)Get(smoke, "exhaustPressurePortId") == "steamEngine.EXHAUST_PRESSURE", "smoke authored IDs restored");
                Check(root.GetComponentsInChildren<Component>(true).Count(c => c != null && c.GetType() == chuffType) == 2, "no owned extra reader remains in live hierarchy");
            };
            for (int mode = 0; mode <= 2; mode++)
            {
                modeField.SetValue(bank, Enum.ToObject(modeField.FieldType, mode));
                for (int repeat = 0; repeat < 4; repeat++) { audioInit.Invoke(audio, new[] { car, flow }); smokeInit.Invoke(smoke, new[] { flow }); }
                Check(((IList)Get(owner, "audio")).Count == 1 && ((IList)Get(owner, "smoke")).Count == 1, "repeated Init enrolls each reader once in mode " + mode);
                Check(Count(car, "MovementStateChanged") == (mode == 2 ? 2 : 1), "repeated Init adds no movement callbacks");
                Check(Count(adhesion, "WheelSlideStateChanged") == (mode == 2 ? 2 : 1) && Count(slip, "WheelslipStateChanged") == (mode == 2 ? 2 : 1), "repeated Init adds no adhesion callbacks");
                audioEnd.Invoke(audio, null); smokeEnd.Invoke(smoke, null); audioEnd.Invoke(audio, null); smokeEnd.Invoke(smoke, null); clean();
            }
            modeField.SetValue(bank, Enum.ToObject(modeField.FieldType, 1));
            injection = "primary-partial"; ExpectFailure(() => audioInit.Invoke(audio, new[] { car, flow }), typeof(NullReferenceException)); injection = null; clean();
            Audit("SetupChuff", audio, chuffType);
            modeField.SetValue(bank, Enum.ToObject(modeField.FieldType, 2));
            injection = "extra-partial"; ExpectFailure(() => audioInit.Invoke(audio, new[] { car, flow }), typeof(NullReferenceException)); injection = null;
            Check(capturedExtra != null && ReferenceEquals(Get(capturedExtra, "car"), null), "partly initialized copied reader releases native car reference"); clean();
            capturedExtra = null;
            injection = "audio-completed"; ExpectFailure(() => audioInit.Invoke(audio, new[] { car, flow }), typeof(InvalidOperationException), "injected after native chuff subscriptions"); injection = null; clean();
            injection = "smoke-completed"; ExpectFailure(() => smokeInit.Invoke(smoke, new[] { flow }), typeof(InvalidOperationException), "injected after native smoke subscriptions"); injection = null; clean();
            // Force native Deinit itself to fail before removing any delegates.
            audioInit.Invoke(audio, new[] { car, flow }); injection = "deinit"; ExpectFailure(() => audioEnd.Invoke(audio, null), typeof(InvalidOperationException), "injected native Deinit failure"); injection = null; clean();
            smokeInit.Invoke(smoke, new[] { flow }); injection = "deinit"; ExpectFailure(() => smokeEnd.Invoke(smoke, null), typeof(InvalidOperationException), "injected native Deinit failure"); injection = null; clean();
            smokeInit.Invoke(smoke, new[] { flow });
            var brokenPort = Get(smoke, "isBoilerBrokenPort");
            var brokenCallback = ((Delegate)Get(brokenPort, "ValueUpdatedInternally")).GetInvocationList().Single(d => ReferenceEquals(d.Target, smoke));
            brokenPort.GetType().GetEvent("ValueUpdatedInternally").RemoveEventHandler(brokenPort, brokenCallback);
            Set(smoke, "isBoilerBrokenPort", null);
            smokeEnd.Invoke(smoke, null); clean();
            // Native Init failed before the owner was published. Enrollment must
            // protect Deinit of the partial reader before retrying native Init.
            owners.GetType().GetMethod("Remove").Invoke(owners, new[] { flow });
            var partialObject = new GameObject("partial pre-owner chimney"); partialObject.transform.SetParent(root.transform);
            var partialSmoke = partialObject.AddComponent(smokeType); SetupSmoke(partialSmoke, partialObject);
            injection = "pre-owner-port"; ExpectFailure(() => smokeInit.Invoke(partialSmoke, new[] { flow }), typeof(InvalidOperationException), "injected native port lookup failure"); injection = null;
            Check(Get(partialSmoke, "chuffEventPort") != null && Get(partialSmoke, "isBoilerBrokenPort") == null, "native pre-owner Init actually stopped with partial port cache");
            owners.GetType().GetMethod("Add").Invoke(owners, new[] { flow, simulation });
            var partialReaders = Array.CreateInstance(smokeType, 1); partialReaders.SetValue(partialSmoke, 0);
            presentation.GetMethod("EnrollInitializedReaders", Fields).Invoke(owner, new object[] { partialReaders, Array.CreateInstance(chuffType, 0) });
            Check(((IList)Get(owner, "smoke")).Count == 1, "partial pre-owner smoke enrollment recovers into one binding");
            smokeEnd.Invoke(partialSmoke, null); clean(); UnityEngine.Object.DestroyImmediate(partialObject);
            // A failure in the presentation End phase must still reset IDs,
            // thresholds, every other loop, and membership for a clean retry.
            var layeredType = Assembly.LoadFrom(Path.Combine(managed, "DV.LayeredAudio.dll")).GetType("LayeredAudio", true);
            var loopObject = new GameObject("failing native loop"); loopObject.SetActive(false); loopObject.transform.SetParent(liveObject.transform);
            var layered = loopObject.AddComponent(layeredType);
            var source = loopObject.AddComponent<AudioSource>();
            var layer = Activator.CreateInstance(layeredType.GetNestedType("Layer"));
            Set(layer, "source", source); Set(layer, "volumeCurve", AnimationCurve.Linear(0, 0, 1, 0));
            var layers = Array.CreateInstance(layer.GetType(), 1); layers.SetValue(layer, 0); Set(layered, "layers", layers);
            layeredType.GetMethod("SetVolume").Invoke(layered, new object[] { 0f });
            Check((float)Get(layered, "lastVolumeInput") == 0, "failure fixture primes native loop cache");
            var loop = Activator.CreateInstance(chuffType.GetNestedType("ChuffLoop")); Set(loop, "chuffLoop", layered);
            var loops = Array.CreateInstance(loop.GetType(), 1); loops.SetValue(loop, 0); Set(audio, "chuffLoops", loops);
            Patch(harmony, layeredType.GetMethod("Stop"), "LoopStopInjection", true);
            audioInit.Invoke(audio, new[] { car, flow }); throwOnStop = layered;
            ExpectFailure(() => audioEnd.Invoke(audio, null), typeof(InvalidOperationException), "injected loop Stop failure"); throwOnStop = null; clean();
            Check(float.IsPositiveInfinity((float)Get(layered, "lastVolumeInput")), "loop Reset executes even when Stop throws");
            audioEnd.Invoke(audio, null); smokeEnd.Invoke(smoke, null); clean();
            // A removed required port fails Prepare and must leave no enrollment.
            var removed = ports["rs060FrontEngine.EXHAUST_PRESSURE"]; ports.Remove("rs060FrontEngine.EXHAUST_PRESSURE");
            modeField.SetValue(bank, Enum.ToObject(modeField.FieldType, 1));
            int expectedNativeErrors = 0;
            Application.LogCallback missingPortLog = (message, stack, kind) => {
                if (kind == LogType.Error && message.Contains("Port[rs060FrontEngine.EXHAUST_PRESSURE] not defined!")) expectedNativeErrors++;
            };
            Application.logMessageReceived += missingPortLog;
            try
            {
                ExpectFailure(() => audioInit.Invoke(audio, new[] { car, flow }), typeof(InvalidOperationException), "S060 exhaust port missing:");
                ExpectFailure(() => smokeInit.Invoke(smoke, new[] { flow }), typeof(InvalidOperationException), "S060 exhaust port missing:");
            }
            finally { Application.logMessageReceived -= missingPortLog; }
            Check(expectedNativeErrors == 2, "each deliberately missing port emits its expected native diagnostic"); clean();
            ports.Add("rs060FrontEngine.EXHAUST_PRESSURE", removed);
            audioInit.Invoke(audio, new[] { car, flow }); smokeInit.Invoke(smoke, new[] { flow });
            audioEnd.Invoke(audio, null); smokeEnd.Invoke(smoke, null); clean();
            owners.GetType().GetMethod("Remove").Invoke(owners, new[] { flow });
            UnityEngine.Object.DestroyImmediate(livery); UnityEngine.Object.DestroyImmediate(carDefinition);
            string runtimeHash;
            using (var sha = System.Security.Cryptography.SHA256.Create()) using (var stream = File.OpenRead(runtime))
                runtimeHash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
            File.WriteAllText(Path.Combine(output, "native-exhaust-failure-validation.json"), "{\"passed\":true,\"runtimeSha256\":\"" + runtimeHash + "\",\"checks\":" + checks + ",\"nativeB99Readers\":true,\"partialNativeInit\":true,\"nativeDeinitFailure\":true,\"endFailure\":true,\"repeatedInitDeinit\":true,\"expectedInjectedNativeErrors\":2,\"gameScene\":false}");
            Debug.Log("Native exhaust failure lifecycle: " + checks + " passed."); EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            File.WriteAllText(Path.Combine(output, "native-exhaust-failure-validation.json"), "{\"passed\":false,\"checks\":" + checks + "}");
            Debug.LogException(error); EditorApplication.Exit(1);
        }
        finally
        {
            injection = null; throwOnStop = null; capturedExtra = null;
            if (harmony != null) harmony.GetType().GetMethod("UnpatchAll").Invoke(harmony, new object[] { "s060.exhaust.failure.audit" });
            AppDomain.CurrentDomain.AssemblyResolve -= resolver;
        }
    }
    private static void AudioInitInjection(object __instance)
    {
        bool extra = ((Component)__instance).name == "[S060 Dockside second exhaust]";
        if ((injection == "extra-partial" && extra) || (injection == "primary-partial" && !extra))
        {
            if (extra) capturedExtra = __instance;
            Set(Get(__instance, "regularChuffConfig"), "spatialCurve", null);
        }
    }
    private static void AudioCompletedInjection(object __instance)
    { if (injection == "audio-completed" && ((Component)__instance).name != "[S060 Dockside second exhaust]") throw new InvalidOperationException("injected after native chuff subscriptions"); }
    private static void SmokeCompletedInjection()
    { if (injection == "smoke-completed") throw new InvalidOperationException("injected after native smoke subscriptions"); }
    private static void DeinitInjection()
    { if (injection == "deinit") throw new InvalidOperationException("injected native Deinit failure"); }
    private static void LoopStopInjection(object __instance)
    { if (ReferenceEquals(__instance, throwOnStop)) throw new InvalidOperationException("injected loop Stop failure"); }
    private static void PortLookupInjection(string __0)
    { if (injection == "pre-owner-port" && __0 == "boiler.IS_BROKEN") throw new InvalidOperationException("injected native port lookup failure before smoke owner enrollment"); }
    private static void SetupSmoke(object smoke, GameObject root)
    {
        Set(smoke, "fireOnPortId", "firebox.FIRE_ON"); Set(smoke, "isBoilerBrokenPortId", "boiler.IS_BROKEN");
        Set(smoke, "chuffEventPortId", "steamEngine.CHUFF_EVENT"); Set(smoke, "exhaustPressurePortId", "steamEngine.EXHAUST_PRESSURE");
        foreach (string prefix in new[] { "smoke", "ember" })
        {
            var particles = new GameObject(prefix); particles.transform.SetParent(root.transform);
            var ps = particles.AddComponent<ParticleSystem>(); var main = ps.main; main.playOnAwake = false;
            Set(smoke, prefix + "ParticlesParent", particles);
            foreach (string suffix in new[] { "StartSpeedMultiplier", "EmissionRateMultiplier", "MaxParticlesMultiplier" }) Set(smoke, prefix + suffix, AnimationCurve.Linear(0, 1, 1, 2));
        }
    }
    private static void Patch(object harmony, MethodBase method, string name, bool prefix)
    {
        var harmonyMethod = harmony.GetType().Assembly.GetType("HarmonyLib.HarmonyMethod", true);
        var hook = Activator.CreateInstance(harmonyMethod, new object[] { typeof(ExhaustFailureNativeAudit).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic) });
        var patch = harmony.GetType().GetMethod("Patch"); var args = new object[patch.GetParameters().Length]; args[0] = method; args[prefix ? 1 : 2] = hook; patch.Invoke(harmony, args);
    }
    private static object Audit(string method, params object[] args) => typeof(ExhaustNativeAudit).GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args);
    private static object Get(object value, string field) => value.GetType().GetField(field, Fields).GetValue(value);
    private static void Set(object value, string field, object data) => value.GetType().GetField(field, Fields).SetValue(value, data);
    private static int Count(object value, string field) { var callback = (Delegate)Get(value, field); return callback == null ? 0 : callback.GetInvocationList().Length; }
    private static void Check(bool result, string message) { if (!result) throw new InvalidOperationException(message); checks++; }
    private static void ExpectFailure(Action action, Type cause, string message = null)
    {
        try { action(); } catch (TargetInvocationException error)
        {
            if (!ContainsCause(error, cause, message))
                throw new InvalidOperationException("original lifecycle failure remains observable: " + (message ?? cause.Name), error);
            checks++; return;
        }
        throw new InvalidOperationException("Expected injected native lifecycle failure.");
    }
    private static bool ContainsCause(Exception error, Type cause, string message)
    {
        if (cause.IsInstanceOfType(error) && (message == null || error.Message.Contains(message))) return true;
        var aggregate = error as AggregateException;
        if (aggregate != null && aggregate.InnerExceptions.Any(inner => ContainsCause(inner, cause, message))) return true;
        return error.InnerException != null && ContainsCause(error.InnerException, cause, message);
    }
}
#endif
