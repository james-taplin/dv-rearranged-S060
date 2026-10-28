#if B99_S060_ARTICULATED
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LocoSim.Implementations;

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

        internal static S060MeyerCapacity Apply(SimulationFlow flow, bool restoring)
        {
            if (CapacityField == null) throw new InvalidOperationException("Native resource capacity field is unavailable.");
            var owner = new S060MeyerCapacity();
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
                return owner;
            }
            catch { owner.Dispose(); throw; }
        }

        private static void Set(ResourceContainer container, float capacity, float amount)
        {
            CapacityField.SetValue(container, capacity);
            container.capacityReadOutPort.Value = capacity;
            container.amountReadOut.Value = Math.Max(0f, Math.Min(amount, capacity));
            container.normalizedReadOutPort.Value = container.amountReadOut.Value / capacity;
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
            changed.Clear();
        }
    }
}
#endif
