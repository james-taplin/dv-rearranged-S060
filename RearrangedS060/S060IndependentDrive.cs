#if B99_S060_ARTICULATED
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DV;
using DV.ECS.Train;
using DV.Simulation.Cars;
using DV.Utils;
using HarmonyLib;
using LocoSim.Definitions;
using LocoSim.Implementations;
using LocoSim.Implementations.Wheels;
using Newtonsoft.Json.Linq;
using Unity.Transforms;
using UnityEngine;

namespace RearrangedS060
{
    // Port of the S282 native group-drive owner. The selected native engines
    // continue to own steam and crank phase; this owner leases only RPM and
    // traction. Each contact contributes once to a separately limited group.
    internal sealed class S060IndependentDrive : MonoBehaviour
    {
        internal sealed class Definition
        {
            internal S060EngineSlot Slot;
            internal Transform EngineRoot;
            internal float Radius;
            internal int[] Contacts;
            internal string Role;
        }
        private sealed class Group
        {
            internal S060EngineSlot Slot;
            internal ReciprocatingSteamEngine Engine;
            internal Transform Root;
            internal string Role;
            internal int Count;
            internal float Radius, Rpm, Velocity, Demand, Force, Brake;
            internal Port OriginalSlotRpm, OriginalEngineRpm, RpmPort;
        }
        private static readonly ConditionalWeakTable<SimulationFlow, S060IndependentDrive> Flows = new ConditionalWeakTable<SimulationFlow, S060IndependentDrive>();
        private static readonly Dictionary<DrivingForce, S060IndependentDrive> Forces = new Dictionary<DrivingForce, S060IndependentDrive>();
        private static int nextGeneration;
        private TrainCar car;
        private S060ArticulatedSimulation simulation;
        private SimulationFlow flow;
        private DrivingForce drivingForce;
        private S060NativeSlipRuntimeState state;
        private Group[] groups;
        private int[,] contactCounts;
        private bool[] drivenBogies;
        private float[] transmittedForces, bogieForces;
        private int powered;
        private float nativeRadius;
        private bool active, released, forceApplicationStarted;
        // Retained donor diagnostic seam: never a release preference or an
        // adhesion boost. Native fixtures can reduce one group's grip only.
        internal float DiagnosticFirstGroupGrip = 1f;
        internal bool IsOwning => active && !released && state != null && state.Ready && state.HasCompletedSample;
        internal static S060IndependentDrive For(TrainCar host) =>
            host?.SimController?.drivingForce != null && Forces.TryGetValue(host.SimController.drivingForce, out var owner) && !owner.released ? owner : null;
        internal bool Settled
        {
            get
            {
                if (!active) return true;
                for (int i = 0; i < groups.Length; i++)
                    if (Mathf.Abs(groups[i].Rpm) >= .1f || state.GetOutput(i).wheelslip != 0) return false;
                return true;
            }
        }
        internal static void Register(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(SimulationFlow), nameof(SimulationFlow.Tick)),
                prefix: new HarmonyMethod(typeof(S060IndependentDrive), nameof(BeforeTick)));
            harmony.Patch(AccessTools.Method(typeof(DrivingForce), "FixedUpdate"),
                prefix: new HarmonyMethod(typeof(S060IndependentDrive), nameof(BeforeForce)));
        }
        internal static S060IndependentDrive Attach(TrainCar car, S060ArticulatedSimulation simulation, Definition[] definitions)
        {
            if (car == null) throw new ArgumentNullException(nameof(car));
            var old = car.GetComponent<S060IndependentDrive>();
            if (old != null && !old.released) throw new InvalidOperationException("Independent drive already owns this car.");
            var owner = car.gameObject.AddComponent<S060IndependentDrive>();
            try { owner.Initialize(car, simulation, definitions); return owner; }
            catch { owner.Release(); Destroy(owner); throw; }
        }
        private void Initialize(TrainCar host, S060ArticulatedSimulation sim, Definition[] definitions)
        {
            if (groups != null || sim?.Flow == null || host.SimController?.drivingForce == null ||
                host.SimController.wheelslipController == null || host.Bogies == null || host.Bogies.Length == 0 ||
                definitions == null || definitions.Length < 2)
                throw new InvalidOperationException("Independent engine/contact map is incomplete.");
            if (Forces.ContainsKey(host.SimController.drivingForce) || Flows.TryGetValue(sim.Flow, out _))
                throw new InvalidOperationException("Native drive already has an independent owner.");
            car = host; simulation = sim; flow = sim.Flow; drivingForce = host.SimController.drivingForce;
            nativeRadius = drivingForce.wheelRadius;
            if (!Positive(nativeRadius)) throw new InvalidOperationException("Native wheel radius is invalid.");
            groups = new Group[definitions.Length];
            contactCounts = new int[definitions.Length, host.Bogies.Length];
            drivenBogies = new bool[host.Bogies.Length];
            transmittedForces = new float[definitions.Length]; bogieForces = new float[host.Bogies.Length];
            for (int i = 0; i < definitions.Length; i++)
            {
                var d = definitions[i];
                if (d?.Slot?.Selected == null || d.EngineRoot == null || !d.EngineRoot.IsChildOf(host.transform) ||
                    !Positive(d.Radius) || d.Contacts == null || d.Contacts.Length != host.Bogies.Length ||
                    d.Slot.crankRpm.port == null || d.Slot.Selected.crankRpm.port == null)
                    throw new InvalidOperationException("Independent group is missing its engine, radius, root or contacts.");
                for (int previous = 0; previous < i; previous++)
                    if (ReferenceEquals(groups[previous].Slot, d.Slot) ||
                        d.EngineRoot.IsChildOf(groups[previous].Root) || groups[previous].Root.IsChildOf(d.EngineRoot))
                        throw new InvalidOperationException("Independent engines and wheel roots must be distinct.");
                int count = 0;
                for (int b = 0; b < host.Bogies.Length; b++)
                {
                    if (d.Contacts[b] < 0) throw new InvalidOperationException("Negative driven contact allocation.");
                    count = checked(count + d.Contacts[b]);
                    contactCounts[i, b] = d.Contacts[b]; drivenBogies[b] |= d.Contacts[b] > 0;
                }
                if (count == 0 || count > ushort.MaxValue) throw new InvalidOperationException("Independent group has invalid driven contacts.");
                powered = checked(powered + count);
                groups[i] = new Group { Slot = d.Slot, Engine = d.Slot.Selected, Root = d.EngineRoot,
                    Radius = d.Radius, Role = d.Role, Count = count,
                    OriginalSlotRpm = d.Slot.crankRpm.port, OriginalEngineRpm = d.Slot.Selected.crankRpm.port,
                    Rpm = host.GetForwardSpeed() * 60f / (2f * Mathf.PI * d.Radius),
                    RpmPort = new Port(d.Slot.id, new PortDefinition(PortType.READONLY_OUT, PortValueType.RPM, "INDEPENDENT_RPM")) };
            }
            if (powered != host.SimController.wheelslipController.NumberOfPoweredAxles)
                throw new InvalidOperationException("Independent contacts do not match native powered axles.");
            ValidateContacts(host);
            Flows.Add(flow, this); Forces.Add(drivingForce, this);
            TryCreateState();
        }
        private void ValidateContacts(TrainCar host)
        {
            var wheels = host.SimController.poweredWheels?.poweredWheels;
            if (wheels == null || wheels.Length != powered)
                throw new InvalidOperationException("Independent contacts do not match the live powered-wheel registry.");
            var observed = new int[groups.Length, host.Bogies.Length];
            for (int w = 0; w < wheels.Length; w++)
            {
                var wheel = wheels[w];
                if (wheel == null || wheel.wheelTransform == null)
                    throw new InvalidOperationException("Independent powered wheel is missing.");
                for (int previous = 0; previous < w; previous++)
                    if (ReferenceEquals(wheels[previous], wheel)) throw new InvalidOperationException("Duplicate independent powered wheel.");
                int group = -1, bogie = -1;
                for (int i = 0; i < groups.Length; i++)
                    if (wheel.wheelTransform.IsChildOf(groups[i].Root)) { group = i; break; }
                for (int b = 0; b < host.Bogies.Length; b++)
                    if (host.Bogies[b] != null && wheel.transform.IsChildOf(host.Bogies[b].transform)) { bogie = b; break; }
                if (group < 0 || bogie < 0)
                    throw new InvalidOperationException("Independent wheel has no unique engine/native bogie owner.");
                observed[group, bogie]++;
            }
            for (int i = 0; i < groups.Length; i++) for (int b = 0; b < host.Bogies.Length; b++)
                if (observed[i, b] != contactCounts[i, b])
                    throw new InvalidOperationException("Declared independent contacts do not match live engine/bogie ownership.");
        }
        private void Update()
        {
            if (released) return;
            try
            {
                if (!ValidEngines() || car.derailed || car.isExploded) { Release(); return; }
                TryCreateState();
                if (IsOwning) S060GroupSlipEffects.Tick(car, this);
            }
            catch (Exception error) { Failed(error); }
        }
        private void TryCreateState()
        {
            if (released || state != null || car?.entity == null || !car.entity.IsConverted ||
                DVConvertToEntity.ConvertSystem == null || DVConvertToEntity.ConvertSystem.World != car.entity.EntityManager.World) return;
            try
            {
                state = new S060NativeSlipRuntimeState(transform, car, groups.Length, ++nextGeneration);
                state.PrepareInputs += Prepare;
                state.OutputsReady += Sample;
                state.Failed += Failed;
            }
            catch (Exception error) { Failed(error); }
        }
        private bool ValidEngines()
        {
            if (car == null || simulation == null || simulation.Flow == null || groups == null ||
                !ReferenceEquals(simulation.Flow, flow) || !ReferenceEquals(car.SimController?.simFlow, flow) ||
                !ReferenceEquals(car.SimController?.drivingForce, drivingForce)) return false;
            for (int i = 0; i < groups.Length; i++)
                if (groups[i] == null || groups[i].Root == null || !ReferenceEquals(groups[i].Slot.Selected, groups[i].Engine)) return false;
            return true;
        }
        private void Failed(Exception error)
        {
            if (released) return;
            Main.Logger.Warning("S060 independent drive unavailable; restoring native drive: " + error);
            Release();
        }
        private void Prepare()
        {
            try { PrepareCore(); }
            catch (Exception error) { Failed(error); }
        }
        private void PrepareCore()
        {
            if (released || !ValidEngines() || car.derailed || car.isExploded) { Release(); return; }
            if (!DV.TimeUtil.IsFlowing || SingletonBehaviour<CarSpawner>.Instance.PoolSetupInProgress ||
                SingletonBehaviour<PausePhysicsHandler>.Instance.PhysicsHandlingInProcess) return;
            var manager = car.entity.EntityManager;
            var shared = manager.GetComponentData<AdhesionControllerSystem.StaticSharedAdhesionCalculationData>(car.entity.Entity);
            var friction = manager.GetComponentData<AdhesionControllerSystem.StaticAdhesionCalculationData>(car.entity.Entity);
            var calc = manager.GetComponentData<AdhesionControllerSystem.AdhesionCalculationData>(car.entity.Entity);
            calc.weightPerAxle = car.massController.WeightPerAxle;
            calc.absSpeed = car.GetAbsSpeed(); calc.sandCoef = car.SimController.wheelslipController.SandCoef;
            calc.brakingForce = car.RearBogie.brakingForce;
            calc.isDerailed = car.derailed; calc.isGrounded = car.groundFriction.IsGrounded;
            state.SetCarrier(shared, friction, calc, new LocalToWorld { Value = car.transform.localToWorldMatrix });
            var slide = manager.GetComponentData<AdhesionControllerSystem.WheelSlideData>(car.entity.Entity);
            var wsc = car.SimController.wheelslipController;
            for (int i = 0; i < groups.Length; i++)
            {
                var group = groups[i];
                group.Demand = group.Slot.torqueOut.Value / group.Radius;
                group.Brake = car.RearBogie.brakingForce * car.Bogies.Length * group.Count / powered;
                state.SetGroup(i, new WheelslipControllerSystem.WheelslipInputData {
                    generatedForce = group.Demand, brakingForce = group.Brake / shared.bogieCount,
                    numberOfPoweredAxles = checked((ushort)group.Count),
                    maxWheelslipRpm = wsc.maxWheelslipRpm * nativeRadius / group.Radius,
                    wheelslipToAdhesionDrop = wsc.wheelslipToAdhesionDrop.Evaluate(state.GetOutput(i).wheelslip),
                    preventWheelslip = wsc.preventWheelslip, isEngineBraking = wsc.IsEngineBraking
                }, shared, slide, i == 0 ? DiagnosticFirstGroupGrip : 1f);
            }
        }
        private void Sample()
        {
            if (released || state == null || !state.HasCompletedSample || active) return;
            try
            {
                for (int i = 0; i < groups.Length; i++)
                {
                    var group = groups[i]; group.RpmPort.Value = group.Rpm;
                    // The wrapper publishes to diagnostics, while the selected
                    // native implementation integrates phase and steam use.
                    group.Slot.crankRpm.port = group.RpmPort;
                    group.Engine.crankRpm.port = group.RpmPort;
                }
                active = true;
            }
            catch (Exception error) { Failed(error); }
        }
        private static void BeforeTick(SimulationFlow __instance, float delta)
        {
            if (Flows.TryGetValue(__instance, out var owner) && owner.IsOwning)
                try { owner.PublishRpm(delta); } catch (Exception error) { owner.Failed(error); }
        }
        private void PublishRpm(float delta)
        {
            if (!ValidEngines() || car.derailed || car.isExploded) { Release(); return; }
            if (!Finite(delta) || delta < 0) throw new InvalidOperationException("Invalid independent RPM step.");
            float speed = car.GetForwardSpeed(), slide = car.adhesionController.wheelSlide;
            if (!Finite(speed) || !Finite(slide)) throw new InvalidOperationException("Invalid native vehicle motion.");
            for (int i = 0; i < groups.Length; i++)
            {
                var group = groups[i]; var output = state.GetOutput(i);
                float ground = speed * 60f / (2f * Mathf.PI * group.Radius);
                float target = slide > 0 ? Mathf.Lerp(ground, 0, slide) : Mathf.Lerp(ground, output.orientedMaxWheelslipRpm, output.wheelslip);
                if (!Finite(target)) throw new InvalidOperationException("Invalid native group wheel speed.");
                group.Rpm = Mathf.SmoothDamp(group.Rpm, target, ref group.Velocity, .2f, float.PositiveInfinity, delta);
                if (target == 0 && Mathf.Abs(group.Rpm) < .01f) group.Rpm = group.Velocity = 0;
                group.RpmPort.Value = group.Rpm;
            }
        }
        private static bool BeforeForce(DrivingForce __instance)
        {
            if (!Forces.TryGetValue(__instance, out var owner) || !owner.IsOwning) return true;
            owner.forceApplicationStarted = false;
            try { return owner.ApplyForce(__instance); }
            catch (Exception error)
            {
                // An unexpected ApplyForce exception after a preceding bogie
                // received force must never run whole-car native traction too.
                bool nativeMayRun = !owner.forceApplicationStarted;
                owner.Failed(error); return nativeMayRun;
            }
        }
        private bool ApplyForce(DrivingForce original)
        {
            if (!ValidEngines() || car.derailed || car.isExploded) { Release(); return true; }
            if (!DV.TimeUtil.IsFlowing || SingletonBehaviour<PausePhysicsHandler>.Instance.PhysicsHandlingInProcess) return false;
            // Finish all validation before applying any force, so a bad contact
            // can restore the native owner without applying a partial group.
            if (car.Bogies.Length != bogieForces.Length) { Release(); return true; }
            for (int b = 0; b < car.Bogies.Length; b++)
                if (drivenBogies[b] && (car.Bogies[b] == null || !car.Bogies[b].enabled ||
                    car.Bogies[b].rb == null || car.Bogies[b].rb.isKinematic)) { Release(); return true; }
            float totalDemand = 0; bool slipping = false;
            for (int i = 0; i < groups.Length; i++)
            {
                var group = groups[i]; var output = state.GetOutput(i);
                float demand = group.Slot.torqueOut.Value / group.Radius;
                if (!Finite(demand) || !Finite(output.totalForceLimit) || output.totalForceLimit < 0 ||
                    !Finite(output.wheelslip) || !Finite(group.Brake) || group.Brake < 0)
                    throw new InvalidOperationException("Invalid independent force calculation.");
                totalDemand += demand;
                if (output.wheelslip > 0) demand = Mathf.Sign(demand) * Mathf.Max(0, Mathf.Abs(demand) - group.Brake);
                transmittedForces[i] = group.Force = car.adhesionController.wheelSlide > 0 ? 0 : Mathf.Clamp(demand, -output.totalForceLimit, output.totalForceLimit);
                slipping |= output.wheelslip > 0;
            }
            if (!Finite(totalDemand)) throw new InvalidOperationException("Invalid total independent force.");
            S060GroupForceRouting.DistributeInto(transmittedForces, contactCounts, bogieForces);
            for (int b = 0; b < bogieForces.Length; b++)
                if (!Finite(bogieForces[b])) throw new InvalidOperationException("Invalid routed independent force.");
            original.generatedForce = totalDemand;
            bool pulling = false;
            for (int b = 0; b < bogieForces.Length; b++) pulling |= bogieForces[b] != 0;
            if ((pulling || slipping) && car.isEligibleForSleep) car.ForceOptimizationState(sleep: false);
            for (int b = 0; b < bogieForces.Length; b++) if (bogieForces[b] != 0)
            {
                forceApplicationStarted = true;
                car.Bogies[b].ApplyForce(bogieForces[b]);
            }
            return false;
        }
        internal bool TryGetWheelState(PoweredWheel wheel, out float slip, out float rpm)
        {
            slip = rpm = 0;
            if (!IsOwning || wheel == null || wheel.wheelTransform == null) return false;
            for (int i = 0; i < groups.Length; i++)
                if (wheel.wheelTransform.IsChildOf(groups[i].Root))
                { slip = state.GetOutput(i).wheelslip; rpm = groups[i].Rpm; return true; }
            return false;
        }
        internal bool TryGetWheelRadius(PoweredWheel wheel, out float radius)
        {
            radius = 0;
            if (released || wheel == null || wheel.wheelTransform == null || groups == null) return false;
            for (int i = 0; i < groups.Length; i++)
                if (wheel.wheelTransform.IsChildOf(groups[i].Root)) { radius = groups[i].Radius; return true; }
            return false;
        }
        internal JObject Report()
        {
            var rows = new JArray();
            if (groups != null) for (int i = 0; i < groups.Length; i++)
            {
                var g = groups[i]; var output = state?.GetOutput(i) ?? default;
                rows.Add(new JObject { ["role"] = g.Role, ["engine"] = g.Slot.id, ["radius"] = g.Radius,
                    ["rpm"] = g.Rpm, ["slip"] = output.wheelslip, ["forceN"] = g.Force,
                    ["limitN"] = output.totalForceLimit, ["contacts"] = g.Count });
            }
            return new JObject { ["owning"] = IsOwning, ["released"] = released, ["groups"] = rows };
        }
        internal void Release()
        {
            if (released) return;
            released = true; active = false;
            if (flow != null && Flows.TryGetValue(flow, out var flowOwner) && ReferenceEquals(flowOwner, this)) Flows.Remove(flow);
            if (!ReferenceEquals(drivingForce, null) && Forces.TryGetValue(drivingForce, out var forceOwner) && ReferenceEquals(forceOwner, this)) Forces.Remove(drivingForce);
            if (groups != null) for (int i = 0; i < groups.Length; i++)
            {
                var group = groups[i]; if (group == null) continue;
                if (ReferenceEquals(group.Slot.crankRpm.port, group.RpmPort)) group.Slot.crankRpm.port = group.OriginalSlotRpm;
                if (ReferenceEquals(group.Engine.crankRpm.port, group.RpmPort)) group.Engine.crankRpm.port = group.OriginalEngineRpm;
                group.Force = 0;
            }
            var retired = state; state = null;
            try { retired?.Dispose(); }
            catch (Exception error) { Main.Logger.Warning("S060 native slip helpers cleanup failed: " + error); }
            if (!ReferenceEquals(car, null))
                try { S060GroupSlipEffects.Release(car); }
                catch (Exception error) { Main.Logger.Warning("S060 group slip effects cleanup failed: " + error); }
        }
        private void OnDisable() => Release();
        private void OnDestroy() => Release();
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Positive(float value) => Finite(value) && value > 0;
    }
}
#endif
