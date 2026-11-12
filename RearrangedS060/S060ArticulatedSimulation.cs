#if B99_S060_ARTICULATED
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DV.Damage;
using DV.Simulation.Cars;
using HarmonyLib;
using LocoSim.Definitions;
using LocoSim.Implementations;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace RearrangedS060
{
    // S282/L74 pattern: create the extra native channel before Initialize, then
    // select it in place. Never call Initialize or replace the TrainCar on use.
    internal sealed class S060ArticulatedSimulation : MonoBehaviour
    {
        // Legacy constant names retained for existing consumers; these are
        // stable main/auxiliary slots, whose physical role depends on the mode.
        internal const string RearId = S060EngineMapping.MainId, FrontId = S060EngineMapping.AuxiliaryId;
        private static readonly ConditionalWeakTable<SimulationFlow, S060ArticulatedSimulation> Owners = new ConditionalWeakTable<SimulationFlow, S060ArticulatedSimulation>();
        private SimController controller;
        private DamageController damage;
        private SimConnectionDefinition original, owned;
        private GameObject definitions;
        private string[] originalDamagers, originalHealth;
        internal S060EngineBank Bank { get; private set; }
        internal SimulationFlow Flow { get; private set; }
        internal S060ExhaustPresentation Exhaust { get; private set; }
        internal static S060ExhaustPresentation ExhaustFor(SimulationFlow flow) =>
            flow != null && Owners.TryGetValue(flow, out var owner) ? owner.Exhaust : null;
        private string failure;
        private S060ArticulatedTrace trace;
        private TrainCar traceCar;

        private void Update() { if (traceCar != null) trace?.Update(traceCar, this); }
        private void OnDisable() { trace?.Disabled(); }
        private void OnDestroy() { trace?.Dispose(); }

        internal static void Register(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(SimController), nameof(SimController.Initialize)),
                prefix: new HarmonyMethod(typeof(S060ArticulatedSimulation), nameof(BeforeInitialize)),
                postfix: new HarmonyMethod(typeof(S060ArticulatedSimulation), nameof(AfterInitialize)));
            harmony.Patch(AccessTools.Method(typeof(SimulationFlow), nameof(SimulationFlow.SetSaveStateData)),
                prefix: new HarmonyMethod(typeof(S060ArticulatedSimulation), nameof(LoadLegacy)));
        }
        private static void BeforeInitialize(SimController __instance, TrainCar trainCar, DamageController damageController)
        {
            if (!S060Parts.IsS060(trainCar)) return;
            var owner = trainCar.GetComponent<S060ArticulatedSimulation>() ?? trainCar.gameObject.AddComponent<S060ArticulatedSimulation>();
            try { owner.Prepare(__instance, damageController); }
            catch (Exception ex)
            {
                owner.failure = ex.Message; owner.Rollback();
                Main.Logger.LogException("S060 articulated graph unavailable; conventional layouts remain available", ex);
            }
        }
        private static void AfterInitialize(SimController __instance, TrainCar trainCar)
        {
            if (!S060Parts.IsS060(trainCar)) return;
            var owner = trainCar.GetComponent<S060ArticulatedSimulation>();
            if (owner?.owned == null || __instance.connectionsDefinition != owner.owned) return;
            owner.Flow = __instance.simFlow;
            owner.Exhaust = new S060ExhaustPresentation(trainCar, owner.Flow, owner.Bank);
            owner.trace?.Dispose(); owner.trace = new S060ArticulatedTrace(); owner.traceCar = trainCar;
            Require(owner.Bank.Main != null && owner.Bank.Auxiliary != null, "S060 engine channels did not instantiate.");
            Owners.Add(owner.Flow, owner);
            owner.Exhaust.EnrollInitialized();
        }
        internal static S060ArticulatedSimulation Ensure(TrainCar car)
        {
            var owner = car.GetComponent<S060ArticulatedSimulation>();
            if (owner?.Flow == null || owner.Flow != car.SimController?.simFlow || owner.failure != null)
                throw new InvalidOperationException("S060 articulated simulation is not ready. Reload the save first. " + owner?.failure);
            return owner;
        }
        internal void Select(S060EngineMode mode)
        {
            Bank.Select(mode);
            // These sums are also refreshed immediately, so saving or querying
            // before the next tick cannot expose the previous mode's output.
            foreach (string id in new[] { "rs060Intake", "rs060Torque", "rs060Exhaust", "rs060MaxFlow" })
                Flow.OrderedSimComps.Single(c => c.id == id).Tick(0);
            // DV caches max blast flow here. This touches no engine/resource state.
            Flow.OrderedSimComps.OfType<SteamExhaust>().Single().InitializationAfterConnecting();
            Exhaust?.Refresh();
            trace?.SelectionChanged(traceCar, mode);
        }
        private void Prepare(SimController sim, DamageController damageController)
        {
            trace?.Dispose(); trace = null; traceCar = null;
            if (Flow != null) Owners.Remove(Flow);
            Flow = null; Bank = null; failure = null;
            Rollback(); controller = sim; damage = damageController;
            original = controller.connectionsDefinition;
            Require(original != null && damage != null, "S060 graph or damage controller is missing.");
            var order = original.executionOrder;
            Require(order != null && order.All(c => c != null) && order.Select(c => c.ID).Distinct().Count() == order.Length, "S060 graph has missing or duplicate IDs.");
            var main = order.OfType<ReciprocatingSteamEngineDefinition>().Single(c => c.ID == RearId);
            Require(main.numCylinders == 2, "Native S060 must have two cylinders.");
            Require(Array.FindIndex(order, c => c.ID == "steamConsumptionCalculator") < Array.IndexOf(order, main) &&
                Array.FindIndex(order, c => c.ID == "exhaust") > Array.IndexOf(order, main), "Native S060 steam order changed.");
            definitions = new GameObject("[S060 articulated definitions]"); definitions.transform.SetParent(sim.transform, false);
            owned = definitions.AddComponent<SimConnectionDefinition>();
            Bank = new S060EngineBank { Compound = new MeyerCycleOwner { Cycle = S060SteamCalibration.Meyer.CreateCycle() } };
            var rear = Slot(main, false); var front = Slot(main, true);
            var refs = original.portReferenceConnections.Select(c => new PortReferenceConnection(c.portReferenceId, c.portId)).ToList();
            var wires = original.connections.Select(c => new Connection(c.fullPortIdOut, c.fullPortIdIn)).ToList();
            var mainRefs = refs.Where(c => c.portReferenceId.StartsWith(RearId + ".", StringComparison.Ordinal)).ToArray();
            Require(mainRefs.Length == 8, "Native S060 engine wiring changed.");
            foreach (var reference in mainRefs) refs.Add(new PortReferenceConnection(FrontId + reference.portReferenceId.Substring(RearId.Length), reference.portId));
            var intake = Sum("rs060Intake", "INTAKE_FLOW", refs);
            var torque = Sum("rs060Torque", "TORQUE_OUT", refs);
            var exhaust = Sum("rs060Exhaust", "EXHAUST_FLOW", refs);
            var capacity = Sum("rs060MaxFlow", "BLAST_CAPACITY", refs);
            Rewire(refs, "steamConsumptionCalculator.ENGINE", RearId + ".INTAKE_FLOW", intake.ID + ".OUT");
            Rewire(refs, "exhaust.EXHAUST_FLOW", RearId + ".EXHAUST_FLOW", exhaust.ID + ".OUT");
            Rewire(refs, "exhaust.ENGINE_MAX_FLOW", RearId + ".MAX_FLOW", capacity.ID + ".OUT");
            var traction = wires.Single(c => c.fullPortIdIn == "traction.TORQUE_IN");
            Require(traction.fullPortIdOut == RearId + ".TORQUE_OUT", "Native S060 traction already modified.");
            traction.fullPortIdOut = torque.ID + ".OUT";
            var muted = definitions.AddComponent<ConfigurablePortDefinition>(); muted.ID = "rs060MutedCock";
            muted.port = new PortDefinition(PortType.READONLY_OUT, PortValueType.GENERIC, "VALUE"); muted.value = 0;
            var extended = new List<SimComponentDefinition> { muted };
            var gearRatio = definitions.AddComponent<ConfigurablePortDefinition>(); gearRatio.ID = "rs060ShayRatio";
            gearRatio.port = new PortDefinition(PortType.READONLY_OUT, PortValueType.GENERIC, "VALUE"); gearRatio.value = S060ShayEngine.Ratio;
            var gearRpm = definitions.AddComponent<S060ShayRpmDefinition>(); gearRpm.ID = "rs060ShayRpm"; gearRpm.Bank = Bank;
            gearRpm.aReader = new PortReferenceDefinition(PortValueType.RPM, "A");
            gearRpm.bReader = new PortReferenceDefinition(PortValueType.GENERIC, "B");
            gearRpm.mulReadOut = new PortDefinition(PortType.OUT, PortValueType.RPM, "OUT");
            refs.Add(new PortReferenceConnection(gearRpm.ID + ".A", mainRefs.Single(r => r.portReferenceId == RearId + "." + main.crankRpm.ID).portId));
            refs.Add(new PortReferenceConnection(gearRpm.ID + ".B", gearRatio.ID + ".VALUE"));
            extended.Add(gearRatio);
            foreach (var component in order)
            {
                if (component.ID == "steamConsumptionCalculator") extended.Add(intake);
                if (component == main) extended.Add(gearRpm);
                extended.Add(component == main ? rear : component);
                if (component == main) extended.AddRange(new SimComponentDefinition[] { front, torque, exhaust, capacity });
            }
            // Compound: HP alone draws boiler steam; LP alone reaches blast.
            // Stable slots publish zero for the internal transfer on each side.
            owned.executionOrder = extended.ToArray(); owned.connections = wires.ToArray(); owned.portReferenceConnections = refs.ToArray();
            originalDamagers = damage.mechanicalPTDamagerPortIds;
            originalHealth = damage.mechanicalPTHealthStateExternalInPortIds;
            Require(originalDamagers.Contains(RearId + ".GENERATED_MECHANICAL_DAMAGE") && originalHealth.Contains(RearId + ".HEALTH_STATE_EXT_IN"), "Native S060 service wiring missing.");
            damage.mechanicalPTDamagerPortIds = originalDamagers.Concat(new[] { FrontId + ".GENERATED_MECHANICAL_DAMAGE" }).ToArray();
            damage.mechanicalPTHealthStateExternalInPortIds = originalHealth.Concat(new[] { FrontId + ".HEALTH_STATE_EXT_IN" }).ToArray();
            controller.connectionsDefinition = owned;
        }
        private S060EngineSlotDefinition Slot(ReciprocatingSteamEngineDefinition native, bool auxiliary)
        {
            string id = S060EngineMapping.SlotId(auxiliary);
            var slot = Clone<S060EngineSlotDefinition>(native, id);
            slot.Bank = Bank; slot.IsFront = auxiliary; slot.Simple = native;
            slot.Dockside = Clone<ReciprocatingSteamEngineDefinition>(native, id);
            slot.Dockside.cylinderBore = S060SteamCalibration.Dockside.Bore;
            slot.Dockside.pistonStroke = S060SteamCalibration.Dockside.Stroke;
            slot.Dockside.steamChestVolume = S060SteamCalibration.Dockside.ChestLitres;
            slot.Dockside.throttleMaxFlow = S060SteamCalibration.Dockside.MaxFlow;
            if (!auxiliary)
            {
                slot.Shay = Clone<ReciprocatingSteamEngineDefinition>(native, id);
                slot.Shay.numCylinders = 3; slot.Shay.cylinderBore = S060SteamCalibration.Shay.Bore; slot.Shay.pistonStroke = S060SteamCalibration.Shay.Stroke;
                slot.Shay.steamChestVolume = S060SteamCalibration.Shay.ChestLitres; slot.Shay.throttleMaxFlow = S060SteamCalibration.Shay.MaxFlow;
            }
            slot.Meyer = Clone<MeyerStageDefinition>(native, id);
            slot.Meyer.Owner = Bank.Compound; slot.Meyer.HighPressure = !auxiliary;
            slot.Meyer.cylinderBore = auxiliary ? S060SteamCalibration.Meyer.NativeLpBore : S060SteamCalibration.Meyer.NativeHpBore;
            slot.Meyer.pistonStroke = S060SteamCalibration.Meyer.NativeStroke;
            slot.Meyer.steamChestVolume = auxiliary ? S060SteamCalibration.Meyer.LpChestLitres : S060SteamCalibration.Meyer.HpChestLitres;
            slot.Meyer.throttleMaxFlow = (float)(auxiliary ? S060SteamCalibration.Meyer.LpFlow : S060SteamCalibration.Meyer.HpFlow);
            return slot;
        }
        private T Clone<T>(ReciprocatingSteamEngineDefinition source, string id) where T : ReciprocatingSteamEngineDefinition
        {
            var target = definitions.AddComponent<T>();
            foreach (var field in typeof(ReciprocatingSteamEngineDefinition).GetFields(BindingFlags.Instance | BindingFlags.Public))
                if (!field.IsInitOnly && (field.FieldType == typeof(float) || field.FieldType == typeof(int))) field.SetValue(target, field.GetValue(source));
            target.ID = id; return target;
        }
        private MultiplePortSumDefinition Sum(string id, string suffix, List<PortReferenceConnection> refs)
        {
            var sum = definitions.AddComponent<S060InitialSumDefinition>(); sum.ID = id;
            var type = suffix == "TORQUE_OUT" ? PortValueType.TORQUE : PortValueType.MASS_RATE;
            sum.inputs = new[] { new PortReferenceDefinition(type, "REAR"), new PortReferenceDefinition(type, "FRONT") };
            sum.output = new PortDefinition(PortType.OUT, type, "OUT");
            refs.Add(new PortReferenceConnection(id + ".REAR", RearId + "." + suffix)); refs.Add(new PortReferenceConnection(id + ".FRONT", FrontId + "." + suffix));
            return sum;
        }
        private static void Rewire(List<PortReferenceConnection> refs, string id, string expected, string source)
        {
            var wire = refs.Single(c => c.portReferenceId == id); Require(wire.portId == expected, "S060 input already modified: " + id); wire.portId = source;
        }
        private void Rollback()
        {
            if (controller != null && controller.connectionsDefinition == owned) controller.connectionsDefinition = original;
            if (damage != null)
            {
                if (originalDamagers != null) damage.mechanicalPTDamagerPortIds = originalDamagers;
                if (originalHealth != null) damage.mechanicalPTHealthStateExternalInPortIds = originalHealth;
            }
            if (definitions != null) { definitions.SetActive(false); Destroy(definitions); }
            definitions = null; owned = null; originalDamagers = originalHealth = null;
        }
        private static void LoadLegacy(SimulationFlow __instance, ref JObject savedData)
        {
            if (savedData == null || !Owners.TryGetValue(__instance, out _ ) || savedData[FrontId] != null || savedData[RearId]?["rs060EngineVersion"] != null) return;
            savedData = (JObject)savedData.DeepClone(); savedData[FrontId] = new JObject();
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
    internal sealed class S060InitialSumDefinition : MultiplePortSumDefinition
    {
        public override SimComponent InstantiateImplementation() => new S060InitialSum(this);
    }
    internal sealed class S060ShayRpmDefinition : ConfigurableMultiplierDefinition
    {
        internal S060EngineBank Bank;
        public override SimComponent InstantiateImplementation()
        { var multiplier = new ConfigurableMultiplier(this); Bank.ShayRpm = multiplier.mulReadOut; return multiplier; }
    }
    internal sealed class S060InitialSum : MultiplePortSum
    {
        internal S060InitialSum(MultiplePortSumDefinition definition) : base(definition) { }
        public override void InitializationAfterConnecting() => Tick(0);
    }
}
#endif
