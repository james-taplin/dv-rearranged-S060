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

// Runs production exhaust routing against the installed B99 readers and ports.
// The car is an inert managed event source: no game world, save or audio device.
public static class ExhaustNativeAudit
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static string managed;
    private static int checks;
    public static void RunBatch()
    {
        string output = Environment.GetEnvironmentVariable("S060_EXHAUST_AUDIT_OUTPUT");
        string runtime = Environment.GetEnvironmentVariable("S060_EXHAUST_AUDIT_RUNTIME");
        Directory.CreateDirectory(output);
        managed = Environment.GetEnvironmentVariable("S060_AUDIT_MANAGED");
        if (string.IsNullOrEmpty(managed)) managed = @"B:\SteamLibrary\steamapps\common\Derail Valley\DerailValley_Data\Managed";
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        var root = new GameObject("Exhaust native reader audit");
        object harmony = null;
        try
        {
            var sim = Assembly.LoadFrom(Path.Combine(managed, "DV.Simulation.dll"));
            var game = Assembly.LoadFrom(Path.Combine(managed, "Assembly-CSharp.dll"));
            var mod = Assembly.LoadFrom(runtime);
            var harmonyAssembly = Assembly.LoadFrom(Path.Combine(managed, "UnityModManager/0Harmony.dll"));
            var harmonyType = harmonyAssembly.GetType("HarmonyLib.Harmony", true);
            harmony = Activator.CreateInstance(harmonyType, "s060.exhaust.offline.audit");
            var presentation = mod.GetType("RearrangedS060.S060ExhaustPresentation", true);
            presentation.GetMethod("Register", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new[] { harmony });
            var bankType = mod.GetType("RearrangedS060.S060EngineBank", true);
            var bank = Activator.CreateInstance(bankType, true);
            var modeField = bankType.GetField("<Mode>k__BackingField", Fields);
            var portType = sim.GetType("LocoSim.Implementations.Port", true);
            var definitionType = sim.GetType("LocoSim.Definitions.PortDefinition", true);
            var typeEnum = sim.GetType("LocoSim.Definitions.PortType", true);
            var valueEnum = sim.GetType("LocoSim.Definitions.PortValueType", true);
            var dictionaryType = typeof(Dictionary<,>).MakeGenericType(typeof(string), portType);
            var ports = (IDictionary)Activator.CreateInstance(dictionaryType);
            string[] suffixes = { "CHUFF_EVENT", "EXHAUST_PRESSURE", "CHUFF_FREQUENCY", "WATER_IN_CYLINDERS_NORMALIZED", "ASHES_IN_PIPES" };
            foreach (var engine in new[] { "steamEngine", "rs060FrontEngine" })
                foreach (var suffix in suffixes) AddPort(ports, portType, definitionType, typeEnum, valueEnum, engine, suffix, suffix == "EXHAUST_PRESSURE" ? 1 : 0);
            AddPort(ports, portType, definitionType, typeEnum, valueEnum, "cylinderCock", "EXT_IN", 0);
            AddPort(ports, portType, definitionType, typeEnum, valueEnum, "firebox", "FIRE_ON", 1);
            AddPort(ports, portType, definitionType, typeEnum, valueEnum, "boiler", "IS_BROKEN", 0);
            var flowType = sim.GetType("LocoSim.Implementations.SimulationFlow", true);
            var flow = FormatterServices.GetUninitializedObject(flowType);
            var readOnlyType = typeof(System.Collections.ObjectModel.ReadOnlyDictionary<,>).MakeGenericType(typeof(string), portType);
            flowType.GetField("fullPortIdToPort", Fields).SetValue(flow, Activator.CreateInstance(readOnlyType, ports));
            var carType = game.GetType("TrainCar", true);
            var car = FormatterServices.GetUninitializedObject(carType);
            // Install an authored, inactive template, as the production S282 path does.
            var chuffType = game.GetType("DV.ModularAudioCar.ChuffClipsSimReader", true);
            var audioTemplate = new GameObject("authored chuff template"); audioTemplate.transform.SetParent(root.transform); audioTemplate.SetActive(false);
            var template = audioTemplate.AddComponent(chuffType); SetupChuff(template, chuffType);
            var liveryType = carType.GetField("carLivery").FieldType;
            var livery = ScriptableObject.CreateInstance(liveryType);
            var parentField = liveryType.GetField("parentType");
            var carDefinition = ScriptableObject.CreateInstance(parentField.FieldType);
            parentField.SetValue(livery, carDefinition);
            parentField.FieldType.GetField("audioPrefab").SetValue(carDefinition, audioTemplate);
            carType.GetField("carLivery").SetValue(car, livery);
            var owner = Activator.CreateInstance(presentation, Fields, null, new[] { car, flow, bank }, null);
            var simulationType = mod.GetType("RearrangedS060.S060ArticulatedSimulation", true);
            var simulation = FormatterServices.GetUninitializedObject(simulationType);
            simulationType.GetField("<Exhaust>k__BackingField", Fields).SetValue(simulation, owner);
            var owners = simulationType.GetField("Owners", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            var audioObject = new GameObject("live native chuffs"); audioObject.transform.SetParent(root.transform);
            var audio = audioObject.AddComponent(chuffType); SetupChuff(audio, chuffType);
            var loopObject = new GameObject("native loop cache fixture"); loopObject.SetActive(false); loopObject.transform.SetParent(audioObject.transform);
            var layerType = Assembly.LoadFrom(Path.Combine(managed, "DV.LayeredAudio.dll")).GetType("LayeredAudio", true);
            var layered = loopObject.AddComponent(layerType);
            var audioSource = loopObject.AddComponent<AudioSource>();
            var layerDefinitionType = layerType.GetNestedType("Layer");
            var layerDefinition = Activator.CreateInstance(layerDefinitionType);
            Set(layerDefinition, "source", audioSource); Set(layerDefinition, "volumeCurve", AnimationCurve.Linear(0, 0, 1, 0));
            var layerDefinitions = Array.CreateInstance(layerDefinitionType, 1); layerDefinitions.SetValue(layerDefinition, 0); Set(layered, "layers", layerDefinitions);
            var loopType = chuffType.GetNestedType("ChuffLoop"); var loop = Activator.CreateInstance(loopType);
            Set(loop, "chuffLoop", layered); Set(loop, "chuffFrequencyToMasterVolume", AnimationCurve.Linear(0, 0, 1, 0));
            var loops = Array.CreateInstance(loopType, 1); loops.SetValue(loop, 0); Set(audio, "chuffLoops", loops);
            var smokeType = game.GetType("DV.Simulation.Controllers.SteamSmokeParticlePortReader", true);
            var smoke = root.AddComponent(smokeType);
            Set(smoke, "fireOnPortId", "firebox.FIRE_ON"); Set(smoke, "isBoilerBrokenPortId", "boiler.IS_BROKEN");
            Set(smoke, "chuffEventPortId", "steamEngine.CHUFF_EVENT"); Set(smoke, "exhaustPressurePortId", "steamEngine.EXHAUST_PRESSURE");
            foreach (var prefix in new[] { "smoke", "ember" })
            {
                var particles = new GameObject(prefix); particles.transform.SetParent(root.transform);
                var system = particles.AddComponent<ParticleSystem>(); var main = system.main; main.playOnAwake = false;
                Set(smoke, prefix + "ParticlesParent", particles);
                foreach (var property in new[] { "StartSpeedMultiplier", "EmissionRateMultiplier", "MaxParticlesMultiplier" })
                    Set(smoke, prefix + property, AnimationCurve.Linear(0, 1, 1, 2));
            }
            var audioInit = chuffType.GetMethod("Init"); var audioDeinit = chuffType.GetMethod("Deinit");
            var smokeInit = smokeType.GetMethod("Init"); var smokeDeinit = smokeType.GetMethod("Deinit");
            var refresh = presentation.GetMethod("Refresh", Fields);
            // Native SimController.Initialize calls particles.Init before its
            // postfix publishes the owner. Exercise that exact ordering first.
            audioInit.Invoke(audio, new[] { car, flow }); smokeInit.Invoke(smoke, new[] { flow });
            Check(SubscriberCount(ports["steamEngine.CHUFF_EVENT"]) == 2, "readers initialize before owner registration");
            owners.GetType().GetMethod("Add").Invoke(owners, new[] { flow, simulation });
            var enroll = presentation.GetMethod("EnrollInitializedReaders", Fields);
            var smokeReaders = Array.CreateInstance(smokeType, 1); smokeReaders.SetValue(smoke, 0);
            var audioReaders = Array.CreateInstance(chuffType, 1); audioReaders.SetValue(audio, 0);
            enroll.Invoke(owner, new object[] { smokeReaders, audioReaders });
            enroll.Invoke(owner, new object[] { smokeReaders, audioReaders });
            Check(SubscriberCount(ports["steamEngine.CHUFF_EVENT"]) == 2, "stock audio and chimney each subscribe once");
            Check(SubscriberCount(ports["rs060FrontEngine.CHUFF_EVENT"]) == 0, "stock dormant unit silent");
            for (int cycle = 0; cycle < 12; cycle++)
                foreach (int mode in new[] { 1, 2, 0 })
                {
                    modeField.SetValue(bank, Enum.ToObject(modeField.FieldType, mode)); refresh.Invoke(owner, null);
                    string engine = mode == 1 ? "rs060FrontEngine" : "steamEngine";
                    Check((string)Get(audio, "chuffEventPortId") == engine + ".CHUFF_EVENT", "audio source mode " + mode);
                    Check((string)Get(audio, "exhaustPressurePortId") == engine + ".EXHAUST_PRESSURE", "audio pressure source");
                    Check((string)Get(smoke, "chuffEventPortId") == engine + ".CHUFF_EVENT", "chimney source mode " + mode);
                    Check(SubscriberCount(ports["steamEngine.CHUFF_EVENT"]) == (mode == 1 ? 0 : 2), "main event subscriptions");
                    Check(SubscriberCount(ports["rs060FrontEngine.CHUFF_EVENT"]) == (mode == 0 ? 0 : 2), "secondary event subscriptions");
                    Check((float)Get(audio, "mediumPressureThreshold") == (mode == 1 ? 3 : 7), "pressure band restores");
                    Check((float)Get(audio, "highPressureThreshold") == (mode == 1 ? 4.5f : 11), "high pressure band restores");
                    if (mode == 1)
                    {
                        ResetPulse(smoke);
                        SetValue(ports["steamEngine.EXHAUST_PRESSURE"], 8); SetValue(ports["rs060FrontEngine.EXHAUST_PRESSURE"], 1);
                        AdvanceEvent(ports["steamEngine.CHUFF_EVENT"]);
                        Check(Get(smoke, "chuffCoro") == null, "HP event cannot start Meyer chimney pulse");
                        SetValue(ports["rs060FrontEngine.EXHAUST_PRESSURE"], 4); AdvanceEvent(ports["rs060FrontEngine.CHUFF_EVENT"]);
                        Check(Get(smoke, "chuffCoro") != null, "LP event starts actual native Meyer chimney coroutine");
                    }
                    if (mode == 2)
                    {
                        var copy = audioObject.transform.parent.GetComponentsInChildren<Component>(true)
                            .First(c => c != null && c.GetType() == chuffType && c.name == "[S060 Dockside second exhaust]");
                        Check(!ReferenceEquals(Get(copy, "regularChuffConfig"), Get(template, "regularChuffConfig")), "second sound configuration is independently owned");
                        Check((Transform)Get(Get(copy, "regularChuffConfig"), "parent") == copy.transform, "second sound source maps to live subtree");
                        Check(!ReferenceEquals(Get(copy, "pressureToVolumeCurve"), Get(template, "pressureToVolumeCurve")), "second sound curve independent of prefab");
                        SetValue(ports["steamEngine.EXHAUST_PRESSURE"], 2); SetValue(ports["rs060FrontEngine.EXHAUST_PRESSURE"], 6);
                        Check(ReadPort(Get(smoke, "exhaustPressurePort")) == 6, "front-only high exhaust reaches chimney");
                        SetValue(ports["steamEngine.EXHAUST_PRESSURE"], 8);
                        Check(ReadPort(Get(smoke, "exhaustPressurePort")) == 8, "rear high exhaust reaches chimney");
                        SetValue(ports["steamEngine.EXHAUST_PRESSURE"], 1); SetValue(ports["rs060FrontEngine.EXHAUST_PRESSURE"], 1);
                        Check(ReadPort(Get(smoke, "exhaustPressurePort")) == 1, "cold stages return chimney to atmosphere");
                        ResetPulse(smoke); SetValue(ports["rs060FrontEngine.EXHAUST_PRESSURE"], 6);
                        AdvanceEvent(ports["steamEngine.CHUFF_EVENT"]);
                        Check(Get(smoke, "chuffCoro") == null, "cold rear unit cannot borrow front unit pressure to pulse");
                        AdvanceEvent(ports["rs060FrontEngine.CHUFF_EVENT"]);
                        Check(Get(smoke, "chuffCoro") != null, "front unit independently starts native chimney pulse");
                        ResetPulse(smoke); SetValue(ports["steamEngine.EXHAUST_PRESSURE"], 6); SetValue(ports["rs060FrontEngine.EXHAUST_PRESSURE"], 1);
                        AdvanceEvent(ports["steamEngine.CHUFF_EVENT"]);
                        Check(Get(smoke, "chuffCoro") != null, "rear unit independently starts native chimney pulse");
                        ResetPulse(smoke); SetValue(ports["steamEngine.EXHAUST_PRESSURE"], 1); SetValue(ports["rs060FrontEngine.EXHAUST_PRESSURE"], 1);
                        float sharedIndex = Mathf.Max(ReadPort(ports["steamEngine.CHUFF_EVENT"]), ReadPort(ports["rs060FrontEngine.CHUFF_EVENT"])) + 1;
                        SetValue(ports["steamEngine.EXHAUST_PRESSURE"], 4); SetValue(ports["rs060FrontEngine.EXHAUST_PRESSURE"], 4);
                        SetValue(ports["steamEngine.CHUFF_EVENT"], sharedIndex); SetValue(ports["rs060FrontEngine.CHUFF_EVENT"], sharedIndex);
                        Check((bool)Get(smoke, "nextChuffQueued"), "equal event indices from both units queue the second pulse");
                    }
                    // Streaming: native Deinit must remove all subscriptions and
                    // authored configuration must survive the next native Init.
                    layerType.GetMethod("SetVolume").Invoke(layered, new object[] { 0f });
                    Check((float)Get(layered, "lastVolumeInput") == 0, "native loop caches prior input");
                    audioDeinit.Invoke(audio, null); smokeDeinit.Invoke(smoke, null);
                    Check(float.IsPositiveInfinity((float)Get(layered, "lastVolumeInput")), "native loop cache resets so identical input can restart playback");
                    Check(SubscriberCount(ports["steamEngine.CHUFF_EVENT"]) == 0 && SubscriberCount(ports["rs060FrontEngine.CHUFF_EVENT"]) == 0, "stream out detaches every chuff callback");
                    Check(SubscriberCount(ports["steamEngine.EXHAUST_PRESSURE"]) == 0 && SubscriberCount(ports["rs060FrontEngine.EXHAUST_PRESSURE"]) == 0, "stream out detaches pressure callbacks");
                    Check((string)Get(audio, "chuffEventPortId") == "steamEngine.CHUFF_EVENT" && (float)Get(audio, "mediumPressureThreshold") == 7, "authored reader state restored");
                    audioInit.Invoke(audio, new[] { car, flow }); smokeInit.Invoke(smoke, new[] { flow });
                }
            var detachedObject = new GameObject("detached interior audio reader");
            var detached = detachedObject.AddComponent(chuffType); SetupChuff(detached, chuffType);
            audioInit.Invoke(detached, new[] { car, flow });
            foreach (int mode in new[] { 1, 2, 0 })
            {
                modeField.SetValue(bank, Enum.ToObject(modeField.FieldType, mode)); refresh.Invoke(owner, null);
                Check(SubscriberCount(ports["steamEngine.CHUFF_EVENT"]) == (mode == 1 ? 0 : 3), "detached interior main subscriptions");
                Check(SubscriberCount(ports["rs060FrontEngine.CHUFF_EVENT"]) == (mode == 0 ? 0 : 3), "detached interior secondary subscriptions");
            }
            audioDeinit.Invoke(detached, null); UnityEngine.Object.DestroyImmediate(detachedObject);
            audioDeinit.Invoke(audio, null); smokeDeinit.Invoke(smoke, null);
            ValidateDetachedAudio(game, sim, mod, harmony, root, flow, ports, portType, definitionType, typeEnum, valueEnum);
            owners.GetType().GetMethod("Remove").Invoke(owners, new[] { flow });
            UnityEngine.Object.DestroyImmediate(livery); UnityEngine.Object.DestroyImmediate(carDefinition);
            string runtimeHash;
            using (var sha = System.Security.Cryptography.SHA256.Create()) using (var stream = File.OpenRead(runtime))
                runtimeHash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
            File.WriteAllText(Path.Combine(output, "native-exhaust-validation.json"), "{\"passed\":true,\"runtimeSha256\":\"" + runtimeHash + "\",\"checks\":" + checks + ",\"modeCycles\":12,\"nativeB99Readers\":true,\"productionHarmonyHooks\":true,\"smokeBeforeOwnerRegistration\":true,\"detachedPooledAudio\":true,\"nativeChimneyPulseStarted\":true,\"gameScene\":false,\"audiblePlaybackValidated\":false}");
            Debug.Log("Native exhaust routing: " + checks + " passed."); EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            foreach (var component in Resources.FindObjectsOfTypeAll<MonoBehaviour>().Where(c => c != null && c.GetType().Name == "ChuffClipsSimReader"))
                foreach (var name in new[] { "chuffLoops", "waterChuffLoops", "ashChuffLoops", "regularChuffConfig", "waterChuffConfig", "ashChuffConfig" })
                {
                    var field = Get(component, name); Debug.Log(component.name + " " + name + "=" + (field == null ? "NULL" : field.GetType().Name));
                    if (field != null && name.EndsWith("Config")) Debug.Log("curves=" + Get(field, "spatialCurve") + "," + Get(field, "chuffFrequencyToMasterVolume"));
                }
            File.WriteAllText(Path.Combine(output, "native-exhaust-validation.json"), "{\"passed\":false,\"checks\":" + checks + "}");
            Debug.LogException(ex); EditorApplication.Exit(1);
        }
        finally
        {
            if (harmony != null) harmony.GetType().GetMethod("UnpatchAll").Invoke(harmony, new object[] { "s060.exhaust.offline.audit" });
            AppDomain.CurrentDomain.AssemblyResolve -= Resolve;
        }
    }
    private static void ValidateDetachedAudio(Assembly game, Assembly sim, Assembly mod, object harmony, GameObject root,
        object flow, IDictionary ports, Type portType, Type definition, Type typeEnum, Type valueEnum)
    {
        var helper = mod.GetType("RearrangedS060.S060NativeAudio", true);
        var stock = mod.GetType("RearrangedS060.S060StockCocks", true);
        foreach (var type in new[] { helper, stock }) type.GetMethod("Register", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new[] { harmony });
        foreach (var suffix in new[] { "CRANK_ROTATION", "CYLINDERS_INLET_VALVE_OPEN", "CYLINDER_COCK_FLOW_NORMALIZED" })
            AddPort(ports, portType, definition, typeEnum, valueEnum, "steamEngine", suffix, suffix == "CYLINDERS_INLET_VALVE_OPEN" ? 3 : 1);
        AddPort(ports, portType, definition, typeEnum, valueEnum, "rs060MutedCock", "VALUE", 0);
        var carType = game.GetType("TrainCar", true);
        var carObject = new GameObject("inactive native car; no game-world Awake"); carObject.SetActive(false);
        carObject.transform.SetParent(root.transform); carObject.transform.SetPositionAndRotation(new Vector3(5, 2, 7), Quaternion.Euler(0, 73, 0));
        var car = carObject.AddComponent(carType);
        var simController = carObject.AddComponent(carType.GetProperty("SimController").PropertyType);
        Set(simController, "simFlow", flow);
        var interior = new GameObject("detached native interior"); interior.transform.SetPositionAndRotation(carObject.transform.position, carObject.transform.rotation);
        Set(car, "interior", interior.transform);
        var audioBase = game.GetType("TrainAudio", true);
        // Skip only CarModularAudio's game-manager mixing registration. Native
        // TrainAudio.SetupForCar, parenting, events and our Harmony hooks run.
        var audioType = game.GetType("DV.ModularAudioCar.CarModularAudio", true);
        var harmonyMethodType = harmony.GetType().Assembly.GetType("HarmonyLib.HarmonyMethod", true);
        var skip = Activator.CreateInstance(harmonyMethodType, new object[] { typeof(ExhaustNativeAudit).GetMethod("SkipOfflineMixing", BindingFlags.Static | BindingFlags.NonPublic) });
        var patch = harmony.GetType().GetMethod("Patch"); var patchArgs = new object[patch.GetParameters().Length];
        patchArgs[0] = audioType.GetMethod("Initialize", Fields); patchArgs[1] = skip; patch.Invoke(harmony, patchArgs);
        var audioObject = new GameObject("pooled native audio fixture"); audioObject.SetActive(false);
        var nativeAudio = audioObject.AddComponent(audioType);
        var cockType = game.GetType("DV.ModularAudioCar.CylinderCockLayeredPortReader", true);
        var reader = audioObject.AddComponent(cockType);
        foreach (var pair in new[] { new[] { "crankRotation", "steamEngine.CRANK_ROTATION" },
            new[] { "cylindersInletValveOpen", "steamEngine.CYLINDERS_INLET_VALVE_OPEN" },
            new[] { "cylinderCockFlowNormalized", "steamEngine.CYLINDER_COCK_FLOW_NORMALIZED" }, new[] { "cylinderCockControl", "cylinderCock.EXT_IN" } })
            Set(reader, pair[0] + "PortId", pair[1]);
        var layerType = Assembly.LoadFrom(Path.Combine(managed, "DV.LayeredAudio.dll")).GetType("LayeredAudio", true);
        var emitters = Array.CreateInstance(layerType, 2);
        for (int i = 0; i < 2; i++)
        {
            var emitter = new GameObject("native cock emitter " + i); emitter.transform.SetParent(audioObject.transform, false);
            emitter.transform.localPosition = new Vector3(i == 0 ? -1 : 1, .8f, 3);
            var layered = emitter.AddComponent(layerType); Set(layered, "layers", Array.CreateInstance(layerType.GetNestedType("Layer"), 0)); emitters.SetValue(layered, i);
        }
        Set(reader, "cylCockAudio", emitters);
        var crackParent = new GameObject("CylinderCrack"); crackParent.transform.SetParent(audioObject.transform, false);
        var crack = new GameObject("SteamRelease_Layered"); crack.transform.SetParent(crackParent.transform, false); crack.transform.localPosition = new Vector3(.6f, .9f, 2.8f);
        var homeCrack = crack.transform.localPosition;
        var setup = audioBase.GetMethod("SetupForCar"); var init = cockType.GetMethod("Init"); var deinit = cockType.GetMethod("Deinit");
        setup.Invoke(nativeAudio, new[] { car }); init.Invoke(reader, new[] { car, flow });
        var stockOwner = Activator.CreateInstance(stock, Fields, null, new[] { car }, null);
        stock.GetMethod("Mute", Fields).Invoke(stockOwner, null);
        Check((string)Get(reader, "cylinderCockFlowNormalizedPortId") == "rs060MutedCock.VALUE", "detached native stock flow muted");
        Check((string)Get(reader, "cylinderCockControlPortId") == "rs060MutedCock.VALUE", "detached cold-air hiss control muted too");
        Check(SubscriberCount(ports["steamEngine.CYLINDER_COCK_FLOW_NORMALIZED"]) == 0, "stock flow callback removed while articulated cocks own outlets");
        Check(SubscriberCount(ports["rs060MutedCock.VALUE"]) == 2, "one native stock reader binds exactly its two muted inputs");
        // A bogie reader has no native TrainAudio ancestor: even the main-slot
        // unit must keep its normal flow/control rather than be muted as stock.
        var unit = new GameObject("articulated unit reader"); unit.transform.SetParent(carObject.transform, false);
        var unitReader = unit.AddComponent(cockType);
        foreach (var field in new[] { "crankRotationPortId", "cylindersInletValveOpenPortId", "cylinderCockFlowNormalizedPortId", "cylinderCockControlPortId" })
            Set(unitReader, field, field == "cylinderCockFlowNormalizedPortId" ? "steamEngine.CYLINDER_COCK_FLOW_NORMALIZED" : field == "cylinderCockControlPortId" ? "cylinderCock.EXT_IN" : Get(reader, field));
        Set(unitReader, "cylCockAudio", Array.CreateInstance(layerType, 0)); init.Invoke(unitReader, new[] { car, flow });
        Check((string)Get(unitReader, "cylinderCockFlowNormalizedPortId") == "steamEngine.CYLINDER_COCK_FLOW_NORMALIZED", "main-slot bogie reader is not mistaken for stock");
        deinit.Invoke(unitReader, null);
        for (int cycle = 0; cycle < 3; cycle++)
        {
            deinit.Invoke(reader, null);
            Check((string)Get(reader, "cylinderCockFlowNormalizedPortId") == "steamEngine.CYLINDER_COCK_FLOW_NORMALIZED", "stream-out restores authored stock flow");
            Check(SubscriberCount(ports["rs060MutedCock.VALUE"]) == 0, "stream-out removes mute callbacks");
            init.Invoke(reader, new[] { car, flow });
            Check(SubscriberCount(ports["rs060MutedCock.VALUE"]) == 2, "stream-in reapplies one mute binding");
        }
        stock.GetMethod("Dispose").Invoke(stockOwner, null);
        Check((string)Get(reader, "cylinderCockControlPortId") == "cylinderCock.EXT_IN", "stock selection restores cab control input");
        Check(SubscriberCount(ports["steamEngine.CYLINDER_COCK_FLOW_NORMALIZED"]) == 1 && SubscriberCount(ports["rs060MutedCock.VALUE"]) == 0, "stock selection restores exactly one native flow callback");
        var offset = new Vector3(0, -.12f, -.4f);
        var placement = helper.GetMethod("Translate", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { car, offset });
        var firstEmitter = ((Component)emitters.GetValue(0)).transform;
        var expected = carObject.transform.TransformPoint(new Vector3(-1, .8f, 3) + offset);
        Check(Vector3.Distance(firstEmitter.position, expected) < .00001f, "detached cock emitter translates in car space");
        Check(Vector3.Distance(crack.transform.position, carObject.transform.TransformPoint(homeCrack + offset)) < .00001f, "detached crack sound translates with cylinder");
        helper.GetMethod("AudioReturning", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new[] { nativeAudio, car });
        Check(crack.transform.localPosition == homeCrack, "pre-pool callback restores crack source pose");
        setup.Invoke(nativeAudio, new[] { car });
        Check(Vector3.Distance(firstEmitter.position, expected) < .00001f, "pooled audio arriving after conversion receives translation once");
        placement.GetType().GetMethod("Dispose").Invoke(placement, null);
        Check(crack.transform.localPosition == homeCrack && firstEmitter.localPosition == new Vector3(-1, .8f, 3), "conventional restoration restores exact native poses");
        var frame = new GameObject("Meyer front frame"); frame.transform.SetParent(carObject.transform, false);
        placement = helper.GetMethod("Follow", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { car, offset, frame.transform });
        Check(crack.transform.parent == frame.transform && firstEmitter.parent == audioObject.transform, "Meyer crack follows cylinder frame while stock cock holder stays native");
        frame.transform.localRotation = Quaternion.Euler(0, 15, 0);
        Check(Vector3.Distance(crack.transform.position, frame.transform.TransformPoint(homeCrack + offset)) < .00001f, "Meyer crack source follows frame articulation");
        helper.GetMethod("AudioReturning", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new[] { nativeAudio, car });
        Check(crack.transform.parent == crackParent.transform && crack.transform.localPosition == homeCrack, "moved source restored beneath pooled controller before release");
        setup.Invoke(nativeAudio, new[] { car });
        Check(crack.transform.parent == frame.transform, "streamed crack source reattaches after native SetupForCar");
        placement.GetType().GetMethod("Dispose").Invoke(placement, null);
        Check(crack.transform.parent == crackParent.transform && crack.transform.localPosition == homeCrack, "Meyer selection cleanup restores original source hierarchy");
        deinit.Invoke(reader, null);
        UnityEngine.Object.DestroyImmediate(audioObject); UnityEngine.Object.DestroyImmediate(interior);
    }
    private static bool SkipOfflineMixing() => false;
    private static void SetupChuff(object reader, Type type)
    {
        foreach (var pair in new[] { new[] { "chuffEvent", "CHUFF_EVENT" }, new[] { "exhaustPressure", "EXHAUST_PRESSURE" },
            new[] { "chuffFrequency", "CHUFF_FREQUENCY" }, new[] { "cylinderWaterNormalized", "WATER_IN_CYLINDERS_NORMALIZED" }, new[] { "ashesInPipes", "ASHES_IN_PIPES" } })
            Set(reader, pair[0] + "PortId", "steamEngine." + pair[1]);
        Set(reader, "cylinderCockControlPortId", "cylinderCock.EXT_IN");
        Set(reader, "mediumPressureThreshold", 7f); Set(reader, "highPressureThreshold", 11f);
        Set(reader, "pressureToVolumeCurve", AnimationCurve.Linear(1, 0, 10, 1));
        foreach (string field in new[] { "regularChuffConfig", "waterChuffConfig", "ashChuffConfig" })
        {
            var definition = Activator.CreateInstance(type.GetField(field).FieldType);
            Set(definition, "chuffFrequencyToMasterVolume", AnimationCurve.Linear(0, 0, 1, 0));
            Set(definition, "spatialCurve", new AnimationCurve()); Set(reader, field, definition);
            Set(definition, "parent", ((Component)reader).transform);
        }
        foreach (var field in new[] { "chuffLoops", "waterChuffLoops", "ashChuffLoops", "waterChuffClips", "ashChuffClips" })
            Set(reader, field, Array.CreateInstance(type.GetField(field).FieldType.GetElementType(), 0));
    }
    private static void AddPort(IDictionary ports, Type port, Type definition, Type typeEnum, Type valueEnum, string component, string suffix, float value)
    {
        var def = Activator.CreateInstance(definition, new[] { Enum.Parse(typeEnum, "READONLY_OUT"), Enum.Parse(valueEnum, "GENERIC"), (object)suffix });
        ports.Add(component + "." + suffix, Activator.CreateInstance(port, new[] { component, def, (object)value }));
    }
    private static int SubscriberCount(object port)
    { var action = (Delegate)port.GetType().GetField("ValueUpdatedInternally", Fields).GetValue(port); return action == null ? 0 : action.GetInvocationList().Length; }
    private static object Get(object obj, string field) { return obj.GetType().GetField(field, Fields).GetValue(obj); }
    private static void Set(object obj, string field, object value) { obj.GetType().GetField(field, Fields).SetValue(obj, value); }
    private static float ReadPort(object port) { return (float)port.GetType().GetProperty("Value").GetValue(port, null); }
    private static void SetValue(object port, float value) { port.GetType().GetProperty("Value").SetValue(port, value, null); }
    private static void AdvanceEvent(object port) { SetValue(port, ReadPort(port) + 1); }
    private static void ResetPulse(object smoke)
    {
        ((MonoBehaviour)smoke).StopAllCoroutines(); Set(smoke, "chuffCoro", null); Set(smoke, "stopAllCoro", null); Set(smoke, "nextChuffQueued", false);
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); checks++; }
    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        string name = new AssemblyName(args.Name).Name;
        var loaded = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name);
        if (loaded != null) return loaded;
        string mods = Environment.GetEnvironmentVariable("S060_AUDIT_MODS");
        if (string.IsNullOrEmpty(mods)) mods = Path.GetFullPath(Path.Combine(managed, "..", "..", "Mods"));
        foreach (string directory in new[] { managed, Path.Combine(managed, "UnityModManager"), Path.Combine(mods, "LocoMeshSplitter") })
        { string path = Path.Combine(directory, name + ".dll"); if (File.Exists(path)) return Assembly.LoadFrom(path); }
        return null;
    }
}
#endif
