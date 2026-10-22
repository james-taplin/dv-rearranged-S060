using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CCL.Types.Components;
using CCL.Types.Proxies.Audio;
using CCL.Types.Proxies.Controllers;
using CCL.Types.Proxies.Ports;
using CCL.Types.Proxies.Simulation.Steam;
using CCL.Types.Proxies.VFX;
using UnityEngine;
using RearrangedS060;
using Object = UnityEngine.Object;

// Standard CCL authoring for cab controls, boiler, native readers and damage.
// Only the atmospheric engine implementations are replaced by Stage 5 runtime.
public static class MeyerSteamAuthoring
{
    public const string Hp = "steamEngine", Lp = "meyerLpEngine";
    public static void Apply(GameObject root, Transform front, Transform rear, bool validateCompound = true)
    {
        bool created = root.GetComponentInChildren<SimConnectionsDefinitionProxy>(true) == null;
        if (created)
        {
            var creator = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("CCL.Creator.Wizards.SimSetup.SteamerSimCreator")).First(t => t != null);
            var instance = Activator.CreateInstance(creator, new object[] { root });
            creator.GetMethod("CreateSimForBasis", BindingFlags.Public | BindingFlags.Instance).Invoke(instance, new object[] { 0 });
        }
        var graph = root.GetComponentInChildren<SimConnectionsDefinitionProxy>(true);
        // A freshly generated wizard graph already contains its live lists.
        // AfterImport would replace them with the not-yet-saved empty JSON.
        if (!created) graph.AfterImport();
        var hp = root.GetComponentsInChildren<ReciprocatingSteamEngineDefinitionProxy>(true).Single(d => d.ID == Hp);
        var hpReferences = graph.portReferenceConnections.Where(c => c.portReferenceId.StartsWith(Hp + ".", StringComparison.Ordinal))
            .Select(c => new PortReferenceConnectionProxy { portReferenceId = c.portReferenceId, portId = c.portId }).ToArray();
        var lp = root.GetComponentsInChildren<ReciprocatingSteamEngineDefinitionProxy>(true).SingleOrDefault(d => d.ID == Lp);
        if (lp == null)
        {
            var go = new GameObject(Lp); go.transform.SetParent(graph.transform, false);
            lp = go.AddComponent<ReciprocatingSteamEngineDefinitionProxy>(); lp.ID = Lp;
            foreach (var field in typeof(ReciprocatingSteamEngineDefinitionProxy).GetFields(BindingFlags.Public | BindingFlags.Instance))
                if (field.FieldType == typeof(float) || field.FieldType == typeof(int)) field.SetValue(lp, field.GetValue(hp));
            lp.cylinderBore = hp.cylinderBore * Mathf.Sqrt(2);
            lp.steamChestVolume = hp.steamChestVolume * 2;
            lp.throttleMaxFlow = hp.throttleMaxFlow * 2;
            lp.OnValidate();
            if (hpReferences.Length != 8) throw new InvalidOperationException("Expected eight native HP control/supply references; got " + string.Join(",", hpReferences.Select(c => c.portReferenceId)));
            foreach (var reference in hpReferences)
            {
                string suffix = reference.portReferenceId.Substring(Hp.Length + 1);
                string source = reference.portId;
                if (suffix == "INTAKE_PRESSURE") source = Hp + ".EXHAUST_PRESSURE";
                if (suffix == "INTAKE_TEMPERATURE") source = Hp + ".EXHAUST_TEMPERATURE";
                graph.portReferenceConnections.Add(new PortReferenceConnectionProxy { portReferenceId = Lp + "." + suffix, portId = source });
            }
            var sumGo = new GameObject("meyerTorque"); sumGo.transform.SetParent(graph.transform, false);
            var sum = sumGo.AddComponent<MultiplePortSumDefinitionProxy>(); sum.ID = "meyerTorque";
            sum.inputs = new[] { new PortReferenceDefinition(DVPortValueType.TORQUE, "HP"), new PortReferenceDefinition(DVPortValueType.TORQUE, "LP") };
            sum.output = new PortDefinition(DVPortType.OUT, DVPortValueType.TORQUE, "OUT"); sum.OnValidate();
            graph.portReferenceConnections.Add(new PortReferenceConnectionProxy { portReferenceId = "meyerTorque.HP", portId = Hp + ".TORQUE_OUT" });
            graph.portReferenceConnections.Add(new PortReferenceConnectionProxy { portReferenceId = "meyerTorque.LP", portId = Lp + ".TORQUE_OUT" });
            graph.connections.Single(c => c.fullPortIdIn == "traction.TORQUE_IN").fullPortIdOut = "meyerTorque.OUT";
            graph.portReferenceConnections.Single(c => c.portReferenceId == "exhaust.EXHAUST_FLOW").portId = Lp + ".EXHAUST_FLOW";
            // Boiler's ENGINE consumer stays HP.INTAKE_FLOW: LP reuses steam.
            // CCL OnValidate already registers new definitions. Position our
            // two components once, rather than executing the engines twice.
            graph.executionOrder.RemoveAll(c => c == lp || c == sum);
            graph.executionOrder.InsertRange(graph.executionOrder.IndexOf(hp) + 1, new SimComponentDefinitionProxy[] { lp, sum });
            var damage = root.GetComponent<DamageControllerProxy>();
            damage.mechanicalPTDamagerPortIds = damage.mechanicalPTDamagerPortIds.Concat(new[] { Lp + ".GENERATED_MECHANICAL_DAMAGE" }).ToArray();
            damage.mechanicalPTHealthStateExternalInPortIds = damage.mechanicalPTHealthStateExternalInPortIds.Concat(new[] { Lp + ".HEALTH_STATE_EXT_IN" }).ToArray();
            var axles = root.GetComponentsInChildren<ConfigurablePortDefinitionProxy>(true).Single(d => d.ID == "poweredAxles"); axles.value = 4;
            graph.OnValidate();
        }
        Constant(graph, "meyerLpAdmission", DVPortValueType.CONTROL);
        if (validateCompound)
        {
            hp.cylinderBore = S060SteamCalibration.Meyer.NativeHpBore; hp.pistonStroke = S060SteamCalibration.Meyer.NativeStroke;
            hp.steamChestVolume = S060SteamCalibration.Meyer.HpChestLitres; hp.throttleMaxFlow = (float)S060SteamCalibration.Meyer.HpFlow;
            lp.cylinderBore = S060SteamCalibration.Meyer.NativeLpBore; lp.pistonStroke = S060SteamCalibration.Meyer.NativeStroke;
            lp.steamChestVolume = S060SteamCalibration.Meyer.LpChestLitres; lp.throttleMaxFlow = (float)S060SteamCalibration.Meyer.LpFlow;
        }
        Constant(graph, "meyerReceiverQuality", DVPortValueType.GENERIC);
        graph.portReferenceConnections.Single(c => c.portReferenceId == Lp + ".THROTTLE_CONTROL").portId = "meyerLpAdmission.VALUE";
        graph.portReferenceConnections.Single(c => c.portReferenceId == Lp + ".INTAKE_QUALITY").portId = "meyerReceiverQuality.VALUE";
        // Repair repeated references in the earlier local draft. Distinct
        // component objects with duplicate IDs still fail bundle validation.
        graph.executionOrder = graph.executionOrder.Distinct().ToList();
        graph.OnValidate();
        Effects(validateCompound ? front : rear, Hp); Effects(validateCompound ? rear : front, Lp);
        if (validateCompound) Validate(root);
    }

    private static void Constant(SimConnectionsDefinitionProxy graph, string id, DVPortValueType valueType)
    {
        if (graph.executionOrder.Any(c => c.ID == id)) return;
        var go = new GameObject(id); go.transform.SetParent(graph.transform, false);
        var value = go.AddComponent<ConfigurablePortDefinitionProxy>(); value.ID = id;
        value.value = 1; value.port = new PortDefinition(DVPortType.READONLY_OUT, valueType, "VALUE"); value.OnValidate();
        graph.executionOrder.RemoveAll(c => c == value);
        graph.executionOrder.Insert(0, value);
    }

    private static void Effects(Transform bogie, string engine)
    {
        var effects = new GameObject("[Meyer engine effects]"); effects.transform.SetParent(bogie, false);
        var particles = effects.AddComponent<ParticlesPortReadersControllerProxy>();
        particles.selfInitialization = false; // runtime owns initialization across pooled reuse
        var readerGo = new GameObject("Cylinder cocks"); readerGo.transform.SetParent(effects.transform, false);
        var reader = readerGo.AddComponent<CylinderCockParticlePortReaderProxy>(); reader.ApplyS060Defaults();
        reader.crankRotationPortId = engine + ".CRANK_ROTATION"; reader.reverserPortId = "reverser.REVERSER";
        reader.cylindersInletValveOpenPortId = engine + ".CYLINDERS_INLET_VALVE_OPEN";
        reader.cylinderCockFlowNormalizedPortId = engine + ".CYLINDER_COCK_FLOW_NORMALIZED";
        reader.forwardSpeedPortId = "traction.FORWARD_SPEED_EXT_IN";
        // CCL has native reader proxies and alias copiers, but no controller
        // proxy for body-mounted audio. MeyerRuntime owns their Init/Deinit.
        var audioReader = effects.AddComponent<CylinderCockLayeredPortReaderProxy>();
        audioReader.crankRotationPortId = reader.crankRotationPortId;
        audioReader.cylindersInletValveOpenPortId = reader.cylindersInletValveOpenPortId;
        audioReader.cylinderCockFlowNormalizedPortId = reader.cylinderCockFlowNormalizedPortId;
        audioReader.cylinderCockControlPortId = "cylinderCock.EXT_IN";
        audioReader.cylCockAudio = new GameObject[2];
        particles.particlePortReaders = new List<ParticlesPortReadersControllerProxy.ParticlePortReader>();
        for (int cylinder = 0; cylinder < 2; cylinder++)
        {
            float x = cylinder == 0 ? .85f : -.85f;
            // Candidate tube endpoints, to be fitted to the finished cylinder
            // meshes. Their ownership/yaw is already the actual engine bogie.
            reader.cylinderSetups[cylinder].frontParticlesParent = Outlet(readerGo.transform, "Cock front " + cylinder, new Vector3(x, .48f, 1.45f));
            reader.cylinderSetups[cylinder].rearParticlesParent = Outlet(readerGo.transform, "Cock rear " + cylinder, new Vector3(x, .48f, 1.05f));
            var drip = new GameObject("Water drip " + cylinder); drip.transform.SetParent(effects.transform, false);
            drip.transform.localPosition = new Vector3(x, .45f, 1.25f);
            drip.AddComponent<CopyVanillaParticleSystem>().SystemToCopy = VanillaParticleSystem.SteamerCylCockWaterDrip;
            particles.particlePortReaders.Add(new ParticlesPortReadersControllerProxy.ParticlePortReader
            {
                particlesParent = drip,
                particleUpdaters = new List<ParticlesPortReadersControllerProxy.ParticlePortReader.PortParticleUpdateDefinition>
                { Gate(engine + ".WATER_IN_CYLINDERS_NORMALIZED"), Gate("cylinderCock.EXT_IN") }
            });
            var sound = new GameObject("Cock audio " + cylinder); sound.transform.SetParent(effects.transform, false);
            sound.transform.localPosition = drip.transform.localPosition;
            sound.AddComponent<CopyVanillaAudioSystem>().AudioSystem = VanillaAudioSystem.SteamerCylinderCock;
            audioReader.cylCockAudio[cylinder] = sound;
        }
        // Refresh happens in the standard CCL proxy replacement/native Init.
        reader.OnValidate();
        particles.OnValidate();
        ValidateOutletAliases(effects);
    }

    private static GameObject Outlet(Transform parent, string name, Vector3 position)
    {
        var holder = new GameObject(name); holder.transform.SetParent(parent, false); holder.transform.localPosition = position;
        holder.transform.localRotation = Quaternion.Euler(90, 0, 0);
        holder.AddComponent<CopyVanillaParticleSystem>().SystemToCopy = VanillaParticleSystem.SteamerExhaustSmallWispy;
        // Native S060/S282 cock outlets combine the wispy and wave systems.
        // Keep that authored pair rather than a faint single-system substitute.
        holder.AddComponent<CopyVanillaParticleSystem>().SystemToCopy = VanillaParticleSystem.SteamerExhaustSmallWave;
        return holder;
    }
    private static ParticlesPortReadersControllerProxy.ParticlePortReader.PortParticleUpdateDefinition Gate(string port)
        => new ParticlesPortReadersControllerProxy.ParticlePortReader.PortParticleUpdateDefinition
        {
            portId = port, inputModifier = new ParticlesPortReadersControllerProxy.ValueModifier(),
            propertiesToUpdate = new List<ParticlesPortReadersControllerProxy.ParticlePortReader.PropertyChangeDefinition>
            { new ParticlesPortReadersControllerProxy.ParticlePortReader.PropertyChangeDefinition
                { propertyType = ParticlesPortReadersControllerProxy.ParticlePortReader.ParticleProperty.ON_OFF,
                  propertyChangeCurve = AnimationCurve.Linear(0, 0, 1, 1) } }
        };

    public static void ValidateOutletAliases(GameObject root)
    {
        foreach (var reader in root.GetComponentsInChildren<CylinderCockParticlePortReaderProxy>(true))
            foreach (var setup in reader.cylinderSetups)
                foreach (var outlet in new[] { setup.frontParticlesParent, setup.rearParticlesParent })
                {
                    var aliases = outlet.GetComponents<CopyVanillaParticleSystem>().Select(c => c.SystemToCopy).ToArray();
                    if (aliases.Length != 2 || !aliases.Contains(VanillaParticleSystem.SteamerExhaustSmallWispy) ||
                        !aliases.Contains(VanillaParticleSystem.SteamerExhaustSmallWave))
                        throw new InvalidOperationException("Cock outlet must retain the native wispy/wave pair: " + outlet.name);
                }
    }

    public static void Validate(GameObject root)
    {
        var graph = root.GetComponentInChildren<SimConnectionsDefinitionProxy>(true);
        foreach (string engine in new[] { Hp, Lp })
        {
            var definition = graph.executionOrder.OfType<ReciprocatingSteamEngineDefinitionProxy>().Single(d => d.ID == engine);
            bool highPressure = engine == Hp;
            if (definition.cylinderBore != (highPressure ? S060SteamCalibration.Meyer.NativeHpBore : S060SteamCalibration.Meyer.NativeLpBore) ||
                definition.pistonStroke != S060SteamCalibration.Meyer.NativeStroke ||
                definition.steamChestVolume != (highPressure ? S060SteamCalibration.Meyer.HpChestLitres : S060SteamCalibration.Meyer.LpChestLitres) ||
                definition.throttleMaxFlow != (float)(highPressure ? S060SteamCalibration.Meyer.HpFlow : S060SteamCalibration.Meyer.LpFlow))
                throw new InvalidOperationException("Meyer definition differs from the shared calibration: " + engine);
            if (graph.portReferenceConnections.Single(c => c.portReferenceId == engine + ".CYLINDER_COCK_CONTROL").portId != "cylinderCock.EXT_IN")
                throw new InvalidOperationException("Meyer cock input disconnected: " + engine);
            var cock = root.GetComponentsInChildren<CylinderCockParticlePortReaderProxy>(true).Single(c => c.crankRotationPortId == engine + ".CRANK_ROTATION");
            if (cock.cylinderCockFlowNormalizedPortId != engine + ".CYLINDER_COCK_FLOW_NORMALIZED" || cock.cylinderSetups.Length != 2)
                throw new InvalidOperationException("Wrong Meyer cock particle binding.");
            if (cock.Validate(out string message, out string highlight) != CCL.Types.SelfValidationResult.Pass)
                throw new InvalidOperationException(message);
            if (root.GetComponentsInChildren<CylinderCockLayeredPortReaderProxy>(true).Single(a => a.crankRotationPortId == engine + ".CRANK_ROTATION").cylCockAudio.Any(a => a == null))
                throw new InvalidOperationException("Missing Meyer cylinder-cock sound alias.");
        }
        if (graph.portReferenceConnections.Single(c => c.portReferenceId == "steamConsumptionCalculator.ENGINE").portId != Hp + ".INTAKE_FLOW" ||
            graph.portReferenceConnections.Single(c => c.portReferenceId == "exhaust.EXHAUST_FLOW").portId != Lp + ".EXHAUST_FLOW")
            throw new InvalidOperationException("Meyer serial steam path changed.");
    }
}
