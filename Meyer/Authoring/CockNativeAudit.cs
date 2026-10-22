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
            File.WriteAllText(Path.Combine(output, "native-cock-validation.json"), "{\"passed\":true,\"checks\":" + checks + ",\"reader\":\"installed B99 CylinderCockParticlePortReader\",\"ownedFixedCrankFlowAndReverser\":true,\"unityGameScene\":false}");
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
        var mod = Assembly.LoadFrom(Environment.GetEnvironmentVariable("S060_EXHAUST_AUDIT_RUNTIME"));
        var timing = mod.GetType("RearrangedS060.S060CockTiming", true).GetMethod("Bind", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new[] { reader });
        Check(systems[0].isEmitting && !systems[1].isEmitting, "owned timing enrollment immediately restores actual phase", ref checks);
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
        foreach (string name in new[] { "crankRotation", "reverser", "cylinderCockFlowNormalized" })
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
        foreach (string name in new[] { "crankRotation", "reverser", "cylinderCockFlowNormalized" })
        {
            var callback = (Delegate)portType.GetField("ValueUpdatedInternally", Fields).GetValue(ports["timing." + name]);
            Check(callback == null, "teardown-first disposal cannot resurrect native callback: " + name, ref checks);
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
