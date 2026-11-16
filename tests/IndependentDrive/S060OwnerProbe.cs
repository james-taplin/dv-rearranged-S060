// The exact candidate coordinator is attached to inactive native component hosts.
// Bogie.ApplyForce is intercepted at the final rail/body boundary, so no train or
// track scene is run. Native ECS, engines, ports and coordinator methods are real.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using DV.ECS.Train;
using DV.ModularAudioCar;
using DV.Simulation.Cars;
using DV.Utils;
using DV.Wheels;
using HarmonyLib;
using LocoSim.Definitions;
using LocoSim.Implementations;
using LocoSim.Implementations.Wheels;
using Newtonsoft.Json.Linq;
using S060SlipFixture;
using Unity.Core;
using Unity.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

internal static class S060OwnerProbe
{
    static readonly BindingFlags Flags = CandidateRuntime.Flags;
    static readonly Dictionary<Bogie, float> forces = new Dictionary<Bogie, float>();
    static readonly Dictionary<Bogie, int> calls = new Dictionary<Bogie, int>();
    static int failAt, forceCalls;
    static bool Capture(Bogie __instance, float inputForce)
    {
        forceCalls++;
        if (failAt != 0 && forceCalls == failAt) throw new InvalidOperationException("Intentional rail-force boundary fault");
        forces[__instance] = (forces.TryGetValue(__instance, out var previous) ? previous : 0) + inputForce;
        calls[__instance] = (calls.TryGetValue(__instance, out var count) ? count : 0) + 1;
        return false;
    }
    static void ClearForces() { forces.Clear(); calls.Clear(); forceCalls = 0; }
    static void Set(object o, string name, object value) => CandidateRuntime.Set(o, name, value);
    static object Get(object o, string name) => CandidateRuntime.Get(o, name);
    static object Call(object o, string name, params object[] args) => CandidateRuntime.Call(o, name, args);
    static Port Port(string name, float value) => new Port("fixture", new PortDefinition(PortType.EXTERNAL_IN, PortValueType.GENERIC, name), value);
    static Port Bound(PortReference reference) => (Port)Get(reference, "port");
    static object Construct(string name) => Activator.CreateInstance(CandidateRuntime.Type(name), true);
    static GameObject Child(string name, Transform parent) { var go = new GameObject(name); go.transform.SetParent(parent, false); return go; }
    static void Near(float a, float b, Action<bool, string> check, string label) => check(Mathf.Abs(a - b) < .01f, label + " (" + a + ", " + b + ")");

    internal static void Run(World world, SimulationSystemGroup simulation, List<string> checks)
    {
        Action<bool, string> check = (value, label) => { if (!value) throw new Exception(label); checks.Add(label); };
        var routing = CandidateRuntime.Type("S060GroupForceRouting");
        var routed = new float[2]; var threeGroups = new int[,] { { 2, 0 }, { 1, 1 }, { 0, 2 } };
        CandidateRuntime.InvokeType(routing, null, "DistributeInto", new float[] { 100, 200, 300 }, threeGroups, routed);
        check(routed[0] == 200 && routed[1] == 400 && routed.Sum() == 600, "Reusable three-group mixed-bogie routing apportions each force exactly once");
        CandidateRuntime.InvokeType(routing, null, "DistributeInto", new float[] { -100, 200, 300 }, threeGroups, routed);
        check(routed[0] == 0 && routed[1] == 400 && routed.Sum() == 400, "Reusable group routing preserves signed force without accumulating prior output");
        bool invalidRouting = false;
        try { CandidateRuntime.InvokeType(routing, null, "DistributeInto", new float[] { 100 }, new int[1, 2], routed); }
        catch (ArgumentException) { invalidRouting = true; }
        check(invalidRouting, "Reusable group routing refuses an engine with no assigned rail contacts");
        var harmony = new Harmony("s060.independent.offline.rail-boundary");
        harmony.Patch(typeof(Bogie).GetMethod("ApplyForce"), prefix: new HarmonyMethod(typeof(S060OwnerProbe).GetMethod("Capture", Flags)));
        var root = new GameObject("[S060 actual coordinator fixture]"); root.SetActive(false);
        object owner = null;
        try
        {
            var loggerType = CandidateRuntime.Type("Main").GetProperty("Logger").PropertyType;
            SetStatic(CandidateRuntime.Type("Main"), "<Logger>k__BackingField", Activator.CreateInstance(loggerType, new object[] { "S060 offline slip fixture" }));
            var car = root.AddComponent<TrainCar>();
            Set(car, "couplers", new Coupler[2]);
            var logic = FormatterServices.GetUninitializedObject(typeof(DV.Logic.Job.Car));
            Set(logic, "carGuid", "ba54f08c-bbea-458f-aad2-d057916b46ce"); Set(car, "logicCar", logic);
            var rb = root.GetComponent<Rigidbody>() ?? root.AddComponent<Rigidbody>(); rb.isKinematic = true; Set(car, "rb", rb);
            Set(car, "numberOfAxles", 4); Set(car, "<isEligibleForSleep>k__BackingField", false);
            var sim = root.AddComponent<SimController>();
            var force = root.AddComponent<DrivingForce>(); Set(force, "wheelRadius", .5f); Set(sim, "drivingForce", force);
            var slip = root.AddComponent<WheelslipController>(); Set(sim, "wheelslipController", slip);
            Set(slip, "numberOfPoweredAxlesPort", Port("powered", 4)); Set(slip, "sandCoefPort", Port("sand", 1));
            slip.maxWheelslipRpm = 370; slip.wheelslipToAdhesionDrop = AnimationCurve.Linear(0, 1, 1, 1);
            var adhesion = (AdhesionController)FormatterServices.GetUninitializedObject(typeof(AdhesionController)); Set(car, "adhesionController", adhesion);
            var ground = (GroundFriction)FormatterServices.GetUninitializedObject(typeof(GroundFriction)); Set(ground, "car", car); Set(car, "groundFriction", ground);
            var mass = FormatterServices.GetUninitializedObject(typeof(TrainMassController)); Set(mass, "car", car);
            Set(mass, "<TotalMass>k__BackingField", 4000f / 9.81f); Set(car, "massController", mass);
            var bogies = new Bogie[2];
            var engineRoots = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                var bogieObject = Child("native bogie " + i, root.transform); bogies[i] = bogieObject.AddComponent<Bogie>();
                var body = bogieObject.AddComponent<Rigidbody>(); body.isKinematic = false; Set(bogies[i], "rb", body);
                engineRoots[i] = Child("powered engine " + i, bogieObject.transform).transform;
            }
            Set(car, "_bogies", bogies);
            var poweredWheels = new PoweredWheel[4];
            for (int i = 0; i < 4; i++)
            {
                int group = i / 2;
                poweredWheels[i] = Child("native powered axle " + i, bogies[group].transform).AddComponent<PoweredWheel>();
                poweredWheels[i].wheelTransform = Child("native wheel alias " + i, engineRoots[group]).transform;
            }
            var wheelManager = new PoweredWheelsManager(); Set(wheelManager, "poweredWheels", poweredWheels); Set(sim, "poweredWheels", wheelManager);
            var shared = new AdhesionControllerSystem.StaticSharedAdhesionCalculationData { bogieCount = 2, numOfAxles = 4 };
            var nativeEntity = root.AddComponent<DVConvertToEntity>();
            nativeEntity.Initialize(world.EntityManager.CreateArchetype(typeof(AdhesionControllerSystem.StaticSharedAdhesionCalculationData),
                typeof(AdhesionControllerSystem.StaticAdhesionCalculationData), typeof(AdhesionControllerSystem.AdhesionCalculationData), typeof(AdhesionControllerSystem.WheelSlideData)));
            Set(car, "<entity>k__BackingField", nativeEntity);
            double elapsed = 100;
            Action stepWorld = () => { elapsed += .02; world.SetTime(new TimeData(elapsed, .02f)); simulation.Update(); };
            stepWorld(); check(nativeEntity.IsConverted, "Actual native car wrapper converts for coordinator attachment");
            world.EntityManager.SetComponentData(nativeEntity.Entity, shared);
            world.EntityManager.SetComponentData(nativeEntity.Entity, new AdhesionControllerSystem.StaticAdhesionCalculationData { wheelslipFrictionCoef = .24f, wheelSlideFrictionCoef = .24f });
            var simOwner = root.AddComponent(CandidateRuntime.Type("S060ArticulatedSimulation"));
            var bank = Construct("S060EngineBank"); var compound = Construct("MeyerCycleOwner");
            Set(compound, "Cycle", CandidateRuntime.InvokeType(CandidateRuntime.Type("S060SteamCalibration+Meyer"), null, "CreateCycle")); Set(bank, "Compound", compound);
            Set(simOwner, "Bank", bank); Set(simOwner, "definitions", Child("native definitions", root.transform));
            var nativeDefinition = Child("stock definition", root.transform).AddComponent<ReciprocatingSteamEngineDefinition>();
            var calibration = JObject.Parse(System.IO.File.ReadAllText(Environment.GetEnvironmentVariable("S060_SLIP_CALIBRATION")))["MonoBehaviour"];
            foreach (var field in typeof(ReciprocatingSteamEngineDefinition).GetFields(Flags))
                if ((field.FieldType == typeof(float) || field.FieldType == typeof(int)) && calibration[field.Name] != null) field.SetValue(nativeDefinition, calibration[field.Name].ToObject(field.FieldType));
            nativeDefinition.ID = "steamEngine";
            var slots = new ReciprocatingSteamEngine[2];
            for (int i = 0; i < 2; i++) slots[i] = (ReciprocatingSteamEngine)((SimComponentDefinition)Call(simOwner, "Slot", nativeDefinition, i == 1)).InstantiateImplementation();
            var input = new Dictionary<string, Port> { ["THROTTLE_CONTROL"] = Port("throttle", 1), ["REVERSER_CONTROL"] = Port("reverser", .7f),
                ["CYLINDER_COCK_CONTROL"] = Port("cocks", 0), ["INTAKE_PRESSURE"] = Port("pressure", 16), ["INTAKE_TEMPERATURE"] = Port("temperature", 200),
                ["INTAKE_QUALITY"] = Port("quality", 1), ["CRANK_RPM"] = Port("rpm", 0), ["LUBRICATION_NORMALIZED"] = Port("oil", 1) };
            Set(bank, "ShayRpm", Port("geared-rpm", 0));
            foreach (var slot in slots)
            {
                foreach (var reference in slot.GetAllPortReferences()) reference.SetPortReference(input[reference.id.Substring(reference.id.LastIndexOf('.') + 1)]);
                slot.SetGameParams(new SimGameParams(true, true, true, 1, 1)); slot.InitializationAfterConnecting();
            }
            Call(bank, "Select", Enum.ToObject(CandidateRuntime.Type("S060EngineMode"), 2));
            var flow = (SimulationFlow)FormatterServices.GetUninitializedObject(typeof(SimulationFlow));
            Set(flow, "OrderedSimComps", slots.Cast<SimComponent>().ToList()); Set(simOwner, "Flow", flow); Set(sim, "simFlow", flow);
            // This fixture formerly had no port registry. Use the production
            // definition extension and real native implementations for the MP port contract.
            var replicationType = CandidateRuntime.Assembly.GetType("RearrangedS060.S060IndependentDriveReplication");
            if (replicationType != null)
            {
                var defs = (SimComponentDefinition[])CandidateRuntime.InvokeType(replicationType, null, "AppendDefinitions", (GameObject)Get(simOwner, "definitions"), new SimComponentDefinition[0]);
                var comps = slots.Cast<SimComponent>().Concat(defs.Select(d => d.InstantiateImplementation())).ToList();
                var ports = comps.SelectMany(c => c.GetAllPorts()).ToList();
                Set(flow, "OrderedSimComps", comps); Set(flow, "AllPorts", ports);
                Set(flow, "fullPortIdToPort", new System.Collections.ObjectModel.ReadOnlyDictionary<string, Port>(ports.ToDictionary(p => p.id)));
                check(defs.Length == 38 && ports.Select(p => p.id).Distinct().Count() == ports.Count, "Actual DLL creates 38 unique native independent-drive replication outputs");
            }
            var driveType = CandidateRuntime.Type("S060IndependentDrive"); var definitionType = driveType.GetNestedType("Definition", Flags);
            var definitions = Array.CreateInstance(definitionType, 2);
            for (int i = 0; i < 2; i++)
            {
                var definition = Activator.CreateInstance(definitionType, true); Set(definition, "Slot", slots[i]); Set(definition, "EngineRoot", engineRoots[i]);
                Set(definition, "Radius", .5f); Set(definition, "Contacts", new[] { i == 0 ? 2 : 0, i == 1 ? 2 : 0 }); Set(definition, "Role", "unit " + i); definitions.SetValue(definition, i);
            }
            var originalSlot = slots.Select(s => Bound(s.crankRpm)).ToArray();
            Func<ReciprocatingSteamEngine, ReciprocatingSteamEngine> selected = slot => (ReciprocatingSteamEngine)Get(slot, "Selected");
            var originalEngine = slots.Select(s => Bound(selected(s).crankRpm)).ToArray();
            Func<object> attach = () => {
                var attached = CandidateRuntime.InvokeType(driveType, null, "Attach", car, simOwner, definitions);
                // The native car host stays inactive to avoid running TrainCar
                // Awake without a game scene. Calculation helpers must remain
                // active, as they are beneath an active car during normal play.
                var state = Get(attached, "state");
                ((GameObject)Get(state, "root")).transform.SetParent(null, false);
                return attached;
            };
            Action release = () => { if (owner == null) return; var state = Get(owner, "state"); var helper = state == null ? null : (GameObject)Get(state, "root"); Call(owner, "Release"); if (helper != null) Object.DestroyImmediate(helper); stepWorld(); };
            owner = attach(); check(!(bool)Get(owner, "IsOwning"), "No group force ownership before completed native sample");
            for (int i = 0; i < 180; i++) { slots[0].torqueOut.Value = 500; slots[1].torqueOut.Value = 50; stepWorld(); }
            check((bool)Get(owner, "IsOwning"), "Actual coordinator attaches and activates after native conversion/sample");
            for (int i = 0; i < 2; i++) check(!ReferenceEquals(Bound(slots[i].crankRpm), originalSlot[i]) && ReferenceEquals(Bound(slots[i].crankRpm), Bound(selected(slots[i]).crankRpm)), "Both wrapper and selected engine lease group RPM, unit " + i);
            for (int i = 0; i < 90; i++) { Call(owner, "PublishRpm", .02f); foreach (var slot in slots) slot.Tick(.02f); }
            check(slots[0].crankRpm.Value > 300 && Mathf.Abs(slots[1].crankRpm.Value) < .01f, "Production group RPM feedback lets slipping front spin while rear grips: " + Call(owner,"Report"));
            check(slots[0].crankRotationReadOut.Value != slots[1].crankRotationReadOut.Value, "Native selected engine phases diverge through separately leased RPM");
            for (int i = 0; i < 2; i++) Near(slots[i].crankRotationReadOut.Value, selected(slots[i]).crankRotationReadOut.Value, check, "Published wrapper phase follows its selected engine " + i);
            slots[0].torqueOut.Value = 500; slots[1].torqueOut.Value = 50; ClearForces();
            bool nativeMayRun = (bool)CandidateRuntime.InvokeType(driveType, null, "BeforeForce", force);
            check(!nativeMayRun && calls.Count == 2 && calls.Values.All(n => n == 1), "Actual coordinator limits and applies each bogie once; pooled native force suppressed");
            Near(forces[bogies[0]], 480, check, "Slipping group receives only its allocated native grip"); Near(forces[bogies[1]], 100, check, "Gripping group does not donate unused adhesion");
            var wheel = poweredWheels[0];
            var wheelArgs = new object[] { wheel, 0f, 0f }; check((bool)driveType.GetMethod("TryGetWheelState", Flags).Invoke(owner, wheelArgs) && (float)wheelArgs[1] > .99f && (float)wheelArgs[2] > 300, "Effects map actual wheel alias to its slipping engine");
            var effectType = CandidateRuntime.Type("S060GroupSlipEffects");
            var sparks = Child("native spark controller", root.transform).AddComponent<WheelslipSparksController>();
            Set(sparks, "car", car); Set(sparks, "wheelslipController", slip);
            // Real S060 layout: stock physics axles are at rail height, stock
            // visual wheel centres and spark anchors stay on the car, and Meyer
            // moves the rear bogie from -1.4 to -6.1 m. Replacement axle origins
            // are visual wheel centres, including the inward front unit.
            root.transform.SetPositionAndRotation(new Vector3(17, 3, -11), Quaternion.Euler(1, 37, -1));
            for (int i = 0; i < 2; i++)
            {
                bogies[i].transform.localPosition = new Vector3(0, 0, i == 0 ? -6.1f : .9f);
                bogies[i].transform.localRotation = Quaternion.Euler(0, i == 0 ? -6 : 6, 0);
                engineRoots[i].localPosition = new Vector3(0, 0, i == 0 ? -.065f : 0);
                engineRoots[i].localRotation = Quaternion.Euler(0, i == 0 ? 0 : 180, 0);
            }
            for (int i = 0; i < poweredWheels.Length; i++)
            {
                poweredWheels[i].wheelTransform.localPosition = new Vector3(0, .5f, i % 2 == 0 ? -.8f : .8f);
                poweredWheels[i].transform.position = poweredWheels[i].wheelTransform.position;
            }
            var originalSparks = new WheelslipSparksController.WheelSparksDefinition[2];
            for (int i = 0; i < 2; i++)
            {
                float stockZ = i == 0 ? -1.4f : 1.7f, sparkOffsetZ = i == 0 ? .002f : .003f;
                var native = Child("retained stock physics axle " + i, bogies[i].transform).AddComponent<PoweredWheel>();
                native.transform.localPosition = new Vector3(0, 0, i == 0 ? 0 : .775f);
                native.wheelTransform = Child("retained stock visual centre " + i, root.transform).transform;
                native.wheelTransform.localPosition = new Vector3(0, .7121296f, stockZ);
                native.wheelTransform.localRotation = Quaternion.Euler(123, 0, 0);
                var left = Child("original left spark " + i, root.transform);
                var right = Child("original right spark " + i, root.transform);
                left.transform.localPosition = new Vector3(-.75f, 0, stockZ + sparkOffsetZ);
                right.transform.localPosition = new Vector3(.75f, 0, stockZ + sparkOffsetZ);
                var leftParticle = left.AddComponent<ParticleSystem>(); var rightParticle = right.AddComponent<ParticleSystem>();
                leftParticle.Stop(); rightParticle.Stop();
                originalSparks[i] = new WheelslipSparksController.WheelSparksDefinition { poweredWheel = native,
                    sparksLeftAnchor = left.transform, sparksRightAnchor = right.transform, sparksLeftPS = leftParticle, sparksRightPS = rightParticle };
            }
            Set(sparks, "wheelSparks", originalSparks);
            var audio = (WheelsAudioModule)FormatterServices.GetUninitializedObject(typeof(WheelsAudioModule)); Set(audio, "trainCar", car);
            var originalAudio = new[] { new WheelsAudioModule.WheelslipAudioDefinition { poweredWheels = new List<PoweredWheel> { poweredWheels[0] } },
                new WheelsAudioModule.WheelslipAudioDefinition { poweredWheels = new List<PoweredWheel> { poweredWheels[2] } } };
            var originalAudioWheels = originalAudio.Select(d => d.poweredWheels).ToArray(); Set(audio, "wheelslipAudioDefs", originalAudio);
            Action bindEffects = () => {
                CandidateRuntime.InvokeType(effectType, null, "Tick", car, owner);
                var audioArgs = new object[] { audio, null };
                effectType.GetMethod("BeforeWheelAudio", Flags).Invoke(null, audioArgs);
                check(audio.wheelslipAudioDefs.Length == 0, "Group audio leases only the native slip definitions during update");
                CandidateRuntime.InvokeType(effectType, null, "AfterWheelAudio", audio, audioArgs[1]);
                check(ReferenceEquals(audio.wheelslipAudioDefs, originalAudio), "Native audio definition array is restored after group output update");
                check(sparks.wheelSparks.Length == 4 && !ReferenceEquals(sparks.wheelSparks, originalSparks), "Native spark leaves bind all four replacement driven axles");
                foreach (var definition in sparks.wheelSparks)
                {
                    int group = Array.IndexOf(poweredWheels, definition.poweredWheel) / 2;
                    float radial = .5f / .712128f, z = (group == 0 ? .002f : .003f) * radial;
                    foreach (var side in new[] { (anchor: definition.sparksLeftAnchor, x: -.75f), (anchor: definition.sparksRightAnchor, x: .75f) })
                    {
                        var expected = new Vector3(side.x, -.7121296f * radial, z);
                        check(Vector3.Distance(side.anchor.localPosition, expected) < .00001f,
                            "Native spark contact follows its replacement wheel centre despite relocated/yawed support and animated stock wheel: " + definition.poweredWheel.name + " / " + side.x);
                        check(Vector3.Distance(side.anchor.position, definition.poweredWheel.transform.TransformPoint(expected)) < .00001f,
                            "Spark contact retains actual articulated axle pose under global rotation: " + definition.poweredWheel.name + " / " + side.x);
                    }
                }
                check(originalAudio.All(d => d.poweredWheels.Count == 2), "Each native audio definition maps its own bogie's two replacement axles");
            };
            Action checkEffectsRestored = () => {
                check(ReferenceEquals(sparks.wheelSparks, originalSparks), "Group release restores exact original native spark definition array");
                check(originalAudio.Select((d, i) => ReferenceEquals(d.poweredWheels, originalAudioWheels[i])).All(value => value), "Group release restores exact native audio wheel-list references");
                check(((IDictionary)effectType.GetField("States", Flags).GetValue(null)).Count == 0, "Group release removes owned effect state");
            };
            bindEffects();
            release();
            checkEffectsRestored();
            for (int i = 0; i < 2; i++) check(ReferenceEquals(Bound(slots[i].crankRpm), originalSlot[i]) && ReferenceEquals(Bound(selected(slots[i]).crankRpm), originalEngine[i]), "Release restores both native RPM references " + i);
            check((bool)CandidateRuntime.InvokeType(driveType, null, "BeforeForce", force), "Stock/native force resumes after group release");
            owner = attach(); for (int i = 0; i < 4; i++) { slots[0].torqueOut.Value = 10; slots[1].torqueOut.Value = 10; stepWorld(); }
            check((bool)Get(owner, "IsOwning"), "Repeated attachment creates a fresh native activation");
            ClearForces(); failAt = 2;
            check(!(bool)CandidateRuntime.InvokeType(driveType, null, "BeforeForce", force) && forceCalls == 2 && calls.Count == 1 && !(bool)Get(owner, "IsOwning"),
                "Fault after first bogie force releases without also invoking pooled native traction");
            failAt = 0; release();
            owner = attach(); for (int i = 0; i < 4; i++) { slots[0].torqueOut.Value = 10; slots[1].torqueOut.Value = 10; stepWorld(); }
            ClearForces(); Set(bogies[1], "rb", null);
            check((bool)CandidateRuntime.InvokeType(driveType, null, "BeforeForce", force) && calls.Count == 0 && !(bool)Get(owner, "IsOwning"), "Invalid last contact releases before any partial force and permits native fallback");
            release();
            Set(bogies[1], "rb", bogies[1].GetComponent<Rigidbody>());
            // Exercise the same exact coordinator with the real finite compound
            // stages. Reduced first-group grip is an existing diagnostic-only
            // native input; torque is equal so the asymmetry is not an animation.
            Call(bank, "Select", Enum.ToObject(CandidateRuntime.Type("S060EngineMode"), 1));
            var meyerPorts = slots.Select(s => Bound(selected(s).crankRpm)).ToArray();
            owner = attach(); Set(owner, "DiagnosticFirstGroupGrip", .25f);
            for (int i = 0; i < 220; i++)
            {
                slots[0].torqueOut.Value = slots[1].torqueOut.Value = 100;
                stepWorld();
                if ((bool)Get(owner, "IsOwning"))
                {
                    Call(owner, "PublishRpm", .02f);
                    foreach (var slot in slots) slot.Tick(.02f);
                }
            }
            check((bool)Get(owner, "IsOwning") && slots[0].crankRpm.Value > 100 && Mathf.Abs(slots[1].crankRpm.Value) < .01f,
                "Meyer HP and LP can receive separate native slip RPM under equal torque/different assigned grip");
            check(slots[0].crankRotationReadOut.Value != slots[1].crankRotationReadOut.Value, "Real compound stages retain separate crank phases while one set slips");
            check(slots[0].intakeFlowReadOut.Value > 0 && slots[1].intakeFlowReadOut.Value == 0,
                "Slipping compound still takes boiler steam through HP only");
            var hot = selected(slots[0]).GetSaveStateData(); var hotAux = selected(slots[1]).GetSaveStateData();
            check(hot["vessels"] is JArray && hot["ledger"] is JArray && (int?)hot["meyerVersion"] == 1,
                "Independent-slip Meyer retains its existing finite receiver/ledger save contract");
            release();
            for (int i = 0; i < 2; i++) check(ReferenceEquals(Bound(selected(slots[i]).crankRpm), meyerPorts[i]), "Meyer release restores original stage RPM reference " + i);
            check(JToken.DeepEquals(hot, selected(slots[0]).GetSaveStateData()) && JToken.DeepEquals(hotAux, selected(slots[1]).GetSaveStateData()),
                "Traction release does not mutate hot compound steam, temperature, damage or phase saves");
            Call(bank, "Select", Enum.ToObject(CandidateRuntime.Type("S060EngineMode"), 0));
            check((bool)CandidateRuntime.InvokeType(driveType, null, "BeforeForce", force), "Return to a conventional single engine uses native traction");
            check(NativeSlipRuntimeState.StateCount == 0, "Coordinator cleanup leaves no registered native helper state");
            check(root.GetComponent<TrainCar>() == car && ReferenceEquals(car.SimController.simFlow, flow), "Coordinator does not replace native car or simulation flow");
            if (replicationType != null)
            {
                // Seed real native ports as the transport would, then run the
                // exact guest coordinator. No API or game networking is started.
                var syncType = CandidateRuntime.Type("S060MultiplayerSync");
                Call(bank, "Select", Enum.ToObject(CandidateRuntime.Type("S060EngineMode"), 2));
                var published = Activator.CreateInstance(replicationType, Flags, null, new object[] { flow, car.CarGUID }, null);
                var channels = (Array)Get(published, "Groups");
                for (int i = 0; i < 2; i++) Call(channels.GetValue(i), "Publish", true, 123f + i, .25f * i, .1f, .35f, 100f, 200f);
                Call(published, "PublishMarker", 77L, 1L, 2);
                CandidateRuntime.InvokeType(syncType, null, "BeginClient");
                CandidateRuntime.InvokeType(syncType, null, "AcceptClient", true, "fixture");
                var lifecycleArgs = new object[] { car, CandidateRuntime.Type("S060Selection").GetProperty("Stock", Flags).GetValue(null), null, false };
                check(!(bool)CandidateRuntime.Type("S060Lifecycle").GetMethod("ApplyLocal", Flags).Invoke(null, lifecycleArgs), "Actual lifecycle refuses direct guest application before touching the native car");
                bool rearRefused = false;
                try { CandidateRuntime.InvokeType(CandidateRuntime.Type("S060MeyerSupport"), null, "RequireRearFree", car); }
                catch (InvalidOperationException) { rearRefused = true; }
                check(rearRefused, "Meyer still rejects an unready rear coupling outside host-approved replay");
                SetStatic(syncType, "applyingCar", car);
                try
                {
                    CandidateRuntime.InvokeType(CandidateRuntime.Type("S060MeyerSupport"), null, "RequireRearFree", car);
                    check(true, "Host-approved replica skips the redundant local Meyer coupling gate");
                }
                finally { SetStatic(syncType, "applyingCar", null); }
                CandidateRuntime.InvokeType(syncType, null, "ReceiveState", new JObject { ["Epoch"] = 77L, ["CarGuid"] = car.CarGUID, ["Arrangement"] = 5, ["Revision"] = 1L }.ToString());
                owner = CandidateRuntime.InvokeType(driveType, null, "Attach", car, simOwner, definitions);
                check(Get(owner, "state") == null && NativeSlipRuntimeState.StateCount == 0, "Guest coordinator creates no force/slip helper entities");
                check((bool)Get(owner, "IsOwning"), "Guest presentation activates only for matching native GUID/epoch/revision ports");
                ClearForces(); check(!(bool)CandidateRuntime.InvokeType(driveType, null, "BeforeForce", force) && calls.Count == 0, "Guest never applies local traction or permits native force fallback");
                var replicaWheel = new object[] { poweredWheels[2], 0f, 0f };
                check((bool)driveType.GetMethod("TryGetWheelState", Flags).Invoke(owner, replicaWheel) && (float)replicaWheel[1] == .25f && (float)replicaWheel[2] == 124f, "Guest sparks/audio receive host group slip and RPM");
                var portValues = flow.AllPorts.Select(p => p.Value).ToArray();
                foreach (var slot in slots) slot.Tick(.02f);
                Call(bank, "Select", Enum.ToObject(CandidateRuntime.Type("S060EngineMode"), 1));
                check(portValues.SequenceEqual(flow.AllPorts.Select(p => p.Value)), "Guest engine ticks and mode selection preserve every replicated native output");
                Call(bank, "Select", Enum.ToObject(CandidateRuntime.Type("S060EngineMode"), 2));
                release();
                check(portValues.SequenceEqual(flow.AllPorts.Select(p => p.Value)), "Guest drive release does not clear host-owned native ports");
                CandidateRuntime.InvokeType(syncType, null, "EndSession");
                check((bool)syncType.GetProperty("IsSimulationAuthority", Flags).GetValue(null), "Native session end restores single-player authority");
                Call(bank, "Select", Enum.ToObject(CandidateRuntime.Type("S060EngineMode"), 3));
                var shay = selected(slots[0]);
                for (int i = 0; i < 100; i++) { input["CRANK_RPM"].Value = 80f; ((Port)Get(bank, "ShayRpm")).Value = 344f; slots[0].Tick(.02f); }
                Near(((Port)Get(slots[0], "GearedTurns")).Value, (float)(double)Get(shay, "GearTurns"), check, "Shay multi-turn gear phase is published through a real native port");
                CandidateRuntime.InvokeType(syncType, null, "BeginClient");
                var gearedPort = (Port)Get(slots[0], "GearedTurns"); gearedPort.Value = 31.25f;
                slots[0].Tick(1f); Call(slots[0], "Publish");
                check(gearedPort.Value == 31.25f, "Guest cannot overwrite host Shay transmission phase");
                CandidateRuntime.InvokeType(syncType, null, "EndSession");
            }
            Call(bank, "Select", Enum.ToObject(CandidateRuntime.Type("S060EngineMode"), 2));
            owner = attach(); for (int i = 0; i < 4; i++) { slots[0].torqueOut.Value = 10; slots[1].torqueOut.Value = 10; stepWorld(); }
            bindEffects(); Object.DestroyImmediate(force); release();
            check(((IDictionary)driveType.GetField("Forces", Flags).GetValue(null)).Count == 0, "Destroyed native DrivingForce releases dictionary ownership despite Unity fake-null");
            checkEffectsRestored();
            force = root.AddComponent<DrivingForce>(); Set(force, "wheelRadius", .5f); Set(sim, "drivingForce", force);
            owner = attach(); for (int i = 0; i < 4; i++) { slots[0].torqueOut.Value = 10; slots[1].torqueOut.Value = 10; stepWorld(); }
            bindEffects(); Object.DestroyImmediate(car); release();
            checkEffectsRestored();
            check(((IDictionary)driveType.GetField("Forces", Flags).GetValue(null)).Count == 0 && NativeSlipRuntimeState.StateCount == 0,
                "Destroyed native TrainCar still restores ports/effects and retires helper ownership");
        }
        finally
        {
            failAt = 0;
            if (owner != null) Call(owner, "Release");
            harmony.UnpatchAll(harmony.Id);
            Object.DestroyImmediate(root);
            world.EntityManager.CompleteAllJobs();
        }
    }
    static void SetStatic(Type type, string name, object value) => type.GetField(name, Flags).SetValue(null, value);
}
