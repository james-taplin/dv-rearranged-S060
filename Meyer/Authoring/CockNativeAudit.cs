#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEditor;
using UnityEngine;

// Offline evidence only: load the installed B99 reader, not a reimplementation.
// No game assemblies or assets are copied into the authoring project or packs.
public static class CockNativeAudit
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static string managed;
    public static void RunBatch()
    {
        var output = Environment.GetEnvironmentVariable("S060_COCK_AUDIT_OUTPUT");
        if (string.IsNullOrEmpty(output)) throw new InvalidOperationException("S060_COCK_AUDIT_OUTPUT is required.");
        Directory.CreateDirectory(output);
        managed = Environment.GetEnvironmentVariable("S060_AUDIT_MANAGED");
        if (string.IsNullOrEmpty(managed)) managed = @"B:\SteamLibrary\steamapps\common\Derail Valley\DerailValley_Data\Managed";
        AppDomain.CurrentDomain.AssemblyResolve += Resolve;
        var root = new GameObject("Native B99 cock lifecycle audit");
        int checks = 0;
        try
        {
            var assembly = Assembly.LoadFrom(Path.Combine(managed, "DV.Simulation.dll"));
            var gameAssembly = Assembly.LoadFrom(Path.Combine(managed, "Assembly-CSharp.dll"));
            var readerType = gameAssembly.GetType("DV.Simulation.Controllers.CylinderCockParticlePortReader", true);
            var reader = root.AddComponent(readerType);
            var setupType = readerType.GetNestedType("CylinderSetup");
            var setups = Array.CreateInstance(setupType, 2);
            var systems = new System.Collections.Generic.List<ParticleSystem>();
            for (int i = 0; i < 2; i++)
            {
                var setup = Activator.CreateInstance(setupType);
                foreach (var side in new[] { "front", "rear" })
                {
                    var leaf = new GameObject(side + " " + i); leaf.transform.SetParent(root.transform, false);
                    // Both native wispy/wave leaves must survive close/reopen.
                    for (int j = 0; j < 2; j++)
                    {
                        var child = new GameObject("system " + j); child.transform.SetParent(leaf.transform, false);
                        var ps = child.AddComponent<ParticleSystem>(); systems.Add(ps);
                        var main = ps.main; main.playOnAwake = false; main.startSpeed = 3; main.startSize = .2f; main.startLifetime = .1f;
                        var emission = ps.emission; emission.rateOverTime = 20;
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    }
                    setupType.GetField(side + "ParticlesParent").SetValue(setup, leaf);
                    setupType.GetField(side + "ActivityCurve").SetValue(setup, AnimationCurve.Linear(0, 1, 1, 1));
                }
                setups.SetValue(setup, i);
            }
            readerType.GetField("cylinderSetups").SetValue(reader, setups);
            foreach (var name in new[] { "startSpeedMultiplierMin", "startSpeedMultiplierMax", "startSizeMultiplierMin", "startSizeMultiplierMax", "emissionRateMultiplierMin", "emissionRateMultiplierMax" })
                readerType.GetField(name).SetValue(reader, 1f);
            var portType = assembly.GetType("LocoSim.Implementations.Port", true);
            var definitionType = assembly.GetType("LocoSim.Definitions.PortDefinition", true);
            var typeEnum = assembly.GetType("LocoSim.Definitions.PortType", true);
            var valueEnum = assembly.GetType("LocoSim.Definitions.PortValueType", true);
            var dictType = typeof(System.Collections.Generic.Dictionary<,>).MakeGenericType(typeof(string), portType);
            var ports = (IDictionary)Activator.CreateInstance(dictType);
            string[] names = { "crankRotation", "reverser", "cylindersInletValveOpen", "cylinderCockFlowNormalized", "forwardSpeed" };
            foreach (var name in names)
            {
                var id = "audit." + name;
                var definition = Activator.CreateInstance(definitionType, new[] { Enum.Parse(typeEnum, "READONLY_OUT"), Enum.Parse(valueEnum, "GENERIC"), (object)name });
                var port = Activator.CreateInstance(portType, new[] { "audit", definition, (object)0f });
                ports.Add(id, port); readerType.GetField(name + "PortId").SetValue(reader, id);
            }
            var flowType = assembly.GetType("LocoSim.Implementations.SimulationFlow", true);
            var flow = FormatterServices.GetUninitializedObject(flowType);
            var readOnly = typeof(System.Collections.ObjectModel.ReadOnlyDictionary<,>).MakeGenericType(typeof(string), portType);
            flowType.GetField("fullPortIdToPort", Fields).SetValue(flow, Activator.CreateInstance(readOnly, ports));
            var init = readerType.GetMethod("Init"); var deinit = readerType.GetMethod("Deinit");
            var update = readerType.GetMethod("UpdateCylCockParticles", Fields);
            var valueProperty = portType.GetProperty("Value");
            Action<string, float> value = (name, v) => valueProperty.SetValue(ports["audit." + name], v, null);
            value("reverser", 1); value("cylindersInletValveOpen", 3);
            init.Invoke(reader, new[] { flow });
            // Native closed reader disables its stopped leaves. Re-init without
            // reactivation reproduces the empty cached-array failure.
            update.Invoke(reader, new object[] { .25f }); update.Invoke(reader, new object[] { .25f });
            deinit.Invoke(reader, null);
            checks += PhaseTimingRegression(root, gameAssembly, assembly);
            checks += WaterDripRegression(root, gameAssembly, assembly);
            Check(systems.All(p => !p.gameObject.activeSelf), "native closed state disables leaves", ref checks);
            foreach (var ps in systems) ps.gameObject.SetActive(true);
            init.Invoke(reader, new[] { flow });
            for (int cycle = 0; cycle < 20; cycle++)
            {
                value("cylinderCockFlowNormalized", 1); update.Invoke(reader, new object[] { .25f });
                Check(systems.All(p => p.isEmitting && p.gameObject.activeSelf), "open cycle " + cycle, ref checks);
                value("cylinderCockFlowNormalized", 0); update.Invoke(reader, new object[] { .25f });
                foreach (var ps in systems) ps.Clear();
                update.Invoke(reader, new object[] { .25f });
                Check(systems.All(p => !p.isEmitting && !p.gameObject.activeSelf), "close cycle " + cycle, ref checks);
            }
            deinit.Invoke(reader, null);
            File.WriteAllText(Path.Combine(output, "native-cock-validation.json"), "{\"passed\":true,\"checks\":" + checks + ",\"reader\":\"installed B99 CylinderCockParticlePortReader\",\"ownedFixedCrankFlowAndReverser\":true,\"ownedInletPublicationOrder\":true,\"nativeLowFlowThreshold\":true,\"ownedWetDripGatesAndReuse\":true,\"unityGameScene\":false}");
            Debug.Log("Actual native B99 cock lifecycle: " + checks + " checks passed.");
            UnityEngine.Object.DestroyImmediate(root);
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(output, "native-cock-validation.json"), "{\"passed\":false,\"checks\":" + checks + "}");
            Debug.LogException(ex); UnityEngine.Object.DestroyImmediate(root); EditorApplication.Exit(1);
        }
        finally { AppDomain.CurrentDomain.AssemblyResolve -= Resolve; }
    }
    private static int PhaseTimingRegression(GameObject parent, Assembly game, Assembly sim)
    {
        int checks = 0;
        var root = new GameObject("Owned fixed-crank timing"); root.transform.SetParent(parent.transform, false);
        var readerType = game.GetType("DV.Simulation.Controllers.CylinderCockParticlePortReader", true);
        var reader = root.AddComponent(readerType);
        var setupType = readerType.GetNestedType("CylinderSetup"); var setup = Activator.CreateInstance(setupType);
        var systems = new ParticleSystem[2];
        for (int i = 0; i < 2; i++)
        {
            var leaf = new GameObject(i == 0 ? "front" : "rear"); leaf.transform.SetParent(root.transform, false);
            var ps = leaf.AddComponent<ParticleSystem>(); systems[i] = ps;
            var main = ps.main; main.playOnAwake = false; main.startLifetime = .1f;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            setupType.GetField(i == 0 ? "frontParticlesParent" : "rearParticlesParent").SetValue(setup, leaf);
            setupType.GetField(i == 0 ? "frontActivityCurve" : "rearActivityCurve").SetValue(setup, new AnimationCurve(
                new Keyframe(0, i == 0 ? 1 : 0), new Keyframe(.49f, i == 0 ? 1 : 0), new Keyframe(.5f, i == 0 ? 0 : 1), new Keyframe(1, i == 0 ? 0 : 1)));
        }
        var setups = Array.CreateInstance(setupType, 1); setups.SetValue(setup, 0); readerType.GetField("cylinderSetups").SetValue(reader, setups);
        foreach (var name in new[] { "startSpeedMultiplierMin", "startSpeedMultiplierMax", "startSizeMultiplierMin", "startSizeMultiplierMax", "emissionRateMultiplierMin", "emissionRateMultiplierMax" }) readerType.GetField(name).SetValue(reader, 1f);
        var portType = sim.GetType("LocoSim.Implementations.Port", true);
        var definition = sim.GetType("LocoSim.Definitions.PortDefinition", true);
        var typeEnum = sim.GetType("LocoSim.Definitions.PortType", true); var valueEnum = sim.GetType("LocoSim.Definitions.PortValueType", true);
        var ports = (IDictionary)Activator.CreateInstance(typeof(System.Collections.Generic.Dictionary<,>).MakeGenericType(typeof(string), portType));
        foreach (string name in new[] { "crankRotation", "reverser", "cylindersInletValveOpen", "cylinderCockFlowNormalized", "forwardSpeed" })
        {
            var def = Activator.CreateInstance(definition, new[] { Enum.Parse(typeEnum, "READONLY_OUT"), Enum.Parse(valueEnum, "GENERIC"), (object)name });
            ports.Add("timing." + name, Activator.CreateInstance(portType, new[] { "timing", def, (object)0f }));
            readerType.GetField(name + "PortId").SetValue(reader, "timing." + name);
        }
        var flowType = sim.GetType("LocoSim.Implementations.SimulationFlow", true); var flow = FormatterServices.GetUninitializedObject(flowType);
        flowType.GetField("fullPortIdToPort", Fields).SetValue(flow, Activator.CreateInstance(typeof(System.Collections.ObjectModel.ReadOnlyDictionary<,>).MakeGenericType(typeof(string), portType), ports));
        Action<string, float> value = (name, v) => portType.GetProperty("Value").SetValue(ports["timing." + name], v, null);
        value("reverser", 1); value("cylindersInletValveOpen", 1); value("cylinderCockFlowNormalized", 1);
        readerType.GetMethod("Init").Invoke(reader, new[] { flow }); value("crankRotation", .125f);
        Check(systems[0].isEmitting && !systems[1].isEmitting, "native crank event selects front outlet", ref checks);
        value("cylinderCockFlowNormalized", .8f);
        Check(!systems[0].isEmitting && systems[1].isEmitting, "unmodified B99 reproduces flow-value-as-phase defect", ref checks);
        value("cylindersInletValveOpen", 0); value("crankRotation", .2f);
        value("cylindersInletValveOpen", 1);
        Check(systems.All(p => !p.isEmitting), "unmodified B99 misses inlet-mask publication after crank", ref checks);
        var mod = Assembly.LoadFrom(Environment.GetEnvironmentVariable("S060_EXHAUST_AUDIT_RUNTIME"));
        var timing = mod.GetType("RearrangedS060.S060CockTiming", true).GetMethod("Bind", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new[] { reader });
        Check(systems[0].isEmitting && !systems[1].isEmitting, "owned timing enrollment immediately restores actual phase", ref checks);
        value("cylindersInletValveOpen", 0);
        Check(systems.All(p => !p.isEmitting), "owned inlet-only closure stops jets at fixed crank and flow", ref checks);
        value("crankRotation", .25f); value("cylindersInletValveOpen", 1);
        Check(systems[0].isEmitting && !systems[1].isEmitting, "owned crank-then-inlet publication resumes jets with unchanged flow", ref checks);
        foreach (float low in new[] { 0f, .006739493f, .01f })
        {
            value("cylinderCockFlowNormalized", low);
            Check(systems.All(p => !p.isEmitting), "actual captured cold-LP flow remains below native jet threshold: " + low, ref checks);
        }
        value("cylinderCockFlowNormalized", .011f);
        Check(systems[0].isEmitting && !systems[1].isEmitting, "native jets resume only above actual flow threshold", ref checks);
        value("cylinderCockFlowNormalized", .6f);
        Check(systems[0].isEmitting && !systems[1].isEmitting, "flow-only update retains fixed crank outlet", ref checks);
        value("reverser", .9f);
        Check(systems[0].isEmitting && !systems[1].isEmitting, "positive reverser-only update retains actual crank", ref checks);
        value("reverser", -.6f);
        Check(!systems[0].isEmitting && systems[1].isEmitting, "reverse swaps native curves at actual fixed crank", ref checks);
        value("cylinderCockFlowNormalized", .4f);
        Check(!systems[0].isEmitting && systems[1].isEmitting, "reverse flow-only update stays on correct end", ref checks);
        value("crankRotation", .75f);
        Check(systems[0].isEmitting && !systems[1].isEmitting, "moving crank retains native reverse timing", ref checks);
        ((IDisposable)timing).Dispose(); ((IDisposable)timing).Dispose();
        readerType.GetMethod("Deinit").Invoke(reader, null);
        foreach (string name in new[] { "crankRotation", "reverser", "cylinderCockFlowNormalized", "cylindersInletValveOpen" })
        {
            var callback = (Delegate)portType.GetField("ValueUpdatedInternally", Fields).GetValue(ports["timing." + name]);
            Check(callback == null, "owned disposal and native Deinit remove every callback: " + name, ref checks);
        }
        foreach (var ps in systems) ps.gameObject.SetActive(true);
        readerType.GetMethod("Init").Invoke(reader, new[] { flow });
        timing = mod.GetType("RearrangedS060.S060CockTiming", true).GetMethod("Bind", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new[] { reader });
        readerType.GetMethod("Deinit").Invoke(reader, null);
        Check(readerType.GetField("crankRotationPort", Fields).GetValue(reader) == null, "actual B99 native Deinit clears cached crank port", ref checks);
        ((IDisposable)timing).Dispose();
        foreach (string name in new[] { "crankRotation", "reverser", "cylinderCockFlowNormalized", "cylindersInletValveOpen" })
        {
            var callback = (Delegate)portType.GetField("ValueUpdatedInternally", Fields).GetValue(ports["timing." + name]);
            Check(callback == null, "teardown-first disposal cannot resurrect native callback: " + name, ref checks);
        }
        return checks;
    }
    private static int WaterDripRegression(GameObject parent, Assembly game, Assembly sim)
    {
        int checks = 0;
        var mod = Assembly.LoadFrom(Environment.GetEnvironmentVariable("S060_EXHAUST_AUDIT_RUNTIME"));
        var runtime = mod.GetType("RearrangedS060.MeyerRuntime", true);
        var harmonyType = Assembly.LoadFrom(Path.Combine(managed, "UnityModManager/0Harmony.dll")).GetType("HarmonyLib.Harmony", true);
        var harmony = Activator.CreateInstance(harmonyType, "s060.cock.drips.offline.audit");
        var processor = harmonyType.GetMethod("CreateClassProcessor").Invoke(harmony, new object[] { runtime.GetNestedType("ParticleDestroy", BindingFlags.NonPublic) });
        processor.GetType().GetMethod("Patch").Invoke(processor, null);
        var root = new GameObject("[Meyer engine effects]"); root.transform.SetParent(parent.transform, false);
        var controllerType = game.GetType("DV.Simulation.Controllers.ParticlesPortReadersController", true);
        var controller = root.AddComponent(controllerType);
        var pprType = controllerType.GetNestedType("ParticlePortReader");
        var updaterType = pprType.GetNestedType("PortParticleUpdateDefinition");
        var propertyType = pprType.GetNestedType("PropertyChangeDefinition");
        var readers = (IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(pprType));
        controllerType.GetField("particlePortReaders").SetValue(controller, readers);
        var gateObjects = new System.Collections.Generic.List<object>();
        try
        {
            // Production preparation must re-enable both pooled native jet
            // leaves and independent drip aliases before native cache capture.
            var cockType = game.GetType("DV.Simulation.Controllers.CylinderCockParticlePortReader", true);
            var cock = root.AddComponent(cockType); var setupType = cockType.GetNestedType("CylinderSetup");
            var setup = Activator.CreateInstance(setupType);
            foreach (string side in new[] { "front", "rear" })
            { var leaf = new GameObject(side); leaf.transform.SetParent(root.transform, false); leaf.AddComponent<ParticleSystem>(); leaf.SetActive(false); setupType.GetField(side + "ParticlesParent").SetValue(setup, leaf); }
            var setups = Array.CreateInstance(setupType, 1); setups.SetValue(setup, 0); cockType.GetField("cylinderSetups").SetValue(cock, setups);
            var portType = sim.GetType("LocoSim.Implementations.Port", true);
            var definitionType = sim.GetType("LocoSim.Definitions.PortDefinition", true);
            var portEnum = sim.GetType("LocoSim.Definitions.PortType", true); var valueEnum = sim.GetType("LocoSim.Definitions.PortValueType", true);
            var ports = (IDictionary)Activator.CreateInstance(typeof(System.Collections.Generic.Dictionary<,>).MakeGenericType(typeof(string), portType));
            foreach (string id in new[] { "rs060FrontEngine.WATER_IN_CYLINDERS_NORMALIZED", "cylinderCock.EXT_IN" })
            {
                int dot = id.LastIndexOf('.'); var name = id.Substring(dot + 1);
                var def = Activator.CreateInstance(definitionType, new[] { Enum.Parse(portEnum, "READONLY_OUT"), Enum.Parse(valueEnum, "GENERIC"), (object)name });
                ports.Add(id, Activator.CreateInstance(portType, new[] { id.Substring(0, dot), def, (object)(id.StartsWith("rs060") ? .25f : 1f) }));
            }
            var flowType = sim.GetType("LocoSim.Implementations.SimulationFlow", true); var flow = FormatterServices.GetUninitializedObject(flowType);
            flowType.GetField("fullPortIdToPort", Fields).SetValue(flow, Activator.CreateInstance(typeof(System.Collections.ObjectModel.ReadOnlyDictionary<,>).MakeGenericType(typeof(string), portType), ports));
            Action<string, float> value = (id, v) => portType.GetProperty("Value").SetValue(ports[id], v, null);
            var systems = new ParticleSystem[2];
            for (int i = 0; i < 2; i++)
            {
                var holder = new GameObject("Water drip " + i); holder.transform.SetParent(root.transform, false);
                var ps = holder.AddComponent<ParticleSystem>(); systems[i] = ps;
                var main = ps.main; main.playOnAwake = false; main.startLifetime = .1f; ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); holder.SetActive(false);
                var ppr = Activator.CreateInstance(pprType); pprType.GetField("particlesParent").SetValue(ppr, holder);
                var updaters = (IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(updaterType));
                foreach (string id in ports.Keys)
                {
                    var gate = Activator.CreateInstance(updaterType); updaterType.GetField("portId").SetValue(gate, id);
                    updaterType.GetField("inputModifier").SetValue(gate, Activator.CreateInstance(controllerType.GetNestedType("ValueModifier")));
                    var properties = (IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(propertyType));
                    var property = Activator.CreateInstance(propertyType);
                    propertyType.GetField("propertyType").SetValue(property, Enum.Parse(pprType.GetNestedType("ParticleProperty"), "ON_OFF"));
                    propertyType.GetField("propertyChangeCurve").SetValue(property, AnimationCurve.Linear(0, 0, 1, 1)); properties.Add(property);
                    updaterType.GetField("propertiesToUpdate").SetValue(gate, properties); updaters.Add(gate); gateObjects.Add(gate);
                }
                pprType.GetField("particleUpdaters").SetValue(ppr, updaters); readers.Add(ppr);
            }
            runtime.GetMethod("PrepareCockParticles", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new[] { controller });
            Check(systems.All(p => p.gameObject.activeSelf), "production preparation reactivates both pooled LP drip aliases", ref checks);
            foreach (var ppr in readers)
            {
                pprType.GetMethod("Init").Invoke(ppr, null); pprType.GetField("particlesPortReader").SetValue(ppr, controller);
                foreach (var gate in (IList)pprType.GetField("particleUpdaters").GetValue(ppr)) updaterType.GetMethod("Init").Invoke(gate, new[] { ppr, flow });
            }
            Check(systems.All(p => p.isEmitting), "wet LP cylinders drip with cab open without any steam-flow port", ref checks);
            for (int cycle = 0; cycle < 12; cycle++)
            {
                value("cylinderCock.EXT_IN", 0); Check(systems.All(p => !p.isEmitting), "native LP drips stop on cab closure " + cycle, ref checks);
                value("cylinderCock.EXT_IN", 1); Check(systems.All(p => p.isEmitting), "native LP drips resume on cab reopening " + cycle, ref checks);
            }
            value("rs060FrontEngine.WATER_IN_CYLINDERS_NORMALIZED", 0);
            Check(systems.All(p => !p.isEmitting), "empty LP cylinders cannot fabricate drips", ref checks);
            value("rs060FrontEngine.WATER_IN_CYLINDERS_NORMALIZED", .00001f);
            Check(systems.All(p => p.isEmitting), "native authored water gate accepts any positive condensate", ref checks);
            foreach (var gate in gateObjects) updaterType.GetMethod("Deinit").Invoke(gate, new[] { flow }); gateObjects.Clear();
            foreach (var port in ports.Values) Check(portType.GetField("ValueUpdatedInternally", Fields).GetValue(port) == null, "native drip teardown removes port callbacks", ref checks);
        }
        finally
        {
            foreach (var ppr in readers) pprType.GetMethod("OnDisable").Invoke(ppr, null);
            UnityEngine.Object.DestroyImmediate(root);
            harmonyType.GetMethod("UnpatchAll").Invoke(harmony, new object[] { "s060.cock.drips.offline.audit" });
        }
        return checks;
    }
    private static Assembly Resolve(object sender, ResolveEventArgs args)
    {
        var name = new AssemblyName(args.Name).Name;
        var loaded = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == name);
        if (loaded != null) return loaded;
        string mods = Environment.GetEnvironmentVariable("S060_AUDIT_MODS");
        if (string.IsNullOrEmpty(mods)) mods = Path.GetFullPath(Path.Combine(managed, "..", "..", "Mods"));
        foreach (string directory in new[] { managed, Path.Combine(managed, "UnityModManager"), Path.Combine(mods, "LocoMeshSplitter") })
        { var path = Path.Combine(directory, name + ".dll"); if (File.Exists(path)) return Assembly.LoadFrom(path); }
        return null;
    }
    private static void Check(bool passed, string message, ref int count)
    { if (!passed) throw new InvalidOperationException(message); count++; }
}
#endif
