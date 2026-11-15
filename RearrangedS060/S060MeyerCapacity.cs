#if B99_S060_ARTICULATED
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using DV.PitStops;
using DV.Simulation.Cars;
using LocoSim.Implementations;
using LocoSim.Resources;
using UnityEngine;

namespace RearrangedS060
{
    // Meyer R2 adds real water and coal capacity (rear side/well tanks and the high
    // hopper; docs/meyer/R2-PLAN.md). DV's ResourceContainer.capacity is readonly, so it
    // is set by reflection with its CAPACITY port. Saves store the filled fraction, so a
    // restore keeps the fraction (correct whichever of arrangement and resources loads
    // first) and a live switch keeps the actual litres/kilograms aboard.
    internal sealed class S060MeyerCapacity : IDisposable
    {
        internal const float Water = 8800f, Coal = 3500f;
        private static readonly FieldInfo CapacityField = AccessTools.Field(typeof(ResourceContainer), nameof(ResourceContainer.capacity));
        private readonly List<(ResourceContainer container, float original)> changed = new List<(ResourceContainer, float)>();
        private SimulationFlow flow;
        private GameObject servicingRoot;

        internal static S060MeyerCapacity Apply(SimulationFlow flow, bool restoring, GameObject servicingRoot)
        {
            if (CapacityField == null) throw new InvalidOperationException("Native resource capacity field is unavailable.");
            var owner = new S060MeyerCapacity { flow = flow, servicingRoot = servicingRoot };
            try
            {
                foreach (var (id, capacity) in new[] { ("water", Water), ("coal", Coal) })
                {
                    var container = flow.OrderedSimComps.OfType<ResourceContainer>().SingleOrDefault(c => c.id == id)
                        ?? throw new InvalidOperationException("Native S060 " + id + " container is missing.");
                    float amount = restoring ? container.normalizedReadOutPort.Value * capacity : container.amountReadOut.Value;
                    owner.changed.Add((container, container.capacity));
                    Set(container, capacity, amount);
                }
                owner.RefreshMass();
                owner.RefreshServicingLimits();
                return owner;
            }
            catch { owner.Dispose(); throw; }
        }

        private static void Set(ResourceContainer container, float capacity, float amount)
        {
            CapacityField.SetValue(container, capacity);
            if (!S060MultiplayerSync.IsSimulationAuthority) return;
            container.capacityReadOutPort.Value = capacity;
            container.amountReadOut.Value = Math.Max(0f, Math.Min(amount, capacity));
            container.normalizedReadOutPort.Value = container.amountReadOut.Value / capacity;
        }

        private void RefreshServicingLimits()
        {
            // B99 caches maxValue at pit-stop initialization; its normal refresh
            // updates only value. Keep existing data objects so an already-open
            // station sees the new limits too. A later-created native controller
            // initializes directly from the updated container capacities.
            if (servicingRoot == null) return;
            var servicing = servicingRoot.GetComponent<SimulatedCarPitStopParameters>();
            if (servicing == null) return;
            var report = servicing.GetCarPitStopParameters();
            foreach (var (container, _) in changed)
            {
                var type = container.resourceType.ConvertToDVResource();
                if (!report.TryGetValue(type, out var data))
                    throw new InvalidOperationException("Native servicing resource missing: " + type);
                data.maxValue = flow.OrderedSimComps.OfType<ResourceContainer>()
                    .Where(c => c.resourceType == container.resourceType).Sum(c => c.capacity);
            }
        }

        private void RefreshMass()
        {
            // Native resource mass normally refreshes on a two-minute timer.
            // A restore or stock clamp changes actual amounts now; refresh the
            // existing mass owner rather than leave old inertia/grip until then.
            // Early load ordering may not have linked its container controller.
            if (servicingRoot == null) return;
            var controller = servicingRoot.GetComponent<SimController>();
            var car = servicingRoot.GetComponent<TrainCar>();
            if (controller?.resourceContainerController == null || car?.massController == null) return;
            car.massController.UpdateTrainCarMass();
        }

        public void Dispose()
        {
            foreach (var (container, original) in changed)
            {
                float amount = container.amountReadOut.Value;
                if (amount > original)
                    Main.Logger.Log("S060 Meyer removed: " + container.id + " above the stock capacity (" + (amount - original).ToString("0") + ") was not kept.");
                Set(container, original, amount);
            }
            RefreshMass();
            RefreshServicingLimits();
            changed.Clear();
        }
    }
}
#endif
