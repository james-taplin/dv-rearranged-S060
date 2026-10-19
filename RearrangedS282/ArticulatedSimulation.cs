#if B99_ALL_STANDARD
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DV.Damage;
using DV.Simulation.Cars;
using DV.ThingTypes;
using HarmonyLib;
using LocoSim.Definitions;
using LocoSim.Implementations;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace RearrangedS282
{
    // L74's native arrangement: two native engines, one native traction/slip group.
    // Own only the extra definitions and this car's connection arrays. The original
    // S282 engine, boiler, controllers and component order are never rewritten.
    internal sealed class ArticulatedSimulation : MonoBehaviour
    {
        internal const string MainEngineId = "steamEngine";
        internal const string RearEngineId = "rearrangedRearSteamEngine";
        // Third engine for the triplex. Present (dormant) on every S282 graph so
        // that arrangement can be selected without rebuilding the native flow.
        internal const string MiddleEngineId = "rearrangedMiddleSteamEngine";
        private const string TorqueId = "rearrangedEngineTorque";
        private const string ExhaustId = "rearrangedEngineExhaust";
        private const string IntakeId = "rearrangedEngineIntake";
        private static readonly ConditionalWeakTable<SimulationFlow, ArticulatedSimulation> Owners =
            new ConditionalWeakTable<SimulationFlow, ArticulatedSimulation>();
        private SimController controller;
        private DamageController damage;
        private SimConnectionDefinition originalGraph, ownedGraph;
        private GameObject definitions;
        private string[] originalDamagers, originalHealth;
        private ArticulatedRearEngine rear, middle;
        private Port torqueSum, exhaustSum, intakeSum;
        private string failure;

        internal bool IsActive => rear != null && rear.Active;
        internal ReciprocatingSteamEngine MainEngine { get; private set; }
        internal ReciprocatingSteamEngine RearEngine => rear;
        internal ReciprocatingSteamEngine MiddleEngine => middle;
        internal bool IsMiddleActive => middle != null && middle.Active;
        internal SimulationFlow Flow { get; private set; }
        internal float RearCapacityFraction { get; private set; }

        // Matches WheelArrangement.IsArticulatedId; kept literal so the graph probe compiles alone.
        internal static bool IsDuplexId(int id) => id == 31 || id == 32 || id == 37 || id == 41 || id == 42 || IsTriplexId(id);
        internal const int TriplexId = 38, TrucklessTriplexId = 40;
        internal static bool IsTriplexId(int id) => id == TriplexId || id == TrucklessTriplexId;

        internal static void Register(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(SimController), nameof(SimController.Initialize)),
                prefix: Hook(nameof(BeforeInitialize)), postfix: Hook(nameof(AfterInitialize)));
            harmony.Patch(AccessTools.Method(typeof(SimulationFlow), nameof(SimulationFlow.SetSaveStateData)),
                prefix: Hook(nameof(LoadLegacyState)));
        }

        private static HarmonyMethod Hook(string method) => new HarmonyMethod(typeof(ArticulatedSimulation), method);

        private static bool IsS282(TrainCar car) => car != null && car.carType == TrainCarType.LocoSteamHeavy &&
            car.transform.Find("LocoS282A_Body") != null;

        private static void BeforeInitialize(SimController __instance, TrainCar trainCar, DamageController damageController)
        {
            if (!IsS282(trainCar)) return;
            var owner = trainCar.GetComponent<ArticulatedSimulation>() ?? trainCar.gameObject.AddComponent<ArticulatedSimulation>();
            try { owner.Prepare(__instance, damageController); }
            catch (Exception ex)
            {
                owner.failure = ex.Message;
                owner.RollBackPreparation();
                Main.Logger.Warning("Articulated S282 simulation preparation failed; standard arrangements remain available: " + ex);
            }
        }

        private static void AfterInitialize(SimController __instance, TrainCar trainCar)
        {
            if (!IsS282(trainCar)) return;
            var owner = trainCar.GetComponent<ArticulatedSimulation>();
            if (owner == null || owner.ownedGraph == null || __instance.connectionsDefinition != owner.ownedGraph) return;
            try { owner.Bind(__instance.simFlow); }
            catch (Exception ex)
            {
                owner.failure = ex.Message;
                owner.Restore();
                Main.Logger.Warning("Articulated S282 simulation could not bind its native engines: " + ex);
            }
        }

        internal static ArticulatedSimulation Ensure(TrainCar car)
        {
            if (!IsS282(car)) throw new InvalidOperationException("Articulated simulation supports the S282 only.");
            var owner = car.GetComponent<ArticulatedSimulation>();
            if (owner == null || owner.Flow == null || owner.Flow != car.SimController?.simFlow ||
                owner.MainEngine == null || owner.rear == null || owner.middle == null || owner.failure != null)
                throw new InvalidOperationException("The S282's articulated native simulation is not ready. " +
                    (owner?.failure ?? "This car initialized before the articulated simulation patch.") +
                    " Reload the save or respawn this car before selecting an articulated arrangement.");
            return owner;
        }

        private void Prepare(SimController sim, DamageController damageController)
        {
            Restore();
            if (Flow != null) Owners.Remove(Flow);
            Flow = null;
            MainEngine = null;
            rear = middle = null;
            failure = null;
            if (ownedGraph != null && sim == controller && sim.connectionsDefinition == ownedGraph) return;
            RollBackPreparation();
            controller = sim;
            damage = damageController;
            originalGraph = sim.connectionsDefinition;
            Require(originalGraph != null, "Native S282 connection definition is missing.");
            Require(damage != null, "Native S282 damage controller is missing.");
            var order = originalGraph.executionOrder;
            Require(order != null && order.All(c => c != null), "Native S282 execution order contains a missing component.");
            Require(order.Select(c => c.ID).Distinct().Count() == order.Length, "Native S282 component IDs are duplicated.");
            Require(!order.Any(c => c.ID.StartsWith("rearrangedEngine", StringComparison.Ordinal) || c.ID == RearEngineId || c.ID == MiddleEngineId),
                "Another articulated engine graph already exists.");
            var main = order.OfType<ReciprocatingSteamEngineDefinition>().Single(c => c.ID == MainEngineId);
            Require(main.numCylinders == 2 && Positive(main.cylinderBore) && Positive(main.pistonStroke) &&
                Positive(main.throttleMaxFlow) && Positive(main.steamChestVolume), "Native two-cylinder S282 calibration is invalid.");
            int mainIndex = Array.IndexOf(order, main);
            int demandIndex = Array.FindIndex(order, c => c.ID == "steamConsumptionCalculator");
            int boilerIndex = Array.FindIndex(order, c => c.ID == "boiler");
            Require(demandIndex >= 0 && demandIndex < boilerIndex && boilerIndex < mainIndex,
                "Native S282 previous-tick steam demand order has changed.");
            Require(Array.FindIndex(order, c => c.ID == "exhaust") > mainIndex &&
                Array.FindIndex(order, c => c.ID == "traction") > mainIndex, "Native S282 engine output order has changed.");

            // The original auxiliary definition supplies .55 m / .711 m at .97
            // scale. For this trial, use TWO such pairs as a conservative total
            // capacity benchmark, subtracting the unchanged native main pair.
            // This is a source-informed native equivalent; it does not claim to
            // reproduce B98's old power curve, equal bores or compound expansion.
            float totalRatio = 2f * .55f * .55f * .97f * .97f * .97f * .711f /
                (main.cylinderBore * main.cylinderBore * main.pistonStroke);
            RearCapacityFraction = totalRatio - 1f;
            Require(Positive(RearCapacityFraction) && RearCapacityFraction < 2f,
                "The native S282 cylinder calibration is incompatible with the original duplex capacity.");

            definitions = new GameObject("Rearranged articulated native definitions");
            definitions.transform.SetParent(sim.transform, false);
            ownedGraph = definitions.AddComponent<SimConnectionDefinition>();
            var aux = definitions.AddComponent<ArticulatedRearEngineDefinition>();
            foreach (var field in typeof(ReciprocatingSteamEngineDefinition).GetFields(BindingFlags.Instance | BindingFlags.Public))
                if (!field.IsInitOnly && (field.FieldType == typeof(float) || field.FieldType == typeof(int)))
                    field.SetValue(aux, field.GetValue(main));
            aux.ID = RearEngineId;
            aux.numCylinders = 2;
            aux.cylinderBore = main.cylinderBore * Mathf.Sqrt(RearCapacityFraction);
            aux.steamChestVolume = main.steamChestVolume * RearCapacityFraction;
            aux.throttleMaxFlow = main.throttleMaxFlow * RearCapacityFraction;
            // The triplex middle engine is an identical second auxiliary pair.
            var auxMiddle = definitions.AddComponent<ArticulatedRearEngineDefinition>();
            foreach (var field in typeof(ReciprocatingSteamEngineDefinition).GetFields(BindingFlags.Instance | BindingFlags.Public))
                if (!field.IsInitOnly && (field.FieldType == typeof(float) || field.FieldType == typeof(int)))
                    field.SetValue(auxMiddle, field.GetValue(aux));
            auxMiddle.ID = MiddleEngineId;
            // Physical rear cylinders retain native lubrication, condensate,
            // cracking and damage. L74's virtual-assist damage suppression does
            // not apply to this second physical pair.
            var torque = Sum(TorqueId, PortType.OUT, PortValueType.TORQUE);
            var exhaust = Sum(ExhaustId, PortType.READONLY_OUT, PortValueType.MASS_RATE);
            var intake = Sum(IntakeId, PortType.READONLY_OUT, PortValueType.MASS_RATE);
            var refs = (originalGraph.portReferenceConnections ?? Array.Empty<PortReferenceConnection>())
                .Select(c => new PortReferenceConnection(c.portReferenceId, c.portId)).ToList();
            var wires = (originalGraph.connections ?? Array.Empty<Connection>())
                .Select(c => new Connection(c.fullPortIdOut, c.fullPortIdIn)).ToList();
            var originalMainReferences = refs.Where(c => c.portReferenceId.StartsWith(MainEngineId + ".", StringComparison.Ordinal)).ToArray();
            Require(originalMainReferences.Length == 8, "Native S282 steam-engine reference wiring has changed.");
            foreach (var reference in originalMainReferences)
                foreach (var engineId in new[] { RearEngineId, MiddleEngineId })
                    refs.Add(new PortReferenceConnection(engineId + reference.portReferenceId.Substring(MainEngineId.Length), reference.portId));
            Pair(refs, TorqueId, "TORQUE_OUT");
            Pair(refs, ExhaustId, "EXHAUST_FLOW");
            Pair(refs, IntakeId, "INTAKE_FLOW");
            var tractionWire = wires.Single(c => c.fullPortIdIn == "traction.TORQUE_IN");
            Require(tractionWire.fullPortIdOut == MainEngineId + ".TORQUE_OUT", "S282 traction input is already modified.");
            tractionWire.fullPortIdOut = TorqueId + ".OUT";
            var demandReference = refs.Single(c => c.portReferenceId == "steamConsumptionCalculator.ENGINE");
            Require(demandReference.portId == MainEngineId + ".INTAKE_FLOW", "S282 boiler engine-demand input is already modified.");
            demandReference.portId = IntakeId + ".OUT";
            var exhaustReference = refs.Single(c => c.portReferenceId == "exhaust.EXHAUST_FLOW");
            Require(exhaustReference.portId == MainEngineId + ".EXHAUST_FLOW", "S282 exhaust input is already modified.");
            exhaustReference.portId = ExhaustId + ".OUT";
            // Keep exhaust.ENGINE_MAX_FLOW on the initialized native main.MAX_FLOW:
            // SteamExhaust caches it before any tick-computed sum exists (as L74).
            var extendedOrder = new List<SimComponentDefinition>(order.Length + 5);
            foreach (var item in order)
            {
                if (item.ID == "steamConsumptionCalculator") extendedOrder.Add(intake);
                extendedOrder.Add(item);
                if (ReferenceEquals(item, main)) extendedOrder.AddRange(new SimComponentDefinition[] { aux, auxMiddle, torque, exhaust });
            }
            ownedGraph.executionOrder = extendedOrder.ToArray();
            ownedGraph.connections = wires.ToArray();
            ownedGraph.portReferenceConnections = refs.ToArray();
            originalDamagers = damage.mechanicalPTDamagerPortIds;
            originalHealth = damage.mechanicalPTHealthStateExternalInPortIds;
            Require(originalDamagers != null && originalDamagers.Contains(MainEngineId + ".GENERATED_MECHANICAL_DAMAGE") &&
                originalHealth != null && originalHealth.Contains(MainEngineId + ".HEALTH_STATE_EXT_IN"),
                "Native S282 engine damage/service wiring is missing.");
            damage.mechanicalPTDamagerPortIds = originalDamagers.Concat(new[] { RearEngineId + ".GENERATED_MECHANICAL_DAMAGE",
                MiddleEngineId + ".GENERATED_MECHANICAL_DAMAGE" }).ToArray();
            damage.mechanicalPTHealthStateExternalInPortIds = originalHealth.Concat(new[] { RearEngineId + ".HEALTH_STATE_EXT_IN",
                MiddleEngineId + ".HEALTH_STATE_EXT_IN" }).ToArray();
            sim.connectionsDefinition = ownedGraph;
        }

        private MultiplePortSumDefinition Sum(string id, PortType type, PortValueType valueType)
        {
            var sum = definitions.AddComponent<MultiplePortSumDefinition>();
            sum.ID = id;
            sum.inputs = new[] { new PortReferenceDefinition(valueType, "MAIN"), new PortReferenceDefinition(valueType, "REAR"),
                new PortReferenceDefinition(valueType, "MIDDLE") };
            sum.output = new PortDefinition(type, valueType, "OUT");
            return sum;
        }

        private static void Pair(List<PortReferenceConnection> refs, string sum, string output)
        {
            refs.Add(new PortReferenceConnection(sum + ".MAIN", MainEngineId + "." + output));
            refs.Add(new PortReferenceConnection(sum + ".REAR", RearEngineId + "." + output));
            refs.Add(new PortReferenceConnection(sum + ".MIDDLE", MiddleEngineId + "." + output));
        }

        private void Bind(SimulationFlow flow)
        {
            Require(flow != null, "Native S282 flow was not initialized.");
            MainEngine = flow.OrderedSimComps.OfType<ReciprocatingSteamEngine>().Single(c => c.id == MainEngineId);
            rear = flow.OrderedSimComps.OfType<ArticulatedRearEngine>().Single(c => c.id == RearEngineId);
            middle = flow.OrderedSimComps.OfType<ArticulatedRearEngine>().Single(c => c.id == MiddleEngineId);
            Require(flow.TryGetPort(TorqueId + ".OUT", out torqueSum) &&
                flow.TryGetPort(ExhaustId + ".OUT", out exhaustSum) && flow.TryGetPort(IntakeId + ".OUT", out intakeSum),
                "Native articulated aggregation ports were not constructed.");
            Flow = flow;
            Owners.Remove(flow);
            Owners.Add(flow, this);
            Restore();
        }

        internal void Apply(int id)
        {
            if (!IsDuplexId(id)) throw new ArgumentOutOfRangeException(nameof(id), "Only articulated arrangements use the articulated engine graph.");
            Require(Flow != null && MainEngine != null && rear != null && middle != null && failure == null,
                "The articulated S282 native simulation is not initialized.");
            Activate(rear);
            if (IsTriplexId(id)) Activate(middle);
        }

        private void Activate(ArticulatedRearEngine engine)
        {
            if (engine.Active) return;
            // Every visual engine group uses the same native powered-wheel phase.
            engine.crankRotation = MainEngine.crankRotation;
            engine.prevCrankRotation = MainEngine.crankRotation;
            engine.crankRotationReadOut.Value = MainEngine.crankRotationReadOut.Value;
            engine.Active = true;
        }

        internal void Restore()
        {
            if (rear == null) return;
            rear.Disable();
            middle?.Disable();
            // Clear the last aggregate immediately, before the next native Tick.
            // All sums subsequently pass the unmodified main engine through.
            if (MainEngine == null) return;
            if (torqueSum != null) torqueSum.Value = MainEngine.torqueOut.Value;
            if (exhaustSum != null) exhaustSum.Value = MainEngine.exhaustFlowReadOut.Value;
            if (intakeSum != null) intakeSum.Value = MainEngine.intakeFlowReadOut.Value;
        }

        private static void LoadLegacyState(SimulationFlow __instance, ref JObject savedData)
        {
            if (savedData == null || (savedData[RearEngineId] != null && savedData[MiddleEngineId] != null) ||
                !Owners.TryGetValue(__instance, out _)) return;
            // Native RSE treats the presence of cylCracked as true, even when its
            // JSON value is false. Its correct uncracked default is an EMPTY object.
            var compatible = (JObject)savedData.DeepClone();
            foreach (var id in new[] { RearEngineId, MiddleEngineId })
                if (compatible[id] == null) compatible[id] = new JObject();
            savedData = compatible;
        }

        private void RollBackPreparation()
        {
            Restore();
            if (Flow != null) Owners.Remove(Flow);
            if (controller != null && controller.connectionsDefinition == ownedGraph) controller.connectionsDefinition = originalGraph;
            if (damage != null)
            {
                if (originalDamagers != null) damage.mechanicalPTDamagerPortIds = originalDamagers;
                if (originalHealth != null) damage.mechanicalPTHealthStateExternalInPortIds = originalHealth;
            }
            if (definitions != null) { definitions.SetActive(false); Destroy(definitions); }
            definitions = null;
            ownedGraph = null;
            originalGraph = null;
            originalDamagers = originalHealth = null;
            Flow = null;
            MainEngine = null;
            rear = middle = null;
            torqueSum = exhaustSum = intakeSum = null;
        }

        private void OnDisable() { Restore(); }
        private void OnDestroy() { RollBackPreparation(); }
        private static bool Positive(float value) => value > 0f && !float.IsInfinity(value) && !float.IsNaN(value);
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }

    internal sealed class ArticulatedRearEngineDefinition : ReciprocatingSteamEngineDefinition
    {
        public override SimComponent InstantiateImplementation() => new ArticulatedRearEngine(this);
    }

    // The only extension to the native engine is an explicit component-off state.
    // Zero regulator alone is insufficient: a warm native chest can still exert
    // torque, and cold rolling cylinders can accumulate water damage and wear.
    internal sealed class ArticulatedRearEngine : ReciprocatingSteamEngine
    {
        internal bool Active;
        internal ArticulatedRearEngine(ReciprocatingSteamEngineDefinition definition) : base(definition)
        {
            // Native repair normally observes this one-frame health increase in
            // Tick. Preserve that repair while dormant, without advancing steam
            // or wear. This subscription is wholly owned by this engine's port.
            healthStateExtIn.ValueUpdatedInternally += OnHealthChanged;
        }
        private void OnHealthChanged(float value)
        {
            if (!Active && isCylinderCrackedReadOut.Value > 0f && value > .95f && healthStateExtIn.Diff > 0f)
            {
                isCylinderCrackedReadOut.Value = 0f;
                waterInCylindersDamageMeter = 0f;
            }
        }
        public override void Tick(float delta) { if (Active) base.Tick(delta); }
        internal void Disable()
        {
            Active = false;
            torqueOut.Value = 0f;
            torqueVelocity = 0f;
            intakeFlowReadOut.Value = intakeFlowNormalizedReadOut.Value = 0f;
            exhaustFlowReadOut.Value = exhaustPressureReadOut.Value = exhaustTemperatureReadOut.Value = 0f;
            chuffFrequencyReadOut.Value = 0f;
            cylindersInletValveOpen.Value = 0f;
            cylinderCockFlowNormalizedReadOut.Value = cylinderCrackFlowNormalizedReadOut.Value = 0f;
            generatedMechanicalDamageReadOut.Value = 0f;
            ashesInPipesReadOut.Value = 0f;
            // Keep native chest/temperature/water/crack state: no free refill,
            // implicit repair, event-counter update, or extra dormant engine tick.
        }
    }
}
#endif
